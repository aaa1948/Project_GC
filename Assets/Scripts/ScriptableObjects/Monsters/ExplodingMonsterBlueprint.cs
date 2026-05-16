using UnityEngine;

namespace Vampire
{
    [CreateAssetMenu(
        fileName = "Exploding Monster",
        menuName = "Blueprints/Monsters/Exploding Monster",
        order = 10)]
    public class ExplodingMonsterBlueprint : MonsterBlueprint
    {
        [Header("Exploding Monster / 자폭 몬스터")]
        [Tooltip("플레이어 판정 레이어입니다. 기존 MeleeMonsterBlueprint의 Melee Layer와 같은 플레이어 레이어를 넣으면 됩니다.")]
        public LayerMask playerLayer;

        [Tooltip("플레이어와 접촉하거나 폭발 범위 안에 있을 때 주는 피해입니다. 일반 몬스터보다 높게 설정합니다.")]
        public float explosionDamage = 45f;

        [Tooltip("폭발 피해 반경입니다. 0.35~0.75 정도부터 테스트하는 것을 추천합니다.")]
        public float explosionRadius = 0.55f;

        [Tooltip("폭발 시 플레이어에게 주는 넉백 힘입니다. 0이면 넉백 없음.")]
        public float explosionKnockback = 0f;

        [Tooltip("스폰 직후 이 시간 동안은 플레이어와 닿아도 폭발하지 않습니다. 스폰 직후 억까 방지용입니다.")]
        public float armDelay = 0.25f;

        [Tooltip("이 거리 안에 플레이어가 들어오면 경고 연출을 시작합니다.")]
        public float warningDistance = 2.2f;

        [Tooltip("경고 상태에서 깜빡이는 속도입니다.")]
        public float warningBlinkSpeed = 12f;

        [Tooltip("자폭으로 죽었을 때 보상을 지급할지 여부입니다. 기본은 false 추천입니다.")]
        public bool rewardOnSelfExplosion = false;

        [Tooltip("자폭 몬스터 디버그 로그를 출력합니다.")]
        public bool debugLog = false;
    }
}