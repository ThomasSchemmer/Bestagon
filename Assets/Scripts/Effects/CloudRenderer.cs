using Unity.Mathematics;
using UnityEngine;

public class CloudRenderer : GameService
{
    public Material Material;

    public GameObject Rectangle;

    private Camera MainCam;
    private ComputeBuffer MalaiseBuffer;
    private MapGenerator MapGen;

    public void OnDestroy()
    {
        MalaiseBuffer?.Release();
    }

    private Vector3 Div (Vector3 A, Vector3 B)
    {
        return new Vector3(A.x / B.x, A.y / B.y, A.z / B.z);
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

        Vector2Int GlobalTileLocation = new(0, 1);
        int HexIndex = GlobalTileLocation.y * HexagonConfig.chunkSize * HexagonConfig.mapMaxChunk + GlobalTileLocation.x;
        // 50
        int IntIndex = (int)(HexIndex / 32.0f); // 1
        int IntRemainder = (HexIndex - IntIndex * 32); // 18
        int ByteIndex = (int)(IntRemainder / 8.0f); // 2
        int BitIndex = IntRemainder - ByteIndex * 8; // 2

        uint IntValue = MalaiseDTOs[IntIndex]; // 3758211264
        uint ByteValue = ((IntValue >> ((3 - ByteIndex) * 8)) & 0xFF); // 192
        uint BitValue = (ByteValue >> (7 - ByteIndex)) & 0x1;
    }

    protected override void StartServiceInternal()
    {
        MainCam = Camera.main;

        Game.RunAfterServiceInit((MapGenerator MapGenerator) =>
        {
            MapGen = MapGenerator;
            int MalaiseIntCount = MapGenerator.GetMalaiseDTOByteCount();
            MalaiseBuffer = new ComputeBuffer(MalaiseIntCount, 4);

            Vector2Int MaxLocation = HexagonConfig.GetMaxLocation().GlobalTileLocation;
            Material.SetVector("_WorldSize", new Vector4(MaxLocation.x, MaxLocation.y, 0, 0));
            Material.SetVector("_TileSize", HexagonConfig.TileSize);
            Material.SetFloat("_ChunkSize", HexagonConfig.chunkSize);
            Material.SetFloat("_NumberOfChunks", HexagonConfig.mapMaxChunk);
            Material.SetBuffer("MalaiseBuffer", MalaiseBuffer);
            PassMaterialBuffer();

            _OnInit?.Invoke();
        });
    }

    protected override void StopServiceInternal() {}
}
