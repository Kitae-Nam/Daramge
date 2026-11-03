# 체스 보스 코드로 배우는 SOLID

작성일: 2026-11-01. 이 문서는 현재 CWH의 검 공격과 2페이즈 리팩터링을 읽기 위한 안내서다.
SOLID를 외운 다음 전체 코드를 한 번에 읽을 필요는 없다. 아래 순서대로 한 기능의 호출 흐름을 따라간다.

## 1. 가장 먼저 볼 파일

코드의 기준 폴더는 `Assets/Members/CWH/SCRIPTS/Bosses/Runtime`이다.
아래 1~7번 경로는 이 폴더 기준이고, 8번 테스트는 한 단계 위 `Bosses/Tests` 폴더에 있다.

| 순서 | 읽을 파일/함수 | 읽으면서 이해할 내용 |
|---|---|---|
| 1 | `Chess/Weapons/BossSword.cs`의 `TryHold`, `TryReturnFromHand` | 상태를 읽는 것과 상태를 바꾸는 것의 차이 |
| 2 | `Chess/Weapons/SwordPoseBinding.cs`의 `TryCapture`, `Restore`, `Returned` | 원래 자세를 한곳에서 저장하고 복원하는 이유 |
| 3 | `Chess/Weapons/SwordSwingMotion.cs` | 계산 데이터와 GameObject를 움직이는 코드의 차이 |
| 4 | `Chess/Patterns/MeleeComboPattern.cs`의 `Execute` | 설정 조회 → 동작 진행 → 한 번의 피해 → 종료 정리 순서 |
| 5 | `Chess/AI/ICombatTuningSources.cs`와 `ChessCombatProfiles.cs` | 패턴이 구체적인 페이즈 조율자 대신 작은 계약을 사용하는 방식 |
| 6 | `Chess/Summons/ChessMinionAttack.cs`, `SectorMinionAttack.cs`, `JumpMinionAttack.cs` | 공통 순서와 교체 가능한 공격 행동 분리 |
| 7 | `Chess/Dash/DashScorchFactory.cs`, `DashScorchField.cs`, `DashScorchVisual.cs` | 조립, 피해·수명, 화면 표시를 각각 나누는 이유 |
| 8 | `Tests/ChessPhaseTwoTests.cs`와 `Tests/SolidDesignTests.cs` | 코드가 실제로 보장하는 동작을 테스트에서 확인하는 방법 |

처음에는 1~3번까지만 읽어도 충분하다. 호출한 함수의 정의로 이동해서 실제로 바뀌는 필드와 반환값을 확인한다.
IDE의 **정의로 이동**, **사용 위치 찾기** 기능을 함께 사용하면 흐름을 찾기 쉽다.

## 2. 이번에 무엇을 나눴는가

### 검의 복귀: 같은 규칙은 한곳에

이전에는 근접 연출과 투척 검이 각각 원래 부모, 위치, 회전, 크기를 저장했다.
부모 비활성화 중 복귀 규칙을 수정하면 두 코드를 각각 고쳐야 했다.

지금은 `SwordPoseBinding`이 그 일을 맡는다.

```text
근접 연출 ─┐
           ├→ SwordPoseBinding → 원래 부모/자세 복원 → Returned 이벤트
투척 소유권 ┘                                          ↓
                                                  BossSword 상태 복귀
```

`Restore()`는 일반 복귀를 즉시 처리한다. 부모 비활성화 중에는 `Restore(true)`로 복귀를 미룬다.
`Returned`는 위치 복원이 끝났다는 알림이다. `BossSword`가 이 알림을 받고 상태를 공전으로 바꾼다.
복귀 담당이 공격 피해나 보스 그로기까지 처리하지 않는다는 점을 본다.

### 공격 판정과 연출: 같은 동작 데이터를 공유

이전에는 피해를 줄 순간을 계산하기 위해 공격 코드가 연출 클래스의 수학 함수를 호출했다.
지금은 `SwordSwingMotion`에 회전 곡선, 차징 각도, 정면 통과 계산이 있다.

- `BossCombatPresentation`은 동작 데이터를 읽고 피벗을 회전한다.
- `MeleeComboPattern`은 같은 데이터를 읽고 정면 통과 시 피해를 한 번 준다.
- 동작 데이터는 체력이나 Transform을 직접 변경하지 않는다.
- `MeleeMotionSource`가 두 코드에 같은 데이터 인스턴스를 공급한다.

동기화가 필요한 정보까지 따로 복제하지 않고, 화면 표시 클래스에 대한 의존만 줄인 것이다.
`_chargeMotionFraction`이 0.8이면 경고 앞 80%에서 검을 당기고 나머지는 준비 자세를 유지한다.

### 페이즈: 전환 담당이 모든 정보를 보관하지 않도록 분리

| 담당 | 현재 역할 |
|---|---|
| `BossPhaseController` | 체력으로 현재 페이즈 판단 |
| `PhaseTwoEncounter` | 공격 정리, 전환 대기, 전투 잠금/재개 |
| `ChessPhaseAppearance` | 페이즈 이벤트에 맞춰 메시와 머티리얼 변경 |
| `ChessCombatProfiles` | 현재 페이즈에 맞는 공격 수치를 조회 계약으로 제공 |
| `ChessSummonCatalog` | 소환 후보 목록에서 한 종류 선택 |
| `DashScorchFactory` | 장판 표시와 판정 컴포넌트 조립 |

`ChessCombatProfiles`는 Inspector에서 조절할 데이터의 보관 장소다. 개별 패턴에는 필요한 값만 전달한다.
`MeleeTuning`, `DashTuning` 같은 `readonly struct`는 한 공격에 사용할 값의 묶음이다.
예를 들어 근접 패턴은 룩 프리팹이나 포효 벽 피해를 조회할 필요가 없다.

### 기물: 종류 분기를 공통 FSM에서 제거

`ChessMinion`은 접근 → 경고 → 공격 → 휴식의 순서를 관리한다.
구체적인 행동은 Inspector의 **Attack**에 연결된 `ChessMinionAttack` 구현이 맡는다.

- `SectorMinionAttack`: 폰의 근접 원형, 룩/비숍의 방향 고정 공격.
- `JumpMinionAttack`: 나이트의 착지점 예고와 점프.
- 새로운 행동은 `ChessMinionAttack`을 상속한 컴포넌트로 추가하고 프리팹에 연결한다.
- `Kind`는 기물의 이름/분류이며 공통 Update의 행동 선택 조건으로 사용하지 않는다.

`LegacyMinionAttackAdapter`에는 옛 종류 분기가 남아 있다. 공격 컴포넌트가 없는 예전 프리팹을
한 번 조립하는 호환 경로이며, 새 행동을 추가할 때 이 어댑터까지 수정할 필요는 없다.

## 3. SOLID를 우리 코드에 연결해서 이해하기

| 원칙 | 쉬운 의미 | 현재 코드에서 볼 예 |
|---|---|---|
| S: 단일 책임 | 서로 다른 이유로 바뀌는 일을 나눈다 | 장판 그림은 Visual, 피해·수명은 Field |
| O: 개방·폐쇄 | 새 행동을 넣을 때 안정적인 기존 흐름의 수정을 줄인다 | 새 기물 공격을 Attack 컴포넌트로 연결 |
| L: 리스코프 치환 | 구현을 바꿔도 호출자가 믿는 약속을 지킨다 | 모든 기물 공격은 준비 실패/완료를 같은 의미로 반환 |
| I: 인터페이스 분리 | 사용하는 기능만 계약으로 받는다 | 근접은 `IMeleeTuningSource`, 포효는 `IRoarTuningSource` |
| D: 의존성 역전 | 중요한 흐름이 교체 가능한 세부 구현에 묶이지 않게 한다 | 피해 장판이 메시 클래스 대신 `IDashScorchVisual` 사용 |

공식 개념 자료는 [Microsoft Learn의 아키텍처 원칙](https://learn.microsoft.com/ko-kr/dotnet/architecture/modern-web-apps-azure/architectural-principles)을 본다.
웹 애플리케이션 예제이므로 프로젝트 구성을 그대로 옮기기보다 **관심사 분리, 단일 책임, 의존성 역전** 설명을 먼저 읽는다.

중요한 구분:

- S는 함수마다 클래스를 만들라는 뜻이 아니다. 같은 이유로 함께 바뀌는 코드는 묶어도 된다.
- O는 기존 코드를 절대로 수정하지 말라는 뜻이 아니다. 반복적으로 늘어나는 종류 분기를 줄이는 방향이다.
- D와 DI는 다르다. D는 의존 방향의 원칙이고, DI는 사용할 구현을 밖에서 전달하는 방법이다.
- 인터페이스를 추가했다는 사실만으로 좋은 설계가 되지는 않는다. 교체할 대상과 계약이 분명해야 한다.
- `GetComponent` 자체가 SOLID 위반은 아니다. 현재 코드의 Awake는 기본 구현을 연결하는 조립 지점이다.
- 테스트 통과는 검사한 동작의 근거다. 설계가 앞으로도 모든 요구에 적합하다는 증명은 아니다.

## 4. 필요한 문법과 공식 자료

### 먼저: 프로퍼티, 인터페이스, 추상 클래스

| 문법 | 현재 코드에서 읽는 방법 | 공식 자료와 읽을 부분 |
|---|---|---|
| `get; private set;` | 상태는 밖에서 읽되 변경은 담당 클래스 안에서 수행 | [C# 프로퍼티](https://learn.microsoft.com/ko-kr/dotnet/csharp/programming-guide/classes-and-structs/properties): 접근자 설명 |
| `interface` | 호출자가 필요한 기능의 이름과 입력/출력을 정한 약속 | [C# 인터페이스](https://learn.microsoft.com/ko-kr/dotnet/csharp/fundamentals/types/interfaces): 선언과 구현 예제 |
| `abstract`, `override` | 공통 부모가 정한 동작을 자식이 구체화 | [C# abstract](https://learn.microsoft.com/ko-kr/dotnet/csharp/language-reference/keywords/abstract): 추상 멤버와 구현된 멤버 |
| `as` | 구현체가 특정 계약을 지원하면 그 계약으로 받음. 아니면 null | `MeleeComboPattern.Awake`의 `_tuningSource as IMeleeTuningSource` |
| `??` | 왼쪽이 null일 때 오른쪽 기본 구현을 사용 | 위 Awake에서 기본 `ChessCombatProfiles`를 연결하는 부분 |

`ChessMinionAttack`을 추상 클래스로 둔 이유는 공격 계약뿐 아니라 대상 참조와 공통 피해 적용 코드도 공유하기 때문이다.
반대로 수치 조회 인터페이스는 공통 상태나 구현이 필요 없어 작은 인터페이스로 두었다.
추상 클래스가 무조건 인터페이스보다 좋거나 나쁜 것은 아니다.

Microsoft Learn에는 최신 C# 문법도 함께 나온다. 처음에는 위 표의 문법과 `get/private set` 예제에 집중한다.
문서에 보이는 `field`, `required` 같은 최신 기능을 Unity 코드에 그대로 복사하지 말고 프로젝트의 C# 지원 버전을 확인한다.

### 다음: 이벤트와 종료 정리

[Microsoft Learn의 이벤트 설명](https://learn.microsoft.com/ko-kr/dotnet/csharp/programming-guide/events/)을 읽고
`SwordPoseBinding.Returned`와 `BossSword.HandleReturned`를 함께 본다.

`Returned?.Invoke()`는 구독자가 있을 때 복귀 완료를 알린다. 복귀 담당은 구독자가 이후 무엇을 할지 직접 결정하지 않는다.
`+=`는 알림을 받을 함수를 등록하고 `-=`는 등록을 해제한다.

[C# 예외 처리 문](https://learn.microsoft.com/ko-kr/dotnet/csharp/language-reference/statements/exception-handling-statements)에서는
`try-finally` 부분을 읽는다. 우리 코드에서는 `MeleeComboPattern.Execute`와 `BossPatternRunner`를 같이 봐야 한다.
Unity 코루틴을 중단할 때 무조건 모든 finally가 실행된다고 가정하지 않는다.
이 프로젝트의 러너는 실행 중인 IEnumerator를 직접 정리하며 Cleanup도 호출하도록 구현되어 있다.

### 그다음: 전략 패턴과 조합

[Unity 공식: ScriptableObjects as Delegate Objects](https://unity.com/how-to/scriptableobjects-delegate-objects)에서
Strategy pattern과 교체 가능한 동작 예제를 읽는다.

공식 예제는 ScriptableObject를 사용하지만 현재 기물은 컴포넌트를 사용한다.
이 프로젝트에서 가져온 핵심은 **공통 흐름이 교체 가능한 행동을 호출한다는 구조**다.
예제를 따라 하려고 지금 당장 모든 공격을 ScriptableObject로 바꿀 필요는 없다.

현재 연결은 다음과 같다.

```text
ChessMinion의 공통 순서
    ↓ ChessMinionAttack 계약
    ├─ SectorMinionAttack
    └─ JumpMinionAttack
```

### 마지막: Unity 저장과 검증

[Unity 직렬화 규칙](https://docs.unity.com/en-us/engine/6000.0/manual/programming-environment/code-reload-serialization/script-serialization/rules)에서
SerializeField, Serializable, UnityEngine.Object 참조와 일반 클래스의 저장 차이를 읽는다.
Unity 문서는 현재 프로젝트의 Unity 6 버전을 선택해 확인한다.

필드 이름이나 담당 컴포넌트를 옮기는 것은 단순한 C# 코드 이동으로 끝나지 않는다.
Inspector 값은 씬/프리팹에 저장되므로 이번에는 기존 필드 값을 새 컴포넌트로 옮겨 연결했다.
`FormerlySerializedAs`는 필드 이름 변경에 유용하지만 다른 컴포넌트로 데이터를 전부 자동 이전하는 기능은 아니다.
스크립트와 에셋의 GUID는 .meta에 있으므로 Unity 파일을 이동할 때 함께 유지한다.

[Unity Test Framework 1.8 매뉴얼](https://docs.unity3d.com/Packages/com.unity.test-framework@1.8/manual/index.html)을 열고
Test Runner, Edit Mode, Play Mode 구분을 읽는다. 이 프로젝트에서는 수학/모델 검사와 실제 NavMesh·콜라이더 검사의 성격이 다르다.
테스트 파일에서는 코드 줄을 그대로 따라 하는 검사보다 **중단되어도 검이 복귀하는 동작**, **벽 피해가 한 번만 적용되는 동작** 같은 보장을 읽는다.

## 5. 자료를 찾는 사이트와 검색어

| 찾을 내용 | 먼저 볼 사이트 | 검색어 예시 |
|---|---|---|
| C# 문법 | Microsoft Learn | `site:learn.microsoft.com/ko-kr/dotnet/csharp interface abstract override` |
| 설계 원칙 | Microsoft Learn 아키텍처 문서 | `site:learn.microsoft.com architectural principles dependency inversion` |
| Unity 기능과 API | Unity Manual / Script Reference | `site:docs.unity.com Unity serialization rules`, `Unity Transform.SetParent` |
| Unity의 패턴 적용 예 | Unity 공식 how-to | `site:unity.com/how-to strategy pattern delegate objects` |
| 테스트 | 설치한 Unity Test Framework 버전의 매뉴얼 | `Unity Test Framework 1.8 PlayMode UnityTest` |

개인 블로그나 영상은 사례를 볼 때 도움이 되지만, API 동작과 버전 조건은 공식 문서에서 다시 확인한다.
검색 결과의 글이 멋진 구조를 보여 주더라도 지금 프로젝트의 문제를 줄여 주는지부터 판단한다.

## 6. 현재 Inspector에서 조절하는 위치

- 검 차징 각도·곡선: 보스의 **Melee Motion Source > Motion**.
- 2페이즈 공격 수치: 보스의 **Chess Combat Profiles > Phase Two**.
- 전환 대기 시간: **Phase Two Encounter > Transition Duration**.
- 검은 킹 모델과 머티리얼: **Chess Phase Appearance**.
- 2페이즈 기물 후보: **Chess Summon Catalog > Phase Two Prefabs**.
- 기물별 공격 행동: 기물 프리팹의 **Chess Minion > Attack**.
- 장판 표시 머티리얼: 보스의 **Dash Scorch Factory > Material**.
- 기본 수치 공급자 대신 다른 구현을 쓸 경우: 패턴의 **Tuning Source**에 해당 인터페이스를 구현한 컴포넌트를 연결.

먼저 한 기능이 시작하고 끝나는 흐름을 이해한 뒤 다른 기능을 읽는다.
모든 클래스와 패턴 이름을 한 번에 외우는 것이 학습 목표는 아니다.

## 7. 이번 변경의 검증 범위

Unity 6000.6.3f1 별도 검증 프로젝트에서 PlayMode 72개가 통과했다.
기존 전투 회귀 검사 65개와 구조 분리 관련 검사 7개를 포함한다.
공통 검 복귀, 표현 컴포넌트 없이 동작 계산, 대체 공격/수치 공급자 연결, 메시 없는 장판 표시 교체 등을 검사했다.
실제 DEV_SCENE 전체 화면의 잔상 모양과 시각적 밸런스는 별도의 게임 뷰 확인 대상이다.
