using System.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

/** 
 * Helper class to pass all necessary values to the actual two shaders doing the cloud computation:
 * - VoronoiCompute: calculates 4 different noises (1 simplex, 3 voronoi) and compresses them into a single int
 * - CloudShader: Takes the noise data and screen space position to render clouds as a post-processing effect
 */
public class CloudRenderer : GameService
{
    public Material Material;
    public RenderTexture TargetTexture;
    public Texture2D DebugTexture;

    public ComputeShader VoronoiCompute;
    [Range(0.25f, 5)]
    public float Zoom = 4;
    [Range(0, 31)]
    public int Slice = 0;
    [Range(1, 4)]
    public int Iterations = 2;
    [Range(0, 5)]
    public float Factor = 1.5f;
    [Range(1, 100)]
    public int CellCount = 25;

    public int ResolutionXZ = 128;
    public int ResolutionY = 8;

    private Camera MainCam;
    private ComputeBuffer MalaiseBuffer;
    private MapGenerator MapGen;

    //private ComputeBuffer CloudNoiseBuffer;
    private int Kernel, DebugKernel;

    public void OnDestroy()
    {
        MalaiseBuffer?.Release();
        //CloudNoiseBuffer?.Dispose();
    }

    public void Update()
    {
        if (!IsInit)
            return;

        Vector3 BL = MainCam.ScreenToWorldPoint(new Vector3(0, 0, 0));
        Vector3 TR = MainCam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        Vector3 BR = MainCam.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0));

        Vector3 Right = (BR - BL) / 2;
        Vector3 Up = (TR - BR) / 2;

        Material.SetVector("_CameraPos", MainCam.transform.position);
        Material.SetVector("_CameraForward", MainCam.transform.forward);
        Material.SetVector("_CameraRight", Right);
        Material.SetVector("_CameraUp", Up);
        Material.SetInt("_bIsEnabled", 1);
        PassMaterialBuffer();
    }

    public void PassMaterialBuffer()
    {
        uint[] MalaiseDTOs = MapGen.GetMalaiseDTOs();
        MalaiseBuffer.SetData(MalaiseDTOs);
    }

    private void Initialize()
    {
        InitializeComputeShader();
        InitializeVertexShader();
        _OnInit?.Invoke();
    }

    private void InitializeComputeShader()
    {
        //CloudNoiseBuffer = new(ResolutionXZ * ResolutionXZ * ResolutionY, sizeof(int));

        Kernel = VoronoiCompute.FindKernel("Main");
        DebugKernel = VoronoiCompute.FindKernel("Debug");
        VoronoiCompute.SetTexture(Kernel, "Result", TargetTexture);
        VoronoiCompute.SetTexture(DebugKernel, "Result", TargetTexture);
        VoronoiCompute.SetTexture(DebugKernel, "DebugTexture", DebugTexture);

        VoronoiCompute.SetFloat("Zoom", Zoom);
        VoronoiCompute.SetFloat("CellCount", CellCount);
        VoronoiCompute.SetFloat("Iterations", Iterations);
        VoronoiCompute.SetFloat("Factor", Factor);
        VoronoiCompute.SetFloat("_ResolutionXZ", ResolutionXZ);
        VoronoiCompute.SetFloat("_ResolutionY", ResolutionY);

        VoronoiCompute.Dispatch(Kernel, ResolutionXZ / 16, ResolutionXZ / 16, ResolutionY);
    }

    private void InitializeVertexShader()
    {
        int MalaiseIntCount = MapGen.GetMalaiseDTOByteCount();
        MalaiseBuffer = new ComputeBuffer(MalaiseIntCount, sizeof(int));

        Location MaxLocation = HexagonConfig.GetMaxLocation();
        Vector2Int MaxTileLocation = MaxLocation.GlobalTileLocation;
        Material.SetVector("_WorldSize", new Vector4(MaxTileLocation.x, MaxTileLocation.y, 0, 0));
        Material.SetVector("_TileSize", HexagonConfig.TileSize);
        Material.SetFloat("_ChunkSize", HexagonConfig.chunkSize);
        Material.SetFloat("_NumberOfChunks", HexagonConfig.mapMaxChunk);
        Material.SetBuffer("MalaiseBuffer", MalaiseBuffer);
        Material.SetTexture("_NoiseTex", TargetTexture);
        Material.SetFloat("_ResolutionXZ", ResolutionXZ);
        Material.SetFloat("_ResolutionY", ResolutionY);
        PassMaterialBuffer();
    }

    protected override void StartServiceInternal()
    {
        MainCam = Camera.main;

        Game.RunAfterServiceInit((MapGenerator MapGenerator) =>
        {
            MapGen = MapGenerator;
            Initialize();
        });
    }

    protected override void StopServiceInternal() {}
}
