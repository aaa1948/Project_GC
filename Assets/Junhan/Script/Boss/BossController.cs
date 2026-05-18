using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    [System.Serializable]
    public class BossPhaseSettings
    {
        [Header("Phase Identity")]
        [Tooltip("인스펙터에서 구분하기 위한 페이즈 이름입니다. 실제 로직에는 큰 영향을 주지 않습니다.")]
        public string phaseName = "Phase";

        [Tooltip("이 페이즈가 시작되는 HP 비율입니다. 0.5면 보스 HP가 50% 이하일 때 진입합니다. 1페이즈는 시작 페이즈라 사실상 사용하지 않습니다.")]
        [Range(0f, 1f)]
        public float enterHpRatio = 1f;

        [Header("Power Multipliers")]
        [Tooltip("보스의 기본 공격, 접촉 데미지, 패턴 데미지에 곱해지는 배율입니다. 1.5면 150%, 2면 200%입니다.")]
        public float damageMultiplier = 1f;

        [Tooltip("보스의 기본 공격 탄환 속도와 패턴 발사체 속도에 곱해지는 배율입니다. 1.5면 150%, 2면 200%입니다.")]
        public float projectileSpeedMultiplier = 1f;

        [Tooltip("보스의 추적 이동 속도와 돌진 패턴 이동 속도에 곱해지는 배율입니다. 1.5면 150%, 2면 200%입니다.")]
        public float movementSpeedMultiplier = 1f;

        [Header("Attack Timing Multipliers")]
        [Tooltip("기본 공격 쿨타임에 곱해지는 배율입니다. 1이면 그대로, 0.5면 기본 공격을 2배 자주 사용합니다.")]
        public float basicAttackCooldownMultiplier = 1f;

        [Tooltip("각 패턴의 개별 쿨타임에 곱해지는 배율입니다. 1이면 그대로, 0.5면 패턴별 쿨타임이 절반으로 줄어듭니다.")]
        public float patternCooldownMultiplier = 1f;

        [Tooltip("보스가 다음 패턴을 사용하기까지 기다리는 전체 패턴 간격에 곱해지는 배율입니다. 0.5면 패턴 사용 빈도가 증가합니다.")]
        public float patternIntervalMultiplier = 1f;

        [Tooltip("패턴이 끝난 뒤 다음 행동까지 기다리는 짧은 후딜 시간에 곱해지는 배율입니다. 0.5면 후딜이 짧아집니다.")]
        public float patternGapMultiplier = 1f;

        [Header("Pattern Selection")]
        [Tooltip("체크하면 이 페이즈에서는 플레이어와의 거리 기반 가중치를 무시하고, 사용 가능한 패턴 중 무작위로 선택합니다. 3페이즈 발광 패턴에 사용하세요.")]
        public bool ignoreDistanceAndUseRandomPattern = false;

        [Header("Phase Transition")]
        [Tooltip("이 페이즈로 넘어갈 때 보스가 멈추는 시간입니다. 이 시간 동안 외형 변경 연출을 넣으면 됩니다.")]
        public float transitionPauseSeconds = 2f;

        [Tooltip("체크하면 페이즈 전환 중 보스가 데미지를 받지 않습니다.")]
        public bool invincibleDuringTransition = true;

        [Tooltip("이 페이즈가 시작될 때 보스 스프라이트를 교체하고 싶으면 여기에 넣으세요. 비워두면 기존 스프라이트를 유지합니다.")]
        public Sprite phaseSprite;

        [Tooltip("이 페이즈가 시작될 때 생성할 이펙트 프리팹입니다. 비워두면 이펙트 없이 페이즈만 전환됩니다.")]
        public GameObject phaseTransitionEffectPrefab;

        public BossPhaseSettings()
        {
        }

        public BossPhaseSettings(
            string phaseName,
            float enterHpRatio,
            float damageMultiplier,
            float projectileSpeedMultiplier,
            float movementSpeedMultiplier,
            float basicAttackCooldownMultiplier,
            float patternCooldownMultiplier,
            float patternIntervalMultiplier,
            float patternGapMultiplier,
            bool ignoreDistanceAndUseRandomPattern,
            float transitionPauseSeconds,
            bool invincibleDuringTransition
        )
        {
            this.phaseName = phaseName;
            this.enterHpRatio = enterHpRatio;
            this.damageMultiplier = damageMultiplier;
            this.projectileSpeedMultiplier = projectileSpeedMultiplier;
            this.movementSpeedMultiplier = movementSpeedMultiplier;
            this.basicAttackCooldownMultiplier = basicAttackCooldownMultiplier;
            this.patternCooldownMultiplier = patternCooldownMultiplier;
            this.patternIntervalMultiplier = patternIntervalMultiplier;
            this.patternGapMultiplier = patternGapMultiplier;
            this.ignoreDistanceAndUseRandomPattern = ignoreDistanceAndUseRandomPattern;
            this.transitionPauseSeconds = transitionPauseSeconds;
            this.invincibleDuringTransition = invincibleDuringTransition;
        }
    }

    public class BossController : MonoBehaviour
    {
        [Header("Boss References")]
        [Tooltip("플레이어 캐릭터입니다. 비워두면 시작 시 자동으로 씬에서 찾습니다.")]
        [SerializeField] private Character playerCharacter;

        [Tooltip("보스의 Rigidbody2D입니다. 비워두면 자동으로 현재 오브젝트에서 찾습니다.")]
        [SerializeField] private Rigidbody2D rb;

        [Tooltip("보스 외형을 담당하는 SpriteRenderer입니다. 페이즈 전환 시 스프라이트 변경에 사용됩니다.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("보스가 사용할 패턴 목록입니다. Auto Collect가 켜져 있으면 보스 오브젝트에 붙은 BossPatternBase들을 자동 수집합니다.")]
        [SerializeField] private List<BossPatternBase> patterns = new List<BossPatternBase>();

        [Tooltip("체크하면 시작 시 보스 본체와 자식 오브젝트에서 BossPatternBase를 상속한 패턴들을 자동 수집합니다.")]
        [SerializeField] private bool autoCollectPatternsFromChildren = true;

        [Header("Boss HP")]
        [Tooltip("보스 최대 체력입니다. BossMonster가 있으면 실제 보스 체력은 BossMonster의 MonsterBlueprint HP와 동기화됩니다.")]
        [SerializeField] private float maxHp = 500f;

        [Tooltip("현재 보스 체력입니다. 런타임 확인용입니다. BossMonster가 있으면 자동 동기화됩니다.")]
        [SerializeField] private float currentHp = 500f;

        [Header("Contact Damage")]
        [Tooltip("보스 몸에 플레이어가 닿았을 때 입히는 기본 접촉 데미지입니다. 실제 데미지는 페이즈 데미지 배율이 곱해집니다.")]
        [SerializeField] private float contactDamage = 10f;

        [Tooltip("접촉 데미지가 반복으로 들어가는 간격입니다.")]
        [SerializeField] private float contactDamageCooldown = 0.6f;

        [Header("Basic Attack")]
        [Tooltip("체크하면 보스가 기본 탄환 공격을 사용합니다.")]
        [SerializeField] private bool enableBasicAttack = true;

        [Tooltip("보스 기본 공격 탄환 프리팹입니다.")]
        [SerializeField] private GameObject basicAttackBulletPrefab;

        [Tooltip("기본 공격 발사 간격입니다. 실제 간격은 페이즈의 Basic Attack Cooldown Multiplier가 곱해집니다.")]
        [SerializeField] private float basicAttackCooldown = 1.6f;

        [Tooltip("기본 공격 데미지입니다. 실제 데미지는 페이즈 데미지 배율이 곱해집니다.")]
        [SerializeField] private float basicAttackDamage = 5f;

        [Tooltip("기본 탄환 속도입니다. 실제 탄속은 페이즈 Projectile Speed Multiplier가 곱해집니다.")]
        [SerializeField] private float basicAttackBulletSpeed = 4.5f;

        [Tooltip("보스 중심에서 탄환을 얼마나 앞쪽에 생성할지 결정합니다.")]
        [SerializeField] private float basicAttackMuzzleOffset = 1.2f;

        [Tooltip("체크하면 특수 패턴 사용 중에도 기본 공격을 계속 발사합니다.")]
        [SerializeField] private bool basicAttackWhileUsingPattern = false;

        [Header("Basic Attack Aim")]
        [Tooltip("체크하면 플레이어 현재 위치가 아니라 이동 방향 앞쪽을 예측 조준합니다.")]
        [SerializeField] private bool usePredictiveBasicAim = true;

        [Tooltip("플레이어가 이 시간 뒤에 있을 것으로 예상되는 위치를 조준합니다.")]
        [SerializeField] private float basicAimLeadTime = 0.45f;

        [Tooltip("조준에 약간의 랜덤 오차를 줍니다. 값이 클수록 피하기 쉬워집니다.")]
        [SerializeField] private float basicAimInaccuracyAngle = 5f;

        [Tooltip("플레이어 이동속도가 이 값보다 낮으면 예측 조준을 사용하지 않습니다.")]
        [SerializeField] private float minPlayerVelocityForPrediction = 0.1f;

        [Header("Basic Attack Visual Sorting")]
        [Tooltip("체크하면 기본 공격 탄환의 SpriteRenderer Sorting Order를 강제로 설정합니다.")]
        [SerializeField] private bool forceBasicBulletSortingOrder = true;

        [Tooltip("기본 공격 탄환의 화면 표시 순서입니다. 보스보다 앞에 보이면 높이고, 뒤에 보여야 하면 낮추세요.")]
        [SerializeField] private int basicBulletSortingOrder = 500;

        [Header("Movement")]
        [Tooltip("체크하면 보스가 플레이어를 향해 이동합니다.")]
        [SerializeField] private bool enableMovement = true;

        [Tooltip("1페이즈 기준 보스 기본 이동속도입니다. 실제 이동속도는 페이즈 Movement Speed Multiplier가 곱해집니다.")]
        [SerializeField] private float baseMoveSpeed = 0.8f;

        [Tooltip("플레이어와 이 거리 이하로 가까워지면 보스가 이동을 멈춥니다.")]
        [SerializeField] private float stopDistanceFromPlayer = 3f;

        [Tooltip("체크하면 패턴 사용 중에도 보스가 천천히 이동합니다.")]
        [SerializeField] private bool moveWhileUsingPattern = true;

        [Tooltip("패턴 사용 중 이동속도 배율입니다. 0.35면 패턴 중 이동속도가 35%로 감소합니다.")]
        [SerializeField] private float patternMoveSpeedMultiplier = 0.35f;

        [Tooltip("보스 이동 보간 시간입니다. 값이 낮을수록 빠르게 방향을 바꿉니다.")]
        [SerializeField] private float movementSmoothTime = 0.18f;

        [Tooltip("체크하면 보스 스프라이트가 플레이어 방향을 바라보도록 좌우 반전됩니다.")]
        [SerializeField] private bool flipSpriteToPlayer = true;

        [Header("Special Pattern Timing")]
        [Tooltip("보스 등장 후 첫 패턴 사용까지 기다리는 시간입니다.")]
        [SerializeField] private float firstPatternDelay = 3f;

        [Tooltip("패턴 사용 간격의 최소값입니다. 실제 값은 페이즈 Pattern Interval Multiplier가 곱해집니다.")]
        [SerializeField] private float patternIntervalMin = 4f;

        [Tooltip("패턴 사용 간격의 최대값입니다. 실제 값은 페이즈 Pattern Interval Multiplier가 곱해집니다.")]
        [SerializeField] private float patternIntervalMax = 7f;

        [Tooltip("보스가 패턴 사용 조건을 다시 확인하는 간격입니다. 너무 낮으면 불필요한 연산이 늘어납니다.")]
        [SerializeField] private float thinkInterval = 0.3f;

        [Tooltip("패턴이 끝난 뒤 다음 행동까지 기다리는 시간입니다. 실제 값은 페이즈 Pattern Gap Multiplier가 곱해집니다.")]
        [SerializeField] private float patternGap = 1.2f;

        [Header("Distance Thresholds")]
        [Tooltip("이 거리 이하이면 근거리 패턴 가중치를 사용합니다.")]
        [SerializeField] private float nearDistanceThreshold = 4f;

        [Tooltip("근거리보다 멀고 이 거리 이하이면 중거리 패턴 가중치를 사용합니다. 이 거리보다 멀면 원거리 가중치를 사용합니다.")]
        [SerializeField] private float midDistanceThreshold = 8f;

        [Header("Phase Settings")]
        [Tooltip("1페이즈 설정입니다. 기본 배율은 100%입니다.")]
        [SerializeField]
        private BossPhaseSettings phase1Settings = new BossPhaseSettings(
            "Phase 1",
            1f,
            1f,
            1f,
            1f,
            1f,
            1f,
            1f,
            1f,
            false,
            0f,
            false
        );

        [Tooltip("2페이즈 설정입니다. 기본값은 HP 50% 이하에서 시작하며, 데미지/탄속/이동속도 150%입니다.")]
        [SerializeField]
        private BossPhaseSettings phase2Settings = new BossPhaseSettings(
            "Phase 2",
            0.5f,
            1.5f,
            1.5f,
            1.5f,
            1f,
            1f,
            1f,
            1f,
            false,
            2f,
            true
        );

        [Tooltip("3페이즈 설정입니다. 기본값은 HP 20% 이하에서 시작하며, 데미지/탄속/이동속도 200%, 거리 무시 랜덤 패턴, 쿨타임 단축입니다.")]
        [SerializeField]
        private BossPhaseSettings phase3Settings = new BossPhaseSettings(
            "Phase 3",
            0.2f,
            2f,
            2f,
            2f,
            0.5f,
            0.5f,
            0.5f,
            0.5f,
            true,
            2f,
            true
        );

        [Header("Debug")]
        [Tooltip("체크하면 보스 이동 관련 로그를 출력합니다.")]
        [SerializeField] private bool debugMovement = false;

        [Tooltip("체크하면 보스 패턴 선택 관련 로그를 출력합니다.")]
        [SerializeField] private bool debugPattern = false;

        [Tooltip("체크하면 보스 기본 공격 로그를 출력합니다.")]
        [SerializeField] private bool debugBasicAttack = false;

        [Tooltip("체크하면 페이즈 전환 로그를 출력합니다.")]
        [SerializeField] private bool debugPhase = true;

        private bool isDead = false;
        private bool isUsingPattern = false;
        private bool isPhaseTransitioning = false;
        private bool isInvincibleByPhase = false;
        private bool healthInitializedFromMonster = false;

        private int currentPhase = 1;

        private float lastContactDamageTime = -999f;
        private Vector2 smoothMoveVelocity;

        private bool externalMovementLock = false;
        private bool suppressContactDamage = false;

        private float basicAttackTimer = 0f;
        private float nextPatternTime = 0f;

        private Vector2 lastPlayerPosition;
        private Vector2 estimatedPlayerVelocity;

        private Coroutine patternLoopCoroutine;
        private Coroutine activePatternCoroutine;
        private Coroutine phaseTransitionCoroutine;

        public float NearDistanceThreshold => nearDistanceThreshold;
        public float MidDistanceThreshold => midDistanceThreshold;
        public int CurrentPhase => currentPhase;
        public Character PlayerCharacter => playerCharacter;
        public Vector3 BossCenterPosition => transform.position;
        public Rigidbody2D Rigidbody => rb;
        public bool IsDead => isDead;
        public bool IsPhaseTransitioning => isPhaseTransitioning;
        public bool IsInvincibleToDamage => isDead || isPhaseTransitioning || isInvincibleByPhase;

        public void SetPlayerCharacter(Character character)
        {
            playerCharacter = character;

            if (playerCharacter != null)
            {
                lastPlayerPosition = playerCharacter.transform.position;
            }
        }

        public void NotifyBossHealthInitialized(float currentHealth, float maximumHealth)
        {
            maxHp = Mathf.Max(1f, maximumHealth);
            currentHp = Mathf.Clamp(currentHealth, 0f, maxHp);
            healthInitializedFromMonster = true;

            if (debugPhase)
            {
                Debug.Log($"[BossController] HP 초기화 완료 / current={currentHp}, max={maxHp}");
            }
        }

        public void NotifyBossHealthChanged(float currentHealth, float maximumHealth)
        {
            maxHp = Mathf.Max(1f, maximumHealth);
            currentHp = Mathf.Clamp(currentHealth, 0f, maxHp);

            if (currentHp <= 0f)
            {
                return;
            }

            TryStartNextPhaseByHp();
        }

        public void NotifyBossDeathStarted()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            isPhaseTransitioning = false;
            isInvincibleByPhase = false;

            StopPatternLoop();
            StopMovementVelocity();

            if (debugPhase)
            {
                Debug.Log("[BossController] Boss death started. Pattern loop stopped.");
            }
        }

        public void SetExternalMovementLock(bool value)
        {
            externalMovementLock = value;

            if (value)
            {
                StopMovementVelocity();
            }
        }

        public void SetSuppressContactDamage(bool value)
        {
            suppressContactDamage = value;
        }

        public float GetModifiedDamage(float baseDamage)
        {
            return baseDamage * GetCurrentPhaseSettings().damageMultiplier;
        }

        public float GetModifiedProjectileSpeed(float baseSpeed)
        {
            return baseSpeed * GetCurrentPhaseSettings().projectileSpeedMultiplier;
        }

        public float GetModifiedMovementSpeed(float baseSpeed)
        {
            return baseSpeed * GetCurrentPhaseSettings().movementSpeedMultiplier;
        }

        public float GetModifiedPatternCooldown(float baseCooldown)
        {
            return Mathf.Max(0.05f, baseCooldown * GetCurrentPhaseSettings().patternCooldownMultiplier);
        }

        public bool ShouldIgnoreDistanceForPatternSelection()
        {
            return GetCurrentPhaseSettings().ignoreDistanceAndUseRandomPattern;
        }

        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void Start()
        {
            if (playerCharacter == null)
            {
                playerCharacter = FindObjectOfType<Character>();
            }

            if (playerCharacter != null)
            {
                lastPlayerPosition = playerCharacter.transform.position;
            }

            if (autoCollectPatternsFromChildren || patterns.Count == 0)
            {
                patterns.Clear();

                BossPatternBase[] foundPatterns = GetComponentsInChildren<BossPatternBase>(true);
                foreach (BossPatternBase pattern in foundPatterns)
                {
                    if (pattern != null)
                    {
                        patterns.Add(pattern);
                    }
                }
            }

            if (!healthInitializedFromMonster)
            {
                currentHp = maxHp;
            }

            currentPhase = 1;
            ApplyPhaseVisual(phase1Settings);

            foreach (BossPatternBase pattern in patterns)
            {
                if (pattern != null)
                {
                    pattern.Init(this);
                }
            }

            basicAttackTimer = 0f;
            ScheduleNextPattern(firstPatternDelay);
            StartPatternLoop();

            if (debugPattern)
            {
                Debug.Log($"[BossController] Pattern Count = {patterns.Count}");
            }
        }

        private void OnDisable()
        {
            StopPatternLoop();
        }

        private void Update()
        {
            UpdatePlayerVelocityEstimate();
            UpdateSpriteFlip();
            UpdateBasicAttack();
        }

        private void FixedUpdate()
        {
            UpdateMovement();
        }

        private void UpdatePlayerVelocityEstimate()
        {
            if (playerCharacter == null)
            {
                return;
            }

            Vector2 currentPlayerPosition = playerCharacter.transform.position;

            if (Time.deltaTime > 0f)
            {
                estimatedPlayerVelocity = (currentPlayerPosition - lastPlayerPosition) / Time.deltaTime;
            }

            lastPlayerPosition = currentPlayerPosition;
        }

        private void UpdateBasicAttack()
        {
            if (!enableBasicAttack || isDead || isPhaseTransitioning || playerCharacter == null)
            {
                return;
            }

            if (isUsingPattern && !basicAttackWhileUsingPattern)
            {
                return;
            }

            if (basicAttackBulletPrefab == null)
            {
                return;
            }

            float finalCooldown = Mathf.Max(
                0.05f,
                basicAttackCooldown * GetCurrentPhaseSettings().basicAttackCooldownMultiplier
            );

            basicAttackTimer += Time.deltaTime;

            if (basicAttackTimer >= finalCooldown)
            {
                basicAttackTimer = 0f;
                FireBasicAttack();
            }
        }

        private void FireBasicAttack()
        {
            Vector2 origin = BossCenterPosition;
            Vector2 aimPosition = GetBasicAttackAimPosition();
            Vector2 direction = (aimPosition - origin).normalized;

            if (direction == Vector2.zero)
            {
                direction = Vector2.right;
            }

            if (basicAimInaccuracyAngle > 0f)
            {
                float randomAngle = Random.Range(-basicAimInaccuracyAngle, basicAimInaccuracyAngle);
                direction = RotateVector(direction, randomAngle);
            }

            Vector3 spawnPosition = (Vector3)origin + (Vector3)(direction * basicAttackMuzzleOffset);

            GameObject bullet = Instantiate(basicAttackBulletPrefab, spawnPosition, Quaternion.identity);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            ApplyBulletSortingOrder(bullet, basicBulletSortingOrder);

            BossSimpleBullet simpleBullet = bullet.GetComponent<BossSimpleBullet>();
            if (simpleBullet == null)
            {
                simpleBullet = bullet.GetComponentInChildren<BossSimpleBullet>();
            }

            if (simpleBullet == null)
            {
                simpleBullet = bullet.AddComponent<BossSimpleBullet>();
                Debug.LogWarning("[BossController] Basic Attack Bullet에 BossSimpleBullet이 없어 자동 추가했습니다.");
            }

            float finalSpeed = GetModifiedProjectileSpeed(basicAttackBulletSpeed);
            float finalDamage = GetModifiedDamage(basicAttackDamage);

            simpleBullet.Init(direction, finalSpeed, finalDamage);

            if (debugBasicAttack)
            {
                Debug.Log($"[BossController] Basic Attack Fired / phase={currentPhase}, speed={finalSpeed}, damage={finalDamage}");
            }
        }

        private Vector2 GetBasicAttackAimPosition()
        {
            Vector2 currentPlayerPosition = playerCharacter.transform.position;

            if (!usePredictiveBasicAim)
            {
                return currentPlayerPosition;
            }

            if (estimatedPlayerVelocity.magnitude < minPlayerVelocityForPrediction)
            {
                return currentPlayerPosition;
            }

            return currentPlayerPosition + estimatedPlayerVelocity * basicAimLeadTime;
        }

        private void ApplyBulletSortingOrder(GameObject bullet, int sortingOrder)
        {
            if (!forceBasicBulletSortingOrder || bullet == null)
            {
                return;
            }

            SpriteRenderer[] renderers = bullet.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.sortingOrder = sortingOrder;
            }
        }

        private void UpdateMovement()
        {
            if (!CanMove())
            {
                StopMovementVelocity();
                return;
            }

            Vector2 bossPosition = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 playerPosition = playerCharacter.transform.position;

            Vector2 toPlayer = playerPosition - bossPosition;
            float distance = toPlayer.magnitude;

            if (distance <= stopDistanceFromPlayer)
            {
                StopMovementVelocity();
                return;
            }

            Vector2 direction = toPlayer.normalized;
            Vector2 targetPosition = playerPosition - direction * stopDistanceFromPlayer;

            float finalSpeed = GetModifiedMovementSpeed(baseMoveSpeed);

            if (isUsingPattern)
            {
                finalSpeed *= patternMoveSpeedMultiplier;
            }

            Vector2 nextPosition = Vector2.SmoothDamp(
                bossPosition,
                targetPosition,
                ref smoothMoveVelocity,
                movementSmoothTime,
                finalSpeed,
                Time.fixedDeltaTime
            );

            float maxStep = finalSpeed * Time.fixedDeltaTime;

            if (Vector2.Distance(bossPosition, nextPosition) > maxStep * 1.5f)
            {
                nextPosition = Vector2.MoveTowards(bossPosition, nextPosition, maxStep);
            }

            if (rb != null)
            {
                rb.MovePosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            if (debugMovement)
            {
                Debug.Log($"[BossController] Moving / phase={currentPhase}, distance={distance:F2}, speed={finalSpeed:F2}");
            }
        }

        private bool CanMove()
        {
            if (!enableMovement)
            {
                return false;
            }

            if (isDead || isPhaseTransitioning)
            {
                return false;
            }

            if (playerCharacter == null)
            {
                return false;
            }

            if (externalMovementLock)
            {
                return false;
            }

            if (isUsingPattern && !moveWhileUsingPattern)
            {
                return false;
            }

            return true;
        }

        private void StopMovementVelocity()
        {
            smoothMoveVelocity = Vector2.zero;

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void UpdateSpriteFlip()
        {
            if (!flipSpriteToPlayer || spriteRenderer == null || playerCharacter == null)
            {
                return;
            }

            float directionX = playerCharacter.transform.position.x - transform.position.x;
            spriteRenderer.flipX = directionX < 0f;
        }

        private void StartPatternLoop()
        {
            if (patternLoopCoroutine != null)
            {
                StopCoroutine(patternLoopCoroutine);
            }

            patternLoopCoroutine = StartCoroutine(PatternLoop());
        }

        private void StopPatternLoop()
        {
            if (activePatternCoroutine != null)
            {
                StopCoroutine(activePatternCoroutine);
                activePatternCoroutine = null;
            }

            if (patternLoopCoroutine != null)
            {
                StopCoroutine(patternLoopCoroutine);
                patternLoopCoroutine = null;
            }

            isUsingPattern = false;
        }

        private IEnumerator PatternLoop()
        {
            while (!isDead)
            {
                if (!isPhaseTransitioning && !isUsingPattern && playerCharacter != null && Time.time >= nextPatternTime)
                {
                    BossPatternBase selectedPattern = SelectPattern();

                    if (selectedPattern != null)
                    {
                        yield return StartCoroutine(UsePattern(selectedPattern));
                    }

                    ScheduleNextPattern();
                }

                yield return new WaitForSeconds(thinkInterval);
            }
        }

        private IEnumerator UsePattern(BossPatternBase pattern)
        {
            isUsingPattern = true;

            if (debugPattern)
            {
                Debug.Log($"[BossController] Use Pattern: {pattern.PatternName} / phase={currentPhase}");
            }

            activePatternCoroutine = StartCoroutine(pattern.Execute());
            yield return activePatternCoroutine;
            activePatternCoroutine = null;

            float finalPatternGap = Mathf.Max(
                0f,
                patternGap * GetCurrentPhaseSettings().patternGapMultiplier
            );

            yield return new WaitForSeconds(finalPatternGap);

            isUsingPattern = false;
        }

        private void ScheduleNextPattern(float overrideDelay = -1f)
        {
            float delay;

            if (overrideDelay >= 0f)
            {
                delay = overrideDelay;
            }
            else
            {
                float min = Mathf.Min(patternIntervalMin, patternIntervalMax);
                float max = Mathf.Max(patternIntervalMin, patternIntervalMax);

                delay = Random.Range(min, max);
                delay *= GetCurrentPhaseSettings().patternIntervalMultiplier;
                delay = Mathf.Max(0.05f, delay);
            }

            nextPatternTime = Time.time + delay;

            if (debugPattern)
            {
                Debug.Log($"[BossController] Next Pattern In {delay:F2}s / phase={currentPhase}");
            }
        }

        private BossPatternBase SelectPattern()
        {
            if (ShouldIgnoreDistanceForPatternSelection())
            {
                return SelectPatternRandomOnly();
            }

            return SelectPatternByDistance();
        }

        private BossPatternBase SelectPatternRandomOnly()
        {
            List<BossPatternBase> validPatterns = new List<BossPatternBase>();

            foreach (BossPatternBase pattern in patterns)
            {
                if (pattern == null || !pattern.CanUse())
                {
                    continue;
                }

                validPatterns.Add(pattern);
            }

            if (validPatterns.Count == 0)
            {
                return null;
            }

            return validPatterns[Random.Range(0, validPatterns.Count)];
        }

        private BossPatternBase SelectPatternByDistance()
        {
            float distance = GetDistanceToPlayer();

            List<BossPatternBase> validPatterns = new List<BossPatternBase>();
            List<int> weights = new List<int>();

            int totalWeight = 0;

            foreach (BossPatternBase pattern in patterns)
            {
                if (pattern == null || !pattern.CanUse())
                {
                    continue;
                }

                int weight = pattern.GetWeight(distance, currentPhase);

                if (weight <= 0)
                {
                    continue;
                }

                validPatterns.Add(pattern);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (validPatterns.Count == 0 || totalWeight <= 0)
            {
                return null;
            }

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < validPatterns.Count; i++)
            {
                cumulative += weights[i];

                if (roll < cumulative)
                {
                    return validPatterns[i];
                }
            }

            return validPatterns[0];
        }

        private float GetDistanceToPlayer()
        {
            if (playerCharacter == null)
            {
                return 999f;
            }

            return Vector2.Distance(transform.position, playerCharacter.transform.position);
        }

        public void TakeDamage(float damage)
        {
            if (IsInvincibleToDamage)
            {
                return;
            }

            currentHp -= damage;
            currentHp = Mathf.Max(0f, currentHp);

            NotifyBossHealthChanged(currentHp, maxHp);

            if (currentHp <= 0f)
            {
                NotifyBossDeathStarted();
            }
        }

        private void TryStartNextPhaseByHp()
        {
            if (isDead || isPhaseTransitioning)
            {
                return;
            }

            float hpRatio = maxHp > 0f ? currentHp / maxHp : 1f;

            if (currentPhase < 2 && hpRatio <= phase2Settings.enterHpRatio)
            {
                StartPhaseTransition(2);
                return;
            }

            if (currentPhase < 3 && hpRatio <= phase3Settings.enterHpRatio)
            {
                StartPhaseTransition(3);
            }
        }

        private void StartPhaseTransition(int targetPhase)
        {
            if (phaseTransitionCoroutine != null)
            {
                StopCoroutine(phaseTransitionCoroutine);
            }

            phaseTransitionCoroutine = StartCoroutine(PhaseTransitionRoutine(targetPhase));
        }

        private IEnumerator PhaseTransitionRoutine(int targetPhase)
        {
            BossPhaseSettings targetSettings = GetPhaseSettings(targetPhase);

            isPhaseTransitioning = true;
            isInvincibleByPhase = targetSettings.invincibleDuringTransition;
            currentPhase = targetPhase;

            StopPatternLoop();
            StopMovementVelocity();

            externalMovementLock = true;
            suppressContactDamage = true;

            ApplyPhaseVisual(targetSettings);

            if (debugPhase)
            {
                Debug.Log($"[BossController] {targetSettings.phaseName} Start / pause={targetSettings.transitionPauseSeconds}, invincible={isInvincibleByPhase}");
            }

            yield return new WaitForSeconds(Mathf.Max(0f, targetSettings.transitionPauseSeconds));

            isInvincibleByPhase = false;
            isPhaseTransitioning = false;

            externalMovementLock = false;
            suppressContactDamage = false;

            basicAttackTimer = 0f;

            ScheduleNextPattern(0.2f);
            StartPatternLoop();

            phaseTransitionCoroutine = null;

            TryStartNextPhaseByHp();
        }

        private void ApplyPhaseVisual(BossPhaseSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            if (settings.phaseSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = settings.phaseSprite;
            }

            if (settings.phaseTransitionEffectPrefab != null)
            {
                Instantiate(settings.phaseTransitionEffectPrefab, BossCenterPosition, Quaternion.identity);
            }
        }

        private BossPhaseSettings GetCurrentPhaseSettings()
        {
            return GetPhaseSettings(currentPhase);
        }

        private BossPhaseSettings GetPhaseSettings(int phase)
        {
            if (phase >= 3)
            {
                return phase3Settings;
            }

            if (phase == 2)
            {
                return phase2Settings;
            }

            return phase1Settings;
        }

        private void TryDealContactDamage(Collider2D other)
        {
            if (isDead || playerCharacter == null)
            {
                return;
            }

            if (suppressContactDamage || isPhaseTransitioning)
            {
                return;
            }

            Character character = other.GetComponentInParent<Character>();

            if (character == null || character != playerCharacter)
            {
                return;
            }

            if (Time.time < lastContactDamageTime + contactDamageCooldown)
            {
                return;
            }

            lastContactDamageTime = Time.time;

            float finalDamage = GetModifiedDamage(contactDamage);
            playerCharacter.TakeDamage(finalDamage);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDealContactDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDealContactDamage(other);
        }

        private Vector2 RotateVector(Vector2 vector, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            ).normalized;
        }
    }
}