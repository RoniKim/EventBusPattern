using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Roni.CustomEventSystem.EventBus.Core;
using Roni.CustomEventSystem.EventBus.Keys;

public class EventBusTest : MonoBehaviour
{
    void OnGUI()
    {
        // GUI 버튼들의 위치와 크기 설정
        float buttonWidth = 200f;
        float buttonHeight = 50f;
        float spacing = 60f;
        float startX = 50f;
        float startY = 50f;

        // 첫 번째 버튼
        if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "등록"))
        {
            Register();
        }

        // 두 번째 버튼
        if (GUI.Button(new Rect(startX, startY + spacing, buttonWidth, buttonHeight), "해제"))
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
        Debug.Log("첫 번째 키 호출");
    }
    void SecondKeyRegister(object _)
    {
        Debug.Log("두 번째 키 호출");
    }
    void ThirdKeyRegister(object _)
    {
        Debug.Log("세 번째 키 호출");
    }

}
