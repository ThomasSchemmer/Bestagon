using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlinePass : ScriptableRenderPass
{
    private ProfilingSampler ProfilingSampler;
    private List<ShaderTagId> ShaderTagsList = new List<ShaderTagId>();
    private FilteringSettings Step1Settings, Step2Settings;
    private Material StencilMat;
    private RenderTexture JumpFloodRT;

    public override void Execute(ScriptableRenderContext Context, ref RenderingData RenderingData)
    {
        if (!RenderingData.cameraData.camera.gameObject.tag.Contains("MainCamera"))
            return;

        if (RenderingData.cameraData.cameraType == CameraType.SceneView)
            return;

        CommandBuffer HighlightCmd = GetHighlightCmdBuffer(Context, ref RenderingData);
        if (HighlightCmd != null)
        {
            Context.ExecuteCommandBuffer(HighlightCmd);
            HighlightCmd.Clear();
            CommandBufferPool.Release(HighlightCmd);
        }
    }

    private CommandBuffer GetHighlightCmdBuffer(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (StencilMat == null)
            return null;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, ProfilingSampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // one for each pass
            DrawingSettings drawing1Settings = CreateDrawingSettings(ShaderTagsList, ref renderingData, SortingCriteria.None);
            drawing1Settings.overrideMaterial = StencilMat;
            drawing1Settings.overrideMaterialPassIndex = 1;
            DrawingSettings drawing2Settings = CreateDrawingSettings(ShaderTagsList, ref renderingData, SortingCriteria.None);
            drawing2Settings.overrideMaterial = StencilMat;
            drawing2Settings.overrideMaterialPassIndex = 2;

            renderingData.cameraData.camera.TryGetCullingParameters(out var cullingParameters);
            CullingResults cullingResults = context.Cull(ref cullingParameters);

            context.DrawRenderers(cullingResults, ref drawing1Settings, ref Step1Settings);
            context.DrawRenderers(cullingResults, ref drawing2Settings, ref Step2Settings);
        }
        return cmd;
    }

    public OutlinePass(OutlineRenderFeature Feature)
    {
        StencilMat = Feature.StencilMat;
        JumpFloodRT = Feature.SourceRT;

        ProfilingSampler = new ProfilingSampler("OutlinePass");
        ShaderTagsList.Add(new ShaderTagId("UniversalForward"));
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        // 1024 = "unit" layer
        Step1Settings = new FilteringSettings(RenderQueueRange.opaque, 1024);
        Step2Settings = new FilteringSettings(RenderQueueRange.opaque, 1024);
    }


}
