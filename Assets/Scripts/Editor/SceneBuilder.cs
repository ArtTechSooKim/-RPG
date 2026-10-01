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
    // 전투 테스트 씬 생성. 샘플 데이터가 없으면 먼저 만든다.
    // 배치모드: -executeMethod WordRPG.EditorTools.SceneBuilder.CreateBattleScene
    public static class SceneBuilder
    {
        public const string BattleScenePath = "Assets/Scenes/Battle.unity";

        [MenuItem("WordRPG/Scenes/Create Battle Scene")]
        public static void CreateBattleScene()
        {
            SampleDataBuilder.Build();
            WordCsvImporter.ImportAll();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Background;
            camera.orthographic = true;

            // 게임 상태·저장 담당 (이후 다른 씬에서도 유지됨)
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

            var screenGo = new GameObject("BattleScreen", typeof(BattleScreen));
            var so = new SerializedObject(screenGo.GetComponent<BattleScreen>());
            so.FindProperty("encounter").objectReferenceValue = Load<EncounterTable>("Assets/Data/Encounters/meadow_field.asset");
            so.FindProperty("words").objectReferenceValue = Load<WordDatabase>("Assets/Data/Words/tier1_meadow.asset");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, BattleScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BattleScenePath, true) };
            Debug.Log($"[WordRPG] 전투 씬 생성: {BattleScenePath}");
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new System.IO.FileNotFoundException(path);
        }
    }
}
