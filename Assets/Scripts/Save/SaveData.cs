using System;
using System.Collections.Generic;
using UnityEngine;
using WordRPG.Game;
using WordRPG.Items;
using WordRPG.Words;

namespace WordRPG.Save
{
    [Serializable]
    public class MonsterSaveData
    {
        [SerializeField] private string speciesId;
        [SerializeField] private int level;
        [SerializeField] private int exp;
        [SerializeField] private int currentHp;

        public string SpeciesId => speciesId;
        public int Level => level;
        public int Exp => exp;
        public int CurrentHp => currentHp;

        private MonsterSaveData() { } // Unity 직렬화용

        public MonsterSaveData(string speciesId, int level, int exp, int currentHp)
        {
            this.speciesId = speciesId;
            this.level = level;
            this.exp = exp;
            this.currentHp = currentHp;
        }
    }

    // 세이브 파일(save.json) 한 개의 내용. 에셋은 전부 id 문자열로 저장해서 에셋 이름이 바뀌어도 깨지지 않는다.
    // 필드를 추가해도 예전 세이브는 그 필드가 기본값으로 읽힌다. 기존 필드의 의미를 바꿀 때만 version을 올리고 변환 코드를 넣을 것
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private long savedAtTicks;
        [SerializeField] private List<MonsterSaveData> party = new List<MonsterSaveData>();
        [SerializeField] private Inventory inventory = new Inventory();
        [SerializeField] private VocabularyProgress vocabulary = new VocabularyProgress();
        [SerializeField] private PlayerRecord record = new PlayerRecord();
        [SerializeField] private WorldState world = new WorldState();

        public int Version => version;
        public DateTime SavedAtUtc => new DateTime(savedAtTicks, DateTimeKind.Utc);
        public IReadOnlyList<MonsterSaveData> Party => party;
        public Inventory Inventory => inventory;
        public VocabularyProgress Vocabulary => vocabulary;
        public PlayerRecord Record => record;
        public WorldState World => world;

        public SaveData() { }

        public SaveData(DateTime savedAtUtc, List<MonsterSaveData> party, Inventory inventory,
            VocabularyProgress vocabulary, PlayerRecord record, WorldState world = null)
        {
            savedAtTicks = savedAtUtc.Ticks;
            this.party = party;
            this.inventory = inventory;
            this.vocabulary = vocabulary;
            this.record = record;
            this.world = world ?? new WorldState();
        }

        public string ToJson() => JsonUtility.ToJson(this, true);

        // 형식이 깨졌으면 예외
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new FormatException("세이브 파일이 비어 있습니다");
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || data.version <= 0) throw new FormatException("세이브 형식이 아닙니다");
            if (data.version > CurrentVersion)
                throw new FormatException($"더 새로운 버전의 세이브입니다 (v{data.version}). 앱을 업데이트하세요");
            data.party = data.party ?? new List<MonsterSaveData>();
            data.inventory = data.inventory ?? new Inventory();
            data.vocabulary = data.vocabulary ?? new VocabularyProgress();
            data.record = data.record ?? new PlayerRecord();
            data.world = data.world ?? new WorldState();
            return data;
        }
    }
}
