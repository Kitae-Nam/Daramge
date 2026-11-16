# CWH 보완 및 파일 정리

## 구현

- 돌진의 물리 벽 검사에 Agent 반경을 반영해 몸통이 벽에 파고들지 않게 했다.
- 가장 가까운 칸이 막히면 이동 가능한 다음 칸 중심으로 복귀한다. 중심 조건과 벽 통과 검사는 유지한다.
- 검 체력 UI는 실행 시 분리해 검의 확대/회전을 받지 않고 검이 파괴되면 함께 삭제한다.
- 재사용 체력바 프리팹의 Fill/Canvas와 전체 너비를 교정하고 검의 UI 오프셋을 월드 위쪽 2로 수정했다.
- 보스 사망 시 소환체/투척 검/장판 정리가 끝나는 것을 실제 통합 테스트로 확인했다.

## 검증

Unity 6000.6.3f1 별도 프로젝트 PlayMode: 34/34 통과.
최신 결과: Validation~/BossPolishResults.xml
최신 로그: Validation~/BossPolishTests.log
재현 준비 스크립트: Validation~/PreparePhaseTwoValidation.ps1

## 정리

확보한 공간: 약 736.9 MiB.
과거 도구와 검증 기록 29개는 압축 후 SHA-256 검증을 거쳐 낱개 파일을 정리했다.
보관 위치: Validation~\Archive\legacy-validation-bb83f44b.zip
런타임 코드, 테스트 소스, 학습 문서, 씬/프리팹/머티리얼과 출처가 불분명한 에셋은 삭제하지 않았다.
주 프로젝트의 Library/Temp와 다른 멤버 파일은 정리 대상에서 제외했다.

정리 항목:
- Validation~\configure_assets.py
- Validation~\configure_player_damage.py
- Validation~\configure_prefab.py
- Validation~\DevScene3HitFeedback.log
- Validation~\DevScene3HitFeedback.result.txt
- Validation~\FixDevScene3HitFeedback.ps1
- Validation~\migrate_solid.py
- Validation~\PhaseTwoCompletionResults.xml
- Validation~\PhaseTwoCompletionTests.log
- Validation~\player-damage-assets.patch
- Validation~\player-damage-final-results.xml
- Validation~\player-damage-final-tests.log
- Validation~\player-damage-results.xml
- Validation~\player-damage-tests.log
- Validation~\playmode-results-2.xml
- Validation~\playmode-results-3.xml
- Validation~\playmode-results.xml
- Validation~\prepare_damage_validation.py
- Validation~\prepare_validation.py
- Validation~\scene.patch
- Validation~\solid-final-results.xml
- Validation~\solid-final-tests.log
- Validation~\solid-results.xml
- Validation~\solid-tests.log
- Validation~\unity-tests-2.log
- Validation~\unity-tests-3.log
- Validation~\unity-tests.log
- Verification\unity-tests.log
- Verification\unity-tests.log.meta
- Validation~/__pycache__
- Validation~/.dotnet
- Validation~/Compile/bin
- Validation~/Compile/obj
- Validation~/UnityProject/Library
- Validation~/UnityProject/Logs
- Validation~/PhaseTwoCompletionProject/Library
- Validation~/PhaseTwoCompletionProject/Logs
- Validation~/Assembly-CSharp-Editor.FeedbackVerification.dll
- Validation~/Assembly-CSharp-Editor.FeedbackVerification.ref.dll
- Validation~/Assembly-CSharp-Editor.FeedbackVerification.pdb
- Validation~/FeedbackVerification.rsp

사용 중이라 보존한 캐시:
