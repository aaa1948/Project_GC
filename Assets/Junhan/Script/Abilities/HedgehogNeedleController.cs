using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    public class HedgehogNeedleController : MonoBehaviour
    {
        private Character sourceCharacter;
        private EntityManager entityManager;
        private SyringeDartAbility sourceNeedleAbility;

        private GameObject projectilePrefab;
        private LayerMask monsterLayer;
        private int projectilePoolIndex;

        private int fallbackNeedleCount;
        private float orbitRadius;
        private float rotationSpeed;
        private float touchFireCooldown;
        private float damageMultiplier;

        private Transform visualRoot;
        private readonly List<Transform> orbitNeedleVisuals = new List<Transform>();

        private Sprite projectileSprite;
        private int projectileSortingLayerId;
        private int projectileSortingOrder;
        private Vector3 projectileVisualBaseScale = Vector3.one;
        private Quaternion projectileVisualBaseRotation = Quaternion.identity;

        private int currentOrbitNeedleCount = -1;

        [Header("Needle Visual")]
        [Tooltip("침 이미지의 날 부분이 바깥쪽을 향하도록 보정하는 각도입니다. 방향이 이상하면 135, -45, 45, -135 중 하나로 테스트하세요.")]
        [SerializeField] private float visualForwardAngleOffset = 135f;

        [Tooltip("고슴도침 회전 침 시각 크기 배율입니다. Visual_Needle의 실제 크기에 곱해집니다.")]
        [SerializeField] private float orbitVisualScaleMultiplier = 1f;

        [Tooltip("고슴도침 회전 속도 배율입니다. 기존 속도가 너무 빠르므로 기본값은 0.35배입니다.")]
        [SerializeField] private float rotationSpeedMultiplier = 0.35f;

        private readonly Dictionary<int, float> nextFireAllowedTimeByTarget =
            new Dictionary<int, float>();
        private readonly HashSet<int> touchingShieldTargetIds = new HashSet<int>();
        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

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
            HedgehogNeedleController controller =
                controllerObject.AddComponent<HedgehogNeedleController>();

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

            fallbackNeedleCount = Mathf.Max(1, needleCount);
            this.orbitRadius = Mathf.Max(0.1f, orbitRadius);
            this.rotationSpeed = rotationSpeed;
            this.touchFireCooldown = Mathf.Max(0.05f, touchFireCooldown);
            this.damageMultiplier = Mathf.Max(0.01f, damageMultiplier);

            projectilePrefab = sourceNeedleAbility.ProjectilePrefab;
            monsterLayer = sourceNeedleAbility.MonsterLayer;
            projectilePoolIndex = entityManager.AddPoolForProjectile(projectilePrefab);

            CacheProjectileVisualInfo();

            transform.position = GetSourceCenterPosition();

            RebuildOrbitVisuals(GetDesiredOrbitNeedleCount());

            if (sourceCharacter != null && sourceCharacter.OnDeath != null)
            {
                sourceCharacter.OnDeath.AddListener(DestroySelf);
            }

            if (debugLog)
            {
                Debug.Log("[고슴도침] HedgehogNeedleController 생성 완료");
            }
        }

        private void Update()
        {
            if (sourceCharacter == null || entityManager == null || sourceNeedleAbility == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = GetSourceCenterPosition();

            int desiredCount = GetDesiredOrbitNeedleCount();

            if (desiredCount != currentOrbitNeedleCount)
            {
                RebuildOrbitVisuals(desiredCount);
            }

            if (visualRoot != null)
            {
                visualRoot.Rotate(
                    Vector3.forward,
                    rotationSpeed * Mathf.Max(0f, rotationSpeedMultiplier) * Time.deltaTime
                );
            }

            DetectEnemiesTouchingShield();
        }

        private int GetDesiredOrbitNeedleCount()
        {
            if (sourceNeedleAbility != null)
            {
                return Mathf.Max(1, sourceNeedleAbility.GetEffectiveProjectileCount());
            }

            return Mathf.Max(1, fallbackNeedleCount);
        }

        private void CacheProjectileVisualInfo()
        {
            SpriteRenderer sourceRenderer = FindPreferredProjectileSpriteRenderer();

            if (sourceRenderer != null)
            {
                projectileSprite = sourceRenderer.sprite;
                projectileSortingLayerId = sourceRenderer.sortingLayerID;
                projectileSortingOrder = sourceRenderer.sortingOrder;
                projectileVisualBaseScale = sourceRenderer.transform.localScale;
                projectileVisualBaseRotation = sourceRenderer.transform.localRotation;
            }
            else
            {
                Debug.LogWarning("[고슴도침] 투사체 프리팹에서 SpriteRenderer를 찾지 못했습니다. 고슴도침 시각 오브젝트가 보이지 않을 수 있습니다.");
                projectileVisualBaseScale = Vector3.one;
                projectileVisualBaseRotation = Quaternion.identity;
            }
        }

        private SpriteRenderer FindPreferredProjectileSpriteRenderer()
        {
            if (projectilePrefab == null)
            {
                return null;
            }

            SpriteRenderer[] renderers = projectilePrefab.GetComponentsInChildren<SpriteRenderer>(true);

            SpriteRenderer firstEnabledRenderer = null;
            SpriteRenderer firstRendererWithSprite = null;

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];

                if (renderer == null || renderer.sprite == null)
                {
                    continue;
                }

                if (firstRendererWithSprite == null)
                {
                    firstRendererWithSprite = renderer;
                }

                if (renderer.enabled && firstEnabledRenderer == null)
                {
                    firstEnabledRenderer = renderer;
                }

                if (renderer.gameObject.name.IndexOf("Visual", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    renderer.gameObject.name.IndexOf("Needle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return renderer;
                }
            }

            return firstEnabledRenderer != null ? firstEnabledRenderer : firstRendererWithSprite;
        }

        private Vector2 GetSourceCenterPosition()
        {
            if (sourceCharacter == null)
            {
                return transform.position;
            }

            if (sourceCharacter.CenterTransform != null)
            {
                return sourceCharacter.CenterTransform.position;
            }

            return sourceCharacter.transform.position;
        }

        private void RebuildOrbitVisuals(int needleCount)
        {
            if (visualRoot != null)
            {
                Destroy(visualRoot.gameObject);
            }

            orbitNeedleVisuals.Clear();

            currentOrbitNeedleCount = Mathf.Max(1, needleCount);

            visualRoot = new GameObject("Orbit Needle Visuals").transform;
            visualRoot.SetParent(transform);
            visualRoot.localPosition = Vector3.zero;

            for (int i = 0; i < currentOrbitNeedleCount; i++)
            {
                GameObject visualObject = new GameObject($"Orbit Needle Visual {i + 1}");
                visualObject.transform.SetParent(visualRoot);

                // 항상 원점 기준 균등 배치:
                // 2개 = 180도, 3개 = 120도, 4개 = 90도 ...
                float angle = i * 360f / currentOrbitNeedleCount;
                Vector2 direction = AngleToVector(angle);

                visualObject.transform.localPosition = direction * orbitRadius;
                visualObject.transform.localRotation =
                    Quaternion.Euler(0f, 0f, angle + visualForwardAngleOffset) *
                    projectileVisualBaseRotation;

                SpriteRenderer visualRenderer = visualObject.AddComponent<SpriteRenderer>();

                if (projectileSprite != null)
                {
                    visualRenderer.sprite = projectileSprite;
                    visualRenderer.sortingLayerID = projectileSortingLayerId;
                    visualRenderer.sortingOrder = projectileSortingOrder + 10;
                    visualRenderer.color = new Color(1f, 1f, 1f, 0.65f);

                    visualObject.transform.localScale =
                        projectileVisualBaseScale * orbitVisualScaleMultiplier;
                }

                orbitNeedleVisuals.Add(visualObject.transform);
            }

            if (debugLog)
            {
                Debug.Log($"[고슴도침] 회전 침 재배치 완료 | 개수 {currentOrbitNeedleCount}");
            }
        }

        private void DetectEnemiesTouchingShield()
        {
            Vector2 centerPosition = GetSourceCenterPosition();

            Collider2D[] hits = Physics2D.OverlapCircleAll(centerPosition, orbitRadius);

            HashSet<int> touchingThisFrame = new HashSet<int>();

            if (hits == null || hits.Length == 0)
            {
                touchingShieldTargetIds.Clear();
                return;
            }

            HashSet<int> checkedTargetsThisFrame = new HashSet<int>();

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

                if (checkedTargetsThisFrame.Contains(targetId))
                {
                    continue;
                }

                checkedTargetsThisFrame.Add(targetId);
                touchingThisFrame.Add(targetId);

                // 이미 결계와 접촉 중이면 반격 침 반복 발사 안 함
                if (touchingShieldTargetIds.Contains(targetId))
                {
                    continue;
                }

                FireCounterNeedleAtTarget(monster);
                touchingShieldTargetIds.Add(targetId);
            }

            // 결계 밖으로 나간 대상은 기록 제거
            // 다시 들어오면 다시 반격 침 1회 가능
            List<int> removeTargets = new List<int>();

            foreach (int targetId in touchingShieldTargetIds)
            {
                if (!touchingThisFrame.Contains(targetId))
                {
                    removeTargets.Add(targetId);
                }
            }

            for (int i = 0; i < removeTargets.Count; i++)
            {
                touchingShieldTargetIds.Remove(removeTargets[i]);
            }
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

            TrapMonster trapMonster = monster as TrapMonster;

            if (trapMonster != null && !trapMonster.IsActive)
            {
                return false;
            }

            return true;
        }

        private bool CanFireAtTarget(int targetId)
        {
            if (!nextFireAllowedTimeByTarget.TryGetValue(targetId, out float nextAllowedTime))
            {
                return true;
            }

            return Time.time >= nextAllowedTime;
        }

        private void FireCounterNeedleAtTarget(Monster targetMonster)
        {
            if (targetMonster == null || sourceCharacter == null || entityManager == null || sourceNeedleAbility == null)
            {
                return;
            }

            Vector2 centerPosition = GetSourceCenterPosition();

            Vector2 targetPosition;

            if (targetMonster.CenterTransform != null)
            {
                targetPosition = targetMonster.CenterTransform.position;
            }
            else
            {
                targetPosition = targetMonster.transform.position;
            }

            Vector2 direction = targetPosition - centerPosition;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();

            Vector2 spawnPosition = centerPosition + direction * orbitRadius;

            SyringeSpecialRuntime runtime = sourceNeedleAbility.GetCurrentSpecialRuntime();

            // 고슴도침 반격 침은 몬스터를 밀어내지 않도록 넉백을 0으로 고정한다.
            Projectile projectile = entityManager.SpawnProjectile(
                projectilePoolIndex,
                spawnPosition,
                sourceNeedleAbility.GetEffectiveDamage() * damageMultiplier,
                0f,
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

            if (debugLog)
            {
                Debug.Log($"[고슴도침] 반격 침 발사 대상: {targetMonster.name} | 넉백 없음");
            }
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
            Gizmos.DrawWireSphere(transform.position, orbitRadius);
        }
    }
}