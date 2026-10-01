using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WordRPG.Game;
using WordRPG.Items;
using WordRPG.Monsters;

namespace WordRPG.EditorTools
{
    // 프로젝트의 모든 MonsterSpecies·ItemData를 GameDatabase에 모은다.
    // 몬스터·아이템을 새로 만들면 이 메뉴를 실행할 것 (안 하면 그 몬스터는 세이브에서 불러올 수 없음 — 테스트가 잡아냄)
    public static class GameDatabaseBuilder
    {
        public const string DatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("WordRPG/Data/Refresh Game Database")]
        public static GameDatabase Refresh()
        {
            var monsters = LoadAll<MonsterSpecies>().OrderBy(m => m.SpeciesId).ToList();
            var items = LoadAll<ItemData>().OrderBy(i => i.ItemId).ToList();
            WarnDuplicates(monsters.Select(m => m.SpeciesId), "몬스터");
            WarnDuplicates(items.Select(i => i.ItemId), "아이템");

            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<GameDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }
            database.ReplaceContents(monsters, items);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"[WordRPG] GameDatabase 갱신: 몬스터 {monsters.Count}, 아이템 {items.Count}");
            return database;
        }

        private static List<T> LoadAll<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .ToList();
        }

        private static void WarnDuplicates(IEnumerable<string> ids, string kind)
        {
            foreach (var group in ids.GroupBy(id => id))
            {
                if (string.IsNullOrEmpty(group.Key)) Debug.LogWarning($"[WordRPG] id가 비어 있는 {kind}가 있습니다");
                else if (group.Count() > 1) Debug.LogWarning($"[WordRPG] {kind} id '{group.Key}'가 {group.Count()}개 중복");
            }
        }
    }

    public static class SaveMenu
    {
        [MenuItem("WordRPG/Save/Open Save Folder")]
        public static void OpenFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("WordRPG/Save/Delete Save Data")]
        public static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("세이브 삭제", "저장된 진행 상황(파티, 단어 학습 기록)을 지우고 새 게임으로 시작합니다.", "삭제", "취소"))
                return;
            new Save.SaveSystem(Application.persistentDataPath).Delete();
            Debug.Log($"[WordRPG] 세이브 삭제: {Application.persistentDataPath}");
        }
    }
}
