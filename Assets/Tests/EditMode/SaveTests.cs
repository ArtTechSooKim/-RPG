using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordRPG.Game;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.Save;
using WordRPG.Words;

namespace WordRPG.Tests
{
    public class SaveSystemTests
    {
        private string directory;
        private SaveSystem system;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "WordRPG_SaveTests_" + Guid.NewGuid().ToString("N"));
            system = new SaveSystem(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        private static SaveData Sample(int gold)
        {
            var inventory = new Inventory();
            inventory.AddGold(gold);
            return new SaveData(DateTime.UtcNow, new List<MonsterSaveData> { new MonsterSaveData("nib", 3, 5, 20) },
                inventory, new VocabularyProgress(), new PlayerRecord());
        }

        [Test]
        public void NoSaveYet()
        {
            Assert.IsFalse(system.HasSave);
            var result = system.Load();
            Assert.IsNull(result.Data);
            Assert.IsNull(result.Error, "파일이 없는 건 오류가 아니다");
        }

        [Test]
        public void SaveThenLoad()
        {
            system.Save(Sample(42));

            var result = system.Load();

            Assert.IsTrue(system.HasSave);
            Assert.IsFalse(result.UsedBackup);
            Assert.AreEqual(42, result.Data.Inventory.Gold);
            Assert.AreEqual("nib", result.Data.Party[0].SpeciesId);
            Assert.AreEqual(SaveData.CurrentVersion, result.Data.Version);
        }

        [Test]
        public void CorruptMainFallsBackToPreviousSave()
        {
            system.Save(Sample(10));
            system.Save(Sample(20)); // 이전 저장(10)이 백업으로
            File.WriteAllText(system.MainPath, "{ 깨진 파일");

            var result = system.Load();

            Assert.IsTrue(result.UsedBackup);
            Assert.AreEqual(10, result.Data.Inventory.Gold);
        }

        [Test]
        public void BothCorruptReportsErrorAndCanBeQuarantined()
        {
            system.Save(Sample(10));
            system.Save(Sample(20));
            File.WriteAllText(system.MainPath, "garbage");
            File.WriteAllText(system.BackupPath, "");

            var result = system.Load();
            Assert.IsNull(result.Data);
            Assert.IsNotNull(result.Error);

            system.QuarantineCorrupt();
            Assert.IsFalse(system.HasSave);
            Assert.AreEqual(2, Directory.GetFiles(directory, "*.corrupt_*").Length, "깨진 파일은 지우지 않고 보관");
        }

        [Test]
        public void NewerVersionIsRejected()
        {
            system.Save(Sample(10));
            File.WriteAllText(system.MainPath, File.ReadAllText(system.MainPath).Replace("\"version\": 1", "\"version\": 99"));
            File.Delete(system.BackupPath);

            var result = system.Load();

            Assert.IsNull(result.Data);
            StringAssert.Contains("새로운 버전", result.Error);
        }

        [Test]
        public void DeleteRemovesEverything()
        {
            system.Save(Sample(1));
            system.Save(Sample(2));
            system.Delete();
            Assert.IsFalse(system.HasSave);
        }
    }

    public class GameSessionTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        private MonsterSpecies nib, shell;
        private GameDatabase database;

        [SetUp]
        public void SetUp()
        {
            nib = TestData.Species("nib", new MonsterStats(30, 14, 8), new MonsterStats(4, 3, 1));
            shell = TestData.Species("bookshell", new MonsterStats(40, 8, 14), new MonsterStats(6, 1, 3));
            database = ScriptableObject.CreateInstance<GameDatabase>();
            database.ReplaceContents(new[] { nib, shell }, new ItemData[0]);
        }

        // 저장 → JSON 문자열 → 다시 읽기 (실제 파일 저장과 같은 경로)
        private GameSession RoundTrip(GameSession session)
        {
            var json = session.ToSaveData(T0).ToJson();
            return GameSession.FromSaveData(SaveData.FromJson(json), database, new[] { nib }, 1);
        }

        [Test]
        public void NewGameStartsWithStarters()
        {
            var session = GameSession.NewGame(new[] { nib, shell }, 3);

            Assert.AreEqual(2, session.Party.Count);
            Assert.AreEqual(3, session.Party[0].Level);
            Assert.AreEqual(0, session.Vocabulary.DiscoveredCount);
        }

        [Test]
        public void EverythingSurvivesSaveAndLoad()
        {
            var session = GameSession.NewGame(new[] { nib, shell }, 1);
            session.Party[0].GainExp(25);          // Lv2, exp 5
            session.Party[1].TakeDamage(7);
            session.Inventory.AddGold(123);
            session.Inventory.Add("shiny_ink", 2);
            var rules = new MasteryRules();
            session.Vocabulary.RecordAnswer("abandon", true, T0, rules);
            session.Vocabulary.RecordAnswer("budget", false, T0, rules);
            session.Record.RecordBattle(true, 5, 1);

            var loaded = RoundTrip(session);

            Assert.AreEqual(2, loaded.Party[0].Level);
            Assert.AreEqual(5, loaded.Party[0].Exp);
            Assert.AreSame(nib, loaded.Party[0].Species);
            Assert.AreEqual(session.Party[1].CurrentHp, loaded.Party[1].CurrentHp);
            Assert.AreEqual(123, loaded.Inventory.Gold);
            Assert.AreEqual(2, loaded.Inventory.GetCount("shiny_ink"));

            Assert.AreEqual(2, loaded.Vocabulary.DiscoveredCount);
            var abandon = loaded.Vocabulary.Find("abandon");
            Assert.AreEqual(MasteryLevel.Learning, abandon.Level);
            Assert.AreEqual(T0 + rules.GetReviewInterval(MasteryLevel.Learning), abandon.NextReviewUtc, "복습 시각 유지");
            Assert.AreEqual(T0, abandon.DiscoveredUtc, "발견 시각 유지 (도감용)");
            Assert.IsTrue(loaded.Vocabulary.Find("budget").InWrongNote, "오답 노트 유지");

            Assert.AreEqual(1, loaded.Record.BattlesWon);
            Assert.AreEqual(5, loaded.Record.CorrectAnswers);
            Assert.AreEqual(0, loaded.LoadWarnings.Count);
        }

        [Test]
        public void LoadedVocabularyKeepsWorkingWithRules()
        {
            var session = GameSession.NewGame(new[] { nib }, 1);
            var rules = new MasteryRules();
            session.Vocabulary.RecordAnswer("abandon", true, T0, rules);

            var loaded = RoundTrip(session);
            var due = loaded.Vocabulary.Find("abandon").NextReviewUtc;
            var change = loaded.Vocabulary.RecordAnswer("abandon", true, due, rules);

            Assert.AreEqual(MasteryLevel.Reviewing, change.After);
            Assert.AreEqual(1, loaded.Vocabulary.Entries.Count, "불러온 뒤에도 같은 단어를 중복 기록하지 않음");
        }

        [Test]
        public void UnknownMonsterIsDroppedWithWarning()
        {
            var save = new SaveData(T0,
                new List<MonsterSaveData> { new MonsterSaveData("deleted_monster", 5, 0, 10), new MonsterSaveData("nib", 2, 0, 10) },
                new Inventory(), new VocabularyProgress(), new PlayerRecord());

            var loaded = GameSession.FromSaveData(save, database, new[] { shell }, 1);

            Assert.AreEqual(1, loaded.Party.Count);
            Assert.AreSame(nib, loaded.Party[0].Species);
            Assert.AreEqual(1, loaded.LoadWarnings.Count);
        }

        [Test]
        public void EmptyPartyFallsBackToStartersButKeepsWords()
        {
            var vocabulary = new VocabularyProgress();
            vocabulary.RecordAnswer("abandon", true, T0, new MasteryRules());
            var save = new SaveData(T0, new List<MonsterSaveData> { new MonsterSaveData("gone", 5, 0, 10) },
                new Inventory(), vocabulary, new PlayerRecord());

            var loaded = GameSession.FromSaveData(save, database, new[] { shell }, 4);

            Assert.AreSame(shell, loaded.Party[0].Species);
            Assert.AreEqual(4, loaded.Party[0].Level);
            Assert.AreEqual(1, loaded.Vocabulary.DiscoveredCount);
        }

        [Test]
        public void FaintedPartyIsRestoredOnLoad()
        {
            var session = GameSession.NewGame(new[] { nib, shell }, 1);
            foreach (var m in session.Party) m.TakeDamage(999);
            Assert.IsFalse(session.CanFight);

            var loaded = RoundTrip(session);

            Assert.IsTrue(loaded.Party.All(m => m.CurrentHp == m.Stats.MaxHp));
        }

        [Test]
        public void OldSaveMissingNewFieldsStillLoads()
        {
            // 이후 버전에서 필드가 늘어나도 예전 세이브(필드 없음)를 읽을 수 있어야 함
            var data = SaveData.FromJson("{\"version\":1,\"party\":[{\"speciesId\":\"nib\",\"level\":2,\"exp\":0,\"currentHp\":10}]}");
            var loaded = GameSession.FromSaveData(data, database, new[] { shell }, 1);

            Assert.AreSame(nib, loaded.Party[0].Species);
            Assert.AreEqual(0, loaded.Inventory.Gold);
            Assert.AreEqual(0, loaded.Vocabulary.DiscoveredCount);
        }
    }

    // 실제 프로젝트 데이터 점검: 새 몬스터를 만들고 GameDatabase 갱신을 잊으면 세이브에서 사라지므로 여기서 잡는다
    public class DataIntegrityTests
    {
        private static List<T> All<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).ToList();
        }

        [Test]
        public void GameDatabaseContainsEveryMonsterAndItemWithUniqueIds()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            Assert.IsNotNull(database, "Assets/Data/GameDatabase.asset 이 없습니다 (WordRPG > Data > Refresh Game Database)");

            foreach (var species in All<MonsterSpecies>())
            {
                Assert.IsFalse(string.IsNullOrEmpty(species.SpeciesId), $"{species.name}: speciesId 비어 있음");
                Assert.AreSame(species, database.FindMonster(species.SpeciesId),
                    $"{species.name}이 GameDatabase에 없거나 id가 중복됩니다 (WordRPG > Data > Refresh Game Database)");
            }
            foreach (var item in All<ItemData>())
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.ItemId), $"{item.name}: itemId 비어 있음");
                Assert.AreSame(item, database.FindItem(item.ItemId), $"{item.name}이 GameDatabase에 없거나 id가 중복됩니다");
            }
        }

        // Art/NinjaAdventure/Monsters/{speciesId}.png 를 넣고 연결을 잊으면 임시 도형으로 나오므로 여기서 잡는다
        [Test]
        public void MonsterArtFilesAreLinkedToTheirSpecies()
        {
            const string folder = "Assets/Resources/Art/NinjaAdventure/Monsters";
            var species = All<MonsterSpecies>();
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string id = System.IO.Path.GetFileNameWithoutExtension(path);
                var owner = species.Find(s => s.SpeciesId == id);
                Assert.IsNotNull(owner, $"{path}: '{id}' 몬스터가 없습니다 (파일 이름 = speciesId)");
                Assert.AreSame(AssetDatabase.LoadAssetAtPath<Sprite>(path), owner.Sprite,
                    $"{id} 그림이 연결되지 않았습니다 (WordRPG > Data > Link Monster Art)");
                count++;
            }
            Assert.Greater(count, 0, "몬스터 그림이 하나도 없습니다 (Tools/import_ninja_art.py)");
        }

        [Test]
        public void WordIdsAreUniqueAcrossAllWordBooks()
        {
            var seen = new Dictionary<string, string>();
            foreach (var book in All<WordDatabase>())
            {
                foreach (var word in book.Words)
                {
                    Assert.IsFalse(seen.ContainsKey(word.Id), $"단어 id '{word.Id}'가 여러 단어장({book.name} 등)에 중복");
                    seen[word.Id] = book.name;
                }
            }
            Assert.Greater(seen.Count, 0);
        }
    }
}
