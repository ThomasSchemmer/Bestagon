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
    protected SVGImage DiscoveryImage;
    protected Image TypeImage;
    protected Image BackgroundImage;
    protected Material Material;
    protected TMPro.TextMeshProUGUI Text;
    protected Button AcceptButton;

    protected bool bIsHovered = false;
    protected bool bIsDiscovered = false;

    // actual templated data (stored as abstract class to fascilate calling of functions)
    protected QuestTemplate QuestObject;

    public void Start()
    {
        BackgroundImage = transform.GetChild(0).GetComponent<Image>();
        TypeImageSVG = transform.GetChild(1).GetComponent<SVGImage>();
        TypeImage = transform.GetChild(2).GetComponent<Image>();
        Text = transform.GetChild(3).GetComponent<TMPro.TextMeshProUGUI>();
        DiscoveryImage = transform.GetChild(4).GetComponent<SVGImage>();
        AcceptButton = transform.GetChild(5).GetComponent<Button>();
        AcceptButton.onClick.RemoveAllListeners();
        AcceptButton.onClick.AddListener(() => {
            QuestObject.OnAccept();
        });
        AcceptButton.gameObject.layer = 0;

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
        DiscoveryImage.gameObject.SetActive(!IsDiscovered());
        AcceptButton.gameObject.SetActive(IsCompleted());
        TypeImageSVG.gameObject.SetActive(!IsCompleted());
        TypeImage.gameObject.SetActive(!IsCompleted());
    }

    public void SetSelected(bool Selected) { }

    public void SetHovered(bool Hovered)
    {
        bIsHovered = Hovered;
        bIsDiscovered = true;

        // can happen after being destroyed and dehover call triggers it
        if (DiscoveryImage == null)
            return;

        DiscoveryImage.enabled = !IsDiscovered();
    }

    public bool IsHovered()
    {
        return bIsHovered || !IsDiscovered();
    }

    public bool IsDiscovered()
    {
        return bIsDiscovered;
    }

    public bool IsCompleted()
    {
        return CurrentProgress >= MaxProgress;
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
 * Do not generate directly (except from savegame) - should be created from questable!
 */
public class Quest<T> : QuestTemplate
{
    private void Destroy()
    {
        _OnQuestCompleted = null;
        GameObject.DestroyImmediate(Parent.gameObject);
    }

    public override void OnAccept()
    {
        CompleteQuest();
        RemoveQuest();

        Destroy();
    }

    public void OnQuestProgress(T Var)
    {
        Parent.CurrentProgress += CheckSuccess(Var);
        Parent.Visualize();
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

        if (FollowUpQuest == null)
            return;

        QuestService.AddQuest(FollowUpQuest);
    }

    public Func<T, int> CheckSuccess;
    public Action<Quest<T>> DeRegisterAction;
    public Questable FollowUpQuest;

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
    public abstract void OnAccept();
}