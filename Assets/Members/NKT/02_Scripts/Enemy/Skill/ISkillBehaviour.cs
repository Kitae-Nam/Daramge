
using CHG._02_Scripts.CoreSystem.AnimationSystem;

namespace NKT.Enemy.Skill
{
    //스킬 오브젝트에 붙여서 스킬의 동작을 조합하는 부품
    public interface ISkillBehaviour
    {
        void OnSkillStart(EnemySkill skill);
        void OnSkillEvent(AnimParamSO skillEvent);
        void OnSkillUpdate();
        void OnSkillEnd(); //정상 종료, 강제 종료(피격 등) 모두 호출된다.
    }
}
