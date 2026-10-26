# 체스 보스 1페이즈 사용법과 공부 순서

## 먼저 실행하기

Unity에서 `Assets/Members/CWH/SCENCES/PhaseOne_Playground.unity`를 열고 Play를 누른다.
또는 상단 메뉴 `CWH > Boss > 1페이즈 > 테스트 씬 열기`를 사용한다.
씬에는 흰 킹, 플레이어 캡슐, 카메라, 벽, 저장된 NavMesh와 다섯 패턴이 연결되어 있다.
자동 AI는 시작 후 3초 기다린 뒤 움직이며 공격한다.

| 조작 | 결과 |
| --- | --- |
| WASD | 플레이어 이동 |
| F | 주변 반지름 6 안의 적/기물/박힌 검을 공격하는 테스트 입력 |
| 마우스 왼쪽 클릭 | 커서 아래 대상을 공격하는 **거리 제한 없는 검증용** 입력 |
| P | 자동 패턴 선택 켜기/끄기. 이미 시작한 공격은 계속 진행한다. |
| R | 테스트 씬 다시 시작 |

클릭은 완성된 플레이어 전투 시스템이 아니다. 소환 기물과 박힌 검을 빠르게 부숴 보는 도구다.
검이 날아갈 때는 공격해도 체력이 감소하지 않고, 바닥에 박힌 다음에만 파괴할 수 있다.
플레이어 체력이 0이 되면 전투가 멈춘다. R로 다시 시작한다.

기존 `DEV_SCENE`에서도 쓰려면 편집 모드에서 보스 루트를 선택하고
`CWH > Boss > 1페이즈 > 현재 보스 연결 및 저장`을 실행한다.
이 메뉴는 컴포넌트/경고/프리팹 참조, 흰 킹 메시와 무기 표시를 연결하고 현재 씬을 저장한다.
기존 공전 검은 비활성화하고 새 공격용 검을 사용한다. 기존 포효 밀치기 수치는 유지한다.
초기 프리셋을 적용하므로 이동 속도/정지 거리/패턴 시간은 아래 기본값으로 바뀐다.
이후에는 Inspector에서 조절하면 된다. 메뉴를 다시 실행하면 프리셋 시간이 다시 적용된다.

## 구현된 규칙

| 기능 | 동작 |
| --- | --- |
| 기본 공격 | 전방 140도, 반지름 9의 검 2연격. 각 타격마다 경고하고 각 타격은 한 번만 피해를 준다. |
| 소환 | 폰 또는 나이트 중 하나를 선택해 같은 종류 두 마리 소환. 하나라도 남아 있으면 재소환 금지. |
| 소환 보호 | 기물이 한 마리 이상 살아 있으면 보스가 받는 피해 ×0.5. 마지막 기물이 사라질 때 해제. |
| 폰 | NavMesh 추적 → 원형 경고 → 근거리 공격 → 휴식. |
| 나이트 | 접근 → 고정된 착지 위치 경고 → 포물선 점프 → 착지 공격 → 휴식. |
| 대쉬 | 직사각형 경고 → 고정 방향 직진. 경계/벽 앞에서 멈추고 같은 대상은 한 번만 맞는다. |
| 검 투척 | 플레이어 위치 고정 → 착지 경고 → 비행 → 착지 피해 → 바닥에서 7초간 파괴 가능. |
| 검 파괴 | 검 체력 60을 깎으면 보스에게 기본 추가 피해 100을 요청하고 그로기 진입. 자연 회수는 보상 없음. |
| 포효 | 원형 경고 후 피해 없이 밀친다. 맞혔는지와 관계없이 보스가 그로기에 들어간다. |
| 그로기 | 기존 피해 계산에 ×1.5. 소환 보호와 겹치면 0.5 × 1.5 = 0.75배. |
| 페이즈 종료 | HP 비율이 0.6 이하가 되면 공격 중단, 소환/검 정리, 추적 정지. 2페이즈 대기 상태. |

HP 60.5%처럼 소수인 체력은 1페이즈다. 표시 숫자를 반올림해서 페이즈를 판단하지 않는다.
2페이즈 공격은 이번 범위에 포함하지 않았다.

검 파괴 보상의 100은 **기본 피해**다. 소환 보호 중에는 실제 50을 받는다.
이 피해를 적용한 다음 그로기에 들어가므로 방금 생긴 그로기가 보상 피해에 소급 적용되지는 않는다.
그로기 도중 다시 진입해도 배율이 누적되지 않는다. 남은 시간만 보장한다.

## 공격이 겹치지 않게 하는 장치

러너는 한 번에 하나의 보스 패턴만 실행하고, 후딜 뒤에 공통 휴식 2초를 추가한다.
AI는 쿨타임/거리/살아 있는 기물/필드의 검 조건을 통과한 패턴에서 선택한다.
다른 선택지가 있으면 직전 패턴을 제외한다. 모든 패턴이 쿨타임이면 추적하며 기다린다.
기물 둘의 최초 대기도 다르며, 기물은 다른 기물이나 보스가 공격을 시작한 동안 새 경고를 시작하지 않는다.
보스도 기물이 공격을 예고하거나 점프 중이면 새 패턴을 시작하지 않는다.
검이 필드에 남아 있을 때는 검을 사용하는 2연격/대쉬/재투척을 기다린다. 소환과 포효는 사용할 수 있다.

## 기본 수치와 조정 위치

| 컴포넌트 | 주로 조절할 값 | 기본값 |
| --- | --- | --- |
| BossPatternRunner | Pattern Interval: 모든 패턴 공통 휴식 | 2초 |
| MeleeComboPattern | Range / Angle / Damage / Second Warning | 9 / 140도 / 각 20 / 0.7초 |
| SummonPattern | Warning / Recovery / Cooldown | 1.5 / 1.2 / 22초 |
| DashPattern | Distance / Speed / Width / Damage | 18 / 30 / 4 / 25 |
| SwordThrowPattern | Impact Radius / Warning / Cooldown | 2.8 / 1.3 / 12초 |
| ThrownBossSword 프리팹 | Flight Duration / Embedded Duration / Impact Damage | 0.8초 / 7초 / 25 |
| BossSwordController | Break Bonus Damage | 100 |
| RoarPattern | Roar Radius / Knockback Speed / Duration | 새 프리셋 10 / 35 / 0.5초 |
| BossGroggyController | Default Duration / Damage Multiplier | 기존 3초 / 1.5배 |
| ChessMinion 프리팹 | Attack Radius / Damage / Warning / Rest | 2.5 / 12 / 1.3초 / 3초 |
| BreakableHealth 프리팹 | Max Health | 폰 80 / 나이트 120 / 검 60 |

프리팹 원본은 `Assets/Members/CWH/PREFABS/PhaseOne`에 있다.
기물 스킬 세부 공격 방식과 수치는 원래 기획에 없었으므로 위 기본 행동을 정했다.
기물에는 체스판 한 칸씩 이동하는 규칙을 적용하지 않았다. 이 게임의 NavMesh 전투 이동을 사용한다.

## 공부할 순서

한 번에 전부 외울 필요는 없다. 한 단계를 읽고 해당 패턴을 테스트한 다음 다음 단계로 넘어간다.

1. `Patterns/MeleeComboPattern.cs`: 가장 단순한 새 패턴. BeginWarning → Execute → Cleanup 순서부터 읽는다.
2. `BossAttackQuery.cs`: OverlapBox는 후보 수집, Contains는 거리/각도 판단이다. HashSet이 중복 Collider 피해를 막는다.
3. `AreaTelegraphPresenter.cs`: 같은 반지름과 각도로 경고 메시를 만든다. 기존 Shader Graph의 진행도/투명도를 그대로 쓴다.
4. `BreakableHealth.cs`: BossHealthModel을 재사용하는 두 번째 예. 체력 계산과 '부서졌을 때 할 일'을 이벤트로 분리한다.
5. `Patterns/SummonPattern.cs` → `BossSummonRoster.cs`: 패턴은 소환 순서, Roster는 살아 있는 목록과 보호 배율을 맡는다.
6. `ChessMinion.cs`: Approaching/Warning/Jumping/Resting 네 상태가 있는 작은 FSM이다. enum과 switch의 연결을 본다.
7. `Patterns/SwordThrowPattern.cs` → `BossSwordController.cs` → `ThrownBossSword.cs`: 공격 진행, 필드 검 관리, 검 한 개의 수명을 나눠 읽는다.
8. `PhaseOneBrain.cs` → 러너의 `IsReadyFor`/`TryRun`: '무엇을 할지 선택'과 '실제로 실행'이 다른 책임임을 확인한다.
9. `BossPhaseController.cs` → `PhaseOneEncounter.cs`: 체력 비율 판단과 공격/소환/이동 정리를 이벤트로 연결한다.
10. `BossCombatPresentation.cs`: 연출이 판정이나 체력을 바꾸지 않는지 확인한다. 마지막으로 기존 대쉬/포효와 비교한다.

`Editor/PhaseOneSetupMenu.cs`는 게임 AI가 아니라 씬/프리팹을 조립하는 도구다.
런타임을 먼저 이해하고 나중에 읽어도 된다. Tests 폴더 역시 첫 공부 순서에서 제외해도 된다.
AreaTelegraphPresenter의 삼각함수/정점 계산도 처음에는 건너뛰어도 된다.
우선 Show는 범위를 그리기, SetProgress는 채워지는 정도, Hide는 투명하게 숨기기라고 읽는다.
착지 경고는 부모 기물이 점프해도 원래 월드 위치를 유지하며 착지할 때까지 보인다.

## 낯선 문법을 읽는 요령

- `?.`는 앞의 참조가 null이 아닐 때만 함수를 호출한다. 선택적인 시각 효과에 사용했다.
- `=>`는 짧은 함수/프로퍼티 본문을 표현한다. 반환형과 오른쪽 식을 먼저 읽는다.
- `event Action<ChessMinion>`은 기물 한 개를 인자로 주는 반환값 없는 알림이다. `+=`로 구독하고 `-=`로 해제한다.
- `HashSet<T>.Add()`는 처음 넣으면 true, 이미 있으면 false다. 그래서 '처음 맞았을 때만 피해'를 한 조건으로 표현할 수 있다.
- `yield return null`은 다음 프레임까지 기다린다. `yield return IEnumerator`는 그 안의 순서가 끝날 때까지 진행한다.
- `finally`와 `Cleanup`은 둘 다 정리 장치다. 돌진 직후 정리는 후딜 전, Cleanup은 정상 종료/취소 모두의 안전망이다.
- `float.MaxValue`는 테스트 파괴 입력에서만 사용한다. 일반 공격력으로 사용하지 않는다.

## SOLID를 적용한 이유

패턴 클래스 한 곳에 이동, 체력, 셰이더, AI 선택까지 넣으면 하나를 고칠 때 다른 기능도 깨지기 쉽다.
패턴은 순서, 이동기는 이동, Query는 판정, Presenter는 표시, Roster는 소환 목록을 맡는다(SRP).
새 패턴은 BossPattern을 상속하고 AI 배열에 넣어 추가할 수 있다(OCP).
패턴들은 공통 경고/실행/정리 계약을 지켜 러너가 같은 방법으로 실행한다(LSP).
피해와 밀치기는 작은 IDamageReceiver/IKnockbackReceiver로 요청한다(ISP/DIP).
Unity의 Transform/NavMeshAgent 자체를 추상화하는 인터페이스를 전부 만들지는 않았다.
실제로 교체되는 경계인 피해 수신자와 패턴부터 분리해 학습과 변경 비용을 줄였다.

## 연출과 확장

기본 시각 연출은 흰 체스 모델, 검 휘두르기/돌진 자세, 회전하며 날아가는 검,
나이트 점프, 원형/부채꼴/직사각형 경고, 타격 시 몸체 펄스, 그로기 기울임이다.
전용 Animator 애니메이션과 음원 제작은 포함하지 않았다.
BossCombatPresentation의 Impact 이벤트에 준비된 소리/파티클을 연결할 수 있다.
경고는 Shader Graph의 Opacity/Progress를 사용하며 매 프레임 GameObject를 켜고 끄지 않는다.
부채꼴은 C#이 만든 평면 메시를 사용하고 셰이더 코드는 추가하지 않았다.

## 직접 풀어 볼 문제

구현 검증: Unity 6000.6.3f1의 별도 프로젝트에서 PlayMode 테스트 39개가 통과했다.
실제 저장된 씬 부팅, 자동 AI 시작, 2연격 회피, 폰/나이트 공격, 소환 보호,
검 파괴/자연 회수/비행 중 취소, 포효, 그로기, 사망 및 60% 경계 정리를 포함한다.
같은 프로젝트 렌더 설정으로 모델과 부채꼴 경고의 화면 표시도 확인했다.

1. AI가 `IsReadyFor()`를 여러 번 호출해도 쿨타임이 시작되면 안 되는 이유는?
2. 같은 플레이어에 Collider가 세 개 있으면 2연격 한 타격당 피해는 몇 번인가? HashSet이 없으면?
3. 두 번째 경고 중 옆으로 피한 플레이어를 향해 매 프레임 방향을 바꾸면 어떤 공정성 문제가 생길까?
4. 기물 두 마리 중 하나만 파괴했을 때 보호를 해제하면 어느 기획 조건을 어기게 될까?
5. 소환 보호와 그로기 상태에서 기본 피해 100을 받으면 실제 피해는? 마지막 기물을 없앤 뒤에는?
6. `RemoveDamageMultiplier(this)` 대신 모든 배율을 지우면 어떤 버그가 생길까?
7. 검이 날아갈 때 `CanTakeDamage`를 false로 두는 이유와 true로 바꾸는 시점은?
8. 검의 자연 회수와 파괴를 같은 처리로 보상하면 플레이어가 공격하지 않아도 어떤 일이 생길까?
9. 검 파괴 보상 처리에서 ActiveSword를 먼저 null로 만드는 이유는? 그로기 이벤트가 Clear를 호출하는 점을 생각해 보자.
10. 보스 체력이 정확히 60%일 때 어떤 상태가 되어야 하며, 경고 중이던 공격은 어떻게 정리되어야 할까?
11. PhaseOneBrain이 직접 NavMeshAgent를 이동시키지 않는 이유는?
12. 새 패턴을 추가할 때 기존 러너의 if/switch를 고치지 않아도 되는 구조는 어떤 SOLID 원칙과 관련 있을까?

답은 번호별로 짧게 적어도 된다. 다음에는 답을 확인하고, 이해가 부족한 함수부터 다시 설명한다.
