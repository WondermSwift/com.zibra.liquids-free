using System;
using System.Runtime.InteropServices;

namespace com.zibra.liquid.Solver
{
    public static class ZibraLiquidBridge
    {
#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	    [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern Int32 CreateFluidInstance();

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	    [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void RegisterParticlesBuffers(Int32 InstanceID, IntPtr ParticlesInitValues, IntPtr PositionMass,
            IntPtr Affine0, IntPtr Affine1, IntPtr DrawableGrid,
            IntPtr PositionRadius);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	    [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void RegisterRenderResources(Int32 InstanceID, IntPtr Color0, IntPtr Color1);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern IntPtr SetCameraParameters(Int32 InstanceID, IntPtr CameraRenderParams);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern IntPtr SetRenderParameters(Int32 InstanceID, IntPtr RenderParams);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	    [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void SetCollidersCount(Int32 InstanceID, int count);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern IntPtr GetRenderEventFunc();

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void RegisterCollidersBuffers(Int32 InstanceID, IntPtr ForceTorque, IntPtr ObjPositions);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void RegisterSolverBuffers(Int32 InstanceID, IntPtr FluidParameters, IntPtr PositionMassCopy,
            IntPtr ParticleDensity, IntPtr GridData, IntPtr IndexGrid, IntPtr GridBlur0, IntPtr GridBlur1,
            IntPtr GridNormal, IntPtr GridSDF, IntPtr GridNodePositions, IntPtr NodeParticlePairs, IntPtr GridID);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void SetFluidParameters(Int32 InstanceID, IntPtr FluidParameters);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	    [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void RegisterVoxelCollider(Int32 InstanceID, IntPtr VoxelIDGrid1, IntPtr VoxelIDGrid2, IntPtr VoxelPositions,
            IntPtr VoxelEmbeddings, int VoxelNum, int colliderNumber);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	    [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void RegisterAnalyticCollider(Int32 InstanceID, int ColliderIndex);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	    [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void UpdateForceInteractionBuffers(Int32 InstanceID, IntPtr ObjPos, int ColliderNum);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	    [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern void ReleaseResources(int InstanceID);

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern IntPtr RunSDFShaderWithDataPtr();

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern IntPtr GetCameraUpdateFunction();

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern bool IsPaidVersion();

#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	        [DllImport ("__Internal")]
#else
#if UNITY_64 || UNITY_EDITOR_64 
        [DllImport("ZibraFluidNative_x64")]
#else
        [DllImport("ZibraFluidNative_x86")]
#endif
#endif
        public static extern IntPtr GetVersion();

        public static readonly string version = Marshal.PtrToStringAnsi(GetVersion());

        public static int EventAndInstanceID(int eventID, int InstanceID)
        {
            return eventID | (InstanceID << 8);
        }
    }
}