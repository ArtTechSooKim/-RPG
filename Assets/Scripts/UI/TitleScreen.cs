using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordRPG.Game;

namespace WordRPG.UI
{
    // 타이틀 (Figma '타이틀'): 로고, 시작 몬스터 3마리, 저장 요약, [이어하기] / [처음부터] (저장이 없으면 [시작하기]), 설정.
    // 처음부터는 확인 창을 거쳐 저장을 지운다. 시작하면 필드 씬으로
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private string fieldScene = GameManager.FieldSceneName;

        private GameManager manager;
        private Action<bool> startOverride; // 테스트: 씬을 바꾸는 대신 (새 게임 여부)를 받는다
        private bool started;

        private RectTransform root;
        private GameObject saveCard;
        private Text saveLine1, saveLine2;
        private readonly List<(Image back, Text initial, Image sprite)> avatars = new List<(Image, Text, Image)>();
        private readonly List<(Image card, Text initial, Image sprite)> cards = new List<(Image, Text, Image)>();
        private Button continueButton, newGameButton;
        private Text newGameLabel;
        private ConfirmDialog dialog;
        private SettingsView settingsView;
        private readonly List<(RectTransform rt, float baseY, float phase)> floaters = new List<(RectTransform, float, float)>();

        public bool IsSettingsOpen => settingsView != null && settingsView.IsOpen;

        public void Configure(Action<bool> onStart) => startOverride = onStart;

        private void Start()
        {
            manager = GameManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[TitleScreen] GameManager가 없습니다");
                return;
            }
            Build();
            Refresh();
            Sound.PlayMusic(Music.Title);
        }

        private void Update()
        {
            // 떠다니는 영단어·별이 천천히 오르내린다
            float t = Time.unscaledTime;
            foreach (var (rt, baseY, phase) in floaters)
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseY + Mathf.Sin(t * 1.2f + phase) * 10f);
        }

        // ------------------------------------------------------------------ 화면

        private void Build()
        {
            UiKit.EnsureEventSystem();
            var canvasGo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            UiKit.Panel("Background", canvasGo.transform, Palette.Background);
            root = UiKit.Stretch("SafeArea", canvasGo.transform);
            UiKit.ApplySafeArea(root);

            var glow = UiKit.IconImage("Glow", root, UiKit.GlowSprite(), 0.5f, 0.745f, 0.5f, 0.745f);
            glow.rectTransform.sizeDelta = new Vector2(1000, 1000);
            glow.color = new Color(Palette.Gold.r, Palette.Gold.g, Palette.Gold.b, 0.16f);

            // 떠다니는 영단어 (Figma 'word-chip')
            var chips = new (string word, float x, float y, float rotation)[]
            {
                ("apple", 64, 170, -8), ("brave", 800, 200, 7), ("memory", 70, 640, -4),
                ("friend", 840, 660, 5), ("dream", 64, 1190, 6), ("magic", 850, 1200, -6),
            };
            for (int i = 0; i < chips.Length; i++)
            {
                var (word, x, y, rotation) = chips[i];
                var chip = UiKit.Pill(UiKit.Panel($"Word_{word}", root, new Color(Palette.PanelLight.r, Palette.PanelLight.g, Palette.PanelLight.b, 0.55f), 0, 1, 0, 1));
                chip.raycastTarget = false;
                var rt = chip.rectTransform;
                rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(48 + word.Length * 17, 54);
                Place(rt, x, y);
                rt.localEulerAngles = new Vector3(0, 0, -rotation);
                UiKit.Label("Text", chip.transform, word, 30, new Color(Palette.TextDim.r, Palette.TextDim.g, Palette.TextDim.b, 0.8f), 0, 0, 1, 1);
                floaters.Add((rt, -y, i * 1.3f));
            }

            // 로고 (게임 이름: 영단어RPG)
            UiKit.Display(UiKit.Label("Logo", root, "영단어RPG", 168, Palette.Gold, 0, 0.7f, 1, 0.86f,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 96));
            UiKit.Label("Subtitle", root, "성유물과 함께하는 영단어 모험", 40, Palette.Text, 0, 0.655f, 1, 0.7f,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            // 가운데 = 주인공, 왼쪽·오른쪽 = 끼운 성유물 (앞의 두 칸)
            var slots = new (float x, float y, float size)[] { (540, 920, 300), (230, 980, 240), (850, 980, 240) };
            foreach (var (x, y, size) in slots)
            {
                var shadow = UiKit.IconImage("Shadow", root, UiKit.GlowSprite(), 0, 1, 0, 1); // 부드러운 그림자
                shadow.preserveAspect = false;
                shadow.color = new Color(0, 0, 0, 0.75f);
                shadow.rectTransform.sizeDelta = new Vector2(size * 1.0f, 70);
                Place(shadow.rectTransform, x, y + size / 2f + 14);
                var card = UiKit.RoundPanel("Monster", root, Palette.PanelLight, UiKit.RadiusLg, 0, 1, 0, 1);
                card.raycastTarget = false;
                card.rectTransform.sizeDelta = new Vector2(size, size);
                Place(card.rectTransform, x, y);
                var initial = UiKit.Display(UiKit.Label("Initial", card.transform, "", Mathf.RoundToInt(size * 0.42f), Palette.Text, 0, 0, 1, 1));
                var sprite = UiKit.IconImage("Sprite", card.transform, null, 0.12f, 0.12f, 0.88f, 0.88f);
                cards.Add((card, initial, sprite));
            }
            foreach (var (x, y, size) in new (float, float, float)[] { (350, 760, 44), (760, 800, 36), (116, 840, 32), (980, 850, 40), (580, 740, 28) })
            {
                var star = UiKit.IconImage("Star", root, UiKit.Icon("star_full"), 0, 1, 0, 1);
                star.rectTransform.sizeDelta = new Vector2(size, size);
                Place(star.rectTransform, x, y);
                star.color = new Color(1, 1, 1, 0.85f);
                floaters.Add((star.rectTransform, -y, x * 0.01f));
            }

            // 저장 요약 카드
            var save = UiKit.RoundPanel("SaveCard", root, Palette.Panel, UiKit.RadiusMd, 0.067f, 0.265f, 0.933f, 0.328f);
            save.raycastTarget = false;
            saveCard = save.gameObject;
            for (int i = 0; i < 3; i++) // 주인공 + 끼운 성유물 두 개
            {
                var back = UiKit.Pill(UiKit.Panel($"Avatar_{i}", save.transform, Palette.PanelLight, 0, 0.5f, 0, 0.5f));
                back.raycastTarget = false;
                back.rectTransform.sizeDelta = new Vector2(72, 72);
                back.rectTransform.anchoredPosition = new Vector2(64 + i * 58, 0);
                var initial = UiKit.Display(UiKit.Label("Initial", back.transform, "", 34, Palette.Text, 0, 0, 1, 1));
                var sprite = UiKit.IconImage("Sprite", back.transform, null, 0.08f, 0.08f, 0.92f, 0.92f);
                avatars.Add((back, initial, sprite));
            }
            saveLine1 = UiKit.Label("Line1", save.transform, "", 34, Palette.Text, 0.25f, 0.5f, 0.98f, 0.92f,
                TextAnchor.MiddleLeft, FontStyle.Bold, true, 20);
            saveLine2 = UiKit.Label("Line2", save.transform, "", 30, Palette.TextDim, 0.25f, 0.08f, 0.98f, 0.5f,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 18);

            continueButton = UiKit.MakeButton("ContinueButton", root, "이어하기", Palette.Gold, 52, 0.067f, 0.177f, 0.933f, 0.237f);
            UiKit.LabelOf(continueButton).color = Palette.OnAccent;
            continueButton.onClick.AddListener(() => Begin(false));
            newGameButton = UiKit.MakeButton("NewGameButton", root, "처음부터", Palette.Neutral, 46, 0.067f, 0.112f, 0.933f, 0.165f);
            newGameLabel = UiKit.LabelOf(newGameButton);
            newGameButton.onClick.AddListener(OnNewGame);

            var settingsButton = UiKit.IconButton("SettingsButton", root, "settings", "설정", Palette.PanelLight, 1, 1, 1, 1);
            var settingsRect = (RectTransform)settingsButton.transform;
            settingsRect.pivot = new Vector2(1, 1);
            settingsRect.sizeDelta = new Vector2(120, 120);
            settingsRect.anchoredPosition = new Vector2(-24, -40);
            settingsButton.onClick.AddListener(OpenSettings);

            UiKit.Label("Footer", root, $"v{Application.version}   ·   글꼴 Jua · Noto Sans KR (SIL OFL 1.1)", 24, Palette.TextDim,
                0, 0.01f, 1, 0.04f);

            dialog = ConfirmDialog.Create(root);
            settingsView = SettingsView.Create(root);
        }

        // Figma 좌표(1080 폭, 위에서부터 y)로 놓되 가로는 비율로 — 화면 폭이 달라도 좌우 균형 유지
        private static void Place(RectTransform rt, float x, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(x / 1080f, 1f);
            rt.anchoredPosition = new Vector2(0, -y);
        }

        // 저장이 있으면 [이어하기] + [처음부터], 없으면 [시작하기] 하나
        private void Refresh()
        {
            bool hasSave = manager.HasSave;
            var session = manager.Session;
            var hero = session.Hero;
            for (int i = 0; i < cards.Count; i++)
            {
                // 0 = 주인공, 1·2 = 끼운 성유물 (없으면 숨김)
                var relic = i == 0 ? null : hero.SlotAt(i - 1);
                bool has = i == 0 || relic != null;
                cards[i].card.gameObject.SetActive(has);
                avatars[i].back.gameObject.SetActive(has && hasSave);
                if (!has) continue;
                var sprite = i == 0 ? UiKit.HeroPortrait(hero.Data) : UiKit.RelicIcon(relic.Data);
                string name = i == 0 ? hero.DisplayName : relic.Data.DisplayName;
                string initial = name.Length > 0 ? name.Substring(0, 1) : "?";
                var color = i == 0 ? Palette.PanelLight : Color.Lerp(Palette.PanelLight, relic.Data.PlaceholderColor, 0.22f);
                cards[i].card.color = color;
                cards[i].initial.text = initial;
                cards[i].initial.enabled = sprite == null;
                cards[i].sprite.sprite = sprite;
                cards[i].sprite.enabled = sprite != null;
                avatars[i].back.color = color;
                avatars[i].initial.text = initial;
                avatars[i].initial.enabled = sprite == null;
                avatars[i].sprite.sprite = sprite;
                avatars[i].sprite.enabled = sprite != null;
            }

            saveCard.SetActive(hasSave);
            continueButton.gameObject.SetActive(hasSave);
            if (hasSave)
            {
                var area = manager.Database != null && session.World.AreaId != null ? manager.Database.FindArea(session.World.AreaId) : null;
                string where = area != null ? area.DisplayName : "초원";
                saveLine1.text = $"{where}  ·  {hero.DisplayName} Lv{hero.Level}  ·  성유물 {hero.Relics.Count}개";
                string total = area != null && area.Words != null ? $" / {area.Words.Words.Count}" : "";
                int battles = session.Record.BattlesWon + session.Record.BattlesLost;
                saveLine2.text = $"발견한 단어 {session.Vocabulary.DiscoveredCount}{total}   ·   전투 {battles}번";
            }

            // 저장이 없으면 시작 버튼이 이어하기 자리(금색)로
            var rt = (RectTransform)newGameButton.transform;
            rt.anchorMin = new Vector2(0.067f, hasSave ? 0.112f : 0.177f);
            rt.anchorMax = new Vector2(0.933f, hasSave ? 0.165f : 0.237f);
            newGameLabel.text = hasSave ? "처음부터" : "시작하기";
            newGameLabel.color = hasSave ? Palette.Text : Palette.OnAccent;
            UiKit.SetColor(newGameButton, hasSave ? Palette.Neutral : Palette.Gold);
        }

        private void OnNewGame()
        {
            if (started) return;
            if (!manager.HasSave)
            {
                Begin(true);
                return;
            }
            dialog.Show("처음부터 시작할까요?",
                "지금까지의 기록(발견한 단어, 몬스터, 아이템)이\n모두 지워져요. 되돌릴 수 없어요.",
                "처음부터", true, () =>
                {
                    manager.DeleteSave();
                    Begin(true);
                });
        }

        private void OpenSettings()
        {
            settingsView.Show(manager.Settings, manager.SaveSettings, () =>
            {
                manager.DeleteSave();
                Refresh();
            });
        }

        private void Begin(bool newGame)
        {
            if (started) return;
            started = true;
            manager.MarkPlaying();
            manager.Save(); // 새 게임도 바로 저장 → 다음에 켜면 [이어하기]
            if (startOverride != null)
            {
                startOverride(newGame);
                return;
            }
            SceneManager.LoadScene(fieldScene);
        }
    }
}
