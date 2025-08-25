using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Roni.CustomEventSystem.EventBus.Core;
using Roni.CustomEventSystem.EventBus.Keys;

public class EventBusTestUI : MonoBehaviour
{   
    public int eventIndex = -1;
    private Button btn;
    private void Awake()
    {
        btn = GetComponent<Button>();

        btn.onClick.AddListener(() => ExcuteEvent());
        //EventBusSystem.Execute(UIEventKeys.)
    }

    void ExcuteEvent()
    {
        switch (eventIndex)
        {
            case 0:
                {
                    EventBusSystem.Execute(TestKeys.FirstKey);
                    break;
                }
            case 1:
                {
                    EventBusSystem.Execute(TestKeys.SecondKey);
                    break;
                }
            case 2:
                {
                    EventBusSystem.Execute(TestKeys.ThirdKey);
                    break;
                }
        }
    }
}
