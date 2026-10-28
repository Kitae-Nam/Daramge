# Runtime 파일 찾기

공용 기반과 체스 보스 전용 구현을 나눴다. 파일명, namespace, 기존 .meta GUID는 유지했다.
Unity의 씬/프리팹은 경로가 아니라 GUID로 스크립트를 찾으므로 기존 연결을 유지한다.

| 폴더 | 찾을 수 있는 기능 |
| --- | --- |
| Common/Health | 체력 모델, 보스 체력, 그로기, 파괴 가능한 물체 체력 |
| Common/Combat | 패턴 계약/실행기/상태, 공격 판정, 전투 잠금, 피해/밀치기 인터페이스 |
| Common/Movement | 일반 NavMesh 추적, 회전, 대상, 이동 상태 제어, 밀치기, 이동 계약 |
| Common/Telegraphs | 셰이더 진행도/투명도, 원형/부채꼴 표시, 기존 이동 파티클 |
| Common/Cinematics | Timeline을 전투 잠금으로 연결하는 BossCutsceneGate |
| Chess/AI | 1페이즈 패턴 선택, 체력 페이즈 판단, 페이즈 종료 정리 |
| Chess/Movement | 체스판 좌표 ChessBoardGrid, 킹 한 칸 추적 ChessKingGridMovement |
| Chess/Patterns | 2연격, 소환, 대쉬, 검 투척, 포효의 실행 순서 |
| Chess/Dash | 대쉬 이동, 직사각형 경고 배치, 고속 명중 판정 |
| Chess/Summons | 기물 목록/피해 감소, 폰/나이트의 행동 |
| Chess/Weapons | 던진 검의 이동/수명, 파괴 보상/회수 |
| Chess/Presentation | 검 자세와 그로기/타격 시각 연출 |
| Demo | 테스트 경기장의 저장된 NavMesh 등록 |

## 이번 변경을 읽는 순서

1. Chess/Weapons/ThrownBossSword.cs: Launch에서 칼끝을 비행 방향에 맞추고 Update에서는 직선 보간만 한다.
2. Common/Combat/BossCombatGate.cs → Common/Cinematics/BossCutsceneGate.cs: 차단 사유 관리와 Timeline 감지를 분리했다.
3. Chess/AI/PhaseOneBrain.cs: 각 패턴의 GetSelectionScore로 거리 적합도를 받고 쿨타임을 확인한다.
4. Chess/Movement/ChessBoardGrid.cs: 월드 위치 ↔ 칸 좌표, BFS로 다음 한 칸 찾기.
5. Chess/Movement/ChessKingGridMovement.cs: 통과 확인 → 한 칸 이동 → 정지 → 다음 칸 판단.
6. Common/Movement/BossMovementStateController.cs: IBossChaseMovement를 통해 일반 이동과 킹 이동을 같은 방식으로 제어한다.

다른 보스는 BossMovement를 사용하면 된다. 킹 이동은 해당 체스 보스의 Movement Driver에만 연결한다.
폰/나이트에는 이번 킹 전용 그리드 이동을 추가하지 않았다.

## 그리드 이동 파티클과 그로기 정지 확인

BossMovementEffect는 Agent.velocity 대신 LateUpdate에서 보스의 실제 수평 위치 변화를 확인한다.
그리드 이동은 Agent를 정지시킨 채 직접 위치를 갱신하므로, Agent 속도만 보면 움직임을 놓칠 수 있기 때문이다.
정지 시 StopEmitting으로 새 먼지만 멈추고 기존 먼지는 수명이 끝날 때까지 남긴다.

킹 루트에는 ChessKingGridMovement가 하나만 있어야 한다. 중복 추가를 막는 속성도 적용했다.
BossMovementStateController의 Movement Driver에는 그 루트의 ChessKingGridMovement를 연결한다.
그리드 이동기도 매 프레임 러너의 상태를 확인해, 그로기 중에는 진행 중인 한 칸 이동과 새 이동을 막는다.

확인은 플레이 모드에서 보스를 멀리서 유도한 뒤, 이동 중 BossGroggyController의
`테스트/그로기 진입`을 실행한다. 그로기 동안 루트의 위치가 유지되고 새 먼지가 나오지 않아야 한다.
그로기가 끝나고 다른 차단 사유가 없으면 정렬/추적과 파티클이 다시 시작되어야 한다.
씬 파일의 컴포넌트 연결도 수정했으므로 이미 열려 있던 씬은 다시 열어 확인한다.
