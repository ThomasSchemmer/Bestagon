using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Tutorial to move the camera */
public class TutorialCameraQuest : Quest<Vector3>
{

    public TutorialCameraQuest() : base()
    {
    }

    public override int CheckSuccess(Vector3 Target)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "Move the camera around";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }
    public override Dictionary<IQuestRegister<Vector3>, ActionList<Vector3>> GetRegisterMap()
    {
        if (Game.GetService<CameraController>() == null)
            return new();

        return new()
        {
            { Game.GetService<CameraController>(), CameraController._OnCameraMoved }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Camera);
    }

    public override int GetStartProgress()
    {
        return 0;
    }

    public override void OnAfterCompletion(){}

    public override void OnCreated(){
    }

    public override bool AreRequirementsFulfilled()
    {
        if (!Game.TryGetService(out TutorialSystem Tuts))
            return false;

        return Tuts.IsInTutorial();
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(TutorialTileQuest);
        return true;
    }

    public override void GrantRewards()
    {
    }
}
