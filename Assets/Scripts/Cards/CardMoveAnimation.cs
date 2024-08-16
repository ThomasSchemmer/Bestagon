using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Helper struct to keep an overview of where the cards need to "float" from/to on turn */
public class CardMoveAnimation
{
    public CardCollection SourceCollection, TargetCollection;
    public float RemainingDurationS;
    public Vector3 StartPosition;
}
