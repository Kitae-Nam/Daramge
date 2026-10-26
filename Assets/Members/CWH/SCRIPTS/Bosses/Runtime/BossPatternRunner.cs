using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 패턴 선택과 분리된 단일 실행기. 경고와 휴식 시간을 공통으로 보장하고 중복 실행을 차단한다.
    /// 각 보스의 AI는 TryRun만 호출하므로 선택 규칙을 바꿔도 실행 흐름을 수정할 필요가 없다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossHealth), typeof(BossGroggyController))]
    public sealed class BossPatternRunner : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _patternInterval = 1f;
        private readonly Dictionary<BossPattern, float> _readyTimes = new Dictionary<BossPattern, float>();
        private readonly Stack<IEnumerator> _executionStack = new Stack<IEnumerator>();
        private BossHealth _health;
        private BossGroggyController _groggy;
        private Coroutine _routine;
        private float _nextPatternTime;
        private bool _warningVisible;
        private bool _isFinishing;
        private bool _isAdvancing;
        private bool _cancelRequested;

        public BossPattern CurrentPattern { get; private set; }
        public BossActionState State { get; private set; }
        public event Action<BossActionState> StateChanged;

        private void Awake()
        {
            _health = GetComponent<BossHealth>();
            _groggy = GetComponent<BossGroggyController>();
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
            _groggy.Entered += OnGroggyEntered;
            _groggy.Exited += OnGroggyExited;
            SetState(RestState());
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
            _groggy.Entered -= OnGroggyEntered;
            _groggy.Exited -= OnGroggyExited;
            CancelCurrentPattern();
        }

        public bool TryRun(BossPattern pattern)
        {
            if (!IsReadyFor(pattern)) return false;

            CurrentPattern = pattern;
            _cancelRequested = false;
            // 중단으로 쿨타임을 우회하지 못하도록 패턴을 수락하는 순간부터 계산한다.
            _readyTimes[pattern] = Time.time + pattern.Cooldown;
            _routine = StartCoroutine(Run(pattern));
            return true;
        }

        /// <summary>AI가 선택 전에 실행 가능 여부만 조회한다. 조회는 쿨타임을 소비하지 않는다.</summary>
        public bool IsReadyFor(BossPattern pattern)
        {
            if (!isActiveAndEnabled || _isFinishing || !ReferenceEquals(CurrentPattern, null) || _health.IsDead ||
                _groggy.IsGroggy
                || pattern == null || !pattern.isActiveAndEnabled || !pattern.CanExecute() ||
                Time.time < _nextPatternTime)
                return false;
            return !_readyTimes.TryGetValue(pattern, out float readyTime) || Time.time >= readyTime;
        }

        public void CancelCurrentPattern()
        {
            // Execute 내부에서 그로기/사망이 발생하면 MoveNext가 반환된 뒤 iterator를 해제한다.
            // 실행 중인 iterator 자체를 Dispose하면 finally의 실행 순서가 깨질 수 있다.
            if (_isAdvancing)
            {
                _cancelRequested = true;
                return;
            }

            BossPattern pattern = CurrentPattern;
            Coroutine routine = _routine;
            _routine = null;
            if (routine != null) StopCoroutine(routine);
            FinishPattern(pattern);
        }

        private IEnumerator Run(BossPattern pattern)
        {
            // 첫 yield로 실행 핸들이 저장된 뒤 외부 이벤트가 발생하도록 순서를 보장한다.
            yield return null; // 다음 프레임까지 대기 
            try
            {
                if (pattern == null) yield break;
                _warningVisible = true;
                SetState(BossActionState.Warning);
                if (CurrentPattern != pattern) yield break;
                pattern.BeginWarning();
                if (CurrentPattern != pattern) yield break;
                float elapsed = 0f;
                float duration = pattern.WarningDuration;

                while (elapsed < duration)
                {
                    // 경고 중 패턴이 사라지거나 취소되면 진행을 종료한다.
                    if (pattern == null ||
                        CurrentPattern != pattern ||
                        !pattern.isActiveAndEnabled)
                    {
                        yield break;
                    }

                    pattern.UpdateWarningProgress(
                        Mathf.Clamp01(elapsed / duration));

                    // 진행도 전달 과정에서 취소됐을 가능성도 확인한다.
                    if (CurrentPattern != pattern) yield break;

                    yield return null;
                    elapsed += Time.deltaTime;
                }

                if (pattern == null || CurrentPattern != pattern)
                    yield break;

                // 프레임 간격 때문에 마지막 값이 1에 못 미치는 것을 보완한다.
                pattern.UpdateWarningProgress(1f);

                if (CurrentPattern != pattern) yield break;
                if (pattern == null || !pattern.isActiveAndEnabled || !pattern.CanExecute()) yield break;

                HideWarning(pattern);
                if (CurrentPattern != pattern) yield break;
                SetState(BossActionState.Executing);
                if (CurrentPattern != pattern) yield break;
                _executionStack.Push(pattern.Execute());

                // 중첩 IEnumerator를 직접 순회해 중단 시 모든 iterator를 Dispose한다.
                // 패턴의 finally도 실행되므로 임시 리소스가 중첩 코루틴 안에 남지 않는다.
                while (_executionStack.Count > 0 && CurrentPattern == pattern)
                {
                    if (pattern == null || !pattern.isActiveAndEnabled) yield break;
                    IEnumerator iterator = _executionStack.Peek();
                    bool moved;
                    _isAdvancing = true;
                    try
                    {
                        moved = iterator.MoveNext();
                    }
                    finally
                    {
                        _isAdvancing = false;
                    }

                    if (_cancelRequested || CurrentPattern != pattern) yield break;
                    if (!moved)
                    {
                        _executionStack.Pop();
                        (iterator as IDisposable)?.Dispose();
                    }
                    else if (iterator.Current is IEnumerator nested) _executionStack.Push(nested);
                    else yield return iterator.Current;
                }

                if (CurrentPattern != pattern) yield break;
                SetState(BossActionState.Recovery);
                yield return new WaitForSeconds(pattern.RecoveryDuration);
            }
            finally
            {
                FinishPattern(pattern);
            }
        }

        private void FinishPattern(BossPattern expectedPattern)
        {
            // 파괴된 Unity 객체도 iterator는 정리해야 하므로 실제 C# null과 구분한다.
            // 이전 코루틴의 finally가 이벤트에서 시작한 새 패턴을 정리하지 않게 소유권도 확인한다.
            if (_isFinishing || ReferenceEquals(CurrentPattern, null) ||
                !ReferenceEquals(CurrentPattern, expectedPattern)) return;
            _isFinishing = true;
            BossPattern pattern = CurrentPattern;
            CurrentPattern = null;
            _routine = null;
            _nextPatternTime = Time.time + Mathf.Max(0f, _patternInterval);
            try
            {
                // 한 iterator의 해제가 실패해도 나머지를 정리해 실행 상태에 고착되지 않게 한다.
                while (_executionStack.Count > 0)
                {
                    IDisposable iterator = _executionStack.Pop() as IDisposable;
                    try
                    {
                        iterator?.Dispose();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, this);
                    }
                }

                try
                {
                    HideWarning(pattern);
                }
                finally
                {
                    if (pattern != null) pattern.Cleanup();
                }
            }
            finally
            {
                _isFinishing = false;
                SetState(RestState());
            }
        }

        private void HideWarning(BossPattern pattern)
        {
            if (!_warningVisible) return;
            _warningVisible = false;
            if (pattern != null) pattern.EndWarning();
        }

        private BossActionState RestState() => _health.IsDead ? BossActionState.Dead
            : _groggy.IsGroggy ? BossActionState.Groggy : BossActionState.Idle;

        private void OnDied()
        {
            CancelCurrentPattern();
            SetState(BossActionState.Dead);
        }

        private void OnGroggyEntered()
        {
            CancelCurrentPattern();
            SetState(RestState());
        }

        private void OnGroggyExited() => SetState(RestState());

        private void SetState(BossActionState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
