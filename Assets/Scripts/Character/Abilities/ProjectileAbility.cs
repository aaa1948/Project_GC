using System.Collections;
using UnityEngine;

namespace Vampire
{
    public class ProjectileAbility : Ability
    {
        [Header("Projectile Stats")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected LayerMask monsterLayer;
        [SerializeField] protected UpgradeableDamage damage;
        [SerializeField] protected UpgradeableProjectileSpeed speed;
        [SerializeField] protected UpgradeableKnockback knockback;
        [SerializeField] protected UpgradeableWeaponCooldown cooldown;
        protected float timeSinceLastAttack;
        protected int projectileIndex;

        protected override void Use()
        {
            base.Use();
            gameObject.SetActive(true);
            timeSinceLastAttack = cooldown.Value;
            projectileIndex = entityManager.AddPoolForProjectile(projectilePrefab);
        }

        protected virtual void Update()
        {
            timeSinceLastAttack += Time.deltaTime;

            //  적용: 실제 쿨타임 = 기본 쿨타임 / 공격 속도 배율
            float effectiveCooldown = cooldown.Value / playerCharacter.AttackSpeedMultiplier;

            if (timeSinceLastAttack >= effectiveCooldown)
            {
                timeSinceLastAttack = Mathf.Repeat(timeSinceLastAttack, effectiveCooldown);
                Attack();
            }
        }

        protected virtual void Attack()
        {
            LaunchProjectile();
        }

        protected virtual void LaunchProjectile()
        {
            // 1. 데이터 확인 로그 (이게 찍혀야 합니다!)
            int extra = playerCharacter.AdditionalProjectiles;
            int total = 1 + extra;
            Debug.Log($"<color=orange>[LaunchProjectile 호출]</color> 총 {total}발 부채꼴 발사 시도");

            float totalDamage = damage.Value * playerCharacter.DamageMultiplier;
            float spreadAngle = 30f;

            for (int i = 0; i < total; i++)
            {
                float offsetAngle = (i - (total - 1) / 2f) * spreadAngle;
                Quaternion rotation = Quaternion.Euler(0, 0, offsetAngle);
                Vector2 shotDirection = rotation * playerCharacter.LookDirection;

                Projectile projectile = entityManager.SpawnProjectile(
                    projectileIndex,
                    playerCharacter.CenterTransform.position,
                    totalDamage,
                    knockback.Value,
                    speed.Value,
                    monsterLayer
                );

                //  이 로그가 콘솔에 찍히는지 꼭 확인해주세요!
                Debug.Log($"<color=cyan>[생성 완료]</color> {i + 1}번째 발사체 ID: {projectile.gameObject.GetInstanceID()} | 각도: {offsetAngle}");

                projectile.OnHitDamageable.AddListener(playerCharacter.OnDealDamage.Invoke);
                projectile.Launch(shotDirection);
            }
        }
    }
}
