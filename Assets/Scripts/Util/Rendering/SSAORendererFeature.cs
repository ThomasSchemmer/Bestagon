using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSAORendererFeature : ScriptableRendererFeature
{
    public Material SSAOMat;

    private SSAORendererPass Pass;
    private SceneNormalsPass NPass;

    public override void AddRenderPasses(ScriptableRenderer Renderer, ref RenderingData renderingData)
    {
        Renderer.EnqueuePass(NPass);
        Renderer.EnqueuePass(Pass);
    }

    public override void Create()
    {
        Pass = new(SSAOMat, "SSAORendererPass");
        NPass = new SceneNormalsPass();
    }

    private class SceneNormalsPass : ScriptableRenderPass
    {
        private ProfilingSampler ProfilingSampler;
        public void Setup()
        {
            ConfigureInput(ScriptableRenderPassInput.Normal);
            ProfilingSampler = new ProfilingSampler("SceneNormalsPass");
            return;
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

    }

    private class SSAORendererPass : ScriptableRenderPass
    {
        private ProfilingSampler ProfilingSampler;
        private Material SSAOMat;
        private List<ShaderTagId> ShaderTagsList = new List<ShaderTagId>();
        private FilteringSettings FilteringSettings;
        private int KernelSize = 10;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.SceneView)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (SSAOMat == null)
                    return;

                CoreUtils.DrawFullScreen(cmd, SSAOMat);

            }   
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public SSAORendererPass(Material SSAOMat, string name)
        {
            this.SSAOMat = SSAOMat;
            ProfilingSampler = new ProfilingSampler(name);
            FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            ShaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
            ShaderTagsList.Add(new ShaderTagId("UniversalForward"));
            ShaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            SSAOMat.SetFloat("_KernelSize", KernelSize);
            SSAOMat.SetVectorArray("_Kernel", GetHemisphere());
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;

            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }


        private Vector4[] GetHemisphere()
        {
            Vector4[] Hemisphere = new Vector4[KernelSize];
            for (int i = 0; i < KernelSize; i++)
            {
                Vector3 Vec = new Vector3()
                {
                    x = Random.Range(-1.0f, 1.0f),
                    y = Random.Range(-1.0f, 1.0f),
                    z = Random.Range(0f, 1.0f),
                };
                Vec.Normalize(); 
                float Scale = i / (float)KernelSize;
                Scale = Mathf.Lerp(0.1f, 1.0f, Scale * Scale);
                Vec *= Scale;
                Hemisphere[i] = Vec;
            }

            return Hemisphere;
        }

    }
}
