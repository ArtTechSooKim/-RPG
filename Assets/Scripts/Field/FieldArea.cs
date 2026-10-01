using System;
using System.Collections.Generic;
using UnityEngine;
using WordRPG.Items;
using WordRPG.Words;

namespace WordRPG.Field
{
    [Serializable]
    public class ChestContent
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int count = 1;
        [SerializeField] private int gold;

        public ItemData Item => item;
        public int Count => count;
        public int Gold => gold;

        private ChestContent() { } // Unity 직렬화용

        public ChestContent(ItemData item, int count, int gold = 0)
        {
            this.item = item;
            this.count = count;
            this.gold = gold;
        }
    }

    // 탐험 지역 하나 (초원, 던전 …): 맵 + 출현 몬스터 + 출제 단어장 + 보물상자 내용물
    [CreateAssetMenu(fileName = "NewArea", menuName = "WordRPG/Field Area", order = 31)]
    public class FieldArea : ScriptableObject
    {
        [Tooltip("세이브에 기록되는 id. 정한 뒤에는 바꾸지 말 것")]
        [SerializeField] private string areaId;
        [SerializeField] private string displayName; // 예: "초원"

        [Tooltip(". 길  , 풀숲(조우)  # 나무  ~ 물  F 회복의 샘  C 보물상자  P 시작 위치")]
        [TextArea(12, 40)]
        [SerializeField] private string map;

        [Header("전투")]
        [SerializeField] private EncounterTable encounters;
        [SerializeField] private WordDatabase words;
        [Tooltip("풀숲 한 걸음마다 조우 확률")]
        [Range(0f, 1f)]
        [SerializeField] private float encounterRate = 0.12f;
        [Tooltip("조우 직후 이 걸음 수만큼은 다시 조우하지 않음")]
        [SerializeField] private int minStepsBetweenEncounters = 4;

        [Header("보물상자 — 맵의 C를 위→아래, 왼→오른 순서로 하나씩 대응")]
        [SerializeField] private List<ChestContent> chests = new List<ChestContent>();

        [NonSerialized] private FieldMap parsed;
        [NonSerialized] private string parsedFrom;

        public string AreaId => areaId;
        public string DisplayName => displayName;
        public EncounterTable Encounters => encounters;
        public WordDatabase Words => words;
        public float EncounterRate => encounterRate;
        public int MinStepsBetweenEncounters => minStepsBetweenEncounters;
        public IReadOnlyList<ChestContent> ChestContents => chests;

        // 맵 텍스트가 바뀌면 다시 해석 (인스펙터에서 고치면서 플레이할 수 있게)
        public FieldMap Map
        {
            get
            {
                if (parsed == null || parsedFrom != map)
                {
                    parsed = FieldMap.Parse(map);
                    parsedFrom = map;
                }
                return parsed;
            }
        }

        // 세이브용 상자 id. 맵에서 상자 위치를 옮기면 새 상자로 취급된다
        public string ChestId(Vector2Int position) => $"{areaId}:{position.x},{position.y}";

        public ChestContent GetChestContent(Vector2Int position)
        {
            int index = Map.ChestIndex(position);
            return index >= 0 && index < chests.Count ? chests[index] : null;
        }
    }
}
