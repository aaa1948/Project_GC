using System.Collections;
using UnityEngine;

namespace Vampire
{
    // 특수 증강: 침술진
    // 대쉬 시작 시 대쉬 직전 위치에 분신을 생성하고,
    // 분신이 360도로 침을 발사한다.
    public class AcupunctureFormationController : MonoBehaviour
    {
        private Character sourceCharacter;
        private EntityManager entityManager;
        private SyringeDartAbility sourceNeedleAbility;

        private GameObject projectilePrefab;
        private LayerMask monsterLayer;
        private int projectilePoolIndex;

        private float formationLifetime;
        private int needleCount;
        private float damageMultiplier;
        private float visualScale;

        private bool previousIsDashing = false;
        private Vector3 previousPlayerPosition;

        private Sprite projectileSprite;
        private int projectileSortingLayerId;
        private int projectileSortingOrder;

        public static AcupunctureFormationController Create(
            Character sourceCharacter,
            EntityManager entityManager,
            SyringeDartAbility sourceNeedleAbility,
            float formationLifetime,
            int needleCount,
            float damageMultiplier,
            float visualScale)
        {
            GameObject controllerObject = new GameObject("Acupuncture Formation Controller");
            AcupunctureFormationController controller = controllerObject.AddComponent<AcupunctureFormationController>();

            controller.Init(
                sourceCharacter,
                entityManager,
                sourceNeedleAbility,
                formationLifetime,
                needleCount,
                damageMultiplier,
                visualScale
            );

            return controller;
        }

        private void Init(
            Character sourceCharacter,
            EntityManager entityManager,
            SyringeDartAbility sourceNeedleAbility,
            float formationLifetime,
            int needleCount,
            float damageMultiplier,
            float visualScale)
        {
            this.sourceCharacter = sourceCharacter;
            this.entityManager = entityManager;
            this.sourceNeedleAbility = sourceNeedleAbility;

            this.formationLifetime = Mathf.Max(0.05f, formationLifetime);
            this.needleCount = Mathf.Max(1, needleCount);
            this.damageMultiplier = Mathf.Max(0.01f, damageMultiplier);
            this.visualScale = Mathf.Max(0.01f, visualScale);

            projectilePrefab = sourceNeedleAbility.ProjectilePrefab;
            monsterLayer = sourceNeedleAbility.MonsterLayer;
            projectilePoolIndex = entityManager.AddPoolForProjectile(projectilePrefab);

            CacheProjectileVisualInfo();

            if (sourceCharacter != null)
            {
                previousPlayerPosition = sourceCharacter.CenterTransform.position;
                previousIsDashing = sourceCharacter.IsDashing;

                if (sourceCharacter.OnDeath != null)
                {
                    sourceCharacter.OnDeath.AddListener(DestroySelf);
                }
            }
        }

        private void Update()
        {
            if (sourceCharacter == null || entityManager == null || sourceNeedleAbility == null)
            {
                Destroy(gameObject);
                return;
            }

            bool currentIsDashing = sourceCharacter.IsDashing;

            // false -> true가 되는 순간이 대쉬 시작.
            // 이때 previousPlayerPosition은 대쉬 직전 플레이어 위치로 사용한다.
            if (!previousIsDashing && currentIsDashing)
            {
                SpawnFormation(previousPlayerPosition);
            }

            previousIsDashing = currentIsDashing;
            previousPlayerPosition = sourceCharacter.CenterTransform.position;
        }

        private void CacheProjectileVisualInfo()
        {
            SpriteRenderer sourceRenderer = null;

            if (projectilePrefab != null)
            {
                sourceRenderer = projectilePrefab.GetComponentInChildren<SpriteRenderer>();
            }

            if (sourceRenderer != null)
            {
                projectileSprite = sourceRenderer.sprite;
                projectileSortingLayerId = sourceRenderer.sortingLayerID;
                projectileSortingOrder = sourceRenderer.sortingOrder;
            }
        }

        private void SpawnFormation(Vector3 spawnPosition)
        {
            GameObject formationObject = new GameObject("Acupuncture Formation Clone");
            formationObject.transform.position = spawnPosition;
            formationObject.transform.localScale = Vector3.one * visualScale;

            SpriteRenderer renderer = formationObject.AddComponent<SpriteRenderer>();

            if (projectileSprite != null)
            {
                renderer.sprite = projectileSprite;
                renderer.sortingLayerID = projectileSortingLayerId;
                renderer.sortingOrder = projectileSortingOrder + 6;
                renderer.color = new Color(1f, 1f, 1f, 0.55f);
            }

            FireRadialNeedles(spawnPosition);
            StartCoroutine(DestroyFormationAfterDelay(formationObject));
        }

        private void FireRadialNeedles(Vector3 origin)
        {
            SyringeSpecialRuntime runtime = sourceNeedleAbility.GetCurrentSpecialRuntime();

            for (int i = 0; i < needleCount; i++)
            {
                float angle = i * 360f / needleCount;
                Vector2 direction = AngleToVector(angle);

                Projectile projectile = entityManager.SpawnProjectile(
                    projectilePoolIndex,
                    origin,
                    sourceNeedleAbility.GetEffectiveDamage() * damageMultiplier,
                    sourceNeedleAbility.GetEffectiveKnockback(),
                    sourceNeedleAbility.GetEffectiveSpeed(),
                    monsterLayer
                );

                if (projectile == null)
                {
                    continue;
                }

                projectile.transform.localScale =
                    Vector3.one * sourceCharacter.ProjectileSizeMultiplier * 0.85f;

                projectile.maxDistance *= sourceCharacter.RangeMultiplier;

                if (projectile is SyringeProjectile syringeProjectile)
                {
                    syringeProjectile.ConfigureSpecials(runtime);
                }

                projectile.OnHitDamageable.AddListener(sourceCharacter.OnDealDamage.Invoke);
                projectile.Launch(direction);
            }
        }

        private IEnumerator DestroyFormationAfterDelay(GameObject formationObject)
        {
            yield return new WaitForSeconds(formationLifetime);

            if (formationObject != null)
            {
                Destroy(formationObject);
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
    }
}