using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Vampire
{
    public class SyringeProjectile : Projectile
    {
        private enum NeedleFlightState
        {
            Normal,
            ReturnForwardPass,
            ReturnArcAroundTarget,
            ReturnToPlayer
        }

        private SyringeSpecialRuntime specials;
        private int remainingPierces;

        private readonly HashSet<int> hitTargetIds = new HashSet<int>();

        // 풀링으로 재사용될 때 maxDistance가 계속 누적되는 것을 막기 위한 원본 사거리 저장
        private float baseMaxDistance;

        // 침귀환 상태
        private NeedleFlightState flightState = NeedleFlightState.Normal;
        private float returnTimer = 0f;

        private Vector2 returnForwardDirection;
        private float returnForwardTravelled = 0f;

        private Vector2 returnArcCenter;
        private float returnArcRadius = 1.1f;
        private float returnArcStartAngle;
        private float returnArcEndAngle;
        private float returnArcTimer = 0f;

        [Header("Needle Flight / 침 비행")]
        [Tooltip("침 이미지의 날 부분이 이동 방향을 향하도록 보정하는 각도입니다. 현재 이미지 기준으로 135가 기본입니다. 반대로 날아가면 값을 조절하세요.")]
        [SerializeField] private float visualForwardAngleOffset = 135f;

        [Tooltip("침귀환 발동 시, 적을 맞힌 뒤 앞으로 한 번 뚫고 지나가는 거리입니다.")]
        [SerializeField] private float returnNeedleForwardPassDistance = 0.65f;

        [Tooltip("침귀환이 적 중심을 기준으로 반원을 그릴 때 사용할 최소 반지름입니다. 몬스터 한 마리 정도 여유 공간을 의미합니다.")]
        [SerializeField] private float returnNeedleArcRadius = 1.1f;

        [Tooltip("침귀환이 반원을 도는 데 걸리는 시간입니다.")]
        [SerializeField] private float returnNeedleArcDuration = 0.28f;

        [Tooltip("침귀환이 최소한 이 정도 각도는 돌아가도록 보정합니다.")]
        [SerializeField] private float returnNeedleMinArcAngle = 130f;

        [Header("Stuck Needle Visual / 꽂힌 침 연출")]
        [Tooltip("체력이 남은 몬스터에게 일반 침이 적중했을 때, 맞은 위치에 침 시각 오브젝트를 남깁니다.")]
        [SerializeField] private bool enableStuckNeedleVisual = true;

        [Tooltip("몬스터 몸에 꽂힌 침이 유지되는 시간입니다.")]
        [SerializeField] private float stuckNeedleLifetime = 2.5f;

        [Tooltip("꽂힌 침의 크기 배율입니다. Visual_Needle의 실제 크기에 곱해집니다.")]
        [SerializeField] private float stuckNeedleScaleMultiplier = 0.85f;

        [Tooltip("꽂힌 침의 투명도입니다.")]
        [SerializeField, Range(0f, 1f)] private float stuckNeedleAlpha = 0.9f;

        [Tooltip("꽂힌 침이 대상 몬스터보다 위에 보이도록 Sorting Order에 더할 값입니다.")]
        [SerializeField] private int stuckNeedleSortingOrderBonus = 5;

        // 플레이어 회복 메서드를 못 찾았을 때 경고가 너무 많이 뜨는 것을 막기 위한 플래그
        private static bool hasWarnedHealMethodMissing = false;

        private bool IsReturnMode =>
            flightState == NeedleFlightState.ReturnForwardPass ||
            flightState == NeedleFlightState.ReturnArcAroundTarget ||
            flightState == NeedleFlightState.ReturnToPlayer;

        protected override void Awake()
        {
            base.Awake();

            baseMaxDistance = maxDistance;
        }

        public override void Setup(
            int projectileIndex,
            Vector2 position,
            float damage,
            float knockback,
            float speed,
            LayerMask targetLayer)
        {
            base.Setup(projectileIndex, position, damage, knockback, speed, targetLayer);

            specials = default;
            remainingPierces = 0;
            hitTargetIds.Clear();

            flightState = NeedleFlightState.Normal;
            returnTimer = 0f;
            returnForwardTravelled = 0f;
            returnArcTimer = 0f;

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

        public override void Launch(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                direction = Vector2.right;
            }

            this.direction = direction.normalized;
            ApplyVisualRotationToDirection(this.direction);

            moveCoroutine = StartCoroutine(Move());
        }

        public override IEnumerator Move()
        {
            float distanceTravelled = 0f;
            float effectiveMaxDistance = maxDistance + specials.rangeBonus;

            while (speed > 0f && !isDespawning)
            {
                switch (flightState)
                {
                    case NeedleFlightState.Normal:
                        if (distanceTravelled >= effectiveMaxDistance)
                        {
                            HitNothing();
                            yield break;
                        }

                        if (specials.homingEnabled)
                        {
                            UpdateHomingDirection();
                        }

                        float normalStep = speed * Time.deltaTime;
                        transform.position += normalStep * (Vector3)direction;
                        distanceTravelled += normalStep;

                        ApplyVisualRotationToDirection(direction);

                        speed -= airResistance * Time.deltaTime;
                        break;

                    case NeedleFlightState.ReturnForwardPass:
                        MoveReturnForwardPass();
                        break;

                    case NeedleFlightState.ReturnArcAroundTarget:
                        MoveReturnArcAroundTarget();
                        break;

                    case NeedleFlightState.ReturnToPlayer:
                        if (!MoveReturnToPlayer())
                        {
                            yield break;
                        }
                        break;
                }

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

            Vector2 desiredDirection =
                ((Vector2)target.position - (Vector2)transform.position).normalized;

            direction = Vector2.Lerp(
                direction,
                desiredDirection,
                specials.homingLerpSpeed * Time.deltaTime
            ).normalized;
        }

        private void ApplyVisualRotationToDirection(Vector2 moveDirection)
        {
            if (moveDirection == Vector2.zero)
            {
                return;
            }

            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

            // visualForwardAngleOffset:
            // 현재 침 이미지가 기본 상태에서 날 부분이 좌하단을 향하고 있으므로,
            // 이동 방향과 날 방향을 맞추기 위한 보정값.
            transform.rotation = Quaternion.Euler(0f, 0f, angle + visualForwardAngleOffset);
        }

        private bool TryGetValidMonsterTarget(Collider2D collider, out Monster monster)
        {
            monster = null;

            if (collider == null)
            {
                return false;
            }

            monster = collider.GetComponentInParent<Monster>();

            if (monster == null)
            {
                return false;
            }

            // 함정 몬스터는 활성화 상태일 때만 공격 대상이 된다.
            // 휴면 상태에서는 유도 대상도 아니고, 적중 대상도 아니다.
            TrapMonster trapMonster = monster as TrapMonster;

            if (trapMonster != null && !trapMonster.IsActive)
            {
                return false;
            }

            return true;
        }

        private bool IsSameTarget(GameObject originalTarget, Monster monster)
        {
            if (originalTarget == null || monster == null)
            {
                return false;
            }

            if (originalTarget == monster.gameObject)
            {
                return true;
            }

            Monster originalMonster = originalTarget.GetComponentInParent<Monster>();

            return originalMonster != null && originalMonster == monster;
        }

        private Transform FindClosestTarget()
        {
            // 보스가 일반 Monster Layer가 아닐 수 있으므로,
            // LayerMask로 먼저 거르지 않고 주변 Collider 전체를 찾은 뒤 Monster 컴포넌트 기준으로 필터링한다.
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, specials.homingRange);

            float closestDistance = float.MaxValue;
            Transform closestTarget = null;

            foreach (Collider2D hit in hits)
            {
                Monster monster;

                if (!TryGetValidMonsterTarget(hit, out monster))
                {
                    continue;
                }

                IDamageable damageable = monster as IDamageable;

                if (damageable == null)
                {
                    continue;
                }

                int targetId = monster.gameObject.GetInstanceID();

                if (hitTargetIds.Contains(targetId))
                {
                    continue;
                }

                Vector2 targetPosition = monster.CenterTransform != null
                    ? (Vector2)monster.CenterTransform.position
                    : (Vector2)monster.transform.position;

                float distance = Vector2.Distance(transform.position, targetPosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = monster.CenterTransform != null
                        ? monster.CenterTransform
                        : monster.transform;
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

            bool isInTargetLayer = (targetLayer & (1 << collider.gameObject.layer)) != 0;

            Monster monsterTarget;
            bool isValidMonsterTarget = TryGetValidMonsterTarget(collider, out monsterTarget);

            // 휴면 상태 TrapMonster처럼 Monster이지만 유효하지 않은 대상은 그냥 통과시킨다.
            if (!isValidMonsterTarget)
            {
                if (monsterTarget != null)
                {
                    return;
                }

                if (!isInTargetLayer)
                {
                    return;
                }
            }

            IDamageable damageable = null;
            Component damageableComponent = null;
            int targetId;

            if (isValidMonsterTarget && monsterTarget != null)
            {
                damageable = monsterTarget as IDamageable;
                damageableComponent = monsterTarget;
                targetId = monsterTarget.gameObject.GetInstanceID();
            }
            else
            {
                damageable = collider.GetComponentInParent<IDamageable>();
                damageableComponent = damageable as Component;

                if (damageableComponent == null)
                {
                    if (!IsReturnMode)
                    {
                        HitNothing();
                    }

                    return;
                }

                targetId = damageableComponent.gameObject.GetInstanceID();
            }

            if (damageable == null || damageableComponent == null)
            {
                if (!IsReturnMode)
                {
                    HitNothing();
                }

                return;
            }

            if (hitTargetIds.Contains(targetId))
            {
                return;
            }

            hitTargetIds.Add(targetId);

            float rawDamage = IsReturnMode
                ? damage * Mathf.Max(0.01f, specials.returnNeedleDamageMultiplier)
                : damage;

            // 실제 보이는 Visual_Needle SpriteRenderer의 월드 위치/회전/스케일 기준.
            Vector3 hitPosition = GetProjectileVisualWorldPosition();
            Quaternion hitRotation = GetProjectileVisualWorldRotation();
            Vector3 hitScale = GetProjectileVisualWorldScale();

            DamageTarget(damageable, damageableComponent, rawDamage);

            TryCreateStuckNeedleVisual(
                damageableComponent,
                hitPosition,
                hitRotation,
                hitScale
            );

            // 귀환 중에는 적을 맞혀도 멈추지 않고 계속 플레이어에게 돌아간다.
            if (IsReturnMode)
            {
                return;
            }

            bool canPierce = specials.pierceEnabled && remainingPierces > 0;

            // 관통침 + 침귀환 조합:
            // 관통 횟수를 먼저 소모하고, 관통이 끝난 다음 귀환한다.
            if (canPierce)
            {
                remainingPierces--;

                if (col != null)
                {
                    StartCoroutine(ReenableColliderNextFrame());
                }

                return;
            }

            if (specials.returnNeedleEnabled)
            {
                BeginReturnNeedleSequence(monsterTarget, direction);
                return;
            }

            DestroyProjectile();
        }

        private Vector3 GetProjectileVisualWorldPosition()
        {
            if (projectileSpriteRenderer != null)
            {
                return projectileSpriteRenderer.transform.position;
            }

            return transform.position;
        }

        private Quaternion GetProjectileVisualWorldRotation()
        {
            if (projectileSpriteRenderer != null)
            {
                return projectileSpriteRenderer.transform.rotation;
            }

            return transform.rotation;
        }

        private Vector3 GetProjectileVisualWorldScale()
        {
            if (projectileSpriteRenderer != null)
            {
                return projectileSpriteRenderer.transform.lossyScale;
            }

            return transform.lossyScale;
        }

        private void BeginReturnNeedleSequence(Monster hitMonster, Vector2 currentMoveDirection)
        {
            if (playerCharacter == null || playerCharacter.CenterTransform == null)
            {
                DestroyProjectile();
                return;
            }

            if (currentMoveDirection == Vector2.zero)
            {
                currentMoveDirection = direction;

                if (currentMoveDirection == Vector2.zero)
                {
                    currentMoveDirection = Vector2.right;
                }
            }

            returnForwardDirection = currentMoveDirection.normalized;
            returnForwardTravelled = 0f;
            returnTimer = 0f;

            if (hitMonster != null)
            {
                returnArcCenter = hitMonster.CenterTransform != null
                    ? (Vector2)hitMonster.CenterTransform.position
                    : (Vector2)hitMonster.transform.position;
            }
            else
            {
                returnArcCenter = transform.position;
            }

            flightState = NeedleFlightState.ReturnForwardPass;

            if (col != null)
            {
                StartCoroutine(ReenableColliderNextFrame());
            }
        }

        private void MoveReturnForwardPass()
        {
            returnTimer += Time.deltaTime;

            if (returnTimer >= Mathf.Max(0.1f, specials.returnNeedleMaxDuration))
            {
                HitNothing();
                return;
            }

            float step = speed * Time.deltaTime;
            transform.position += step * (Vector3)returnForwardDirection;
            returnForwardTravelled += step;

            direction = returnForwardDirection;
            ApplyVisualRotationToDirection(direction);

            if (returnForwardTravelled >= Mathf.Max(0f, returnNeedleForwardPassDistance))
            {
                BeginReturnArcAroundTarget();
            }
        }

        private void BeginReturnArcAroundTarget()
        {
            Vector2 currentPosition = transform.position;
            Vector2 fromCenter = currentPosition - returnArcCenter;

            if (fromCenter.sqrMagnitude <= 0.0001f)
            {
                fromCenter = returnForwardDirection;

                if (fromCenter == Vector2.zero)
                {
                    fromCenter = Vector2.right;
                }
            }

            float currentDistance = fromCenter.magnitude;
            returnArcRadius = Mathf.Max(returnNeedleArcRadius, currentDistance);

            returnArcStartAngle = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;

            Vector2 toPlayer =
                (Vector2)playerCharacter.CenterTransform.position - returnArcCenter;

            if (toPlayer.sqrMagnitude <= 0.0001f)
            {
                toPlayer = -returnForwardDirection;
            }

            float desiredEndAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            float signedDelta = Mathf.DeltaAngle(returnArcStartAngle, desiredEndAngle);

            if (Mathf.Abs(signedDelta) < Mathf.Max(10f, returnNeedleMinArcAngle))
            {
                float sign = Mathf.Sign(signedDelta);

                if (Mathf.Approximately(sign, 0f))
                {
                    Vector3 cross = Vector3.Cross(returnForwardDirection, toPlayer.normalized);
                    sign = cross.z >= 0f ? 1f : -1f;
                }

                signedDelta = sign * Mathf.Max(10f, returnNeedleMinArcAngle);
            }

            returnArcEndAngle = returnArcStartAngle + signedDelta;
            returnArcTimer = 0f;
            flightState = NeedleFlightState.ReturnArcAroundTarget;
        }

        private void MoveReturnArcAroundTarget()
        {
            returnTimer += Time.deltaTime;

            if (returnTimer >= Mathf.Max(0.1f, specials.returnNeedleMaxDuration))
            {
                HitNothing();
                return;
            }

            float duration = Mathf.Max(0.05f, returnNeedleArcDuration);
            returnArcTimer += Time.deltaTime;

            float t = Mathf.Clamp01(returnArcTimer / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            float angle = Mathf.Lerp(returnArcStartAngle, returnArcEndAngle, easedT);
            float rad = angle * Mathf.Deg2Rad;

            Vector2 previousPosition = transform.position;

            Vector2 nextPosition = returnArcCenter + new Vector2(
                Mathf.Cos(rad),
                Mathf.Sin(rad)
            ) * returnArcRadius;

            transform.position = nextPosition;

            Vector2 moveDirection = nextPosition - previousPosition;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                direction = moveDirection.normalized;
                ApplyVisualRotationToDirection(direction);
            }

            if (t >= 1f)
            {
                flightState = NeedleFlightState.ReturnToPlayer;

                if (col != null)
                {
                    StartCoroutine(ReenableColliderNextFrame());
                }
            }
        }

        private bool MoveReturnToPlayer()
        {
            if (playerCharacter == null || playerCharacter.CenterTransform == null)
            {
                HitNothing();
                return false;
            }

            returnTimer += Time.deltaTime;

            if (returnTimer >= Mathf.Max(0.1f, specials.returnNeedleMaxDuration))
            {
                HitNothing();
                return false;
            }

            Vector2 currentPosition = transform.position;
            Vector2 targetPosition = playerCharacter.CenterTransform.position;
            Vector2 toPlayer = targetPosition - currentPosition;

            if (toPlayer.magnitude <= Mathf.Max(0.05f, specials.returnNeedleArriveDistance))
            {
                DestroyProjectile();
                return false;
            }

            direction = toPlayer.normalized;

            float returnStep =
                speed *
                Mathf.Max(0.01f, specials.returnNeedleSpeedMultiplier) *
                Time.deltaTime;

            transform.position += returnStep * (Vector3)direction;
            ApplyVisualRotationToDirection(direction);

            return true;
        }

        private void DamageTarget(IDamageable damageable, Component damageableComponent, float rawDamage)
        {
            PlayerGeneralStatRuntime statRuntime =
                PlayerGeneralStatRuntime.GetOrCreate(playerCharacter);

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

            if (IsReturnMode)
            {
                return;
            }

            if (specials.returnNeedleEnabled)
            {
                return;
            }

            if (specials.pierceEnabled)
            {
                return;
            }

            if (damageableComponent == null)
            {
                return;
            }

            Monster monster =
                damageableComponent.GetComponent<Monster>() ??
                damageableComponent.GetComponentInParent<Monster>();

            if (monster == null)
            {
                return;
            }

            TrapMonster trapMonster = monster as TrapMonster;

            if (trapMonster != null && !trapMonster.IsActive)
            {
                return;
            }

            // 데미지를 준 뒤에도 살아있는 몬스터에게만 침을 남긴다.
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

            // 월드 위치/회전/스케일을 유지한 채 몬스터에 붙인다.
            stuckTransform.SetParent(monster.transform, true);

            SpriteRenderer renderer = stuckNeedleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = projectileSpriteRenderer.sprite;
            renderer.flipX = projectileSpriteRenderer.flipX;
            renderer.flipY = projectileSpriteRenderer.flipY;

            Color baseColor = projectileSpriteRenderer.color;
            baseColor.a = stuckNeedleAlpha;
            renderer.color = baseColor;

            SpriteRenderer targetRenderer = monster.GetComponentInChildren<SpriteRenderer>();

            if (targetRenderer != null)
            {
                renderer.sortingLayerID = targetRenderer.sortingLayerID;
                renderer.sortingOrder =
                    targetRenderer.sortingOrder + Mathf.Max(5, stuckNeedleSortingOrderBonus);
            }
            else
            {
                renderer.sortingLayerID = projectileSpriteRenderer.sortingLayerID;
                renderer.sortingOrder =
                    projectileSpriteRenderer.sortingOrder + Mathf.Max(5, stuckNeedleSortingOrderBonus);
            }

            StuckNeedleVisual stuckVisual = stuckNeedleObject.AddComponent<StuckNeedleVisual>();
            stuckVisual.Init(monster, stuckNeedleLifetime);
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
            Monster monster =
                damageableComponent.GetComponent<Monster>() ??
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
            Monster monster =
                damageableComponent.GetComponent<Monster>() ??
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
            // 보스가 targetLayer에 없을 수 있으므로 전체 Collider 검색 후 Monster 기준으로 필터링한다.
            Collider2D[] hits =
                Physics2D.OverlapCircleAll(transform.position, specials.explosionRadius);

            HashSet<int> damagedIds = new HashSet<int>();

            PlayerGeneralStatRuntime statRuntime =
                PlayerGeneralStatRuntime.GetOrCreate(playerCharacter);

            foreach (Collider2D hit in hits)
            {
                Monster monster;

                if (!TryGetValidMonsterTarget(hit, out monster))
                {
                    continue;
                }

                if (IsSameTarget(originalTarget, monster))
                {
                    continue;
                }

                int splashId = monster.gameObject.GetInstanceID();

                if (damagedIds.Contains(splashId))
                {
                    continue;
                }

                IDamageable splashDamageable = monster as IDamageable;
                Component splashComponent = monster;

                if (splashDamageable == null || splashComponent == null)
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