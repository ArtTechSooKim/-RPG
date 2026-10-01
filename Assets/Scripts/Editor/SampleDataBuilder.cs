using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using WordRPG.Field;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.Words;

namespace WordRPG.EditorTools
{
    // MVP용 샘플 데이터(스킬·아이템·몬스터·출현표) 생성. 이미 있는 에셋은 건드리지 않으므로
    // 인스펙터에서 수치를 고친 뒤 다시 실행해도 안전하다. 배치모드: -executeMethod WordRPG.EditorTools.SampleDataBuilder.Build
    public static class SampleDataBuilder
    {
        private const string Root = "Assets/Data";

        [MenuItem("WordRPG/Data/Create Sample Data")]
        public static void Build()
        {
            // --- 아이템 (진화 재료) ---
            var shinyInk = Item("shiny_ink", "빛나는 잉크", "펜촉이를 진화시키는 반짝이는 잉크.", new Color(0.3f, 0.4f, 1f));
            var hardCover = Item("hard_cover", "단단한 표지", "책껍질을 진화시키는 두꺼운 가죽 표지.", new Color(0.55f, 0.35f, 0.2f));
            var sparkleDust = Item("sparkle_dust", "반짝 가루", "등불이를 진화시키는 빛나는 가루.", new Color(1f, 0.9f, 0.4f));

            // --- 지역 도감 완성 징표 (지역 특색에 맞는 기념물) ---
            var meadowKeepsake = Item("keepsake_meadow", "네잎클로버 책갈피",
                "초원 도감을 완성한 증표. 행운을 부르는 네잎클로버가 곱게 눌려 있다.", new Color(0.35f, 0.8f, 0.35f), ItemKind.Keepsake);

            // --- 스킬: 기본기는 쉬운 영→한, 필살기는 어려운 한→영 ---
            var poke = Skill("nib_poke", "찌르기", "펜촉으로 콕 찌른다.", SkillKind.Damage, SkillTarget.SingleEnemy, 20, QuizDirection.EnglishToMeaning);
            var inkSplash = Skill("nib_ink_splash", "잉크 뿌리기", "적 전체에 잉크를 뿌린다.", SkillKind.Damage, SkillTarget.AllEnemies, 14, QuizDirection.MeaningToEnglish);
            var inkStorm = Skill("quill_ink_storm", "잉크 폭풍", "거센 잉크 폭풍으로 적 전체를 공격한다.", SkillKind.Damage, SkillTarget.AllEnemies, 22, QuizDirection.MeaningToEnglish);

            var tackle = Skill("shell_tackle", "몸통박치기", "단단한 몸으로 부딪친다.", SkillKind.Damage, SkillTarget.SingleEnemy, 16, QuizDirection.EnglishToMeaning);
            var bookShield = Skill("shell_book_shield", "책 방패", "아군 전체에 보호막을 친다.", SkillKind.Guard, SkillTarget.AllAllies, 4, QuizDirection.MeaningToEnglish);
            var encycloWall = Skill("tortoise_encyclo_wall", "백과 방벽", "두꺼운 백과사전으로 아군 전체를 지킨다.", SkillKind.Guard, SkillTarget.AllAllies, 10, QuizDirection.MeaningToEnglish);

            var lightOrb = Skill("lumi_light_orb", "빛 구슬", "작은 빛 구슬을 던진다.", SkillKind.Damage, SkillTarget.SingleEnemy, 14, QuizDirection.EnglishToMeaning);
            var healingLight = Skill("lumi_healing_light", "치유의 빛", "아군 전체의 HP를 회복한다.", SkillKind.Heal, SkillTarget.AllAllies, 8, QuizDirection.MeaningToEnglish);
            var wisdomLight = Skill("lantern_wisdom_light", "지혜의 빛", "따뜻한 지혜의 빛으로 아군 전체를 크게 회복한다.", SkillKind.Heal, SkillTarget.AllAllies, 14, QuizDirection.MeaningToEnglish);

            var splat = Skill("slime_splat", "끈적 공격", "끈적한 잉크를 튀긴다.", SkillKind.Damage, SkillTarget.SingleEnemy, 14, QuizDirection.EnglishToMeaning);
            var scratch = Skill("bat_scratch", "낙서 할퀴기", "삐뚤빼뚤한 발톱으로 할퀸다.", SkillKind.Damage, SkillTarget.SingleEnemy, 16, QuizDirection.EnglishToMeaning);
            var forgetFog = Skill("goblin_forget_fog", "망각의 안개", "기억을 흐리는 안개로 파티 전체를 공격한다.", SkillKind.Damage, SkillTarget.AllEnemies, 10, QuizDirection.EnglishToMeaning);
            var blankBonk = Skill("goblin_blank_bonk", "깜빡 방망이", "머리를 하얗게 만드는 방망이질.", SkillKind.Damage, SkillTarget.SingleEnemy, 18, QuizDirection.EnglishToMeaning);

            // --- 아군 몬스터 (진화형 먼저 만들어야 참조 가능) ---
            var quillKnight = Monster("quill_knight", "깃펜기사", "펜촉이가 진화한 모습. 깃펜을 검처럼 휘두른다.", MonsterRole.Attacker,
                new Color(0.15f, 0.25f, 0.7f), new MonsterStats(38, 19, 10), new MonsterStats(5, 4, 2), new[] { poke, inkStorm });
            var nib = Monster("nib", "펜촉이", "작은 펜촉 몬스터. 날카로운 끝으로 적을 찌른다.", MonsterRole.Attacker,
                new Color(0.25f, 0.45f, 0.95f), new MonsterStats(30, 14, 8), new MonsterStats(4, 3, 1), new[] { poke, inkSplash },
                quillKnight, 5, shinyInk, 3);

            var tortoise = Monster("encyclotortoise", "백과거북", "책껍질이 진화한 모습. 등껍질이 백과사전만큼 두껍다.", MonsterRole.Defender,
                new Color(0.4f, 0.25f, 0.1f), new MonsterStats(50, 11, 18), new MonsterStats(7, 2, 4), new[] { tackle, encycloWall });
            var bookshell = Monster("bookshell", "책껍질", "책을 등껍질 삼아 지고 다니는 거북. 동료를 지킨다.", MonsterRole.Defender,
                new Color(0.6f, 0.4f, 0.2f), new MonsterStats(40, 8, 14), new MonsterStats(6, 1, 3), new[] { tackle, bookShield },
                tortoise, 5, hardCover, 3);

            var sageLantern = Monster("sage_lantern", "지혜등불", "등불이가 진화한 모습. 지혜의 빛으로 동료를 치유한다.", MonsterRole.Supporter,
                new Color(1f, 0.7f, 0.1f), new MonsterStats(34, 14, 12), new MonsterStats(5, 3, 2), new[] { lightOrb, wisdomLight });
            var lumi = Monster("lumi", "등불이", "작은 등불 몬스터. 따뜻한 빛으로 동료를 돌본다.", MonsterRole.Supporter,
                new Color(1f, 0.85f, 0.3f), new MonsterStats(28, 10, 9), new MonsterStats(4, 2, 2), new[] { lightOrb, healingLight },
                sageLantern, 5, sparkleDust, 3);

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

            // --- 출현표 ---
            var meadowEncounters = Encounters("meadow_field", 1, 2,
                new EncounterTable.Entry(inkSlime, 1, 3, 10),
                new EncounterTable.Entry(scribbleBat, 1, 3, 8));
            Encounters("word_dungeon", 1, 3,
                new EncounterTable.Entry(forgetGoblin, 3, 5, 5),
                new EncounterTable.Entry(inkSlime, 2, 4, 5),
                new EncounterTable.Entry(scribbleBat, 2, 4, 5));

            // 단어장(CSV에서 만들어짐)에 지역 정보와 도감 완성 보상 지정. 이미 지정돼 있으면 건드리지 않음
            WordCsvImporter.ImportAll();
            ConfigureRegion("Assets/Data/Words/tier1_meadow.asset", "meadow", "초원", meadowKeepsake, 500);

            // --- 필드: 초원. 보물상자는 맵의 C를 위→아래, 왼→오른 순으로 대응 (진화 재료를 얻는 도감 외 경로) ---
            Area("meadow", "초원", MeadowMap, meadowEncounters,
                AssetDatabase.LoadAssetAtPath<WordDatabase>("Assets/Data/Words/tier1_meadow.asset"),
                new ChestContent(hardCover, 2),   // 오른쪽 위 구석
                new ChestContent(shinyInk, 2),    // 왼쪽 위 방
                new ChestContent(sparkleDust, 2), // 가운데 풀밭
                new ChestContent(null, 0, 100));  // 마을 위 풀밭

            AssetDatabase.SaveAssets();
            GameDatabaseBuilder.Refresh();
            Debug.Log("[WordRPG] 샘플 데이터 생성 완료 (기존 에셋은 유지)");
        }

        // . 길  , 풀숲(조우)  # 나무  ~ 물  F 회복의 샘  C 보물상자  P 시작 위치 (아래쪽 마을에서 출발해 북쪽으로 탐험)
        private const string MeadowMap =
                "###################\n" +
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
                "#.#.#...F.....#.#.#\n" +
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
                }
            });
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
            ItemKind kind = ItemKind.EvolutionMaterial)
        {
            return CreateIfMissing<ItemData>($"{Root}/Items/{id}.asset", so =>
            {
                Prop(so, "itemId").stringValue = id;
                Prop(so, "displayName").stringValue = name;
                Prop(so, "description").stringValue = description;
                Prop(so, "kind").enumValueIndex = (int)kind;
                Prop(so, "placeholderColor").colorValue = color;
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
            MonsterSpecies evolvesTo = null, int evolveLevel = 5, ItemData evolveItem = null, int evolveItemCount = 3,
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

                Prop(so, "evolvesTo").objectReferenceValue = evolvesTo;
                Prop(so, "evolveLevel").intValue = evolveLevel;
                Prop(so, "evolveItem").objectReferenceValue = evolveItem;
                Prop(so, "evolveItemCount").intValue = evolveItemCount;

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
