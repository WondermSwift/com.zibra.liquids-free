#if UNITY_PIPELINE_HDRP

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using com.zibra.liquid.Solver;

namespace com.zibra.liquid
{
    public class FluidHDRPRenderComponent : CustomPassVolume
    {
        public class FluidHDRPRender : CustomPass
        {
            public ZibraLiquid liquid;
            RTHandle Depth;

            protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
            {
                Depth = RTHandles.Alloc(
                    Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R32_SFloat,
                    // We don't need alpha for this effect
                    useDynamicScale: true, name: "Depth buffer"
                );
            }

            protected override void Execute(CustomPassContext ctx)
            {
                if (liquid.initialized && liquid.simulationInternalFrame > 1)
                {
                    liquid.SetMaterialParams(ctx.hdCamera.camera);
                    liquid.UpdateNativeTextures(ctx.hdCamera.camera);
                    liquid.UpdateNativeRenderParams(ctx.hdCamera.camera);

                    var depth = Shader.PropertyToID("_CameraDepthTexture");
                    ctx.cmd.GetTemporaryRT(depth, ctx.hdCamera.camera.pixelWidth, ctx.hdCamera.camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.RFloat);

                    //copy screen to background
                    var scale = RTHandles.rtHandleProperties.rtHandleScale;
                    ctx.cmd.Blit(ctx.cameraColorBuffer, liquid.background, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);
                    //blit depth to temp RT
                    HDUtils.BlitCameraTexture(ctx.cmd, ctx.cameraDepthBuffer, Depth);
                    ctx.cmd.Blit(Depth, depth, new Vector2(scale.x, scale.y), Vector2.zero, 1, 0);

                    liquid.RenderParticelsNative(ctx.cmd);
                    CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.None);
                    //bind temp depth RT
                    ctx.cmd.SetGlobalTexture("_CameraDepthTexture", depth);
                    liquid.RenderFluid(ctx.cmd);
                    ctx.cmd.ReleaseTemporaryRT(depth);
                }
            }

            protected override void Cleanup()
            {
                CoreUtils.Destroy(Depth);
            }
        }

        public FluidHDRPRender fluidPass;
    }
}

#endif //UNITY_PIPELINE_HDRP