#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Wintek.CustomEventSystem.EventBus.Editor
{
    [System.Serializable]
    public class EventKeyDefinition
    {
        public string keyName = "";
        public string keyValue = "";
        public string parameterType = "object";
        public string customType = ""; // 커스텀 타입용
        public string description = "";
        public bool enabled = true;
   

        public static readonly string[] CommonTypes = new string[]
        {
            "object", "string", "int", "float", "bool", "Vector2", "Vector3",
            "GameObject", "Transform", "Color", "Texture2D", "Custom"
        };

        // 실제 사용할 타입 반환
        public string GetActualType()
        {
            return parameterType == "Custom" ? customType : parameterType;
        }

        // 커스텀 타입인지 확인
        public bool IsCustomType()
        {
            return parameterType == "Custom" && !string.IsNullOrEmpty(customType);
        }
    }

    [System.Serializable]
    public class EventKeyCategory
    {
        public string categoryName = "";
        public string className = "";
        public string description = "";
        public List<EventKeyDefinition> keys = new List<EventKeyDefinition>();
        public bool enabled = true;
        public bool expanded = true; // UI에서 펼침/접힘 상태

        public EventKeyCategory()
        {
        }

        public EventKeyCategory(string name, string cls, string desc = "")
        {
            categoryName = name;
            className = cls;
            description = desc;
        }
    }

    [System.Serializable]
    public class EventKeyCollection
    {
        public List<EventKeyCategory> categories = new List<EventKeyCategory>();
        public string version = "1.0";
        public string lastModified;
        public string author;

        public EventKeyCollection()
        {
            lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            author = System.Environment.UserName;
        }
    }

    public class EventKeyEditor : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<EventKeyCategory> categories = new List<EventKeyCategory>();

        // 설정
        private string outputPath = "Assets/00.Script/000.Runtime/000.Content/001.UI/EventBus/Keys";
        private string configPath = "Assets/Editor/EventKeyConfig.json";
        private string namespaceName = "Wintek.CustomEventSystem.EventBus.Keys";

        // 검색/필터
        private string searchFilter = "";
        private bool showOnlyEnabled = false;
        private string categoryFilter = "All";

        // 저장 방식 선택
        private enum SaveMode { EditorPrefs, JsonFile, Both }
        private SaveMode saveMode = SaveMode.JsonFile;

        // 폴더블 섹션 상태
        private bool showSettings = true;
        private bool showSaveSettings = false;
        private bool showFilters = true;

        // 탭 시스템
        private enum EditorTab { Main, Settings, Advanced }
        private EditorTab currentTab = EditorTab.Main;

        // 편집 상태
        private int editingCategoryIndex = -1;
        private int editingKeyIndex = -1;
        private bool compactMode = false;

        // 새 카테고리/키 추가 UI
        private string newCategoryName = "";
        private string newClassName = "";
        private string newCategoryDesc = "";

        [MenuItem("Tools/Event Bus/Key Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<EventKeyEditor>("EventKey Manager");
            window.minSize = new Vector2(700, 500);
            window.LoadKeyDefinitions();
        }

        void OnEnable()
        {
            LoadKeyDefinitions();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DrawHeader();
            DrawTabs();

            switch (currentTab)
            {
                case EditorTab.Main:
                    DrawMainTab();
                    break;
                case EditorTab.Settings:
                    DrawSettingsTab();
                    break;
                case EditorTab.Advanced:
                    DrawAdvancedTab();
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("EventKey 카테고리 관리자", EditorStyles.largeLabel);

            GUILayout.FlexibleSpace();

            // 상태 표시
            int totalKeys = categories.Sum(c => c.keys.Count);
            int enabledKeys = categories.Sum(c => c.keys.Count(k => k.enabled));
            EditorGUILayout.LabelField($"{categories.Count} 카테고리, {enabledKeys}/{totalKeys} 키",
                EditorStyles.miniLabel, GUILayout.Width(150));

            // 컴팩트 모드 토글
            compactMode = GUILayout.Toggle(compactMode, "컴팩트", EditorStyles.toolbarButton, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();

            if (!compactMode)
            {
                EditorGUILayout.HelpBox("카테고리별로 EventKey 클래스들을 생성합니다. (예: UIEventKeys, ActionEventKeys, SystemEventKeys)", MessageType.Info);
            }
        }

        void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(currentTab == EditorTab.Main, "메인", EditorStyles.toolbarButton))
                currentTab = EditorTab.Main;
            if (GUILayout.Toggle(currentTab == EditorTab.Settings, "설정", EditorStyles.toolbarButton))
                currentTab = EditorTab.Settings;
            if (GUILayout.Toggle(currentTab == EditorTab.Advanced, "고급", EditorStyles.toolbarButton))
                currentTab = EditorTab.Advanced;

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        void DrawMainTab()
        {
            DrawFoldoutSection("필터 및 검색", ref showFilters, DrawFilterSection);

            EditorGUILayout.Space(5);
            DrawNewCategorySection();
            EditorGUILayout.Space(5);
            DrawCategoriesList();
            EditorGUILayout.Space(5);
            DrawMainButtons();
        }

        void DrawSettingsTab()
        {
            DrawFoldoutSection("코드 생성 설정", ref showSettings, DrawCodeGenerationSettings);
            EditorGUILayout.Space(10);
            DrawFoldoutSection("저장 설정", ref showSaveSettings, DrawSaveSettingsContent);
        }

        void DrawAdvancedTab()
        {
            DrawAdvancedFeatures();
        }

        void DrawFoldoutSection(string title, ref bool foldout, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                EditorGUILayout.Space(5);
                drawContent?.Invoke();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawCodeGenerationSettings()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("출력 폴더:", GUILayout.Width(80));
            outputPath = EditorGUILayout.TextField(outputPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string newPath = EditorUtility.OpenFolderPanel("코드 출력 폴더", outputPath, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    outputPath = FileUtil.GetProjectRelativePath(newPath);
                    if (string.IsNullOrEmpty(outputPath))
                        outputPath = newPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            namespaceName = EditorGUILayout.TextField("네임스페이스:", namespaceName);

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("각 카테고리별로 별도의 클래스 파일이 생성됩니다.\n예: UIEventKeys.cs, ActionEventKeys.cs", MessageType.Info);
        }

        void DrawSaveSettingsContent()
        {
            saveMode = (SaveMode)EditorGUILayout.EnumPopup("저장 방식:", saveMode);

            string helpText = saveMode switch
            {
                SaveMode.EditorPrefs => "EditorPrefs 저장 (개인용, 버전관리 제외)",
                SaveMode.JsonFile => "JSON 파일 저장 (팀 공유, 버전관리 포함)",
                SaveMode.Both => "둘 다 사용 (하이브리드)",
                _ => ""
            };

            EditorGUILayout.HelpBox(helpText, MessageType.Info);

            if (saveMode != SaveMode.EditorPrefs)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("설정 파일:", GUILayout.Width(80));
                configPath = EditorGUILayout.TextField(configPath);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string newPath = EditorUtility.SaveFilePanel("설정 파일 경로", Path.GetDirectoryName(configPath), "EventKeyConfig", "json");
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        configPath = FileUtil.GetProjectRelativePath(newPath);
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (File.Exists(configPath))
                {
                    var fileInfo = new FileInfo(configPath);
                    EditorGUILayout.LabelField($"{fileInfo.Length}b, {fileInfo.LastWriteTime:MM-dd HH:mm}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("파일 없음 (저장시 생성)", EditorStyles.miniLabel);
                }
            }
        }

        void DrawFilterSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("검색:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();

            // 카테고리 필터
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("카테고리:", GUILayout.Width(70));
            var availableCategories = new List<string> { "All" };
            availableCategories.AddRange(categories.Select(c => c.categoryName));

            int currentIndex = availableCategories.IndexOf(categoryFilter);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(currentIndex, availableCategories.ToArray());
            if (newIndex >= 0 && newIndex < availableCategories.Count)
            {
                categoryFilter = availableCategories[newIndex];
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            showOnlyEnabled = EditorGUILayout.Toggle("활성화만", showOnlyEnabled, GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            // 퀵 액션 버튼들
            if (GUILayout.Button("전체 펼치기", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                categories.ForEach(c => c.expanded = true);
            }
            if (GUILayout.Button("전체 접기", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                categories.ForEach(c => c.expanded = false);
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawNewCategorySection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("새 카테고리 추가", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("카테고리명:", GUILayout.Width(80));
            newCategoryName = EditorGUILayout.TextField(newCategoryName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("클래스명:", GUILayout.Width(80));
            newClassName = EditorGUILayout.TextField(newClassName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("설명:", GUILayout.Width(80));
            newCategoryDesc = EditorGUILayout.TextField(newCategoryDesc);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("카테고리 추가", GUILayout.Width(100)))
            {
                if (!string.IsNullOrWhiteSpace(newCategoryName) && !string.IsNullOrWhiteSpace(newClassName))
                {
                    AddNewCategory(newCategoryName, newClassName, newCategoryDesc);
                    newCategoryName = "";
                    newClassName = "";
                    newCategoryDesc = "";
                }
                else
                {
                    EditorUtility.DisplayDialog("경고", "카테고리명과 클래스명을 입력해주세요.", "확인");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        void DrawCategoriesList()
        {
            var filteredCategories = GetFilteredCategories();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"카테고리 목록 ({filteredCategories.Count}/{categories.Count}개)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int categoryIndex = 0; categoryIndex < filteredCategories.Count; categoryIndex++)
            {
                var category = filteredCategories[categoryIndex];
                var originalCategoryIndex = categories.IndexOf(category);

                DrawCategoryHeader(category, originalCategoryIndex);

                if (category.expanded)
                {
                    DrawCategoryKeys(category, originalCategoryIndex);
                }

                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawCategoryHeader(EventKeyCategory category, int categoryIndex)
        {
            EditorGUILayout.BeginHorizontal("box", GUILayout.MinHeight(25));

            // 펼침/접힘 토글
            category.expanded = EditorGUILayout.Foldout(category.expanded, "", true);

            // 활성화 토글
            category.enabled = EditorGUILayout.Toggle(category.enabled, GUILayout.Width(20));

            // 카테고리 정보
            if (editingCategoryIndex == categoryIndex)
            {
                category.categoryName = EditorGUILayout.TextField(category.categoryName, GUILayout.MinWidth(100));
                category.className = EditorGUILayout.TextField(category.className, GUILayout.MinWidth(100));
                category.description = EditorGUILayout.TextField(category.description);
            }
            else
            {
                EditorGUILayout.LabelField($"{category.categoryName}", EditorStyles.boldLabel, GUILayout.MinWidth(100));
                EditorGUILayout.LabelField($"({category.className})", EditorStyles.miniLabel, GUILayout.MinWidth(100));
                EditorGUILayout.LabelField($"{category.keys.Count}개 키", EditorStyles.miniLabel, GUILayout.Width(60));
                if (!string.IsNullOrEmpty(category.description))
                {
                    EditorGUILayout.LabelField(category.description, EditorStyles.miniLabel);
                }
            }

            GUILayout.FlexibleSpace();

            // 카테고리 편집 버튼
            if (editingCategoryIndex == categoryIndex)
            {
                if (GUILayout.Button("✓", GUILayout.Width(25)))
                {
                    editingCategoryIndex = -1;
                    SaveKeyDefinitions();
                }
            }
            else
            {
                if (GUILayout.Button("✎", GUILayout.Width(25)))
                {
                    editingCategoryIndex = categoryIndex;
                }
            }

            // 새 키 추가
            if (GUILayout.Button("+ 키", GUILayout.Width(40)))
            {
                AddNewKeyToCategory(categoryIndex);
            }

            // 카테고리 이동
            if (GUILayout.Button("▲", GUILayout.Width(25)) && categoryIndex > 0)
            {
                SwapCategories(categoryIndex, categoryIndex - 1);
            }
            if (GUILayout.Button("▼", GUILayout.Width(25)) && categoryIndex < categories.Count - 1)
            {
                SwapCategories(categoryIndex, categoryIndex + 1);
            }

            // 카테고리 삭제
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("삭제 확인",
                    $"'{category.categoryName}' 카테고리와 모든 키({category.keys.Count}개)를 삭제하시겠습니까?", "삭제", "취소"))
                {
                    categories.RemoveAt(categoryIndex);
                    SaveKeyDefinitions();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawCategoryKeys(EventKeyCategory category, int categoryIndex)
        {
            var filteredKeys = GetFilteredKeysInCategory(category);

            if (filteredKeys.Count == 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(30);
                EditorGUILayout.LabelField("키가 없습니다.", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                return;
            }

            for (int keyIndex = 0; keyIndex < filteredKeys.Count; keyIndex++)
            {
                var key = filteredKeys[keyIndex];
                var originalKeyIndex = category.keys.IndexOf(key);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(30); // 들여쓰기

                if (compactMode)
                    DrawCompactKeyDefinition(key, categoryIndex, originalKeyIndex);
                else
                    DrawDetailedKeyDefinition(key, categoryIndex, originalKeyIndex);

                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawCompactKeyDefinition(EventKeyDefinition keyDef, int categoryIndex, int keyIndex)
        {
            EditorGUILayout.BeginHorizontal("box");

            // 활성화 토글
            keyDef.enabled = EditorGUILayout.Toggle(keyDef.enabled, GUILayout.Width(20));

            // 키 정보
            var combinedIndex = categoryIndex * 1000 + keyIndex; // 유니크한 인덱스 생성
            if (editingKeyIndex == combinedIndex)
            {
                keyDef.keyName = EditorGUILayout.TextField(keyDef.keyName, GUILayout.MinWidth(100));
                keyDef.keyValue = EditorGUILayout.TextField(keyDef.keyValue, GUILayout.MinWidth(100));

                int typeIndex = System.Array.IndexOf(EventKeyDefinition.CommonTypes, keyDef.parameterType);
                if (typeIndex < 0) typeIndex = 0;
                int newTypeIndex = EditorGUILayout.Popup(typeIndex, EventKeyDefinition.CommonTypes, GUILayout.Width(80));
                if (newTypeIndex >= 0) keyDef.parameterType = EventKeyDefinition.CommonTypes[newTypeIndex];

                if (keyDef.parameterType == "Custom")
                {
                    keyDef.customType = EditorGUILayout.TextField(keyDef.customType, GUILayout.Width(100));
                }
            }
            else
            {
                if (GUILayout.Button(keyDef.keyName, EditorStyles.label, GUILayout.MinWidth(100)))
                {
                    editingKeyIndex = editingKeyIndex == combinedIndex ? -1 : combinedIndex;
                }

                EditorGUILayout.LabelField($"= \"{keyDef.keyValue}\"", EditorStyles.miniLabel, GUILayout.MinWidth(100));
                EditorGUILayout.LabelField($"<{keyDef.GetActualType()}>", EditorStyles.miniLabel, GUILayout.Width(80));
            }

            GUILayout.FlexibleSpace();

            // 컨트롤 버튼들
            if (editingKeyIndex == combinedIndex)
            {
                if (GUILayout.Button("✓", GUILayout.Width(25)))
                {
                    editingKeyIndex = -1;
                    SaveKeyDefinitions();
                }
            }
            else
            {
                if (GUILayout.Button("✎", GUILayout.Width(25)))
                {
                    editingKeyIndex = combinedIndex;
                }
            }

            DrawKeyMoveButtons(categoryIndex, keyIndex);
            DrawKeyDeleteButton(categoryIndex, keyIndex, keyDef.keyName);

            EditorGUILayout.EndHorizontal();
        }

        void DrawDetailedKeyDefinition(EventKeyDefinition keyDef, int categoryIndex, int keyIndex)
        {
            EditorGUILayout.BeginVertical("box");

            // 헤더 라인
            EditorGUILayout.BeginHorizontal();
            keyDef.enabled = EditorGUILayout.Toggle(keyDef.enabled, GUILayout.Width(20));
            EditorGUILayout.LabelField($"#{keyIndex}", GUILayout.Width(30));

            DrawKeyMoveButtons(categoryIndex, keyIndex);
            DrawKeyDeleteButton(categoryIndex, keyIndex, keyDef.keyName);

            EditorGUILayout.EndHorizontal();

            if (!keyDef.enabled)
                GUI.enabled = false;

            // 키 정보
            keyDef.keyName = EditorGUILayout.TextField("변수 명:", keyDef.keyName);
            keyDef.keyValue = EditorGUILayout.TextField("키 값:", keyDef.keyValue);

            // 타입 선택
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("타입:", GUILayout.Width(70));
            int selectedIndex = System.Array.IndexOf(EventKeyDefinition.CommonTypes, keyDef.parameterType);
            if (selectedIndex < 0) selectedIndex = 0;
            int newIndex = EditorGUILayout.Popup(selectedIndex, EventKeyDefinition.CommonTypes);
            if (newIndex >= 0) keyDef.parameterType = EventKeyDefinition.CommonTypes[newIndex];
            EditorGUILayout.EndHorizontal();

            // 커스텀 타입
            if (keyDef.parameterType == "Custom")
            {
                EditorGUILayout.BeginHorizontal();
                keyDef.customType = EditorGUILayout.TextField("커스텀 타입:", keyDef.customType);
                if (GUILayout.Button("검색", GUILayout.Width(40)))
                {
                    ShowTypeSearchWindow(keyDef);
                }
                EditorGUILayout.EndHorizontal();
            }

            keyDef.description = EditorGUILayout.TextField("설명:", keyDef.description);

            if (!keyDef.enabled)
                GUI.enabled = true;

            // 미리보기
            if (!string.IsNullOrEmpty(keyDef.keyName) && !string.IsNullOrEmpty(keyDef.keyValue))
            {
                string preview = $"public static readonly EventKey<{keyDef.GetActualType()}> {keyDef.keyName} = new EventKey<{keyDef.GetActualType()}>(\"{keyDef.keyValue}\", \"{keyDef.description}\");";
                EditorGUILayout.LabelField("미리보기:", EditorStyles.miniLabel);
                EditorGUILayout.SelectableLabel(preview, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            EditorGUILayout.EndVertical();
        }

        void DrawKeyMoveButtons(int categoryIndex, int keyIndex)
        {
            if (GUILayout.Button("▲", GUILayout.Width(25)) && keyIndex > 0)
            {
                SwapKeysInCategory(categoryIndex, keyIndex, keyIndex - 1);
            }
            if (GUILayout.Button("▼", GUILayout.Width(25)) && keyIndex < categories[categoryIndex].keys.Count - 1)
            {
                SwapKeysInCategory(categoryIndex, keyIndex, keyIndex + 1);
            }
        }

        void DrawKeyDeleteButton(int categoryIndex, int keyIndex, string keyName)
        {
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("삭제 확인", $"'{keyName}' 키를 삭제하시겠습니까?", "삭제", "취소"))
                {
                    categories[categoryIndex].keys.RemoveAt(keyIndex);
                    editingKeyIndex = -1;
                    SaveKeyDefinitions();
                }
            }
        }

        void DrawMainButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("저장", GUILayout.Height(30), GUILayout.Width(80)))
            {
                SaveKeyDefinitions();
                ShowNotification(new GUIContent("저장 완료!"));
            }

            if (GUILayout.Button("불러오기", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("불러오기", "현재 편집 내용이 사라집니다.", "불러오기", "취소"))
                {
                    LoadKeyDefinitions();
                    ShowNotification(new GUIContent("불러오기 완료!"));
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("코드 생성", GUILayout.Height(30)))
            {
                SaveKeyDefinitions();
                GenerateCode();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawAdvancedFeatures()
        {
            EditorGUILayout.LabelField("고급 기능", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("데이터 관리", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("JSON으로 내보내기"))
            {
                ExportToJson();
            }
            if (GUILayout.Button("JSON에서 가져오기"))
            {
                ImportFromJson();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("중복 키 찾기"))
            {
                FindDuplicateKeys();
            }
            if (GUILayout.Button("정리하기"))
            {
                CleanupKeys();
            }
            EditorGUILayout.EndHorizontal();

       

            EditorGUILayout.Space(10);

            if (GUILayout.Button("모두 초기화", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("초기화 확인", "모든 카테고리와 키 정의를 삭제하시겠습니까?", "삭제", "취소"))
                {
                    categories.Clear();
                    SaveKeyDefinitions();
                    ShowNotification(new GUIContent("초기화 완료!"));
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 통계 정보
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("통계 정보", EditorStyles.boldLabel);

            int totalKeys = categories.Sum(c => c.keys.Count);
            int enabledKeys = categories.Sum(c => c.keys.Count(k => k.enabled));
            int enabledCategories = categories.Count(c => c.enabled);

            EditorGUILayout.LabelField($"• 전체 카테고리: {categories.Count}개");
            EditorGUILayout.LabelField($"• 활성화된 카테고리: {enabledCategories}개");
            EditorGUILayout.LabelField($"• 전체 키: {totalKeys}개");
            EditorGUILayout.LabelField($"• 활성화된 키: {enabledKeys}개");

            if (categories.Any())
            {
                EditorGUILayout.LabelField("• 카테고리별 키 수:");
                foreach (var category in categories.Take(5))
                {
                    EditorGUILayout.LabelField($"  - {category.categoryName}: {category.keys.Count}개", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();
        }

        // 카테고리 관련 메서드들
        void AddNewCategory(string categoryName, string className, string description = "")
        {
            // 중복 검사
            if (categories.Any(c => c.categoryName == categoryName || c.className == className))
            {
                EditorUtility.DisplayDialog("오류", "같은 이름의 카테고리나 클래스가 이미 존재합니다.", "확인");
                return;
            }

            var newCategory = new EventKeyCategory(categoryName, className, description);
            categories.Add(newCategory);
            SaveKeyDefinitions();
        }

     
       

        void AddNewKeyToCategory(int categoryIndex)
        {
            if (categoryIndex >= 0 && categoryIndex < categories.Count)
            {
                var category = categories[categoryIndex];
                var newKey = new EventKeyDefinition
                {
                    keyName = $"NewKey{category.keys.Count}",
                    keyValue = $"{category.className}_NewKey{category.keys.Count}",
                    parameterType = "object",
                    description = "새로운 이벤트 키",
                    enabled = true
                };

                category.keys.Add(newKey);
                SaveKeyDefinitions();
            }
        }

        void SwapCategories(int index1, int index2)
        {
            if (index1 >= 0 && index1 < categories.Count &&
                index2 >= 0 && index2 < categories.Count)
            {
                var temp = categories[index1];
                categories[index1] = categories[index2];
                categories[index2] = temp;
                SaveKeyDefinitions();
            }
        }

        void SwapKeysInCategory(int categoryIndex, int keyIndex1, int keyIndex2)
        {
            if (categoryIndex >= 0 && categoryIndex < categories.Count)
            {
                var category = categories[categoryIndex];
                if (keyIndex1 >= 0 && keyIndex1 < category.keys.Count &&
                    keyIndex2 >= 0 && keyIndex2 < category.keys.Count)
                {
                    var temp = category.keys[keyIndex1];
                    category.keys[keyIndex1] = category.keys[keyIndex2];
                    category.keys[keyIndex2] = temp;
                    SaveKeyDefinitions();
                }
            }
        }

        // 필터링 관련 메서드들
        List<EventKeyCategory> GetFilteredCategories()
        {
            return categories.Where(category =>
                (!showOnlyEnabled || category.enabled) &&
                (categoryFilter == "All" || category.categoryName == categoryFilter) &&
                (string.IsNullOrEmpty(searchFilter) ||
                 category.categoryName.ToLower().Contains(searchFilter.ToLower()) ||
                 category.className.ToLower().Contains(searchFilter.ToLower()) ||
                 category.description.ToLower().Contains(searchFilter.ToLower()) ||
                 category.keys.Any(k => k.keyName.ToLower().Contains(searchFilter.ToLower()) ||
                                       k.keyValue.ToLower().Contains(searchFilter.ToLower()) ||
                                       k.description.ToLower().Contains(searchFilter.ToLower())))
            ).ToList();
        }

        List<EventKeyDefinition> GetFilteredKeysInCategory(EventKeyCategory category)
        {
            return category.keys.Where(key =>
                (!showOnlyEnabled || key.enabled) &&
                (string.IsNullOrEmpty(searchFilter) ||
                 key.keyName.ToLower().Contains(searchFilter.ToLower()) ||
                 key.keyValue.ToLower().Contains(searchFilter.ToLower()) ||
                 key.description.ToLower().Contains(searchFilter.ToLower()))
            ).ToList();
        }

        // 중복 검사 및 정리
        void FindDuplicateKeys()
        {
            var allKeys = new List<(EventKeyDefinition key, string categoryName)>();

            foreach (var category in categories)
            {
                foreach (var key in category.keys)
                {
                    allKeys.Add((key, category.categoryName));
                }
            }

            var duplicates = allKeys
                .GroupBy(item => item.key.keyValue)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Any())
            {
                var message = new StringBuilder("중복된 키 값들:\n\n");
                foreach (var group in duplicates)
                {
                    message.AppendLine($"• '{group.Key}': {group.Count()}개");
                    foreach (var item in group)
                    {
                        message.AppendLine($"  - {item.key.keyName} ({item.categoryName})");
                    }
                    message.AppendLine();
                }

                EditorUtility.DisplayDialog("중복 키 발견", message.ToString(), "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("검사 완료", "중복된 키가 없습니다.", "확인");
            }
        }

        void CleanupKeys()
        {
            int removedKeys = 0;
            int removedCategories = 0;

            // 빈 키들 제거
            foreach (var category in categories)
            {
                int beforeCount = category.keys.Count;
                category.keys.RemoveAll(k =>
                    string.IsNullOrWhiteSpace(k.keyName) ||
                    string.IsNullOrWhiteSpace(k.keyValue));
                removedKeys += beforeCount - category.keys.Count;
            }

            // 빈 카테고리들 제거
            int beforeCategoryCount = categories.Count;
            categories.RemoveAll(c =>
                string.IsNullOrWhiteSpace(c.categoryName) ||
                string.IsNullOrWhiteSpace(c.className));
            removedCategories = beforeCategoryCount - categories.Count;

            if (removedKeys > 0 || removedCategories > 0)
            {
                SaveKeyDefinitions();
                EditorUtility.DisplayDialog("정리 완료",
                    $"빈 키 {removedKeys}개, 빈 카테고리 {removedCategories}개를 제거했습니다.", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("정리 완료", "제거할 빈 항목이 없습니다.", "확인");
            }
        }

        // 저장/로드 관련 메서드들
        void SaveKeyDefinitions()
        {
            var collection = new EventKeyCollection
            {
                categories = categories,
                lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                author = System.Environment.UserName
            };

            if (saveMode == SaveMode.EditorPrefs || saveMode == SaveMode.Both)
            {
                string json = JsonUtility.ToJson(collection, true);
                EditorPrefs.SetString("EventKeyEditor_Categories", json);
            }

            if (saveMode == SaveMode.JsonFile || saveMode == SaveMode.Both)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                    string json = JsonUtility.ToJson(collection, true);
                    File.WriteAllText(configPath, json, Encoding.UTF8);
                    AssetDatabase.Refresh();
                }
                catch (System.Exception ex)
                {
                    EditorUtility.DisplayDialog("저장 오류", $"JSON 파일 저장 실패:\n{ex.Message}", "확인");
                }
            }
        }

        void LoadKeyDefinitions()
        {
            EventKeyCollection collection = null;

            if ((saveMode == SaveMode.JsonFile || saveMode == SaveMode.Both) && File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath, Encoding.UTF8);
                    collection = JsonUtility.FromJson<EventKeyCollection>(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"JSON 파일 로드 실패: {ex.Message}");
                }
            }

            if (collection == null && (saveMode == SaveMode.EditorPrefs || saveMode == SaveMode.Both))
            {
                string json = EditorPrefs.GetString("EventKeyEditor_Categories", "");
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        collection = JsonUtility.FromJson<EventKeyCollection>(json);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"EditorPrefs 로드 실패: {ex.Message}");
                    }
                }
            }

            if (collection != null && collection.categories != null)
            {
                categories = collection.categories;
            }
            else
            {
                categories = new List<EventKeyCategory>();
            }
        }

        void ExportToJson()
        {
            string path = EditorUtility.SaveFilePanel("JSON으로 내보내기", "", "EventKeyCategories", "json");
            if (!string.IsNullOrEmpty(path))
            {
                var collection = new EventKeyCollection { categories = categories };
                string json = JsonUtility.ToJson(collection, true);
                File.WriteAllText(path, json, Encoding.UTF8);
                EditorUtility.DisplayDialog("내보내기 완료", $"파일이 저장되었습니다:\n{path}", "확인");
            }
        }

        void ImportFromJson()
        {
            string path = EditorUtility.OpenFilePanel("JSON에서 가져오기", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var collection = JsonUtility.FromJson<EventKeyCollection>(json);
                    if (collection?.categories != null)
                    {
                        categories = collection.categories;
                        SaveKeyDefinitions();

                        int totalKeys = categories.Sum(c => c.keys.Count);
                        EditorUtility.DisplayDialog("가져오기 완료",
                            $"{categories.Count}개 카테고리, {totalKeys}개 키를 가져왔습니다.", "확인");
                    }
                }
                catch (System.Exception ex)
                {
                    EditorUtility.DisplayDialog("가져오기 실패", $"파일 읽기 실패:\n{ex.Message}", "확인");
                }
            }
        }

        string ValidateCustomType(EventKeyDefinition keyDef)
        {
            if (string.IsNullOrEmpty(keyDef.customType)) return string.Empty;

            try
            {
                System.Type foundType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foundType = assembly.GetType(keyDef.customType);
                    if (foundType != null) break;

                    var types = assembly.GetTypes().Where(t => t.Name == keyDef.customType).ToArray();
                    if (types.Length > 0)
                    {
                        foundType = types[0];
                        keyDef.customType = foundType.FullName;
                        break;
                    }
                }

                return foundType != null
                    ? $"유효한 타입: {foundType.FullName}"
                    : $"타입을 찾을 수 없습니다: {keyDef.customType}";
            }
            catch (System.Exception ex)
            {
                return $"타입 검증 오류: {ex.Message}";
            }
        }

        void ShowTypeSearchWindow(EventKeyDefinition keyDef)
        {
            TypeSearchPopup.Show(type =>
            {
                keyDef.customType = type.FullName;
                Repaint();
            });
        }

        // 코드 생성
        void GenerateCode()
        {
            var enabledCategories = categories.Where(c => c.enabled).ToList();

            if (enabledCategories.Count == 0)
            {
                EditorUtility.DisplayDialog("경고", "활성화된 카테고리가 없습니다.", "확인");
                return;
            }

            try
            {
                Directory.CreateDirectory(outputPath);

                int generatedFiles = 0;
                int totalKeys = 0;

                foreach (var category in enabledCategories)
                {
                    var enabledKeys = category.keys.Where(k => k.enabled).ToList();
                    if (enabledKeys.Count == 0) continue;

                    var sb = new StringBuilder();
                    string fileName = $"{category.className}.cs";
                    string filePath = Path.Combine(outputPath, fileName);

                    // 헤더
                    sb.AppendLine("// 이 파일은 EventKey Manager에 의해 자동 생성되었습니다.");
                    sb.AppendLine("// 직접 수정하지 마세요. Manager에서 수정하세요.");
                    sb.AppendLine($"// 생성 시간: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();

                    // using
                    sb.AppendLine("using System;");
                    sb.AppendLine("using UnityEngine;");
                    sb.AppendLine("using Wintek.CustomEventSystem.EventBus.Core;");
                    sb.AppendLine();

                    // namespace 시작
                    sb.AppendLine($"namespace {namespaceName}");
                    sb.AppendLine("{");

                    // 클래스 시작
                    sb.AppendLine($"    /// <summary>");
                    sb.AppendLine($"    /// {category.description}");
                    sb.AppendLine($"    /// {enabledKeys.Count}개의 키가 정의되어 있습니다.");
                    sb.AppendLine($"    /// </summary>");
                    sb.AppendLine($"    public static class {category.className}");
                    sb.AppendLine("    {");

                    // 키 정의들
                    foreach (var key in enabledKeys.OrderBy(k => k.keyName))
                    {
                        string actualType = key.GetActualType();

                        if (!string.IsNullOrEmpty(key.description))
                        {
                            sb.AppendLine($"        /// <summary>");
                            sb.AppendLine($"        /// {key.description}");
                            if (key.IsCustomType())
                            {
                                sb.AppendLine($"        /// 커스텀 타입: {actualType}");
                            }
                            sb.AppendLine($"        /// </summary>");
                        }
                        sb.AppendLine($"        public static readonly EventKey<{actualType}> {key.keyName} = new EventKey<{actualType}>(\"{key.keyValue}\", \"{key.description}\");");
                        sb.AppendLine();
                    }

                    // 클래스 끝
                    sb.AppendLine("    }");

                    // namespace 끝
                    sb.AppendLine("}");

                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                    generatedFiles++;
                    totalKeys += enabledKeys.Count;
                }

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("성공",
                    $"코드가 성공적으로 생성되었습니다!\n\n" +
                    $"생성된 파일: {generatedFiles}개\n" +
                    $"총 키 개수: {totalKeys}개\n" +
                    $"경로: {outputPath}", "확인");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("오류", $"코드 생성 중 오류가 발생했습니다:\n{ex.Message}", "확인");
            }
        }
    }
}
#endif