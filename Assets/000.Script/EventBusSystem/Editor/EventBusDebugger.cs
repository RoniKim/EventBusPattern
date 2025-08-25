#if UNITY_EDITOR
using Roni.CustomEventSystem.EventBus.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace Wintek.CustomEventSystem.EventBus.Editor
{
    internal sealed class DebuggerKeyInfo
    {
        public string KeyValue;
        public string Description;
        public string DeclaringType;
        public Type ParameterType;
        public FieldInfo Field;
        public object CachedKeyObject;

        private string _statusReg;
        private string _statusUnreg;
        private string _friendlyType;

        public string GetStatusText(bool isRegistered) => isRegistered
            ? _statusReg ??= "상태: 현재 등록됨"
            : _statusUnreg ??= "상태: 현재 미등록";

        public string GetFriendlyTypeName() => _friendlyType ??= TypeNameHelper.GetFriendlyTypeName(ParameterType);
    }

    internal static class TypeNameHelper
    {
        private static readonly Dictionary<Type, string> Cache = new();

        public static string GetFriendlyTypeName(Type type)
        {
            if (type == null) return "unknown";
            if (Cache.TryGetValue(type, out var cached)) return cached;

            string result = type.IsGenericType
                ? $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName))}>"
                : type.Name switch
                {
                    "Object" => "object",
                    "String" => "string",
                    "Int32" => "int",
                    "Single" => "float",
                    "Double" => "double",
                    "Boolean" => "bool",
                    _ => type.Name
                };

            Cache[type] = result;
            return result;
        }
    }

    [InitializeOnLoad]
    internal static class EventKeyIndex
    {
        private static readonly object LockObj = new();
        private static bool Built;
        private static List<DebuggerKeyInfo> KeysList = new();
        private static Dictionary<string, DebuggerKeyInfo> KeyMap = new(StringComparer.Ordinal);
        private static string LastFilter = string.Empty;
        private static List<DebuggerKeyInfo> CachedResults = new();

        static EventKeyIndex() => AssemblyReloadEvents.afterAssemblyReload += Clear;

        public static IReadOnlyList<DebuggerKeyInfo> Keys => Built ? KeysList : Build();

        public static void Refresh() { Clear(); Build(); }

        public static void Clear()
        {
            lock (LockObj)
            {
                Built = false;
                KeysList.Clear();
                KeyMap.Clear();
                CachedResults.Clear();
                LastFilter = string.Empty;
            }
        }

        private static List<DebuggerKeyInfo> Build()
        {
            lock (LockObj)
            {
                if (Built) return KeysList;
                var results = new List<DebuggerKeyInfo>(256);
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException rtle) { types = rtle.Types.Where(t => t != null).ToArray(); }
                    catch { continue; }

                    foreach (var type in types)
                    {
                        if (!type.IsClass || !type.IsAbstract || !type.IsSealed) continue;
                        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (!f.IsInitOnly || !f.FieldType.IsGenericType || f.FieldType.GetGenericTypeDefinition() != typeof(EventKey<>)) continue;
                            object keyObj;
                            try { keyObj = f.GetValue(null); } catch { continue; }
                            if (keyObj == null) continue;

                            string value = null, desc = null;
                            try { value = f.FieldType.GetProperty("Value")?.GetValue(keyObj)?.ToString(); } catch { }
                            try { desc = f.FieldType.GetProperty("Description")?.GetValue(keyObj)?.ToString(); } catch { }
                            if (string.IsNullOrEmpty(value)) continue;

                            results.Add(new DebuggerKeyInfo
                            {
                                KeyValue = value,
                                Description = desc ?? string.Empty,
                                DeclaringType = type.Name,
                                ParameterType = f.FieldType.GetGenericArguments()[0],
                                Field = f,
                                CachedKeyObject = keyObj
                            });
                        }
                    }
                }
                results.Sort((a, b) => string.CompareOrdinal(a.DeclaringType, b.DeclaringType) != 0
                    ? string.CompareOrdinal(a.DeclaringType, b.DeclaringType)
                    : string.CompareOrdinal(a.KeyValue, b.KeyValue));
                KeysList = results;
                KeyMap = results.ToDictionary(k => k.KeyValue, StringComparer.Ordinal);
                Built = true;
                return KeysList;
            }
        }

        public static IReadOnlyList<DebuggerKeyInfo> Search(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return Keys;
            if (string.Equals(filter, LastFilter, StringComparison.Ordinal)) return CachedResults;

            LastFilter = filter.Trim();
            CachedResults = Keys.Where(k =>
                k.KeyValue.IndexOf(LastFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                k.DeclaringType.IndexOf(LastFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrEmpty(k.Description) && k.Description.IndexOf(LastFilter, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();
            return CachedResults;
        }
    }

    public sealed class EventBusDebuggerLight : EditorWindow
    {
        private Vector2 _scroll;
        private string _search = "";
        private bool _liveMode;
        private double _lastLiveRefresh;
        private float _liveInterval = 1f;

        private readonly Dictionary<string, bool> _foldouts = new(StringComparer.Ordinal);

        private static readonly GUIContent GC_Clear = new("Clear");
        private static readonly GUIContent GC_RefreshIndex = new("Refresh Index");
        private static readonly GUIContent GC_RefreshReg = new("Refresh Registered");
        private static readonly GUIContent GC_ClearAll = new("Clear All Events");
        private static readonly GUIContent GC_Live = new("Live");
        private static readonly GUIContent GC_Test = new("테스트값:");
        private static readonly GUIContent GC_Exec = new("Execute");
        private static readonly GUIContent GC_ExecDef = new("Execute (default)");

        [MenuItem("Tools/Event Bus/Debugger")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<EventBusDebuggerLight>("EventBus Debugger");
            wnd.minSize = new Vector2(600, 320);
        }

        private void OnEnable() => EditorApplication.update += OnUpdate;
        private void OnDisable() => EditorApplication.update -= OnUpdate;

        private void OnUpdate()
        {
            if (!_liveMode) return;
            if (EditorApplication.timeSinceStartup - _lastLiveRefresh < _liveInterval) return;
            _lastLiveRefresh = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space();

            var table = EventBusSystem.GetAllRegistered() ?? new(StringComparer.Ordinal);
            var meta = EventBusSystem.GetRegisterMeta() ?? new Dictionary<string, List<EventBusSystem.EventRegisterInfo>>(StringComparer.Ordinal);
            var regKeys = new HashSet<string>(table.Keys, StringComparer.Ordinal);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var group in EventKeyIndex.Search(_search).GroupBy(k => k.DeclaringType).OrderBy(g => g.Key))
            {
                bool exp = GetFoldout(group.Key);
                exp = EditorGUILayout.Foldout(exp, group.Key, true);
                SetFoldout(group.Key, exp);
                if (!exp) continue;

                foreach (var key in group)
                    DrawKeyRow(key, regKeys.Contains(key.KeyValue), table, meta);

                EditorGUILayout.Space(4);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var style = GUI.skin.FindStyle("ToolbarSearchTextField") ?? GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField;
                _search = GUILayout.TextField(_search ?? string.Empty, style, GUILayout.MinWidth(180));
                if (!string.IsNullOrEmpty(_search) && GUILayout.Button(GC_Clear, EditorStyles.toolbarButton, GUILayout.Width(60)))
                    _search = string.Empty;

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(GC_RefreshIndex, EditorStyles.toolbarButton, GUILayout.Width(100))) EventKeyIndex.Refresh();
                if (GUILayout.Button(GC_RefreshReg, EditorStyles.toolbarButton, GUILayout.Width(120))) Repaint();
                if (GUILayout.Button(GC_ClearAll, EditorStyles.toolbarButton, GUILayout.Width(140)) && EditorUtility.DisplayDialog("확인", "모든 등록된 이벤트를 제거하시겠습니까?", "예", "아니오"))
                { EventBusSystem.UnregisterAll(); Repaint(); }

                if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(80))) ExpandAll();
                if (GUILayout.Button("Contract All", EditorStyles.toolbarButton, GUILayout.Width(90))) ContractAll();

                _liveMode = GUILayout.Toggle(_liveMode, GC_Live, EditorStyles.toolbarButton, GUILayout.Width(42));
                using (new EditorGUI.DisabledScope(!_liveMode))
                    _liveInterval = EditorGUILayout.Slider(_liveInterval, 0.2f, 5f, GUILayout.Width(160));
            }
        }

        private void ExpandAll()
        {
            foreach (var item in _foldouts.Keys.ToList())
                SetFoldout(item, true);
        }

        private void ContractAll()
        {
            foreach (var item in _foldouts.Keys.ToList())
                SetFoldout(item, false);
        }

        private void DrawKeyRow(DebuggerKeyInfo info, bool isReg, Dictionary<string, Delegate> table, IReadOnlyDictionary<string, List<EventBusSystem.EventRegisterInfo>> meta)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(info.KeyValue, EditorStyles.boldLabel);
                    if (!string.IsNullOrEmpty(info.Description))
                        EditorGUILayout.LabelField(info.Description, EditorStyles.miniLabel);
                    if (info.ParameterType != null)
                        EditorGUILayout.LabelField($"<{info.GetFriendlyTypeName()}>", EditorStyles.miniLabel, GUILayout.Width(140));
                }
                EditorGUILayout.LabelField(info.GetStatusText(isReg), isReg ? EditorStyles.boldLabel : EditorStyles.miniLabel);
                if (isReg && meta.TryGetValue(info.KeyValue, out var list))
                    foreach (var m in list)
                        EditorGUILayout.LabelField($"→ {m.TargetClassName}.{m.MethodName} (Param: {TypeNameHelper.GetFriendlyTypeName(m.ParamType)})", EditorStyles.miniLabel);
                if (isReg && table.TryGetValue(info.KeyValue, out var del) && del != null)
                    EditorGUILayout.LabelField($"등록된 액션 수: {del.GetInvocationList().Length}", EditorStyles.miniLabel);

                DrawExecute(info);
            }
        }

        private void DrawExecute(DebuggerKeyInfo info)
        {
            var t = info.ParameterType;
            if (t != null && t != typeof(object))
            {
                if (t.IsPrimitive || t == typeof(string) || t == typeof(float) || t == typeof(double) || t == typeof(bool))
                {
                    string key = $"EventBusDebuggerLight_Input_{info.KeyValue}";
                    string input = SessionState.GetString(key, "");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(GC_Test, GUILayout.Width(60));
                        string newInput = EditorGUILayout.TextField(input);
                        if (newInput != input) SessionState.SetString(key, newInput);
                        if (GUILayout.Button(GC_Exec, GUILayout.Width(80))) EventExecutor.TryExecute(info, t, ParseParam(newInput, t));
                    }
                }
                else if (GUILayout.Button(GC_ExecDef, GUILayout.Width(140)))
                    EventExecutor.TryExecute(info, t, Activator.CreateInstance(t));
            }
            else if (GUILayout.Button(GC_Exec, GUILayout.Width(80)))
                EventExecutor.TryExecuteVoid(info);
        }

        private static object ParseParam(string input, Type t)
        {
            try
            {
                if (t == typeof(int)) return int.Parse(input);
                if (t == typeof(float)) return float.Parse(input);
                if (t == typeof(double)) return double.Parse(input);
                if (t == typeof(bool)) return input.Equals("true", StringComparison.OrdinalIgnoreCase) || input == "1";
                if (t == typeof(string)) return input;
            }
            catch { Debug.LogWarning("입력값 파싱 실패"); }
            return null;
        }

        private bool GetFoldout(string g) => _foldouts.TryGetValue(g, out var v) ? v : (_foldouts[g] = SessionState.GetInt($"EventBusDebuggerLight_Foldout_{g}", 1) != 0);
        private void SetFoldout(string g, bool v) { _foldouts[g] = v; SessionState.SetInt($"EventBusDebuggerLight_Foldout_{g}", v ? 1 : 0); }
    }

    internal static class EventExecutor
    {
        private static readonly Dictionary<(Type, Type), MethodInfo> Cache = new();

        private static MethodInfo ResolveExecute(Type keyType, Type paramType)
        {
            if (Cache.TryGetValue((keyType, paramType), out var mi)) return mi;
            var busType = typeof(EventBusSystem);
            mi = busType.GetMethod("Execute", new[] { keyType, paramType });
            if (mi == null && keyType.IsGenericType && keyType.GetGenericTypeDefinition() == typeof(EventKey<>))
            {
                var keyArg = keyType.GetGenericArguments()[0];
                var generic = busType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "Execute" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
                if (generic != null) { try { mi = generic.MakeGenericMethod(keyArg); } catch { } }
            }
            Cache[(keyType, paramType)] = mi;
            return mi;
        }

        public static void TryExecute(DebuggerKeyInfo info, Type paramType, object param)
        {
            var keyObj = info.CachedKeyObject ??= info.Field?.GetValue(null);
            if (keyObj == null) { Debug.LogWarning("키 객체를 찾을 수 없습니다."); return; }
            var method = ResolveExecute(keyObj.GetType(), paramType);
            if (method == null) { Debug.LogWarning("Execute 메서드 호출 실패"); return; }
            try { method.Invoke(null, new object[] { keyObj, param }); }
            catch (Exception e) { Debug.LogException(e); }
        }

        public static void TryExecuteVoid(DebuggerKeyInfo info)
        {
            var keyObj = info.CachedKeyObject ??= info.Field?.GetValue(null);
            if (keyObj == null) { Debug.LogWarning("키 객체를 찾을 수 없습니다."); return; }
            var method = ResolveExecute(keyObj.GetType(), typeof(object));
            if (method == null) { Debug.LogWarning("Execute(object) 메서드를 찾을 수 없습니다."); return; }
            try { method.Invoke(null, new object[] { keyObj, null }); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
#endif
