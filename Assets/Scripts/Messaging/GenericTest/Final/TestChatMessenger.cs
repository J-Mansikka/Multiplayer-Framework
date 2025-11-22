using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestChatMessenger : NewObject
{
    public bool isClient;
    public TMP_Text chatWindow;
    public TMP_InputField input;
    public RectTransform window;
    private MnetInt intTest;
    private MnetStringUnicode messageTest;


    private void Awake()
    {
        Initialize();
        chatWindow.text = local.name + " is ready to chat.";
    }

    public void Start()
    {
        if (transform.parent != null) window.position = window.position + new Vector3(690, 0, 0);
    }

    public override void UpdateState()
    {

    }

    public void SendString()
    {
        if (local.name == "Client") Debug.Log("Client tried to sent");
        messageTest.Value = input.text;
        Call("teststring");
    }

    public void SendInt()
    {
        int numba;
        if (int.TryParse(input.text, out numba))
        {
            intTest.Value = numba;
            Call("testint");
        }
    }


    public void _TestString()
    {
        chatWindow.text = messageTest.Value;
    }

    public void _TestInt()
    {
       chatWindow.text = ""+intTest.Value;
    }


}
