using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>투척 동작이 알아야 하는 소유권 계약만 제공한다. 생성형/실물형 수명 차이는 구현체가 맡는다.</summary>
    internal interface IThrownSwordOwnership
    {
        Transform Prepare();
        void Embed();
        void Complete(bool broken, bool deferParenting);
    }

    internal sealed class InventorySwordOwnership : IThrownSwordOwnership
    {
        private readonly BossSword _sword;
        public InventorySwordOwnership(BossSword sword) => _sword = sword;
        public Transform Prepare()
        {
            _sword.PoseBinding.TryCapture();
            _sword.transform.SetParent(null, true);
            return _sword.transform;
        }
        // TODO : 2026.11.02 수정자 최원희 이유 : 검이 바닥에 박히고 우뚝 스지 않게 하기 위해서  
        public void Embed()
        {
            _sword.TryEmbed();
            // _sword.transform.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.down);
        }
        public void Complete(bool broken, bool deferParenting)
        {
            _sword.SwingTrail.StopAndClear();
            if (broken)
            {
                _sword.TryBreak();
                Object.Destroy(_sword.gameObject);
            }
            else _sword.PoseBinding.Restore(deferParenting);
        }
    }

    // 공전 검 목록이 없는 기존 프리팹의 일회용 수명을 분리하여 실물 검의 복귀 코드에 섞지 않는다.
    internal sealed class TransientSwordOwnership : IThrownSwordOwnership
    {
        private readonly GameObject _root;
        private readonly Transform _visual;
        public TransientSwordOwnership(GameObject root, Transform visual) { _root = root; _visual = visual; }
        public Transform Prepare() => _visual;
        public void Embed() { if (_visual != null) _visual.localRotation = Quaternion.Euler(180f, 0f, 0f); }
        public void Complete(bool broken, bool deferParenting) => Object.Destroy(_root);
    }
}
