using UnityEngine;

namespace Vampire
{
    [CreateAssetMenu(
        fileName = "Elite Melee Monster",
        menuName = "Blueprints/Monsters/Elite Melee Monster",
        order = 2
    )]
    public class EliteMonsterBlueprint : MeleeMonsterBlueprint
    {
        [Header("Elite Source / 원본 몬스터 참조")]
        [Tooltip("이 엘리트가 외형을 따라갈 원본 일반 근접 몬스터 Blueprint입니다. 예: 初級小兵, 中級小兵, 高級小兵")]
        public MeleeMonsterBlueprint sourceNormalBlueprint;

        [Tooltip("체크하면 원본 일반 몬스터 Blueprint의 walkSpriteSequence와 walkFrameTime을 사용합니다.")]
        public bool useSourceVisual = true;

        [Tooltip("체크하면 원본 일반 몬스터 Blueprint의 이동속도, 가속도, 공격력, 공격속도, 근접 레이어를 사용합니다.")]
        public bool useSourceCombatStats = true;

        [Tooltip("체크하면 원본 일반 몬스터 Blueprint의 경험치/코인 드롭 테이블을 기본 드롭으로 사용합니다.")]
        public bool useSourceLootTables = true;

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

        public MeleeMonsterBlueprint GetSourceOrSelf()
        {
            if (sourceNormalBlueprint != null)
            {
                return sourceNormalBlueprint;
            }

            return this;
        }

        public Sprite[] GetEffectiveWalkSpriteSequence()
        {
            if (useSourceVisual &&
                sourceNormalBlueprint != null &&
                sourceNormalBlueprint.walkSpriteSequence != null &&
                sourceNormalBlueprint.walkSpriteSequence.Length > 0)
            {
                return sourceNormalBlueprint.walkSpriteSequence;
            }

            return walkSpriteSequence;
        }

        public float GetEffectiveWalkFrameTime()
        {
            if (useSourceVisual && sourceNormalBlueprint != null)
            {
                return sourceNormalBlueprint.walkFrameTime;
            }

            return walkFrameTime;
        }

        public float GetEffectiveBaseHP()
        {
            if (sourceNormalBlueprint != null)
            {
                return sourceNormalBlueprint.hp;
            }

            return hp;
        }

        public float GetEffectiveMoveSpeed()
        {
            if (useSourceCombatStats && sourceNormalBlueprint != null)
            {
                return sourceNormalBlueprint.movespeed;
            }

            return movespeed;
        }

        public float GetEffectiveAcceleration()
        {
            if (useSourceCombatStats && sourceNormalBlueprint != null)
            {
                return sourceNormalBlueprint.acceleration;
            }

            return acceleration;
        }

        public float GetEffectiveAttack()
        {
            if (useSourceCombatStats && sourceNormalBlueprint != null)
            {
                return sourceNormalBlueprint.atk;
            }

            return atk;
        }

        public float GetEffectiveAttackSpeed()
        {
            if (useSourceCombatStats && sourceNormalBlueprint != null)
            {
                return sourceNormalBlueprint.atkspeed;
            }

            return atkspeed;
        }

        public LayerMask GetEffectiveMeleeLayer()
        {
            if (useSourceCombatStats && sourceNormalBlueprint != null)
            {
                return sourceNormalBlueprint.meleeLayer;
            }

            return meleeLayer;
        }
    }
}