using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Helper class to pass all necessary values to the actual two shaders doing the cloud computation:
 * - WhorleyCompute: calculates 4 different noises (1 simplex, 3 WhorleyCompute) and compresses them into a single int
 * - CloudShader: Takes the noise data and screen space position to render clouds as a post-processing effect
 */
public class CloudRenderer : GameService
{
    public Material Material;
    public RenderTexture TargetTexture;
    public RenderTexture TempTexture;

    public ComputeShader WhorleyShader;
    [Range(1, 20)]
    public int NumPoints = 10;
    [Range(0.1f, 5)]
    public float Zoom = 1;
    [Range(0.01f, 0.2f)]
    public float SimplexZoom = 1;
    [Range(1, 4)]
    public int Iterations = 2;
    [Range(1, 5)]
    public int Face = 1;

    private Camera MainCam;
    private ComputeBuffer MalaiseBuffer;

    private ComputeBuffer PointBuffer;
    private ComputeBuffer DirectionsBuffer;
    private ComputeBuffer MinMaxBuffer;

    private MapGenerator MapGen;

    public void OnDestroy()
    {
        ClearBuffers(true);
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

    public void Initialize()
    {
        CreateWhorleyNoise();
        InitializeVertexShader();
        _OnInit?.Invoke();
    }

    private void InitializeVertexShader()
    {
        ClearBuffers(true);
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
        Material.SetFloat("_ResolutionXZ", TargetTexture.width);
        Material.SetFloat("_ResolutionY", TargetTexture.volumeDepth);
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

    public void CreateWhorleyNoise()
    {
        ClearBuffers();
        FillPointBuffer();

        int createNoiseKernel = WhorleyShader.FindKernel("CreateNoiseTexture");
        int normalizeKernel = WhorleyShader.FindKernel("Normalize");
        Vector3Int ImageSize = new(TargetTexture.width, TargetTexture.height, TargetTexture.volumeDepth);
        Vector3Int AmountGroups = new(ImageSize.x / THREAD_AMOUNT, ImageSize.y / THREAD_AMOUNT, ImageSize.z / THREAD_AMOUNT_Z);

        FillWhorleyBuffers(createNoiseKernel, normalizeKernel, ImageSize, AmountGroups);

        WhorleyShader.Dispatch(createNoiseKernel, AmountGroups.x, AmountGroups.y, AmountGroups.z);

        int[] data = new int[MinMaxBuffer.count];
        MinMaxBuffer.GetData(data);

        WhorleyShader.Dispatch(normalizeKernel, AmountGroups.x, AmountGroups.y, AmountGroups.z);
    }

    public void Tile()
    {
        Texture2D tex = new Texture2D(TargetTexture.width, TargetTexture.height, TextureFormat.RGB24, false);
        var OldRT = RenderTexture.active;
        RenderTexture.active = TargetTexture;

        tex.ReadPixels(new Rect(0, 0, TargetTexture.width, TargetTexture.height), 0, 0);
        tex.Apply();

        RenderTexture.active = OldRT;

        Graphics.Blit(tex, TempTexture, new Vector2(4, 4), new Vector2(0,0));
        DestroyImmediate(tex);
    }

    private void FillPointBuffer()
    {
        Random.InitState(5);
        List<Vector3> Points = new List<Vector3>(NumPoints * NumPoints * NumPoints);   
        float scale = 1.0f / NumPoints;
        for (int z = 0; z < NumPoints; z++)
        {
            for (int y = 0; y < NumPoints; y++)
            {
                for (int x = 0; x < NumPoints; x++)
                {
                    {
                        Vector3 p = new Vector3(
                            (x * scale + Random.Range(0, scale)) * TargetTexture.width,
                            (y * scale + Random.Range(0, scale)) * TargetTexture.height,
                            (z * scale + Random.Range(0, scale)) * TargetTexture.volumeDepth
                        );
                        Points.Add(p);
                    }
                }
            }
        }

        PointBuffer = new ComputeBuffer(Points.Count, sizeof(float) * 3);
        PointBuffer.SetData(Points);
    }

    private void FillWhorleyBuffers(int createNoiseKernel, int normalizeKernel, Vector3Int ImageSize, Vector3Int AmountGroups)
    {

        DirectionsBuffer = new ComputeBuffer(DIRECTIONS.Length, sizeof(float) * 3);
        DirectionsBuffer.SetData(DIRECTIONS);

        int MaxValue = 999999;
        int MinValue = -MaxValue;
        MinMaxBuffer = new ComputeBuffer(8, sizeof(int));
        MinMaxBuffer.SetData(new int[8] { MaxValue, MinValue, MaxValue, MinValue, MaxValue, MinValue, MaxValue, MinValue });

        WhorleyShader.SetBuffer(createNoiseKernel, "points", PointBuffer);
        WhorleyShader.SetBuffer(createNoiseKernel, "directions", DirectionsBuffer);
        WhorleyShader.SetBuffer(createNoiseKernel, "minMax", MinMaxBuffer);

        WhorleyShader.SetInt("directionsCount", DIRECTIONS.Length);
        WhorleyShader.SetFloat("pointCount", NumPoints);
        WhorleyShader.SetVector("size", new Vector4(ImageSize.x, ImageSize.y, ImageSize.z, 0));
        WhorleyShader.SetVector("amountGroups", new(AmountGroups.x, AmountGroups.y, AmountGroups.z, 0));

        WhorleyShader.SetFloat("zoom", Zoom);
        WhorleyShader.SetInt("iterations", Iterations);
        WhorleyShader.SetInt("face", Face);
        WhorleyShader.SetFloat("simplexZoom", SimplexZoom);

        WhorleyShader.SetTexture(createNoiseKernel, "result", TargetTexture);

        WhorleyShader.SetBuffer(normalizeKernel, "minMax", MinMaxBuffer);
        WhorleyShader.SetTexture(normalizeKernel, "result", TargetTexture);
    }

    public void Debug()
    {
        ClearBuffers();
        FillPointBuffer();
        int debugKernel = WhorleyShader.FindKernel("Debug");

        WhorleyShader.SetFloat("pointCount", NumPoints);
        WhorleyShader.SetBuffer(debugKernel, "points", PointBuffer);
        WhorleyShader.SetTexture(debugKernel, "result", TargetTexture);

        WhorleyShader.Dispatch(debugKernel, 1, 1, 1);
    }

    public void Clear()
    {
        ClearBuffers();
        int clearKernel = WhorleyShader.FindKernel("Clear");
        Vector3Int ImageSize = new(TargetTexture.width, TargetTexture.height, TargetTexture.volumeDepth);
        Vector3Int AmountGroups = new(ImageSize.x / THREAD_AMOUNT, ImageSize.y / THREAD_AMOUNT, ImageSize.z / THREAD_AMOUNT_Z);

        WhorleyShader.SetVector("size", new(ImageSize.x, ImageSize.y, ImageSize.z, 0));
        WhorleyShader.SetVector("amountGroups", new(AmountGroups.x, AmountGroups.y, AmountGroups.z, 0));

        WhorleyShader.SetTexture(clearKernel, "result", TargetTexture);

        WhorleyShader.Dispatch(clearKernel, AmountGroups.x, AmountGroups.y, AmountGroups.z);
    }

    private void ClearBuffers(bool bClearVertex = false)
    {
        if (bClearVertex)
        {
            MalaiseBuffer?.Release();
        }
        PointBuffer?.Release();
        DirectionsBuffer?.Release();
        MinMaxBuffer?.Release();
    }

    private static Vector3[] DIRECTIONS3D = new Vector3[27]{
        new Vector3(+0, +0, -1),
        new Vector3(+1, +0, -1),
        new Vector3(+1, -1, -1),
        new Vector3(+0, -1, -1),
        new Vector3(-1, -1, -1),
        new Vector3(-1, +0, -1),
        new Vector3(-1, +1, -1),
        new Vector3(+0, +1, -1),
        new Vector3(+1, +1, -1),
        new Vector3(+0, +0, +0),
        new Vector3(+1, +0, +0),
        new Vector3(+1, -1, +0),
        new Vector3(+0, -1, +0),
        new Vector3(-1, -1, +0),
        new Vector3(-1, +0, +0),
        new Vector3(-1, +1, +0),
        new Vector3(+0, +1, +0),
        new Vector3(+1, +1, +0),
        new Vector3(+0, +0, +1),
        new Vector3(+1, +0, +1),
        new Vector3(+1, -1, +1),
        new Vector3(+0, -1, +1),
        new Vector3(-1, -1, +1),
        new Vector3(-1, +0, +1),
        new Vector3(-1, +1, +1),
        new Vector3(+0, +1, +1),
        new Vector3(+1, +1, +1),
    };

    private static Vector3[] DIRECTIONS2D = new Vector3[9]{
        new Vector3(+0, +0, +0),
        new Vector3(+1, +0, +0),
        new Vector3(+1, -1, +0),
        new Vector3(+0, -1, +0),
        new Vector3(-1, -1, +0),
        new Vector3(-1, +0, +0),
        new Vector3(-1, +1, +0),
        new Vector3(+0, +1, +0),
        new Vector3(+1, +1, +0)
    };

    private static Vector3[] DIRECTIONS = DIRECTIONS3D;
    private static int THREAD_AMOUNT = 8;
    private static int THREAD_AMOUNT_Z = 4;
}
