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
        [SerializeField] protected TextMeshProUGUI thermometerText;

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
        [SerializeField] protected float rangeMultiplier = 1.0f; // 기본 1배

        [Header("Combat Stat Pockets")]
        [SerializeField] protected float attackSpeedMultiplier = 1.0f;
        [SerializeField] protected float damageMultiplier = 1.0f;
        [SerializeField] protected float maxHealthBonus = 0f;
        [SerializeField] protected float projectileSpeedMultiplier = 1.0f;

        [Header("Utility Stat Pockets")]
        [SerializeField] protected float magnetRangeBonus = 0f;
        [SerializeField] protected float expMultiplier = 1.0f;
        [SerializeField] protected float critChance = 0.03f;
        [SerializeField] protected float luckMultiplier = 1.0f;
        [SerializeField] protected float lifeSteal = 0f;
        [SerializeField] protected float healOnKill = 0f;
        [SerializeField] protected float projectileSizeMultiplier = 1.0f; // 기본 크기 1배


        [Header("Special Ability Flags")]
        [SerializeField] protected int additionalProjectiles = 0;
        [SerializeField] protected bool hasShield = false;
        [SerializeField] protected int reviveCount = 0;
        [SerializeField] protected float invincibilityTimeBonus = 0f;
        [SerializeField] private bool hasAntibioticBomb = false; //  항생제 폭탄 스위치

        [Header("Dash Settings")]
        [SerializeField] protected bool enableDash = true;
        [SerializeField] protected float dashDistance = 3.5f;
        [SerializeField] protected float dashDuration = 0.14f;
        [SerializeField] protected float dashRechargeTime = 1.2f;
        [SerializeField] protected int maxDashCharges = 1;
        [SerializeField] protected bool invincibleDuringDash = true;
        [SerializeField] protected bool stopVelocityAfterDash = true;

        [Header("Dash Collision")]
        [Tooltip("체크하면 대쉬 중 플레이어의 일반 충돌 콜라이더를 Trigger로 바꿔 몬스터를 밀지 않고 관통합니다.")]
        [SerializeField] protected bool passThroughCollidersDuringDash = true;

        [Tooltip("기존 Trigger 콜라이더까지 같이 처리할지 여부입니다. 보통은 꺼두는 것을 추천합니다.")]
        [SerializeField] protected bool includeTriggerCollidersInDashGhost = false;

        [Header("Dash Debug")]
        [SerializeField] protected bool debugDashLog = false;

        [Header("Recovery Stats")]
        [SerializeField] protected float healOnIdlePerSecond = 0f; // 정지 시 초당 회복량
        
        [Header("Legendary Utility Stats")]
        [SerializeField] protected bool autoCollectItems = false; // 기본값은 꺼짐

        [Header("Debuff Stats")]
        [SerializeField] private float slowChance = 0f; // 기본 둔화 확률 0% (0.0f)

        [Header("Utility Combat Stats")]
        [SerializeField] private int additionalPierce = 0; // 기본 추가 관통 0회

        [SerializeField] private float burnChance = 0f; // 기본 화상 확률 0%

        [Header("Rare Item Flags")]
        [SerializeField] private bool hasGinsengStick = false;
        [SerializeField] private float antibioticBombChance = 0f;
        [SerializeField] private bool hasThermometer = false; // 전자체온계
        [SerializeField] private int mouthwashCount = 0;//구강청결제
        [SerializeField] private bool hasReflexHammer = false;

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

        protected bool isDashing = false;
        protected bool isInvincible = false;
        protected int currentDashCharges = 1;
        protected Coroutine dashCoroutine = null;
        protected Coroutine dashRechargeCoroutine = null;

        private readonly List<Collider2D> dashGhostedColliders = new List<Collider2D>();
        private readonly Dictionary<Collider2D, bool> originalTriggerStateByCollider = new Dictionary<Collider2D, bool>();

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

        public bool HasThermometer => hasThermometer;
       //전자체온계 공속 스택
        private int thermometerStacks = 0;
        private float thermometerStackTimer = 0f;
        private float thermometerMoveAccumulator = 0f;

        public float AttackSpeedMultiplier
        {
            get
            {
                float finalSpeed = attackSpeedMultiplier;

                // 전자체온계를 보유하고 있다면 스택당 공격 속도 증가!
                if (hasThermometer)
                {
                    finalSpeed += (thermometerStacks * 0.03f);; //  스택당 공속 +3% (기획에 맞게 조절 가능)
                }

                return finalSpeed;
            }
        }
        //까지 공속스택
        public int MouthwashCount => mouthwashCount;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public float BurnChance => burnChance;
        public float CritChance => critChance;
        public int AdditionalProjectiles => additionalProjectiles;
        public float InvincibilityTimeBonus => invincibilityTimeBonus;
        public float LifeSteal => lifeSteal;
        public float HealOnKill => healOnKill;
        public float ProjectileSizeMultiplier => projectileSizeMultiplier;
        public float RangeMultiplier => rangeMultiplier;
        public int CurrentDashCharges => currentDashCharges;
        public int MaxDashCharges => maxDashCharges;
        public bool IsDashing => isDashing;
        public float AntibioticBombChance => antibioticBombChance;
        public bool AutoCollectItems => autoCollectItems;
        public UnityEvent<float> OnDealDamage { get; } = new UnityEvent<float>();
        public UnityEvent OnDeath { get; } = new UnityEvent();
        public float SlowChance => slowChance;

        public int AdditionalPierce => additionalPierce;
        public CharacterBlueprint Blueprint => characterBlueprint;
        public Vector2 Velocity => rb != null ? rb.velocity : Vector2.zero;

        public bool HasAntibioticBomb => hasAntibioticBomb;

        
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

        private void OnDisable()
        {
            RestoreDashCollisionGhost();
        }

        public virtual void Init(EntityManager entityManager, AbilityManager abilityManager, StatsManager statsManager)
        {
            this.entityManager = entityManager;
            this.abilityManager = abilityManager;
            this.statsManager = statsManager;

            OnDealDamage.AddListener(statsManager.IncreaseDamageDealt);

            // 2.  [추가된 진짜 흡혈 로직!] 데미지를 줄 때마다 흡혈 수치만큼 체력 회복
            OnDealDamage.AddListener((damage) =>
            {
                if (lifeSteal > 0)
                {
                    GainHealth(damage * lifeSteal);
                }
            });

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

            InitDash();
            UpdateThermometerDisplay();
        }

        protected virtual void Update()
        {
            UpdateDashInput();

            //  [추가] 정지 시 회복 로직 호출
            HandleHealOnIdle();

            HandleThermometerMovementAndDecay();//체온계 지속시간 실시간 감시

            if (lookIndicator != null)
            {
                lookIndicator.transform.localPosition = lookDirection * lookIndicatorRadius;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = lookDirection.x < 0;
            }
        }

        private void HandleHealOnIdle()
        {
            // 회복량이 설정되어 있고, 캐릭터가 멈춰있을 때 (속도가 거의 0일 때)
            if (healOnIdlePerSecond > 0 && Velocity.magnitude < 0.1f)
            {
                //  고정 수치 대신 최대 체력(GetMaxHealth())의 퍼센트 비율로 회복하게 만듭니다!
                float healAmount = GetMaxHealth() * healOnIdlePerSecond;
                GainHealth(healAmount * Time.deltaTime);
            }
        }


        protected virtual void FixedUpdate()
        {
            if (isDashing)
            {
                return;
            }

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

        private void InitDash()
        {
            maxDashCharges = Mathf.Max(1, maxDashCharges);
            currentDashCharges = maxDashCharges;
        }

        private void UpdateDashInput()
        {
            if (!CanReadDashInput())
            {
                return;
            }

            bool dashPressed =
                Keyboard.current.leftShiftKey.wasPressedThisFrame ||
                Keyboard.current.rightShiftKey.wasPressedThisFrame;

            if (dashPressed)
            {
                TryDash();
            }
        }

        private bool CanReadDashInput()
        {
            if (!enableDash)
            {
                return false;
            }

            if (!alive)
            {
                return false;
            }

            if (Keyboard.current == null)
            {
                return false;
            }

            if (abilitySelectionDialog != null && abilitySelectionDialog.MenuOpen)
            {
                return false;
            }

            return true;
        }

        public bool TryDash()
        {
            if (!enableDash)
            {
                return false;
            }

            if (!alive)
            {
                return false;
            }

            if (isDashing)
            {
                return false;
            }

            if (currentDashCharges <= 0)
            {
                if (debugDashLog)
                {
                    Debug.Log("[Dash] 사용 가능한 대쉬 횟수가 없습니다.");
                }

                return false;
            }

            Vector2 dashDirection = GetDashDirection();

            if (dashDirection == Vector2.zero)
            {
                return false;
            }

            currentDashCharges--;

            if (dashCoroutine != null)
            {
                StopCoroutine(dashCoroutine);
            }

            dashCoroutine = StartCoroutine(DashCoroutine(dashDirection));

            if (dashRechargeCoroutine == null)
            {
                dashRechargeCoroutine = StartCoroutine(DashRechargeCoroutine());
            }

            if (debugDashLog)
            {
                Debug.Log($"[Dash] 대쉬 사용 | 남은 대쉬 = {currentDashCharges}/{maxDashCharges}");
            }

            return true;
        }

        private Vector2 GetDashDirection()
        {
            if (moveDirection != Vector2.zero)
            {
                return moveDirection.normalized;
            }

            if (lookDirection != Vector2.zero)
            {
                return lookDirection.normalized;
            }

            return Vector2.right;
        }

        private IEnumerator DashCoroutine(Vector2 dashDirection)
        {
            isDashing = true;
            ApplyDashCollisionGhost();

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            StartWalkAnimation();

            Vector2 startPosition = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 targetPosition = startPosition + dashDirection.normalized * dashDistance;

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, dashDuration);

            while (elapsed < safeDuration)
            {
                float t = elapsed / safeDuration;
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                Vector2 nextPosition = Vector2.Lerp(startPosition, targetPosition, easedT);

                if (rb != null)
                {
                    rb.MovePosition(nextPosition);
                }
                else
                {
                    transform.position = nextPosition;
                }

                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            if (rb != null)
            {
                rb.MovePosition(targetPosition);

                if (stopVelocityAfterDash)
                {
                    rb.velocity = Vector2.zero;
                }
            }
            else
            {
                transform.position = targetPosition;
            }

            yield return new WaitForFixedUpdate();

            RestoreDashCollisionGhost();

            isDashing = false;
            dashCoroutine = null;

            if (debugDashLog)
            {
                Debug.Log("[Dash] 대쉬 종료");
            }
        }

        private void ApplyDashCollisionGhost()
        {
            if (!passThroughCollidersDuringDash)
            {
                return;
            }

            RestoreDashCollisionGhost();

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D targetCollider = colliders[i];

                if (targetCollider == null)
                {
                    continue;
                }

                if (!targetCollider.enabled)
                {
                    continue;
                }

                if (targetCollider.isTrigger && !includeTriggerCollidersInDashGhost)
                {
                    continue;
                }

                originalTriggerStateByCollider[targetCollider] = targetCollider.isTrigger;
                dashGhostedColliders.Add(targetCollider);

                targetCollider.isTrigger = true;
            }

            if (debugDashLog)
            {
                Debug.Log($"[Dash] 충돌 관통 활성화 | 대상 콜라이더 수 = {dashGhostedColliders.Count}");
            }
        }

        private void RestoreDashCollisionGhost()
        {
            if (dashGhostedColliders.Count <= 0)
            {
                originalTriggerStateByCollider.Clear();
                return;
            }

            for (int i = 0; i < dashGhostedColliders.Count; i++)
            {
                Collider2D targetCollider = dashGhostedColliders[i];

                if (targetCollider == null)
                {
                    continue;
                }

                if (originalTriggerStateByCollider.TryGetValue(targetCollider, out bool originalIsTrigger))
                {
                    targetCollider.isTrigger = originalIsTrigger;
                }
            }

            dashGhostedColliders.Clear();
            originalTriggerStateByCollider.Clear();

            if (debugDashLog)
            {
                Debug.Log("[Dash] 충돌 관통 해제");
            }
        }

        private IEnumerator DashRechargeCoroutine()
        {
            while (currentDashCharges < maxDashCharges)
            {
                yield return new WaitForSeconds(dashRechargeTime);

                if (!alive)
                {
                    dashRechargeCoroutine = null;
                    yield break;
                }

                currentDashCharges = Mathf.Min(currentDashCharges + 1, maxDashCharges);

                if (debugDashLog)
                {
                    Debug.Log($"[Dash] 대쉬 충전 | 현재 대쉬 = {currentDashCharges}/{maxDashCharges}");
                }
            }

            dashRechargeCoroutine = null;
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
            if (isDashing)
            {
                return;
            }

            rb.velocity += knockback * Mathf.Sqrt(rb.drag);
        }

        public override void TakeDamage(float damage, Vector2 knockback = default(Vector2))
        {
            if (!alive)
            {
                return;
            }

            if (isDashing && invincibleDuringDash)
            {
                if (debugDashLog)
                {
                    Debug.Log("[Dash] 대쉬 무적으로 피해 무시");
                }

                return;
            }

            if (isInvincible)
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

            // ---------------------------------------------------------------------------------
            //  [추가] 반사신경 망치 로직: 아픈 데미지가 들어왔을 때 주변 적들을 사방으로 날려버립니다!
            // ---------------------------------------------------------------------------------
            if (hasReflexHammer && damage > 0f && UnityEngine.Random.value < 0.005f) // 💡 0.5% 확률 (1.0 기준 0.005)
            {
                TriggerReflexHammer();
            }

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
            isInvincible = true;

            spriteRenderer.sharedMaterial = hitMaterial;

            yield return new WaitForSeconds(0.15f + invincibilityTimeBonus);

            spriteRenderer.sharedMaterial = defaultMaterial;

            isInvincible = false;
        }

        private IEnumerator DeathAnimation()
        {
            alive = false;
            RestoreDashCollisionGhost();

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
        public void AddHealOnIdle(float amount)
        {
            healOnIdlePerSecond += amount;
        }

        public void AddBurnChance(float amount)
        {
            burnChance += amount;
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
            if (spriteAnimator != null)
            {
                spriteAnimator.StopAnimating(true);
            }
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

        
        public float DamageMultiplier
        {
            get
            {
                // 원래 적용되던 기본 공격력 배율 변수
                float finalMultiplier = damageMultiplier;

                
                if (hasGinsengStick && (currentHealth / GetMaxHealth()) <= 0.3f)
                {
                    finalMultiplier += 0.5f; // 공격력 +50% 증가
                }

                return finalMultiplier;
            }
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
        public void EnableThermometer()
        {
            hasThermometer = true;
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
        public void AddRangeBoost(float amount)
        {
            rangeMultiplier += amount;
        }

        public void AddMagnetRange(float amount)
        {
            magnetRangeBonus += amount;
        }

        public void AddExpMultiplier(float amount)
        {
            expMultiplier += amount;
        }
        public void AddAdditionalPierce(int amount)
        {
            additionalPierce += amount;
        }
        // 인공눈물과 치실이 사거리를 늘릴 때 사용할 함수입니다.
        public void AddRangeMultiplier(float amount)
        {
           
            rangeMultiplier += amount;
        }
        public void AddCritChance(float amount)
        {
            critChance += amount;
        }
        // 주사기가 적을 맞힐 때마다 플레이어의 체온(공속 스택)을 올립니다.
        public void AddThermometerStack()
        {
            if (!hasThermometer) return;

            if (thermometerStacks < 10) //  최대 10스택 제한 (최대 공속 +50%)
            {
                thermometerStacks++;
                Debug.Log($"<color=cyan>[체온계 스택]</color> 적중 완료! 현재 스택: <b>{thermometerStacks}</b> (공속 +{thermometerStacks * 5}%)");
            }

            thermometerStackTimer = 3.0f; //  3초 동안 공격 안 하면 스택 초기화 (지속 시간)
        }

        //  매 프레임마다 타이머를 깎아서 스택을 식히는 최적화 함수
        private void HandleThermometerMovementAndDecay()
        {
            if (!hasThermometer) return;

            //  [상황 A] 플레이어가 이동 중일 때 (예열)
            if (moveDirection != Vector2.zero)
            {
                thermometerStackTimer = 0f;
                thermometerMoveAccumulator += Time.deltaTime;

                // 0.25초 연속 이동 시 1스택 충전
                if (thermometerMoveAccumulator >= 0.25f)
                {
                    thermometerMoveAccumulator = 0f;
                    if (thermometerStacks < 10)
                    {
                        thermometerStacks++;

                        //  스택이 올라갔으니 화면 UI를 갱신합니다.
                        UpdateThermometerDisplay(); //  [UI 갱신 추가]

                        Debug.Log($"<color=cyan>[체온계 예열]</color> 이동 중! 스택: <b>{thermometerStacks}</b> (공속 +{thermometerStacks * 3}%)");
                    }
                }
            }
            //  [상황 B] 플레이어가 제자리에 멈췄을 때 (즉시 실시간 냉각)
            else
            {
                thermometerMoveAccumulator = 0f;

                if (thermometerStacks > 0)
                {
                    thermometerStackTimer -= Time.deltaTime;

                    if (thermometerStackTimer <= 0f)
                    {
                        thermometerStacks--;

                        // 2️⃣ [여기에 추가!] 스택이 깎였으니 화면 UI를 실시간으로 갱신합니다.
                        UpdateThermometerDisplay(); //  [UI 갱신 추가]

                        thermometerStackTimer = 0.15f;

                        if (thermometerStacks == 0)
                        {
                            Debug.Log("<color=gray>[체온계 냉각]</color> 완전히 냉각되었습니다. 공속 초기화.");
                        }
                        else
                        {
                            Debug.Log($"<color=gray>[체온계 냉각]</color> 정지 상태! 체온 저하 중... 남은 스택: <b>{thermometerStacks}</b> (공속 +{thermometerStacks * 3}%)");
                        }
                    }
                }
            }
        }

        private void UpdateThermometerDisplay()
        {
            if (thermometerText == null) return;

            // 체온계가 없거나 0스택(정상 상태)이면 화면에서 깔끔하게 숨김
            if (!hasThermometer || thermometerStacks == 0)
            {
                thermometerText.text = "";
                return;
            }

            //  1. 데이터 실시간 계산
            float displayTemp = 36.5f + thermometerStacks;            // 36.5도에서 스택당 1도씩 상승
            int bonusAttackSpeed = thermometerStacks * 3;              // [레버 2]: 스택당 공속 3% 증가
            string maxLabel = thermometerStacks >= 10 ? " <color=red>[MAX]</color>" : "";

            //  2. 상현님이 요청하신 콤팩트하고 화려한 형식으로 문자열 조합!
            // 출력 예시 ➔ 🌡️ 온도 스택: 5 (41.5°C | 공속 +15%)
            thermometerText.text = $"<color=orange>🌡️ 온도 스택: {thermometerStacks}</color> ({displayTemp:0.0}°C | 공속 +{bonusAttackSpeed}%){maxLabel}";
        }

        // 피격 시 주변 적들을 탐색해 강력한 물리 넉백을 선사하는 함수
        private void TriggerReflexHammer()
        {
            float pushRadius = 3.5f;     //  넉백 충격파 반경
            float strongForce = 15.0f;   //  적들을 날려버릴 강력한 힘

            // 내 주변 반경 내의 모든 콜라이더 탐색
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pushRadius);

            foreach (Collider2D hit in hits)
            {
                // 몬스터 컴포넌트 추출
                Monster monster = hit.GetComponentInParent<Monster>();
                if (monster == null) continue;

                IDamageable damageable = monster as IDamageable;
                if (damageable != null)
                {
                    // 내 중심에서 몬스터를 바라보는 바깥 방향 벡터 계산
                    Vector2 pushDirection = (monster.transform.position - transform.position).normalized;

                    // 완전히 겹쳐있다면 랜덤 방향으로 보정
                    if (pushDirection == Vector2.zero)
                    {
                        pushDirection = Random.insideUnitCircle.normalized;
                    }

                    //  데미지는 0, 강력한 넉백 힘과 방향을 타겟에게 전달합니다!
                    damageable.TakeDamage(0f, pushDirection * strongForce);
                }
            }

            Debug.Log("<color=red>[반사신경 망치]</color> 쾅! 반사 신경 작동, 주변 적들을 격퇴했습니다.");
        }
        public void AddLuck(float amount)
        {
            luckMultiplier += amount;
        }

        public void EnableGinsengStick()
        {
            hasGinsengStick = true;
        }
        //전자체온계
        public void AddMouthwash()
        {
            mouthwashCount++;
        }
        //반사신경망치
        public void EnableReflexHammer()
        {
            hasReflexHammer = true;
        }

        public void EnableAntibioticBomb()
        {
            // 한 번 살 때마다 폭발 확률 5%(0.05)씩 누적!
            antibioticBombChance += 0.05f;
        }

        public void AddLifeSteal(float amount)
        {
            lifeSteal += amount;
        }

        public void AddHealOnKill(float amount)
        {
            healOnKill += amount;
        }
        public void AddProjectileSize(float amount)
        {
            projectileSizeMultiplier += amount;
        }
        public void AddDamageMultiplier(float amount)
        {
            damageMultiplier += amount;
        }
        public void AddProjectileCount(int count)
        {
            additionalProjectiles += count;
        }
        public void EnableAutoCollect()
        {
            autoCollectItems = true;
        }
        public void AddSlowChance(float amount)
        {
            slowChance += amount;
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

        // =====================================================================
        // Dash 확장용 메서드
        // 나중에 증강/아이템에서 호출해서 대쉬 횟수나 성능을 늘릴 수 있음
        // =====================================================================

        public void AddDashCharge(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            maxDashCharges += amount;
            currentDashCharges = Mathf.Min(currentDashCharges + amount, maxDashCharges);
        }

        public void AddDashDistance(float amount)
        {
            dashDistance += amount;
            dashDistance = Mathf.Max(0.1f, dashDistance);
        }

        public void AddDashRechargeSpeed(float amount)
        {
            dashRechargeTime -= amount;
            dashRechargeTime = Mathf.Max(0.1f, dashRechargeTime);
        }
    }
}