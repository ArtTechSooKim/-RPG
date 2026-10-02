using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using WordRPG.Field;
using WordRPG.Heroes;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.Words;

namespace WordRPG.EditorTools
{
    // MVP용 샘플 데이터(주인공·성유물·스킬·아이템·적 몬스터·출현표·지역) 생성. 이미 있는 에셋은 건드리지 않으므로
    // 인스펙터에서 수치를 고친 뒤 다시 실행해도 안전하다. 배치모드: -executeMethod WordRPG.EditorTools.SampleDataBuilder.Build
    public static class SampleDataBuilder
    {
        private const string Root = "Assets/Data";

        public const string HeroPath = Root + "/Hero/hero.asset";

        [MenuItem("WordRPG/Data/Create Sample Data")]
        public static void Build()
        {
            // --- 아이템: 성유물 강화 재료 (전투·보물상자·보스에서 얻음. 상점에서는 팔지 않음) ---
            var shinyInk = Item("shiny_ink", "빛나는 잉크", "깃펜을 강화하는 반짝이는 잉크.", new Color(0.3f, 0.4f, 1f));
            var hardCover = Item("hard_cover", "단단한 표지", "백과사전·기억의 성배를 강화하는 두꺼운 가죽 표지.", new Color(0.55f, 0.35f, 0.2f));
            var sparkleDust = Item("sparkle_dust", "반짝 가루", "등불·마법 지팡이를 강화하는 빛나는 가루.", new Color(1f, 0.9f, 0.4f));

            // --- 상처약 (상점에서 판매, 필드·전투에서 사용) ---
            var potion = Item("potion", "상처약", "상처에 바르면 HP를 40 회복한다. 필드·전투에서 쓸 수 있다.",
                new Color(0.9f, 0.35f, 0.3f), ItemKind.Consumable, 40);
            var largePotion = Item("potion_large", "큰 상처약", "HP를 120 회복하는 진한 약. 필드·전투에서 쓸 수 있다.",
                new Color(0.85f, 0.2f, 0.3f), ItemKind.Consumable, 120);

            // --- 지역 도감 완성 징표 (지역 특색에 맞는 기념물) ---
            var meadowKeepsake = Item("keepsake_meadow", "네잎클로버 책갈피",
                "초원 도감을 완성한 증표. 행운을 부르는 네잎클로버가 곱게 눌려 있다.", new Color(0.35f, 0.8f, 0.35f), ItemKind.Keepsake);

            // --- 기술: 기본기는 쉬운 영→한, 성유물 기술은 대부분 어려운 한→영 ---
            var swing = Skill("hero_swing", "휘두르기", "들고 있는 사전으로 힘껏 휘두른다.", SkillKind.Damage, SkillTarget.SingleEnemy, 18, QuizDirection.EnglishToMeaning);
            var inkSplash = Skill("nib_ink_splash", "잉크 뿌리기", "적 전체에 잉크를 뿌린다.", SkillKind.Damage, SkillTarget.AllEnemies, 14, QuizDirection.MeaningToEnglish);
            var inkStorm = Skill("quill_ink_storm", "잉크 폭풍", "거센 잉크 폭풍으로 적 전체를 공격한다.", SkillKind.Damage, SkillTarget.AllEnemies, 22, QuizDirection.MeaningToEnglish);
            var bookShield = Skill("shell_book_shield", "책 방패", "두꺼운 책으로 몸을 가려 보호막을 친다.", SkillKind.Guard, SkillTarget.AllAllies, 4, QuizDirection.MeaningToEnglish);
            var encycloWall = Skill("tortoise_encyclo_wall", "백과 방벽", "백과사전을 펼쳐 단단한 방벽을 세운다.", SkillKind.Guard, SkillTarget.AllAllies, 10, QuizDirection.MeaningToEnglish);
            var healingLight = Skill("lumi_healing_light", "치유의 빛", "따뜻한 빛으로 HP를 회복한다.", SkillKind.Heal, SkillTarget.AllAllies, 8, QuizDirection.MeaningToEnglish);
            var wisdomLight = Skill("lantern_wisdom_light", "지혜의 빛", "지혜의 빛으로 HP를 크게 회복한다.", SkillKind.Heal, SkillTarget.AllAllies, 14, QuizDirection.MeaningToEnglish);
            var spellBolt = Skill("wand_spell_bolt", "철자 번개", "철자를 정확히 외우면 적 하나에게 번개가 떨어진다.", SkillKind.Damage, SkillTarget.SingleEnemy, 26, QuizDirection.MeaningToEnglish);
            var spellStorm = Skill("wand_thunder_spell", "낙뢰 주문", "긴 주문으로 적 하나에게 거대한 벼락을 떨어뜨린다.", SkillKind.Damage, SkillTarget.SingleEnemy, 34, QuizDirection.MeaningToEnglish);
            var recall = Skill("grail_recall", "되찾은 기억", "잊었던 기억을 되찾아 HP를 회복한다.", SkillKind.Heal, SkillTarget.Self, 16, QuizDirection.EnglishToMeaning);
            var blessing = Skill("grail_blessing", "기억의 축복", "되찾은 기억이 빛이 되어 HP를 크게 회복한다.", SkillKind.Heal, SkillTarget.Self, 26, QuizDirection.EnglishToMeaning);

            var splat = Skill("slime_splat", "끈적 공격", "끈적한 잉크를 튀긴다.", SkillKind.Damage, SkillTarget.SingleEnemy, 14, QuizDirection.EnglishToMeaning);
            var scratch = Skill("bat_scratch", "낙서 할퀴기", "삐뚤빼뚤한 발톱으로 할퀸다.", SkillKind.Damage, SkillTarget.SingleEnemy, 16, QuizDirection.EnglishToMeaning);
            var forgetFog = Skill("goblin_forget_fog", "망각의 안개", "기억을 흐리는 안개로 주인공을 공격한다.", SkillKind.Damage, SkillTarget.AllEnemies, 10, QuizDirection.EnglishToMeaning);
            var memoryDrain = Skill("boss_memory_drain", "기억 흡수", "빼앗은 기억으로 자신의 HP를 회복한다.", SkillKind.Heal, SkillTarget.Self, 8, QuizDirection.EnglishToMeaning);
            var blankBonk = Skill("goblin_blank_bonk", "깜빡 방망이", "머리를 하얗게 만드는 방망이질.", SkillKind.Damage, SkillTarget.SingleEnemy, 18, QuizDirection.EnglishToMeaning);

            // --- 성유물 (예전 아군 몬스터 3마리 → 깃펜·백과사전·등불, 그리고 새로 찾는 마법 지팡이·기억의 성배) ---
            var quill = Relic("relic_quill", "깃펜", "펜촉이가 남긴 깃펜. 잉크를 뿌려 적을 한꺼번에 공격한다.", MonsterRole.Attacker,
                new Color(0.25f, 0.45f, 0.95f), inkSplash, inkStorm, new MonsterStats(0, 4, 0), new MonsterStats(0, 2, 0), shinyInk);
            var book = Relic("relic_book", "백과사전", "책껍질이 지고 다니던 두꺼운 사전. 펼치면 단단한 방패가 된다.", MonsterRole.Defender,
                new Color(0.6f, 0.4f, 0.2f), bookShield, encycloWall, new MonsterStats(10, 0, 4), new MonsterStats(4, 0, 1), hardCover);
            var lantern = Relic("relic_lantern", "등불", "등불이가 남긴 작은 등불. 따뜻한 빛으로 상처를 감싼다.", MonsterRole.Supporter,
                new Color(1f, 0.85f, 0.3f), healingLight, wisdomLight, new MonsterStats(15, 0, 0), new MonsterStats(5, 0, 0), sparkleDust);
            var wand = Relic("relic_wand", "마법 지팡이", "철자를 정확히 외우면 번개가 떨어지는 지팡이. 서고 깊은 곳에 잠들어 있었다.", MonsterRole.Attacker,
                new Color(0.55f, 0.45f, 0.95f), spellBolt, spellStorm, new MonsterStats(0, 6, 0), new MonsterStats(0, 2, 0), sparkleDust);
            var grail = Relic("relic_grail", "기억의 성배", "까먹대왕이 삼켰던 기억이 담긴 잔. 잊었던 힘을 되찾아 준다.", MonsterRole.Supporter,
                new Color(1f, 0.75f, 0.2f), recall, blessing, new MonsterStats(20, 0, 2), new MonsterStats(5, 0, 1), hardCover);

            // --- 주인공: 혼자 싸운다. 시작 성유물은 깃펜 하나 ---
            HeroAsset(swing, quill);

            // --- 적 몬스터 ---
            var inkSlime = Monster("ink_slime", "잉크 슬라임", "쏟아진 잉크가 뭉쳐 생긴 슬라임.", MonsterRole.Attacker,
                new Color(0.35f, 0.2f, 0.5f), new MonsterStats(22, 9, 8), new MonsterStats(5, 2, 2), new[] { splat },
                enemyExp: 6, enemyGold: 5, drops: new[] { new ItemDrop(shinyInk, 0.4f), new ItemDrop(hardCover, 0.15f) });
            var scribbleBat = Monster("scribble_bat", "낙서 박쥐", "공책 귀퉁이 낙서에서 태어난 박쥐.", MonsterRole.Attacker,
                new Color(0.5f, 0.5f, 0.55f), new MonsterStats(18, 11, 6), new MonsterStats(4, 2, 1), new[] { scratch },
                enemyExp: 7, enemyGold: 6, drops: new[] { new ItemDrop(sparkleDust, 0.4f), new ItemDrop(shinyInk, 0.15f) });
            var forgetGoblin = Monster("forget_goblin", "까먹깨비", "외운 단어를 까먹게 만드는 도깨비. 던전 깊은 곳에 산다.", MonsterRole.Defender,
                new Color(0.2f, 0.7f, 0.6f), new MonsterStats(40, 12, 10), new MonsterStats(8, 3, 2), new[] { forgetFog, blankBonk },
                enemyExp: 15, enemyGold: 20, drops: new[] { new ItemDrop(hardCover, 0.6f), new ItemDrop(sparkleDust, 0.3f) });

            // --- 보스: 잊혀진 서고 꼭대기의 까먹대왕. 강화 재료 3종 + 성유물 '기억의 성배' ---
            var forgetKing = Monster("boss_forget_king", "까먹대왕", "까먹깨비들의 우두머리. 서고의 기억을 몽땅 먹어 치우고 있다.", MonsterRole.Attacker,
                new Color(0.15f, 0.6f, 0.55f), new MonsterStats(50, 14, 12), new MonsterStats(8, 3, 2), new[] { forgetFog, blankBonk, memoryDrain },
                enemyExp: 25, enemyGold: 30,
                drops: new[] { new ItemDrop(shinyInk, 1f), new ItemDrop(hardCover, 1f), new ItemDrop(sparkleDust, 1f) });

            // --- 출현표 ---
            var meadowEncounters = Encounters("meadow_field", 1, 2,
                new EncounterTable.Entry(inkSlime, 1, 3, 10),
                new EncounterTable.Entry(scribbleBat, 1, 3, 8));
            var dungeonEncounters = Encounters("word_dungeon", 1, 3,
                new EncounterTable.Entry(forgetGoblin, 3, 5, 5),
                new EncounterTable.Entry(inkSlime, 2, 4, 5),
                new EncounterTable.Entry(scribbleBat, 2, 4, 5));

            // 단어장(CSV에서 만들어짐)에 지역 정보와 도감 완성 보상 지정. 이미 지정돼 있으면 건드리지 않음
            WordCsvImporter.ImportAll();
            ConfigureRegion("Assets/Data/Words/tier1_meadow.asset", "meadow", "초원", meadowKeepsake, 500);

            // --- 필드: 초원. 보물상자는 맵의 C를 위→아래, 왼→오른 순으로 대응 (강화 재료·성유물을 얻는 곳) ---
            Area("meadow", "초원", MeadowMap, meadowEncounters,
                AssetDatabase.LoadAssetAtPath<WordDatabase>("Assets/Data/Words/tier1_meadow.asset"),
                new ChestContent(hardCover, 2),          // 오른쪽 위 구석
                new ChestContent(shinyInk, 2),           // 왼쪽 위 방
                new ChestContent(sparkleDust, 2),        // 가운데 풀밭
                new ChestContent(null, 0, 50, book));    // 마을 위 풀밭 — 두 번째 성유물

            // --- 마을 상점: 상처약 (강화 재료는 팔지 않음 — 사용자 결정, 진화가 너무 쉬워서) ---
            var meadowShop = ShopAsset("meadow_shop", "초원 마을 잡화점",
                new ShopEntry(potion, 30), new ShopEntry(largePotion, 90));
            AssignShopIfEmpty("Assets/Data/Areas/meadow.asset", meadowShop);

            // --- 던전: 잊혀진 서고 (초원 북쪽 동굴 입구로 연결). MVP는 난이도 하나라 단어장은 초원과 같음 ---
            Area("library", "잊혀진 서고", LibraryMap, dungeonEncounters,
                AssetDatabase.LoadAssetAtPath<WordDatabase>("Assets/Data/Words/tier1_meadow.asset"),
                new ChestContent(null, 0, 100, lantern), // 왼쪽 위 열람실
                new ChestContent(null, 0, 0, wand));     // 오른쪽 위 열람실
            ConfigureAreaIfNew("Assets/Data/Areas/library.asset", so =>
            {
                Prop(so, "theme").enumValueIndex = (int)FieldTheme.Library;
                Prop(so, "encounterRate").floatValue = 0.14f;
                var boss = Prop(so, "boss");
                boss.FindPropertyRelative("species").objectReferenceValue = forgetKing;
                boss.FindPropertyRelative("level").intValue = 7;
                boss.FindPropertyRelative("rewardRelic").objectReferenceValue = grail;
            });
            LinkDoorsIfEmpty("Assets/Data/Areas/meadow.asset", "Assets/Data/Areas/library.asset");

            AssetDatabase.SaveAssets();
            GameDatabaseBuilder.Refresh();
            Debug.Log("[WordRPG] 샘플 데이터 생성 완료 (기존 에셋은 유지)");
        }

        // . 길  , 풀숲(조우)  # 나무  ~ 물  F 회복의 샘  C 보물상자  P 시작 위치 (아래쪽 마을에서 출발해 북쪽으로 탐험)
        private const string MeadowMap =
                "###########D#######\n" +
                "#,,,,,,#,,,,,,,,,C#\n" +
                "#,,C,,,#,,,,~~~,,,#\n" +
                "#,,,,,,.,,,,~~~,,,#\n" +
                "#,,,,,,#,,,,,,,,,,#\n" +
                "###.#######.#######\n" +
                "#.....,,,,,.......#\n" +
                "#.~~~.,,,,,.,,,,,.#\n" +
                "#.~~~.,,,,,.,,C,,.#\n" +
                "#.....,,,,,.,,,,,.#\n" +
                "#####.......#######\n" +
                "#,,,,,,,.,,,,,,,,,#\n" +
                "#,,,,,,,.,,,,,,,,,#\n" +
                "#,,,##,,.,,##,,,,,#\n" +
                "#,,,##,,.,,##,,,C,#\n" +
                "#,,,,,,,.,,,,,,,,,#\n" +
                "########.##########\n" +
                "#.................#\n" +
                "#.#.#.E.F..S..#.#.#\n" +
                "#.................#\n" +
                "#.#.#...P.....#.#.#\n" +
                "#.................#\n" +
                "###################\n";

        private static void Area(string id, string name, string map, EncounterTable encounters, WordDatabase words,
            params ChestContent[] chests)
        {
            CreateIfMissing<FieldArea>($"{Root}/Areas/{id}.asset", so =>
            {
                Prop(so, "areaId").stringValue = id;
                Prop(so, "displayName").stringValue = name;
                Prop(so, "map").stringValue = map;
                Prop(so, "encounters").objectReferenceValue = encounters;
                Prop(so, "words").objectReferenceValue = words;
                var list = Prop(so, "chests");
                list.arraySize = chests.Length;
                for (int i = 0; i < chests.Length; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("item").objectReferenceValue = chests[i].Item;
                    element.FindPropertyRelative("count").intValue = chests[i].Count;
                    element.FindPropertyRelative("gold").intValue = chests[i].Gold;
                    element.FindPropertyRelative("relic").objectReferenceValue = chests[i].Relic;
                }
            });
        }

        private static ShopData ShopAsset(string id, string name, params ShopEntry[] entries)
        {
            return CreateIfMissing<ShopData>($"{Root}/Shops/{id}.asset", so =>
            {
                Prop(so, "displayName").stringValue = name;
                var list = Prop(so, "entries");
                list.arraySize = entries.Length;
                for (int i = 0; i < entries.Length; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("item").objectReferenceValue = entries[i].Item;
                    element.FindPropertyRelative("price").intValue = entries[i].Price;
                }
            });
        }

        // 이미 있는 지역 에셋에 상점이 비어 있을 때만 연결 (인스펙터에서 바꾼 값은 유지)
        private static void AssignShopIfEmpty(string areaPath, ShopData shop)
        {
            var area = AssetDatabase.LoadAssetAtPath<FieldArea>(areaPath);
            if (area == null || area.Shop != null) return;
            var so = new SerializedObject(area);
            Prop(so, "shop").objectReferenceValue = shop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // . 길  , 흩어진 책장(조우)  # 책장  ~ 잉크 웅덩이  F 회복의 샘  C 보물상자  B 보스  D 출입구(초원으로)  P 시작 위치
        // 아래 출입구에서 들어와 위로: 책장 미로 → 샘 → 큰 열람실(상자 2개) → 꼭대기 보스 방
        private const string LibraryMap =
                "#################\n" +
                "######..B..######\n" +
                "######.....######\n" +
                "########.########\n" +
                "#C,,,,,,.,,,,,,C#\n" +
                "#,,~~~,,.,,~~~,,#\n" +
                "#,,~~~,,.,,~~~,,#\n" +
                "#,,,,,,,.,,,,,,,#\n" +
                "########.########\n" +
                "#...F...........#\n" +
                "#.#####.#.#####.#\n" +
                "#.#,,,,,,,,,,,#.#\n" +
                "#.#,#########,#.#\n" +
                "#.#,,,,,,,,,,,#.#\n" +
                "#.#####,#,#####.#\n" +
                "#...,,,,,,,,,...#\n" +
                "#...,,,,,,,,,...#\n" +
                "########.########\n" +
                "#,,,,,,,.,,,,,,,#\n" +
                "#,,#,,#,.,#,,#,,#\n" +
                "#,,,,,,,.,,,,,,,#\n" +
                "#######...#######\n" +
                "#######.P.#######\n" +
                "########D########\n";

        // 새로 만든 지역에만 추가 설정 (보스가 아직 없을 때 = 처음 만들 때). 인스펙터에서 바꾼 값은 유지
        private static void ConfigureAreaIfNew(string areaPath, Action<SerializedObject> configure)
        {
            var area = AssetDatabase.LoadAssetAtPath<FieldArea>(areaPath);
            if (area == null || area.Boss != null) return;
            var so = new SerializedObject(area);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 두 지역의 첫 번째 출입구(D)를 서로 연결. 출입구 설정이 비어 있을 때만
        private static void LinkDoorsIfEmpty(string pathA, string pathB)
        {
            var a = AssetDatabase.LoadAssetAtPath<FieldArea>(pathA);
            var b = AssetDatabase.LoadAssetAtPath<FieldArea>(pathB);
            if (a == null || b == null) return;
            SetFirstExit(a, b);
            SetFirstExit(b, a);
        }

        private static void SetFirstExit(FieldArea from, FieldArea to)
        {
            if (from.Exits.Count > 0) return;
            var so = new SerializedObject(from);
            var exits = Prop(so, "exits");
            exits.arraySize = 1;
            exits.GetArrayElementAtIndex(0).FindPropertyRelative("target").objectReferenceValue = to;
            exits.GetArrayElementAtIndex(0).FindPropertyRelative("targetDoorIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRegion(string wordBookPath, string regionId, string regionName, ItemData keepsake, int gold)
        {
            var book = AssetDatabase.LoadAssetAtPath<WordDatabase>(wordBookPath);
            if (book == null)
            {
                Debug.LogWarning($"[WordRPG] 단어장이 없어 지역 설정을 건너뜀: {wordBookPath}");
                return;
            }
            if (!string.IsNullOrEmpty(book.RegionId)) return;

            var so = new SerializedObject(book);
            Prop(so, "regionId").stringValue = regionId;
            Prop(so, "regionName").stringValue = regionName;
            Prop(so, "completionKeepsake").objectReferenceValue = keepsake;
            Prop(so, "completionGold").intValue = gold;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemData Item(string id, string name, string description, Color color,
            ItemKind kind = ItemKind.Material, int healAmount = 0)
        {
            return CreateIfMissing<ItemData>($"{Root}/Items/{id}.asset", so =>
            {
                Prop(so, "itemId").stringValue = id;
                Prop(so, "displayName").stringValue = name;
                Prop(so, "description").stringValue = description;
                Prop(so, "kind").enumValueIndex = (int)kind;
                Prop(so, "placeholderColor").colorValue = color;
                Prop(so, "healAmount").intValue = healAmount;
            });
        }

        // 강화 비용 (+0→+1 … +4→+5): 재료 개수 · 골드
        private static readonly (int items, int gold)[] UpgradeCosts = { (1, 30), (2, 60), (2, 90), (3, 120), (3, 150) };

        private static RelicData Relic(string id, string name, string description, MonsterRole role, Color color,
            SkillData skill, SkillData awakened, MonsterStats baseBonus, MonsterStats perLevel, ItemData material)
        {
            return CreateIfMissing<RelicData>($"{Root}/Relics/{id}.asset", so =>
            {
                Prop(so, "relicId").stringValue = id;
                Prop(so, "displayName").stringValue = name;
                Prop(so, "description").stringValue = description;
                Prop(so, "role").enumValueIndex = (int)role;
                Prop(so, "placeholderColor").colorValue = color;
                Prop(so, "skill").objectReferenceValue = skill;
                Prop(so, "awakenedSkill").objectReferenceValue = awakened;
                Prop(so, "awakenLevel").intValue = 3;
                SetStats(Prop(so, "baseBonus"), baseBonus);
                SetStats(Prop(so, "bonusPerLevel"), perLevel);
                Prop(so, "upgradeItem").objectReferenceValue = material;
                var costs = Prop(so, "upgradeCosts");
                costs.arraySize = UpgradeCosts.Length;
                for (int i = 0; i < UpgradeCosts.Length; i++)
                {
                    costs.GetArrayElementAtIndex(i).FindPropertyRelative("itemCount").intValue = UpgradeCosts[i].items;
                    costs.GetArrayElementAtIndex(i).FindPropertyRelative("gold").intValue = UpgradeCosts[i].gold;
                }
            });
        }

        private static HeroData HeroAsset(SkillData basicSkill, params RelicData[] startingRelics)
        {
            return CreateIfMissing<HeroData>(HeroPath, so =>
            {
                Prop(so, "displayName").stringValue = "주인공";
                Prop(so, "description").stringValue = "단어의 힘을 성유물에 담아 싸우는 견습 모험가.";
                SetStats(Prop(so, "baseStats"), new MonsterStats(60, 14, 10));
                SetStats(Prop(so, "growthPerLevel"), new MonsterStats(8, 3, 2));
                Prop(so, "startLevel").intValue = 3;
                Prop(so, "basicSkill").objectReferenceValue = basicSkill;
                var list = Prop(so, "startingRelics");
                list.arraySize = startingRelics.Length;
                for (int i = 0; i < startingRelics.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = startingRelics[i];
            });
        }

        private static SkillData Skill(string id, string name, string description, SkillKind kind, SkillTarget target,
            int power, QuizDirection direction)
        {
            return CreateIfMissing<SkillData>($"{Root}/Skills/{id}.asset", so =>
            {
                Prop(so, "skillId").stringValue = id;
                Prop(so, "displayName").stringValue = name;
                Prop(so, "description").stringValue = description;
                Prop(so, "kind").enumValueIndex = (int)kind;
                Prop(so, "target").enumValueIndex = (int)target;
                Prop(so, "power").intValue = power;
                Prop(so, "quizDirection").enumValueIndex = (int)direction;
            });
        }

        private static MonsterSpecies Monster(string id, string name, string description, MonsterRole role, Color color,
            MonsterStats baseStats, MonsterStats growth, SkillData[] skills,
            int enemyExp = 5, int enemyGold = 5, ItemDrop[] drops = null)
        {
            return CreateIfMissing<MonsterSpecies>($"{Root}/Monsters/{id}.asset", so =>
            {
                Prop(so, "speciesId").stringValue = id;
                Prop(so, "displayName").stringValue = name;
                Prop(so, "description").stringValue = description;
                Prop(so, "role").enumValueIndex = (int)role;
                Prop(so, "placeholderColor").colorValue = color;
                SetStats(Prop(so, "baseStats"), baseStats);
                SetStats(Prop(so, "growthPerLevel"), growth);

                var skillList = Prop(so, "skills");
                skillList.arraySize = skills.Length;
                for (int i = 0; i < skills.Length; i++) skillList.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];

                Prop(so, "expReward").intValue = enemyExp;
                Prop(so, "goldReward").intValue = enemyGold;
                var dropList = Prop(so, "drops");
                dropList.arraySize = drops?.Length ?? 0;
                for (int i = 0; i < dropList.arraySize; i++)
                {
                    var element = dropList.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("item").objectReferenceValue = drops[i].Item;
                    element.FindPropertyRelative("chance").floatValue = drops[i].Chance;
                    element.FindPropertyRelative("count").intValue = drops[i].Count;
                }
            });
        }

        private static EncounterTable Encounters(string id, int minGroup, int maxGroup, params EncounterTable.Entry[] entries)
        {
            return CreateIfMissing<EncounterTable>($"{Root}/Encounters/{id}.asset", so =>
            {
                Prop(so, "minGroupSize").intValue = minGroup;
                Prop(so, "maxGroupSize").intValue = maxGroup;
                var list = Prop(so, "entries");
                list.arraySize = entries.Length;
                for (int i = 0; i < entries.Length; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("species").objectReferenceValue = entries[i].Species;
                    element.FindPropertyRelative("minLevel").intValue = entries[i].MinLevel;
                    element.FindPropertyRelative("maxLevel").intValue = entries[i].MaxLevel;
                    element.FindPropertyRelative("weight").intValue = entries[i].Weight;
                }
            });
        }

        private static void SetStats(SerializedProperty property, MonsterStats stats)
        {
            property.FindPropertyRelative("maxHp").intValue = stats.MaxHp;
            property.FindPropertyRelative("attack").intValue = stats.Attack;
            property.FindPropertyRelative("defense").intValue = stats.Defense;
        }

        private static T CreateIfMissing<T>(string path, Action<SerializedObject> fill) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            var so = new SerializedObject(asset);
            fill(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static SerializedProperty Prop(SerializedObject so, string name)
        {
            return so.FindProperty(name) ?? throw new ArgumentException($"{so.targetObject.GetType().Name}에 '{name}' 필드가 없습니다");
        }
    }
}
