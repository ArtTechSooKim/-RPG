using System.IO;
using UnityEditor;
using UnityEngine;
using WordRPG.Monsters;

namespace WordRPG.EditorTools
{
    // Assets/Resources/Art/NinjaAdventure/Monsters/{speciesId}.png 가 있으면 그 몬스터의 Sprite 칸에 연결한다.
    // 그림 파일은 Tools/import_ninja_art.py 가 만든다. 그림이 없는 몬스터는 임시 도형(색 칸 + 첫 글자) 그대로
    public static class MonsterArtLinker
    {
        public const string MonsterArtFolder = "Assets/Resources/Art/NinjaAdventure/Monsters";

        [MenuItem("WordRPG/Data/Link Monster Art")]
        public static int LinkAll()
        {
            int linked = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:MonsterSpecies", new[] { "Assets/Data" }))
            {
                var species = AssetDatabase.LoadAssetAtPath<MonsterSpecies>(AssetDatabase.GUIDToAssetPath(guid));
                if (species == null || string.IsNullOrEmpty(species.SpeciesId)) continue;
                string path = $"{MonsterArtFolder}/{species.SpeciesId}.png";
                if (!File.Exists(path)) continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogWarning($"[WordRPG] {path} 를 스프라이트로 불러올 수 없습니다");
                    continue;
                }
                var so = new SerializedObject(species);
                var property = so.FindProperty("sprite");
                if (property.objectReferenceValue != sprite)
                {
                    property.objectReferenceValue = sprite;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(species);
                }
                linked++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[WordRPG] 몬스터 그림 연결: {linked}종");
            return linked;
        }
    }
}
