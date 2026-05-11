using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    // 전설 증강: 고슴도침
    // 수정 버전:
    // 시간이 지나면 자동으로 발사하지 않는다.
    // 적이 플레이어 주변의 침 실드 범위에 닿았을 때만 해당 적 방향으로 반격 침을 발사한다.
    public class HedgehogNeedleController : MonoBehaviour
    {
        private Character sourceCharacter;
        private EntityManager entityManager;
        private SyringeDartAbility sourceNeedleAbility;

        private GameObject projectilePrefab;
        private LayerMask monsterLayer;
        private int projectilePoolIndex;

        private int needleCount;
        private float orbitRadius;
        private float rotationSpeed;
        private float touchFireCooldown;
        private float damageMultiplier;

        private Transform visualRoot;
        private readonly List<Transform> orbitNeedleVisuals = new List<Transform>();

        // 같은 적에게 매 프레임 침이 발사되는 것을 막기 위한 대상별 쿨타임
        private readonly Dictionary<int, float> nextFireAllowedTimeByTarget = new Dictionary<int, float>();

        public static HedgehogNeedleController Create(
            Character sourceCharacter,
            EntityManager entityManager,
            SyringeDartAbility sourceNeedleAbility,
            int needleCount,
            float orbitRadius,
            float rotationSpeed,
            float touchFireCooldown,
            float damageMultiplier)
        {
            GameObject controllerObject = new GameObject("Hedgehog Needle Barrier");
            HedgehogNeedleController controller = controllerObject.AddComponent<HedgehogNeedleController>();

            controller.Init(
                sourceCharacter,
                entityManager,
                sourceNeedleAbility,
                needleCount,
                orbitRadius,
                rotationSpeed,
                touchFireCooldown,
                damageMultiplier
            );

            return controller;
        }

        private void Init(
            Character sourceCharacter,
            EntityManager entityManager,
            SyringeDartAbility sourceNeedleAbility,
            int needleCount,
            float orbitRadius,
            float rotationSpeed,
            float touchFireCooldown,
            float damageMultiplier)
        {
            this.sourceCharacter = sourceCharacter;
            this.entityManager = entityManager;
            this.sourceNeedleAbility = sourceNeedleAbility;

            this.needleCount = Mathf.Max(1, needleCount);
            this.orbitRadius = Mathf.Max(0.1f, orbitRadius);
            this.rotationSpeed = rotationSpeed;
            this.touchFireCooldown = Mathf.Max(0.05f, touchFireCooldown);
            this.damageMultiplier = Mathf.Max(0.01f, damageMultiplier);

            projectilePrefab = sourceNeedleAbility.ProjectilePrefab;
            monsterLayer = sourceNeedleAbility.MonsterLayer;

            projectilePoolIndex = entityManager.AddPoolForProjectile(projectilePrefab);

            transform.position = sourceCharacter.CenterTransform.position;

            CreateOrbitVisuals();

            if (sourceCharacter.OnDeath != null)
            {
                sourceCharacter.OnDeath.AddListener(DestroySelf);
            }
        }

        private void Update()
        {
            if (sourceCharacter == null || entityManager == null || sourceNeedleAbility == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = sourceCharacter.CenterTransform.position;

            if (visualRoot != null)
            {
                visualRoot.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            }

            DetectEnemiesTouchingShield();
        }

        private void CreateOrbitVisuals()
        {
            visualRoot = new GameObject("Orbit Needle Visuals").transform;
            visualRoot.SetParent(transform);
            visualRoot.localPosition = Vector3.zero;

            SpriteRenderer sourceRenderer = null;

            if (projectilePrefab != null)
            {
                sourceRenderer = projectilePrefab.GetComponentInChildren<SpriteRenderer>();
            }

            for (int i = 0; i < needleCount; i++)
            {
                GameObject visualObject = new GameObject($"Orbit Needle Visual {i + 1}");
                visualObject.transform.SetParent(visualRoot);

                float angle = i * 360f / needleCount;
                Vector2 direction = AngleToVector(angle);

                visualObject.transform.localPosition = direction * orbitRadius;
                visualObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

                SpriteRenderer visualRenderer = visualObject.AddComponent<SpriteRenderer>();

                if (sourceRenderer != null)
                {
                    visualRenderer.sprite = sourceRenderer.sprite;
                    visualRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
                    visualRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;
                    visualRenderer.color = new Color(1f, 1f, 1f, 0.65f);
                    visualObject.transform.localScale = projectilePrefab.transform.localScale;
                }

                orbitNeedleVisuals.Add(visualObject.transform);
            }
        }

        private void DetectEnemiesTouchingShield()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                sourceCharacter.CenterTransform.position,
                orbitRadius,
                monsterLayer
            );

            if (hits == null || hits.Length == 0)
            {
                return;
            }

            HashSet<int> checkedTargetsThisFrame = new HashSet<int>();

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                Component damageableComponent = damageable as Component;

                if (damageable == null || damageableComponent == null)
                {
                    continue;
                }

                int targetId = damageableComponent.gameObject.GetInstanceID();

                // 같은 적이 여러 콜라이더를 가지고 있을 수 있으니 한 프레임에 한 번만 처리
                if (checkedTargetsThisFrame.Contains(targetId))
                {
                    continue;
                }

                checkedTargetsThisFrame.Add(targetId);

                if (!CanFireAtTarget(targetId))
                {
                    continue;
                }

                FireCounterNeedleAtTarget(damageableComponent.transform);
                nextFireAllowedTimeByTarget[targetId] = Time.time + touchFireCooldown;
            }
        }

        private bool CanFireAtTarget(int targetId)
        {
            if (!nextFireAllowedTimeByTarget.TryGetValue(targetId, out float nextAllowedTime))
            {
                return true;
            }

            return Time.time >= nextAllowedTime;
        }

        private void FireCounterNeedleAtTarget(Transform target)
        {
            if (target == null || sourceCharacter == null || entityManager == null || sourceNeedleAbility == null)
            {
                return;
            }

            Vector2 centerPosition = sourceCharacter.CenterTransform.position;
            Vector2 targetPosition = target.position;

            Vector2 direction = targetPosition - centerPosition;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();

            // 실드 가장자리에서 적 방향으로 침이 나가는 느낌
            Vector2 spawnPosition = centerPosition + direction * orbitRadius;

            SyringeSpecialRuntime runtime = sourceNeedleAbility.GetCurrentSpecialRuntime();

            Projectile projectile = entityManager.SpawnProjectile(
                projectilePoolIndex,
                spawnPosition,
                sourceNeedleAbility.GetEffectiveDamage() * damageMultiplier,
                sourceNeedleAbility.GetEffectiveKnockback(),
                sourceNeedleAbility.GetEffectiveSpeed(),
                monsterLayer
            );

            if (projectile == null)
            {
                return;
            }

            projectile.transform.localScale =
                Vector3.one * sourceCharacter.ProjectileSizeMultiplier * 0.8f;

            projectile.maxDistance *= sourceCharacter.RangeMultiplier;

            if (projectile is SyringeProjectile syringeProjectile)
            {
                syringeProjectile.ConfigureSpecials(runtime);
            }

            projectile.OnHitDamageable.AddListener(sourceCharacter.OnDealDamage.Invoke);
            projectile.Launch(direction);
        }

        private Vector2 AngleToVector(float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;

            return new Vector2(
                Mathf.Cos(rad),
                Mathf.Sin(rad)
            ).normalized;
        }

        private void DestroySelf()
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (sourceCharacter != null && sourceCharacter.OnDeath != null)
            {
                sourceCharacter.OnDeath.RemoveListener(DestroySelf);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 씬 뷰에서 고슴도침 실드 범위를 확인하기 위한 디버그 표시
            Gizmos.DrawWireSphere(transform.position, orbitRadius);
        }
    }
}