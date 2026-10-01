using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WordRPG.Field;
using WordRPG.Game;
using WordRPG.Monsters;
using WordRPG.UI;
using WordRPG.Words;

namespace WordRPG.EditorTools
{
    // 씬 생성 (필드 = 메인, 전투 연습). 샘플 데이터가 없으면 먼저 만든다.
    // 배치모드: -executeMethod WordRPG.EditorTools.SceneBuilder.CreateAllScenes
    public static class SceneBuilder
    {
        public const string BattleScenePath = "Assets/Scenes/Battle.unity";

        public const string FieldScenePath = "Assets/Scenes/Field.unity";

        // 메인 씬: 초원 필드 (빌드 첫 번째 씬)
        [MenuItem("WordRPG/Scenes/Create Field Scene")]
        public static void CreateFieldScene()
        {
            PrepareData();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera(PlaceholderArt.OutsideMap);
            AddGameManager();

            var fieldGo = new GameObject("FieldScreen", typeof(FieldScreen));
            var so = new SerializedObject(fieldGo.GetComponent<FieldScreen>());
            so.FindProperty("area").objectReferenceValue = Load<FieldArea>("Assets/Data/Areas/meadow.asset");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, FieldScenePath);
            UpdateBuildSettings();
            Debug.Log($"[WordRPG] 필드 씬 생성: {FieldScenePath}");
        }

        // 전투만 연속으로 해보는 연습 씬
        [MenuItem("WordRPG/Scenes/Create Battle Scene")]
        public static void CreateBattleScene()
        {
            PrepareData();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera(Palette.Background);
            AddGameManager();

            var screenGo = new GameObject("BattleScreen", typeof(BattleScreen));
            var so = new SerializedObject(screenGo.GetComponent<BattleScreen>());
            so.FindProperty("encounter").objectReferenceValue = Load<EncounterTable>("Assets/Data/Encounters/meadow_field.asset");
            so.FindProperty("words").objectReferenceValue = Load<WordDatabase>("Assets/Data/Words/tier1_meadow.asset");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, BattleScenePath);
            UpdateBuildSettings();
            Debug.Log($"[WordRPG] 전투 씬 생성: {BattleScenePath}");
        }

        [MenuItem("WordRPG/Scenes/Create All Scenes")]
        public static void CreateAllScenes()
        {
            CreateBattleScene();
            CreateFieldScene();
        }

        private static void PrepareData()
        {
            SampleDataBuilder.Build();
            WordCsvImporter.ImportAll();
        }

        private static void AddCamera(Color background)
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = true;
            cameraGo.transform.position = new Vector3(0, 0, -10);
        }

        // 게임 상태·저장 담당 (씬이 바뀌어도 유지됨)
        private static void AddGameManager()
        {
            var managerGo = new GameObject("GameManager", typeof(GameManager));
            var manager = new SerializedObject(managerGo.GetComponent<GameManager>());
            manager.FindProperty("database").objectReferenceValue = Load<GameDatabase>(GameDatabaseBuilder.DatabasePath);
            var starters = manager.FindProperty("starterParty");
            string[] ids = { "nib", "bookshell", "lumi" };
            starters.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
                starters.GetArrayElementAtIndex(i).objectReferenceValue = Load<MonsterSpecies>($"Assets/Data/Monsters/{ids[i]}.asset");
            manager.FindProperty("starterLevel").intValue = 3;
            manager.ApplyModifiedPropertiesWithoutUndo();
        }

        // 필드가 첫 씬 (앱 실행 시 시작), 전투 연습 씬은 두 번째
        private static void UpdateBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var path in new[] { FieldScenePath, BattleScenePath })
            {
                if (System.IO.File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new System.IO.FileNotFoundException(path);
        }
    }
}
