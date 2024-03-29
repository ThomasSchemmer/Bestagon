using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageSystem : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        DisplayMessages();
        Game.Instance._OnPause += OnPause;
        Game.Instance._OnResume += OnResume;
    }

    private void OnPause()
    {
        EnableMessages(false);
    }

    private void OnResume()
    {
        EnableMessages(true);
    }

    private void EnableMessages(bool IsEnabled)
    {
        foreach (Message Message in Messages)
        {
            Message.gameObject.SetActive(IsEnabled);
        }
    }

    public static void CreateMessage(Message.Type Type, string Text) {
        if (!Instance)
            return;
        
        GameObject MessageObj = Instantiate(Instance.MessagePrefab);
        Message Message = MessageObj.GetComponent<Message>();
        Message.Initialize(Instance.GetSpriteByType(Type), Text);
        Message.transform.SetParent(Instance.transform);
        Instance.Messages.Add(Message);

        if (Instance.Messages.Count > MaxMessages) {
            Message ToDelete = Instance.Messages[0];
            Instance.Messages.RemoveAt(0);
            Destroy(ToDelete.gameObject);
        }

        Instance.DisplayMessages();
    }

    public static void DeleteMessage(Message Message) {
        if (!Instance || !Message)
            return;

        Instance.Messages.Remove(Message);
        DestroyImmediate(Message.gameObject);

        Instance.DisplayMessages();
    }

    public static void DeleteAllMessages() {
        // reverse to avoid change-while-iterating
        int Count = Instance.Messages.Count - 1;
        for (int i = Count; i >= 0; i--) {
            Message ToDelete = Instance.Messages[i];
            Instance.Messages.Remove(ToDelete);
            Destroy(ToDelete.gameObject);
        }
    }

    private Sprite GetSpriteByType(Message.Type Type) {
        switch (Type) {
            case Message.Type.Warning: return WarningSprite;
            default: return ErrorSprite;
        }
    }

    private void DisplayMessages() {
        for (int i = 0; i < transform.childCount; i++) {
            Transform Child = transform.GetChild(i);
            Child.transform.localPosition = Offset * i + Position;
            Child.transform.localScale = Vector3.one;
        }
    }

    public List<Message> Messages = new();
    public GameObject MessagePrefab;

    public Sprite WarningSprite;
    public Sprite ErrorSprite;

    private static MessageSystem Instance;
    private static Vector3 Offset = new Vector3(0, -80, 0);
    private static Vector3 Position = new Vector3(0, 200, 0);
    private static int MaxMessages = 8;
}
