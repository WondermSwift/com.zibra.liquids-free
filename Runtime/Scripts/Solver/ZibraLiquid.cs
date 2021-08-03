using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using com.zibra.liquid.SDFObjects;
using com.zibra.liquid.DataStructures;
using com.zibra.liquid.Utilities;
#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_EDITOR
using UnityEditor.Presets;
#endif
using AOT;

namespace com.zibra.liquid.Solver
{
    /// <summary>
    /// Main ZibraFluid solver component
    /// </summary>
    [AddComponentMenu("Zibra/Zibra Liquid")]
    [ExecuteInEditMode] // Careful! This makes script execute in edit mode. 
    // Use "EditorApplication.isPlaying" for play mode only check. 
    // Encase this check and "using UnityEditor" in "#if UNITY_EDITOR" preprocessor directive to prevent build errors
    public class ZibraLiquid : MonoBehaviour
    {
        private static readonly int ReflProbe = Shader.PropertyToID("_ReflProbe");
        private static readonly int ReflProbeHDR = Shader.PropertyToID("_ReflProbe_HDR");
        private static readonly int ReflProbeBoxMax = Shader.PropertyToID("_ReflProbe_BoxMax");
        private static readonly int ReflProbeBoxMin = Shader.PropertyToID("_ReflProbe_BoxMin");
        private static readonly int ReflProbeProbePosition = Shader.PropertyToID("_ReflProbe_ProbePosition");
        private static readonly int VpMatrix = Shader.PropertyToID("VP_Matrix");
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");
        private static readonly int Metal = Shader.PropertyToID("_Metal");
        private static readonly int RefrDistort = Shader.PropertyToID("_RefrDistort");
        private static readonly int Shadowing = Shader.PropertyToID("_Shadowing");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int RefrColor = Shader.PropertyToID("_RefrColor");
        private static readonly int ReflColor = Shader.PropertyToID("_ReflColor");
        private static readonly int ContainerScale = Shader.PropertyToID("containerScale");
        private static readonly int ContainerPos = Shader.PropertyToID("containerPos");
        private static readonly int Size = Shader.PropertyToID("GridSize");
        private static readonly int Diameter = Shader.PropertyToID("diameter");

        /// <summary>
        /// A list of all instances of the ZibraFluid solver
        /// </summary>
        public static List<ZibraLiquid> AllFluids = new List<ZibraLiquid>();

        private const int MPM_THREADS = 256;

        #region PARTICLES

        [StructLayout(LayoutKind.Sequential)]
        private class ParticlesInitValues
        {
            public Vector3 InitPos;
            public int Length;
            public Vector3 InitVel;
            public float InitMass;
            public Vector3 InitScale;
            public int ParticlesCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class CameraRender
        {
            public Matrix4x4 View;
            public Matrix4x4 ViewProjection;
            public Matrix4x4 ViewProjectionInverse;
            public Vector3 WorldSpaceCameraPos;
            public float Diameter;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RenderParameters
        {
            public float blurRadius;
            public float bilateralWeight;
        }

        //List of all cameras we have added a command buffer to
        private readonly Dictionary<Camera, CommandBuffer> cameraCBs = new Dictionary<Camera, CommandBuffer>();
        private List<Camera> cameras = new List<Camera>();
        public Dictionary<Camera, IntPtr> camNativeParams = new Dictionary<Camera, IntPtr>();
        public int MaxNumParticles { get; private set; } = 262144;
        public int NumParticles { get; private set; } = 262144;
        public ComputeBuffer PositionMass { get; private set; }
        public ComputeBuffer PositionRadius { get; private set; } //in world space
        public ComputeBuffer Velocity { get; private set; }
        public ComputeBuffer[] Affine { get; private set; }
      
        public MaterialParametersMPM mpmMaterialParameters;
        public bool isEnabled = true;
        public float particleRadius = 0.1f;
        public float particleMass = 1.0f;
        public Bounds bounds;
        
        private bool usingCustomReflectionProbe;
        private ComputeBuffer drawableGrid;
        public RenderTexture background;
        private RenderTexture color0;
        private RenderTexture color1;
        private Material fluidMaterial;
        private Mesh quad;
        private CommandBuffer renderCommandBuffer;
        private ParticlesInitValues particlesInitValuesParams;
        private CameraRender cameraRenderParams;
        private RenderParameters renderParams;

        #endregion

        #region SOLVER

        /// <summary>
        /// Native solver instance ID number
        /// </summary>
        public int CurrentInstanceID;

        [StructLayout(LayoutKind.Sequential)]
        private class FluidParameters
        {
            public Vector3 GridSize;
            public Int32 NumParticles;
            public Vector3 ContainerScale;
            public Int32 NumNodes;
            public Vector3 ContainerPos;
            public Int32 group;
            public Vector3 gravity;
            public Int32 simulation_frame;
            public Vector3 node_delta;
            public Single dt; //time step
            public Vector3 Direction;
            public Single simulation_time;
            public Single velocity_clamp;
            public Single affine_amount;
            public Single boundary_force;
            public Single boundary_friction;
            public Single dynamic_viscosity;
            public Single eos_stiffness;
            public Single eos_power;
            public Single rest_density;
            public Single elastic_mu;
            public Single elastic_lambda;
            public Single density_compensation;
            public Single deformation_decay;
            public Single NormalizationConstant;
            public Int32 SoftBody;
        }

        private const int BlockDim = 8;
        public Vector3Int GridSizeBlocks { get; private set; }
        public ComputeBuffer GridData { get; private set; }
        public ComputeBuffer IndexGrid { get; private set; }
        public ComputeBuffer GridBlur0 { get; private set; }
        public ComputeBuffer GridNormal { get; private set; }
        public ComputeBuffer GridSDF { get; private set; }
        public ComputeBuffer GridID { get; private set; }
        public ComputeBuffer SurfaceGridType { get; private set; }

        /// <summary>
        /// Current timestep
        /// </summary>
        public float timestep = 0.0f;

        /// <summary>
        /// Simulation time passed (in simulation time units)
        /// </summary>
        public float simulationInternalTime;

        /// <summary>
        /// Number of simulation iterations done so far
        /// </summary>
        public int simulationInternalFrame;

        public ComputeBuffer ParticleDensity;
        private int numNodes;
        private SolverParametersMPM parameters;
        private FluidParameters fluidParametersParams;
        private ComputeBuffer positionMassCopy;
        private ComputeBuffer gridBlur1;
        private ComputeBuffer gridNodePositions;
        private ComputeBuffer nodeParticlePairs;
        private CommandBuffer solverCommandBuffer;
        private CommandBuffer SolverCommandBuffer;

        /// <summary>
        ///Forces acting on the SDF colliders
        /// </summary>
        private Vector3[] ObjectForces;
        /// <summary>
        ///Torques acting on the SDF colliders
        /// </summary>
        private Vector3[] ObjectTorques;
        #endregion

        /// <summary>
        /// Using a reference mesh renderer to set the container size and position
        /// </summary>
        public MeshRenderer ContainerReference
        {
            set
            {
                containerReference = value;
                useContainerReference = true;
                SetContainerReference(value);
            }
            get => containerReference;
        }
        
        public bool IsSimulatingInBackground { get; set; }

        /// <summary>
        /// The grid size of the simulation
        /// </summary>
        public Vector3Int GridSize { get; private set; }

        /// <summary>
        /// Is this instance of the solver currently active
        /// </summary>
        public bool Activated { get; private set; }

        public static string AllsdfCollidersName => nameof(allsdfColliders);
        public static string ContainerReferenceName => nameof(containerReference);
        
#if SHADER_API_D3D11
        public string test;
#endif

#if UNITY_EDITOR
        [Tooltip("The current chosen simulation preset")]
        public Preset simulationPreset;
#endif
        [Tooltip("Use a custom reflection probe")]
        public ReflectionProbe reflectionProbe;

        [Tooltip("The maximum allowed simulation timestep")] [Range(1e-1f, 4.0f)]
        public float timeStepMax = 1.00f;

        [Tooltip("The speed of the simulation, how many simulation time units per second")] [Range(1.0f, 100.0f)]
        public float simTimePerSec = 40.0f;

        [Tooltip("The maximum allowed particle number")] [Range(1024, 4194304)]
        public int maxParticleNumber = 262144;

        [Tooltip("The number of solver iterations per frame, in most cases one iteration is sufficient")] [Range(1, 10)]
        public int iterationsPerFrame = 1;

        [Tooltip("Asynchronously update the liquid - rigid body interaction forces (is faster, but might be less stable)")]
        public bool UseAsyncForceUpdate = true;

        public bool RunSimulation = true;
        
        [Tooltip(
            "Main parameter that regulates the resolution of the simulation. Defines the size of the simulation grid cell in world length units")]
        [Min(1.0e-5f)]
        public float cellSize = 0.1f;

        [Tooltip("Sets the resolution of the largest sid of the grids container equal to this value")]
        [Min(16)]
        public int gridResolution = 128;

        [Range(1e-2f, 16.0f)] 
        public float emitterDensity = 1.0f;

        [Tooltip("Use Native DX11 implemented plugin")]
        public bool useNativePlugin = true;
        public bool runSimulation = true;
        private bool needDraw = true;

        /// <summary>
        /// Main parameters of the simulation
        /// </summary>
        public SolverParametersMPM solverParameters;

        /// <summary>
        /// Main rendering parameters
        /// </summary>
        public MaterialParametersMPM materialParameters;

        /// <summary>
        /// Solver container size
        /// </summary>
        public Vector3 containerSize = new Vector3(10, 10, 10);

        /// <summary>
        /// Solver container position
        /// </summary>
        public Vector3 containerPos;

        /// <summary>
        /// Solver particle emitter size
        /// </summary>
        public Vector3 emitterSize = new Vector3(5, 5, 5);

        /// <summary>
        /// Solver particle position size
        /// </summary>
        public Vector3 emitterPos = new Vector3(0, 5.0f, 0);

        /// <summary>
        /// Should we use a container reference
        /// </summary>
        public bool useContainerReference;

        /// <summary>
        /// Initial velocity of the fluid
        /// </summary>
        public Vector3 fluidInitialVelocity;

        /// <summary>
        /// List of used SDF colliders
        /// </summary>
        public List<SDFCollider> sdfColliders;
        
        public int avgFrameRate;
        public float deltaTime;
        public float smoothDeltaTime;

        /// <summary>
        /// Container reference
        /// </summary>
        [SerializeField] private MeshRenderer containerReference;

        /// <summary>
        /// List of all SDF colliders
        /// </summary>
        [SerializeField] private List<SDFCollider> allsdfColliders;
        
        private bool wasError;
        private bool needForceInteractionUpdate;

        /// <summary>
        /// Is solver initialized
        /// </summary>
        public bool initialized;

#if UNITY_PIPELINE_HDRP
        private FluidHDRPRenderComponent hdrpRenderer;
#endif

#if UNITY_PIPELINE_URP
        private FluidURPRenderComponent urpRenderer;
#endif

        /// <summary>
        /// Activate the solver
        /// </summary>
        public void Run()
        {
            runSimulation = true;
        }

        /// <summary>
        /// Stop the solver
        /// </summary>
        public void Stop()
        {
            runSimulation = false;
        }

        protected void Awake()
        {
#if UNITY_EDITOR
#if UNITY_PIPELINE_HDRP
            needDraw = false;
            hdrpRenderer = gameObject.GetComponent<FluidHDRPRenderComponent>();
            if (hdrpRenderer == null)
            {
                hdrpRenderer = gameObject.AddComponent<FluidHDRPRenderComponent>();
                hdrpRenderer.fluidPass = hdrpRenderer.AddPassOfType(typeof(FluidHDRPRenderComponent.FluidHDRPRender)) as FluidHDRPRenderComponent.FluidHDRPRender;
                hdrpRenderer.fluidPass.name = "ZibraLiquidRenderer";
                hdrpRenderer.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
                hdrpRenderer.fluidPass.liquid = this;
            }
#endif
#if UNITY_PIPELINE_URP
            needDraw = false;
#endif
#endif
        }

        protected void OnEnable()
        {
            if (!AllFluids?.Contains(this) ?? false)
            {
                AllFluids.Add(this);
            }

            if (sdfColliders == null)
                sdfColliders = new List<SDFCollider>();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif
            Init();
        }

        private void InitializeParticles(int number,
            Vector3 position, Vector3 velocity, Vector3 scale,
            float targetCellSize, float density)
        {
            quad = PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Quad);
            MaxNumParticles = number;

            var emitterCells = scale;

            NumParticles = (int) (density * emitterCells.x * emitterCells.y * emitterCells.z);
            NumParticles = Math.Min(NumParticles, MaxNumParticles);
            isEnabled = true;
            var numParticlesRounded =
                (int) Math.Ceiling((double) NumParticles / MPM_THREADS) * MPM_THREADS; // round to workgroup size

            GridSize = Vector3Int.CeilToInt(containerSize / targetCellSize);

            PositionMass = new ComputeBuffer(NumParticles, 4 * sizeof(float));
            PositionRadius = new ComputeBuffer(NumParticles, 4 * sizeof(float));
            Affine = new ComputeBuffer[2];
            Affine[0] = new ComputeBuffer(4 * numParticlesRounded, 2 * sizeof(int));
            Affine[1] = new ComputeBuffer(4 * numParticlesRounded, 2 * sizeof(int));

            var currentNumNodes = GridSize[0] * GridSize[1] * GridSize[2];

            drawableGrid = new ComputeBuffer(currentNumNodes, sizeof(uint), ComputeBufferType.Counter);

            fluidMaterial = new Material(Resources.Load<Shader>("Shaders/FluidShader"));

            particlesInitValuesParams = new ParticlesInitValues();
            cameraRenderParams = new CameraRender();
            renderParams = new RenderParameters();

            //Create render command buffer
            renderCommandBuffer = new CommandBuffer
            {
                name = "ZibraLiquid.Render"
            };

            particlesInitValuesParams.InitMass = particleMass;
            particlesInitValuesParams.ParticlesCount = NumParticles;
            particlesInitValuesParams.InitPos = position;
            particlesInitValuesParams.InitVel = velocity;
            particlesInitValuesParams.InitScale = scale;

            GCHandle gcparamBuffer = GCHandle.Alloc(particlesInitValuesParams, GCHandleType.Pinned);

            ZibraLiquidBridge.RegisterParticlesBuffers(CurrentInstanceID, gcparamBuffer.AddrOfPinnedObject(),
                PositionMass.GetNativeBufferPtr(), Affine[0].GetNativeBufferPtr(), Affine[1].GetNativeBufferPtr(),
                drawableGrid.GetNativeBufferPtr(), PositionRadius.GetNativeBufferPtr());
            gcparamBuffer.Free();
            renderCommandBuffer.IssuePluginEvent(ZibraLiquidBridge.GetRenderEventFunc(), ZibraLiquidBridge.EventAndInstanceID(1, CurrentInstanceID));

            Graphics.ExecuteCommandBuffer(renderCommandBuffer);
        }

        private void InitializeSolver(Camera currentCamera, SolverParametersMPM targetSolverParameters, float csize)
        {
            simulationInternalTime = 0.0f;
            simulationInternalFrame = 0;
            parameters = solverParameters;
            cellSize = csize;
            numNodes = GridSize[0] * GridSize[1] * GridSize[2];
            GridSizeBlocks = Vector3Int.CeilToInt(new Vector3Int(GridSize.x / BlockDim + 1, GridSize.y / BlockDim + 1, GridSize.z / BlockDim + 1));
            GridData = new ComputeBuffer(numNodes, 2 * sizeof(int));
            GridNormal = new ComputeBuffer(numNodes, 4 * sizeof(float));
            GridBlur0 = new ComputeBuffer(numNodes, 2 * sizeof(int));
            gridBlur1 = new ComputeBuffer(numNodes, 2 * sizeof(int));
            GridSDF = new ComputeBuffer(numNodes, sizeof(float));
            GridID = new ComputeBuffer(numNodes, sizeof(int));
            gridNodePositions = new ComputeBuffer(numNodes, 4 * sizeof(float));

            ObjectForces = new Vector3[sdfColliders.Count];
            ObjectTorques = new Vector3[sdfColliders.Count];

            IndexGrid = new ComputeBuffer(numNodes, 2 * sizeof(int));

            int NumParticlesRounded = (int)Math.Ceiling((double)NumParticles / MPM_THREADS) * MPM_THREADS;// round to workgroup size

            ParticleDensity = new ComputeBuffer(NumParticlesRounded, sizeof(float));
            positionMassCopy = new ComputeBuffer(NumParticlesRounded, 4 * sizeof(float));
            nodeParticlePairs = new ComputeBuffer(NumParticlesRounded, 2 * sizeof(int));

            solverCommandBuffer = new CommandBuffer
            {
                name = "ZibraLiquid.Solver"
            };

            fluidParametersParams = new FluidParameters();

            SetFluidParameters();

            var gcparamBuffer = GCHandle.Alloc(fluidParametersParams, GCHandleType.Pinned);

            ZibraLiquidBridge.RegisterSolverBuffers(CurrentInstanceID, gcparamBuffer.AddrOfPinnedObject(),
                positionMassCopy.GetNativeBufferPtr(), ParticleDensity.GetNativeBufferPtr(),
                GridData.GetNativeBufferPtr(), IndexGrid.GetNativeBufferPtr(), GridBlur0.GetNativeBufferPtr(),
                gridBlur1.GetNativeBufferPtr(), GridNormal.GetNativeBufferPtr(),
                GridSDF.GetNativeBufferPtr(), gridNodePositions.GetNativeBufferPtr(),
                nodeParticlePairs.GetNativeBufferPtr(), GridID.GetNativeBufferPtr());
            gcparamBuffer.Free();

            Graphics.ExecuteCommandBuffer(solverCommandBuffer);
        }

        /// <summary>
        /// Initializes a new instance of ZibraFluid
        /// </summary>
        public void Init()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            try
            {
                Vector3 container = containerPos - containerSize * 0.5f;
                Vector3 newpos = (emitterPos + containerSize * 0.5f) / cellSize;

                CurrentInstanceID = ZibraLiquidBridge.CreateFluidInstance();

                InitializeParticles(maxParticleNumber, newpos, fluidInitialVelocity, emitterSize / cellSize, cellSize,
                    solverParameters.ParticlesPerCell * emitterDensity);
                InitializeSolver(Camera.main, solverParameters, cellSize);
                ZibraLiquidBridge.SetCollidersCount(CurrentInstanceID, FindObjectsOfType<SDFCollider>().Length);

                Activated = true;
            }
            catch
            {
                wasError = true;
                Debug.LogError("Fatal error, Zibra Liquid not initialized");
                Activated = false;
                throw;
            }
        }

        /// <summary>
        /// Sets the container reference
        /// </summary>
        /// <param name="referenceRenderer">Reference renderer</param>
        private void SetContainerReference(Renderer referenceRenderer = null)
        {
            if (referenceRenderer != null)
            {
                containerPos = transform.InverseTransformPoint(referenceRenderer.bounds.center);
                containerSize = referenceRenderer.bounds.size;
            }
        }

        protected void Update()
        {
            allsdfColliders = SDFCollider.AllColliders;

            if (useContainerReference) SetContainerReference();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) return;
#endif

            if (!initialized) return;

            if (wasError) return;

            deltaTime = Time.deltaTime;
            smoothDeltaTime = smoothDeltaTime * 0.98f + deltaTime * 0.02f;

            var timeStep = System.Math.Min(simTimePerSec * smoothDeltaTime / (float)iterationsPerFrame, timeStepMax);

            if (runSimulation)
            {
                for (var i = 0; i < iterationsPerFrame; i++)
                {
                    StepPhysics(timeStep);
                }
            }

            mpmMaterialParameters = materialParameters;
            particleRadius = mpmMaterialParameters.ParticleScale * cellSize /
                             (float)Math.Pow(solverParameters.ParticlesPerCell, 0.333f);
            particleMass = 1.0f;

            if (needDraw)
            {
                foreach (var cam in Camera.allCameras)
                {
                    DrawTotal(cam);
                }
            }
        }


        /// <summary>
        /// Update the camera parameters for the particle renderer
        /// </summary>
        /// <param name="cam">Camera</param>
        public void UpdateCamera(Camera cam)
        {
            if (!camNativeParams.ContainsKey(cam)) return;

            cameraRenderParams.View = cam.worldToCameraMatrix;
            cameraRenderParams.ViewProjection = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
            cameraRenderParams.ViewProjectionInverse = cameraRenderParams.ViewProjection.inverse;
            cameraRenderParams.Diameter = particleRadius;
            cameraRenderParams.WorldSpaceCameraPos = cam.transform.position;
            
            //update the data at the pointer
            Marshal.StructureToPtr(cameraRenderParams, camNativeParams[cam], true);
        }

        /// <summary>
        /// Update render parameters for a given camera
        /// </summary>
        /// <param name="cam">Camera</param>
        public void UpdateNativeRenderParams(Camera cam)
        {
            if(camNativeParams.Count == 0)
            {
                //For the built-in rendering pipeline
                Camera.onPreRender += UpdateCamera;
            }

            if(!camNativeParams.ContainsKey(cam))
            {
                //allocate memory for this camera parameters
                camNativeParams[cam] = Marshal.AllocHGlobal(Marshal.SizeOf(cameraRenderParams));
                //add camera to list
                cameras.Add(cam);
                //update parameters
                UpdateCamera(cam);
                //set initial parameters in the native plugin
                ZibraLiquidBridge.SetCameraParameters(CurrentInstanceID, camNativeParams[cam]);
            }

            renderParams.blurRadius = mpmMaterialParameters.BlurRadius;
            renderParams.bilateralWeight = mpmMaterialParameters.BilateralWeight;

            GCHandle gcparamBuffer = GCHandle.Alloc(renderParams, GCHandleType.Pinned);
            ZibraLiquidBridge.SetRenderParameters(CurrentInstanceID, gcparamBuffer.AddrOfPinnedObject());
            gcparamBuffer.Free();
        }

        /// <summary>
        /// Render the particles from the native plugin
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        public void RenderParticelsNative(CommandBuffer cmdBuffer)
        {
            cmdBuffer.SetRenderTarget(color0);
            cmdBuffer.ClearRenderTarget(true, true, Color.clear);
            cmdBuffer.IssuePluginEvent(ZibraLiquidBridge.GetRenderEventFunc(), ZibraLiquidBridge.EventAndInstanceID(4, CurrentInstanceID));
        }

        /// <summary>
        /// Render the fluid surface
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        public void RenderFluid(CommandBuffer cmdBuffer)
        {
            cmdBuffer.SetGlobalTexture("_Background", background);
            cmdBuffer.SetGlobalTexture("_FluidColor", color0);
            fluidMaterial.SetBuffer("GridNormal", GridNormal);

            if (usingCustomReflectionProbe)
                cmdBuffer.EnableShaderKeyword("CUSTOM_REFLECTION_PROBE");
            else
                cmdBuffer.DisableShaderKeyword("CUSTOM_REFLECTION_PROBE");
            
            cmdBuffer.DrawMesh(quad, transform.localToWorldMatrix, fluidMaterial, 0, 0);

            Graphics.ExecuteCommandBuffer(cmdBuffer);
        }

        /// <summary>
        /// Render the fluid
        /// </summary>
        /// <param name="cam">Camera to render to</param>
        public void DrawTotal(Camera cam)
        {
            if (!isEnabled || fluidMaterial == null)
                return;

            SetMaterialParams();
            bool isDirty = UpdateNativeTextures(cam);
            UpdateNativeRenderParams(cam);

            //Already added the command buffer to this camera
            if (!cameraCBs.ContainsKey(cam) || isDirty)
            {
                CameraEvent cameraEvent = (cam.actualRenderingPath == RenderingPath.Forward) ? CameraEvent.BeforeForwardAlpha : CameraEvent.AfterLighting;

                if (isDirty && cameraCBs.ContainsKey(cam))
                    cam.RemoveCommandBuffer(cameraEvent, cameraCBs[cam]);

                renderCommandBuffer.Dispose();
                renderCommandBuffer = new CommandBuffer();

                //add command buffer to camera
                cam.AddCommandBuffer(cameraEvent, renderCommandBuffer);

                //enable depth texture
                cam.depthTextureMode = DepthTextureMode.Depth;

                //update native camera parameters
                renderCommandBuffer.IssuePluginEventAndData(ZibraLiquidBridge.GetCameraUpdateFunction(), ZibraLiquidBridge.EventAndInstanceID(0, CurrentInstanceID), camNativeParams[cam]);

                renderCommandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, background);
                RenderParticelsNative(renderCommandBuffer);
                renderCommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                RenderFluid(renderCommandBuffer);

                //add camera to the list
                cameraCBs[cam] = renderCommandBuffer;
            }
        }

        /// <summary>
        /// Update the material parameters
        /// </summary>
        public void SetMaterialParams()
        {
            fluidMaterial.SetFloat("_Opacity", mpmMaterialParameters.Opacity);
            fluidMaterial.SetFloat("_Metal", mpmMaterialParameters.Metal);
            fluidMaterial.SetFloat("_RefrDistort", mpmMaterialParameters.RefractionDistort);
            fluidMaterial.SetFloat("_Shadowing", mpmMaterialParameters.Shadowing);
            fluidMaterial.SetFloat("_Smoothness", mpmMaterialParameters.Smoothness);
           
            fluidMaterial.SetVector("_RefrColor", mpmMaterialParameters.RefractionColor);
            fluidMaterial.SetVector("_ReflColor", mpmMaterialParameters.ReflectionColor);
            
            fluidMaterial.SetVector("containerScale", containerSize);
            fluidMaterial.SetVector("containerPos", containerPos);
            fluidMaterial.SetVector("GridSize", (Vector3)GridSize);
            fluidMaterial.SetFloat("_Foam", mpmMaterialParameters.Foam);
            fluidMaterial.SetFloat("_FoamDensity", mpmMaterialParameters.FoamDensity * parameters.ParticlesPerCell);
            fluidMaterial.SetFloat("diameter", particleRadius);

            if (reflectionProbe != null)
            {
                usingCustomReflectionProbe = true;
                //custom reflection probe
                fluidMaterial.SetTexture(ReflProbe, reflectionProbe.texture);
                fluidMaterial.SetVector(ReflProbeHDR, reflectionProbe.textureHDRDecodeValues);
                fluidMaterial.SetVector(ReflProbeBoxMax, reflectionProbe.bounds.max);
                fluidMaterial.SetVector(ReflProbeBoxMin, reflectionProbe.bounds.min);
                fluidMaterial.SetVector(ReflProbeProbePosition, reflectionProbe.transform.position);
            }
            else usingCustomReflectionProbe = false;
        }

        /// <summary>
        /// Update Native textures for a given camera
        /// </summary>
        /// <param name="cam">Camera</param>
        public bool UpdateNativeTextures(Camera cam)
        {
            int width = cam.pixelWidth;
            int height = cam.pixelHeight;

            bool isDirty = false;

            if (background == null || background.width != width || background.height != height)
            {
                if (background != null)
                {
                    background.Release();
                    DestroyImmediate(background);
                }

                background = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
                background.Create();
            }

            if (color0 == null || color0.width != width || color0.height != height)
            {
                if (color0 != null)
                {
                    color0.Release();
                    DestroyImmediate(color0);
                }

                color0 = new RenderTexture(width, height, 16, RenderTextureFormat.ARGBFloat);
                color0.enableRandomWrite = true;
                color0.Create();
                isDirty = true;
            }

            if (color1 == null || color1.width != width || color1.height != height)
            {
                if (color1 != null)
                {
                    color1.Release();
                    DestroyImmediate(color1);
                }

                color1 = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
                color1.enableRandomWrite = true;
                color1.Create();
                isDirty = true;
            }

            if (isDirty)
            {
                ZibraLiquidBridge.RegisterRenderResources(CurrentInstanceID, color0.GetNativeTexturePtr(), color1.GetNativeTexturePtr());
            }

            return isDirty;
        }

        private void StepPhysics(float dt)
        {
            if (dt <= 0.0)
            {
                return;
            }

            timestep = dt;
            solverCommandBuffer.Clear();

            SetFluidParameters();
            AddColliderSDFs();

            var gcparamBuffer = GCHandle.Alloc(fluidParametersParams, GCHandleType.Pinned);
            ZibraLiquidBridge.SetFluidParameters(CurrentInstanceID, gcparamBuffer.AddrOfPinnedObject());
            gcparamBuffer.Free();

            solverCommandBuffer.IssuePluginEvent(ZibraLiquidBridge.GetRenderEventFunc(), ZibraLiquidBridge.EventAndInstanceID(3, CurrentInstanceID));
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            //update internal time
            simulationInternalTime += timestep;
            simulationInternalFrame++;
        }

        private void AddColliderSDFs()
        {
            var id = 0;
            foreach (var sdfCollider in sdfColliders.Where(sdfCollider => sdfCollider != null && sdfCollider.enabled))
            {
                //compute the SDF and unite the sdfs with the given buffer
                //first argument is the positions and the second is the buffer to write to
                sdfCollider.ComputeSDF_Unite(CurrentInstanceID, solverCommandBuffer, gridNodePositions, GridSDF, GridID, id, numNodes);
                id++;
            }
        }

        private static float INT2Float(int a)
        {
            const float f2IScale = 5000.0f;
            const float maxINT = 2147483647.0f;

            return a / maxINT * f2IScale;
        }

        private void SetFluidParameters()
        {
            fluidParametersParams.affine_amount = 4.0f * (1.0f - parameters.Viscosity);
            fluidParametersParams.boundary_force = parameters.BoundaryForce;
            fluidParametersParams.boundary_friction = 0.1f;
            fluidParametersParams.ContainerPos = containerPos;
            fluidParametersParams.ContainerScale = containerSize;
            fluidParametersParams.deformation_decay = 0.0f;
            fluidParametersParams.density_compensation = 0.0f;
            fluidParametersParams.Direction = Vector3.zero;
            fluidParametersParams.dt = timestep;
            fluidParametersParams.dynamic_viscosity = 0.0f;
            fluidParametersParams.elastic_lambda = 0.0f;
            fluidParametersParams.elastic_mu = 0.0f;
            fluidParametersParams.eos_power = parameters.FluidStiffnessPower;
            fluidParametersParams.eos_stiffness = parameters.FluidStiffness;
            fluidParametersParams.gravity = parameters.Gravity/100.0f;
            fluidParametersParams.GridSize = GridSize;
            fluidParametersParams.group = 0;
            fluidParametersParams.node_delta = Vector3.zero;
            fluidParametersParams.NumNodes = numNodes;
            fluidParametersParams.NumParticles = NumParticles;
            fluidParametersParams.rest_density = parameters.ParticlesPerCell;
            fluidParametersParams.simulation_frame = simulationInternalFrame;
            fluidParametersParams.simulation_time = simulationInternalTime;
            fluidParametersParams.velocity_clamp = parameters.VelocityLimit;
            fluidParametersParams.SoftBody = 0;
        }

        /// <summary>
        /// Disable fluid render for a given camera
        /// </summary>
        public void DisableFoCamera(Camera cam)
        {
            CameraEvent cameraEvent = cam.actualRenderingPath == RenderingPath.Forward
                ? CameraEvent.AfterSkybox
                : CameraEvent.AfterLighting;
            cam.RemoveCommandBuffer(cameraEvent, renderCommandBuffer);
            cameraCBs.Remove(cam);
        }

        //dispose the objects
        protected void OnDisable()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (!initialized)
            {
                return;
            }

            if (Activated)
            {
                ZibraLiquidBridge.ReleaseResources(CurrentInstanceID);
                Activated = false;
            }

            PositionMass?.Release();
            PositionRadius?.Release();
            Affine[0]?.Release();
            Affine[1]?.Release();
            drawableGrid?.Release();
            GridData?.Release();
            IndexGrid?.Release();
            ParticleDensity?.Release();
            nodeParticlePairs?.Release();
            positionMassCopy?.Release();
            GridNormal?.Release();
            GridBlur0?.Release();
            gridBlur1?.Release();
            GridSDF?.Release();
            GridID?.Release();
            gridNodePositions?.Release();

            cameraCBs.Clear();

            foreach (var cam in cameraCBs)
            {
                if (cam.Key != null)
                {
                    CameraEvent cameraEvent = cam.Key.actualRenderingPath == RenderingPath.Forward
                    ? CameraEvent.AfterSkybox
                    : CameraEvent.AfterLighting;
                    if (cam.Key)
                    {
                        cam.Key.RemoveCommandBuffer(cameraEvent, cam.Value);
                    }
                }
            }

            //free allocated memory
            foreach (var data in camNativeParams)
            {
                Marshal.FreeHGlobal(data.Value);
            }

            solverCommandBuffer = null;

            initialized = false;

            if (AllFluids?.Contains(this) ?? false)
            {
                AllFluids.Remove(this);
            }
        }
    }
}