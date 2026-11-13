# King PhaseTwo Reveal

적용 씬: `Assets/Members/CWH/SCENCES/DEV_SCNE_2.unity`  
대상: `The_King(Boss)Real`  
씬 오브젝트: `King PhaseTwo Cutscene`  
길이: 5.5초 / 60fps

| 시간 | 연출 |
| --- | --- |
| 0–1.0초 | 현재 게임플레이 카메라에서 보스 전용 카메라로 블렌드 |
| 0.35–2.95초 | 발밑의 금색 마법진 |
| 0.6–2.65초 | 보라색 기운 응축, 카메라 접근 및 FOV 36도 |
| 2.65초 | Dark Fire 폭발과 함께 Signal로 검은 킹 외형 공개 |
| 2.65–4.95초 | 각성 오라, 카메라 반동과 여운 |
| 4.3–5.5초 | 시작 시 선택된 게임플레이 카메라로 블렌드 복귀 |

## 확인과 편집

1. Hierarchy에서 `King PhaseTwo Cutscene`을 선택하고 Timeline 창을 연다.
2. Playable Director의 에셋은 `King_PhaseTwo_Reveal.playable`이다. Play On Awake는 꺼져 있다.
3. 카메라 움직임은 `King_PhaseTwo_Camera.anim`의 `PhaseTwoCamera` 로컬 위치·회전·렌즈를 편집한다. 이 씬은 직교 투영이므로 실제 화면의 줌은 `Lens.OrthographicSize`가 결정한다. FOV 곡선도 함께 제공한다.
4. 파티클은 각 Control Track의 클립 시간과 씬의 해당 자식 오브젝트에서 조정한다.
5. Play Mode에서 보스 체력을 60% 이하로 낮추면 자동 재생된다. 기본 최대 체력 1000이면 체력 컴포넌트의 `테스트/기본 피해 100`을 네 번 실행한다.
6. 컷신 컴포넌트의 `테스트/2페이즈 컷신 재생 (Play Mode)`로 카메라를 다시 확인할 수도 있다. 외형 전환까지 확인하려면 실제 2페이즈로 진입한다.

## 구조를 이렇게 나눈 이유

- `BossPhaseController`는 기존 체력/페이즈 판단을 유지한다.
- `BossPhaseCutscene`은 이벤트 구독, 기준점 배치, 전투·피해 잠금, 종료 정리만 담당한다.
- Timeline은 카메라 구도, 파티클 시간, 외형 공개 시점을 담당한다. Follow 및 회전 추적 컴포넌트와 Transform 애니메이션이 경쟁하지 않는다.
- 기준점은 재생 직전 본체 Renderer의 중심·높이와 보스의 Y축 방향을 한 번 복사한다. 보스의 자식으로 따라가지 않기 때문에 이후 회전에 연출이 끌려가지 않는다.
- 카메라 리그는 재생 직전 플레이어 카메라의 월드 회전을 별도로 복사한다. 보스를 중심에 두면서 플레이어와 같은 방향·내려다보는 각도에서 줌인한다. 자식 카메라는 리그의 -Z축으로만 움직여 측면으로 돌아가지 않는다.
- 카메라와 이펙트 좌표는 보스 높이를 1로 정규화했다. 보스 크기를 바꿔도 함께 비례한다. Root Transform은 Timeline에서 애니메이션하지 않는다.
- 직교 렌즈 곡선은 제작 당시 보스 높이에 맞춰 생성한다. 보스 크기를 변경하면 `CWH > Boss > 2페이즈 컷신 렌즈 줌 보정`으로 렌즈 곡선을 다시 맞춘다. 투영 방식을 유지해 직교/원근 전환 시 화면이 튀는 것을 피한다.
- 진입/복귀 Shot은 재생 시작 시 Brain이 사용 중인 카메라를 참조한다. 캐릭터별 카메라가 달라도 같은 에셋을 사용한다.
- 플레이어 입력 잠금은 Inspector의 Started/Finished 이벤트에서 기존 `CharacterChange.LockInput/UnlockInput`에 연결했다.
- `ChessPhaseAppearance._deferToCutscene`은 이 씬의 인스턴스에서만 켰다. 다른 씬은 기존처럼 즉시 검은 킹으로 전환한다.
- Stop, 비활성화, 사망 시에도 같은 정리 경로를 거쳐 피해/전투 잠금과 입력을 복원한다. Wrap Mode는 None이다.

## 사용한 프로젝트 파티클

- Epic Toon FX / `MagicCircleSimpleYellow`
- Epic Toon FX / `MagicChargeBlue` — 씬 인스턴스만 보라색으로 조정
- EffectCore / ShaderGraphVersion / `explosion_stylized_medium_darkFire_ShaderGraph`
- Epic Toon FX / `MagicAuraBlue` — 씬 인스턴스만 보라색으로 조정

기존 `2Phase TimeLine.playable`과 등장 Timeline은 별도 에셋으로 유지한다. 새 연출 생성 메뉴는 `CWH > Boss > 2페이즈 컷신 생성 및 연결`이며, 이미 생성된 에셋을 덮어쓰지 않는다.
