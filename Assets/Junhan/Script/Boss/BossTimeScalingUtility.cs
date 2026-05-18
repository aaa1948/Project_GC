using System;
using System.Reflection;
using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// 보스 소환 시점에 따라 보스 HP / 기본 공격 / 접촉 피해 / 패턴 피해를 스케일링하는 유틸리티.
    /// 
    /// 이 유틸리티는 기존 BossController와 BossPatternBase 계열 코드를 크게 뜯지 않기 위해
    /// Reflection으로 private SerializeField 값을 런타임에 조정한다.
    /// </summary>
    public static class BossTimeScalingUtility
    {
        private static readonly string[] BossControllerDamageFields =
        {
            "contactDamage",
            "basicAttackDamage"
        };

        private static readonly string[] BossPatternDamageFields =
        {
            "chargeDamagePhase1",
            "chargeDamagePhase2",
            "explosionDamage",
            "bulletDamage"
        };

        public static float EvaluateMultiplier(AnimationCurve curve, float minute, float fallbackValue = 1f)
        {
            if (curve == null || curve.length == 0)
            {
                return fallbackValue;
            }

            return Mathf.Max(0.01f, curve.Evaluate(minute));
        }

        public static float CalculateMonsterHpBuff(MonsterBlueprint bossBlueprint, float hpMultiplier)
        {
            if (bossBlueprint == null)
            {
                return 0f;
            }

            float multiplier = Mathf.Max(0.01f, hpMultiplier);

            return bossBlueprint.hp * (multiplier - 1f);
        }

        public static void ApplyToSpawnedBoss(
            GameObject bossObject,
            float hpMultiplier,
            float damageMultiplier,
            bool debugLog = true)
        {
            if (bossObject == null)
            {
                return;
            }

            hpMultiplier = Mathf.Max(0.01f, hpMultiplier);
            damageMultiplier = Mathf.Max(0.01f, damageMultiplier);

            BossController bossController = bossObject.GetComponent<BossController>();

            if (bossController == null)
            {
                bossController = bossObject.GetComponentInChildren<BossController>(true);
            }

            if (bossController != null)
            {
                ApplyBossControllerScaling(bossController, hpMultiplier, damageMultiplier, debugLog);
            }

            ApplyPatternDamageScaling(bossObject, damageMultiplier, debugLog);

            if (debugLog)
            {
                Debug.Log(
                    $"[BossTimeScalingUtility] 보스 시간대 스케일 적용 완료 | " +
                    $"Boss={bossObject.name} | HP x{hpMultiplier:0.##} | Damage x{damageMultiplier:0.##}"
                );
            }
        }

        private static void ApplyBossControllerScaling(
            BossController bossController,
            float hpMultiplier,
            float damageMultiplier,
            bool debugLog)
        {
            if (bossController == null)
            {
                return;
            }

            if (MultiplyFloatField(bossController, "maxHp", hpMultiplier, out float oldMaxHp, out float newMaxHp))
            {
                SetFloatField(bossController, "currentHp", newMaxHp);

                if (debugLog)
                {
                    Debug.Log(
                        $"[BossTimeScalingUtility] BossController HP 스케일 | " +
                        $"{oldMaxHp:0.##} → {newMaxHp:0.##}"
                    );
                }
            }

            for (int i = 0; i < BossControllerDamageFields.Length; i++)
            {
                string fieldName = BossControllerDamageFields[i];

                if (MultiplyFloatField(bossController, fieldName, damageMultiplier, out float oldValue, out float newValue))
                {
                    if (debugLog)
                    {
                        Debug.Log(
                            $"[BossTimeScalingUtility] BossController Damage 스케일 | " +
                            $"{fieldName}: {oldValue:0.##} → {newValue:0.##}"
                        );
                    }
                }
            }
        }

        private static void ApplyPatternDamageScaling(
            GameObject bossObject,
            float damageMultiplier,
            bool debugLog)
        {
            BossPatternBase[] patterns = bossObject.GetComponentsInChildren<BossPatternBase>(true);

            for (int i = 0; i < patterns.Length; i++)
            {
                BossPatternBase pattern = patterns[i];

                if (pattern == null)
                {
                    continue;
                }

                for (int fieldIndex = 0; fieldIndex < BossPatternDamageFields.Length; fieldIndex++)
                {
                    string fieldName = BossPatternDamageFields[fieldIndex];

                    if (MultiplyFloatField(pattern, fieldName, damageMultiplier, out float oldValue, out float newValue))
                    {
                        if (debugLog)
                        {
                            Debug.Log(
                                $"[BossTimeScalingUtility] Pattern Damage 스케일 | " +
                                $"{pattern.GetType().Name}.{fieldName}: {oldValue:0.##} → {newValue:0.##}"
                            );
                        }
                    }
                }
            }
        }

        private static bool MultiplyFloatField(
            object target,
            string fieldName,
            float multiplier,
            out float oldValue,
            out float newValue)
        {
            oldValue = 0f;
            newValue = 0f;

            if (target == null)
            {
                return false;
            }

            FieldInfo field = FindField(target.GetType(), fieldName);

            if (field == null || field.FieldType != typeof(float))
            {
                return false;
            }

            oldValue = (float)field.GetValue(target);
            newValue = oldValue * multiplier;

            field.SetValue(target, newValue);

            return true;
        }

        private static bool SetFloatField(object target, string fieldName, float value)
        {
            if (target == null)
            {
                return false;
            }

            FieldInfo field = FindField(target.GetType(), fieldName);

            if (field == null || field.FieldType != typeof(float))
            {
                return false;
            }

            field.SetValue(target, value);

            return true;
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            Type currentType = type;

            while (currentType != null)
            {
                FieldInfo field = currentType.GetField(fieldName, flags);

                if (field != null)
                {
                    return field;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }
    }
}