using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Helper class to store Production uis more efficiently and update the icons instead of 
 * all GOs every time it updates. 
 */
public class ProductionGroup : MonoBehaviour
{
    private NumberedIconScreen[] IconScreens;
    private bool bSubscribe;
    private bool bIgnoreClicks;


    public void Initialize(Production Production, RectTransform GroupTransform, ISelectable Parent, bool bSubscribe, bool bIgnoreClicks = false)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        IconScreens = new NumberedIconScreen[MaxIconScreens];
        this.bSubscribe = bSubscribe;
        this.bIgnoreClicks = bIgnoreClicks;

        var Tuples = Production.GetTuples();
        if (Tuples.Count > MaxIconScreens)
        {
            Debug.LogError("Cannot create all IconScreens necessary, would cause overflow!");
        }

        for (int i = 0; i < MaxIconScreens; i++)
        {
            NumberedIconScreen UnitScreen = IconFactory.GetVisualsForNumberedIcon(GroupTransform, i);
            UnitScreen.Initialize(null, string.Empty, Parent);
            IconScreens[i] = UnitScreen;
        }

        UpdateVisuals(Production);
    }

    public void UpdateVisuals(Production Production)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        var Tuples = Production.GetTuples();
        for (int i = 0; i < MaxIconScreens; i++)
        {
            bool bIsNullIcon = i >= Tuples.Count;
            Tuple<Production.Type, int> Tuple = bIsNullIcon ? null : Tuples[i];
            Sprite Sprite = bIsNullIcon ? null : IconFactory.GetIconForProduction(Tuple.Key);
            string IconDescription = bIsNullIcon ? string.Empty : Tuple.Key.ToString();
            int Value = bIsNullIcon ? -1 : Tuple.Value;
            
            NumberedIconScreen UnitScreen = IconScreens[i];
            UnitScreen.SetSprite(Sprite);
            UnitScreen.SetHoverTooltip(IconDescription);
            UnitScreen.SetIgnored(bIgnoreClicks || bIsNullIcon);
            UnitScreen.UpdateVisuals(Value);
            UnitScreen.gameObject.SetActive(!bIsNullIcon);
            UnitScreen.gameObject.name = bIsNullIcon ? string.Empty : Tuple.Key.ToString();

            if (bSubscribe && !bIsNullIcon)
            {
                UnitScreen.SetSubscription(Tuple.Key, Tuple.Value);
            }
            else
            {
                UnitScreen.SetSubscription(default, -1);
            }
        }
    }

    // Anything more than this will result in overflowing cards!
    private static int MaxIconScreens = 3;
}
