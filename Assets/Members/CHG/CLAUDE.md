# CHG 작업 폴더

## Claude 사용 규칙

- 항상 한국어로 답한다.
- Unity 관련 파일(코드 `.cs`, 씬 `.unity`, 프리팹, Animator, SO 에셋, `.meta` 등)은 직접 수정하지 않는다. 어떻게 수정할지만 알려주고, 실제 작업은 사용자가 한다.
- 문서(`.md`) 생성·수정 같은 Unity 밖의 작업은 직접 해도 된다.
- 수정안을 알려줄 때:
  - 파일 경로와 메서드 이름을 함께 적는다.
  - 파일 전체가 아니라 바뀐 부분만 보여준다.
    - 메서드를 수정했으면 그 메서드만 보여준다. 메서드 안은 생략 없이 전체를 적는다(`// ...기존 코드` 같은 표현 금지).
    - 필드, `using`을 추가·수정했으면 그 줄만 보여준다.
  - 바뀐 줄에는 `// 추가`, `// 수정` 주석을 붙이고, 삭제할 줄은 `// 삭제`로 표시해서 보여준다.
  - 새로 추가하는 필드, `using`, 메서드, 클래스도 빠짐없이 적는다.
  - 왜 이렇게 바꾸는지 이유를 함께 설명한다.
- 요청과 관련 없는 버그나 개선점을 발견하면 수정안에 섞지 말고 따로 알려준다.
- 명령에 모호한 부분이 있으면 추측해서 진행하지 말고, 먼저 질문해서 확실히 한 다음 진행한다.

## 개발 환경

- Unity 6.6 — 수정안의 API는 이 버전 기준으로 작성한다.

## 폴더 개요

이 폴더(`Assets/Members/CHG`)는 CHG 개인 작업 공간이다. 다른 멤버 폴더(`CWH`, `KU`, `NKT`)는 건드리지 않는다.

| 폴더 | 내용 |
| --- | --- |
| `01_Scenes` | 테스트 씬 |
| `02_Scripts` | C# 스크립트 |
| `03.Datas` | ScriptableObject 데이터, Input 에셋, Animator Controller |
| `04.Prefabs` | 모듈 / 스킬 프리팹 |
| `05.Graphic` | 모델, 애니메이션, 에셋 팩, 머티리얼, 셰이더 |

## 코딩 규칙

### 설계 원칙
- SOLID 원칙을 준수한다. 수정안이 원칙과 충돌하면 그 이유를 함께 설명한다.
  - **S** 단일 책임: 클래스는 한 가지 역할만 맡는다.
  - **O** 개방-폐쇄: 기능 추가는 기존 코드 수정보다 확장(상속, 인터페이스)으로 한다.
  - **L** 리스코프 치환: 자식 클래스는 부모 클래스 자리에 넣어도 문제없이 동작해야 한다.
  - **I** 인터페이스 분리: 인터페이스는 작게 나누고, 쓰지 않는 메서드 구현을 강요하지 않는다.
  - **D** 의존성 역전: 구체 클래스보다 인터페이스나 추상 클래스에 의존한다.

### 네임스페이스
- `CHG._02_Scripts.<폴더 경로>` 형식으로, 폴더 구조와 일치시킨다. (`Members.` 접두사는 붙이지 않는다.)
  - 예: `02_Scripts/Player/Combat/` → `CHG._02_Scripts.Player.Combat`

### 필드 / 프로퍼티
- 기본은 `private`.
- 인스펙터에서 값을 넣어야 할 때만 `[SerializeField] private`.
- 인스펙터 노출 + 외부에서 읽기가 필요할 때만 `[field: SerializeField] public X { get; private set; }`.
- 외부 공개가 필요한 값은 public 필드 대신 프로퍼티를 쓴다.

### 이름 규칙
| 대상 | 표기 | 예시 |
| --- | --- | --- |
| 클래스, 메서드, 프로퍼티 | PascalCase | `HeadbuttSkill`, `UseSkill()`, `IsUsing` |
| 직렬화 필드 | camelCase | `dashDuration`, `damageCaster` |
| 코드 내부용 private / protected 필드 | `_camelCase` | `_player`, `_movement` |
| 인터페이스 | `I` 접두사 | `IDamageable`, `ISkill` |
| 추상 클래스 | `Abstract` 접두사 | `AbstractPlayerSkill` |
| ScriptableObject | `SO` 접미사 | `StatSO`, `AnimParamSO` |
| 이벤트 핸들러 | `Handle` 접두사 | `HandleDeath`, `HandleHitEvent` |

### 기타
- 이벤트는 구독한 곳에서 `OnDestroy`(또는 대응되는 해제 시점)에 반드시 구독 해제한다.
- 필수 참조의 null 체크는 `Debug.Assert(x != null, $"... : {gameObject.name}");` 형식을 쓴다.
- 한 줄짜리 메서드 / 프로퍼티는 `=>`를 쓴다.
- 애니메이션은 문자열 대신 `AnimParamSO`의 `ParamHash`로 재생한다.
