# DiceRogue 주사위 스킬/효과 레퍼런스

이 문서는 현재 프로젝트에서 플레이어 주사위에 들어갈 수 있는 스킬과, 스킬 시스템이 지원하는 효과 종류를 개발용으로 정리한 문서입니다.

## 문서 목적

- 현재 플레이어가 획득 가능한 주사위 스킬 목록을 빠르게 확인한다.
- 각 스킬이 어떤 수치와 효과를 가지는지 기획/밸런싱 기준으로 본다.
- 코드상 지원되는 효과와 실제 사용 중인 효과를 구분한다.
- 나중에 스킬을 추가하거나 문서를 갱신할 때 어디를 수정해야 하는지 바로 찾을 수 있게 한다.

## 소스 오브 트루스

현재 런타임 기준 스킬 풀은 `Assets/Resources/DiceRogue/RunConfig.asset`만 보면 안 된다.

- 실제 런타임 구성: `Assets/Game/Scripts/Runtime/Systems/RunContentFactory.cs`
- 플레이어 보상 스킬 선택: `Assets/Game/Scripts/Runtime/Systems/RewardSystem.cs`
- 스킬 데이터 구조: `Assets/Game/Scripts/Data/SkillDefinition.cs`
- 효과 적용 로직: `Assets/Game/Scripts/Runtime/Systems/SkillEffectExecutor.cs`
- 전투 중 DP/첫 등장 보너스 처리: `Assets/Game/Scripts/Runtime/Systems/BattleSystem.cs`
- Rage / Berserk / 공격 보너스 계산: `Assets/Game/Scripts/Runtime/Models/RunRuntimeModels.cs`

정리하면:

- 플레이어가 실제로 보상/상점에서 얻는 스킬은 `RunContentFactory.BuildRuntimeConfig()`에서 만든 `skillLibrary` 기준이다.
- `Assets/Game/Data/Generated/Skills` 안의 구형 에셋은 일부가 남아 있어도 현재 런타임 스킬 풀과 다를 수 있다.

## 현재 플레이어 획득 가능 스킬 목록

현재 플레이어 스킬 풀은 총 9종이다.

| ID | 이름 | 성향 | 액션 | 대상 | 기본 효과 | 업그레이드 변화 | 비고 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `basic_attack` | Basic Attack | 중립 | Attack | RandomEnemy | 피해 6 | 피해 +3 | 가장 단순한 기본 공격 |
| `defensive_stance` | Defensive Stance | 방어 | Defense | Self | Shield +10, Armor +3 | Shield +4, Armor +2 | 방어형 핵심 기본기 |
| `focused_defense` | Focus Defense | 방어 | Defense | Self | Shield +8, 다음 턴 Shield +8 | 즉시/다음 턴 Shield 각각 +3 | 지속 방어 유지용 |
| `counter` | Counter | 방어 | Attack | RandomEnemy | 현재 Shield의 50%만큼 피해 | Shield 비례 피해 +20%p | Shield 누적 빌드용 |
| `shield_burst` | Shield Burst | 방어 | Attack | AllEnemies | 현재 Shield를 전부 소모하고 전체 적에게 60%만큼 피해 | 비율 +25%p | 광역기, Shield 전부 소모 |
| `blood_slash` | Blood Slash | 광전사 | Attack | RandomEnemy | 피해 8, 자해 4, Rage +2 | 피해 +3, 자해 +1, Rage +1 | HP를 태워 Rage 수급 |
| `fury` | Fury | 광전사 | Buff | Self | Rage +5 | Rage +2 | 순수 Rage 수급기 |
| `savage_strike` | Savage Strike | 광전사 | Attack | HighHpEnemy | 피해 12, Rage 5 소모 | 피해 +10, Rage 소모 -1 | 강한 단일 마무리기 |
| `vampiric_slash` | Vampiric Slash | 광전사 | Attack | HighHpEnemy | 피해 8, 흡혈 50% | 피해 +3, 흡혈 +20%p | 안정성 있는 광전사 공격기 |

## 현재 시작 주사위와 프리셋

### 시작 6면

Balanced Starter:

1. `Basic Attack`
2. `Defensive Stance`
3. `Basic Attack`
4. `Focus Defense`
5. `Counter`
6. `Fury`

### 빌드 성향 프리셋

`DiceBuildIdentity` 기준 프리셋:

| 성향 | 6면 구성 |
| --- | --- |
| Balanced | Basic Attack / Defensive Stance / Basic Attack / Focus Defense / Counter / Fury |
| Defensive | Defensive Stance / Focus Defense / Defensive Stance / Counter / Shield Burst / Basic Attack |
| Berserker | Blood Slash / Fury / Savage Strike / Vampiric Slash / Blood Slash / Basic Attack |

이 프리셋은 다음 용도로 사용된다.

- 시작 구성
- 빌드 전환
- 보상 선택 시 현재 빌드 성향 판단 보조

## 효과 시스템 지원 범위

`SkillDefinition` 기준으로 시스템이 지원하는 효과는 아래와 같다.

| 효과 필드 | 설명 | 현재 플레이어 사용 | 현재 적 사용 | 비고 |
| --- | --- | --- | --- | --- |
| `attackAmount` | 기본 피해 | 사용 | 사용 | 가장 기본적인 공격 수치 |
| `shieldAmount` | 즉시 Shield 획득 | 사용 | 사용 | Defense 액션에서 적용 |
| `armorAmount` | Armor 획득 | 사용 | 사용 | 고정 피해 경감용 |
| `nextTurnShieldAmount` | 다음 턴 Shield 예약 | 사용 | 미사용 | Focus Defense에서 사용 |
| `selfDamageAmount` | 자해 | 사용 | 미사용 | Blood Slash에서 사용 |
| `rageGainAmount` | Rage 획득 | 사용 | 미사용 | Blood Slash, Fury |
| `rageCostAmount` | Rage 소모 | 사용 | 미사용 | Savage Strike |
| `lifestealPercent` | 준 HP 피해 기준 흡혈 | 사용 | 미사용 | Vampiric Slash |
| `shieldDamagePercent` | 현재 Shield 비례 추가 피해 | 사용 | 미사용 | Counter, Shield Burst |
| `attackModifierAmount` | 다음 턴 공격력 변경 | 미사용 | 사용 | 적 디버프용으로 사용 중 |
| `dicePointModifierAmount` | 다음 턴 DP 변경 | 미사용 | 사용 | 적 디버프용으로 사용 중 |
| `repeatCount` | 동일 공격 반복 횟수 | 미사용 | 사용 | 적 연타 패턴에서 사용 |
| `bonusDicePointsOnFirstRoll` | 해당 면 첫 등장 시 DP 추가 | 미사용 | 미사용 | 시스템은 지원하지만 현재 스킬 없음 |
| `summonTemplate` / `summonCount` | 소환 | 미사용 | 사용 | 보스 샤먼 전용 |
| `summonedAllyAttackBonusAmount` | 소환된 아군 공격력 버프 | 미사용 | 사용 | 보스 샤먼 전용 |
| `consumeAllShield` | 현재 Shield 전부 소모 | 사용 | 미사용 | Shield Burst |

## 액션 타입과 타겟 타입

### 액션 타입

| 타입 | 설명 |
| --- | --- |
| `Attack` | 대상에게 피해를 주는 계열 |
| `Defense` | 본인에게 Shield/Armor/다음 턴 Shield를 주는 계열 |
| `Buff` | Rage, 자기 버프, 소환, 아군 강화 |
| `Debuff` | 적에게 다음 턴 공격력/DP 약화 |

### 타겟 타입

| 타입 | 설명 |
| --- | --- |
| `Self` | 자기 자신 |
| `RandomEnemy` | 살아있는 적 중 랜덤 1명 |
| `HighHpEnemy` | 살아있는 적 중 현재 HP가 가장 높은 적 |
| `AllEnemies` | 살아있는 적 전체 |

## 현재 구현상 중요한 전투 메모

기획이나 밸런싱 시 바로 영향을 주는 구현 사항들이다.

### Rage는 모든 공격 스킬 피해에 더해진다

현재 공격 피해 계산은 각 스킬의 기본 피해에 `actor.GetAttackBonus()`를 더한다.

`GetAttackBonus()` 구성:

- 현재 Rage
- 다음 턴 공격력 버프/디버프
- 지속 공격력 보너스
- Berserk 공격력 보너스

즉, 현재 구현에서는 `Savage Strike`만 Rage 영향을 받는 것이 아니라, 모든 공격 스킬이 Rage 수치만큼 추가 피해를 얻는다.

### Berserk 발동 조건

- Rage 최대치는 `15`
- Rage가 최대치에 도달하면 `2턴` 동안 Berserk 활성화
- Berserk 중 효과:
- 공격력 `+5`
- 흡혈 `+30%`
- 턴 시작 DP `+1`

위 내용은 광전사 빌드 밸런스에 직접 영향이 크므로 스킬 설명과 실제 동작이 어긋나지 않는지 수시로 확인할 필요가 있다.

### 첫 등장 DP 보너스는 시스템만 있고 현재 미사용

`bonusDicePointsOnFirstRoll`는 전투 시스템에서 정상 처리되지만, 현재 플레이어/적 스킬 중 이 필드를 실제로 쓰는 스킬은 없다.

## 보상 풀 기준 정리

플레이어는 `RewardSystem`에서 `runConfig.SkillLibrary`를 기준으로 새 스킬 제안을 받는다.

- 기본 보상: 스킬 2개 제안 + 면 업그레이드 1개
- 엘리트 전투: 스킬 3개 제안 + 업그레이드 1개
- 상점: 스킬 2개 제안 + 업그레이드 1개

보상 선택 로직 특징:

- 아직 해금되지 않은 스킬을 우선 제안
- 현재 주사위 구성을 보고 방어형/광전사형 성향을 추정
- 상황에 따라 방어형 스킬과 광전사형 스킬을 섞어 제안

## 현재 런타임에 연결되지 않은 구형 스킬 에셋

아래 에셋은 프로젝트 안에 남아 있지만 현재 `BuildRuntimeConfig()`의 플레이어 `skillLibrary`에는 포함되지 않는다.

- `Heavy Slash`
- `Guard`
- `Fortify`
- `First Aid`
- `Rage Burst`

이 항목들은 다음 가능성이 있다.

- 예전 프로토타입 데이터
- 에디터 생성물 잔존
- 향후 재사용 후보

실제 사용 여부를 확인할 때는 에셋 존재만 보지 말고 반드시 `RunContentFactory.BuildRuntimeConfig()`의 `skillLibrary` 포함 여부를 확인한다.

## 스킬 추가 시 체크리스트

새 스킬을 실제 게임에 반영하려면 보통 아래 순서로 확인한다.

1. `RunContentFactory.BuildRuntimeConfig()`에 `CreateSkill(...)` 추가
2. `skillLibrary`에 새 스킬 등록
3. 필요하면 시작 주사위 또는 프리셋에 반영
4. 방어형/광전사형 분류가 필요하면 `RewardSystem`의 태그 판정 갱신
5. 새 효과가 기존 필드로 표현되지 않으면 `SkillDefinition`과 `SkillEffectExecutor` 확장
6. 스킬 설명 문구와 실제 동작이 일치하는지 검증
7. 이 문서의 표에 새 행 추가

## 문서 갱신 규칙

이 문서는 아래 기준으로 유지한다.

- 플레이어가 실제 획득 가능한 스킬은 반드시 `현재 플레이어 획득 가능 스킬 목록` 표에 기록
- 적 전용 신규 메커니즘이 생기면 `효과 시스템 지원 범위` 표에 반영
- 설명과 실제 구현이 다르면 `현재 구현상 중요한 전투 메모`에 우선 기록
- 구형 에셋이 재활성화되면 `현재 런타임에 연결되지 않은 구형 스킬 에셋` 항목에서 제거

## 빠른 참조 파일

- `Assets/Game/Scripts/Runtime/Systems/RunContentFactory.cs`
- `Assets/Game/Scripts/Runtime/Systems/RewardSystem.cs`
- `Assets/Game/Scripts/Data/SkillDefinition.cs`
- `Assets/Game/Scripts/Runtime/Systems/SkillEffectExecutor.cs`
- `Assets/Game/Scripts/Runtime/Systems/BattleSystem.cs`
- `Assets/Game/Scripts/Runtime/Models/RunRuntimeModels.cs`
