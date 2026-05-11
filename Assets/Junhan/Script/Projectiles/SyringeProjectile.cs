using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Vampire
{
    public class SyringeProjectile : Projectile
    {
        private SyringeSpecialRuntime specials;
        private int remainingPierces;

        private readonly HashSet<int> hitTargetIds = new HashSet<int>();

        // 풀링으로 재사용될 때 maxDistance가 계속 누적되는 것을 막기 위한 원본 사거리 저장
        private float baseMaxDistance;

        // 플레이어 회복 메서드를 못 찾았을 때 경고가 너무 많이 뜨는 것을 막기 위한 플래그
        private static bool hasWarnedHealMethodMissing = false;

        protected override void Awake()
        {
            base.Awake();

            baseMaxDistance = maxDistance;
        }

        public override void Setup(int projectileIndex, Vector2 position, float damage, float knockback, float speed, LayerMask targetLayer)
        {
            base.Setup(projectileIndex, position, damage, knockback, speed, targetLayer);

            specials = default;
            remainingPierces = 0;
            hitTargetIds.Clear();

            // 투사체 풀링 시 이전 발사에서 적용된 사거리 배율이 남지 않도록 초기화
            maxDistance = baseMaxDistance;
        }

        public void ConfigureSpecials(SyringeSpecialRuntime runtime)
        {
            specials = runtime;

            if (runtime.pierceEnabled)
            {
                remainingPierces = Mathf.Max(0, runtime.pierceCount);
            }
            else
            {
                remainingPierces = 0;
            }
        }

        public override IEnumerator Move()
        {
            float distanceTravelled = 0;
            float timeOffScreen = 0;
            float effectiveMaxDistance = maxDistance + specials.rangeBonus;

            while (distanceTravelled < effectiveMaxDistance && timeOffScreen < despawnTime && speed > 0)
            {
                if (specials.homingEnabled)
                {
                    UpdateHomingDirection();
                }

                float step = speed * Time.deltaTime;

                transform.position += step * (Vector3)direction;
                distanceTravelled += step;

                transform.RotateAround(transform.position, Vector3.back, Time.deltaTime * 100 * rotationSpeed);

                speed -= airResistance * Time.deltaTime;

                yield return null;
            }

            HitNothing();
        }

        private void UpdateHomingDirection()
        {
            Transform target = FindClosestTarget();

            if (target == null)
            {
                return;
            }

            Vector2 desiredDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;

            direction = Vector2.Lerp(
                direction,
                desiredDirection,
                specials.homingLerpSpeed * Time.deltaTime
            ).normalized;
        }

        private Transform FindClosestTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, specials.homingRange, targetLayer);

            float closestDistance = float.MaxValue;
            Transform closestTarget = null;

            foreach (Collider2D hit in hits)
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                Component damageableComponent = damageable as Component;

                if (damageableComponent == null)
                {
                    continue;
                }

                int targetId = damageableComponent.gameObject.GetInstanceID();

                if (hitTargetIds.Contains(targetId))
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, damageableComponent.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = damageableComponent.transform;
                }
            }

            return closestTarget;
        }

        protected override void OnTriggerEnter2D(Collider2D collider)
        {
            if (isDespawning || !gameObject.activeInHierarchy)
            {
                return;
            }

            if ((targetLayer & (1 << collider.gameObject.layer)) == 0)
            {
                return;
            }

            IDamageable damageable = collider.GetComponentInParent<IDamageable>();
            Component damageableComponent = damageable as Component;

            if (damageable == null || damageableComponent == null)
            {
                HitNothing();
                return;
            }

            int targetId = damageableComponent.gameObject.GetInstanceID();

            if (hitTargetIds.Contains(targetId))
            {
                return;
            }

            hitTargetIds.Add(targetId);

            damageable.TakeDamage(damage, knockback * direction);
            OnHitDamageable?.Invoke(damage);

            if (specials.poisonEnabled)
            {
                ApplyPoison(damageableComponent);
            }

            if (specials.honeyEnabled)
            {
                ApplyHoneySlow(damageableComponent);
            }

            if (specials.mosquitoEnabled)
            {
                ApplyMosquitoHeal(damageableComponent);
            }

            if (specials.explosionEnabled)
            {
                ApplyExplosion(damageableComponent.gameObject);
            }

            bool canPierce = specials.pierceEnabled && remainingPierces > 0;

            if (canPierce)
            {
                remainingPierces--;

                if (col != null)
                {
                    StartCoroutine(ReenableColliderNextFrame());
                }

                return;
            }

            DestroyProjectile();
        }

        private IEnumerator ReenableColliderNextFrame()
        {
            if (col == null)
            {
                yield break;
            }

            col.enabled = false;

            yield return null;

            if (!isDespawning && gameObject.activeInHierarchy)
            {
                col.enabled = true;
            }
        }

        private void ApplyPoison(Component damageableComponent)
        {
            Monster monster = damageableComponent.GetComponent<Monster>() ??
                              damageableComponent.GetComponentInParent<Monster>();

            if (monster == null)
            {
                return;
            }

            PoisonStatus poisonStatus = monster.GetComponent<PoisonStatus>();

            if (poisonStatus == null)
            {
                poisonStatus = monster.gameObject.AddComponent<PoisonStatus>();
            }

            poisonStatus.Apply(
                specials.poisonDuration,
                specials.poisonTickInterval,
                specials.poisonTickDamage
            );
        }

        private void ApplyHoneySlow(Component damageableComponent)
        {
            Monster monster = damageableComponent.GetComponent<Monster>() ??
                              damageableComponent.GetComponentInParent<Monster>();

            if (monster == null)
            {
                return;
            }

            HoneySlowStatus honeySlowStatus = monster.GetComponent<HoneySlowStatus>();

            if (honeySlowStatus == null)
            {
                honeySlowStatus = monster.gameObject.AddComponent<HoneySlowStatus>();
            }

            honeySlowStatus.Apply(
                specials.honeyDuration,
                specials.honeySlowMultiplier
            );
        }

        private void ApplyMosquitoHeal(Component damageableComponent)
        {
            // HP 1 전설 증강이 켜져 있으면 모기침 회복은 무조건 막는다.
            if (specials.healingBlocked)
            {
                return;
            }

            if (playerCharacter == null)
            {
                return;
            }

            float healAmount = specials.mosquitoHealPerHit;

            if (IsBossLikeTarget(damageableComponent))
            {
                healAmount *= specials.mosquitoBossHealMultiplier;
            }

            TryHealPlayer(healAmount);
        }

        private bool IsBossLikeTarget(Component damageableComponent)
        {
            if (damageableComponent == null)
            {
                return false;
            }

            Component[] components = damageableComponent.GetComponentsInParent<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;

                if (typeName.Contains("Boss"))
                {
                    return true;
                }
            }

            string objectName = damageableComponent.gameObject.name;

            return objectName.Contains("Boss") || objectName.Contains("보스");
        }

        private void TryHealPlayer(float healAmount)
        {
            if (playerCharacter == null || healAmount <= 0f)
            {
                return;
            }

            string[] healMethodNames =
            {
                "GainHealth",
                "Heal",
                "AddHealth",
                "RestoreHealth"
            };

            MethodInfo healMethod = null;

            foreach (string methodName in healMethodNames)
            {
                healMethod = playerCharacter.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new System.Type[] { typeof(float) },
                    null
                );

                if (healMethod != null)
                {
                    break;
                }
            }

            if (healMethod == null)
            {
                if (!hasWarnedHealMethodMissing)
                {
                    Debug.LogWarning(
                        "[모기침] Character에서 체력 회복 메서드를 찾지 못했습니다. " +
                        "GainHealth(float), Heal(float), AddHealth(float), RestoreHealth(float) 중 하나가 필요합니다."
                    );

                    hasWarnedHealMethodMissing = true;
                }

                return;
            }

            healMethod.Invoke(playerCharacter, new object[] { healAmount });
        }

        private void ApplyExplosion(GameObject originalTarget)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, specials.explosionRadius, targetLayer);

            HashSet<int> damagedIds = new HashSet<int>();

            foreach (Collider2D hit in hits)
            {
                IDamageable splashDamageable = hit.GetComponentInParent<IDamageable>();
                Component splashComponent = splashDamageable as Component;

                if (splashDamageable == null || splashComponent == null)
                {
                    continue;
                }

                int splashId = splashComponent.gameObject.GetInstanceID();

                if (originalTarget != null && splashId == originalTarget.GetInstanceID())
                {
                    continue;
                }

                if (damagedIds.Contains(splashId))
                {
                    continue;
                }

                damagedIds.Add(splashId);

                splashDamageable.TakeDamage(specials.explosionDamage, Vector2.zero);
            }
        }
    }
}