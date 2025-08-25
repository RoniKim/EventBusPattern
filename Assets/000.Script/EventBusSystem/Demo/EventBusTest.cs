using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Roni.CustomEventSystem.EventBus.Core;
using Roni.CustomEventSystem.EventBus.Keys;

public class EventBusTest : MonoBehaviour
{
    void OnGUI()
    {
        // GUI ��ư���� ��ġ�� ũ�� ����
        float buttonWidth = 200f;
        float buttonHeight = 50f;
        float spacing = 60f;
        float startX = 50f;
        float startY = 50f;

        // ù ��° ��ư
        if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "���"))
        {
            Register();
        }

        // �� ��° ��ư
        if (GUI.Button(new Rect(startX, startY + spacing, buttonWidth, buttonHeight), "����"))
        {
            EventBusSystem.UnregisterAll();
        }
    }

    private void OnDisable()
    {
        EventBusSystem.UnregisterAll();
    }

    void Register()
    {
        EventBusSystem.Register(TestKeys.FirstKey, FirstKeyRegister);
        EventBusSystem.Register(TestKeys.SecondKey, SecondKeyRegister);
        EventBusSystem.Register(TestKeys.ThirdKey, ThirdKeyRegister);
    }

    void FirstKeyRegister(object _)
    {
        Debug.Log("ù ��° Ű ȣ��");
    }
    void SecondKeyRegister(object _)
    {
        Debug.Log("�� ��° Ű ȣ��");
    }
    void ThirdKeyRegister(object _)
    {
        Debug.Log("�� ��° Ű ȣ��");
    }

}
