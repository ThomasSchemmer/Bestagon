using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Class representing any screen, aka 2D element in the game (overlays, constant UI elements..)
 * Needs to have a container and background element to support enabling/disabling, as well as highlighting
 * eg:
 * Screen
 *  -> Container
 *      -> Background
 *      -> ..
 */
public abstract class ScreenUI : MonoBehaviour
{
    protected GameObject Container;
    protected GameObject Background;

    void Start()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        Container = transform.GetChild(0).gameObject;
        Background = Container.transform.GetChild(0).gameObject;
    }

    public virtual void Show()
    {
        Container.SetActive(true);
    }

    public virtual void Hide()
    {
        Container.SetActive(false);
    }
}
