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

    public ComputeShader WhorleyShader;
    [Range(1, 20)]
    public int NumPoints = 10;
    [Range(0.25f, 5)]
    public float Zoom = 4;
    [Range(1, 4)]
    public int Iterations = 2;
    [Range(0, 5)]
    public float Factor = 1.5f;

    private Camera MainCam;
    private ComputeBuffer MalaiseBuffer;
    private ComputeBuffer PointBuffer;
    private ComputeBuffer DirectionsBuffer;
    private ComputeBuffer DistancesBuffer;
    private ComputeBuffer MaxGroupDistancesBuffer;
    private ComputeBuffer MaxMinDistanceBuffer;

    private MapGenerator MapGen;

    public void OnDestroy()
    {
        ClearBuffers();
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
        Vector3Int ImageSize = new(TargetTexture.width, TargetTexture.height, TargetTexture.volumeDepth);
        Vector3Int AmountGroups = new(ImageSize.x / 16, ImageSize.y / 16, ImageSize.z / 1);

        FillWhorleyBuffers(createNoiseKernel, ImageSize, AmountGroups);

        WhorleyShader.Dispatch(createNoiseKernel, AmountGroups.x, AmountGroups.y, AmountGroups.z);
    }

    public void Tile()
    {
        Texture2D Tex = new(TargetTexture.width, TargetTexture.height, TextureFormat.ARGB32, TargetTexture.mipmapCount, true);
        Graphics.CopyTexture(TargetTexture, Tex);
        Graphics.Blit(Tex, TargetTexture, new Vector2(2, 2), new Vector2(0, 0));
    }

    private void FillPointBuffer()
    {
        Random.InitState(5);
        List<Vector3> Points = new List<Vector3>(NumPoints * NumPoints );   //* NumPoints 
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

    private void FillWhorleyBuffers(int createNoiseKernel, Vector3Int ImageSize, Vector3Int AmountGroups)
    {

        DirectionsBuffer = new ComputeBuffer(DIRECTIONS.Length, sizeof(float) * 3);
        DirectionsBuffer.SetData(DIRECTIONS);

        DistancesBuffer = new ComputeBuffer(ImageSize.x * ImageSize.y * ImageSize.z, sizeof(float));
        MaxGroupDistancesBuffer = new ComputeBuffer(AmountGroups.x * AmountGroups.y, sizeof(float));
        MaxMinDistanceBuffer = new ComputeBuffer(1, sizeof(float));

        WhorleyShader.SetBuffer(createNoiseKernel, "points", PointBuffer);
        WhorleyShader.SetBuffer(createNoiseKernel, "directions", DirectionsBuffer);
        WhorleyShader.SetBuffer(createNoiseKernel, "distances", DistancesBuffer);
        WhorleyShader.SetBuffer(createNoiseKernel, "maxGroupDistances", MaxGroupDistancesBuffer);
        WhorleyShader.SetBuffer(createNoiseKernel, "maxMinDistance", MaxMinDistanceBuffer);

        WhorleyShader.SetInt("directionsCount", DIRECTIONS.Length);
        WhorleyShader.SetFloat("pointCount", NumPoints);
        WhorleyShader.SetVector("size", new(ImageSize.x, ImageSize.y, ImageSize.z, 0));
        WhorleyShader.SetVector("amountGroups", new(AmountGroups.x, AmountGroups.y, AmountGroups.z, 0));

        WhorleyShader.SetTexture(createNoiseKernel, "result", TargetTexture);
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
        Vector3Int AmountGroups = new(ImageSize.x / 16, ImageSize.y / 16, ImageSize.z / 1);

        WhorleyShader.SetVector("size", new(ImageSize.x, ImageSize.y, ImageSize.z, 0));
        WhorleyShader.SetVector("amountGroups", new(AmountGroups.x, AmountGroups.y, AmountGroups.z, 0));

        WhorleyShader.SetTexture(clearKernel, "result", TargetTexture);

        WhorleyShader.Dispatch(clearKernel, AmountGroups.x, AmountGroups.y, AmountGroups.z);
    }

    private void ClearBuffers()
    {
        MalaiseBuffer?.Release();
        PointBuffer?.Release();
        DirectionsBuffer?.Release();
        DistancesBuffer?.Release();
        MaxGroupDistancesBuffer?.Release();
        MaxMinDistanceBuffer?.Release();
    }


    private static Vector3[] DIRECTIONS2D = new Vector3[9]{
        new Vector3(+0, +0),
        new Vector3(+1, +0),
        new Vector3(+1, -1),
        new Vector3(+0, -1),
        new Vector3(-1, -1),
        new Vector3(-1, +0),
        new Vector3(-1, +1),
        new Vector3(+0, +1),
        new Vector3(+1, +1),
    };

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
    private static Vector3[] DIRECTIONS = DIRECTIONS3D;
}
