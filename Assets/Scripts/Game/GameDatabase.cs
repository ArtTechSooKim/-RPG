using System.Collections.Generic;
using UnityEngine;
using WordRPG.Items;
using WordRPG.Monsters;

namespace WordRPG.Game
{
    // 세이브 파일의 id(speciesId, itemId)를 실제 에셋으로 되돌리는 목록.
    // 메뉴 WordRPG > Data > Refresh Game Database 가 프로젝트의 모든 몬스터·아이템을 자동으로 채운다
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "WordRPG/Game Database", order = -10)]
    public class GameDatabase : ScriptableObject
    {
        [SerializeField] private List<MonsterSpecies> monsters = new List<MonsterSpecies>();
        [SerializeField] private List<ItemData> items = new List<ItemData>();

        public IReadOnlyList<MonsterSpecies> Monsters => monsters;
        public IReadOnlyList<ItemData> Items => items;

        public MonsterSpecies FindMonster(string speciesId)
        {
            foreach (var species in monsters)
            {
                if (species != null && species.SpeciesId == speciesId) return species;
            }
            return null;
        }

        public ItemData FindItem(string itemId)
        {
            foreach (var item in items)
            {
                if (item != null && item.ItemId == itemId) return item;
            }
            return null;
        }

        // 에디터 수집기 전용
        public void ReplaceContents(IEnumerable<MonsterSpecies> newMonsters, IEnumerable<ItemData> newItems)
        {
            monsters = new List<MonsterSpecies>(newMonsters);
            items = new List<ItemData>(newItems);
        }
    }
}
