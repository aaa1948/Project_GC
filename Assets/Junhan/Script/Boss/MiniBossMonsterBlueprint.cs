using UnityEngine;

namespace Vampire
{
    [CreateAssetMenu(
        fileName = "Mini Boss Monster",
        menuName = "Blueprints/Monsters/Mini Boss Monster",
        order = 30)]
    public class MiniBossMonsterBlueprint : MeleeMonsterBlueprint
    {
        [Header("Mini Boss")]
        [Tooltip("미니보스 외형 크기 배율입니다. 1.5면 일반 몬스터보다 1.5배 크게 보입니다.")]
        public float visualScaleMultiplier = 1.5f;

        [Tooltip("이 값이 true면 스폰 로그를 출력합니다.")]
        public bool debugLog = true;
    }
}