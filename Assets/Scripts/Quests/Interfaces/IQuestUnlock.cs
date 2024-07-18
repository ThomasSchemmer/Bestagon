using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Used to designate a class for unlocking quests*/
public interface IQuestUnlock {
    public bool ShouldUnlock();
}
