using com.zibra.liquid.Editor.Utilities;
using com.zibra.liquid.Solver;
using com.zibra.liquid.SDFObjects;
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquid), true)]
    public class ZibraLiquidEditor : UnityEditor.Editor
    {
        [MenuItem("GameObject/Zibra/Zibra Liquid", false, 10)]
        private static void CreateZibraLiquid(MenuCommand menuCommand)
        {
            // Create a custom game object
            var go = new GameObject("ZibraLiquid");
            go.AddComponent<ZibraLiquid>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Zibra/Analytic SDF Collider", false, 10)]
        private static void CreateAnalyticSDFCollider(MenuCommand menuCommand)
        {
            // Create a custom game object
            var go = new GameObject("AnalyticSDFCollider");
            go.AddComponent<AnalyticSDFCollider>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        private enum EditMode
        {
            None,
            Container,
            Emitter
        }

        private static readonly Color containerColor = new Color(1f, 0.8f, 0.4f);
        private static readonly Color emitterColor = new Color(0.4f, 0.4f, 1f);

        private ZibraLiquid ZibraLiquid;

        private SerializedProperty SimulationPreset;
        private SerializedProperty TimeStepMax;
        private SerializedProperty SimTimePerSec;

        private SerializedProperty MaxParticleNumber;
        private SerializedProperty IterationsPerFrame;
        private SerializedProperty GridResolution;
        private SerializedProperty RunSimulation;
        private SerializedProperty ContainerReference;

        private SerializedProperty SolverParameters;
        private SerializedProperty MaterialParameters;

        private SerializedProperty FluidInitialVelocity;
        private SerializedProperty EmitterDensity;
        private SerializedProperty sdfColliders;
        private SerializedProperty allsdfColliders;
        private SerializedProperty reflectionProbe;

        private bool dropdownToggle;
        private EditMode editMode;
        private readonly BoxBoundsHandle boxBoundsHandleContainer = new BoxBoundsHandle();
        private readonly BoxBoundsHandle boxBoundsHandleEmitter = new BoxBoundsHandle();

        private GUIStyle containerText;
        private GUIStyle emitterText;

        protected void OnEnable()
        {
            ZibraLiquid = target as ZibraLiquid;

            SimulationPreset = serializedObject.FindProperty("simulationPreset");
            reflectionProbe = serializedObject.FindProperty("reflectionProbe");
            TimeStepMax = serializedObject.FindProperty("timeStepMax");
            SimTimePerSec = serializedObject.FindProperty("simTimePerSec");

            MaxParticleNumber = serializedObject.FindProperty("maxParticleNumber");
            IterationsPerFrame = serializedObject.FindProperty("iterationsPerFrame");
            GridResolution = serializedObject.FindProperty("gridResolution");

            RunSimulation = serializedObject.FindProperty("runSimulation");
            ContainerReference = serializedObject.FindProperty("containerReference");

            SolverParameters = serializedObject.FindProperty("solverParameters");
            MaterialParameters = serializedObject.FindProperty("materialParameters");

            FluidInitialVelocity = serializedObject.FindProperty("fluidInitialVelocity");
            EmitterDensity = serializedObject.FindProperty("emitterDensity");
            sdfColliders = serializedObject.FindProperty("sdfColliders");
            allsdfColliders = serializedObject.FindProperty("allsdfColliders");

            containerText = new GUIStyle
            {
                alignment = TextAnchor.MiddleLeft, 
                normal = {textColor = containerColor}
            };

            emitterText = new GUIStyle
            {
                alignment = TextAnchor.MiddleLeft, 
                normal = {textColor = emitterColor}
            };
        }

        protected void OnSceneGUI()
        {
            if (ZibraLiquid == null)
            {
                Debug.LogError("ZibraLiquidEditor not attached to ZibraLiquid component.");
                return;
            }

            var localToWorld = Matrix4x4.TRS(ZibraLiquid.transform.position, ZibraLiquid.transform.rotation, Vector3.one);

            ZibraLiquid.containerPos = ZibraLiquid.transform.position;
            ZibraLiquid.transform.rotation = Quaternion.identity;
            ZibraLiquid.transform.localScale = Vector3.one;

            using (new Handles.DrawingScope(containerColor, localToWorld))
            {
                if (editMode == EditMode.Container)
                {
                    ZibraLiquid.useContainerReference = false;

                    Handles.Label(Vector3.zero, "Container Area", containerText);

                    boxBoundsHandleContainer.center = Vector3.zero;
                    boxBoundsHandleContainer.size = ZibraLiquid.containerSize;

                    EditorGUI.BeginChangeCheck();
                    var newPos = Handles.PositionHandle(Vector3.zero, Quaternion.identity);
                    boxBoundsHandleContainer.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        // record the target object before setting new values so changes can be undone/redone
                        Undo.RecordObject(ZibraLiquid, "Change Container");

                        var startPos = ZibraLiquid.containerPos;
                        ZibraLiquid.containerPos = newPos;
                        ZibraLiquid.containerPos += boxBoundsHandleContainer.center - startPos;
                        ZibraLiquid.containerSize = boxBoundsHandleContainer.size;
                        EditorUtility.SetDirty(ZibraLiquid);
                    }
                }
                else
                {
                    Handles.DrawWireCube(Vector3.zero, ZibraLiquid.containerSize);
                }
            }

            using (new Handles.DrawingScope(emitterColor, localToWorld))
            {
                if (editMode == EditMode.Emitter)
                {
                    Handles.Label(ZibraLiquid.emitterPos, "Emitter Area", emitterText);

                    boxBoundsHandleEmitter.center = ZibraLiquid.emitterPos;
                    boxBoundsHandleEmitter.size = ZibraLiquid.emitterSize;

                    EditorGUI.BeginChangeCheck();
                    var newPos = Handles.PositionHandle(ZibraLiquid.emitterPos, Quaternion.identity);
                    boxBoundsHandleEmitter.DrawHandle();
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        // record the target object before setting new values so changes can be undone/redone
                        Undo.RecordObject(ZibraLiquid, "Change Emitter");

                        var startPos = ZibraLiquid.emitterPos;
                        ZibraLiquid.emitterPos = newPos;
                        ZibraLiquid.emitterPos += boxBoundsHandleEmitter.center - startPos;
                        ZibraLiquid.emitterSize = boxBoundsHandleEmitter.size;
                        EditorUtility.SetDirty(ZibraLiquid);
                    }
                }
                else
                {
                    Handles.DrawWireCube(ZibraLiquid.emitterPos, ZibraLiquid.emitterSize);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (ZibraLiquid == null)
            {
                Debug.LogError("ZibraLiquidEditor not attached to ZibraLiquid component.");
                return;
            }

            ZibraLiquid.transform.position = ZibraLiquid.containerPos;
            ZibraLiquid.transform.rotation = Quaternion.identity;
            ZibraLiquid.transform.localScale = Vector3.one;

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            if (GUILayout.Button(EditorGUIUtility.IconContent("EditCollider"), GUILayout.MaxWidth(40),
                GUILayout.Height(30)))
            {
                editMode = editMode == EditMode.Container ? EditMode.None : EditMode.Container;
                SceneView.RepaintAll();
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Edit Container Area", containerText, GUILayout.MaxWidth(100),
                GUILayout.Height(30));
            GUILayout.Space(40);

            EditorGUI.BeginChangeCheck();
            ZibraLiquid.useContainerReference = GUILayout.Toggle(ZibraLiquid.useContainerReference,
                "Use Reference Mesh Renderer", GUILayout.Height(30));

            if (EditorGUI.EndChangeCheck() && ZibraLiquid.useContainerReference)
            {
                editMode = EditMode.None;

                SceneView.RepaintAll();
            }

            if (ZibraLiquid.useContainerReference)
            {
                if (ZibraLiquid.ContainerReference != null)
                {
                    ZibraLiquid.containerPos = ZibraLiquid.ContainerReference.bounds.center;
                    ZibraLiquid.containerSize = ZibraLiquid.ContainerReference.bounds.size;

                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (ZibraLiquid.useContainerReference)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(ContainerReference);

                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                ZibraLiquid.containerPos =
                    EditorGUILayout.Vector3Field(new GUIContent("Container Center"), ZibraLiquid.containerPos);
                ZibraLiquid.containerSize =
                    EditorGUILayout.Vector3Field(new GUIContent("Container Size"), ZibraLiquid.containerSize);

                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            }


            GUILayout.Space(25);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            if (GUILayout.Button(EditorGUIUtility.IconContent("EditCollider"), GUILayout.MaxWidth(40),
                GUILayout.Height(30)))
            {
                editMode = editMode == EditMode.Emitter ? EditMode.None : EditMode.Emitter;
                SceneView.RepaintAll();
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Edit Emitter Area", emitterText, GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            ZibraLiquid.emitterPos =
                EditorGUILayout.Vector3Field(new GUIContent("Emitter Center"), ZibraLiquid.emitterPos);
            ZibraLiquid.emitterSize =
                EditorGUILayout.Vector3Field(new GUIContent("Emitter Size"), ZibraLiquid.emitterSize);

            //Get bounding boxes
            Vector3 containerMin = ZibraLiquid.containerPos - ZibraLiquid.containerSize * 0.5f;
            Vector3 containerMax = ZibraLiquid.containerPos + ZibraLiquid.containerSize * 0.5f;

            Vector3 emitterMin = ZibraLiquid.containerPos + ZibraLiquid.emitterPos - ZibraLiquid.emitterSize * 0.5f;
            Vector3 emitterMax = ZibraLiquid.containerPos + ZibraLiquid.emitterPos + ZibraLiquid.emitterSize * 0.5f;

            //Clamp emitter to container boundaries
            emitterMin = Vector3.Min(Vector3.Max(emitterMin, containerMin), containerMax);
            emitterMax = Vector3.Min(Vector3.Max(emitterMax, containerMin), containerMax);

            ZibraLiquid.emitterPos = (emitterMin + emitterMax) * 0.5f - ZibraLiquid.containerPos;
            ZibraLiquid.emitterSize = emitterMax - emitterMin;

            //Negative size not permitted
            ZibraLiquid.emitterSize = new Vector3(
                Math.Abs(ZibraLiquid.emitterSize.x),
                Math.Abs(ZibraLiquid.emitterSize.y),
                Math.Abs(ZibraLiquid.emitterSize.z)
            );

            EditorGUILayout.PropertyField(FluidInitialVelocity);
            EditorGUILayout.PropertyField(EmitterDensity);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            GUILayout.Space(25);

            EditorGUILayout.PropertyField(sdfColliders, true);

            if (ZibraLiquid.sdfColliders.Count > 5)
            {
                for (int i = 5; i < ZibraLiquid.sdfColliders.Count; i++)
                    ZibraLiquid.sdfColliders.RemoveAt(i);
            }

            GUIContent btnTxt = new GUIContent("Add Collider");
            var rtBtn = GUILayoutUtility.GetRect(btnTxt, GUI.skin.button, GUILayout.MaxWidth(400));
            rtBtn.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rtBtn.center.y);

            if (EditorGUI.DropdownButton(rtBtn, btnTxt, FocusType.Keyboard))
            {
                dropdownToggle = !dropdownToggle;
            }

            if (dropdownToggle && allsdfColliders.isArray)
            {
                EditorGUI.indentLevel++;

                var empty = true;

                for (var i = 0; i < allsdfColliders.arraySize; i++)
                {
                    var serializedProperty = allsdfColliders.GetArrayElementAtIndex(i);
                    var sdfCollider = serializedProperty.objectReferenceValue as SDFCollider;

                    if (sdfCollider != null && ZibraLiquid.sdfColliders.Contains(sdfCollider))
                    {
                        continue;
                    }

                    empty = false;

                    EditorGUILayout.BeginHorizontal();

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(serializedProperty, true);
                    EditorGUI.EndDisabledGroup();

                    if (ZibraLiquid.sdfColliders.Count < 5)
                    {
                        if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
                        {
                            ZibraLiquid.sdfColliders.Add(sdfCollider);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (empty)
                {

                    GUIContent labelText = new GUIContent("The list is empty.");
                    var rtLabel = GUILayoutUtility.GetRect(labelText, GUI.skin.label, GUILayout.ExpandWidth(false));
                    rtLabel.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rtLabel.center.y);

                    EditorGUI.LabelField(rtLabel, labelText);
                }

                EditorGUI.indentLevel--;
            }

            if (!ZibraLiquid.Activated)
            {
                GUILayout.Label("Use default simulation preset: ");
                EditorGUILayout.PropertyField(SimulationPreset);
                if (GUILayout.Button("Apply Simulation Preset"))
                {
                    if (ZibraLiquid.simulationPreset.IsValid())
                        PresetUtility.ApplyPresetExcludingProperties(ZibraLiquid.simulationPreset, ZibraLiquid,
                            nameof(ZibraLiquid.simulationPreset), nameof(ZibraLiquid.containerSize),
                            nameof(ZibraLiquid.containerPos), nameof(ZibraLiquid.emitterSize),
                            nameof(ZibraLiquid.emitterPos), nameof(ZibraLiquid.useContainerReference),
                            ZibraLiquid.ContainerReferenceName, nameof(ZibraLiquid.ContainerReference),
                            nameof(ZibraLiquid.fluidInitialVelocity), nameof(ZibraLiquid.sdfColliders),
                            ZibraLiquid.AllsdfCollidersName, nameof(ZibraLiquid.reflectionProbe),
                            nameof(ZibraLiquid.emitterDensity), nameof(ZibraLiquid.cellSize), nameof(ZibraLiquid.gridResolution),
                            nameof(ZibraLiquid.simTimePerSec), nameof(ZibraLiquid.RunSimulation),
                            nameof(ZibraLiquid.RunSimulation), nameof(ZibraLiquid.MaxNumParticles));
                    else Debug.LogError("Invalid Preset");
                }
            }

            GUILayout.Space(25);

            GUILayout.Label("Simulation statistics: ");
            GUILayout.Space(5);

            if (ZibraLiquid.Activated)
            {
                GUILayout.Label("Current time step: " + ZibraLiquid.timestep);
                GUILayout.Label("Internal time: " + ZibraLiquid.simulationInternalTime);
                GUILayout.Label("Simulation frame: " + ZibraLiquid.simulationInternalFrame);
                GUILayout.Label("Active particles: " + ZibraLiquid.NumParticles);
            }
            else
            {
                var emitterCells = ZibraLiquid.emitterSize / ZibraLiquid.cellSize;
                var num = (int) (ZibraLiquid.emitterDensity * ZibraLiquid.solverParameters.ParticlesPerCell * emitterCells.x *
                                 emitterCells.y * emitterCells.z);
                num = Math.Min(num, ZibraLiquid.maxParticleNumber);
                GUILayout.Label("Emitter particles: " + num);
            }

            var solverRes = Vector3Int.CeilToInt(ZibraLiquid.containerSize / ZibraLiquid.cellSize);
            GUILayout.Label("Solver grid resolution: " + solverRes);

            GUILayout.Space(25);

            EditorGUILayout.PropertyField(TimeStepMax);
            EditorGUILayout.PropertyField(SimTimePerSec);

            EditorGUILayout.PropertyField(MaxParticleNumber);
            EditorGUILayout.PropertyField(IterationsPerFrame);

            if (!ZibraLiquid.Activated) EditorGUILayout.PropertyField(GridResolution);
            ZibraLiquid.cellSize = Math.Max(ZibraLiquid.containerSize.x, Math.Max(ZibraLiquid.containerSize.y, ZibraLiquid.containerSize.z))/ZibraLiquid.gridResolution;
            EditorGUILayout.PropertyField(RunSimulation);

            EditorGUILayout.PropertyField(SolverParameters);
            EditorGUILayout.PropertyField(MaterialParameters);
            EditorGUILayout.PropertyField(reflectionProbe);

            serializedObject.ApplyModifiedProperties();
        }
    }
}