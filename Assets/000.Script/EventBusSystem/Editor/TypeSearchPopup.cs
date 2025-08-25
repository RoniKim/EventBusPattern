// ���ο� �˻� �˾� ������ Ŭ����
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Wintek.CustomEventSystem.EventBus.Editor
{

    public class TypeSearchPopup : EditorWindow
    {
        private Action<Type> onTypeSelected;
        private static List<Type> cachedTypes = null;
        private string search = "";
        private Vector2 scroll;

        public static void Show(Action<Type> onSelected)
        {
            var wnd = CreateInstance<TypeSearchPopup>();
            wnd.onTypeSelected = onSelected;
            wnd.titleContent = new GUIContent("Ÿ�� �˻�");
            wnd.InitIfNeeded();
            wnd.ShowAuxWindow();
        }

        private void InitIfNeeded()
        {
            if (cachedTypes != null) return;

            cachedTypes = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string asmName = asm.FullName;
                // Editor/Unity/System ����� �̸� ����
                if (asmName.StartsWith("Unity") || asmName.StartsWith("System") || asmName.Contains("Editor"))
                    continue;
                try
                {
                    cachedTypes.AddRange(
                        asm.GetTypes().Where(t =>
                            t.IsPublic && !t.IsAbstract && !t.IsGenericType &&
                            (t.Namespace?.StartsWith("System") == false) &&
                            (t.Namespace?.StartsWith("Unity") == false) &&
                            (t.Namespace?.Contains("Editor") == false)
                        ));
                }
                catch { continue; }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            search = EditorGUILayout.TextField("�˻�", search);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            IEnumerable<Type> filtered = cachedTypes;

            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(t =>
                    t.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (t.Namespace?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
            }

            // �ʹ� ������ �ִ� 200�������� ����
            var filteredArr = filtered.Take(200).ToArray();

            foreach (var type in filteredArr)
            {
                string ns = type.Namespace ?? "Global";
                string label = $"{ns}.{type.Name}";
                if (GUILayout.Button(label, GUILayout.ExpandWidth(true)))
                {
                    onTypeSelected?.Invoke(type);
                    Close();
                }
            }
            EditorGUILayout.EndScrollView();

            if (filteredArr.Length == 0)
                EditorGUILayout.HelpBox("�˻� ��� ����", MessageType.Info);
            else if (filtered.Count() > 200)
                EditorGUILayout.HelpBox($"�˻������ 200���� �ʰ��մϴ�. �� ��ü������ �Է��ϼ���.", MessageType.Warning);

            EditorGUILayout.EndVertical();
        }
    }


}