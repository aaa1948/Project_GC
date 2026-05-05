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
            //  적용: 최종 데미지 = 기본 데미지 * 공격력 배율
            float totalDamage = damage.Value * playerCharacter.DamageMultiplier;

            Projectile projectile = entityManager.SpawnProjectile(projectileIndex, playerCharacter.CenterTransform.position, totalDamage, knockback.Value, speed.Value, monsterLayer);
            projectile.OnHitDamageable.AddListener(playerCharacter.OnDealDamage.Invoke);
            projectile.Launch(playerCharacter.LookDirection);
        }
    }
}