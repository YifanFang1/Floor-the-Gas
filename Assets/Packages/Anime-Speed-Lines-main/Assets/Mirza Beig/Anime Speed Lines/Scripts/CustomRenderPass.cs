using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPass : ScriptableRenderPass
{
    RTHandle source;
    RTHandle destination;

    CustomRenderPassFeature.CustomRenderPassSettings settings;
    static readonly int _TempTargetId = Shader.PropertyToID("_CustomRenderPassTempTarget");

    public CustomRenderPass(CustomRenderPassFeature.CustomRenderPassSettings settings)
    {
        this.settings = settings; 
        renderPassEvent = settings.renderPassEvent;
        requiresIntermediateTexture = true;
    }

#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable CS0618 // Type or member is obsolete
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateHandleIfNeeded(ref destination, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CustomRenderPassTempTarget");
        source = renderingData.cameraData.renderer.cameraColorTargetHandle;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (settings.material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();

        Blitter.BlitCameraTexture(cmd, source, destination, settings.material, 0);
        Blitter.BlitCameraTexture(cmd, destination, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
#pragma warning restore CS0618
#pragma warning restore CS0672

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // RTHandle cleanup is handled by ReAllocateHandleIfNeeded
    }

    public void Dispose()
    {
        destination?.Release();
    }
}


