using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

/** for some reason the scriptable renderpipeline doesnt want to execute the copy depth call,
* so just force it manually.
* https://forum.unity.com/threads/how-to-use-copydepthpass-in-custom-render-feature-to-update-depth-texture.1459861/
*/
public class CopyDepthRender : ScriptableRendererFeature
{
    CopyDepthPass _copyDepthPass;
    RTHandle _depthDestHandle;
    RTHandle _depthSourceHandle;
    readonly static FieldInfo depthTextureFieldInfo = typeof(UniversalRenderer).GetField("m_DepthTexture", BindingFlags.NonPublic | BindingFlags.Instance);

    public override void Create()
    {
        _copyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingTransparents, CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/CopyDepth"));
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_copyDepthPass == null)
            return;

        renderer.EnqueuePass(_copyDepthPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (depthTextureFieldInfo == null)
            return;

        _depthDestHandle = depthTextureFieldInfo.GetValue(renderer) as RTHandle;
        _depthSourceHandle = renderer.cameraDepthTargetHandle;
        if (_depthDestHandle == null || _depthSourceHandle.rt == null)
            return;

        _copyDepthPass.Setup(_depthSourceHandle, _depthDestHandle);
    }
}