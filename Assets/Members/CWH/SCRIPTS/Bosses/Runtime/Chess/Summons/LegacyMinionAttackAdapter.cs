using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 공격 컴포넌트가 저장되지 않은 옛 프리팹만 한 번 조립한다.
    /// 새 행동은 프리팹에 직접 연결하므로 이 종류 분기를 확장할 필요가 없다.
    /// </summary>
    internal static class LegacyMinionAttackAdapter
    {
        public static ChessMinionAttack Resolve(GameObject owner, ChessMinionKind kind, float range, float angle)
        {
            if (kind == ChessMinionKind.Knight) return owner.AddComponent<JumpMinionAttack>();
            var sector = owner.AddComponent<SectorMinionAttack>();
            if (kind == ChessMinionKind.Rook || kind == ChessMinionKind.Bishop)
                sector.ConfigureLine(range, angle, kind == ChessMinionKind.Bishop ? 45f : 0f);
            return sector;
        }
    }
}
