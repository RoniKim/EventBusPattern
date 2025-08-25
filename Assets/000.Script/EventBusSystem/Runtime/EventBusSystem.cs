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
                Debug.LogWarning($"[UIEventBus] Ÿ�� �浹! {keyValue}�� {typeof(T)}�� �ƴ� �ٸ� Ÿ���� ��ϵ�.");
                return;
            }

#if UNITY_EDITOR
            // ��Ÿ������ ���
            if (!_registerMeta.TryGetValue(keyValue, out var list))
                _registerMeta[keyValue] = list = new List<EventRegisterInfo>();

            // callback�� ���� ����� ���� �߰�
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

                // ��ϵ� �׼��� ������ Ű ��ü�� ����
                if (_eventTable[keyValue] == null)
                    _eventTable.Remove(keyValue);
            }

#if UNITY_EDITOR
            if (_registerMeta.TryGetValue(keyValue, out var list))
            {
                // �ش� �ݹ�� ��ġ�ϴ� ��Ÿ������ ����
                foreach (var metadel in callback.GetInvocationList())
                {
                    list.RemoveAll(info =>
                        info.MethodName == metadel.Method.Name &&
                        info.TargetToString == (metadel.Target?.ToString() ?? "(static)"));
                }

                // ����Ʈ�� ��������� Ű ����
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
                        Debug.LogError($"[UIEventBus] {keyValue} ���� �� ����: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[UIEventBus] {keyValue}�� ��ϵ� �׼��� Ÿ���� {typeof(T)}�� ��ġ���� �ʽ��ϴ�.");
                }
            }
            else
            {
                Debug.LogWarning($"[UIEventBus] {keyValue}�� ��ϵ� ������ ����.");
            }
        }

        public static void UnregisterAll()
        {
            _eventTable.Clear();
#if UNITY_EDITOR
            _registerMeta.Clear();
#endif
        }

        // ���� �޼����
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