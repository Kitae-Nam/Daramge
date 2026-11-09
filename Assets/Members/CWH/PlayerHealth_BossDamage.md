# 보스 공격과 PlayerHealthModule 연결

## 현재 규칙

| 공격 | 1페이즈 | 2페이즈 |
|---|---:|---:|
| 근접 공격 한 타격 | 1 | 2 |
| 대쉬 한 번 명중 | 1 | 2 |
| 투척 검 한 자루 착지 명중 | 1 | 2 |
| 소환 기물 한 번 명중 | 1 | 2 |
| 대쉬 장판 | 없음 | 피해 주기마다 1 |
| 포효 직격 | 피해 없이 밀치기 | 피해 없이 밀치기 |
| 포효로 밀린 뒤 벽 충돌 | 없음 | 2 |

여러 검에 각각 맞거나 연격을 여러 번 맞으면 각 명중의 피해를 받는다.
같은 판정 안에서 플레이어의 콜라이더가 여러 개 겹쳐도 기존 HashSet으로 중복 피해를 막는다.
장판의 피해 주기와 수명, 공격의 범위와 속도는 기존 설정을 유지했다.
검 파괴로 보스에게 주는 추가 피해와 보스의 그로기 피해 배율은 별개이므로 바꾸지 않았다.

## 코드 흐름과 역할

```text
BossAttackDamage: 페이즈별 피해량 결정
    ↓
근접/대쉬/투척/기물의 명중 판정
    ↓ IDamageReceiver.ReceiveDamage
PlayerHealthDamageReceiver: 호출 형식 변환
    ↓ PlayerHealthModule.ApplyDamage
플레이어의 체력 변경 및 사망 알림
```

- `BossAttackDamage`는 1페이즈 1, 2페이즈 2, 장판 1이라는 전투 규칙만 담당한다.
- `PlayerHealthDamageReceiver`는 인터페이스 호출을 실제 플레이어 모듈의 함수로 바꾸는 어댑터다.
- `BossPlayerHealthBinding`은 보스의 BossTarget에서 플레이어를 찾아 어댑터를 연결하는 조립 담당이다.
- CHG 플레이어 코드와 프리팹은 수정하지 않는다. 플레이어의 실제 체력 모듈에 피해를 요청한다.

보스 Runtime은 asmdef 어셈블리이고 PlayerHealthModule은 Assembly-CSharp에 있다.
Runtime에서 PlayerHealthModule을 직접 참조하면 어셈블리 의존 방향이 맞지 않는다.
두 구체 구현을 함께 볼 수 있는 Assembly-CSharp에 어댑터를 두고 Runtime은 기존 인터페이스를 사용한다.
이렇게 해야 공격 코드마다 플레이어 구현을 직접 참조하지 않는다(DIP).

## 씬과 프리팹 연결

`The_King(Boss)`와 `PhaseOneBoss` 프리팹 루트에 BossPlayerHealthBinding을 추가했다.
FuseScene과 DEV_SCENE의 해당 프리팹 인스턴스는 이 컴포넌트를 상속한다.
BossTarget에 실제 플레이어 루트를 연결하면 시작 시 그 아래의 PlayerHealthModule을 찾아 연결한다.
플레이어 타깃이 바뀌거나 체력 모듈이 늦게 준비되면 다시 찾는다.
어댑터를 여러 번 설치하지 않고 기존 컴포넌트를 재사용한다.

별도의 보스 오브젝트를 직접 만들 경우 BossTarget과 BossPlayerHealthBinding을 함께 연결한다.
테스트용 IDamageReceiver만 있고 PlayerHealthModule이 없는 대상에는 어댑터를 추가하지 않는다.

## 왜 실제 감소량을 반환하는가

IDamageReceiver의 반환값은 요청한 피해가 아니라 실제로 감소한 체력이다.
남은 체력이 1일 때 피해 2를 요청해도 감소량은 1이다.
어댑터는 ApplyDamage 전후 체력을 비교해 이 계약을 지킨다(LSP).

PlayerHealthModule은 ApplyDamage(0)도 최소 1 피해로 처리하므로,
어댑터는 0 이하, NaN, Infinity를 먼저 거른다.
피해 요청만으로 새 체력을 계산하지 않고 기존 모듈의 정수 반올림과 사망 처리를 사용한다.

## 피해량을 변경할 때

보스 전투의 최종 피해 규칙은 BossAttackDamage에 있다.
기존 패턴의 Damage와 2페이즈 DamageMultiplier는 페이즈 컨트롤러가 없는 독립 실행의 호환 설정이다.
정상 보스 전투에서는 최종 피해가 페이즈 규칙으로 결정되므로 오래된 저장값이 큰 피해를 주지 않는다.
프리팹에 저장된 기존 피해 수치도 1/2로 함께 맞췄다.

투척 피해는 던질 때, 기물 피해는 소환 초기화 때 결정한다.
검이 날아가는 중 페이즈가 변해도 이미 시작한 공격의 피해가 갑자기 바뀌지 않는다.

## 검증

Unity 6000.6.3f1의 별도 검증 프로젝트에서 PlayMode 테스트 85개가 통과했다.
이번에 추가한 13개에는 실제 PlayerHealthModule 연동, 1/2 피해,
다중 콜라이더 중복 방지, 투척 피해, 장판 틱당 1 피해, 포효 직격 무피해와 벽 충돌 2 피해 검증이 포함된다.
기존 자연 회수 테스트 2개는 순간 복귀 대신 현재 회수 연출 완료를 기다리도록 갱신했다.
FuseScene 전체를 실제로 조작하는 시각적 플레이 검증은 포함하지 않는다.
