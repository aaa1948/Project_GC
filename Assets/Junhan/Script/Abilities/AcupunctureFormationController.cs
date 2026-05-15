using UnityEngine;

namespace Vampire
{
    // 특수 증강: 침술진
    //
    // 수정 내용:
    // - 대쉬 시작 위치에 분신 이미지를 생성하지 않는다.
    // - 대쉬 시작 위치에서 360도 원형으로 침만 발사한다.
    // - 아이템/일반 증강/전설 증강으로 증가한 발사체 수를 침술진 발사 수에도 반영한다.
    public class AcupunctureFormationController : MonoBehaviour
    {
        private Character sourceCharacter;
        private EntityManager entityManager;
        private SyringeDartAbility sourceNeedleAbility;

        private GameObject projectilePrefab;
        private LayerMask monsterLayer;
        private int projectilePoolIndex;

        private int baseNeedleCount;
        private float damageMultiplier;

        private bool previousIsDashing = false;
        private Vector3 previousPlayerPosition;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

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
            AcupunctureFormationController controller =
                controllerObject.AddComponent<AcupunctureFormationController>();

            controller.Init(
                sourceCharacter,
                entityManager,
                sourceNeedleAbility,
                needleCount,
                damageMultiplier
            );

            return controller;
        }

        private void Init(
            Character sourceCharacter,
            EntityManager entityManager,
            SyringeDartAbility sourceNeedleAbility,
            int needleCount,
            float damageMultiplier)
        {
            this.sourceCharacter = sourceCharacter;
            this.entityManager = entityManager;
            this.sourceNeedleAbility = sourceNeedleAbility;

            baseNeedleCount = Mathf.Max(1, needleCount);
            this.damageMultiplier = Mathf.Max(0.01f, damageMultiplier);

            projectilePrefab = sourceNeedleAbility.ProjectilePrefab;
            monsterLayer = sourceNeedleAbility.MonsterLayer;
            projectilePoolIndex = entityManager.AddPoolForProjectile(projectilePrefab);

            if (sourceCharacter != null)
            {
                previousPlayerPosition = sourceCharacter.CenterTransform.position;
                previousIsDashing = sourceCharacter.IsDashing;

                if (sourceCharacter.OnDeath != null)
                {
                    sourceCharacter.OnDeath.AddListener(DestroySelf);
                }
            }

            if (debugLog)
            {
                Debug.Log("[침술진] 컨트롤러 생성 완료 - 분신 이미지 없이 원형 발사만 사용");
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
                FireRadialNeedles(previousPlayerPosition);
            }

            previousIsDashing = currentIsDashing;
            previousPlayerPosition = sourceCharacter.CenterTransform.position;
        }

        private void FireRadialNeedles(Vector3 origin)
        {
            SyringeSpecialRuntime runtime = sourceNeedleAbility.GetCurrentSpecialRuntime();

            int finalNeedleCount = GetFinalNeedleCount();

            if (debugLog)
            {
                Debug.Log($"[침술진] 원형 침 발사 | 발사 수 {finalNeedleCount}");
            }

            for (int i = 0; i < finalNeedleCount; i++)
            {
                float angle = i * 360f / finalNeedleCount;
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

        private int GetFinalNeedleCount()
        {
            int finalCount = baseNeedleCount;

            if (sourceNeedleAbility != null)
            {
                // 기본 발사체 수 1개는 침술진의 기본 needleCount에 이미 포함되어 있다고 보고,
                // 추가 발사체 증가분만 침술진 발사 수에 더한다.
                int projectileBonus = Mathf.Max(0, sourceNeedleAbility.GetEffectiveProjectileCount() - 1);
                finalCount += projectileBonus;
            }

            return Mathf.Max(1, finalCount);
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