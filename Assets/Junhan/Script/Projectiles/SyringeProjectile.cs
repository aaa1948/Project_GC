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
            ReturnCurveToPlayer,
            ReturnToPlayer
        }

        private SyringeSpecialRuntime specials;
        private int remainingPierces;

        // 유도침이 이미 맞힌 대상을 다시 추적하지 않게 하기 위한 기록
        private readonly HashSet<int> hitTargetIds = new HashSet<int>();

        // 실제 데미지 중복 방지용 기록
        // 하나의 침 투사체는 같은 몬스터에게 1번만 데미지를 준다.
        private readonly HashSet<int> damagedTargetIds = new HashSet<int>();

        // 풀링으로 재사용될 때 maxDistance가 계속 누적되는 것을 막기 위한 원본 사거리 저장
        private float baseMaxDistance;

        // 침귀환 상태
        private NeedleFlightState flightState = NeedleFlightState.Normal;
        private float returnTimer = 0f;

        // 침귀환: 적중 후 1회 관통
        private Vector2 returnForwardDirection;
        private float returnForwardTravelled = 0f;

        // 침귀환: 물방울 호 형태 곡선 복귀
        private float returnCurveTimer = 0f;
        private Vector2 returnCurveStart;
        private Vector2 returnCurveControl1;
        private Vector2 returnCurveControl2;
        private Vector2 returnCurveEnd;

        [Header("Needle Flight / 침 비행")]
        [Tooltip("침 이미지의 날 부분이 이동 방향을 향하도록 보정하는 각도입니다. 방향이 반대면 135, -45, 45, -135 중 하나로 테스트하세요.")]
        [SerializeField] private float visualForwardAngleOffset = 135f;

        [Tooltip("침귀환 발동 시, 적을 맞힌 뒤 앞으로 한 번 뚫고 지나가는 거리입니다.")]
        [SerializeField] private float returnNeedleForwardPassDistance = 0.65f;

        [Tooltip("침귀환이 물방울 호처럼 휘어지는 데 걸리는 시간입니다.")]
        [SerializeField] private float returnNeedleCurveDuration = 0.35f;

        [Tooltip("적중 후 앞으로 더 뻗어나가는 곡선 핸들 길이입니다.")]
        [SerializeField] private float returnNeedleCurveForwardHandle = 0.9f;

        [Tooltip("몬스터 중심 기준 옆으로 벌어지는 곡선 폭입니다. 몬스터 한 마리 정도 여유를 주려면 1~1.4 정도가 적당합니다.")]
        [SerializeField] private float returnNeedleCurveSideOffset = 1.2f;

        [Tooltip("곡선이 플레이어 쪽으로 끌려오는 정도입니다.")]
        [SerializeField] private float returnNeedleCurveReturnPull = 0.7f;

        [Header("Manual Hit Scan / 수동 적중 검사")]
        [Tooltip("Trigger 이벤트가 누락되어도 침 이동 경로를 직접 검사해서 적중시키는 기능입니다.")]
        [SerializeField] private bool useManualHitScan = true;

        [Tooltip("침이 이동한 경로를 검사할 때 사용할 원형 반경입니다. 침이 너무 잘 안 맞으면 0.12~0.18 사이로 올려보세요.")]
        [SerializeField] private float manualHitScanRadius = 0.12f;

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
            flightState == NeedleFlightState.ReturnCurveToPlayer ||
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
            damagedTargetIds.Clear();

            flightState = NeedleFlightState.Normal;
            returnTimer = 0f;
            returnForwardTravelled = 0f;
            returnCurveTimer = 0f;

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

                        Vector2 normalPreviousPosition = transform.position;
                        Vector2 normalNextPosition =
                            normalPreviousPosition + direction * normalStep;

                        NeedleFlightState normalStateBeforeMove = flightState;

                        bool normalCanContinue =
                            PerformManualHitScan(normalPreviousPosition, normalNextPosition);

                        if (!normalCanContinue || isDespawning)
                        {
                            yield break;
                        }

                        if (flightState == normalStateBeforeMove)
                        {
                            transform.position = normalNextPosition;
                            distanceTravelled += normalStep;
                            ApplyVisualRotationToDirection(direction);
                        }

                        speed -= airResistance * Time.deltaTime;
                        break;

                    case NeedleFlightState.ReturnForwardPass:
                        if (!MoveReturnForwardPass())
                        {
                            yield break;
                        }
                        break;

                    case NeedleFlightState.ReturnCurveToPlayer:
                        if (!MoveReturnCurveToPlayer())
                        {
                            yield break;
                        }
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

        private bool TryRegisterProjectileHit(int targetId)
        {
            if (targetId < 0)
            {
                return false;
            }

            // 하나의 침 투사체는 같은 대상에게 절대 2번 이상 데미지를 주지 않는다.
            if (damagedTargetIds.Contains(targetId))
            {
                return false;
            }

            damagedTargetIds.Add(targetId);

            // 유도침이 이미 맞힌 대상은 다시 추적하지 않도록 같이 기록한다.
            hitTargetIds.Add(targetId);

            return true;
        }

        private bool PerformManualHitScan(Vector2 previousPosition, Vector2 nextPosition)
        {
            if (!useManualHitScan)
            {
                return true;
            }

            float radius = Mathf.Max(0.01f, manualHitScanRadius);

            // 현재 위치에 이미 겹쳐 있는 대상 검사
            if (!ProcessOverlapHits(previousPosition, radius))
            {
                return false;
            }

            Vector2 delta = nextPosition - previousPosition;
            float distance = delta.magnitude;

            if (distance <= 0.0001f)
            {
                return ProcessOverlapHits(nextPosition, radius);
            }

            Vector2 castDirection = delta / distance;

            RaycastHit2D[] hits = Physics2D.CircleCastAll(
                previousPosition,
                radius,
                castDirection,
                distance
            );

            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(
                    hits,
                    (a, b) => a.distance.CompareTo(b.distance)
                );

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D hitCollider = hits[i].collider;

                    if (hitCollider == null)
                    {
                        continue;
                    }

                    Vector2 hitPosition = hits[i].point;

                    if (hitPosition == Vector2.zero)
                    {
                        hitPosition = hitCollider.ClosestPoint(previousPosition);
                    }

                    transform.position = hitPosition;

                    bool canContinue = ProcessHitCollider(hitCollider);

                    if (!canContinue)
                    {
                        return false;
                    }

                    if (isDespawning)
                    {
                        return false;
                    }

                    // 적중으로 귀환 상태가 시작됐다면 이번 프레임의 직선 이동은 여기서 멈춘다.
                    if (flightState == NeedleFlightState.ReturnForwardPass &&
                        !IsReturnModeStartedByPreviousStateCheck())
                    {
                        return true;
                    }
                }
            }

            // 최종 위치에서도 한 번 더 검사
            return ProcessOverlapHits(nextPosition, radius);
        }

        // 현재 구조에서는 위 함수명이 의미상 어색하지만,
        // 실질적으로는 "상태 변경 후 현재 이동 루프를 계속해도 되는지"를 보조하는 용도다.
        private bool IsReturnModeStartedByPreviousStateCheck()
        {
            return false;
        }

        private bool ProcessOverlapHits(Vector2 position, float radius)
        {
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, radius);

            if (overlaps == null || overlaps.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider2D overlap = overlaps[i];

                if (overlap == null)
                {
                    continue;
                }

                bool canContinue = ProcessHitCollider(overlap);

                if (!canContinue)
                {
                    return false;
                }

                if (isDespawning)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessHitCollider(Collider2D collider)
        {
            if (collider == null || isDespawning || !gameObject.activeInHierarchy)
            {
                return true;
            }

            bool isInTargetLayer = (targetLayer & (1 << collider.gameObject.layer)) != 0;

            Monster monsterTarget;
            bool isValidMonsterTarget = TryGetValidMonsterTarget(collider, out monsterTarget);

            // 휴면 상태 TrapMonster처럼 Monster이지만 유효하지 않은 대상은 그냥 통과시킨다.
            if (!isValidMonsterTarget)
            {
                if (monsterTarget != null)
                {
                    return true;
                }

                if (!isInTargetLayer)
                {
                    return true;
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
                        return false;
                    }

                    return true;
                }

                targetId = damageableComponent.gameObject.GetInstanceID();
            }

            if (damageable == null || damageableComponent == null)
            {
                if (!IsReturnMode)
                {
                    HitNothing();
                    return false;
                }

                return true;
            }

            // 핵심:
            // 기존의 타겟 판정은 유지하고,
            // 같은 침이 같은 몬스터를 여러 번 때리는 것만 여기서 차단한다.
            if (!TryRegisterProjectileHit(targetId))
            {
                return true;
            }

            float rawDamage = IsReturnMode
                ? damage * Mathf.Max(0.01f, specials.returnNeedleDamageMultiplier)
                : damage;

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
                return true;
            }

            bool canPierce = specials.pierceEnabled && remainingPierces > 0;

            // 관통침 + 침귀환 조합:
            // 관통 횟수를 먼저 소모하고, 관통이 끝난 다음 귀환한다.
            if (canPierce)
            {
                remainingPierces--;
                return true;
            }

            if (specials.returnNeedleEnabled)
            {
                BeginReturnNeedleSequence(monsterTarget, direction);
                return true;
            }

            DestroyProjectile();
            return false;
        }

        protected override void OnTriggerEnter2D(Collider2D collider)
        {
            ProcessHitCollider(collider);
        }

        private void OnTriggerStay2D(Collider2D collider)
        {
            ProcessHitCollider(collider);
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
            returnCurveTimer = 0f;

            flightState = NeedleFlightState.ReturnForwardPass;
        }

        private bool MoveReturnForwardPass()
        {
            returnTimer += Time.deltaTime;

            if (returnTimer >= Mathf.Max(0.1f, specials.returnNeedleMaxDuration))
            {
                HitNothing();
                return false;
            }

            float step = speed * Time.deltaTime;

            Vector2 previousPosition = transform.position;
            Vector2 nextPosition =
                previousPosition + returnForwardDirection * step;

            NeedleFlightState stateBeforeMove = flightState;

            bool canContinue = PerformManualHitScan(previousPosition, nextPosition);

            if (!canContinue || isDespawning)
            {
                return false;
            }

            if (flightState == stateBeforeMove)
            {
                transform.position = nextPosition;
                returnForwardTravelled += step;

                direction = returnForwardDirection;
                ApplyVisualRotationToDirection(direction);

                if (returnForwardTravelled >= Mathf.Max(0f, returnNeedleForwardPassDistance))
                {
                    BeginReturnCurveToPlayer();
                }
            }

            return true;
        }

        private void BeginReturnCurveToPlayer()
        {
            if (playerCharacter == null || playerCharacter.CenterTransform == null)
            {
                HitNothing();
                return;
            }

            Vector2 start = transform.position;
            Vector2 playerPosition = playerCharacter.CenterTransform.position;

            Vector2 toPlayer = playerPosition - start;

            if (toPlayer.sqrMagnitude <= 0.0001f)
            {
                toPlayer = -returnForwardDirection;
            }

            Vector2 toPlayerDirection = toPlayer.normalized;

            Vector2 sideA = new Vector2(-returnForwardDirection.y, returnForwardDirection.x).normalized;
            Vector2 sideB = -sideA;

            // 플레이어 방향과 더 잘 이어지는 쪽으로 호를 휘게 한다.
            Vector2 chosenSide = Vector2.Dot(sideA, toPlayerDirection) >= Vector2.Dot(sideB, toPlayerDirection)
                ? sideA
                : sideB;

            float sideOffset = Mathf.Max(0.1f, returnNeedleCurveSideOffset);
            float forwardHandle = Mathf.Max(0.05f, returnNeedleCurveForwardHandle);
            float returnPull = Mathf.Max(0.05f, returnNeedleCurveReturnPull);

            returnCurveStart = start;

            // 물방울 호 느낌:
            // P0: 적을 관통한 지점
            // P1: 기존 진행 방향으로 조금 더 뻗음
            // P2: 옆으로 둥글게 빠지면서 플레이어 방향으로 끌림
            // P3: 플레이어 근처
            returnCurveControl1 = start + returnForwardDirection * forwardHandle;
            returnCurveControl2 = start + chosenSide * sideOffset + toPlayerDirection * returnPull;
            returnCurveEnd = playerPosition;

            returnCurveTimer = 0f;
            flightState = NeedleFlightState.ReturnCurveToPlayer;
        }

        private bool MoveReturnCurveToPlayer()
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

            float duration = Mathf.Max(0.05f, returnNeedleCurveDuration);
            returnCurveTimer += Time.deltaTime;

            float t = Mathf.Clamp01(returnCurveTimer / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            Vector2 previousPosition = transform.position;
            Vector2 nextPosition = CubicBezier(
                returnCurveStart,
                returnCurveControl1,
                returnCurveControl2,
                returnCurveEnd,
                easedT
            );

            NeedleFlightState stateBeforeMove = flightState;

            bool canContinue = PerformManualHitScan(previousPosition, nextPosition);

            if (!canContinue || isDespawning)
            {
                return false;
            }

            if (flightState == stateBeforeMove)
            {
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
                }
            }

            return true;
        }

        private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float oneMinusT = 1f - t;

            return
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;
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

            Vector2 nextPosition = currentPosition + direction * returnStep;

            NeedleFlightState stateBeforeMove = flightState;

            bool canContinue = PerformManualHitScan(currentPosition, nextPosition);

            if (!canContinue || isDespawning)
            {
                return false;
            }

            if (flightState == stateBeforeMove)
            {
                transform.position = nextPosition;
                ApplyVisualRotationToDirection(direction);
            }

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