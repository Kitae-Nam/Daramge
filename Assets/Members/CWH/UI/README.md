# 보스 체력바 (UI Toolkit)

붕괴: 스타레일 참고 이미지처럼 상단 중앙에 원형 초상화, 빨간 페이즈 원,
보스 이름, 얇은 빨간 체력바를 표시한다. 하얀 저항도 게이지는 포함하지 않는다.
초상화는 직접 지정하는 Sprite를 사용하며 미지정 시 B 문자가 표시된다.

## 씬 연결

1. Unity에서 BossHealth가 있는 보스를 선택한다.
2. `GameObject > CWH > Boss Health Bar (UI Toolkit)`을 실행한다.
3. 생성된 오브젝트의 BossHealthBarController에서 Boss, Boss Name, Portrait,
   Phase Count, Current Phase를 지정한다. 선택했던 보스는 자동 연결된다.
4. 기존 개발 씬의 BossHealthBarUI를 사용하려면 새로 생성하지 않고,
   기존 PanelRenderer와 같은 오브젝트에 BossHealthBarController를 추가하고 Boss를 연결한다.
   새 UI를 생성한 경우에는 중복 표시가 되지 않도록 기존 오브젝트를 비활성화한다.

수동 연결 시 PanelRenderer에 `BossHealthBar UI.uxml`과
`Default Panel Settings.asset`을 넣고 BossHealthBarController를 추가한다.
Unity 6.6의 PanelRenderer.RegisterUIReloadCallback으로 UI 루트를 전달받으므로
최초 생성과 UXML 리로드, 컨트롤러 비활성화 후 재활성화 시 View를 다시 연결한다.
버전 번호를 제공하는 VersionedUIReloadCallback을 사용해 같은 트리의 중복 알림은 건너뛴다.

## 전투 코드 연결

```csharp
// 보스 교체 시 이벤트 구독과 체력 잔상을 함께 초기화한다.
bossHud.Bind(bossHealth, "THE KING", portraitSprite, phaseCount: 5);

// 페이즈 전환 규칙은 보스가 결정하고 UI에는 결과만 전달한다(1부터 시작).
bossHud.SetPhaseState(phaseCount: 5, currentPhase: 2);

// 전투 종료 후 숨기려면 바인딩을 해제한다.
bossHud.Bind(null, "", null, phaseCount: 1);
```

Phase Count만큼 원을 생성하고, 지난 페이즈는 어둡게, 현재 페이즈는 밝게,
남은 페이즈는 빨갛게 표시한다. UI는 체력 비율로 페이즈를 임의 추측하지 않는다.
페이즈가 바뀔 때 체력을 다시 채우는 처리는 보스 시스템에서 담당한다.

실제 체력은 BossHealth.HealthChanged 즉시 갱신된다. 피해 잔상은 Lag Delay(기본 0.25초)
후 Catch Up Duration(기본 0.8초에 전체 폭 이동)의 속도로 따라온다.
연속 피해 시 대기 시간을 재시작한다. 회복과 보스 교체 시 잔상은 즉시 맞춘다.
UI 애니메이션은 unscaledDeltaTime을 사용하므로 일시정지 중에도 계속 진행한다.
보스 사망 시 0%와 잔상을 보여주며, 숨김은 Bind(null, ...)로 제어한다.

## 구조

- HealthBarLag: UI와 독립적인 지연/추격 계산. 시간 값을 전달받아 프레임률에 관계없이 검증한다.
- BossHealthBarView: UXML 조회, 초상화, 원, 게이지 표시만 담당한다.
- BossHealthBarController: 보스 이벤트의 구독/해제와 위 두 객체의 연결만 담당한다.
- Editor/BossHealthBarMenu: 생성 편의 기능을 에디터 전용 어셈블리로 격리한다.

