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

        // 침귀환 상태
        private bool isReturningToPlayer = false;
        private float returnTimer = 0f;

        [Header("Stuck Needle Visual / 꽂힌 침 연출")]
        [Tooltip("체력이 남은 몬스터에게 일반 침이 적중했을 때, 맞은 위치에 침 시각 오브젝트를 남깁니다.")]
        [SerializeField] private bool enableStuckNeedleVisual = true;

        [Tooltip("몬스터 몸에 꽂힌 침이 유지되는 시간입니다.")]
        [SerializeField] private float stuckNeedleLifetime = 2.5f;

        [Tooltip("꽂힌 침의 크기 배율입니다.")]
        [SerializeField] private float stuckNeedleScaleMultiplier = 0.85f;

        [Tooltip("꽂힌 침의 투명도입니다.")]
        [SerializeField, Range(0f, 1f)] private float stuckNeedleAlpha = 0.9f;

        [Tooltip("꽂힌 침이 원래 침보다 위에 보이도록 Sorting Order에 더할 값입니다.")]
        [SerializeField] private int stuckNeedleSortingOrderBonus = 1;

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

            isReturningToPlayer = false;
            returnTimer = 0f;

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
            float effectiveMaxDistance = maxDistance + specials.rangeBonus;

            while (speed > 0 && !isDespawning)
            {
                if (isReturningToPlayer)
                {
                    if (playerCharacter == null || playerCharacter.CenterTransform == null)
                    {
                        HitNothing();
                        yield break;
                    }

                    returnTimer += Time.deltaTime;

                    if (returnTimer >= Mathf.Max(0.1f, specials.returnNeedleMaxDuration))
                    {
                        HitNothing();
                        yield break;
                    }

                    Vector2 currentPosition = transform.position;
                    Vector2 targetPosition = playerCharacter.CenterTransform.position;
                    Vector2 toPlayer = targetPosition - currentPosition;

                    if (toPlayer.magnitude <= Mathf.Max(0.05f, specials.returnNeedleArriveDistance))
                    {
                        DestroyProjectile();
                        yield break;
                    }

                    direction = toPlayer.normalized;

                    float returnStep = speed * Mathf.Max(0.01f, specials.returnNeedleSpeedMultiplier) * Time.deltaTime;
                    transform.position += returnStep * (Vector3)direction;

                    transform.RotateAround(transform.position, Vector3.back, Time.deltaTime * 100 * rotationSpeed);

                    yield return null;
                    continue;
                }

                if (distanceTravelled >= effectiveMaxDistance)
                {
                    HitNothing();
                    yield break;
                }

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
                if (!isReturningToPlayer)
                {
                    HitNothing();
                }

                return;
            }

            int targetId = damageableComponent.gameObject.GetInstanceID();

            if (hitTargetIds.Contains(targetId))
            {
                return;
            }

            hitTargetIds.Add(targetId);

            float finalDamage = isReturningToPlayer
                ? damage * Mathf.Max(0.01f, specials.returnNeedleDamageMultiplier)
                : damage;

            Vector3 hitPosition = transform.position;
            Quaternion hitRotation = transform.rotation;
            Vector3 hitScale = transform.lossyScale;

            DamageTarget(damageable, damageableComponent, finalDamage);

            // 일반 상태의 침이 살아있는 몬스터에게 박혔을 때만 시각적으로 침을 남긴다.
            TryCreateStuckNeedleVisual(
                damageableComponent,
                hitPosition,
                hitRotation,
                hitScale
            );

            // 귀환 중에는 적을 맞혀도 멈추지 않고 계속 플레이어에게 돌아간다.
            if (isReturningToPlayer)
            {
                return;
            }

            bool canPierce = specials.pierceEnabled && remainingPierces > 0;

            // 핵심 수정:
            // 관통침 + 침귀환을 같이 가지고 있을 때,
            // 침귀환보다 관통 횟수 소모를 먼저 처리한다.
            //
            // 예: pierceCount = 2
            // 1번째 적중 → 관통, remainingPierces 1
            // 2번째 적중 → 관통, remainingPierces 0
            // 3번째 적중 → 더 이상 관통 불가 → 침귀환 발동
            if (canPierce)
            {
                remainingPierces--;

                if (col != null)
                {
                    StartCoroutine(ReenableColliderNextFrame());
                }

                return;
            }

            // 관통 횟수를 모두 사용했거나, 관통침이 없을 때 침귀환이 켜져 있으면 귀환한다.
            if (specials.returnNeedleEnabled)
            {
                BeginReturnToPlayer();
                return;
            }

            DestroyProjectile();
        }

        private void DamageTarget(IDamageable damageable, Component damageableComponent, float rawDamage)
        {
            PlayerGeneralStatRuntime statRuntime = PlayerGeneralStatRuntime.GetOrCreate(playerCharacter);

            bool isCritical = false;
            float finalDamage = rawDamage;

            if (statRuntime != null)
            {
                finalDamage = statRuntime.CalculateOffensiveDamage(
                    playerCharacter,
                    damageableComponent,
                    rawDamage,
                    out isCritical
                );
            }

            float finalKnockback = knockback;

            if (statRuntime != null)
            {
                finalKnockback *= statRuntime.KnockbackMultiplier;
            }

            damageable.TakeDamage(finalDamage, finalKnockback * direction);
            OnHitDamageable?.Invoke(finalDamage);

            if (isCritical)
            {
                Debug.Log($"[치명타] 침 공격 치명타 발생 | 피해 {finalDamage:0.##}");
            }

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
        }
        private void TryCreateStuckNeedleVisual(
            Component damageableComponent,
            Vector3 hitPosition,
            Quaternion hitRotation,
            Vector3 hitScale)
        {
            if (!enableStuckNeedleVisual)
            {
                return;
            }

            // 귀환 중인 침은 플레이어에게 돌아가야 하므로 남기지 않는다.
            if (isReturningToPlayer)
            {
                return;
            }

            // 침귀환 증강이 있으면 침이 돌아가야 하므로 남기지 않는다.
            if (specials.returnNeedleEnabled)
            {
                return;
            }

            // 관통침이 있으면 침이 뚫고 지나가야 하므로 남기지 않는다.
            // 대물침도 내부적으로 pierceEnabled를 사용하므로 여기서 자연스럽게 제외된다.
            if (specials.pierceEnabled)
            {
                return;
            }

            if (damageableComponent == null)
            {
                return;
            }

            Monster monster = damageableComponent.GetComponent<Monster>() ??
                              damageableComponent.GetComponentInParent<Monster>();

            // 몬스터가 아니면 침 박힘 연출을 만들지 않는다.
            if (monster == null)
            {
                return;
            }

            // 데미지 적용 후에도 살아있는 몬스터에게만 침을 남긴다.
            if (monster.HP <= 0f)
            {
                return;
            }

            if (projectileSpriteRenderer == null || projectileSpriteRenderer.sprite == null)
            {
                return;
            }

            GameObject stuckNeedleObject = new GameObject("Stuck Needle Visual");
            Transform stuckTransform = stuckNeedleObject.transform;

            stuckTransform.position = hitPosition;
            stuckTransform.rotation = hitRotation;
            stuckTransform.localScale = hitScale * stuckNeedleScaleMultiplier;

            // 월드 위치를 유지한 채 몬스터에 붙인다.
            // 이렇게 하면 몬스터가 움직일 때 꽂힌 침도 같이 따라간다.
            stuckTransform.SetParent(monster.transform, true);

            SpriteRenderer renderer = stuckNeedleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = projectileSpriteRenderer.sprite;
            renderer.flipX = projectileSpriteRenderer.flipX;
            renderer.flipY = projectileSpriteRenderer.flipY;
            renderer.sortingLayerID = projectileSpriteRenderer.sortingLayerID;
            renderer.sortingOrder = projectileSpriteRenderer.sortingOrder + stuckNeedleSortingOrderBonus;

            Color baseColor = projectileSpriteRenderer.color;
            baseColor.a = stuckNeedleAlpha;
            renderer.color = baseColor;

            StuckNeedleVisual stuckVisual = stuckNeedleObject.AddComponent<StuckNeedleVisual>();
            stuckVisual.Init(monster, stuckNeedleLifetime);
        }

        private void BeginReturnToPlayer()
        {
            if (playerCharacter == null || playerCharacter.CenterTransform == null)
            {
                DestroyProjectile();
                return;
            }

            isReturningToPlayer = true;
            returnTimer = 0f;

            if (col != null)
            {
                StartCoroutine(ReenableColliderNextFrame());
            }
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

            PlayerGeneralStatRuntime statRuntime = PlayerGeneralStatRuntime.GetOrCreate(playerCharacter);

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

                float splashDamage = specials.explosionDamage;
                bool isCritical = false;

                if (statRuntime != null)
                {
                    splashDamage = statRuntime.CalculateOffensiveDamage(
                        playerCharacter,
                        splashComponent,
                        splashDamage,
                        out isCritical
                    );
                }

                splashDamageable.TakeDamage(splashDamage, Vector2.zero);

                if (isCritical)
                {
                    Debug.Log($"[치명타] 폭발침 치명타 발생 | 피해 {splashDamage:0.##}");
                }
            }
        }
    }

    // 몬스터 몸에 꽂힌 침 시각 오브젝트.
    // 일정 시간이 지나면 사라지고, 몬스터가 먼저 죽으면 같이 사라진다.
    public class StuckNeedleVisual : MonoBehaviour
    {
        private Monster ownerMonster;
        private float lifetime = 2.5f;
        private Coroutine lifetimeCoroutine;

        public void Init(Monster ownerMonster, float lifetime)
        {
            this.ownerMonster = ownerMonster;
            this.lifetime = Mathf.Max(0.05f, lifetime);

            if (this.ownerMonster != null)
            {
                this.ownerMonster.OnKilled.AddListener(OnOwnerKilled);
            }

            lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(lifetime);

            Destroy(gameObject);
        }

        private void OnOwnerKilled(Monster killedMonster)
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }

            if (ownerMonster != null)
            {
                ownerMonster.OnKilled.RemoveListener(OnOwnerKilled);
            }
        }
    }
}