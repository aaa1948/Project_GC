using UnityEngine;

namespace Vampire
{
    [CreateAssetMenu(fileName = "Trap Monster", menuName = "Blueprints/Monsters/Trap Monster", order = 2)]
    public class TrapMonsterBlueprint : MonsterBlueprint
    {
        [Header("Trap State")]
        [Tooltip("활성화 상태일 때 함정 몬스터의 체력입니다.")]
        public float activeHealth = 30f;

        [Tooltip("플레이어를 한 번에 주는 틱 데미지입니다.")]
        public float tickDamage = 5f;

        [Tooltip("틱 데미지를 주는 간격입니다.")]
        public float tickInterval = 0.5f;

        [Tooltip("활성화되면 플레이어를 이 시간 동안 묶습니다. 0 이하이면 무제한으로 묶습니다.")]
        public float bindDuration = 4f;

        [Tooltip("활성화 직후 자동으로 플레이어를 묶습니다.")]
        public bool bindPlayerOnActivate = true;

        [Header("Trap Spawn - Test")]
        [Tooltip("테스트용: 체크하면 스폰 시 플레이어 근처로 위치를 보정합니다.")]
        public bool spawnNearPlayerForTest = true;

        [Tooltip("플레이어와의 최소 거리입니다.")]
        public float spawnMinDistanceFromPlayer = 2f;

        [Tooltip("플레이어와의 최대 거리입니다.")]
        public float spawnMaxDistanceFromPlayer = 4f;

        [Header("Trap Visual")]
        [Tooltip("휴면 상태 스프라이트들입니다. 1장이면 정지 이미지, 여러 장이면 루프 애니메이션처럼 재생됩니다.")]
        public Sprite[] dormantSprites;

        [Tooltip("활성화 상태 스프라이트들입니다.")]
        public Sprite[] activeSprites;

        [Tooltip("사망 상태 스프라이트들입니다.")]
        public Sprite[] deathSprites;

        [Tooltip("스프라이트 프레임 간격입니다.")]
        public float animationFrameTime = 0.15f;

        [Tooltip("사망 애니메이션 종료 후 오브젝트 제거까지 대기 시간입니다.")]
        public float deathDespawnDelay = 0.15f;
    }
}