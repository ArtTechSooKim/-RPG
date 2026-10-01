# CLAUDE.md — WordRPG (영단어 RPG)

> Claude Code가 세션 시작 시 자동으로 읽는 프로젝트 컨텍스트.
> 결정 사항·게임 규칙·진행 현황은 `Docs/GDD.md`, 원본 기획은 `Docs/PRD.txt`. 큰 기능 작업 전에 GDD를 확인할 것.

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
6. **네이밍**: PascalCase 클래스/메서드/프로퍼티, camelCase 필드. SO 필드는 `[SerializeField] private` + 읽기 전용 프로퍼티. 클래스명은 영문, 화면 표시명은 한국어 필드(`displayName = "펜촉이"`). 주석은 한국어
7. **Git LFS 사용 중**: 이미지·오디오·폰트·네이티브 플러그인. 새 바이너리 타입 추가 시 `.gitattributes` 확인
8. **입력은 Input System 전용**: EventSystem에 `InputSystemUIInputModule` 사용 (`StandaloneInputModule` 금지)
9. **세로 화면 기준 UI**: 1080×1920 레퍼런스, 한 손 조작, 4지선다 버튼은 화면 하단

## 폴더 구조

```
Assets/
  Scripts/              WordRPG.asmdef (런타임)
    Core/               CSV 파서 등 공용
    Words/              단어, 숙련도(VocabularyProgress), 출제(WordSelector), 4지선다(QuizGenerator)
    Monsters/           MonsterSpecies·SkillData SO, MonsterInstance, LevelCurve, Evolution
    Items/              ItemData SO, Inventory
    Battle/             BattleEngine, BattleFormulas, BattleReward
    Field/              EncounterTable (이후 이동·조우)
    UI/                 BattleScreen(코드로 uGUI 구성), UnitView, UiKit — 세로 1080x1920, OS 한글 폰트
    Editor/             WordRPG.Editor.asmdef — CSV 임포터, 샘플 데이터 생성기
  Tests/EditMode/       WordRPG.Tests.EditMode.asmdef (로직)
  Tests/PlayMode/       WordRPG.Tests.PlayMode.asmdef (UI 버튼을 눌러 전투 한 판 진행)
  Data/                 Words, Monsters, Skills, Items, Encounters (SO 에셋)
  Scenes/
Docs/                   PRD.txt, GDD.md
```

## 명령어

에디터 메뉴: `WordRPG > Data > Import Word CSVs`, `WordRPG > Data > Create Sample Data` (없는 에셋만 생성), `WordRPG > Scenes > Create Battle Scene`

전투 화면 확인: `Assets/Scenes/Battle.unity`를 열고 Play (Game 뷰를 세로 비율로, 예: 1080x1920)

배치모드 (Unity 에디터가 이 프로젝트를 열고 있으면 실행 불가):

```bash
UNITY="C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Unity.exe"
# 테스트 (-testPlatform EditMode 또는 PlayMode)
"$UNITY" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile tests.log
# 샘플 데이터 생성
"$UNITY" -batchmode -quit -nographics -projectPath . -executeMethod WordRPG.EditorTools.SampleDataBuilder.Build -logFile build.log
```

## 환경 메모

- F: 드라이브는 소유권을 기록하지 않는 파일시스템이라 Git이 `safe.directory` 등록을 요구함 (등록 완료)
- 상위 폴더 `F:\GameProject`는 별개 저장소(Spiritual-Warfare)이므로 거기서 git 명령 실행 주의
