using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MeshPreview : CardPreview
{
    public override void Init(Card Card)
    {
        MeshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    public override void Show(HexagonVisualization Hex)
    {
        base.Show(Hex);
        transform.position = Hex.transform.position + Previewable.GetOffset();
        transform.localRotation = Previewable.GetRotation();
    }

    protected override void SetAllowed(bool bIsAllowed)
    {
        MaterialPropertyBlock Block = new MaterialPropertyBlock();
        Block.SetFloat("_Allowed", bIsAllowed ? 1 : 0);
        MeshRenderer.SetPropertyBlock(Block);
    }

    protected void InitRendering()
    {
        MeshFilter.sharedMesh = GetPreviewMesh();
        MeshRenderer.enabled = true;
        MeshRenderer.material = GetPreviewMaterial();
    }

    public abstract Mesh GetPreviewMesh();
    public abstract Material GetPreviewMaterial();

    protected MeshFilter MeshFilter;
    protected MeshRenderer MeshRenderer;
}
