using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class AmberScreen : ScreenUI
{
    public Transform TuningContainer;
    public GameObject ActiveAmbersIcon;
    private List<AmberTuneScreen> Tunings = new();

    public void Start()
    {
        Game.RunAfterServicesInit((AmberService Ambers, IconFactory IconFactory) =>
        {
            Initialize();
            for (int i = 0; i < Ambers.AvailableAmbers.Count; i++)
            {
                AttributeType Type = Ambers.AvailableAmbers.GetKeyAt(i);
                CreateTuning(Type, i);
            }

            InitAmberIcon(IconFactory);
            AmberService._OnAmberAssigned += Refresh;
        });

    }

    private void InitAmberIcon(IconFactory IconFactory)
    {
        GameObject IconGO = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Amber, null, 0);
        IconGO.transform.SetParent(ActiveAmbersIcon.transform, false);
        ActiveAmbersIcon = IconGO;
        UpdateAmberIcon();
    }

    private void Refresh()
    {
        foreach (var Screen in Tunings)
        {
            Screen.Refresh();
        }
        UpdateAmberIcon();
    }

    private void UpdateAmberIcon()
    {
        if (!Game.TryGetService(out AmberService Ambers))
            return;

        var Icon = ActiveAmbersIcon.GetComponent<NumberedIconScreen>();
        Icon.UpdateVisuals(Ambers.ActiveAmberCount, Ambers.AvailableAmberCount);
    }

    private void OnDestroy()
    {
        AmberService._OnAmberAssigned -= Refresh;
    }

    private void CreateTuning(AttributeType Type, int Index)
    {
        GameObject TuningGO = new(Type.ToString());
        TuningGO.transform.SetParent(TuningContainer, false);
        var Tuning = TuningGO.AddComponent<AmberTuneScreen>();
        Tuning.Initialize(Type, Index);
        Tunings.Add(Tuning);
    }
}
