using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestService : GameService
{

    public Quest<T> AddQuest<T>(
        int CurrentProgress, 
        int MaxProgress, 
        Sprite Sprite, 
        Func<T, int> CheckSuccess,
        Action CompletionCallback
    )
    {
        GameObject QuestObj = Instantiate(QuestPrefab);
        Quest Quest = QuestObj.AddComponent<Quest>();
        Quest<T> QuestT = new(Quest);
        Quest.Add<T>(QuestT);
        QuestT.CheckSuccess = CheckSuccess;
        QuestT.AddCompletionCallback(CompletionCallback);
        Quest.CurrentProgress = CurrentProgress;
        Quest.MaxProgress = MaxProgress;
        Quest.Sprite = Sprite;

        QuestObj.transform.SetParent(transform, false);

        Quests.Add(Quest);
        return QuestT;
    }


    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((IconFactory IconFactory) =>
        {
            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal()
    {
    }

    public GameObject QuestPrefab;
    private List<Quest> Quests = new();
}
