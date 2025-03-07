using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/**
 * Renderpass creating a 512x512 RenderTexture containing the outlines of 
 * all units. This texture can then be used in the actual @OutlinePass
 * Uses a compute shader to flipflop two buffers and converts the result into 
 * the RenderTexture
 */
public class StencilPass : ScriptableRenderPass
{
    private ProfilingSampler ProfilingSampler;
    private List<ShaderTagId> ShaderTagsList = new List<ShaderTagId>();
    private FilteringSettings FilteringSettings;

    private Material StencilMat;
    private RenderTexture SourceRT;
    private ComputeShader JumpFloodCompute;
    private int MainKernel, InitKernel;
    public GraphicsFence AsyncFence;

    private ComputeBuffer BufferA, BufferB;
    private RenderTexture TargetRT;
    private RTHandle CameraTargetHandle;

    public override void Execute(ScriptableRenderContext Context, ref RenderingData RenderingData)
    {
        if (!ShouldRender(ref RenderingData))
            return;

        CommandBuffer Cmd = GetRegularCmdBuffer(Context, ref RenderingData);
        if (Cmd != null)
        {
            Context.ExecuteCommandBuffer(Cmd);
            Cmd.Clear();
            CommandBufferPool.Release(Cmd);
        }

        CommandBuffer AsyncCmd = GetAsyncCmdBuffer(Context, ref RenderingData);
        if (AsyncCmd != null)
        {
            Context.ExecuteCommandBufferAsync(AsyncCmd, ComputeQueueType.Default);
            AsyncCmd.Clear();
            CommandBufferPool.Release(AsyncCmd);
        }
    }

    private CommandBuffer GetAsyncCmdBuffer(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (JumpFloodCompute == null)
            return null;

        int X = OutlineRenderFeature.Width / 32;
        int Y = OutlineRenderFeature.Width / 32;

        if (X <= 0 || Y <= 0)
            return null;

        CommandBuffer AsyncCmd = CommandBufferPool.Get();
        int StepSize = OutlineRenderFeature.Width / 2;
        using (new ProfilingScope(AsyncCmd, ProfilingSampler))
        {
            context.ExecuteCommandBuffer(AsyncCmd);
            AsyncCmd.Clear();
            AsyncCmd.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

            AsyncCmd.SetComputeBufferParam(JumpFloodCompute, InitKernel, "OriginBuffer", BufferA);
            AsyncCmd.SetComputeTextureParam(JumpFloodCompute, InitKernel, "Source", SourceRT);
            AsyncCmd.DispatchCompute(JumpFloodCompute, InitKernel, X, Y, 1);

            for (int i = 0; i < Mathf.Log(OutlineRenderFeature.Width, 2); i++)
            {
                AddStep(ref StepSize, i, AsyncCmd);
            }
            AsyncFence = AsyncCmd.CreateAsyncGraphicsFence();
        }
        return AsyncCmd;
    }

    private void AddStep(ref int StepSize, int Count, CommandBuffer AsyncCmd)
    {
        int X = OutlineRenderFeature.Width / 32;
        int Y = OutlineRenderFeature.Width / 32;

        bool bIsEven = (Count % 2) == 0;
        AsyncCmd.SetComputeIntParam(JumpFloodCompute, "StepSize", StepSize);
        AsyncCmd.SetComputeBufferParam(JumpFloodCompute, MainKernel, "OriginBuffer", bIsEven ? BufferA : BufferB);
        AsyncCmd.SetComputeBufferParam(JumpFloodCompute, MainKernel, "TargetBuffer", !bIsEven ? BufferA : BufferB);
        AsyncCmd.SetComputeTextureParam(JumpFloodCompute, MainKernel, "Target", TargetRT);
        AsyncCmd.DispatchCompute(JumpFloodCompute, MainKernel, X, Y, 1);
        StepSize /= 2;
    }

    private CommandBuffer GetRegularCmdBuffer(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!ShouldRender(ref renderingData))
            return null;
        if (StencilMat == null)
            return null;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, ProfilingSampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear(); 
            
            FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, 1024);
            DrawingSettings drawingSettings = CreateDrawingSettings(ShaderTagsList, ref renderingData, SortingCriteria.None);
            drawingSettings.overrideMaterial = StencilMat;

            renderingData.cameraData.camera.TryGetCullingParameters(out var cullingParameters);
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            context.DrawRenderers(cullingResults, ref drawingSettings, ref FilteringSettings);
            cmd.Blit(CameraTargetHandle, SourceRT);
        }
        return cmd;
    }

    public StencilPass(OutlineRenderFeature Feature)
    {
        StencilMat = Feature.StencilMat;
        SourceRT = Feature.SourceRT;
        TargetRT = Feature.TargetRT;
        if (!TargetRT.IsCreated())
        {
            TargetRT.Create();
        }
        JumpFloodCompute = Feature.JumpFloodCompute;
        BufferA = Feature.BufferA;
        BufferB = Feature.BufferB;
        if (JumpFloodCompute != null)
        {
            MainKernel = JumpFloodCompute.FindKernel("Main");
            InitKernel = JumpFloodCompute.FindKernel("Init");
        }
        ProfilingSampler = new ProfilingSampler("StencilPass");
        ShaderTagsList.Add(new ShaderTagId("UniversalForward"));
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);
        if (!ShouldRender(ref renderingData))
            return;

        CameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
    }

    private bool ShouldRender(ref RenderingData RenderingData)
    {
        if (!RenderingData.cameraData.camera.gameObject.tag.Contains("OverlayCamera"))
            return false;

        if (RenderingData.cameraData.cameraType == CameraType.SceneView)
            return false;

        return true;
    }

}