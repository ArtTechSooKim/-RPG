using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using WordRPG.Battle;
using WordRPG.Field;
using WordRPG.Game;
using WordRPG.Monsters;

namespace WordRPG.UI
{
    // 탑다운 필드: 한 칸씩 이동, 풀숲 조우 → 전투(BattleScreen을 위에 덮음) → 원래 자리로 복귀.
    // 출입구(D)를 밟으면 다른 지역으로 (같은 씬에서 맵만 바꿔 그림).
    // 보물상자·회복의 샘·진화의 제단·상점·보스는 '부딪혀서' 사용. 입력은 화면 아래 가상 패드 + 키보드(방향키/WASD).
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
        private GameDatabase database;
        private GameSettings settings;
        private Action saveProgress;
        private Action saveSettings;
        private Action deleteSave;
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
        private float walkTime; // 걷기 애니메이션 시계 (멈추면 서 있는 모습)
        private Vector3 moveFrom, moveTo;
        private float interactCooldown;

        private Camera cam;
        private Transform player;
        private SpriteRenderer playerRenderer;
        private Tilemap tilemap;
        private readonly Dictionary<string, Tile> tileCache = new Dictionary<string, Tile>();
        private BattleScreen battle;
        private bool bossBattle;

        private RectTransform hudRoot;
        private Text areaLabel, dexLabel, toastText;
        private GameObject toastPanel;
        private float toastUntil;
        private Image flash;
        private HoldButton padUp, padDown, padLeft, padRight;
        private DexView dexView;
        private EvolutionView evolutionView;
        private ShopView shopView;
        private InventoryView inventoryView;
        private SettingsView settingsView;
        private MinimapView minimap;
        private MapView mapView;
        private Text[] badgeNames;
        private Text[] badgeInitials;
        private Image[] badgeSprites;
        private RectTransform[] badgeFills;
        private Image[] badgeFillImages;

        public Vector2Int PlayerCell => walker.Position;
        public Direction Facing => walker.Facing;
        public bool IsMoving => moving;
        public bool IsInBattle => inBattle || transitioning;
        public BattleScreen Battle => battle;
        public GameSession Session => session;
        public FieldArea CurrentArea => area;
        public bool IsPanelOpen => dexView.IsOpen || evolutionView.IsOpen || shopView.IsOpen
                                   || inventoryView.IsOpen || settingsView.IsOpen || mapView.IsOpen;
        public MinimapView Minimap => minimap;
        public string ToastMessage => toastPanel != null && toastPanel.activeSelf ? toastText.text : "";

        // 코드로 만들 때(테스트) Start 전에 호출. session을 안 주면 GameManager 것을 쓴다
        // database: 세이브의 마지막 지역이 다른 곳이면 거기서 시작하기 위해 지역을 찾는 데 쓴다 (소지품 화면의 아이템 찾기에도)
        // gameSettings / onSettingsChanged / onDeleteSave: 설정 화면용. 안 주면 GameManager 것
        public void Configure(FieldArea fieldArea, GameSession gameSession = null, Action onSave = null,
            float step = 0.16f, float animScale = 1f, BattleConfig config = null, GameDatabase gameDatabase = null,
            GameSettings gameSettings = null, Action onSettingsChanged = null, Action onDeleteSave = null)
        {
            area = fieldArea;
            session = gameSession;
            database = gameDatabase;
            saveProgress = onSave;
            stepDuration = step;
            animationScale = animScale;
            if (config != null) battleConfig = config;
            settings = gameSettings;
            saveSettings = onSettingsChanged;
            deleteSave = onDeleteSave;
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
                if (database == null) database = manager.Database;
                if (settings == null) settings = manager.Settings;
                if (saveSettings == null) saveSettings = manager.SaveSettings;
                // 설정에서 저장 데이터를 지우면 타이틀로 (타이틀 씬이 없으면 이 씬을 처음부터)
                if (deleteSave == null) deleteSave = () =>
                {
                    manager.DeleteSave();
                    GameManager.ReturnToTitle();
                };
                manager.MarkPlaying();
            }
            if (settings == null) settings = new GameSettings();
            if (session == null || area == null)
            {
                Debug.LogError("[FieldScreen] GameManager(또는 Configure의 session) / area 가 비어 있습니다");
                return false;
            }

            // 세이브의 마지막 위치가 다른 지역(예: 던전)이면 그 지역에서 시작
            var startArea = area;
            string savedAreaId = session.World.AreaId;
            if (database != null && savedAreaId != null && savedAreaId != area.AreaId)
                startArea = database.FindArea(savedAreaId) ?? area;

            FieldMap map;
            try
            {
                map = startArea.Map;
            }
            catch (FormatException e)
            {
                Debug.LogError($"[FieldScreen] {startArea.name} 맵 오류: {e.Message}");
                return false;
            }

            initialized = true;
            rng = new System.Random();
            area = startArea;

            CreateWorldObjects();
            BuildHud();
            BuildBattle();

            // 저장된 위치가 이 지역의 걸을 수 있는 칸이면 거기서, 아니면 시작 위치에서
            var spawn = session.World.TryGetPosition(area.AreaId, out var saved) && map.IsWalkable(saved) ? saved : map.Start;
            EnterArea(area, spawn);

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
                walkTime += Time.deltaTime;
                playerRenderer.sprite = PlayerArt.Get(walker.Facing, Mathf.FloorToInt(walkTime * PlayerArt.FramesPerSecond));
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
            else if (walkTime > 0f)
            {
                walkTime = 0f;
                playerRenderer.sprite = PlayerArt.Get(walker.Facing, 0);
            }
        }

        // ------------------------------------------------------------------ 이동·상호작용

        private void TryStep(Direction direction)
        {
            var outcome = walker.TryStep(direction);
            playerRenderer.sprite = PlayerArt.Get(walker.Facing, 0);

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
                case FieldTile.Boss: ChallengeBoss(); break;
            }
        }

        private void ChallengeBoss()
        {
            var boss = area.Boss;
            if (boss == null)
            {
                ShowToast("아무도 없다.");
                return;
            }
            string name = boss.Species.DisplayName;
            if (session.World.IsBossDefeated(area.BossId))
            {
                ShowToast($"{UiKit.WithJosa(name, "이", "가")} 있던 자리에\n펼쳐진 책이 빛나고 있다.");
                return;
            }
            var enemies = new List<MonsterInstance> { new MonsterInstance(boss.Species, boss.Level) };
            StartCoroutine(Encounter(enemies, $"보스 출현! {name} Lv{boss.Level} — 정답으로 맞서라!", true));
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
            minimap.Picture.SetPlayer(walker.Position);
            var tile = walker.Map.Get(walker.Position);
            if (tile == FieldTile.Door)
            {
                var exit = area.GetExit(walker.Position);
                if (exit != null && exit.Target != null) StartCoroutine(UseDoor(exit));
                else ShowToast("문이 굳게 닫혀 있다.");
                return;
            }
            if (encounterCounter.OnStep(tile == FieldTile.Grass, rng)) StartCoroutine(Encounter());
        }

        // 출입구: 화면을 어둡게 → 도착 지역의 해당 출입구 칸에 나타남 → 밝게.
        // 도착은 '걸음'이 아니라서 바로 되돌아가지 않는다 (한 칸 벗어났다 다시 밟아야 이동)
        private IEnumerator UseDoor(AreaExit exit)
        {
            var target = exit.Target;
            var doors = target.Map.Doors;
            if (exit.TargetDoorIndex < 0 || exit.TargetDoorIndex >= doors.Count)
            {
                Debug.LogError($"[FieldScreen] {area.name}의 출입구가 {target.name}의 {exit.TargetDoorIndex}번 D를 가리키지만 없음");
                ShowToast("문이 굳게 닫혀 있다.");
                yield break;
            }

            transitioning = true;
            Sound.Play(Sfx.Door);
            flash.gameObject.SetActive(true);
            float half = 0.25f * animationScale;
            for (float t = 0; t < half; t += Time.unscaledDeltaTime)
            {
                flash.color = new Color(0, 0, 0, t / half);
                yield return null;
            }

            EnterArea(target, doors[exit.TargetDoorIndex]);
            saveProgress?.Invoke();

            for (float t = 0; t < half; t += Time.unscaledDeltaTime)
            {
                flash.color = new Color(0, 0, 0, 1f - t / half);
                yield return null;
            }
            flash.gameObject.SetActive(false);
            transitioning = false;
            ShowToast(area.DisplayName, 1.5f);
        }

        private void OpenChest(Vector2Int cell)
        {
            var result = session.OpenChest(area, cell);
            if (result.WasEmpty)
            {
                ShowToast("빈 상자다.");
                return;
            }

            tilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), TileFor(FieldTile.Chest, true));
            Sound.Play(Sfx.Coin);
            minimap.Redraw();
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
            Sound.Play(Sfx.Fountain);
            ShowToast("회복의 샘 — 파티가 모두 회복되었다!");
        }

        private IEnumerator Encounter(List<MonsterInstance> enemies = null, string intro = null, bool boss = false)
        {
            transitioning = true;
            Sound.Play(Sfx.Encounter);
            Sound.PlayMusic(boss ? Music.Boss : Music.Battle);
            flash.gameObject.SetActive(true);
            float duration = 0.4f * animationScale;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                flash.color = new Color(1, 1, 1, Mathf.PingPong(k * 3f, 1f) * 0.85f);
                yield return null;
            }
            flash.gameObject.SetActive(false);

            enemies = enemies ?? area.Encounters.Roll(rng);
            transitioning = false;
            inBattle = true;
            bossBattle = boss;
            HideToast();
            battle.BeginBattle(enemies, area.Words, OnBattleFinished, intro, area.Theme, boss);
        }

        private Music AreaMusic => area.Theme == FieldTheme.Library ? Music.Library : Music.Meadow;

        private void OnBattleFinished(bool won)
        {
            inBattle = false;
            Sound.PlayMusic(AreaMusic);
            encounterCounter.Reset();
            bool wasBoss = bossBattle;
            bossBattle = false;

            if (won && wasBoss)
            {
                session.World.MarkBossDefeated(area.BossId);
                var bossCell = walker.Map.BossPosition.Value;
                tilemap.SetTile(new Vector3Int(bossCell.x, bossCell.y, 0), TileFor(FieldTile.Boss, true));
                minimap.Redraw();
                saveProgress?.Invoke();
                string name = area.Boss.Species.DisplayName;
                ShowToast($"★ {UiKit.WithJosa(name, "을", "를")} 물리쳤다!\n{area.DisplayName}에 잊혀진 기억이 돌아왔다", 4f);
            }
            else if (!won)
            {
                // 패배: 전투 화면이 이미 파티를 회복시켰다. 이 지역의 시작 위치로 돌아간다
                walker.WarpTo(walker.Map.Start);
                SnapPlayer();
                session.World.SetPosition(area.AreaId, walker.Position);
                saveProgress?.Invoke();
                ShowToast($"{area.DisplayName} 시작 지점으로 돌아왔다. 파티가 회복되었다!");
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
            playerRenderer.sprite = PlayerArt.Get(walker.Facing, 0);
            minimap?.Picture.SetPlayer(walker.Position);
        }

        // 지역을 바꿔 그린다 (처음 시작할 때, 출입구를 지날 때)
        private void EnterArea(FieldArea newArea, Vector2Int position)
        {
            area = newArea;
            var map = area.Map;
            walker = new FieldWalker(map, position);
            encounterCounter = new EncounterCounter(area.EncounterRate, area.MinStepsBetweenEncounters);
            session.World.SetPosition(area.AreaId, position);

            tilemap.ClearAllTiles();
            for (int x = 0; x < map.Width; x++)
            for (int y = 0; y < map.Height; y++)
            {
                var cell = new Vector2Int(x, y);
                var kind = map.Get(cell);
                bool done = kind == FieldTile.Chest && session.World.IsChestOpened(area.ChestId(cell))
                            || kind == FieldTile.Boss && session.World.IsBossDefeated(area.BossId);
                tilemap.SetTile(new Vector3Int(x, y, 0), TileFor(kind, done));
            }

            cam.backgroundColor = PlaceholderArt.OutsideColor(area.Theme);
            if (!inBattle) Sound.PlayMusic(AreaMusic);
            minimap.SetArea(map, area.Theme, IsCellDone);
            SnapPlayer();
            UpdateCamera();
            RefreshHud();
        }

        private Tile TileFor(FieldTile kind, bool done)
        {
            string key = $"{area.Theme}_{kind}_{done}";
            if (!tileCache.TryGetValue(key, out var tile))
            {
                tile = MakeTile(FieldArt.ForTile(kind, area.Theme, done));
                tileCache[key] = tile;
            }
            return tile;
        }

        private void CreateWorldObjects()
        {
            var gridGo = new GameObject("FieldGrid", typeof(Grid));
            gridGo.transform.SetParent(transform, false);
            var tilemapGo = new GameObject("Tiles", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapGo.transform.SetParent(gridGo.transform, false);
            tilemap = tilemapGo.GetComponent<Tilemap>();

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

            // 상단: 지역 이름 · 도감 진행 · 골드
            var top = UiKit.Panel("TopBar", hudRoot, Palette.Scrim, 0, 0.94f, 1, 1);
            top.raycastTarget = false;
            areaLabel = UiKit.Display(UiKit.Label("Area", top.transform, "", 44, Palette.Gold, 0.03f, 0, 0.4f, 1,
                TextAnchor.MiddleLeft, FontStyle.Normal, true, 24));
            dexLabel = UiKit.Label("DexProgress", top.transform, "", 30, Palette.Text, 0.4f, 0, 0.97f, 1,
                TextAnchor.MiddleRight);

            // 왼쪽 위 미니맵 (누르면 큰 지도). 맵 데이터에서 그리므로 지역이 늘어나도 자동
            minimap = MinimapView.Create(hudRoot);
            minimap.Button.onClick.AddListener(OpenMap);

            // 오른쪽 세로 메뉴 (Figma 'Field — HUD (메뉴 버튼)'): 도감 · 가방 · 설정
            var menu = UiKit.Rect("Menu", hudRoot, 1, 0.865f, 1, 0.865f);
            menu.pivot = new Vector2(1, 1);
            menu.sizeDelta = new Vector2(120, 3 * 120 + 2 * 16);
            menu.anchoredPosition = new Vector2(-24, 0);
            var entries = new (string name, string icon, string label, UnityEngine.Events.UnityAction open)[]
            {
                ("DexButton", "dex", "도감", OpenDex),
                ("BagButton", "bag", "가방", OpenBag),
                ("SettingsButton", "settings", "설정", OpenSettings),
            };
            for (int i = 0; i < entries.Length; i++)
            {
                float topY = 1f - i * (136f / 392f);
                var button = UiKit.IconButton(entries[i].name, menu, entries[i].icon, entries[i].label, Palette.Scrim,
                    0, topY - 120f / 392f, 1, topY);
                button.onClick.AddListener(entries[i].open);
            }

            // 파티 HP
            var strip = UiKit.Rect("PartyStrip", hudRoot, 0, 0.875f, 1, 0.935f);
            badgeNames = new Text[GameSession.MaxPartySize];
            badgeInitials = new Text[GameSession.MaxPartySize];
            badgeSprites = new Image[GameSession.MaxPartySize];
            badgeFills = new RectTransform[GameSession.MaxPartySize];
            badgeFillImages = new Image[GameSession.MaxPartySize];
            for (int i = 0; i < GameSession.MaxPartySize; i++)
            {
                // Figma 'Party Badge': 얼굴 원 + 이름 + HP 바
                var badge = UiKit.RoundPanel($"Badge_{i}", strip, Palette.Scrim, UiKit.RadiusMd, i / 3f, 0, (i + 1) / 3f, 1);
                badge.raycastTarget = false;
                UiKit.Pad(badge.rectTransform, 8, 0, 8, 0);
                var avatar = UiKit.Pill(UiKit.Panel("Avatar", badge.transform, Palette.PanelLight, 0, 0.62f, 0, 0.62f));
                avatar.raycastTarget = false;
                avatar.rectTransform.pivot = new Vector2(0, 0.5f);
                avatar.rectTransform.sizeDelta = new Vector2(48, 48);
                avatar.rectTransform.anchoredPosition = new Vector2(14, 0);
                badgeInitials[i] = UiKit.Display(UiKit.Label("Initial", avatar.transform, "", 30, Palette.Text, 0, 0, 1, 1));
                badgeSprites[i] = UiKit.IconImage("Sprite", avatar.transform, null, 0.05f, 0.05f, 0.95f, 0.95f);
                badgeNames[i] = UiKit.Label("Name", badge.transform, "", 28, Palette.Text, 0, 0.4f, 1, 0.86f,
                    TextAnchor.MiddleLeft, FontStyle.Bold, true, 16);
                badgeNames[i].rectTransform.offsetMin = new Vector2(74, 0);
                badgeNames[i].rectTransform.offsetMax = new Vector2(-8, 0);
                var back = UiKit.Pill(UiKit.Panel("HpBack", badge.transform, Palette.Track, 0.05f, 0.14f, 0.95f, 0.32f));
                back.raycastTarget = false;
                badgeFillImages[i] = UiKit.Pill(UiKit.Panel("HpFill", back.transform, Palette.Good));
                badgeFillImages[i].raycastTarget = false;
                badgeFills[i] = badgeFillImages[i].rectTransform;
            }

            // 알림
            var toast = UiKit.RoundPanel("Toast", hudRoot, Palette.Scrim, UiKit.RadiusMd, 0.06f, 0.29f, 0.94f, 0.37f);
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
            evolutionView = EvolutionView.Create(hudRoot, animationScale);
            shopView = ShopView.Create(hudRoot);
            inventoryView = InventoryView.Create(hudRoot);
            settingsView = SettingsView.Create(hudRoot);
            mapView = MapView.Create(hudRoot);
        }

        private static HoldButton PadButton(RectTransform parent, string name, string arrow,
            float minX, float minY, float maxX, float maxY)
        {
            var image = UiKit.RoundPanel(name, parent, new Color(1, 1, 1, 0.22f), UiKit.RadiusMd, minX, minY, maxX, maxY);
            UiKit.Label("Arrow", image.transform, arrow, 52, new Color(1, 1, 1, 0.92f), 0, 0, 1, 1, TextAnchor.MiddleCenter, FontStyle.Bold);
            return image.gameObject.AddComponent<HoldButton>();
        }

        private void BuildBattle()
        {
            var battleGo = new GameObject("Battle");
            battleGo.transform.SetParent(transform, false);
            battle = battleGo.AddComponent<BattleScreen>();
            battle.Configure(area.Encounters, area.Words, session, animationScale, battleConfig, saveProgress, loop: false);
        }

        // 메뉴는 걷는 중·전투 중·다른 창이 열려 있을 때는 열지 않는다
        private bool CanOpenMenu => !(inBattle || transitioning || moving || IsPanelOpen);

        private void OpenMap()
        {
            if (CanOpenMenu) mapView.Show(area, minimap.Texture, walker.Position, session);
        }

        // 미니맵에서 '끝난 곳' (연 상자, 쓰러뜨린 보스)
        private bool IsCellDone(Vector2Int cell)
        {
            switch (walker.Map.Get(cell))
            {
                case FieldTile.Chest: return session.World.IsChestOpened(area.ChestId(cell));
                case FieldTile.Boss: return session.World.IsBossDefeated(area.BossId);
                default: return false;
            }
        }

        private void OpenBag()
        {
            if (CanOpenMenu) inventoryView.Show(session, database);
        }

        private void OpenSettings()
        {
            if (CanOpenMenu) settingsView.Show(settings, saveSettings, deleteSave);
        }

        private void OpenDex()
        {
            if (!CanOpenMenu) return;
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
                var sprite = monster.Species.Sprite;
                badgeSprites[i].sprite = sprite;
                badgeSprites[i].enabled = sprite != null;
                badgeInitials[i].enabled = sprite == null;
                badgeInitials[i].text = monster.DisplayName.Length > 0 ? monster.DisplayName.Substring(0, 1) : "?";
                badgeFillImages[i].enabled = ratio > 0f;
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
