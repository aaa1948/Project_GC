using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

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
        [SerializeField] protected PointBar healthBar;  // 혈량조
        [SerializeField] protected PointBar expBar;     // 경험조
        [SerializeField] protected Collider2D collectableCollider;
        [SerializeField] protected Collider2D meleeHitboxCollider;
        [SerializeField] protected ParticleSystem dustParticles;
        [SerializeField] protected Material defaultMaterial, hitMaterial, deathMaterial;
        [SerializeField] protected ParticleSystem deathParticles;

        [Header("Character Data")]
        [SerializeField] protected CharacterBlueprint characterBlueprint;

        [Header("Runtime Stats (Monitoring)")]
        [SerializeField] protected bool alive = true;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected int currentLevel = 1;
        [SerializeField] protected float currentExp = 0;

        [Header("Progression Details")]
        [SerializeField] protected float nextLevelExp = 5;
        [SerializeField] protected float expToNextLevel = 5;

        [Header("Upgradeable Systems")]
        [SerializeField] protected UpgradeableMovementSpeed movementSpeed;
        [SerializeField] protected UpgradeableArmor armor;

        // 💡 1순위: 전투 스탯 주머니
        [Header("Combat Stat Pockets (Item Effects)")]
        [SerializeField] protected float attackSpeedMultiplier = 1.0f; // 비타민 C
        [SerializeField] protected float damageMultiplier = 1.0f;      // 단백질 쉐이크
        [SerializeField] protected float maxHealthBonus = 0f;          // 칼슘 우유
        [SerializeField] protected float projectileSpeedMultiplier = 1.0f; // 인공눈물

        // 💡 2순위: 유틸리티 스탯 주머니
        [Header("Utility Stat Pockets")]
        [SerializeField] protected float magnetRangeBonus = 0f;    // 자석 파스
        [SerializeField] protected float expMultiplier = 1.0f;     // 오메가-3
        [SerializeField] protected float critChance = 0.05f;       // 물파스 (기본 5%)
        [SerializeField] protected float luckMultiplier = 1.0f;    // 운 증가

        // 💡 3, 4순위: 특수 기능 플래그 (Rare/Legendary)
        [Header("Special Ability Flags")]
        [SerializeField] protected int additionalProjectiles = 0;    // 일회용 주사기 (투사체+1)
        [SerializeField] protected bool hasShield = false;            // KF94 마스크 (보호막)
        [SerializeField] protected int reviveCount = 0;               // 줄기세포 (부활)
        [SerializeField] protected float invincibilityTimeBonus = 0f; // 일회용 밴드 (무적시간)

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

        // --- 💡 무기/외부 스크립트를 위한 정보 공개 통로 (Getters) ---
        public float DamageMultiplier => damageMultiplier;
        public float AttackSpeedMultiplier => attackSpeedMultiplier;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public float CritChance => critChance;
        public int AdditionalProjectiles => additionalProjectiles;
        public float InvincibilityTimeBonus => invincibilityTimeBonus;
        // -------------------------------------------------------------

        public Vector2 LookDirection
        {
            get { return lookDirection; }
            set { if (value != Vector2.zero) lookDirection = value; }
        }
        public Transform CenterTransform { get => centerTransform; }
        public Collider2D CollectableCollider { get => collectableCollider; }
        public float Luck { get => characterBlueprint.luck * luckMultiplier; } // 💡 운 배율 적용
        public int CurrentLevel { get => currentLevel; }
        public UnityEvent<float> OnDealDamage { get; } = new UnityEvent<float>();
        public UnityEvent OnDeath { get; } = new UnityEvent();
        public CharacterBlueprint Blueprint { get => characterBlueprint; }
        public Vector2 Velocity { get => rb.velocity; }

        // Spatial Hash Grid Client Interface
        public Vector2 Position => transform.position;
        public Vector2 Size => meleeHitboxCollider.bounds.size;
        public Dictionary<int, int> ListIndexByCellIndex { get; set; }
        public int QueryID { get; set; } = -1;

        void Awake()
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
            healthBar.Setup(currentHealth, 0, characterBlueprint.hp);
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
                lookDirection = moveDirection;
            else
                StopWalkAnimation();

            if (alive)
                rb.velocity += moveDirection * characterBlueprint.acceleration * Time.deltaTime;
        }

        public void GainExp(float exp)
        {
            if (alive)
                coroutineQueue.EnqueueCoroutine(GainExpCoroutine(exp * expMultiplier)); // 💡 경험치 배율 적용
        }

        private IEnumerator GainExpCoroutine(float exp)
        {
            if (alive)
            {
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
        }

        private IEnumerator LevelUpCoroutine()
        {
            if (alive)
            {
                currentLevel++;
                UpdateLevelDisplay();
                abilitySelectionDialog.Open();
                while (abilitySelectionDialog.MenuOpen)
                {
                    yield return null;
                }
            }
        }

        private void UpdateLevelDisplay()
        {
            levelText.text = "LV " + currentLevel;
        }

        public override void Knockback(Vector2 knockback)
        {
            rb.velocity += knockback * Mathf.Sqrt(rb.drag);
        }

        public override void TakeDamage(float damage, Vector2 knockback = default(Vector2))
        {
            if (alive)
            {
                if (armor.Value >= damage)
                    damage = damage < 1 ? damage : 1;
                else
                    damage -= armor.Value;

                healthBar.SubtractPoints(damage);
                currentHealth -= damage;
                rb.velocity += knockback * Mathf.Sqrt(rb.drag);
                statsManager.IncreaseDamageTaken(damage);

                if (currentHealth <= 0)
                {
                    StartCoroutine(DeathAnimation());
                }
                else
                {
                    if (hitAnimationCoroutine != null) StopCoroutine(hitAnimationCoroutine);
                    hitAnimationCoroutine = StartCoroutine(HitAnimation());
                }
            }
        }

        private IEnumerator HitAnimation()
        {
            spriteRenderer.sharedMaterial = hitMaterial;
            yield return new WaitForSeconds(0.15f);
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

        public void GainHealth(float health)
        {
            float totalMaxHp = characterBlueprint.hp + maxHealthBonus; // 보너스 포함 계산
            healthBar.AddPoints(health);
            currentHealth += health;

            if (currentHealth > totalMaxHp)
                currentHealth = totalMaxHp;
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
                spriteAnimator.StartAnimating();
        }

        public void StopWalkAnimation()
        {
            spriteAnimator.StopAnimating(true);
        }

        public void SetMoveDirection(InputAction.CallbackContext context)
        {
            moveDirection = context.action.ReadValue<Vector2>().normalized;
        }

        // =====================================================================
        //  배달부(ShopStatApplier)를 위한 공식 데이터 입구들
        // =====================================================================

        // 1. 전투 스탯 (Common)
        public void AddAttackSpeed(float amount) => attackSpeedMultiplier += amount;
        public void AddDamageMultiplier(float amount) => damageMultiplier += amount;
        public void AddProjectileSpeed(float amount) => projectileSpeedMultiplier += amount;
        public void AddMaxHealthBonus(float amount)
        {
            maxHealthBonus += amount;
            healthBar.Setup(currentHealth, 0, characterBlueprint.hp + maxHealthBonus);
        }
        public void AddMoveSpeedBoost(float amount)
        {
            movementSpeed.Value += amount;
            UpdateMoveSpeed();
        }

        // 2. 유틸리티 스탯 (Uncommon)
        public void AddMagnetRange(float amount) => magnetRangeBonus += amount;
        public void AddExpMultiplier(float amount) => expMultiplier += amount;
        public void AddCritChance(float amount) => critChance += amount;
        public void AddLuck(float amount) => luckMultiplier += amount;

        // 3. 특수 능력 플래그 (Rare / Legendary)
        public void AddProjectileCount(int count) => additionalProjectiles += count;
        public void EnableShield() => hasShield = true;
        public void AddReviveCount(int count) => reviveCount += count;
        public void AddInvincibilityTime(float amount) => invincibilityTimeBonus += amount;
    }
}
