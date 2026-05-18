using UnityEngine;

namespace Vampire
{
    public class BossHomingMissile : MonoBehaviour
    {
        private Character targetCharacter;

        private float speed;
        private float damage;
        private float lifeTime;

        private float turnSpeed;
        private float laneOffsetDistance;
        private float laneFadeDistance;
        private float initialSpreadDuration;

        private bool destroyByPlayerProjectile;
        private LayerMask playerProjectileLayerMask;
        private float projectileHitCheckRadius;

        private bool rotateToMoveDirection;
        private float visualForwardAngleOffset;
        private GameObject destroyEffectPrefab;

        private Vector2 moveDirection;
        private int laneIndex;
        private int laneCount;

        private float spawnTime;
        private bool initialized;
        private bool isDestroying;

        private Rigidbody2D rb;
        private Collider2D missileCollider;

        public void Init(
            Character target,
            Vector2 initialDirection,
            float missileSpeed,
            float missileDamage,
            float missileLifeTime,
            int missileLaneIndex,
            int totalLaneCount,
            float missileTurnSpeed,
            float missileLaneOffsetDistance,
            float missileLaneFadeDistance,
            float missileInitialSpreadDuration,
            bool canBeDestroyedByPlayerProjectile,
            LayerMask projectileLayerMask,
            float projectileCheckRadius,
            bool shouldRotateToMoveDirection,
            float missileVisualForwardAngleOffset,
            GameObject missileDestroyEffectPrefab
        )
        {
            targetCharacter = target;

            if (initialDirection == Vector2.zero)
            {
                initialDirection = Vector2.right;
            }

            moveDirection = initialDirection.normalized;

            speed = missileSpeed;
            damage = missileDamage;
            lifeTime = missileLifeTime;

            laneIndex = missileLaneIndex;
            laneCount = Mathf.Max(1, totalLaneCount);

            turnSpeed = missileTurnSpeed;
            laneOffsetDistance = missileLaneOffsetDistance;
            laneFadeDistance = missileLaneFadeDistance;
            initialSpreadDuration = missileInitialSpreadDuration;

            destroyByPlayerProjectile = canBeDestroyedByPlayerProjectile;
            playerProjectileLayerMask = projectileLayerMask;
            projectileHitCheckRadius = projectileCheckRadius;

            rotateToMoveDirection = shouldRotateToMoveDirection;
            visualForwardAngleOffset = missileVisualForwardAngleOffset;
            destroyEffectPrefab = missileDestroyEffectPrefab;

            spawnTime = Time.time;
            initialized = true;
            isDestroying = false;

            ApplyVisualRotation(moveDirection);
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            missileCollider = GetComponent<Collider2D>();

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void Update()
        {
            if (!initialized || isDestroying)
            {
                return;
            }

            if (lifeTime > 0f && Time.time >= spawnTime + lifeTime)
            {
                DestroyMissile();
                return;
            }

            MoveMissile();

            if (destroyByPlayerProjectile)
            {
                CheckPlayerProjectileOverlap();
            }
        }

        private void MoveMissile()
        {
            Transform targetTransform = GetTargetTransform();

            if (targetTransform == null)
            {
                MoveForwardOnly();
                return;
            }

            Vector2 currentPosition = transform.position;
            Vector2 targetPosition = targetTransform.position;

            Vector2 desiredDirection = moveDirection;
            float elapsed = Time.time - spawnTime;

            if (elapsed < initialSpreadDuration)
            {
                desiredDirection = moveDirection;
            }
            else
            {
                Vector2 toPlayer = targetPosition - currentPosition;

                if (toPlayer.sqrMagnitude > 0.0001f)
                {
                    Vector2 toPlayerDirection = toPlayer.normalized;

                    Vector2 sideDirection = new Vector2(
                        -toPlayerDirection.y,
                        toPlayerDirection.x
                    );

                    float centerIndex = (laneCount - 1) * 0.5f;
                    float laneValue = laneIndex - centerIndex;

                    float distanceToPlayer = toPlayer.magnitude;
                    float laneFade = Mathf.Clamp01(
                        distanceToPlayer / Mathf.Max(0.01f, laneFadeDistance)
                    );

                    Vector2 laneTargetPosition =
                        targetPosition +
                        sideDirection * laneValue * laneOffsetDistance * laneFade;

                    Vector2 toLaneTarget = laneTargetPosition - currentPosition;

                    if (toLaneTarget.sqrMagnitude > 0.0001f)
                    {
                        desiredDirection = toLaneTarget.normalized;
                    }
                    else
                    {
                        desiredDirection = toPlayerDirection;
                    }
                }
            }

            moveDirection = Vector2.Lerp(
                moveDirection,
                desiredDirection,
                Mathf.Clamp01(turnSpeed * Time.deltaTime)
            ).normalized;

            Vector2 nextPosition = currentPosition + moveDirection * speed * Time.deltaTime;

            if (rb != null)
            {
                rb.MovePosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            ApplyVisualRotation(moveDirection);
        }

        private void MoveForwardOnly()
        {
            Vector2 currentPosition = transform.position;
            Vector2 nextPosition = currentPosition + moveDirection * speed * Time.deltaTime;

            if (rb != null)
            {
                rb.MovePosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            ApplyVisualRotation(moveDirection);
        }

        private Transform GetTargetTransform()
        {
            if (targetCharacter == null)
            {
                return null;
            }

            if (targetCharacter.CenterTransform != null)
            {
                return targetCharacter.CenterTransform;
            }

            return targetCharacter.transform;
        }

        private void ApplyVisualRotation(Vector2 direction)
        {
            if (!rotateToMoveDirection || direction == Vector2.zero)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                angle + visualForwardAngleOffset
            );
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized || isDestroying)
            {
                return;
            }

            if (TryHitPlayer(other))
            {
                return;
            }

            if (destroyByPlayerProjectile && TryDetectPlayerProjectile(other))
            {
                DestroyMissile();
            }
        }

        private bool TryHitPlayer(Collider2D other)
        {
            if (targetCharacter == null || other == null)
            {
                return false;
            }

            Character character = other.GetComponentInParent<Character>();

            if (character == null || character != targetCharacter)
            {
                return false;
            }

            targetCharacter.TakeDamage(damage);
            DestroyMissile();

            return true;
        }

        private bool TryDetectPlayerProjectile(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if ((playerProjectileLayerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return false;
            }

            Projectile projectile = other.GetComponentInParent<Projectile>();

            return projectile != null;
        }

        private void CheckPlayerProjectileOverlap()
        {
            if (projectileHitCheckRadius <= 0f)
            {
                return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                projectileHitCheckRadius,
                playerProjectileLayerMask
            );

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];

                if (hit == null || hit == missileCollider)
                {
                    continue;
                }

                Projectile projectile = hit.GetComponentInParent<Projectile>();

                if (projectile != null)
                {
                    DestroyMissile();
                    return;
                }
            }
        }

        private void DestroyMissile()
        {
            if (isDestroying)
            {
                return;
            }

            isDestroying = true;

            if (destroyEffectPrefab != null)
            {
                Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}