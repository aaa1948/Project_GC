using UnityEngine;

namespace Vampire
{
    [CreateAssetMenu(
        fileName = "Elite Monster",
        menuName = "Blueprints/Monsters/Elite Monster",
        order = 2
    )]
    public class EliteMonsterBlueprint : MonsterBlueprint
    {
        [Header("Elite Settings")]
        [Tooltip("엘리트 몬스터 외형 크기 배율입니다. 2면 일반 몬스터의 2배 크기입니다.")]
        public float scaleMultiplier = 2f;

        [Tooltip("엘리트 몬스터 체력 배율입니다. 2면 최종 체력이 2배입니다.")]
        public float hpMultiplier = 2f;

        [Header("Elite Silver Reward")]
        [Tooltip("기존 실버 보상 함수를 몇 번 호출할지 정합니다. 1이면 일반 몬스터와 동일, 2면 대략 2배 보상입니다.")]
        public int silverRewardCalls = 2;

        [Header("Elite Coin Reward")]
        [Tooltip("엘리트 처치 시 추가로 확정 드롭할 코인 개수입니다.")]
        public int guaranteedExtraCoinCount = 2;

        [Tooltip("엘리트 처치 시 추가로 드롭할 코인 종류입니다.")]
        public CoinType guaranteedExtraCoinType = CoinType.Bronze1;

        [Tooltip("추가 코인이 한 점에 겹치지 않도록 퍼지는 반경입니다.")]
        public float extraCoinScatterRadius = 0.35f;

        [Header("Debug")]
        [Tooltip("엘리트 몬스터 스폰/사망 로그를 출력합니다.")]
        public bool debugLog = false;
    }
}