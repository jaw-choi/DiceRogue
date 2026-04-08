# DiceRogue 프로토타입

## 개요
이 프로젝트는 Unity 2D 기반의 로그라이크 오토배틀 프로토타입입니다.

현재 구조는 아래 5개 씬을 기준으로 동작합니다.
- `Boot`
- `MainMenu`
- `MapScene`
- `BattleScene`
- `RewardScene`

핵심 요구사항 반영 상태:
- 전투 수동/자동 토글 지원
- 자동 모드: 턴이 자동으로 계속 진행
- 수동 모드: 매 턴 `주사위 굴리기` 버튼을 눌러야 진행
- 시작 주사위는 `기본 공격`, `수비`만 사용
- 새로운 스킬은 보상에서 배운 뒤에만 장착 가능
- 모든 화면 텍스트는 `TextMeshPro` 기준으로 사용

## 중요한 UI 규칙
이 프로젝트에서는 `Unity UI Text` 대신 아래 규칙으로 통일합니다.

- 일반 텍스트:
  - `TextMeshPro - Text (UI)`
- 버튼:
  - 씬에서 버튼 오브젝트는 반드시 `Button - TextMeshPro`로 생성
  - 코드에서는 Unity 구조상 `Button` 컴포넌트를 그대로 참조
  - 버튼 안의 라벨 텍스트는 반드시 `TextMeshPro - Text (UI)` 사용
- 토글 라벨:
  - `TextMeshPro - Text (UI)` 사용

즉, `Button`과 `Toggle`은 Unity UI 기본 컴포넌트를 써도 되지만, 안에 들어가는 글자는 전부 TMP여야 합니다.

추가 설명:
- `TMP_Text`, `TMP_InputField`, `TMP_Dropdown`은 있지만 `TMP_Button`이라는 별도 타입은 없습니다.
- 따라서 `BattleSceneController`를 포함한 모든 SceneController에서 버튼 필드는 `Button` 타입으로 유지하는 것이 정상입니다.
- 대신 Inspector에는 `Button - TextMeshPro`로 만든 버튼 오브젝트를 연결해야 합니다.

## 코드 구조
### 데이터
- `SkillDefinition`
- `CombatantTemplate`
- `RunConfig`

### 런타임 시스템
- `GameRunManager`
- `DiceSystem`
- `BattleSystem`
- `RewardSystem`
- `MapSystem`

### 씬 컨트롤러
- `BootSceneController`
- `MainMenuSceneController`
- `MapSceneController`
- `BattleSceneController`
- `RewardSceneController`

### UI 보조
- `UIStateController`
- `UIInputSystemHelper`

## 파일 목록
- `Assets/Game/Scripts/Data/SkillDefinition.cs`
- `Assets/Game/Scripts/Data/CombatantTemplate.cs`
- `Assets/Game/Scripts/Data/RunConfig.cs`
- `Assets/Game/Scripts/Runtime/Models/CombatEnums.cs`
- `Assets/Game/Scripts/Runtime/Models/BattleActionResults.cs`
- `Assets/Game/Scripts/Runtime/Models/RunRuntimeModels.cs`
- `Assets/Game/Scripts/Runtime/Systems/DiceSystem.cs`
- `Assets/Game/Scripts/Runtime/Systems/BattleSystem.cs`
- `Assets/Game/Scripts/Runtime/Systems/RewardSystem.cs`
- `Assets/Game/Scripts/Runtime/Systems/MapSystem.cs`
- `Assets/Game/Scripts/Runtime/Systems/RunContentFactory.cs`
- `Assets/Game/Scripts/Runtime/Systems/GameRunManager.cs`
- `Assets/Game/Scripts/UI/UIStateController.cs`
- `Assets/Game/Scripts/UI/UIInputSystemHelper.cs`
- `Assets/Game/Scripts/Scenes/BootSceneController.cs`
- `Assets/Game/Scripts/Scenes/MainMenuSceneController.cs`
- `Assets/Game/Scripts/Scenes/MapSceneController.cs`
- `Assets/Game/Scripts/Scenes/BattleSceneController.cs`
- `Assets/Game/Scripts/Scenes/BattlePresenter.cs`
- `Assets/Game/Scripts/Scenes/UnitView.cs`
- `Assets/Game/Scripts/Scenes/FloatingTextSpawner.cs`
- `Assets/Game/Scripts/Scenes/BattleHUD.cs`
- `Assets/Game/Scripts/Scenes/RewardSceneController.cs`
- `Assets/Game/Scripts/Editor/RunSeedDataCreator.cs`

## 먼저 해야 할 일
이 프로젝트는 코드와 런타임 구조가 먼저 준비된 상태입니다.
즉, Unity 에디터 안에서 아래 작업을 직접 해줘야 정상 실행됩니다.

해야 하는 큰 작업 순서:
1. TMP Essentials 임포트 확인
2. 시드 데이터 생성
3. 씬 5개 생성
4. 각 씬에 Canvas와 TMP UI 배치
5. 각 씬에 SceneController 연결
6. `RunConfig.asset` 연결
7. `Build Settings`에 씬 등록
8. `Boot` 씬부터 플레이 테스트

## 1. TMP Essentials 임포트
### 목적
TextMeshPro UI를 쓰기 위해 기본 폰트/리소스를 준비합니다.

### 작업 순서
1. Unity 상단 메뉴에서 `Window > TextMeshPro > Import TMP Essential Resources` 를 클릭합니다.
2. 이미 임포트된 상태라면 다시 할 필요는 없습니다.

### 확인 포인트
- `TextMeshPro` UI 오브젝트를 만들 수 있어야 합니다.
- 콘솔에 TMP 관련 missing resource 에러가 없어야 합니다.

## 2. 시드 데이터 생성
### 목적
`RunConfig`, 스킬, 적 템플릿을 에디터에서 수정 가능한 에셋으로 생성합니다.

### 작업 순서
1. Unity에서 프로젝트를 엽니다.
2. 상단 메뉴에서 `Tools > DiceRogue > Generate Run Seed Data` 를 클릭합니다.
3. 생성이 끝나면 아래 경로를 확인합니다.

생성 경로:
- `Assets/Game/Data/Generated/Skills`
- `Assets/Game/Data/Generated/Combatants`
- `Assets/Resources/DiceRogue/RunConfig.asset`

### 확인 포인트
- `RunConfig.asset` 이 생성되어 있어야 합니다.
- 스킬 에셋이 여러 개 생성되어 있어야 합니다.
- 플레이어/적 템플릿 에셋이 생성되어 있어야 합니다.

## 3. Build Settings에 씬 등록
### 목적
씬 이동이 `SceneManager.LoadScene()` 으로 이루어지기 때문에, 씬 이름이 정확히 등록되어 있어야 합니다.

### 작업 순서
1. 상단 메뉴에서 `File > Build Profiles` 또는 `File > Build Settings` 를 엽니다.
2. 아래 이름으로 씬을 생성하거나 추가합니다.

등록 순서:
1. `Boot`
2. `MainMenu`
3. `MapScene`
4. `BattleScene`
5. `RewardScene`

### 주의
- 씬 이름은 코드와 동일해야 합니다.
- 철자와 대소문자를 바꾸지 않는 것이 안전합니다.

## 4. Boot 씬 설정
### 목적
게임 시작 시 `GameRunManager` 를 준비하고 `MainMenu` 로 보내는 시작 씬입니다.

### 씬 생성
1. `Assets/Scenes` 폴더에서 새 씬을 하나 만듭니다.
2. 이름을 `Boot.unity` 로 저장합니다.

### Hierarchy에서 만들 오브젝트
- `GameBootstrap`

### `GameBootstrap`에 붙일 컴포넌트
- `BootSceneController`

### Inspector 연결
- `Run Config`
  - `Assets/Resources/DiceRogue/RunConfig.asset`
- `Boot Delay`
  - 기본값 `0.1` 권장

### 이 씬에서 추가로 필요한 것
- 별도 UI는 없어도 됩니다.
- 카메라는 기본 Main Camera만 있어도 됩니다.

## 5. MainMenu 씬 설정
### 목적
런 시작, 디버그 전투, 디버그 보상으로 진입하는 메뉴 씬입니다.

### 씬 생성
1. 새 씬 생성
2. 이름을 `MainMenu.unity` 로 저장

### Hierarchy 권장 구조
```text
Main Camera
Canvas
  Panel
    TitleText(TMP)
    StatusText(TMP)
    ConfigText(TMP)
    StartRunButton
      Label(TMP)
    DebugBattleButton
      Label(TMP)
    DebugRewardButton
      Label(TMP)
    QuitButton
      Label(TMP)
MainMenuRoot
```

### Canvas 설정
- `Canvas`
  - Render Mode: `Screen Space - Overlay`
- `Canvas Scaler`
  - UI Scale Mode: `Scale With Screen Size`
  - Reference Resolution: `1080 x 1920` 권장

### 텍스트 만들기
텍스트는 반드시 아래 메뉴로 생성합니다.
- `GameObject > UI > Text - TextMeshPro`

### 버튼 만들기
버튼은 아래 메뉴로 생성합니다.
- `GameObject > UI > Button - TextMeshPro`

### `MainMenuRoot`에 붙일 컴포넌트
- `MainMenuSceneController`

### Inspector 연결
- `Run Config`
- `Title Text`
  - `TitleText` 의 TMP 컴포넌트
- `Status Text`
  - `StatusText` 의 TMP 컴포넌트
- `Config Text`
  - `ConfigText` 의 TMP 컴포넌트
- `Start Run Button`
- `Debug Battle Button`
- `Debug Reward Button`
- `Quit Button`

## 6. MapScene 씬 설정
### 목적
현재 플레이어 상태와 맵 진행도를 보여주고 다음 전투 노드를 선택하는 씬입니다.

### 씬 생성
1. 새 씬 생성
2. 이름을 `MapScene.unity` 로 저장

### Hierarchy 권장 구조
```text
Main Camera
Canvas
  Panel
    HeaderText(TMP)
    PlayerStatsText(TMP)
    DiceText(TMP)
    UnlockedSkillsText(TMP)
    MapProgressText(TMP)
    NodeButtonRoot
      NodeButtonTemplate
        Label(TMP)
    ReturnMenuButton
      Label(TMP)
    DebugRewardButton
      Label(TMP)
MapSceneRoot
```

### `NodeButtonTemplate` 만들기
1. `GameObject > UI > Button - TextMeshPro` 로 버튼을 하나 만듭니다.
2. 이름을 `NodeButtonTemplate` 으로 바꿉니다.
3. 버튼 자식에 있는 TMP 라벨 텍스트는 기본 문구 아무거나 넣어도 됩니다.
4. 이 버튼은 코드에서 복제하므로 템플릿 용도로 사용합니다.

### `MapSceneRoot`에 붙일 컴포넌트
- `MapSceneController`

### Inspector 연결
- `Run Config`
- `Header Text`
- `Player Stats Text`
- `Dice Text`
- `Unlocked Skills Text`
- `Map Progress Text`
- `Node Button Root`
- `Node Button Template`
- `Return Menu Button`
- `Debug Reward Button`

### 화면에서 확인할 정보
- 플레이어 HP
- Shield
- Armor
- Rage
- 현재 주사위 6면
- 언락한 스킬 목록
- 진행 가능한 맵 노드

## 7. BattleScene 씬 설정
### 목적
기존 `BattleSystem` 전투 규칙을 그대로 사용하면서, 그 결과를 스프라이트 기반으로 보여주는 씬입니다.
자동/수동 토글, `주사위 굴리기` 버튼, 유닛 뷰, 전투 HUD가 핵심입니다.

### 씬 생성
1. 새 씬 생성
2. 이름을 `BattleScene.unity` 로 저장

### Hierarchy 권장 구조
```text
Main Camera
BattleBackground
Canvas
  BattlePanel
    TopBar
      TurnInfoText(TMP)
      DiceResultText(TMP)
    MiddleArea
      PlayerArea
        PlayerUnitView
          AnimatedRoot
            Highlight
            SpriteImage
          HpBar
          NameText(TMP)
          HpText(TMP)
          ShieldArmorText(TMP)
          RageText(TMP)
          PopupAnchor
      EnemyArea
        EnemyUnitView01
          AnimatedRoot
            Highlight
            SpriteImage
          HpBar
          NameText(TMP)
          HpText(TMP)
          ShieldArmorText(TMP)
          RageText(TMP)
          PopupAnchor
        EnemyUnitView02
        EnemyUnitView03
    BottomPanel
      PlayerStatsText(TMP)
      EnemyStatsText(TMP)
      SummaryText(TMP)
    AutoBattleToggle
      Label(TMP)
    RollButton
      Label(TMP)
    BackToMenuButton
      Label(TMP)
  FloatingTextCanvas
    FloatingTextTemplate(TMP, 비활성)
  ResultPanel
    ResultText(TMP)
    ContinueButton
      Label(TMP)
BattleStates
BattlePresenterRoot
BattleSceneRoot
```

### 배경 오브젝트
- `BattleBackground`
  - `SpriteRenderer` 를 붙입니다.
  - 단색 사각형 스프라이트나 간단한 던전 배경 이미지를 연결합니다.
  - 플레이어가 왼쪽, 적이 오른쪽에 보이도록 카메라 중앙 배경 용도로 둡니다.

### UnitView 프리팹/오브젝트 만들기
`PlayerUnitView`, `EnemyUnitView01`, `EnemyUnitView02`, `EnemyUnitView03` 는 같은 구조를 사용합니다.

권장 구조:
```text
PlayerUnitView
  AnimatedRoot
    Highlight
    SpriteImage
  HpBar
  NameText(TMP)
  HpText(TMP)
  ShieldArmorText(TMP)
  RageText(TMP)
  PopupAnchor
```

각 자식 오브젝트 설정:
- `AnimatedRoot`
  - `RectTransform`
  - 스프라이트가 실제로 앞뒤로 움직일 루트
- `Highlight`
  - `Image`
  - 노란색/흰색 반투명 이미지
  - 평소에는 꺼져 있고 행동 시 켜짐
- `SpriteImage`
  - `Image`
  - 플레이어/적 플레이스홀더 스프라이트 표시
- `HpBar`
  - `Slider`
  - Fill Area 를 가진 기본 슬라이더 사용
- `NameText`
  - `TextMeshPro - Text (UI)`
- `HpText`
  - `TextMeshPro - Text (UI)`
- `ShieldArmorText`
  - `TextMeshPro - Text (UI)`
- `RageText`
  - `TextMeshPro - Text (UI)`
- `PopupAnchor`
  - `RectTransform`
  - 떠오르는 데미지 텍스트 기준점

### `UnitView` 연결
아래 4개 오브젝트에 모두 `UnitView` 를 붙입니다.
- `PlayerUnitView`
- `EnemyUnitView01`
- `EnemyUnitView02`
- `EnemyUnitView03`

`UnitView` Inspector 연결:
- `Animated Root`
  - 각 UnitView의 `AnimatedRoot`
- `Popup Anchor`
  - 각 UnitView의 `PopupAnchor`
- `Canvas Group`
  - UnitView 루트에 `CanvasGroup` 추가 후 연결
- `Sprite Image`
  - `SpriteImage`
- `Highlight Image`
  - `Highlight`
- `Hp Bar`
  - `HpBar`
- `Name Text`
  - `NameText`
- `Hp Text`
  - `HpText`
- `Shield Armor Text`
  - `ShieldArmorText`
- `Rage Text`
  - `RageText`
- `Fallback Sprite`
  - 간단한 원형/사각형 플레이스홀더 스프라이트

### `BattleHUD` 오브젝트
`BattlePanel` 또는 별도 `BattleHUDRoot` 오브젝트를 만들고 `BattleHUD` 를 붙입니다.

`BattleHUD` Inspector 연결:
- `Turn Info Text`
  - `TurnInfoText`
- `Player Stats Text`
  - `PlayerStatsText`
- `Enemy Stats Text`
  - `EnemyStatsText`
- `Current Dice Result Text`
  - `DiceResultText`
- `Summary Text`
  - `SummaryText`

### FloatingText 설정
`FloatingTextCanvas` 는 `Canvas` 아래 별도 레이어로 두는 것을 권장합니다.

설정 순서:
1. `FloatingTextCanvas` 오브젝트 생성
2. 자식으로 `FloatingTextTemplate` 생성
3. `FloatingTextTemplate` 는 `TextMeshPro - Text (UI)` 로 만들고 기본 문구는 `-99`
4. 색상은 흰색, 정렬은 가운데
5. `FloatingTextTemplate` 오브젝트는 비활성화

`BattlePresenterRoot` 에 `FloatingTextSpawner` 를 붙이고 아래를 연결합니다.
- `Canvas Root`
  - `FloatingTextCanvas` 의 `RectTransform`
- `Floating Text Prefab`
  - `FloatingTextTemplate` 의 TMP 컴포넌트

### `BattlePresenter` 설정
`BattlePresenterRoot` 오브젝트를 만들고 `BattlePresenter` 를 붙입니다.

`BattlePresenter` Inspector 연결:
- `Player View`
  - `PlayerUnitView`
- `Enemy Views`
  - Element 0: `EnemyUnitView01`
  - Element 1: `EnemyUnitView02`
  - Element 2: `EnemyUnitView03`
- `Battle Hud`
  - `BattleHUD` 컴포넌트
- `Floating Text Spawner`
  - `BattlePresenterRoot` 또는 별도 오브젝트의 `FloatingTextSpawner`

현재 제약:
- 실제 전투 데이터는 아직 단일 적 기준입니다.
- 따라서 `EnemyUnitView02`, `EnemyUnitView03` 는 지금은 자동으로 숨겨집니다.
- 나중에 `BattleSystem` 이 다중 적을 지원하면 같은 구조를 그대로 확장할 수 있습니다.

### 토글 만들기
토글은 아래 메뉴를 권장합니다.
- `GameObject > UI > Toggle`

그 다음 토글 안의 기본 `Text`가 있다면 삭제하고 TMP 텍스트를 새로 붙여서 라벨로 사용합니다.

### `BattleStates` 설정
`BattleStates` 오브젝트를 만들고 `UIStateController`를 붙입니다.

States 리스트에 2개를 추가합니다.
- `Id: Battle`
  - `Panel: BattlePanel`
- `Id: Result`
  - `Panel: ResultPanel`

### `BattleSceneRoot`에 붙일 컴포넌트
- `BattleSceneController`

### Inspector 연결
- `Run Config`
- `State Controller`
  - `BattleStates`
- `Battle State Id`
  - `Battle`
- `Result State Id`
  - `Result`
- `Battle Hud`
  - `BattleHUD` 컴포넌트
- `Battle Presenter`
  - `BattlePresenter` 컴포넌트
- `Result Text`
  - TMP 컴포넌트
- `Auto Battle Toggle`
- `Roll Button`
- `Continue Button`
- `Back To Menu Button`

### 전투 UI에서 꼭 테스트할 것
- `Auto Battle Toggle` 이 켜져 있으면 자동 진행
- `Auto Battle Toggle` 이 꺼져 있으면 `RollButton` 을 눌러야 턴 진행
- 공격 시 공격자가 앞으로 돌진했다가 돌아오는지 확인
- 공격 시 방어자 스프라이트가 피격 반응을 보이는지 확인
- 데미지/실드/회복/Rage 텍스트가 떠오르는지 확인
- 플레이어와 적의 HP Bar, Shield, Armor, Rage 가 계속 갱신되는지 확인
- 광폭화 스킬에서 `BERSERK!` 팝업이 보이는지 확인
- 사망 시 유닛이 페이드아웃 되는지 확인
- 전투 종료 시 `ResultPanel` 로 전환되는지 확인

## 8. RewardScene 씬 설정
### 목적
보상 선택과 주사위 슬롯 장착/강화를 처리하는 씬입니다.

### 씬 생성
1. 새 씬 생성
2. 이름을 `RewardScene.unity` 로 저장

### Hierarchy 권장 구조
```text
Main Camera
Canvas
  RewardSelectPanel
    HeaderText(TMP)
    PlayerStatsText(TMP)
    DiceText(TMP)
    PromptText(TMP)
    RewardButtonRoot
      RewardButtonTemplate
        Label(TMP)
    SkipButton
      Label(TMP)
  SlotSelectPanel
    SlotPromptText(TMP)
    SlotButtonRoot
      SlotButtonTemplate
        Label(TMP)
RewardStates
RewardSceneRoot
```

### `RewardStates` 설정
`RewardStates` 오브젝트를 만들고 `UIStateController`를 붙입니다.

States 리스트에 2개를 추가합니다.
- `Id: RewardSelect`
  - `Panel: RewardSelectPanel`
- `Id: SlotSelect`
  - `Panel: SlotSelectPanel`

### 버튼 템플릿 만들기
`RewardButtonTemplate`
- `GameObject > UI > Button - TextMeshPro` 로 생성
- 보상 목록용 버튼 템플릿

`SlotButtonTemplate`
- `GameObject > UI > Button - TextMeshPro` 로 생성
- 주사위 6면 슬롯 선택용 버튼 템플릿

### `RewardSceneRoot`에 붙일 컴포넌트
- `RewardSceneController`

### Inspector 연결
- `Run Config`
- `State Controller`
  - `RewardStates`
- `Reward Select State Id`
  - `RewardSelect`
- `Slot Select State Id`
  - `SlotSelect`
- `Header Text`
  - TMP 컴포넌트
- `Player Stats Text`
  - TMP 컴포넌트
- `Dice Text`
  - TMP 컴포넌트
- `Prompt Text`
  - TMP 컴포넌트
- `Reward Button Root`
- `Slot Button Root`
- `Reward Button Template`
- `Slot Button Template`
- `Skip Button`

### 보상 씬에서 꼭 테스트할 것
- 새 스킬 보상 선택 시 슬롯 선택 화면으로 넘어가는지
- 슬롯 선택 시 그 슬롯에 새 스킬이 장착되는지
- 강화 보상 선택 시 선택한 면만 강화되는지
- 스킵 버튼이 맵으로 돌아가는지

## 9. RunConfig 연결
모든 SceneController에는 `Run Config` 필드가 있습니다.

여기에 반드시 아래 에셋을 연결하세요.
- `Assets/Resources/DiceRogue/RunConfig.asset`

연결이 빠지면:
- 씬 이동이 꼬일 수 있음
- 디버그 시작이 제대로 안 될 수 있음
- 기본 데이터가 원하는 값과 다를 수 있음

## 10. EventSystem 관련
이 프로젝트는 코드에서 `EventSystem` 과 `InputSystemUIInputModule` 을 자동 보정합니다.
그래도 에디터에서는 아래를 권장합니다.

권장 사항:
- 씬마다 EventSystem을 굳이 수동으로 만들지 않아도 됨
- 만약 직접 만들었다면 `StandaloneInputModule` 대신 `InputSystemUIInputModule` 사용

## 11. 에디터에서 바로 테스트하는 방법
### 정상 루트 테스트
1. `Boot` 씬 오픈
2. Play
3. `Start Run`
4. `MapScene` 에서 전투 선택
5. `BattleScene` 에서 자동/수동 둘 다 시험
6. `RewardScene` 에서 스킬 학습 또는 강화 선택
7. 다시 `MapScene` 으로 돌아와 주사위 변경 확인
8. 보스까지 진행

### 빠른 디버그 테스트
`MainMenu` 에서:
- `Debug Battle`
- `Debug Reward`

또는 `MapScene`, `BattleScene`, `RewardScene` 을 직접 열고 Play 해도 `GameRunManager` 가 디버그용 런을 자동 생성합니다.

## 12. Inspector에서 자주 실수하는 부분
- `Run Config` 미연결
- TMP 텍스트 대신 구형 `Text` 를 만들어서 넣음
- 버튼 안 라벨이 `Text` 인데 TMP라고 생각하고 넘김
- `Node Button Template`, `Reward Button Template`, `Slot Button Template` 미연결
- `UIStateController` 의 `Id` 문자열 오타
- `Build Settings` 에 씬 이름 누락
- `ContinueButton` 과 `RollButton` 을 반대로 연결
- `AutoBattleToggle` 을 `BattleSceneController` 에 연결하지 않음

## 13. 현재 게임 규칙 요약
- 시작 주사위:
  - 기본 공격
  - 수비
  - 기본 공격
  - 수비
  - 기본 공격
  - 수비
- 이후 보상에서 새 스킬을 배워야 다른 슬롯에 넣을 수 있음
- 전투 중 표시되는 디버그 정보:
  - HP
  - Shield
  - Armor
  - Rage
  - 현재 주사위 6면

## 14. TODO
- 실제 씬 프리셋과 UI 프리팹은 아직 없음
- 다음 작업 추천:
1. 각 씬에 공통 TMP 폰트 스타일 적용
2. 버튼 색상으로 `공격`, `방어`, `회복`, `광폭화` 구분
3. 적 주사위 정보도 별도 TMP 텍스트로 노출
4. 사운드와 간단한 애니메이션 추가

## 15. 플레이 테스트 체크리스트
1. `Boot` 에서 `MainMenu` 로 넘어간다.
2. `Start Run` 으로 `MapScene` 진입이 된다.
3. 시작 주사위가 `기본 공격/수비`만 가진다.
4. `BattleScene` 에서 자동 토글 ON 시 자동 진행된다.
5. 자동 토글 OFF 시 `주사위 굴리기` 버튼을 눌러야 진행된다.
6. 전투 중 HP, Shield, Armor, Rage 가 갱신된다.
7. 일반 전투 승리 후 `RewardScene` 으로 이동한다.
8. 새 스킬 학습 후 원하는 슬롯에 장착된다.
9. 강화 보상 후 해당 슬롯 수치가 올라간다.
10. 보상 후 다시 맵으로 돌아간다.
11. 보스 승리 또는 패배 후 `MainMenu` 로 돌아간다.
## 전투 플레이스홀더 스프라이트
- 전투 씬에 정식 유닛 이미지가 아직 없어도 전투는 바로 진행됩니다.
- `CombatantTemplate.BattleSprite` 가 비어 있으면 `UnitView` 가 런타임에 원형 플레이스홀더 스프라이트를 자동 생성합니다.
- `BattleScene` 에 `BattleHUD` 나 `BattlePresenter` 연결이 비어 있어도 기본 전투 UI와 유닛 뷰를 런타임에 자동 구성합니다.
- 나중에 정식 이미지를 넣고 싶으면 각 `CombatantTemplate` 의 `BattleSprite` 만 교체하면 됩니다.
