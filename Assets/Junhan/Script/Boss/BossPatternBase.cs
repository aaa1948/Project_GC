using System.Collections;
using UnityEngine;

namespace Vampire
{
    public abstract class BossPatternBase : MonoBehaviour
    {
        [Header("Pattern Info")]
        [Tooltip("패턴 이름입니다. BossController Debug Pattern을 켰을 때 Console에 표시됩니다.")]
        [SerializeField] protected string patternName;

        [Tooltip("이 패턴 자체의 기본 쿨타임입니다. 실제 쿨타임은 보스 현재 페이즈의 Pattern Cooldown Multiplier가 곱해집니다.")]
        [SerializeField] protected float cooldown = 3f;

        [Header("Distance Weights - Phase 1")]
        [Tooltip("1페이즈에서 플레이어가 근거리일 때 이 패턴이 선택될 가중치입니다. 0이면 근거리에서 선택되지 않습니다.")]
        [SerializeField] protected int nearWeightPhase1 = 10;

        [Tooltip("1페이즈에서 플레이어가 중거리일 때 이 패턴이 선택될 가중치입니다. 0이면 중거리에서 선택되지 않습니다.")]
        [SerializeField] protected int midWeightPhase1 = 10;

        [Tooltip("1페이즈에서 플레이어가 원거리일 때 이 패턴이 선택될 가중치입니다. 0이면 원거리에서 선택되지 않습니다.")]
        [SerializeField] protected int farWeightPhase1 = 10;

        [Header("Distance Weights - Phase 2")]
        [Tooltip("2페이즈에서 플레이어가 근거리일 때 이 패턴이 선택될 가중치입니다.")]
        [SerializeField] protected int nearWeightPhase2 = 10;

        [Tooltip("2페이즈에서 플레이어가 중거리일 때 이 패턴이 선택될 가중치입니다.")]
        [SerializeField] protected int midWeightPhase2 = 10;

        [Tooltip("2페이즈에서 플레이어가 원거리일 때 이 패턴이 선택될 가중치입니다.")]
        [SerializeField] protected int farWeightPhase2 = 10;

        [Header("Distance Weights - Phase 3")]
        [Tooltip("3페이즈에서 플레이어가 근거리일 때 이 패턴이 선택될 가중치입니다. 단, BossController의 3페이즈 Ignore Distance가 켜져 있으면 이 값은 무시됩니다.")]
        [SerializeField] protected int nearWeightPhase3 = 10;

        [Tooltip("3페이즈에서 플레이어가 중거리일 때 이 패턴이 선택될 가중치입니다. 단, BossController의 3페이즈 Ignore Distance가 켜져 있으면 이 값은 무시됩니다.")]
        [SerializeField] protected int midWeightPhase3 = 10;

        [Tooltip("3페이즈에서 플레이어가 원거리일 때 이 패턴이 선택될 가중치입니다. 단, BossController의 3페이즈 Ignore Distance가 켜져 있으면 이 값은 무시됩니다.")]
        [SerializeField] protected int farWeightPhase3 = 10;

        protected BossController bossController;
        protected float lastUseTime = -999f;

        public string PatternName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(patternName))
                {
                    return GetType().Name;
                }

                return patternName;
            }
        }

        public virtual void Init(BossController controller)
        {
            bossController = controller;
        }

        public virtual bool CanUse()
        {
            float finalCooldown = cooldown;

            if (bossController != null)
            {
                finalCooldown = bossController.GetModifiedPatternCooldown(cooldown);
            }

            return Time.time >= lastUseTime + finalCooldown;
        }

        public int GetWeight(float distanceToPlayer, int phase)
        {
            if (bossController == null)
            {
                return 0;
            }

            if (phase <= 1)
            {
                if (distanceToPlayer <= bossController.NearDistanceThreshold)
                {
                    return nearWeightPhase1;
                }

                if (distanceToPlayer <= bossController.MidDistanceThreshold)
                {
                    return midWeightPhase1;
                }

                return farWeightPhase1;
            }

            if (phase == 2)
            {
                if (distanceToPlayer <= bossController.NearDistanceThreshold)
                {
                    return nearWeightPhase2;
                }

                if (distanceToPlayer <= bossController.MidDistanceThreshold)
                {
                    return midWeightPhase2;
                }

                return farWeightPhase2;
            }

            if (distanceToPlayer <= bossController.NearDistanceThreshold)
            {
                return nearWeightPhase3;
            }

            if (distanceToPlayer <= bossController.MidDistanceThreshold)
            {
                return midWeightPhase3;
            }

            return farWeightPhase3;
        }

        public IEnumerator Execute()
        {
            lastUseTime = Time.time;
            yield return ExecutePattern();
        }

        protected abstract IEnumerator ExecutePattern();
    }
}