using System;
using System.Collections.Generic;
using UnityEngine;
using WordRPG.Field;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.Save;
using WordRPG.Words;

namespace WordRPG.Game
{
    public class ChestResult
    {
        public bool WasEmpty { get; }  // 이미 연 상자 (또는 내용물 미지정)
        public ItemData Item { get; }
        public int Count { get; }
        public int Gold { get; }

        public ChestResult(bool wasEmpty, ItemData item, int count, int gold)
        {
            WasEmpty = wasEmpty;
            Item = item;
            Count = count;
            Gold = gold;
        }
    }

    // 플레이어 한 명의 게임 진행 상태 전체: 파티, 소지품, 단어 학습 기록, 전적, 필드 위치·연 상자.
    // 세이브 파일(SaveData)과 서로 변환된다. MonoBehaviour가 아니라 테스트에서 바로 만들 수 있다
    public class GameSession
    {
        public const int MaxPartySize = 3;

        private readonly List<MonsterInstance> party;
        private readonly List<string> loadWarnings = new List<string>();

        public IReadOnlyList<MonsterInstance> Party => party;
        public Inventory Inventory { get; }
        public VocabularyProgress Vocabulary { get; }
        public PlayerRecord Record { get; }
        public WorldState World { get; }
        public IReadOnlyList<string> LoadWarnings => loadWarnings;

        public bool CanFight => party.Exists(m => !m.IsFainted);

        private GameSession(List<MonsterInstance> party, Inventory inventory, VocabularyProgress vocabulary,
            PlayerRecord record, WorldState world)
        {
            this.party = party;
            Inventory = inventory;
            Vocabulary = vocabulary;
            Record = record;
            World = world;
        }

        public static GameSession NewGame(IReadOnlyList<MonsterSpecies> starters, int level)
        {
            return new GameSession(CreateParty(starters, level), new Inventory(), new VocabularyProgress(),
                new PlayerRecord(), new WorldState());
        }

        public void RestoreParty()
        {
            foreach (var monster in party) monster.RestoreFully();
        }

        // 도감을 모두 채운 지역의 보상(징표 + 골드)을 지급한다. 지역당 한 번만 — 이미 받은 지역은 건너뜀.
        // 나중에 단어가 추가돼 완성률이 내려가도 받은 보상은 그대로 유지
        public List<DexCompletion> ClaimDexRewards(IEnumerable<WordDatabase> regions)
        {
            var completed = new List<DexCompletion>();
            foreach (var region in regions)
            {
                if (region == null || string.IsNullOrEmpty(region.RegionId)) continue;
                if (Record.HasClaimedRegion(region.RegionId)) continue;
                if (!Dex.GetProgress(region, Vocabulary).IsComplete) continue;

                Record.MarkRegionClaimed(region.RegionId);
                if (region.CompletionKeepsake != null) Inventory.Add(region.CompletionKeepsake);
                if (region.CompletionGold > 0) Inventory.AddGold(region.CompletionGold);
                completed.Add(new DexCompletion(region));
            }
            return completed;
        }

        // 보물상자는 상자마다 한 번만 열린다 (세이브에 기록). 진화 재료를 얻는 도감 외 경로
        public ChestResult OpenChest(FieldArea area, Vector2Int position)
        {
            string chestId = area.ChestId(position);
            if (World.IsChestOpened(chestId)) return new ChestResult(true, null, 0, 0);

            var content = area.GetChestContent(position);
            World.MarkChestOpened(chestId);
            if (content == null) return new ChestResult(true, null, 0, 0);

            if (content.Item != null && content.Count > 0) Inventory.Add(content.Item, content.Count);
            if (content.Gold > 0) Inventory.AddGold(content.Gold);
            return new ChestResult(false, content.Item, content.Count, content.Gold);
        }

        public SaveData ToSaveData(DateTime nowUtc)
        {
            var partyData = new List<MonsterSaveData>();
            foreach (var m in party)
                partyData.Add(new MonsterSaveData(m.Species.SpeciesId, m.Level, m.Exp, m.CurrentHp));
            return new SaveData(nowUtc, partyData, Inventory, Vocabulary, Record, World);
        }

        // 세이브에 있는 몬스터 종을 찾을 수 없으면(삭제·id 변경) 그 몬스터는 빼고 경고를 남긴다.
        // 파티가 통째로 비면 시작 몬스터로 채운다 — 가장 소중한 단어 학습 기록은 그대로 유지
        public static GameSession FromSaveData(SaveData data, GameDatabase database,
            IReadOnlyList<MonsterSpecies> fallbackStarters, int fallbackLevel)
        {
            var warnings = new List<string>();
            var restored = new List<MonsterInstance>();
            foreach (var saved in data.Party)
            {
                var species = database != null ? database.FindMonster(saved.SpeciesId) : null;
                if (species == null)
                {
                    warnings.Add($"세이브의 몬스터 '{saved.SpeciesId}'를 찾을 수 없어 제외했습니다");
                    continue;
                }
                if (restored.Count >= MaxPartySize) break;
                restored.Add(new MonsterInstance(species, saved.Level, saved.Exp, saved.CurrentHp));
            }

            if (restored.Count == 0)
            {
                warnings.Add("파티를 불러오지 못해 시작 몬스터로 채웠습니다 (단어 학습 기록은 유지)");
                restored = CreateParty(fallbackStarters, fallbackLevel);
            }

            var session = new GameSession(restored, data.Inventory, data.Vocabulary, data.Record, data.World);
            session.loadWarnings.AddRange(warnings);

            // 전멸한 채로 저장됐다면(패배 직후 앱 종료 등) 마을로 돌아온 것으로 보고 회복
            if (!session.CanFight) session.RestoreParty();
            return session;
        }

        private static List<MonsterInstance> CreateParty(IReadOnlyList<MonsterSpecies> starters, int level)
        {
            var list = new List<MonsterInstance>();
            if (starters != null)
            {
                foreach (var species in starters)
                {
                    if (species != null && list.Count < MaxPartySize) list.Add(new MonsterInstance(species, level));
                }
            }
            if (list.Count == 0) throw new ArgumentException("시작 몬스터가 없습니다", nameof(starters));
            return list;
        }
    }
}
