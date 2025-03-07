using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/**
 * Reimplementation of Unity's FSRP, as the overlay camera doesn't work with it
 * Stripped out some unnecessary code as well
 */
public class FullScreenRenderFeature : ScriptableRendererFeature
{
    public Material Material;
    private FullScreenRenderPass Pass;

    public override void AddRenderPasses(ScriptableRenderer Renderer, ref RenderingData renderingData)
    {
        if (!Application.isPlaying)
            return;

        if (renderingData.cameraData.isPreviewCamera)
            return;

        if (!renderingData.cameraData.camera.gameObject.tag.Contains("MainCamera"))
            return;
    
        Renderer.EnqueuePass(Pass);
    }

    public override void Create()
    {
        Pass = new FullScreenRenderPass(Material);
    }

    private class FullScreenRenderPass : ScriptableRenderPass
    {
        private Material Material;
        private static MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

        public FullScreenRenderPass(Material Material)
        {
            this.Material = Material; 
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void Execute(ScriptableRenderContext Context, ref RenderingData renderingData)
        {
            PropertyBlock.Clear();
            CommandBuffer Cmd = CommandBufferPool.Get();
            Context.ExecuteCommandBuffer(Cmd);
            Cmd.Clear();
            Cmd.DrawProcedural(Matrix4x4.identity, Material, 0, MeshTopology.Triangles, 3, 1, PropertyBlock);

            Context.ExecuteCommandBuffer(Cmd);
            Cmd.Clear();
            CommandBufferPool.Release(Cmd);
        }
    }
}
