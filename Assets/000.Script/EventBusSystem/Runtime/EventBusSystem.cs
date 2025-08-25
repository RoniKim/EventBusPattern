using System.Collections.Generic;
using System;
using UnityEngine;
using Roni.Utility.Debugging;

namespace Roni.CustomEventSystem.EventBus.Core
{
    public static class EventBusSystem
    {
        private static readonly Dictionary<string, Delegate> _eventTable = new();

#if UNITY_EDITOR
        public static Dictionary<string, Delegate> GetAllRegistered()
        {
            return _eventTable;
        }

        public class EventRegisterInfo
        {
            public Type ParamType;
            public string TargetClassName;
            public string MethodName;
            public string TargetToString;
            public string KeyValue;

        }

        private static readonly Dictionary<string, List<EventRegisterInfo>> _registerMeta = new();

        public static IReadOnlyDictionary<string, List<EventRegisterInfo>> GetRegisterMeta()
        {
            return _registerMeta;
        }
#endif

        public static void Register<T>(EventKey<T> key, Action<T> callback)
        {
            string keyValue = key.Value;

            if (!_eventTable.ContainsKey(keyValue))
                _eventTable[keyValue] = null;

            if (_eventTable[keyValue] is Action<T> old)
                _eventTable[keyValue] = old + callback;
            else if (_eventTable[keyValue] == null)
                _eventTable[keyValue] = callback;
            else
            {
                Debug.LogWarning($"[UIEventBus] 타입 충돌! {keyValue}에 {typeof(T)}이 아닌 다른 타입이 등록됨.");
                return;
            }

#if UNITY_EDITOR
            // 메타데이터 기록
            if (!_registerMeta.TryGetValue(keyValue, out var list))
                _registerMeta[keyValue] = list = new List<EventRegisterInfo>();

            // callback이 여러 개라면 각각 추가
            foreach (var del in callback.GetInvocationList())
            {
                list.Add(new EventRegisterInfo
                {
                    ParamType = typeof(T),
                    TargetClassName = del.Target != null ? del.Target.GetType().Name : "(static)",
                    MethodName = del.Method.Name,
                    TargetToString = del.Target?.ToString() ?? "(static)",
                    KeyValue = keyValue
                });
            }
#endif

            DebugExtensions.ShowMessageDebug(Color.green, "Register UI Event", $"Key : {keyValue} ParamType : {typeof(T)} {callback.Target}");
        }

        public static void Unregister<T>(EventKey<T> key, Action<T> callback)
        {
            string keyValue = key.Value;

            if (_eventTable.TryGetValue(keyValue, out var del) && del is Action<T> act)
            {
                _eventTable[keyValue] = act - callback;

                // 등록된 액션이 없으면 키 자체를 제거
                if (_eventTable[keyValue] == null)
                    _eventTable.Remove(keyValue);
            }

#if UNITY_EDITOR
            if (_registerMeta.TryGetValue(keyValue, out var list))
            {
                // 해당 콜백과 일치하는 메타데이터 제거
                foreach (var metadel in callback.GetInvocationList())
                {
                    list.RemoveAll(info =>
                        info.MethodName == metadel.Method.Name &&
                        info.TargetToString == (metadel.Target?.ToString() ?? "(static)"));
                }

                // 리스트가 비어있으면 키 제거
                if (list.Count == 0)
                    _registerMeta.Remove(keyValue);
            }
#endif
        }

        public static void Execute<T>(EventKey<T> key, T data)
        {            
            string keyValue = key.Value;

            if (_eventTable.TryGetValue(keyValue, out var del))
            {
                if (del is Action<T> action)
                {
                    try
                    {
                        action.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UIEventBus] {keyValue} 실행 중 오류: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[UIEventBus] {keyValue}에 등록된 액션의 타입이 {typeof(T)}와 일치하지 않습니다.");
                }
            }
            else
            {
                Debug.LogWarning($"[UIEventBus] {keyValue}에 등록된 리스너 없음.");
            }
        }

        public static void UnregisterAll()
        {
            _eventTable.Clear();
#if UNITY_EDITOR
            _registerMeta.Clear();
#endif
        }

        // 편의 메서드들
        public static void Execute<T>(EventKey<T> key) where T : new()
        {
            Execute(key, new T());
        }

        public static void ExecuteVoid(EventKey<object> key)
        {
            Execute(key, null);
        }
    }
}