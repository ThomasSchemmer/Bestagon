using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Fotograph : GameService
{
    public Material RenderMat;

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceStart((MeshFactory Factory) => {
            StartCoroutine(TakePictures());
        });
    }

    protected override void StopServiceInternal() {}

    private IEnumerator TakePictures()
    {
        HexagonConfig.HexagonType[] Types = (HexagonConfig.HexagonType[])Enum.GetValues(typeof(HexagonConfig.HexagonType));
        foreach (var Type in Types)
        {
            yield return new WaitForEndOfFrame();
            TakePicture(Type);
        }
    }

    private void TakePicture(HexagonConfig.HexagonType Type)
    {
        GameObject Target = new GameObject();
        Target.transform.localScale = Vector3.one * 0.4f;
        Target.transform.rotation = Quaternion.Euler(-90, 0, 0);
        MeshFilter Filter = Target.AddComponent<MeshFilter>();
        MeshRenderer Renderer = Target.AddComponent<MeshRenderer>();
        Renderer.sharedMaterial = RenderMat;

        HexagonData Data = new HexagonData();
        Data.Type = Type;
        Data.UpdateDiscoveryState(HexagonData.DiscoveryState.Visited);
        if (!TileMeshGenerator.TryCreateMesh(Data, out Mesh Mesh))
        {
            Destroy(Target);
            return;
        }
        Filter.sharedMesh = Mesh;

        SetMaterialData(Type, Renderer);
        TakeActualFoto(Type);

        Destroy(Target);
        Debug.Log("Taken picture of " + Type);
    }

    private void TakeActualFoto(HexagonConfig.HexagonType Type)
    {
        Camera MainCam = Camera.main;
        RenderTexture rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
        MainCam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(256, 256, TextureFormat.ARGB32, false);
        MainCam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        MainCam.targetTexture = null;
        RenderTexture.active = null;
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = Application.dataPath + "/Resources/Pictures/" + Type.ToString() + ".png";
        System.IO.File.WriteAllBytes(filename, bytes);
        Destroy(rt);
    }

    private void SetMaterialData(HexagonConfig.HexagonType Type, MeshRenderer Renderer)
    {
        MaterialPropertyBlock Block = new MaterialPropertyBlock();
        Block.SetFloat("_Selected", 0);
        Block.SetFloat("_Hovered", 0);
        Block.SetFloat("_Adjacent", 0);
        Block.SetFloat("_Malaised", 0);
        Block.SetFloat("_Type", HexagonConfig.MaskToInt((int)Type, 16) + 1);
        Block.SetFloat("_Value", 0);
        Renderer.SetPropertyBlock(Block);
    }
    protected override void ResetInternal()
    {
    }
}
