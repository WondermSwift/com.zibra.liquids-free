#if UNITY_PIPELINE_URP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using com.zibra.liquid.Solver;

namespace com.zibra.liquid
{
    public class FluidURPRenderComponent : ScriptableRendererFeature
    {
        public class FluidURPRenderPass :ScriptableRenderPass
        {
            private int depth;

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                depth = Shader.PropertyToID("_CameraDepthTexture");
                cmd.GetTemporaryRT(depth, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.RFloat);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                CommandBuffer cmd = CommandBufferPool.Get("ZibraLiquid.Render");

                foreach (var liquid in ZibraLiquid.AllFluids)
                {
                    if (liquid != null && liquid.initialized)
                    {
                        liquid.SetMaterialParams(camera);
                        liquid.UpdateNativeTextures(camera);
                        liquid.UpdateNativeRenderParams(camera);

                        camera.depthTextureMode = DepthTextureMode.Depth;
                        var cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;
                        var cameraDepthTexture = renderingData.cameraData.renderer.cameraDepthTarget;
                        liquid.UpdateCamera(camera);
                        //set initial parameters in the native plugin
                        ZibraLiquidBridge.SetCameraParameters(liquid.CurrentInstanceID, liquid.camNativeParams[camera]);
                        cmd.IssuePluginEventAndData(ZibraLiquidBridge.GetCameraUpdateFunction(), ZibraLiquidBridge.EventAndInstanceID(0, liquid.CurrentInstanceID), liquid.camNativeParams[camera]);
                        cmd.Blit(cameraColorTexture, liquid.background);
                        //blit depth to temp RT
                        cmd.Blit(cameraDepthTexture, depth);
                        liquid.RenderParticelsNative(cmd);
                        cmd.SetRenderTarget(cameraColorTexture);
                        //bind temp depth RT
                        cmd.SetGlobalTexture("_CameraDepthTexture", depth);
                        liquid.RenderFluid(cmd);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(depth);
            }
        }

        public FluidURPRenderPass fluidPass;

        public override void Create()
        {
            fluidPass = new FluidURPRenderPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(fluidPass);
        }
    }
}

#endif //UNITY_PIPELINE_HDRP