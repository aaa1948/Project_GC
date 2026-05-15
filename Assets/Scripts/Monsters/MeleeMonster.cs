using UnityEngine;

namespace Vampire
{
    public class MeleeMonster : Monster
    {
        protected new MeleeMonsterBlueprint monsterBlueprint;
        protected EliteMonsterBlueprint eliteMonsterBlueprint;

        protected float timeSinceLastAttack;

        public override void Setup(
            int monsterIndex,
            Vector2 position,
            MonsterBlueprint monsterBlueprint,
            float hpBuff = 0)
        {
            base.Setup(monsterIndex, position, monsterBlueprint, hpBuff);

            this.monsterBlueprint = monsterBlueprint as MeleeMonsterBlueprint;
            this.eliteMonsterBlueprint = monsterBlueprint as EliteMonsterBlueprint;

            if (this.monsterBlueprint == null)
            {
                Debug.LogError(
                    $"[MeleeMonster] 잘못된 Blueprint가 들어왔습니다. " +
                    $"MeleeMonster 프리팹에는 MeleeMonsterBlueprint 계열만 넣어야 합니다. " +
                    $"현재 Blueprint: {(monsterBlueprint != null ? monsterBlueprint.name : "NULL")} / " +
                    $"Type: {(monsterBlueprint != null ? monsterBlueprint.GetType().Name : "NULL")}",
                    this
                );
            }

            timeSinceLastAttack = 0f;
        }

        protected override void Update()
        {
            base.Update();

            timeSinceLastAttack += Time.deltaTime;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!alive)
            {
                return;
            }

            if (playerCharacter == null || rb == null || monsterBlueprint == null)
            {
                return;
            }

            Vector2 moveDirection =
                ((Vector2)playerCharacter.transform.position - (Vector2)transform.position).normalized;

            rb.velocity += moveDirection * GetEffectiveAcceleration() * Time.fixedDeltaTime;

            if (entityManager != null && entityManager.Grid != null)
            {
                entityManager.Grid.UpdateClient(this);
            }
        }

        private void OnCollisionStay2D(Collision2D col)
        {
            if (!alive)
            {
                return;
            }

            if (monsterBlueprint == null)
            {
                return;
            }

            if (playerCharacter == null)
            {
                return;
            }

            if (col == null || col.collider == null)
            {
                return;
            }

            bool isTargetLayer =
                (GetEffectiveMeleeLayer() & (1 << col.collider.gameObject.layer)) != 0;

            if (!isTargetLayer)
            {
                return;
            }

            float attackDelay = 1.0f / Mathf.Max(0.01f, GetEffectiveAttackSpeed());

            if (timeSinceLastAttack < attackDelay)
            {
                return;
            }

            playerCharacter.TakeDamage(GetEffectiveAttack());
            timeSinceLastAttack = Mathf.Repeat(timeSinceLastAttack, attackDelay);
        }

        private float GetEffectiveAcceleration()
        {
            if (eliteMonsterBlueprint != null)
            {
                return eliteMonsterBlueprint.GetEffectiveAcceleration();
            }

            return monsterBlueprint.acceleration;
        }

        private float GetEffectiveAttack()
        {
            if (eliteMonsterBlueprint != null)
            {
                return eliteMonsterBlueprint.GetEffectiveAttack();
            }

            return monsterBlueprint.atk;
        }

        private float GetEffectiveAttackSpeed()
        {
            if (eliteMonsterBlueprint != null)
            {
                return eliteMonsterBlueprint.GetEffectiveAttackSpeed();
            }

            return monsterBlueprint.atkspeed;
        }

        private LayerMask GetEffectiveMeleeLayer()
        {
            if (eliteMonsterBlueprint != null)
            {
                return eliteMonsterBlueprint.GetEffectiveMeleeLayer();
            }

            return monsterBlueprint.meleeLayer;
        }
    }
}