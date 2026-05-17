using UnityEngine;

namespace Vampire
{
    public static class SyringeAbilityResolver
    {
        public static SyringeDartAbility FindOwnedOrFirst(AbilityManager abilityManager)
        {
            if (abilityManager == null)
            {
                return null;
            }

            SyringeDartAbility[] abilities =
                abilityManager.GetComponentsInChildren<SyringeDartAbility>(true);

            SyringeDartAbility first = null;

            for (int i = 0; i < abilities.Length; i++)
            {
                SyringeDartAbility ability = abilities[i];

                if (ability == null)
                {
                    continue;
                }

                if (first == null)
                {
                    first = ability;
                }

                // 실제로 플레이어가 소유 중인 시작 침 능력을 우선 사용
                if (ability.Owned)
                {
                    return ability;
                }
            }

            return first;
        }
    }
}