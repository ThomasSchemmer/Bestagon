using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Message : MonoBehaviour
{
    public enum Type {
        Success,
        Warning,
        Error
    }

    public void Initialize(Sprite Sprite, string Message) {
        transform.GetChild(1).GetComponent<Image>().sprite = Sprite;
        transform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = Message;
        transform.GetChild(3).GetComponent<Button>().onClick.AddListener(() => {
            MessageSystemScreen.DeleteMessage(this);
        });
    }
}
