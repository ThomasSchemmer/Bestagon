using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/**
 * Custom render feature to generate a good looking outline for hidden meshes
 * Heavily inspired by BGolus blogpost https://bgolus.medium.com/the-quest-for-very-wide-outlines-ba82ed442cd9
 * 
 * Uses the overlay camera to render meshes on specific layers to a rendertexture with a white base color
 * The meshes themselfs will be black
 * Calculates distance inside the meshes using jump-flooding
 */
public class OutlineRenderFeature : ScriptableRendererFeature
{
    public RenderTexture SourceRT, TargetRT;
    public Material StencilMat;
    public ComputeShader JumpFloodCompute;
    public ComputeBuffer BufferA, BufferB;

    private StencilPass StencilPass;
    private OutlinePass OutlinePass;

    public override void AddRenderPasses(ScriptableRenderer Renderer, ref RenderingData renderingData)
    {
        if (!Application.isPlaying)
            return;

        if (renderingData.cameraData.camera.gameObject.tag.Contains("OverlayCamera"))
        {
            Renderer.EnqueuePass(StencilPass);
        }
        if (renderingData.cameraData.camera.gameObject.tag.Contains("MainCamera"))
        {
            Renderer.EnqueuePass(OutlinePass);
        }
    }
    protected override void Dispose(bool Disposing)
    {
        CleanUp();
    }

    private void CleanUp()
    {
        if (BufferA != null)
        {
            BufferA.Release();
        }
        if (BufferB != null)
        {
            BufferB.Release();
        }
    }

    public override void Create()
    {
        CleanUp();
        BufferA = new(Width * Width, sizeof(uint) + sizeof(float));
        BufferB = new(Width * Width, sizeof(uint) + sizeof(float));


        StencilPass = new(this);
        OutlinePass = new(this);
    }

    public static int Width = 512;

}
