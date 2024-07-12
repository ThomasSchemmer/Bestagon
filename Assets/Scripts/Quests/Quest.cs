using System;
using Unity.VectorGraphics;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UI;

/**
 * wrapper for templated subquest
 * Unity cannot add templated components, so we need to store the reference
 * Contains visual elements
 */
public class Quest : MonoBehaviour, UIElement
{
    public enum Type
    {
        Positive,
        Negative,
        Main
    }

    public Sprite Sprite;
    public float CurrentProgress = 0;
    public float MaxProgress = 5;
    public string Message = "";
    public Type QuestType = Type.Positive;

    protected SVGImage TypeImageSVG;
    protected Image TypeImage;
    protected Image BackgroundImage;
    protected Material Material;
    protected TMPro.TextMeshProUGUI Text;
    protected bool bIsHovered = false;

    // actual templated data (stored as abstract class to fascilate calling of functions)
    protected QuestTemplate QuestObject;

    public void Start()
    {
        BackgroundImage = transform.GetChild(0).GetComponent<Image>();
        TypeImageSVG = transform.GetChild(1).GetComponent<SVGImage>();
        TypeImage = transform.GetChild(2).GetComponent<Image>();
        Text = transform.GetChild(3).GetComponent<TMPro.TextMeshProUGUI>();

        // create a new one cause instanciation doesnt work on UI for some reason,
        // no access to MaterialBlock
        Material = Instantiate(TypeImage.material);
        TypeImage.material = Material;
        Visualize();
    }

    public void OnDestroy()
    {
        DestroyImmediate(Material);
        if (QuestObject == null)
            return;

        QuestObject.RemoveQuestCallback();
        QuestObject = null;
    }

    public void Add(QuestTemplate QuestObject)
    {
        this.QuestObject = QuestObject;
    }

    public void Visualize()
    {
        if (Material == null)
            return;

        Material.SetFloat("_CurrentProgress", CurrentProgress);
        Material.SetFloat("_MaxProgress", MaxProgress);
        bool bIsPositive = QuestType != Type.Negative;
        Material.SetFloat("_IsPositive", bIsPositive ? 1f : 0f);

        if (Sprite == null)
        {
            TypeImageSVG.color = new(0, 0, 0, 0);
        }
        else
        {
            TypeImageSVG.sprite = Sprite;
            TypeImageSVG.color = new(1, 1, 1, 1);
        }
        Text.text = Message + " (" + CurrentProgress + "/" +MaxProgress + ")";
        BackgroundImage.color = QuestType == Type.Main ? MainQuestColor : NormalQuestColor;
    }

    public void SetSelected(bool Selected) { }

    public void SetHovered(bool Hovered)
    {
        bIsHovered = Hovered;
    }

    public bool IsHovered()
    {
        return bIsHovered;
    }

    public void ClickOn(Vector2 PixelPos) { }

    public void Interact() { }

    public bool IsEqual(ISelectable other)
    {
        return other is QuestService;
    }

    public bool CanBeLongHovered()
    {
        return false;
    }

    private static Color NormalQuestColor = new(1, 1, 1, 1);
    private static Color MainQuestColor = new(0.52f, 0.68f, 0.85f, 1);

}


/** 
 * Actual templated quest, including different callbacks
 * Only has a weak ref to the monobehaviour parent, but should still have same lifetime!
 */
public class Quest<T> : QuestTemplate
{
    private void Destroy()
    {
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
        RemoveQuest();

        Destroy();
    }

    public Quest(Quest Parent){
        this.Parent = Parent;
    }

    ~Quest()
    {
        RemoveQuestCallback();
    }

    public void CompleteQuest()
    {
        RemoveQuestCallback();
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

    public Quest GetParent()
    {
        return Parent;
    }

    public override void RemoveQuestCallback()
    {
        DeRegisterAction.Invoke(this);
    }

    public void RemoveQuest()
    {
        if (!Game.TryGetService(out QuestService QuestService))
            return;

        QuestService.RemoveQuest(Parent);
    }

    public Func<T, int> CheckSuccess;
    public Action<Quest<T>> DeRegisterAction;

    protected Quest Parent;

    public delegate void OnQuestCompleted();
    public event OnQuestCompleted _OnQuestCompleted;
}

/**
 * We cant easily directly store and access a templated object, so use an abstract interface instead
 */
public abstract class QuestTemplate
{
    public abstract void RemoveQuestCallback();
}