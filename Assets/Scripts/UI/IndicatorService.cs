using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndicatorService : GameService
{
    public GameObject IndicatorPrefab;
    public GameObject IndicatorContainerPrefab;

    private Dictionary<IndicatorComponent, RectTransform> IndicatorMap = new();
    private List<IndicatorComponent> MarkedToDelete = new();
    private Camera MainCam;

    protected override void StartServiceInternal(){
        MainCam = Camera.main;
        Workers._OnWorkersAssigned += OnWorkerChanged;
    }

    protected override void StopServiceInternal(){
        Workers._OnWorkersAssigned -= OnWorkerChanged;
    }

    public void FixedUpdate()
    {
        foreach (var Key in IndicatorMap.Keys)
        {
            Key.UpdateVisuals();
        }

        foreach (var Key in MarkedToDelete)
        {
            DestroyImmediate(IndicatorMap[Key].gameObject);
            IndicatorMap.Remove(Key);
        }
        MarkedToDelete.Clear();
    }

    private void OnWorkerChanged(Location Location)
    {
        foreach (var Key in IndicatorMap.Keys)
        {
            if (!Key.IsFor(Location))
                continue;

            Key.UpdateSpritesVisuals();
        }
    }

    public void Register(IndicatorComponent IndComp) {
        if (IndicatorMap.ContainsKey(IndComp))
            return;

        GameObject Container = Instantiate(IndicatorContainerPrefab);
        RectTransform RectTrans = Container.GetComponent<RectTransform>();
        RectTrans.SetParent(transform);
        IndicatorMap[IndComp] = RectTrans;
        IndComp.CreateVisuals();
    }

    public void Deregister(IndicatorComponent IndComp)
    {
        if (!IndicatorMap.ContainsKey(IndComp))
            return;

        MarkedToDelete.Add(IndComp);
    }

    public RectTransform GetFor(IndicatorComponent IndComp)
    {
        if (!IndicatorMap.ContainsKey(IndComp))
            return null;

        return IndicatorMap[IndComp];
    }

    public bool IsRegistered(IndicatorComponent IndComp)
    {
        return IndicatorMap.ContainsKey(IndComp);
    }

    public void DeleteFor(IndicatorComponent IndComp)
    {
        IndicatorMap.Remove(IndComp);
    }

    public Vector2 WorldPosToScreenPos(Vector3 WorldPos)
    {
        return MainCam.WorldToScreenPoint(WorldPos);
    }
}
