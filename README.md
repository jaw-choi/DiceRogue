# DiceRogue

Unity 2D 기반의 주사위 로그라이크 프로토타입입니다.  
플레이어는 6면 주사위의 각 면에 스킬을 배치하고, 전투 보상으로 면을 교체하거나 강화하면서 한 번의 런을 진행합니다.

## 현재 프로토타입 범위

- 씬 흐름: `Boot -> MainMenu -> MapScene -> BattleScene -> RewardScene`
- 전투: 자동 진행 중심의 턴제 주사위 전투
- 빌드 구성: 6면 주사위 커스터마이즈
- 적 구성: 일반 적, 엘리트, 보스, 소환 유닛
- 진행 구조: 1스테이지 던전 맵 + 보상 루프
- UI: 모바일 가독성을 우선한 단순 고대비 런타임 UI

## 오늘 바뀐 점

### 1. 데이터 구조 확장

- `DiceLoadoutDefinition` 추가
  - 플레이어와 적의 6면 주사위 구성을 ScriptableObject로 관리할 수 있게 변경
- `SkillDatabase` 추가
  - 프로토타입 스킬을 플레이어용 / 적용으로 나눠 관리 가능
- `RunConfig` 확장
  - 단일 적 기준에서 벗어나 `EncounterDefinition`, `MapNodeDefinition` 기반의 전투/맵 데이터 구성 지원

### 2. 전투 시스템 확장

- 멀티 적 전투 지원
- 소환 유닛 처리 지원
- 턴 리포트에 현재 행동 유닛, 플레이어 DP, 턴 순서 정보 추가
- HUD에서 전투 흐름을 따라가기 쉬운 로그 구조로 정리
- `BattleSimulationRunner` 추가
  - 에디터 메뉴에서 전투 시뮬레이션 확인 가능

### 3. 스킬 시스템 정리

- `SkillEffectExecutor` 추가
  - 스킬 실행 책임을 전투 루프에서 분리
- 공격 / 방어 / 버프 / 디버프 카테고리 처리 정리
- 자기 자신 / 랜덤 적 / 전체 적 / 체력 높은 적 타게팅 지원
- 보호막 소비, 분노 획득/소모, 다음 턴 보호막, 흡혈, 반복 타격, DP 변경, 소환, 소환수 공격력 오라까지 데이터 기반 처리 가능

### 4. 주사위 빌드 시스템 강화

- 6면 고정 검증 로직 추가
- 보상에서 면 교체 / 면 강화 지원
- 시작 빌드와 프로토타입 성향 전환 지원
  - `Balanced`
  - `Defensive`
  - `Berserker`

### 5. 적/조우 데이터 확장

- Slime, Goblin, Golem, Shaman, Summoned Goblin 프로토타입 데이터 반영
- 단일 적 전투에서 조우 단위 전투로 확장
- 보스 조우와 일반 조우를 같은 프레임워크에서 처리 가능하게 정리

### 6. 맵/보상 루프 추가

- 1스테이지용 경량 던전 맵 구현
- 노드 타입 지원
  - `Battle`
  - `EliteBattle`
  - `Reward`
  - `Shop`
  - `Boss`
- 전투 후 보상 선택, 면 교체, 면 강화, 스킵 흐름 연결

### 7. UI 가독성 개선

- 전투 HUD 재구성
  - 플레이어 HP / Shield / Armor / Rage / Berserk / DP 표시
  - 적 상태 요약 표시
  - 현재 행동 유닛 표시
  - 턴 순서 표시
  - 주사위 결과 로그 표시
- 맵 화면 재구성
  - 현재 6면 정보, 노드 선택지, 진행 상황 표시
- 보상 화면 재구성
  - 현재 6면 확인
  - 보상 선택 후 교체/강화 대상 면 선택

## 이전 버전과 달라진 점

### 이전 버전

- 전투가 사실상 단일 적 중심 구조에 가까웠음
- 스킬 실행 로직이 전투 시스템에 더 강하게 묶여 있었음
- 주사위 면 데이터와 빌드 정체성이 분리되어 있지 않았음
- 적/보스/소환을 같은 데이터 구조로 다루기 어려웠음
- 맵은 매우 단순하거나 선형 흐름에 가까웠음
- UI는 디버그 성격이 강하고, 전투 로그와 상태 확인이 덜 명확했음

### 현재 버전

- 멀티 적 / 소환 / 보스 전투를 같은 전투 프레임워크에서 처리
- 스킬이 ScriptableObject 기반 데이터와 실행기 구조로 분리
- 6면 주사위를 명시적 데이터 모델로 관리
- 보상 루프를 통해 면 교체/강화가 자연스럽게 연결
- 맵 노드와 조우 데이터가 런 진행 구조에 포함
- 전투/맵/보상 UI가 실제 플레이 테스트 가능한 수준으로 정리

## 주요 스크립트

### 데이터

- `Assets/Game/Scripts/Data/SkillDefinition.cs`
- `Assets/Game/Scripts/Data/CombatantTemplate.cs`
- `Assets/Game/Scripts/Data/DiceLoadoutDefinition.cs`
- `Assets/Game/Scripts/Data/SkillDatabase.cs`
- `Assets/Game/Scripts/Data/RunConfig.cs`

### 런타임 모델

- `Assets/Game/Scripts/Runtime/Models/CombatEnums.cs`
- `Assets/Game/Scripts/Runtime/Models/BattleActionResults.cs`
- `Assets/Game/Scripts/Runtime/Models/RunRuntimeModels.cs`

### 시스템

- `Assets/Game/Scripts/Runtime/Systems/DiceSystem.cs`
- `Assets/Game/Scripts/Runtime/Systems/BattleSystem.cs`
- `Assets/Game/Scripts/Runtime/Systems/SkillEffectExecutor.cs`
- `Assets/Game/Scripts/Runtime/Systems/RewardSystem.cs`
- `Assets/Game/Scripts/Runtime/Systems/MapSystem.cs`
- `Assets/Game/Scripts/Runtime/Systems/RunContentFactory.cs`
- `Assets/Game/Scripts/Runtime/Systems/GameRunManager.cs`

### 씬/UI

- `Assets/Game/Scripts/Scenes/MapSceneController.cs`
- `Assets/Game/Scripts/Scenes/BattleSceneController.cs`
- `Assets/Game/Scripts/Scenes/BattlePresenter.cs`
- `Assets/Game/Scripts/Scenes/BattleHUD.cs`
- `Assets/Game/Scripts/Scenes/RewardSceneController.cs`
- `Assets/Game/Scripts/Scenes/UnitView.cs`

### 에디터 도구

- `Assets/Game/Scripts/Editor/RunSeedDataCreator.cs`
- `Assets/Game/Scripts/Editor/BattleSimulationRunner.cs`

## 다음 확인 권장 사항

- Unity에서 스크립트 리컴파일 확인
- `Tools > DiceRogue > Generate Run Seed Data` 실행
- 맵 -> 전투 -> 보상 -> 맵 루프 1회 수동 테스트
- 보상에서 면 교체/강화 후 실제 전투 반영 여부 확인
- Shaman 소환 및 멀티 적 HUD 표시 확인

## 현재 한계

- 정식 아트 에셋과 연출은 아직 최소 수준
- 일부 문자열/표현은 프로토타입 기준이며 추가 다듬기가 필요
- README 기준 설명은 현재 구현 상태를 반영하며, 실제 밸런스 수치는 계속 바뀔 수 있음
