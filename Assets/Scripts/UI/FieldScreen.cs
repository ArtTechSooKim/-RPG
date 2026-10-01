using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using WordRPG.Battle;
using WordRPG.Field;
using WordRPG.Game;

namespace WordRPG.UI
{
    // 탑다운 필드: 한 칸씩 이동, 풀숲 조우 → 전투(BattleScreen을 위에 덮음) → 원래 자리로 복귀.
    // 보물상자·회복의 샘·진화의 제단·상점은 '부딪혀서' 사용. 입력은 화면 아래 가상 패드 + 키보드(방향키/WASD).
    // 이동·조우 규칙은 FieldWalker / EncounterCounter(순수 C#)가 하고 여기서는 화면과 입력만 다룬다
    public class FieldScreen : MonoBehaviour
    {
        [SerializeField] private FieldArea area;
        [SerializeField] private BattleConfig battleConfig = new BattleConfig();
        [Tooltip("한 칸 이동에 걸리는 시간(초)")]
        [SerializeField] private float stepDuration = 0.16f;
        [Tooltip("화면 가로에 보이는 타일 수")]
        [SerializeField] private float tilesAcross = 11f;
        [Tooltip("연출 시간 배율. 테스트에서는 아주 작게")]
        [SerializeField] private float animationScale = 1f;

        private GameSession session;
        private Action saveProgress;
        private string statusMessage;
        private bool loadedFromSave;

        private System.Random rng;
        private FieldWalker walker;
        private EncounterCounter encounterCounter;
        private bool initialized;
        private bool moving;
        private bool inBattle;
        private bool transitioning;
        private float moveT;
        private Vector3 moveFrom, moveTo;
        private float interactCooldown;

        private Camera cam;
        private Transform player;
        private SpriteRenderer playerRenderer;
        private Tilemap tilemap;
        private Tile openedChestTile;
        private BattleScreen battle;

        private RectTransform hudRoot;
        private Text areaLabel, dexLabel, toastText;
        private GameObject toastPanel;
        private float toastUntil;
        private Image flash;
        private HoldButton padUp, padDown, padLeft, padRight;
        private DexView dexView;
        private EvolutionView evolutionView;
        private ShopView shopView;
        private Text[] badgeNames;
        private RectTransform[] badgeFills;
        private Image[] badgeFillImages;

        public Vector2Int PlayerCell => walker.Position;
        public Direction Facing => walker.Facing;
        public bool IsMoving => moving;
        public bool IsInBattle => inBattle || transitioning;
        public BattleScreen Battle => battle;
        public GameSession Session => session;
        public bool IsPanelOpen => dexView.IsOpen || evolutionView.IsOpen || shopView.IsOpen;
        public string ToastMessage => toastPanel != null && toastPanel.activeSelf ? toastText.text : "";

        // 코드로 만들 때(테스트) Start 전에 호출. session을 안 주면 GameManager 것을 쓴다
        public void Configure(FieldArea fieldArea, GameSession gameSession = null, Action onSave = null,
            float step = 0.16f, float animScale = 1f, BattleConfig config = null)
        {
            area = fieldArea;
            session = gameSession;
            saveProgress = onSave;
            stepDuration = step;
            animationScale = animScale;
            if (config != null) battleConfig = config;
        }

        private void Start() => EnsureInitialized();

        private bool EnsureInitialized()
        {
            if (initialized) return true;

            if (session == null && GameManager.Instance != null)
            {
                var manager = GameManager.Instance;
                session = manager.Session;
                saveProgress = manager.Save;
                statusMessage = manager.StatusMessage;
                loadedFromSave = manager.LoadedFromSave;
            }
            if (session == null || area == null)
            {
                Debug.LogError("[FieldScreen] GameManager(또는 Configure의 session) / area 가 비어 있습니다");
                return false;
            }

            FieldMap map;
            try
            {
                map = area.Map;
            }
            catch (FormatException e)
            {
                Debug.LogError($"[FieldScreen] {area.name} 맵 오류: {e.Message}");
                return false;
            }

            initialized = true;
            rng = new System.Random();

            // 저장된 위치가 이 지역의 걸을 수 있는 칸이면 거기서, 아니면 시작 위치에서
            var spawn = session.World.TryGetPosition(area.AreaId, out var saved) && map.IsWalkable(saved) ? saved : map.Start;
            walker = new FieldWalker(map, spawn);
            encounterCounter = new EncounterCounter(area.EncounterRate, area.MinStepsBetweenEncounters);
            session.World.SetPosition(area.AreaId, spawn);

            BuildWorld(map);
            BuildHud();
            BuildBattle();
            SnapPlayer();
            UpdateCamera();
            RefreshHud();

            if (!string.IsNullOrEmpty(statusMessage)) ShowToast(statusMessage, 3f);
            if (!loadedFromSave) ShowToast("진한 풀숲을 걸으면 야생 몬스터가 나타나요!\n상자·샘·제단·상점은 부딪혀서 사용", 4f);
            return true;
        }

        private void Update()
        {
            if (!initialized) return;
            UpdateToast();
            UpdateCamera();

            if (inBattle || transitioning) return;
            if (IsPanelOpen)
            {
                interactCooldown = 0.5f; // 창을 닫은 직후 같은 방향을 누르고 있어도 바로 다시 열리지 않게
                return;
            }

            if (moving)
            {
                moveT += Time.deltaTime / Mathf.Max(0.001f, stepDuration);
                player.position = Vector3.Lerp(moveFrom, moveTo, Mathf.Clamp01(moveT));
                if (moveT >= 1f)
                {
                    moving = false;
                    player.position = moveTo;
                    OnStepFinished();
                }
                return;
            }

            interactCooldown -= Time.deltaTime;
            var direction = ReadDirection();
            if (direction.HasValue) TryStep(direction.Value);
        }

        // ------------------------------------------------------------------ 이동·상호작용

        private void TryStep(Direction direction)
        {
            var outcome = walker.TryStep(direction);
            playerRenderer.sprite = PlaceholderArt.Player(walker.Facing);

            switch (outcome.Kind)
            {
                case StepKind.Moved:
                    moving = true;
                    moveT = 0f;
                    moveFrom = player.position;
                    moveTo = CellCenter(outcome.Target);
                    break;
                case StepKind.Interacted:
                    if (interactCooldown <= 0f) Interact(outcome);
                    break;
            }
        }

        private void Interact(StepOutcome outcome)
        {
            interactCooldown = 0.6f;
            switch (outcome.TargetTile)
            {
                case FieldTile.Chest: OpenChest(outcome.Target); break;
                case FieldTile.Fountain: UseFountain(); break;
                case FieldTile.Altar: evolutionView.Show(session, OnTownChanged); break;
                case FieldTile.Shop:
                    if (area.Shop == null) ShowToast("상점 문이 닫혀 있다.");
                    else shopView.Show(area.Shop, session, OnTownChanged);
                    break;
            }
        }

        // 진화·구매 직후 저장하고 HUD(파티 이름·골드) 갱신
        private void OnTownChanged()
        {
            saveProgress?.Invoke();
            RefreshHud();
        }

        private void OnStepFinished()
        {
            session.World.SetPosition(area.AreaId, walker.Position);
            bool onGrass = walker.Map.Get(walker.Position) == FieldTile.Grass;
            if (encounterCounter.OnStep(onGrass, rng)) StartCoroutine(Encounter());
        }

        private void OpenChest(Vector2Int cell)
        {
            var result = session.OpenChest(area, cell);
            if (result.WasEmpty)
            {
                ShowToast("빈 상자다.");
                return;
            }

            tilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), openedChestTile);
            string loot = result.Item != null ? $"{result.Item.DisplayName} x{result.Count}" : "";
            if (result.Gold > 0) loot += (loot.Length > 0 ? " + " : "") + $"{result.Gold} 골드";
            ShowToast($"보물상자를 열었다!\n{loot} 획득", 2.5f);
            saveProgress?.Invoke();
            RefreshHud();
        }

        private void UseFountain()
        {
            session.RestoreParty();
            saveProgress?.Invoke();
            RefreshHud();
            ShowToast("회복의 샘 — 파티가 모두 회복되었다!");
        }

        private IEnumerator Encounter()
        {
            transitioning = true;
            flash.gameObject.SetActive(true);
            float duration = 0.4f * animationScale;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                flash.color = new Color(1, 1, 1, Mathf.PingPong(k * 3f, 1f) * 0.85f);
                yield return null;
            }
            flash.gameObject.SetActive(false);

            var enemies = area.Encounters.Roll(rng);
            transitioning = false;
            inBattle = true;
            HideToast();
            battle.BeginBattle(enemies, area.Words, OnBattleFinished);
        }

        private void OnBattleFinished(bool won)
        {
            inBattle = false;
            encounterCounter.Reset();
            if (!won)
            {
                // 패배: 전투 화면이 이미 파티를 회복시켰다. 시작 위치(회복의 샘 앞)로 돌아간다
                walker.WarpTo(walker.Map.Start);
                SnapPlayer();
                session.World.SetPosition(area.AreaId, walker.Position);
                saveProgress?.Invoke();
                ShowToast("회복의 샘으로 돌아왔다. 파티가 회복되었다!");
            }
            RefreshHud();
        }

        private Direction? ReadDirection()
        {
            if (padUp.IsHeld) return Direction.Up;
            if (padDown.IsHeld) return Direction.Down;
            if (padLeft.IsHeld) return Direction.Left;
            if (padRight.IsHeld) return Direction.Right;

            var keyboard = Keyboard.current;
            if (keyboard == null) return null;
            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) return Direction.Up;
            if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) return Direction.Down;
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) return Direction.Left;
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) return Direction.Right;
            return null;
        }

        // ------------------------------------------------------------------ 월드(타일맵·플레이어·카메라)

        private static Vector3 CellCenter(Vector2Int cell) => new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

        private void SnapPlayer()
        {
            moving = false;
            player.position = CellCenter(walker.Position);
            playerRenderer.sprite = PlaceholderArt.Player(walker.Facing);
        }

        private void BuildWorld(FieldMap map)
        {
            var gridGo = new GameObject("FieldGrid", typeof(Grid));
            gridGo.transform.SetParent(transform, false);
            var tilemapGo = new GameObject("Tiles", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapGo.transform.SetParent(gridGo.transform, false);
            tilemap = tilemapGo.GetComponent<Tilemap>();

            var tiles = new System.Collections.Generic.Dictionary<FieldTile, Tile>();
            foreach (FieldTile kind in Enum.GetValues(typeof(FieldTile))) tiles[kind] = MakeTile(PlaceholderArt.ForTile(kind));
            openedChestTile = MakeTile(PlaceholderArt.ForTile(FieldTile.Chest, openedChest: true));

            for (int x = 0; x < map.Width; x++)
            for (int y = 0; y < map.Height; y++)
            {
                var cell = new Vector2Int(x, y);
                var kind = map.Get(cell);
                bool opened = kind == FieldTile.Chest && session.World.IsChestOpened(area.ChestId(cell));
                tilemap.SetTile(new Vector3Int(x, y, 0), opened ? openedChestTile : tiles[kind]);
            }

            var playerGo = new GameObject("Player", typeof(SpriteRenderer));
            playerGo.transform.SetParent(transform, false);
            player = playerGo.transform;
            playerRenderer = playerGo.GetComponent<SpriteRenderer>();
            playerRenderer.sortingOrder = 10;

            cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
                camGo.transform.SetParent(transform, false); // 직접 만든 카메라는 필드와 함께 정리
                cam = camGo.GetComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PlaceholderArt.OutsideMap;
        }

        private static Tile MakeTile(Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            return tile;
        }

        private void UpdateCamera()
        {
            if (cam == null) return;
            float visibleHeight = tilesAcross / Mathf.Max(0.1f, cam.aspect);
            cam.orthographicSize = visibleHeight / 2f;
            // 아래쪽은 가상 패드가 가리므로 플레이어가 화면 높이 60% 지점에 오게 카메라를 내린다
            var target = player.position + Vector3.down * (visibleHeight * 0.10f);
            cam.transform.position = new Vector3(target.x, target.y, -10f);
        }

        // ------------------------------------------------------------------ HUD

        private void BuildHud()
        {
            UiKit.EnsureEventSystem();
            var canvasGo = new GameObject("FieldHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            hudRoot = UiKit.Stretch("SafeArea", canvasGo.transform);
            UiKit.ApplySafeArea(hudRoot);

            // 상단: 지역 이름 · 도감 진행 · 도감 버튼
            var top = UiKit.Panel("TopBar", hudRoot, new Color(0, 0, 0, 0.55f), 0, 0.94f, 1, 1);
            top.raycastTarget = false;
            areaLabel = UiKit.Label("Area", top.transform, "", 38, Palette.Gold, 0.03f, 0, 0.32f, 1,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            dexLabel = UiKit.Label("DexProgress", top.transform, "", 30, Palette.Text, 0.32f, 0, 0.78f, 1,
                TextAnchor.MiddleRight);
            var dexButton = UiKit.MakeButton("DexButton", top.transform, "도감", Palette.Button, 34, 0.8f, 0.08f, 0.97f, 0.92f);
            dexButton.onClick.AddListener(OpenDex);

            // 파티 HP
            var strip = UiKit.Rect("PartyStrip", hudRoot, 0, 0.875f, 1, 0.935f);
            badgeNames = new Text[GameSession.MaxPartySize];
            badgeFills = new RectTransform[GameSession.MaxPartySize];
            badgeFillImages = new Image[GameSession.MaxPartySize];
            for (int i = 0; i < GameSession.MaxPartySize; i++)
            {
                var badge = UiKit.Panel($"Badge_{i}", strip, new Color(0, 0, 0, 0.55f), i / 3f, 0, (i + 1) / 3f, 1);
                badge.raycastTarget = false;
                UiKit.Pad(badge.rectTransform, 6, 0, 6, 0);
                badgeNames[i] = UiKit.Label("Name", badge.transform, "", 28, Palette.Text, 0.04f, 0.42f, 0.96f, 1,
                    TextAnchor.MiddleCenter, FontStyle.Bold, true, 16);
                var back = UiKit.Panel("HpBack", badge.transform, new Color(0.04f, 0.05f, 0.09f), 0.06f, 0.14f, 0.94f, 0.36f);
                back.raycastTarget = false;
                badgeFillImages[i] = UiKit.Panel("HpFill", back.transform, Palette.Good);
                badgeFillImages[i].raycastTarget = false;
                badgeFills[i] = badgeFillImages[i].rectTransform;
            }

            // 알림
            var toast = UiKit.Panel("Toast", hudRoot, new Color(0, 0, 0, 0.78f), 0.06f, 0.29f, 0.94f, 0.37f);
            toast.raycastTarget = false;
            toastPanel = toast.gameObject;
            toastText = UiKit.Label("Text", toast.transform, "", 34, Palette.Text, 0, 0, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Normal, true, 20);
            UiKit.Pad(toastText.rectTransform, 20, 6, 20, 6);
            toastPanel.SetActive(false);

            // 가상 방향 패드 (한 손 조작용으로 아래 가운데)
            var pad = UiKit.Rect("Pad", hudRoot, 0.25f, 0.02f, 0.75f, 0.27f);
            padUp = PadButton(pad, "Pad_Up", "▲", 0.35f, 0.67f, 0.65f, 1f);
            padDown = PadButton(pad, "Pad_Down", "▼", 0.35f, 0f, 0.65f, 0.33f);
            padLeft = PadButton(pad, "Pad_Left", "◀", 0f, 0.335f, 0.3f, 0.665f);
            padRight = PadButton(pad, "Pad_Right", "▶", 0.7f, 0.335f, 1f, 0.665f);

            // 조우 연출용 번쩍임
            flash = UiKit.Panel("EncounterFlash", hudRoot, Color.clear);
            flash.raycastTarget = false;
            flash.gameObject.SetActive(false);

            dexView = DexView.Create(hudRoot);
            evolutionView = EvolutionView.Create(hudRoot);
            shopView = ShopView.Create(hudRoot);
        }

        private static HoldButton PadButton(RectTransform parent, string name, string arrow,
            float minX, float minY, float maxX, float maxY)
        {
            var image = UiKit.Panel(name, parent, new Color(1, 1, 1, 0.22f), minX, minY, maxX, maxY);
            UiKit.Label("Arrow", image.transform, arrow, 64, new Color(1, 1, 1, 0.9f), 0, 0, 1, 1);
            return image.gameObject.AddComponent<HoldButton>();
        }

        private void BuildBattle()
        {
            var battleGo = new GameObject("Battle");
            battleGo.transform.SetParent(transform, false);
            battle = battleGo.AddComponent<BattleScreen>();
            battle.Configure(area.Encounters, area.Words, session, animationScale, battleConfig, saveProgress, loop: false);
        }

        private void OpenDex()
        {
            if (inBattle || transitioning || moving || IsPanelOpen) return;
            var completed = session.ClaimDexRewards(new[] { area.Words });
            if (completed.Count > 0)
            {
                saveProgress?.Invoke();
                ShowToast($"★ {area.Words.RegionName} 도감 완성! 보상을 받았다", 3f);
            }
            dexView.Show(area.Words, session, DateTime.UtcNow);
        }

        private void RefreshHud()
        {
            areaLabel.text = area.DisplayName;
            var progress = Dex.GetProgress(area.Words, session.Vocabulary);
            dexLabel.text = $"발견 {progress.Discovered}/{progress.Total}   {session.Inventory.Gold}G";

            for (int i = 0; i < badgeNames.Length; i++)
            {
                bool has = i < session.Party.Count;
                badgeNames[i].transform.parent.gameObject.SetActive(has);
                if (!has) continue;
                var monster = session.Party[i];
                float ratio = Mathf.Clamp01((float)monster.CurrentHp / Mathf.Max(1, monster.Stats.MaxHp));
                badgeNames[i].text = $"{monster.DisplayName} Lv{monster.Level}";
                badgeFills[i].anchorMax = new Vector2(ratio, 1);
                badgeFillImages[i].color = ratio > 0.5f ? Palette.Good : ratio > 0.25f ? Palette.Gold : Palette.Bad;
            }
        }

        private void ShowToast(string message, float seconds = 2f)
        {
            toastText.text = message;
            toastPanel.SetActive(true);
            toastUntil = Time.unscaledTime + seconds;
        }

        private void HideToast() => toastPanel.SetActive(false);

        private void UpdateToast()
        {
            if (toastPanel.activeSelf && Time.unscaledTime >= toastUntil) toastPanel.SetActive(false);
        }
    }
}
