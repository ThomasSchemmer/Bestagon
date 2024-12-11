using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageSystemScreen : ScreenUI
{
    protected override void Initialize()
    {
        base.Initialize();
        Instance = this;
        DisplayMessages();
        Game.Instance._OnPause += OnPause;
        Game.Instance._OnResume += OnResume;
    }

    private void OnPause()
    {
        Hide();
    }

    private void OnResume()
    {
        Show();
    }

    public static void CreateMessage(Message.Type Type, string Text) {
        if (!Instance)
            return;
        
        GameObject MessageObj = Instantiate(Instance.MessagePrefab);
        Message Message = MessageObj.GetComponent<Message>();
        Message.Initialize(Instance.GetSpriteByType(Type), Text);
        Message.transform.SetParent(Instance.Container.transform, false);
        Instance.Messages.Add(Message);

        if (Instance.Messages.Count > MaxMessages) {
            Message ToDelete = Instance.Messages[0];
            Instance.Messages.RemoveAt(0);
            Destroy(ToDelete.gameObject);
        }

        Instance.DisplayMessages();
    }

    public static void DeleteMessage(Message Message, bool bDisplayAfter = true) {
        if (!Instance || !Message)
            return;

        Instance.Messages.Remove(Message);
        DestroyImmediate(Message.gameObject);

        if (!bDisplayAfter)
            return;

        Instance.DisplayMessages();
    }

    public static void DeleteAllMessages() {
        // reverse to avoid change-while-iterating
        int Count = Instance.Messages.Count - 1;
        for (int i = Count; i >= 0; i--) {
            DeleteMessage(Instance.Messages[i], false);
        }
    }

    private Sprite GetSpriteByType(Message.Type Type) {
        switch (Type) {
            case Message.Type.Warning: return WarningSprite;
            case Message.Type.Success: return SuccessSprite;
            default: return ErrorSprite;
        }
    }

    private void DisplayMessages() {
        int MessageCount = 0;
        for (int i = 0; i < Container.transform.childCount; i++) {
            Transform Child = Container.transform.GetChild(i);
            if (Child.GetComponent<Message>() == null)
                continue;

            Child.transform.localPosition = Offset * MessageCount + Position;
            Child.transform.localScale = Vector3.one;
            MessageCount++;
        }
    }

    protected override bool CountsAsPopup()
    {
        return false;
    }

    public List<Message> Messages = new();
    public GameObject MessagePrefab;

    public Sprite WarningSprite;
    public Sprite ErrorSprite;
    public Sprite SuccessSprite;

    private static MessageSystemScreen Instance;
    private static Vector3 Offset = new Vector3(0, -80, 0);
    private static Vector3 Position = new Vector3(0, 200, 0);
    private static int MaxMessages = 8;
}
