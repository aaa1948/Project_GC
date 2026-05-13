using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    // 전설 증강: 고슴도침
    //
    // 역할:
    // - 플레이어 주변에 회전하는 침 결계를 만든다.
    // - 시간이 지나면 자동 발사하지 않는다.
    // - 적이 실드 범위에 닿았을 때만 해당 적 방향으로 반격 침을 발사한다.
    //
    // 수정 내용:
    // - 기존 monsterLayer 기반 검색을 제거하고 Monster 컴포넌트 기준으로 감지한다.
    // - 보스가 Monster Full / Monster Legs 레이어 밖에 있어도 Monster 계열이면 감지 가능하다.
    // - 휴면 상태 TrapMonster는 감지하지 않는다.
    // - 활성화 상태 TrapMonster는 감지한다.
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

        // 같은 대상에게 매 프레임 반격 침이 발사되는 것을 막기 위한 대상별 쿨타임
        private readonly Dictionary<int, float> nextFireAllowedTimeByTarget = new Dictionary<int, float>();

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

            transform.position = GetSourceCenterPosition();

            CreateOrbitVisuals();

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

            if (visualRoot != null)
            {
                visualRoot.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            }

            DetectEnemiesTouchingShield();
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
                    visualRenderer.sortingOrder = sourceRenderer.sortingOrder + 10;
                    visualRenderer.color = new Color(1f, 1f, 1f, 0.65f);
                    visualObject.transform.localScale = projectilePrefab.transform.localScale;
                }

                orbitNeedleVisuals.Add(visualObject.transform);
            }
        }

        private void DetectEnemiesTouchingShield()
        {
            Vector2 centerPosition = GetSourceCenterPosition();

            // 핵심 수정:
            // 기존에는 monsterLayer만 검색해서 보스가 레이어 밖이면 감지하지 못했다.
            // 이제는 주변 Collider 전체를 찾고, Monster 컴포넌트가 있는 대상만 필터링한다.
            Collider2D[] hits = Physics2D.OverlapCircleAll(centerPosition, orbitRadius);

            if (hits == null || hits.Length == 0)
            {
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

                // 같은 몬스터가 여러 Collider를 가지고 있을 수 있으므로 한 프레임에 한 번만 처리
                if (checkedTargetsThisFrame.Contains(targetId))
                {
                    continue;
                }

                checkedTargetsThisFrame.Add(targetId);

                if (!CanFireAtTarget(targetId))
                {
                    continue;
                }

                FireCounterNeedleAtTarget(monster);

                nextFireAllowedTimeByTarget[targetId] = Time.time + touchFireCooldown;
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

            // 휴면 상태 함정 몬스터는 고슴도침 대상에서 제외한다.
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

            if (debugLog)
            {
                Debug.Log($"[고슴도침] 반격 침 발사 대상: {targetMonster.name}");
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