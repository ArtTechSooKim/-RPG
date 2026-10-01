using System;
using UnityEngine;
using WordRPG.Monsters;
using WordRPG.Save;

namespace WordRPG.Game
{
    // 게임 진행 상태(GameSession)와 저장을 맡는 오브젝트. 씬이 바뀌어도 유지된다.
    // 자동 저장: 문제에 답할 때마다, 전투 결과 후(화면 쪽에서 Save 호출), 앱이 백그라운드로 갈 때, 종료될 때
    public class GameManager : MonoBehaviour
    {
        // 테스트가 실제 세이브 파일을 건드리지 않도록 저장 폴더를 바꾸는 용도. 평소에는 null
        public static string SaveDirectoryOverride;

        [SerializeField] private GameDatabase database;
        [SerializeField] private MonsterSpecies[] starterParty;
        [SerializeField] private int starterLevel = 3;

        private SaveSystem saveSystem;

        public static GameManager Instance { get; private set; }
        public GameSession Session { get; private set; }
        public bool LoadedFromSave { get; private set; }
        public string StatusMessage { get; private set; } = "";
        public string SaveDirectory => saveSystem?.Directory;
        public GameDatabase Database => database;

        // 코드로 만들 때(테스트) 비활성 오브젝트에 붙이고 Configure → SetActive(true) 순서로 쓴다
        public void Configure(GameDatabase db, MonsterSpecies[] starters, int level)
        {
            database = db;
            starterParty = starters;
            starterLevel = level;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

            if (database == null) Debug.LogError("[GameManager] GameDatabase가 비어 있어 세이브의 몬스터를 불러올 수 없습니다");
            saveSystem = new SaveSystem(SaveDirectoryOverride ?? Application.persistentDataPath);
            LoadOrCreate();
        }

        private void LoadOrCreate()
        {
            var result = saveSystem.Load();
            if (result.Data != null)
            {
                Session = GameSession.FromSaveData(result.Data, database, starterParty, starterLevel);
                LoadedFromSave = true;
                StatusMessage = $"이어하기 — 발견한 단어 {Session.Vocabulary.DiscoveredCount}개, " +
                                $"전투 {Session.Record.BattlesWon}승 {Session.Record.BattlesLost}패";
                if (result.UsedBackup)
                {
                    StatusMessage += " (백업에서 복구)";
                    Debug.LogWarning($"[GameManager] 세이브가 손상되어 백업에서 불러왔습니다: {result.Error}");
                }
                foreach (var warning in Session.LoadWarnings) Debug.LogWarning($"[GameManager] {warning}");
                return;
            }

            if (result.Error != null)
            {
                Debug.LogError($"[GameManager] 세이브를 읽을 수 없어 새 게임으로 시작합니다. 원본은 보관합니다: {result.Error}");
                saveSystem.QuarantineCorrupt();
            }
            Session = GameSession.NewGame(starterParty, starterLevel);
            LoadedFromSave = false;
            StatusMessage = "새 게임 시작!";
        }

        public void Save()
        {
            if (Session == null) return;
            try
            {
                saveSystem.Save(Session.ToSaveData(DateTime.UtcNow));
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] 저장 실패: {e.Message}");
            }
        }

        // 세이브를 지우고 처음부터 (설정 화면이 생기면 연결)
        public void StartNewGame()
        {
            saveSystem.Delete();
            Session = GameSession.NewGame(starterParty, starterLevel);
            LoadedFromSave = false;
            StatusMessage = "새 게임 시작!";
            Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
