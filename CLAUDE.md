# CLAUDE.md — WordRPG (영단어 RPG)

> Claude Code가 세션 시작 시 자동으로 읽는 프로젝트 컨텍스트.
> 결정 사항·게임 규칙·진행 현황은 `Docs/GDD.md`, 원본 기획은 `Docs/PRD.txt`. 큰 기능 작업 전에 GDD를 확인할 것.

## 개발현황보고서 (사용자 요청 — 반드시 지킬 것)

- 세션을 시작하면 루트의 `개발현황보고서.txt`를 먼저 읽고 어디까지 했는지 파악한다
- **작업을 할 때마다** 같은 파일을 갱신한다 (작업을 마치고 사용자에게 보고하기 전에):
  1. 맨 위 `[현재 상태]`: 마지막 업데이트 날짜, 마지막 커밋, 지금 해볼 수 있는 것, PRD STEP 진행도, 아직 안 되는 것
  2. `[다음 작업]`, `[사용자가 직접 해야 할 일]`: 끝난 항목은 지우거나 [x] 처리, 새 항목 추가
  3. `[작업 로그]` 맨 위에 새 항목: `[날짜] #번호 제목 (커밋 해시)` + 무엇을 했는지, 검증 결과, 남은 문제
- 사용자가 나중에 읽고 이어서 시작할 수 있게 쉬운 한국어로, 코드 용어는 최소화. 형식은 일반 텍스트(.txt)

## 프로젝트 개요

- **장르**: 몬스터 육성 턴제 RPG + 영단어 학습. "공부하려고 켜는 게임"이 아니라 "게임하다 보니 단어를 외우고 있는 게임"
- **엔진**: Unity 6000.3.11f1 (2D, Built-in RP) / **플랫폼**: 모바일, **세로 고정**, Android 우선 / **개발**: 1인
- **핵심 루프**: 탐험 → 랜덤 조우 → 전투(스킬마다 단어 문제) → 보상 → 성장·진화 → 새 지역·어려운 단어
- **현재 단계**: MVP — 마을 1, 필드 1, 던전 1, 아군 3종(+진화) + 적 3종, 단어 100개

## 기술 규칙 (반드시 준수)

1. **게임 데이터는 ScriptableObject**: 몬스터, 스킬, 아이템, 출현표, 단어장. 하드코딩 금지
   - 단어는 `Assets/Data/Words/*.csv`를 고치면 같은 이름의 `WordDatabase` .asset이 자동 갱신됨
2. **로직과 UI 분리**: `Words/ Monsters/ Items/ Battle/`은 순수 C# (MonoBehaviour 없음). UI는 `BattleEngine`이 돌려주는 `BattleEvent` 목록을 연출만 한다
3. **새 로직에는 EditMode 테스트**: `Assets/Tests/EditMode/`. SO는 `TestData` 도우미로 생성
4. **시간·랜덤은 주입**: 로직에서 `DateTime.UtcNow`, `UnityEngine.Random` 직접 사용 금지 → `DateTime nowUtc` / `System.Random` 파라미터로 받기 (테스트 결정성)
5. **저장 데이터는 id 문자열로 참조**: wordId, itemId, speciesId. 에셋 이름이 바뀌어도 세이브가 깨지지 않게
   - 몬스터·아이템을 새로 만들면 `WordRPG > Data > Refresh Game Database` 실행 (안 하면 세이브에서 불러올 수 없음, DataIntegrityTests가 잡음)
   - 이미 출시된 id는 바꾸지 말 것. SaveData에 필드 추가는 자유(예전 세이브는 기본값), 기존 필드 의미를 바꿀 때만 version 올리고 변환
   - 게임 진행 상태는 `GameManager.Instance.Session`에서 얻고, 바뀌면 `GameManager.Save()` 호출
   - 새 지역(단어장)은 regionId·regionName·징표 아이템(종류 Keepsake)·골드를 지정해야 함 (RegionDataTests가 검사)
   - 필드 맵은 FieldArea의 글자 맵 (GDD 7장). 상자 수 = 내용물 수, 모든 상자·샘 도달 가능 (AreaDataTests가 검사)
6. **네이밍**: PascalCase 클래스/메서드/프로퍼티, camelCase 필드. SO 필드는 `[SerializeField] private` + 읽기 전용 프로퍼티. 클래스명은 영문, 화면 표시명은 한국어 필드(`displayName = "펜촉이"`). 주석은 한국어
7. **Git LFS 사용 중**: 이미지·오디오·폰트·네이티브 플러그인. 새 바이너리 타입 추가 시 `.gitattributes` 확인
8. **입력은 Input System 전용**: EventSystem에 `InputSystemUIInputModule` 사용 (`StandaloneInputModule` 금지)
9. **한국어 조사**: 이름 뒤 조사는 `UiKit.WithJosa(name, "이", "가")`로 (펜촉이가 / 책껍질이, 깃펜기사로 / 백과거북으로)
10. **세로 화면 기준 UI**: 1080×1920 레퍼런스, 한 손 조작, 4지선다 버튼은 화면 하단

## 폴더 구조

```
Assets/
  Scripts/              WordRPG.asmdef (런타임)
    Core/               CSV 파서 등 공용
    Words/              단어, 숙련도(VocabularyProgress), 출제(WordSelector), 4지선다(QuizGenerator)
    Monsters/           MonsterSpecies·SkillData SO, MonsterInstance, LevelCurve, Evolution
    Items/              ItemData SO, Inventory, ShopData SO + Shop(구매 규칙)
    Battle/             BattleEngine, BattleFormulas, BattleReward
    Field/              FieldMap(맵 글자→격자), FieldWalker(이동), EncounterCounter(조우), FieldArea(지역 SO), EncounterTable
    Game/               GameSession(진행 상태 전체), GameManager(씬 간 유지 + 자동 저장), GameDatabase(id→에셋), PlayerRecord, Dex(도감 규칙)
    Save/               SaveData(JSON 형식), SaveSystem(임시파일+백업으로 안전 저장)
    UI/                 FieldScreen(필드·HUD·가상 패드), BattleScreen(필드 위에 덮이는 전투, 단독 연습 모드도 있음),
                        DexView(도감), EvolutionView(진화의 제단), ShopView(상점), UnitView, HoldButton,
                        PlaceholderArt(임시 도트 생성), UiKit(+ WithJosa 한국어 조사) — 세로 1080x1920, OS 한글 폰트
    Editor/             WordRPG.Editor.asmdef — CSV 임포터, 샘플 데이터 생성기
  Tests/EditMode/       WordRPG.Tests.EditMode.asmdef (로직)
  Tests/PlayMode/       WordRPG.Tests.PlayMode.asmdef (UI 버튼을 눌러 전투 한 판 진행)
  Data/                 Words, Monsters, Skills, Items, Encounters, Areas, Shops (SO 에셋), GameDatabase.asset
  Scenes/               Field.unity (메인, 빌드 첫 씬), Battle.unity (전투만 반복하는 연습 씬)
Docs/                   PRD.txt, GDD.md
```

## 명령어

에디터 메뉴: `WordRPG > Data > Import Word CSVs`, `WordRPG > Data > Create Sample Data` (없는 에셋만 생성), `WordRPG > Data > Refresh Game Database`, `WordRPG > Scenes > Create All Scenes` (필드·전투 씬 다시 생성), `WordRPG > Save > Delete Save Data / Open Save Folder`

게임 실행: `Assets/Scenes/Field.unity`를 열고 Play (Game 뷰를 세로 비율로, 예: 1080x1920). 전투만 연습: `Battle.unity`

배치모드 (Unity 에디터가 이 프로젝트를 열고 있으면 실행 불가):

```bash
UNITY="C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Unity.exe"
# 테스트 (-testPlatform EditMode 또는 PlayMode)
"$UNITY" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile tests.log
# 샘플 데이터 + 씬 생성
"$UNITY" -batchmode -quit -nographics -projectPath . -executeMethod WordRPG.EditorTools.SceneBuilder.CreateAllScenes -logFile build.log
```

## 환경 메모

- F: 드라이브는 소유권을 기록하지 않는 파일시스템이라 Git이 `safe.directory` 등록을 요구함 (등록 완료)
- 상위 폴더 `F:\GameProject`는 별개 저장소(Spiritual-Warfare)이므로 거기서 git 명령 실행 주의
