using System;
using UnityEngine;
using UnityEngine.UI;

/**
 * wrapper for templated subquest
 * Unity cannot add templated components, so we need to store the reference
 */
public class Quest : MonoBehaviour
{
    public Sprite Sprite;
    public float CurrentProgress = 0;
    public float MaxProgress = 5;
    public bool bIsPositive = true;

    protected Image Image;
    protected Material Material;

    protected object SelfQuest;

    public void Add<T>(Quest<T> SelfQuest)
    {
        this.SelfQuest = SelfQuest;
    }

    public Quest<T> Get<T>()
    {
        if (SelfQuest is not Quest<T>)
            return null;

        return SelfQuest as Quest<T>;
    }

    public void Start()
    {
        Image = transform.GetChild(1).GetComponent<Image>();
        Material = Instantiate(Image.material);
        Image.material = Material;
        Visualize();
    }

    public void OnDestroy()
    {
        DestroyImmediate(Material);
    }

    public void Visualize()
    {
        if (Material == null)
            return;

        Material.SetFloat("_CurrentProgress", CurrentProgress);
        Material.SetFloat("_MaxProgress", MaxProgress);
        Material.SetTexture("_TypeTex", Sprite.texture);
        Material.SetFloat("_IsPositive", bIsPositive ? 1f : 0f);
    }
}

/** 
 * Actual templated quest, including different callbacks
 * Only has a weak ref to the monobehaviour parent, but should still have same lifetime!
 */
public class Quest<T>
{
    private void Destroy()
    {
        Subscriber -= OnQuestProgress;
        _OnQuestCompleted = null;
        GameObject.DestroyImmediate(Parent.gameObject);
    }

    public void OnQuestProgress(T Var)
    {
        Parent.CurrentProgress += CheckSuccess(Var);
        Parent.Visualize();
        if (Parent.CurrentProgress < Parent.MaxProgress)
            return;

        CompleteQuest();

        Destroy();
    }

    public Quest(Quest Parent){
        this.Parent = Parent;
    }

    public void CompleteQuest()
    {
        _OnQuestCompleted?.Invoke();
        _OnQuestCompleted = null;
    }

    public void AddCompletionCallback(Action Callback)
    {
        _OnQuestCompleted += () =>
        {
            Callback();
        };
    }

    public Func<T, int> CheckSuccess;
    public Action<T> Subscriber;

    protected Quest Parent;

    public delegate void OnQuestCompleted();
    public event OnQuestCompleted _OnQuestCompleted;
}
