using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WordRPG.Battle;
using WordRPG.Field;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.Words;

namespace WordRPG.UI
{
    // 세로 화면 전투 UI. 모든 규칙은 BattleEngine이 처리하고, 여기서는 입력을 넘기고 돌려받은 BattleEvent를 연출만 한다.
    // UI는 전부 코드로 만든다 (아트 전 플레이스홀더). 씬에는 이 컴포넌트 하나와 카메라만 있으면 된다
    public class BattleScreen : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private MonsterSpecies[] partySpecies;
        [SerializeField] private int partyLevel = 3;
        [SerializeField] private EncounterTable encounter;
        [SerializeField] private WordDatabase words;

        [Header("밸런스")]
        [SerializeField] private BattleConfig battleConfig = new BattleConfig();
        [SerializeField] private MasteryRules masteryRules = new MasteryRules();

        [Header("연출")]
        [Tooltip("연출 대기 시간 배율. 테스트에서는 아주 작게")]
        [SerializeField] private float animationScale = 1f;

        // --- 런타임 상태 ---
        private System.Random rng;
        private VocabularyProgress vocabulary;
        private WordQuizService quizService;
        private Inventory inventory;
        private List<MonsterInstance> party;
        private BattleEngine engine;
        private bool started;

        private Canvas canvas;
        private RectTransform root;
        private RectTransform enemyArea;
        private UnitView[] partyViews;
        private readonly List<UnitView> enemyViews = new List<UnitView>();
        private readonly Dictionary<BattleUnit, UnitView> viewOf = new Dictionary<BattleUnit, UnitView>();
        private readonly Dictionary<BattleUnit, (int hp, int shield)> shown = new Dictionary<BattleUnit, (int, int)>();
        private readonly List<string> logLines = new List<string>();

        private Text roundLabel, vocabLabel, logLabel;
        private GameObject skillPanel, quizPanel, cardPanel, resultPanel;

        private Text skillTitle;
        private readonly List<Button> skillButtons = new List<Button>();
        private Button cancelButton;

        private Text quizPrompt, quizHint;
        private RectTransform timerFill;
        private Image timerFillImage;
        private readonly List<Button> choiceButtons = new List<Button>();

        private Text cardTitle, cardWord, cardMeaning, cardExtra, cardConfirmLabel;
        private Button cardConfirm;

        private Text resultTitle, resultBody;
        private Button resultPrimary, resultSecondary;

        // --- 입력 대기용 ---
        private SkillData pickedSkill;
        private BattleUnit pickedTarget;
        private SkillData targetingSkill;
        private int pickedChoice = int.MinValue;
        private bool cardConfirmed;
        private int resultChoice = -1;

        public BattleEngine Engine => engine;
        public bool IsResultVisible => resultPanel != null && resultPanel.activeSelf;
        public string ResultTitle => resultTitle != null ? resultTitle.text : "";
        public IReadOnlyList<MonsterInstance> Party => party;
        public VocabularyProgress Vocabulary => vocabulary;

        // 코드로 만든 BattleScreen(테스트 등)이 Start 전에 데이터를 넣는 용도
        public void Configure(MonsterSpecies[] species, int level, EncounterTable table, WordDatabase database,
            float animScale = 1f, BattleConfig config = null)
        {
            partySpecies = species;
            partyLevel = level;
            encounter = table;
            words = database;
            animationScale = animScale;
            if (config != null) battleConfig = config;
        }

        private void Start()
        {
            if (started) return;
            started = true;

            if (partySpecies == null || partySpecies.Length == 0 || encounter == null || words == null)
            {
                Debug.LogError("[BattleScreen] partySpecies / encounter / words 가 비어 있습니다");
                return;
            }

            rng = new System.Random();
            vocabulary = new VocabularyProgress();
            inventory = new Inventory();
            quizService = new WordQuizService(words.Words, vocabulary, masteryRules, rng);
            party = new List<MonsterInstance>();
            foreach (var species in partySpecies)
            {
                if (species != null && party.Count < 3) party.Add(new MonsterInstance(species, partyLevel));
            }

            BuildUi();
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ 흐름

        private IEnumerator MainLoop()
        {
            while (true)
            {
                StartNewBattle();
                yield return PlayBattle();
                yield return ShowResult();
            }
        }

        private void StartNewBattle()
        {
            var enemies = encounter.Roll(rng);
            engine = new BattleEngine(party, enemies, quizService, battleConfig, rng);

            foreach (var view in enemyViews) Destroy(view.Root.gameObject);
            enemyViews.Clear();
            viewOf.Clear();

            int n = engine.Enemies.Count;
            float start = (1f - n / 3f) / 2f;
            for (int i = 0; i < n; i++)
            {
                var view = UnitView.Create(enemyArea, $"Enemy_{i}", start + i / 3f, 0, start + (i + 1) / 3f, 1);
                view.Bind(engine.Enemies[i]);
                enemyViews.Add(view);
                viewOf[engine.Enemies[i]] = view;
            }
            for (int i = 0; i < partyViews.Length; i++)
            {
                if (i < engine.Party.Count)
                {
                    partyViews[i].Root.gameObject.SetActive(true);
                    partyViews[i].Bind(engine.Party[i]);
                    viewOf[engine.Party[i]] = partyViews[i];
                }
                else
                {
                    partyViews[i].Root.gameObject.SetActive(false);
                }
            }

            logLines.Clear();
            var names = new StringBuilder();
            foreach (var enemy in engine.Enemies)
            {
                if (names.Length > 0) names.Append(", ");
                names.Append($"{enemy.DisplayName} Lv{enemy.Monster.Level}");
            }
            Log($"야생 몬스터 출현! {names}");
            roundLabel.text = "라운드 1";
            RefreshVocabLabel();
            Snapshot();
            SyncAll();
        }

        private IEnumerator PlayBattle()
        {
            while (!engine.IsOver)
            {
                // 1. 스킬(과 대상) 선택
                yield return ChooseSkill();
                var question = engine.SelectSkill(pickedSkill, pickedTarget);

                // 2. 처음 보는 단어면 뜻부터 보여준다
                if (question.IsNewWord)
                    yield return ShowWordCard("새 단어!", question.Word, "확인", Palette.Gold);

                // 3. 문제
                float secondsTaken = 0f;
                yield return AskQuestion(question, seconds => secondsTaken = seconds);
                int choice = pickedChoice;

                Snapshot();
                var events = engine.SubmitAnswer(choice, secondsTaken);
                bool correct = choice >= 0 && question.IsCorrect(choice) && secondsTaken <= battleConfig.AnswerTimeLimitSeconds;

                // 4. 정답/오답 표시. 틀리면 정답을 꼭 보여줘서 학습 순간으로 만든다
                MarkChoices(question, choice);
                yield return Wait(0.8f);
                if (!correct)
                {
                    string title = choice < 0 ? "시간 초과! 오답 노트에 추가" : "오답! 오답 노트에 추가";
                    yield return ShowWordCard(title, question.Word, "다음", Palette.Bad);
                }

                // 5. 결과 연출
                HideAllPanels();
                yield return PlayEvents(events);
            }
        }

        private IEnumerator ChooseSkill()
        {
            var actor = engine.CurrentActor;
            pickedSkill = null;
            pickedTarget = null;
            targetingSkill = null;
            UpdateFrames();
            ShowSkillMenu(actor);

            while (pickedSkill == null) yield return null;
            ClearSelectable();
        }

        private IEnumerator AskQuestion(QuizQuestion question, Action<float> onDone)
        {
            ShowPanel(quizPanel);
            quizPrompt.text = question.Prompt;
            string hint = question.Direction == QuizDirection.EnglishToMeaning ? "이 단어의 뜻은?" : "알맞은 영단어는?";
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                bool used = i < question.Choices.Count;
                choiceButtons[i].gameObject.SetActive(used);
                if (!used) continue;
                UiKit.LabelOf(choiceButtons[i]).text = question.Choices[i];
                UiKit.SetColor(choiceButtons[i], Palette.Button);
                choiceButtons[i].interactable = true;
            }

            pickedChoice = int.MinValue;
            float startTime = Time.unscaledTime;
            float limit = battleConfig.AnswerTimeLimitSeconds;
            float elapsed = 0f;

            while (pickedChoice == int.MinValue)
            {
                elapsed = Time.unscaledTime - startTime;
                if (elapsed >= limit)
                {
                    pickedChoice = -1;
                    elapsed = limit;
                    break;
                }

                bool critical = elapsed <= battleConfig.CriticalTimeSeconds;
                quizHint.text = critical ? $"{hint}   ★ 크리티컬 찬스!" : hint;
                timerFill.anchorMax = new Vector2(Mathf.Clamp01(1f - elapsed / limit), 1);
                timerFillImage.color = critical ? Palette.Gold : (limit - elapsed < 3f ? Palette.Bad : Palette.Info);
                yield return null;
            }

            foreach (var button in choiceButtons) button.interactable = false;
            onDone(elapsed);
        }

        private void MarkChoices(QuizQuestion question, int chosen)
        {
            for (int i = 0; i < question.Choices.Count; i++)
            {
                if (i == question.CorrectIndex) UiKit.SetColor(choiceButtons[i], Palette.Good);
                else if (i == chosen) UiKit.SetColor(choiceButtons[i], Palette.Bad);
            }
        }

        private IEnumerator ShowWordCard(string title, WordEntry word, string buttonText, Color titleColor)
        {
            cardTitle.text = title;
            cardTitle.color = titleColor;
            cardWord.text = word.English;
            cardMeaning.text = word.Meaning;
            var extra = new StringBuilder();
            if (word.PartOfSpeech.Length > 0) extra.Append($"({word.PartOfSpeech})");
            if (word.Example.Length > 0)
            {
                if (extra.Length > 0) extra.Append("  ");
                extra.Append(word.Example);
                if (word.ExampleMeaning.Length > 0) extra.Append($"\n{word.ExampleMeaning}");
            }
            cardExtra.text = extra.ToString();
            cardConfirmLabel.text = buttonText;
            cardConfirmed = false;
            ShowPanel(cardPanel);

            while (!cardConfirmed) yield return null;
        }

        private IEnumerator PlayEvents(IReadOnlyList<BattleEvent> events)
        {
            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case BattleEventType.QuizAnswered:
                        Log((e.Correct ? "정답! " : "오답… ") + MasteryText(e.Mastery));
                        yield return Wait(0.5f);
                        break;

                    case BattleEventType.SkillUsed:
                        Log($"{e.Actor.DisplayName}의 {e.Skill.DisplayName}!");
                        yield return Wait(0.5f);
                        break;

                    case BattleEventType.SkillFailed:
                        Log($"{e.Actor.DisplayName}의 {e.Skill.DisplayName} 실패…");
                        Float(e.Actor, "실패", Palette.TextDim, 44);
                        yield return Wait(0.7f);
                        break;

                    case BattleEventType.Damage:
                        var d = shown[e.Target];
                        shown[e.Target] = (Mathf.Max(0, d.hp - e.Amount), Mathf.Max(0, d.shield - e.Absorbed));
                        Sync(e.Target);
                        string dmgText = e.Amount > 0 ? $"-{e.Amount}" : "막음!";
                        Float(e.Target, e.IsCritical ? $"크리티컬! {dmgText}" : dmgText,
                            e.Amount > 0 ? Palette.Bad : Palette.Info, e.IsCritical ? 58 : 48);
                        Log(e.Absorbed > 0
                            ? $"{e.Target.DisplayName} 피해 {e.Amount} (보호막이 {e.Absorbed} 흡수)"
                            : $"{e.Target.DisplayName} 피해 {e.Amount}" + (e.IsCritical ? " — 크리티컬!" : ""));
                        yield return Wait(0.6f);
                        break;

                    case BattleEventType.Heal:
                        var h = shown[e.Target];
                        shown[e.Target] = (h.hp + e.Amount, h.shield);
                        Sync(e.Target);
                        Float(e.Target, $"+{e.Amount}", Palette.Good, 48);
                        Log($"{e.Target.DisplayName} HP +{e.Amount}" + (e.IsCritical ? " — 크리티컬!" : ""));
                        yield return Wait(0.6f);
                        break;

                    case BattleEventType.Shield:
                        var s = shown[e.Target];
                        shown[e.Target] = (s.hp, s.shield + e.Amount);
                        Sync(e.Target);
                        Float(e.Target, $"보호막 +{e.Amount}", Palette.Info, 40);
                        Log($"{e.Target.DisplayName} 보호막 +{e.Amount}");
                        yield return Wait(0.6f);
                        break;

                    case BattleEventType.Defeated:
                        Log($"{e.Target.DisplayName} 쓰러졌다!");
                        yield return Wait(0.5f);
                        break;

                    case BattleEventType.RoundStarted:
                        roundLabel.text = $"라운드 {e.Round}";
                        foreach (var unit in engine.Party) shown[unit] = (shown[unit].hp, 0);
                        SyncAll();
                        yield return Wait(0.2f);
                        break;
                }
            }

            RefreshVocabLabel();
            SyncAll();
        }

        private IEnumerator ShowResult()
        {
            bool victory = engine.Phase == BattlePhase.Victory;
            var body = new StringBuilder();

            if (victory)
            {
                var reward = engine.CalculateReward();
                var levels = BattleRewardCalculator.Apply(reward, party, inventory);
                resultTitle.text = "승리!";
                resultTitle.color = Palette.Gold;
                body.AppendLine($"경험치 +{reward.Exp}   골드 +{reward.Gold}  (보유 {inventory.Gold})");
                foreach (var item in reward.Items) body.AppendLine($"획득: {item.Item.DisplayName} x{item.Count}");
                for (int i = 0; i < party.Count; i++)
                {
                    if (levels[i] > 0) body.AppendLine($"{party[i].DisplayName} 레벨 업! → Lv{party[i].Level}");
                }
            }
            else
            {
                resultTitle.text = "패배…";
                resultTitle.color = Palette.Bad;
                body.AppendLine("파티가 전멸했다. 마을에서 회복하고 다시 도전하자!");
            }

            int up = 0, down = 0;
            foreach (var change in engine.MasteryChanges)
            {
                if (change.LeveledUp) up++;
                else if (change.LeveledDown) down++;
            }
            body.AppendLine();
            body.AppendLine($"정답 {engine.CorrectAnswers}   오답 {engine.WrongAnswers}   단어 숙련도 ▲{up} ▼{down}");
            body.Append($"학습한 단어 {LearnedCount()}/{words.Words.Count}");
            resultBody.text = body.ToString();

            SetResultButtons(victory ? "다음 전투" : "파티 회복 후 재도전", victory ? "파티 회복 후 전투" : null);
            resultChoice = -1;
            ShowPanel(resultPanel);
            while (resultChoice < 0) yield return null;

            // 패배 후 재도전, 또는 '회복' 선택 시 파티 완전 회복
            if (!victory || resultChoice == 1)
            {
                foreach (var monster in party) monster.RestoreFully();
            }
        }

        // ------------------------------------------------------------------ 스킬 메뉴 / 대상 선택

        private void ShowSkillMenu(BattleUnit actor)
        {
            ShowPanel(skillPanel);
            skillTitle.text = $"{actor.DisplayName}의 차례 — 스킬을 고르세요";
            cancelButton.gameObject.SetActive(false);

            for (int i = 0; i < skillButtons.Count; i++)
            {
                bool used = i < actor.Skills.Count;
                skillButtons[i].gameObject.SetActive(used);
                if (!used) continue;

                var skill = actor.Skills[i];
                UiKit.LabelOf(skillButtons[i]).text = SkillLabel(skill);
                UiKit.SetColor(skillButtons[i], SkillColor(skill));
                skillButtons[i].interactable = true;
                skillButtons[i].onClick.RemoveAllListeners();
                skillButtons[i].onClick.AddListener(() => OnSkillClicked(skill));
            }
        }

        private void OnSkillClicked(SkillData skill)
        {
            if (!skill.NeedsTargetChoice)
            {
                pickedSkill = skill;
                return;
            }

            var candidates = skill.Target == SkillTarget.SingleEnemy ? engine.Enemies : engine.Party;
            var alive = new List<BattleUnit>();
            foreach (var unit in candidates)
            {
                if (!unit.IsDefeated) alive.Add(unit);
            }

            // 적이 한 마리뿐이면 대상 선택을 건너뛴다
            if (skill.Target == SkillTarget.SingleEnemy && alive.Count == 1)
            {
                pickedTarget = alive[0];
                pickedSkill = skill;
                return;
            }

            targetingSkill = skill;
            skillTitle.text = $"{skill.DisplayName} — 대상을 선택하세요";
            foreach (var button in skillButtons) button.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(true);

            foreach (var unit in alive)
            {
                var target = unit;
                var view = viewOf[unit];
                view.SetSelectable(true, () =>
                {
                    pickedTarget = target;
                    pickedSkill = skill;
                });
                view.SetFrame(Palette.Info);
            }
        }

        private void OnCancelTargeting()
        {
            if (targetingSkill == null) return;
            targetingSkill = null;
            ClearSelectable();
            UpdateFrames();
            ShowSkillMenu(engine.CurrentActor);
        }

        private void ClearSelectable()
        {
            foreach (var view in viewOf.Values) view.SetSelectable(false);
            UpdateFrames();
        }

        private void UpdateFrames()
        {
            foreach (var pair in viewOf)
            {
                bool isActor = engine != null && !engine.IsOver && pair.Key == engine.CurrentActor;
                pair.Value.SetFrame(isActor ? (Color?)Palette.Gold : null);
            }
        }

        private static string SkillLabel(SkillData skill)
        {
            string kind = skill.Kind == SkillKind.Damage ? "공격" : skill.Kind == SkillKind.Heal ? "회복" : "보호막";
            string target;
            switch (skill.Target)
            {
                case SkillTarget.SingleEnemy: target = "적 1체"; break;
                case SkillTarget.AllEnemies: target = "적 전체"; break;
                case SkillTarget.SingleAlly: target = "아군 1체"; break;
                case SkillTarget.AllAllies: target = "아군 전체"; break;
                default: target = "자신"; break;
            }
            string quiz = skill.QuizDirection == QuizDirection.EnglishToMeaning ? "영→한 · 쉬움" : "한→영 · 어려움";
            return $"{skill.DisplayName}\n<size=30>{kind} {skill.Power} · {target}  |  문제 {quiz}</size>";
        }

        private static Color SkillColor(SkillData skill)
        {
            switch (skill.Kind)
            {
                case SkillKind.Damage: return new Color(0.6f, 0.25f, 0.28f);
                case SkillKind.Heal: return new Color(0.2f, 0.5f, 0.33f);
                default: return new Color(0.22f, 0.38f, 0.62f);
            }
        }

        // ------------------------------------------------------------------ 화면 갱신 도우미

        private void Snapshot()
        {
            shown.Clear();
            foreach (var unit in engine.Party) shown[unit] = (unit.Hp, unit.Shield);
            foreach (var unit in engine.Enemies) shown[unit] = (unit.Hp, unit.Shield);
        }

        private void Sync(BattleUnit unit) => viewOf[unit].Sync(shown[unit].hp, shown[unit].shield);

        private void SyncAll()
        {
            foreach (var unit in engine.Party) shown[unit] = (unit.Hp, unit.Shield);
            foreach (var unit in engine.Enemies) shown[unit] = (unit.Hp, unit.Shield);
            foreach (var pair in viewOf) pair.Value.Sync(shown[pair.Key].hp, shown[pair.Key].shield);
            UpdateFrames();
        }

        private void RefreshVocabLabel() => vocabLabel.text = $"학습한 단어 {LearnedCount()}/{words.Words.Count}";

        private int LearnedCount()
        {
            int count = 0;
            foreach (var entry in vocabulary.Entries)
            {
                if (entry.Level > MasteryLevel.New) count++;
            }
            return count;
        }

        private void Log(string line)
        {
            logLines.Add(line);
            if (logLines.Count > 3) logLines.RemoveAt(0);
            logLabel.text = string.Join("\n", logLines);
        }

        private static string MasteryText(MasteryChange change)
        {
            if (change.After > change.Before) return $"[{change.Before.DisplayName()} → {change.After.DisplayName()} ▲]";
            if (change.After < change.Before) return $"[{change.Before.DisplayName()} → {change.After.DisplayName()} ▼]";
            return $"[{change.After.DisplayName()}]";
        }

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds * animationScale);
        }

        private void Float(BattleUnit unit, string text, Color color, int size)
        {
            if (!viewOf.TryGetValue(unit, out var view)) return;
            StartCoroutine(FloatRoutine(view.Root, text, color, size));
        }

        private IEnumerator FloatRoutine(RectTransform target, string text, Color color, int size)
        {
            var label = UiKit.Label("Float", root, text, size, color, 0.5f, 0.5f, 0.5f, 0.5f,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var rt = label.rectTransform;
            rt.sizeDelta = new Vector2(520, 90);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.85f);
            outline.effectDistance = new Vector2(3, -3);

            Vector3 start = target.TransformPoint(target.rect.center);
            float duration = 0.9f * animationScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                rt.position = start + Vector3.up * (140f * k * canvas.scaleFactor);
                var c = label.color;
                c.a = 1f - k * k;
                label.color = c;
                outline.effectColor = new Color(0, 0, 0, 0.85f * c.a);
                yield return null;
            }
            Destroy(label.gameObject);
        }

        // ------------------------------------------------------------------ 패널 전환

        private void ShowPanel(GameObject panel)
        {
            skillPanel.SetActive(panel == skillPanel);
            quizPanel.SetActive(panel == quizPanel);
            cardPanel.SetActive(panel == cardPanel);
            resultPanel.SetActive(panel == resultPanel);
        }

        private void HideAllPanels() => ShowPanel(null);

        private void SetResultButtons(string primary, string secondary)
        {
            UiKit.LabelOf(resultPrimary).text = primary;
            bool two = secondary != null;
            resultSecondary.gameObject.SetActive(two);
            if (two) UiKit.LabelOf(resultSecondary).text = secondary;
            var rt = (RectTransform)resultPrimary.transform;
            rt.anchorMin = new Vector2(two ? 0.03f : 0.2f, 0.02f);
            rt.anchorMax = new Vector2(two ? 0.49f : 0.8f, 0.2f);
        }

        // ------------------------------------------------------------------ UI 생성

        private void BuildUi()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            UiKit.Panel("Background", canvasGo.transform, Palette.Background);

            root = UiKit.Stretch("SafeArea", canvasGo.transform);
            ApplySafeArea(root);

            // 상단 바
            roundLabel = UiKit.Label("Round", root, "라운드 1", 36, Palette.Gold, 0.03f, 0.945f, 0.4f, 1f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            vocabLabel = UiKit.Label("Vocab", root, "", 32, Palette.TextDim, 0.4f, 0.945f, 0.97f, 1f,
                TextAnchor.MiddleRight);

            enemyArea = UiKit.Rect("EnemyArea", root, 0, 0.62f, 1, 0.94f);

            // 전투 로그
            var logPanel = UiKit.Panel("LogPanel", root, Palette.Panel, 0.03f, 0.512f, 0.97f, 0.612f);
            logLabel = UiKit.Label("Log", logPanel.transform, "", 31, Palette.Text, 0, 0, 1, 1,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 22);
            UiKit.Pad(logLabel.rectTransform, 24, 8, 24, 8);

            // 아군 카드 3장
            var partyArea = UiKit.Rect("PartyArea", root, 0, 0.355f, 1, 0.51f);
            partyViews = new UnitView[3];
            for (int i = 0; i < 3; i++)
                partyViews[i] = UnitView.Create(partyArea, $"Party_{i}", i / 3f, 0, (i + 1) / 3f, 1);

            // 하단 패널들
            var bottom = UiKit.Rect("Bottom", root, 0, 0, 1, 0.35f);
            UiKit.Pad(bottom, 24, 24, 24, 12);
            BuildSkillPanel(bottom);
            BuildQuizPanel(bottom);
            BuildCardPanel(bottom);
            BuildResultPanel(bottom);
            HideAllPanels();
        }

        private void BuildSkillPanel(Transform parent)
        {
            skillPanel = UiKit.Stretch("SkillPanel", parent).gameObject;
            skillTitle = UiKit.Label("Title", skillPanel.transform, "", 40, Palette.Text, 0, 0.84f, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Bold, true, 24);

            for (int i = 0; i < 4; i++)
            {
                float top = 0.82f - i * 0.2066f;
                var button = UiKit.MakeButton($"SkillButton_{i}", skillPanel.transform, "", Palette.Button, 44,
                    0, top - 0.19f, 1, top);
                var label = UiKit.LabelOf(button);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                skillButtons.Add(button);
            }

            cancelButton = UiKit.MakeButton("CancelButton", skillPanel.transform, "취소", new Color(0.35f, 0.35f, 0.4f),
                44, 0.25f, 0.04f, 0.75f, 0.24f);
            cancelButton.onClick.AddListener(OnCancelTargeting);
        }

        private void BuildQuizPanel(Transform parent)
        {
            quizPanel = UiKit.Stretch("QuizPanel", parent).gameObject;
            quizPrompt = UiKit.Label("Prompt", quizPanel.transform, "", 72, Palette.Text, 0, 0.8f, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Bold, true, 30);
            quizHint = UiKit.Label("Hint", quizPanel.transform, "", 30, Palette.TextDim, 0, 0.735f, 1, 0.8f);

            var timerBack = UiKit.Panel("TimerBack", quizPanel.transform, new Color(0.04f, 0.05f, 0.09f),
                0.02f, 0.695f, 0.98f, 0.72f);
            timerFillImage = UiKit.Panel("TimerFill", timerBack.transform, Palette.Info);
            timerFill = timerFillImage.rectTransform;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                float top = 0.68f - i * 0.1725f;
                var button = UiKit.MakeButton($"Choice_{i}", quizPanel.transform, "", Palette.Button, 44,
                    0, top - 0.16f, 1, top, bestFit: true);
                button.onClick.AddListener(() =>
                {
                    if (pickedChoice == int.MinValue) pickedChoice = index;
                });
                choiceButtons.Add(button);
            }
        }

        private void BuildCardPanel(Transform parent)
        {
            cardPanel = UiKit.Stretch("CardPanel", parent).gameObject;
            UiKit.Panel("Bg", cardPanel.transform, Palette.Panel);
            cardTitle = UiKit.Label("Title", cardPanel.transform, "", 44, Palette.Gold, 0, 0.8f, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Bold, true, 24);
            cardWord = UiKit.Label("Word", cardPanel.transform, "", 88, Palette.Text, 0.03f, 0.56f, 0.97f, 0.8f,
                TextAnchor.MiddleCenter, FontStyle.Bold, true, 36);
            cardMeaning = UiKit.Label("Meaning", cardPanel.transform, "", 54, Palette.Gold, 0.03f, 0.38f, 0.97f, 0.57f,
                TextAnchor.MiddleCenter, FontStyle.Bold, true, 26);
            cardExtra = UiKit.Label("Extra", cardPanel.transform, "", 30, Palette.TextDim, 0.05f, 0.2f, 0.95f, 0.38f,
                TextAnchor.UpperCenter, FontStyle.Normal, true, 20);
            cardConfirm = UiKit.MakeButton("CardConfirmButton", cardPanel.transform, "확인", Palette.Button, 44,
                0.2f, 0.03f, 0.8f, 0.2f);
            cardConfirmLabel = UiKit.LabelOf(cardConfirm);
            cardConfirm.onClick.AddListener(() => cardConfirmed = true);
        }

        private void BuildResultPanel(Transform parent)
        {
            resultPanel = UiKit.Stretch("ResultPanel", parent).gameObject;
            UiKit.Panel("Bg", resultPanel.transform, Palette.Panel);
            resultTitle = UiKit.Label("Title", resultPanel.transform, "", 60, Palette.Gold, 0, 0.8f, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            resultBody = UiKit.Label("Body", resultPanel.transform, "", 34, Palette.Text, 0.05f, 0.24f, 0.95f, 0.8f,
                TextAnchor.UpperLeft, FontStyle.Normal, true, 22);
            resultPrimary = UiKit.MakeButton("ResultButton_Primary", resultPanel.transform, "", Palette.Button, 38,
                0.03f, 0.02f, 0.49f, 0.2f, bestFit: true);
            resultPrimary.onClick.AddListener(() => resultChoice = 0);
            resultSecondary = UiKit.MakeButton("ResultButton_Secondary", resultPanel.transform, "", new Color(0.2f, 0.5f, 0.33f),
                38, 0.51f, 0.02f, 0.97f, 0.2f, bestFit: true);
            resultSecondary.onClick.AddListener(() => resultChoice = 1);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            // 이 프로젝트는 Input System 전용이라 StandaloneInputModule이 아닌 InputSystemUIInputModule을 쓴다
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void ApplySafeArea(RectTransform rt)
        {
            var safe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        }
    }
}
