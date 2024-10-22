using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RelicScreen : ScreenUI
{
    public List<RelicGroupScreen> RelicGroups = new();

    public Transform RelicContainer;

    protected TMPro.TextMeshProUGUI ActiveText;

    protected override void Initialize()
    {
        base.Initialize();
        ActiveText = Container.transform.GetChild(3).GetComponent<TMPro.TextMeshProUGUI>();

        if (!Game.TryGetService(out RelicService RelicService))
            return;

        RelicService.OnRelicDiscoveryChanged.Add(SetCount);
    }

    public override void Show()
    {
        base.Show();

        ClearRelics();
        Game.RunAfterServiceInit((RelicService RelicService) =>
        {
            InitializeRelicGroups();
        });

        SetCount(RelicType.DEFAULT, Unlockables.State.Locked);
    }

    public override void Hide()
    {
        base.Hide();
        ClearRelics();
    }

    public void OnToggle()
    {
        if (Container == null)
            return;

        if (Container.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void SetCount(RelicType Type, Unlockables.State State)
    {
        if (!Game.TryGetService(out RelicService RelicService))
            return;

        ActiveText.text = "Active: " + RelicService.CurrentActiveRelics + "/" + RelicService.MaxActiveRelics;
    }

    private void InitializeRelicGroups()
    {
        if (!Game.TryGetServices(out RelicService RelicService, out IconFactory IconFactory))
            return;

        for (int i = 0; i < RelicService.UnlockableRelics.GetCategoryCount(); i++)
        {
            var Category = RelicService.UnlockableRelics.GetCategory(i);
            var GroupScreen = IconFactory.CreateRelicGroup(RelicContainer, Category, i);
            RelicGroups.Add(GroupScreen);
        }
    }

    private void ClearRelics()
    {
        for (int i = RelicGroups.Count - 1; i >= 0; i--)
        {
            Destroy(RelicGroups[i].gameObject);
        }
        RelicGroups.Clear();
    }

    private void OnDestroy()
    {
        if (!Game.TryGetService(out RelicService RelicService))
            return;

        RelicService.OnRelicDiscoveryChanged.Remove(SetCount);
    }
}
