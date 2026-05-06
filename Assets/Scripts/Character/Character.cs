using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Vampire
{
    public class Character : IDamageable, ISpatialHashGridClient
    {
        [Header("Dependencies")]
        [SerializeField] protected Transform centerTransform;
        [SerializeField] protected Transform lookIndicator;
        [SerializeField] protected float lookIndicatorRadius;
        [SerializeField] protected TextMeshProUGUI levelText;
        [SerializeField] protected AbilitySelectionDialog abilitySelectionDialog;
        [SerializeField] protected PointBar healthBar;
        [SerializeField] protected PointBar expBar;
        [SerializeField] protected Collider2D collectableCollider;
        [SerializeField] protected Collider2D meleeHitboxCollider;
        [SerializeField] protected ParticleSystem dustParticles;
        [SerializeField] protected Material defaultMaterial, hitMaterial, deathMaterial;
        [SerializeField] protected ParticleSystem deathParticles;

        [Header("Character Data")]
        [SerializeField] protected CharacterBlueprint characterBlueprint;

        [Header("Runtime Stats")]
        [SerializeField] protected bool alive = true;
        [SerializeField] protected int currentLevel = 1;
        [SerializeField] protected float currentExp = 0;
        [SerializeField] protected float nextLevelExp = 5;
        [SerializeField] protected float expToNextLevel = 5;
        [SerializeField] protected float currentHealth;

        [Header("Upgradeable Systems")]
        [SerializeField] protected UpgradeableMovementSpeed movementSpeed;
        [SerializeField] protected UpgradeableArmor armor;

        [Header("Combat Stat Pockets")]
        [SerializeField] protected float attackSpeedMultiplier = 1.0f;
        [SerializeField] protected float damageMultiplier = 1.0f;
        [SerializeField] protected float maxHealthBonus = 0f;
        [SerializeField] protected float projectileSpeedMultiplier = 1.0f;

        [Header("Utility Stat Pockets")]
        [SerializeField] protected float magnetRangeBonus = 0f;
        [SerializeField] protected float expMultiplier = 1.0f;
        [SerializeField] protected float critChance = 0.05f;
        [SerializeField] protected float luckMultiplier = 1.0f;

        [Header("Special Ability Flags")]
        [SerializeField] protected int additionalProjectiles = 0;
        [SerializeField] protected bool hasShield = false;
        [SerializeField] protected int reviveCount = 0;
        [SerializeField] protected float invincibilityTimeBonus = 0f;

        protected SpriteRenderer spriteRenderer;
        protected SpriteAnimator spriteAnimator;
        protected AbilityManager abilityManager;
        protected EntityManager entityManager;
        protected StatsManager statsManager;
        protected Rigidbody2D rb;
        protected ZPositioner zPositioner;
        protected Vector2 lookDirection = Vector2.right;
        protected CoroutineQueue coroutineQueue;
        protected Coroutine hitAnimationCoroutine = null;
        protected Vector2 moveDirection;

        public Vector2 LookDirection
        {
            get
            {
                return lookDirection;
            }
            set
            {
                if (value != Vector2.zero)
                {
                    lookDirection = value;
                }
            }
        }

        public Transform CenterTransform => centerTransform;
        public Collider2D CollectableCollider => collectableCollider;

        public float Luck => characterBlueprint.luck * luckMultiplier;
        public int CurrentLevel => currentLevel;

        public float DamageMultiplier => damageMultiplier;
        public float AttackSpeedMultiplier => attackSpeedMultiplier;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public float CritChance => critChance;
        public int AdditionalProjectiles => additionalProjectiles;
        public float InvincibilityTimeBonus => invincibilityTimeBonus;

        public UnityEvent<float> OnDealDamage { get; } = new UnityEvent<float>();
        public UnityEvent OnDeath { get; } = new UnityEvent();

        public CharacterBlueprint Blueprint => characterBlueprint;
        public Vector2 Velocity => rb.velocity;

        // Spatial Hash Grid Client Interface
        public Vector2 Position => transform.position;
        public Vector2 Size => meleeHitboxCollider.bounds.size;
        public Dictionary<int, int> ListIndexByCellIndex { get; set; }
        public int QueryID { get; set; } = -1;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            zPositioner = gameObject.AddComponent<ZPositioner>();
            spriteAnimator = GetComponentInChildren<SpriteAnimator>();
            spriteRenderer = spriteAnimator.GetComponent<SpriteRenderer>();

            characterBlueprint = CrossSceneData.CharacterBlueprint;
        }

        public virtual void Init(EntityManager entityManager, AbilityManager abilityManager, StatsManager statsManager)
        {
            this.entityManager = entityManager;
            this.abilityManager = abilityManager;
            this.statsManager = statsManager;

            OnDealDamage.AddListener(statsManager.IncreaseDamageDealt);

            coroutineQueue = new CoroutineQueue(this);
            coroutineQueue.StartLoop();

            currentHealth = characterBlueprint.hp;
            healthBar.Setup(currentHealth, 0, GetMaxHealth());

            expBar.Setup(currentExp, 0, nextLevelExp);

            currentLevel = 1;
            UpdateLevelDisplay();

            spriteAnimator.Init(characterBlueprint.walkSpriteSequence, characterBlueprint.walkFrameTime, false);

            movementSpeed = new UpgradeableMovementSpeed();
            movementSpeed.Value = characterBlueprint.movespeed;
            abilityManager.RegisterUpgradeableValue(movementSpeed, true);
            UpdateMoveSpeed();

            armor = new UpgradeableArmor();
            armor.Value = characterBlueprint.armor;
            abilityManager.RegisterUpgradeableValue(armor, true);

            zPositioner.Init(transform);
        }

        protected virtual void Update()
        {
            lookIndicator.transform.localPosition = lookDirection * lookIndicatorRadius;
            spriteRenderer.flipX = lookDirection.x < 0;
        }

        protected virtual void FixedUpdate()
        {
            if (moveDirection != Vector2.zero)
            {
                lookDirection = moveDirection;
            }
            else
            {
                StopWalkAnimation();
            }

            if (alive)
            {
                rb.velocity += moveDirection * characterBlueprint.acceleration * Time.deltaTime;
            }
        }

        public void GainExp(float exp)
        {
            if (alive)
            {
                coroutineQueue.EnqueueCoroutine(GainExpCoroutine(exp * expMultiplier));
            }
        }

        private IEnumerator GainExpCoroutine(float exp)
        {
            if (!alive)
            {
                yield break;
            }

            while (currentExp + exp >= nextLevelExp)
            {
                float expDiff = nextLevelExp - currentExp;

                currentExp += expDiff;
                exp -= expDiff;

                expBar.Setup(currentExp, 0, nextLevelExp);

                yield return LevelUpCoroutine();

                float prevLevelExp = nextLevelExp;

                expToNextLevel += characterBlueprint.LevelToExpIncrease(currentLevel);
                nextLevelExp += expToNextLevel;

                expBar.Setup(currentExp, prevLevelExp, nextLevelExp);
            }

            currentExp += exp;
            expBar.AddPoints(exp);
        }

        private IEnumerator LevelUpCoroutine()
        {
            if (!alive)
            {
                yield break;
            }

            currentLevel++;
            UpdateLevelDisplay();

            abilitySelectionDialog.Open();

            while (abilitySelectionDialog.MenuOpen)
            {
                yield return null;
            }
        }

        private void UpdateLevelDisplay()
        {
            if (levelText != null)
            {
                levelText.text = "LV " + currentLevel;
            }
        }

        public override void Knockback(Vector2 knockback)
        {
            rb.velocity += knockback * Mathf.Sqrt(rb.drag);
        }

        public override void TakeDamage(float damage, Vector2 knockback = default(Vector2))
        {
            if (!alive)
            {
                return;
            }

            if (hasShield)
            {
                hasShield = false;
                return;
            }

            if (armor.Value >= damage)
            {
                damage = damage < 1 ? damage : 1;
            }
            else
            {
                damage -= armor.Value;
            }

            healthBar.SubtractPoints(damage);
            currentHealth -= damage;

            rb.velocity += knockback * Mathf.Sqrt(rb.drag);

            statsManager.IncreaseDamageTaken(damage);

            if (currentHealth <= 0)
            {
                if (reviveCount > 0)
                {
                    Revive();
                    return;
                }

                StartCoroutine(DeathAnimation());
            }
            else
            {
                if (hitAnimationCoroutine != null)
                {
                    StopCoroutine(hitAnimationCoroutine);
                }

                hitAnimationCoroutine = StartCoroutine(HitAnimation());
            }
        }

        private IEnumerator HitAnimation()
        {
            spriteRenderer.sharedMaterial = hitMaterial;

            yield return new WaitForSeconds(0.15f + invincibilityTimeBonus);

            spriteRenderer.sharedMaterial = defaultMaterial;
        }

        private IEnumerator DeathAnimation()
        {
            alive = false;

            spriteRenderer.sharedMaterial = deathMaterial;

            abilityManager.DestroyActiveAbilities();
            StopWalkAnimation();
            deathParticles.Play();

            float height = spriteRenderer.bounds.size.y;
            float t = 0;

            while (t < 1)
            {
                spriteRenderer.sharedMaterial = deathMaterial;
                deathParticles.transform.position = transform.position + Vector3.up * height * (1 - t);
                deathMaterial.SetFloat("_Wipe", t);

                t += Time.deltaTime;

                yield return null;
            }

            deathMaterial.SetFloat("_Wipe", 1.0f);

            yield return new WaitForSeconds(0.5f);

            OnDeath.Invoke();
            spriteRenderer.enabled = false;
        }

        private void Revive()
        {
            reviveCount--;

            currentHealth = GetMaxHealth() * 0.5f;
            healthBar.Setup(currentHealth, 0, GetMaxHealth());

            if (hitAnimationCoroutine != null)
            {
                StopCoroutine(hitAnimationCoroutine);
            }

            hitAnimationCoroutine = StartCoroutine(HitAnimation());
        }

        public void GainHealth(float health)
        {
            float maxHealth = GetMaxHealth();

            healthBar.AddPoints(health);
            currentHealth += health;

            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
        }

        public void SetLookDirecton(InputAction.CallbackContext context)
        {
            LookDirection = context.ReadValue<Vector2>();
        }

        public void UpdateMoveSpeed()
        {
            rb.drag = characterBlueprint.acceleration / (movementSpeed.Value * movementSpeed.Value);
        }

        public void Move(Vector2 moveDirection)
        {
            this.moveDirection = moveDirection;
        }

        public void StartWalkAnimation()
        {
            if (alive)
            {
                spriteAnimator.StartAnimating();
            }
        }

        public void StopWalkAnimation()
        {
            spriteAnimator.StopAnimating(true);
        }

        public void SetMoveDirection(InputAction.CallbackContext context)
        {
            moveDirection = context.action.ReadValue<Vector2>().normalized;
        }

        private float GetMaxHealth()
        {
            return characterBlueprint.hp + maxHealthBonus;
        }

        // =====================================================================
        // Lobby Carry Item / Shop Item 효과 적용용 공식 메서드
        // =====================================================================

        public void AddAttackSpeed(float amount)
        {
            attackSpeedMultiplier += amount;
        }

        public void AddDamageMultiplier(float amount)
        {
            damageMultiplier += amount;
        }

        public void AddProjectileSpeed(float amount)
        {
            projectileSpeedMultiplier += amount;
        }

        public void AddMaxHealthBonus(float amount)
        {
            maxHealthBonus += amount;

            float maxHealth = GetMaxHealth();

            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }

            healthBar.Setup(currentHealth, 0, maxHealth);
        }

        public void AddMoveSpeedBoost(float amount)
        {
            if (movementSpeed == null)
            {
                return;
            }

            movementSpeed.Value += amount;
            UpdateMoveSpeed();
        }

        public void AddMagnetRange(float amount)
        {
            magnetRangeBonus += amount;
        }

        public void AddExpMultiplier(float amount)
        {
            expMultiplier += amount;
        }

        public void AddCritChance(float amount)
        {
            critChance += amount;
        }

        public void AddLuck(float amount)
        {
            luckMultiplier += amount;
        }

        public void AddProjectileCount(int count)
        {
            additionalProjectiles += count;
        }

        public void EnableShield()
        {
            hasShield = true;
        }

        public void AddReviveCount(int count)
        {
            reviveCount += count;
        }

        public void AddInvincibilityTime(float amount)
        {
            invincibilityTimeBonus += amount;
        }
    }
}