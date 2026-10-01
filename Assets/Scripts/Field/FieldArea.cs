using System;
using System.Collections.Generic;
using UnityEngine;
using WordRPG.Items;
using WordRPG.Monsters;
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

    // 출입구(D) 하나의 연결: 도착 지역 + 도착 지역 맵의 몇 번째 D에 나타나는지
    [Serializable]
    public class AreaExit
    {
        [SerializeField] private FieldArea target;
        [Tooltip("도착 지역 맵에서 D를 위→아래, 왼→오른 순으로 센 번호 (0부터)")]
        [SerializeField] private int targetDoorIndex;

        public FieldArea Target => target;
        public int TargetDoorIndex => targetDoorIndex;

        private AreaExit() { } // Unity 직렬화용

        public AreaExit(FieldArea target, int targetDoorIndex)
        {
            this.target = target;
            this.targetDoorIndex = targetDoorIndex;
        }
    }

    [Serializable]
    public class BossEncounter
    {
        [SerializeField] private MonsterSpecies species;
        [SerializeField] private int level = 7;

        public MonsterSpecies Species => species;
        public int Level => level;

        private BossEncounter() { } // Unity 직렬화용

        public BossEncounter(MonsterSpecies species, int level)
        {
            this.species = species;
            this.level = level;
        }
    }

    // 탐험 지역 하나 (초원, 던전 …): 맵 + 출현 몬스터 + 출제 단어장 + 보물상자 내용물
    [CreateAssetMenu(fileName = "NewArea", menuName = "WordRPG/Field Area", order = 31)]
    public class FieldArea : ScriptableObject
    {
        [Tooltip("세이브에 기록되는 id. 정한 뒤에는 바꾸지 말 것")]
        [SerializeField] private string areaId;
        [SerializeField] private string displayName; // 예: "초원"
        [SerializeField] private FieldTheme theme = FieldTheme.Meadow;

        [Tooltip(". 길  , 풀숲(조우)  # 나무  ~ 물  P 시작 위치  D 출입구  /  부딪혀서 사용: F 회복의 샘  C 보물상자  E 진화의 제단  S 상점  B 보스")]
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

        [Header("마을")]
        [Tooltip("맵의 S(상점)에 부딪히면 여는 상점")]
        [SerializeField] private ShopData shop;

        [Header("보물상자 — 맵의 C를 위→아래, 왼→오른 순서로 하나씩 대응")]
        [SerializeField] private List<ChestContent> chests = new List<ChestContent>();

        [Header("출입구 — 맵의 D를 위→아래, 왼→오른 순서로 하나씩 대응")]
        [SerializeField] private List<AreaExit> exits = new List<AreaExit>();

        [Header("보스 — 맵의 B (한 번 쓰러뜨리면 다시 나오지 않음)")]
        [SerializeField] private BossEncounter boss;

        [NonSerialized] private FieldMap parsed;
        [NonSerialized] private string parsedFrom;

        public string AreaId => areaId;
        public string DisplayName => displayName;
        public EncounterTable Encounters => encounters;
        public WordDatabase Words => words;
        public float EncounterRate => encounterRate;
        public int MinStepsBetweenEncounters => minStepsBetweenEncounters;
        public IReadOnlyList<ChestContent> ChestContents => chests;
        public ShopData Shop => shop;
        public FieldTheme Theme => theme;
        public IReadOnlyList<AreaExit> Exits => exits;
        public BossEncounter Boss => boss != null && boss.Species != null ? boss : null;
        public string BossId => $"{areaId}:boss";

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

        public AreaExit GetExit(Vector2Int position)
        {
            int index = Map.DoorIndex(position);
            return index >= 0 && index < exits.Count ? exits[index] : null;
        }

        public ChestContent GetChestContent(Vector2Int position)
        {
            int index = Map.ChestIndex(position);
            return index >= 0 && index < chests.Count ? chests[index] : null;
        }
    }
}
