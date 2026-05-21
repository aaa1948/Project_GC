using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vampire
{
    public class SyringeDartAbility : ProjectileAbility
    {
        [Header("Syringe Dart Stats")]
        [SerializeField] protected UpgradeableProjectileCount projectileCount;
        [SerializeField] protected float syringeDelay = 0.08f;

        [Header("Syringe Range")]
        [Tooltip("기본 침 최대 사거리입니다. 이제 Shuriken.prefab의 Max Distance 대신 이 값을 기준으로 사용합니다.")]
        [SerializeField] private float baseSyringeMaxDistance = 6f;

        [Header("Spread Settings")]
        [SerializeField] private float angleBetweenProjectiles = 8f;
        [SerializeField] private float maxTotalSpreadAngle = 120f;

        [Header("Active Special Augments")]
        [SerializeField] private bool poisonEnabled = false;
        [SerializeField] private bool explosionEnabled = false;
        [SerializeField] private bool homingEnabled = false;
        [SerializeField] private bool pierceEnabled = false;
        [SerializeField] private bool honeyEnabled = false;
        [SerializeField] private bool mosquitoEnabled = false;
        [SerializeField] private bool returnNeedleEnabled = false;
        [SerializeField] private bool acupunctureFormationEnabled = false;

        [Header("Poison Settings")]
        [SerializeField] private float poisonDuration = 3f;
        [SerializeField] private float poisonTickInterval = 0.5f;
        [SerializeField] private float poisonTickDamage = 2f;

        [Header("Explosion Settings")]
        [SerializeField] private float explosionRadius = 1.5f;
        [SerializeField] private float explosionDamage = 2f;
        [Tooltip("폭발침 특수증강을 먹었을 때 폭발이 발생할 확률입니다. 1이면 100% 확률로 폭발합니다.")]
        [SerializeField, Range(0f, 1f)] private float specialExplosionChance = 1f;

        [Header("Homing Settings")]
        [SerializeField] private float homingRange = 6f;
        [SerializeField] private float homingLerpSpeed = 8f;

        [Header("Pierce Settings")]
        [Tooltip("관통침 기본 관통 횟수. 2라면 첫 적중 이후 추가로 2번 더 관통 가능.")]
        [SerializeField] private int pierceCount = 2;

        [Header("Honey Needle Settings")]
        [Tooltip("꿀침 둔화 지속 시간")]
        [SerializeField] private float honeyDuration = 2.5f;

        [Tooltip("0.6이면 몬스터의 체감 이동속도가 약 60% 수준으로 감소합니다. 1이면 둔화 없음.")]
        [SerializeField] private float honeySlowMultiplier = 0.6f;

        [Header("Mosquito Needle Settings")]
        [Tooltip("모기침 적중 1회당 회복량")]
        [SerializeField] private float mosquitoHealPerHit = 1f;

        [Tooltip("대상 이름/컴포넌트 이름에 Boss가 들어가면 회복량에 곱해지는 배율")]
        [SerializeField] private float mosquitoBossHealMultiplier = 2f;

        [Header("Return Needle / 침귀환 Settings")]
        [Tooltip("귀환 중 침 속도 배율")]
        [SerializeField] private float returnNeedleSpeedMultiplier = 1.25f;

        [Tooltip("귀환 경로에서 주는 피해 배율. 0.7이면 현재 침 데미지의 70%")]
        [SerializeField] private float returnNeedleDamageMultiplier = 0.7f;

        [Tooltip("플레이어와 이 거리 이하로 가까워지면 귀환 완료로 판단하고 사라집니다.")]
        [SerializeField] private float returnNeedleArriveDistance = 0.45f;

        [Tooltip("귀환 상태로 유지될 수 있는 최대 시간")]
        [SerializeField] private float returnNeedleMaxDuration = 2.5f;

        [Header("Acupuncture Formation / 침술진 Settings")]
        [Tooltip("침술진 획득 시 추가되는 대쉬 횟수")]
        [SerializeField] private int acupunctureFormationBonusDashCharges = 1;

        [Tooltip("대쉬 시작 위치에 생성되는 분신 지속 시간")]
        [SerializeField] private float acupunctureFormationLifetime = 0.35f;

        [Tooltip("분신이 360도로 발사하는 침 개수")]
        [SerializeField] private int acupunctureFormationNeedleCount = 12;

        [Tooltip("분신이 발사하는 침의 데미지 배율. 0.55면 현재 침 데미지의 55%")]
        [SerializeField] private float acupunctureFormationDamageMultiplier = 0.55f;

        [Tooltip("분신 시각 크기")]
        [SerializeField] private float acupunctureFormationVisualScale = 1f;

        private AcupunctureFormationController acupunctureFormationController;

        [Header("Legendary - Life Burn / HP 1")]
        [SerializeField] private bool lifeBurnEnabled = false;
        [SerializeField] private float lifeBurnDamageMultiplier = 3f;
        [SerializeField] private int lifeBurnBonusProjectiles = 10;
        [SerializeField] private float lifeBurnBonusRange = 3f;

        [Header("Legendary - Clone Culture")]
        [SerializeField] private bool cloneLegendaryTaken = false;

        [Header("Legendary - Hedgehog Needle / 고슴도침")]
        [SerializeField] private bool hedgehogNeedleEnabled = false;

        [Tooltip("플레이어 주변을 도는 침 개수")]
        [SerializeField] private int hedgehogNeedleCount = 8;

        [Tooltip("플레이어와 회전 침 사이 거리. 이 거리가 실드 접촉 판정 범위가 됩니다.")]
        [SerializeField] private float hedgehogOrbitRadius = 1.25f;

        [Tooltip("회전 속도")]
        [SerializeField] private float hedgehogRotationSpeed = 180f;

        [Tooltip("적이 실드에 닿았을 때 같은 대상에게 다시 반격 침을 발사할 수 있기까지의 시간")]
        [SerializeField] private float hedgehogTouchFireCooldown = 0.35f;

        [Tooltip("고슴도침이 발사하는 반격 침의 데미지 배율. 0.45면 기본 침 데미지의 45%")]
        [SerializeField] private float hedgehogDamageMultiplier = 0.45f;

        private HedgehogNeedleController hedgehogNeedleController;

        [Header("Legendary - Heavy Snipe / 대물침")]
        [SerializeField] private bool heavySnipeEnabled = false;

        [Tooltip("true면 우클릭으로 차지, false면 좌클릭으로 차지합니다. 마우스가 없으면 Space 키로 차지합니다.")]
        [SerializeField] private bool useRightMouseForHeavySnipe = true;

        [Tooltip("100% 풀차지까지 걸리는 시간")]
        [SerializeField] private float heavyMaxChargeTime = 1.5f;

        [Header("Heavy Snipe Damage By Charge")]
        [Tooltip("0% 차지 피해 배율. 1이면 현재 데미지 100%")]
        [SerializeField] private float heavyDamageAt0 = 1f;

        [Tooltip("30% 차지 피해 배율. 1.3이면 현재 데미지 130%")]
        [SerializeField] private float heavyDamageAt30 = 1.3f;

        [Tooltip("50% 차지 피해 배율. 1.7이면 현재 데미지 170%")]
        [SerializeField] private float heavyDamageAt50 = 1.7f;

        [Tooltip("70% 차지 피해 배율. 2.0이면 현재 데미지 200%")]
        [SerializeField] private float heavyDamageAt70 = 2f;

        [Tooltip("100% 차지 피해 배율. 3.0이면 현재 데미지 300%")]
        [SerializeField] private float heavyDamageAt100 = 3f;

        [Header("Heavy Snipe Pierce By Charge")]
        [SerializeField] private int heavyPierceAt0 = 0;
        [SerializeField] private int heavyPierceAt30 = 5;
        [SerializeField] private int heavyPierceAt50 = 10;
        [SerializeField] private int heavyPierceAt70 = 20;

        [Tooltip("100% 직전까지 사용할 최대 관통 수. 100% 풀차지 순간에는 이 값을 쓰지 않고 무제한 관통 처리합니다.")]
        [SerializeField] private int heavyPierceAt99 = 40;

        [Header("Heavy Snipe Size By Charge")]
        [Tooltip("0% 차지 침 크기")]
        [SerializeField] private float heavySizeAt0 = 1f;

        [Tooltip("30% 차지 침 크기")]
        [SerializeField] private float heavySizeAt30 = 1.25f;

        [Tooltip("50% 차지 침 크기")]
        [SerializeField] private float heavySizeAt50 = 1.55f;

        [Tooltip("70% 차지 침 크기")]
        [SerializeField] private float heavySizeAt70 = 1.85f;

        [Tooltip("100% 차지 침 크기")]
        [SerializeField] private float heavySizeAt100 = 2.4f;

        [Header("Heavy Snipe Range By Charge")]
        [Tooltip("0% 차지 추가 사거리")]
        [SerializeField] private float heavyRangeBonusAt0 = 0f;

        [Tooltip("30% 차지 추가 사거리")]
        [SerializeField] private float heavyRangeBonusAt30 = 1.5f;

        [Tooltip("50% 차지 추가 사거리")]
        [SerializeField] private float heavyRangeBonusAt50 = 3f;

        [Tooltip("70% 차지 추가 사거리")]
        [SerializeField] private float heavyRangeBonusAt70 = 5f;

        [Tooltip("100% 차지 추가 사거리")]
        [SerializeField] private float heavyRangeBonusAt100 = 8f;

        [Header("Heavy Snipe Knockback")]
        [SerializeField] private float heavyKnockbackMultiplierAt0 = 1f;
        [SerializeField] private float heavyKnockbackMultiplierAt100 = 2f;

        [Header("Heavy Snipe Debug")]
        [SerializeField] private bool debugHeavySnipe = true;

        private bool isHeavyCharging = false;
        private float heavyChargeTimer = 0f;

        // 이기어침 + 대물침 조합에서 사용하는 현재 차지율
        private float cursorHeavyChargeRatio = 0f;

        [Header("Legendary - Cursor Controlled Needle / 이기어침")]
        [SerializeField] private bool cursorControlEnabled = false;

        [Tooltip("마우스 포인트를 따라가는 속도. 높을수록 더 즉각적으로 따라갑니다.")]
        [SerializeField] private float cursorNeedleFollowSpeed = 8f;

        [Tooltip("이기어침 피해 판정 반경")]
        [SerializeField] private float cursorNeedleHitRadius = 0.45f;

        [Tooltip("이기어침 피해 배율. 1이면 현재 침 데미지 100%")]
        [SerializeField] private float cursorNeedleDamageMultiplier = 1f;

        [Tooltip("같은 적에게 다시 피해를 줄 수 있기까지의 시간")]
        [SerializeField] private float cursorNeedleDamageInterval = 0.25f;

        [Tooltip("마우스를 따라다니는 이기어침 시각 크기")]
        [SerializeField] private float cursorNeedleVisualScale = 1.2f;

        [Tooltip("유도침을 보유 중일 때 이기어침의 피해 판정 반경 증가량")]
        [SerializeField] private float cursorNeedleHomingHitRadiusBonus = 0.35f;

        [Header("Cursor Needle Back Display / 이기어침 등 뒤 전시")]
        [Tooltip("플레이어 중심 기준 등 뒤 전시 위치")]
        [SerializeField] private Vector2 cursorNeedleBackDisplayOffset = new Vector2(0f, 0.9f);

        [Tooltip("등 뒤에 전시되는 침 사이 간격")]
        [SerializeField] private float cursorNeedleBackDisplaySpacing = 0.35f;

        [Tooltip("등 뒤에 전시되는 침의 아치 높이")]
        [SerializeField] private float cursorNeedleBackDisplayArcHeight = 0.25f;

        [Tooltip("등 뒤에 전시되는 침 크기")]
        [SerializeField] private float cursorNeedleBackDisplayScale = 0.8f;

        private CursorControlledNeedleController cursorControlledNeedleController;
        
        public GameObject ProjectilePrefab => projectilePrefab;
        public LayerMask MonsterLayer => monsterLayer;

        protected override void Update()
        {
            // 이기어침이 활성화되면 기본 자동 공격은 멈춘다.
            // 단, 대물침도 같이 보유 중이면 우클릭 차지로 이기어침 자체를 강화한다.
            if (cursorControlEnabled)
            {
                HandleCursorControlModeUpdate();
                return;
            }

            // 대물침만 보유한 상태:
            // - 평소에는 기존 기본 자동 공격 유지
            // - 우클릭을 누르거나 차징 중일 때만 기본 공격을 멈추고 대물침 차지 처리
            if (heavySnipeEnabled)
            {
                bool commandHeld = IsHeavySnipeCommandHeld();

                if (commandHeld || isHeavyCharging)
                {
                    HandleHeavySnipeUpdate();
                    return;
                }

                base.Update();
                return;
            }

            base.Update();
        }

        protected override void Attack()
        {
            StartCoroutine(LaunchSyringes());
        }

        protected IEnumerator LaunchSyringes()
        {
            int totalProjectileCount = GetEffectiveProjectileCount();

            Vector2 baseDirection = playerCharacter.LookDirection;

            if (baseDirection == Vector2.zero)
            {
                baseDirection = Vector2.right;
            }

            timeSinceLastAttack -= totalProjectileCount * syringeDelay;

            for (int i = 0; i < totalProjectileCount; i++)
            {
                Vector2 spreadDirection = GetSpreadDirection(baseDirection, i, totalProjectileCount);
                LaunchSyringeProjectile(spreadDirection);

                yield return new WaitForSeconds(syringeDelay);
            }
        }

        private void LaunchSyringeProjectile(Vector2 direction)
        {
            Projectile projectile = entityManager.SpawnProjectile(
                projectileIndex,
                playerCharacter.CenterTransform.position,
                GetEffectiveDamage(),
                GetEffectiveKnockback(),
                GetEffectiveSpeed(),
                monsterLayer
            );

            if (playerCharacter != null)
            {
                projectile.transform.localScale = Vector3.one * GetPlayerProjectileSizeMultiplier();

                // 핵심 수정:
                // Shuriken.prefab의 Max Distance를 곱해서 쓰지 않고,
                // SyringeDartAbility의 baseSyringeMaxDistance를 기준으로 명확하게 세팅한다.
                projectile.maxDistance = GetEffectiveSyringeMaxDistance();
            }

            if (projectile is SyringeProjectile syringeProjectile)
            {
                syringeProjectile.ConfigureSpecials(BuildSpecialRuntime());
            }
            else
            {
                Debug.LogWarning(
                    $"[SyringeDartAbility] Spawned projectile is '{projectile.GetType().Name}', not 'SyringeProjectile'. " +
                    "Projectile Prefab 연결을 다시 확인하세요."
                );
            }

            projectile.OnHitDamageable.AddListener(playerCharacter.OnDealDamage.Invoke);
            projectile.Launch(direction);
        }

        private void HandleCursorControlModeUpdate()
        {
            // 이기어침만 있을 때는 기본 공격도, 대물침 차지도 하지 않는다.
            if (!heavySnipeEnabled)
            {
                cursorHeavyChargeRatio = 0f;
                isHeavyCharging = false;
                heavyChargeTimer = 0f;
                return;
            }

            bool commandHeld = IsHeavySnipeCommandHeld();

            if (commandHeld)
            {
                if (!isHeavyCharging)
                {
                    isHeavyCharging = true;
                    heavyChargeTimer = 0f;

                    if (debugHeavySnipe)
                    {
                        Debug.Log("[이기어침+대물침] 이기어침 충전 시작");
                    }
                }

                heavyChargeTimer += Time.deltaTime;
                cursorHeavyChargeRatio = Mathf.Clamp01(heavyChargeTimer / Mathf.Max(0.01f, heavyMaxChargeTime));

                // 풀차지 이후에도 누르고 있으면 100% 상태 유지
                if (cursorHeavyChargeRatio >= 1f)
                {
                    heavyChargeTimer = heavyMaxChargeTime;
                }
            }
            else
            {
                if (isHeavyCharging && debugHeavySnipe)
                {
                    HeavySnipeChargeStats releaseStats = CalculateHeavySnipeChargeStats(cursorHeavyChargeRatio);

                    Debug.Log(
                        $"[이기어침+대물침] 충전 해제 | " +
                        $"차지 {cursorHeavyChargeRatio * 100f:0}% | " +
                        $"데미지 {releaseStats.damageMultiplier * 100f:0}% | " +
                        $"크기 x{releaseStats.sizeMultiplier:0.00}"
                    );
                }

                isHeavyCharging = false;
                heavyChargeTimer = 0f;
                cursorHeavyChargeRatio = 0f;
            }
        }

        private void HandleHeavySnipeUpdate()
        {
            timeSinceLastAttack += Time.deltaTime;

            bool commandHeld = IsHeavySnipeCommandHeld();
            float effectiveCooldown = GetEffectiveCooldown();

            // 아직 차징 중이 아닐 때
            if (!isHeavyCharging)
            {
                // 우클릭을 누르고 있고, 공격 쿨타임이 준비됐을 때만 차징 시작
                if (commandHeld && timeSinceLastAttack >= effectiveCooldown)
                {
                    isHeavyCharging = true;
                    heavyChargeTimer = 0f;

                    if (debugHeavySnipe)
                    {
                        Debug.Log("[대물침] 차지 시작 - 기본 공격 정지");
                    }
                }

                // 우클릭을 누르고 있지만 쿨타임이 아직이면 기본 공격도 하지 않고 대기
                // 우클릭을 안 누른 상태는 Update()에서 base.Update()로 빠지므로 여기로 오지 않음
                return;
            }

            // 차징 중
            heavyChargeTimer += Time.deltaTime;

            float chargeRatio = Mathf.Clamp01(heavyChargeTimer / Mathf.Max(0.01f, heavyMaxChargeTime));
            bool reachedFullCharge = heavyChargeTimer >= heavyMaxChargeTime;

            // 우클릭을 떼거나 풀차지가 되면 발사
            if (!commandHeld || reachedFullCharge)
            {
                float finalChargeRatio = reachedFullCharge ? 1f : chargeRatio;

                FireHeavySnipe(finalChargeRatio);

                timeSinceLastAttack = 0f;
                isHeavyCharging = false;
                heavyChargeTimer = 0f;

                if (debugHeavySnipe)
                {
                    Debug.Log(
                        $"[대물침] 발사 완료 | 차지 {finalChargeRatio * 100f:0}% - 기본 공격 재개"
                    );
                }
            }
        }

        private bool IsHeavySnipeCommandHeld()
        {
            if (Mouse.current != null)
            {
                if (useRightMouseForHeavySnipe)
                {
                    return Mouse.current.rightButton.isPressed;
                }

                return Mouse.current.leftButton.isPressed;
            }

            if (Keyboard.current != null)
            {
                return Keyboard.current.spaceKey.isPressed;
            }

            return false;
        }

        private void FireHeavySnipe(float chargeRatio)
        {
            if (playerCharacter == null || entityManager == null)
            {
                return;
            }

            HeavySnipeChargeStats stats = CalculateHeavySnipeChargeStats(chargeRatio);
            Vector2 aimDirection = GetAimDirectionFromMouseOrLookDirection();

            SyringeSpecialRuntime runtime = BuildSpecialRuntime();

            // 대물침은 침귀환과 무관하게 기존 차지샷으로 작동해야 하므로 귀환 효과를 강제로 끈다.
            runtime.returnNeedleEnabled = false;

            // 대물침은 전설 차지샷이므로 유도침 증강의 영향을 받지 않는다.
            // 유도침을 보유하고 있어도 대물침은 발사 시점의 aimDirection 그대로 직선 비행한다.
            runtime.homingEnabled = false;
            runtime.homingLerpSpeed = 0f;

            runtime.pierceEnabled = true;
            runtime.pierceCount = stats.unlimitedPierce ? int.MaxValue : stats.pierceCount;
            runtime.rangeBonus += stats.rangeBonus;

            Projectile projectile = entityManager.SpawnProjectile(
                projectileIndex,
                playerCharacter.CenterTransform.position,
                GetEffectiveDamage() * stats.damageMultiplier,
                GetEffectiveKnockback() * stats.knockbackMultiplier,
                GetEffectiveSpeed(),
                monsterLayer
            );

            if (playerCharacter != null)
            {
                projectile.transform.localScale =
                    Vector3.one * GetPlayerProjectileSizeMultiplier() * stats.sizeMultiplier;

                // 핵심 수정:
                // 대물침도 기본 사거리는 SyringeDartAbility에서 관리하고,
                // 차지 사거리 보너스는 runtime.rangeBonus로 SyringeProjectile에서 더해진다.
                projectile.maxDistance = GetEffectiveSyringeMaxDistance();
            }

            if (projectile is SyringeProjectile syringeProjectile)
            {
                syringeProjectile.ConfigureSpecials(runtime);
            }

            projectile.OnHitDamageable.AddListener(playerCharacter.OnDealDamage.Invoke);
            projectile.Launch(aimDirection);
        }

        private Vector2 GetAimDirectionFromMouseOrLookDirection()
        {
            if (Mouse.current != null && Camera.main != null && playerCharacter != null)
            {
                Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
                Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
                mouseWorldPosition.z = playerCharacter.CenterTransform.position.z;

                Vector2 direction =
                    (Vector2)mouseWorldPosition -
                    (Vector2)playerCharacter.CenterTransform.position;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction.normalized;
                }
            }

            if (playerCharacter != null && playerCharacter.LookDirection != Vector2.zero)
            {
                return playerCharacter.LookDirection.normalized;
            }

            return Vector2.right;
        }

        private HeavySnipeChargeStats CalculateHeavySnipeChargeStats(float chargeRatio)
        {
            chargeRatio = Mathf.Clamp01(chargeRatio);

            HeavySnipeChargeStats stats = new HeavySnipeChargeStats();

            stats.damageMultiplier = EvaluateChargeFloat(
                chargeRatio,
                heavyDamageAt0,
                heavyDamageAt30,
                heavyDamageAt50,
                heavyDamageAt70,
                heavyDamageAt100
            );

            stats.sizeMultiplier = EvaluateChargeFloat(
                chargeRatio,
                heavySizeAt0,
                heavySizeAt30,
                heavySizeAt50,
                heavySizeAt70,
                heavySizeAt100
            );

            stats.rangeBonus = EvaluateChargeFloat(
                chargeRatio,
                heavyRangeBonusAt0,
                heavyRangeBonusAt30,
                heavyRangeBonusAt50,
                heavyRangeBonusAt70,
                heavyRangeBonusAt100
            );

            stats.knockbackMultiplier = Mathf.Lerp(
                heavyKnockbackMultiplierAt0,
                heavyKnockbackMultiplierAt100,
                chargeRatio
            );

            if (chargeRatio >= 0.999f)
            {
                stats.unlimitedPierce = true;
                stats.pierceCount = int.MaxValue;
            }
            else
            {
                stats.unlimitedPierce = false;

                stats.pierceCount = EvaluateChargeInt(
                    chargeRatio,
                    heavyPierceAt0,
                    heavyPierceAt30,
                    heavyPierceAt50,
                    heavyPierceAt70,
                    heavyPierceAt99
                );
            }

            return stats;
        }

        private float EvaluateChargeFloat(
            float chargeRatio,
            float valueAt0,
            float valueAt30,
            float valueAt50,
            float valueAt70,
            float valueAt100)
        {
            if (chargeRatio <= 0.3f)
            {
                return Mathf.Lerp(valueAt0, valueAt30, Mathf.InverseLerp(0f, 0.3f, chargeRatio));
            }

            if (chargeRatio <= 0.5f)
            {
                return Mathf.Lerp(valueAt30, valueAt50, Mathf.InverseLerp(0.3f, 0.5f, chargeRatio));
            }

            if (chargeRatio <= 0.7f)
            {
                return Mathf.Lerp(valueAt50, valueAt70, Mathf.InverseLerp(0.5f, 0.7f, chargeRatio));
            }

            return Mathf.Lerp(valueAt70, valueAt100, Mathf.InverseLerp(0.7f, 1f, chargeRatio));
        }

        private int EvaluateChargeInt(
            float chargeRatio,
            int valueAt0,
            int valueAt30,
            int valueAt50,
            int valueAt70,
            int valueAt99)
        {
            float evaluatedValue;

            if (chargeRatio <= 0.3f)
            {
                evaluatedValue = Mathf.Lerp(valueAt0, valueAt30, Mathf.InverseLerp(0f, 0.3f, chargeRatio));
            }
            else if (chargeRatio <= 0.5f)
            {
                evaluatedValue = Mathf.Lerp(valueAt30, valueAt50, Mathf.InverseLerp(0.3f, 0.5f, chargeRatio));
            }
            else if (chargeRatio <= 0.7f)
            {
                evaluatedValue = Mathf.Lerp(valueAt50, valueAt70, Mathf.InverseLerp(0.5f, 0.7f, chargeRatio));
            }
            else
            {
                evaluatedValue = Mathf.Lerp(valueAt70, valueAt99, Mathf.InverseLerp(0.7f, 0.999f, chargeRatio));
            }

            return Mathf.Max(0, Mathf.RoundToInt(evaluatedValue));
        }

        private SyringeSpecialRuntime BuildSpecialRuntime()
        {
            float additionalPierceFromCharacter = GetPublicFloatProperty(playerCharacter, "AdditionalPierce", 0f);

            int bonusPierce = pierceEnabled
                ? Mathf.RoundToInt(additionalPierceFromCharacter)
                : 0;

            int totalPierceCount = pierceEnabled
                ? Mathf.Max(0, pierceCount + bonusPierce)
                : 0;

            float antibioticBombChance = 0f;

            if (playerCharacter != null)
            {
                antibioticBombChance = Mathf.Max(0f, playerCharacter.AntibioticBombChance);
            }

            float finalExplosionChance = 0f;

            if (explosionEnabled)
            {
                finalExplosionChance = Mathf.Max(finalExplosionChance, specialExplosionChance);
            }

            if (antibioticBombChance > 0f)
            {
                finalExplosionChance = Mathf.Max(finalExplosionChance, antibioticBombChance);
            }

            SyringeSpecialRuntime runtime = new SyringeSpecialRuntime
            {
                slowChance = playerCharacter != null ? playerCharacter.SlowChance : 0f,
                burnChance = playerCharacter != null ? playerCharacter.BurnChance : 0f,
                thermometerEnabled = playerCharacter != null && playerCharacter.HasThermometer,
                reflectCount = playerCharacter != null ? playerCharacter.MouthwashCount : 0,

                poisonEnabled = poisonEnabled,
                poisonDuration = poisonDuration,
                poisonTickInterval = poisonTickInterval,
                poisonTickDamage = poisonTickDamage,

                explosionEnabled = finalExplosionChance > 0f,
                explosionRadius = explosionRadius,
                explosionDamage = explosionDamage,
                explosionChance = finalExplosionChance,

                homingEnabled = homingEnabled,
                homingRange = homingRange,
                homingLerpSpeed = homingLerpSpeed,

                // 핵심 수정:
                // 관통침 증강을 먹었을 때만 기본 침이 관통한다.
                // 기본 pierceCount가 2여도 pierceEnabled가 false면 관통하지 않는다.
                pierceEnabled = pierceEnabled && totalPierceCount > 0,
                pierceCount = totalPierceCount,

                honeyEnabled = honeyEnabled,
                honeyDuration = honeyDuration,
                honeySlowMultiplier = honeySlowMultiplier,

                mosquitoEnabled = mosquitoEnabled,
                mosquitoHealPerHit = mosquitoHealPerHit,
                mosquitoBossHealMultiplier = mosquitoBossHealMultiplier,

                // 침귀환은 그대로 유지.
                // 복귀 중에는 SyringeProjectile의 IsReturnMode 로직에 의해 관통 처리된다.
                returnNeedleEnabled = returnNeedleEnabled,
                returnNeedleSpeedMultiplier = returnNeedleSpeedMultiplier,
                returnNeedleDamageMultiplier = returnNeedleDamageMultiplier,
                returnNeedleArriveDistance = returnNeedleArriveDistance,
                returnNeedleMaxDuration = returnNeedleMaxDuration,

                healingBlocked = lifeBurnEnabled,
                rangeBonus = lifeBurnEnabled ? lifeBurnBonusRange : 0f
            };

            return runtime;
        }

        public Vector2 GetSpreadDirection(Vector2 baseDirection, int projectileIndex, int totalCount)
        {
            if (baseDirection == Vector2.zero)
            {
                baseDirection = Vector2.right;
            }

            baseDirection.Normalize();

            if (totalCount <= 1)
            {
                return baseDirection;
            }

            float totalSpreadAngle = angleBetweenProjectiles * (totalCount - 1);
            totalSpreadAngle = Mathf.Min(totalSpreadAngle, maxTotalSpreadAngle);

            float actualAngleStep = totalSpreadAngle / (totalCount - 1);
            float startAngle = -totalSpreadAngle * 0.5f;
            float angleOffset = startAngle + (actualAngleStep * projectileIndex);

            return RotateVector(baseDirection, angleOffset);
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

        public float GetEffectiveDamage()
        {
            float multiplier = lifeBurnEnabled ? lifeBurnDamageMultiplier : 1f;

            if (playerCharacter != null)
            {
                multiplier *= playerCharacter.DamageMultiplier;
            }

            return damage.Value * multiplier;
        }

        public float GetEffectiveKnockback()
        {
            return knockback.Value;
        }

        public float GetEffectiveSpeed()
        {
            float multiplier = 1f;

            if (playerCharacter != null)
            {
                multiplier *= playerCharacter.ProjectileSpeedMultiplier;
            }

            return speed.Value * multiplier;
        }

        public float GetEffectiveCooldown()
        {
            float attackSpeedMultiplier = 1f;

            if (playerCharacter != null)
            {
                attackSpeedMultiplier = Mathf.Max(0.01f, playerCharacter.AttackSpeedMultiplier);
            }

            return cooldown.Value / attackSpeedMultiplier;
        }

        public int GetEffectiveProjectileCount()
        {
            int totalCount = projectileCount.Value;

            if (playerCharacter != null)
            {
                totalCount += playerCharacter.AdditionalProjectiles;
            }

            if (lifeBurnEnabled)
            {
                totalCount += lifeBurnBonusProjectiles;
            }

            return Mathf.Max(1, totalCount);
        }

        public float GetEffectiveSyringeMaxDistance()
        {
            return Mathf.Max(0.1f, baseSyringeMaxDistance) * GetPlayerRangeMultiplier();
        }

        public SyringeSpecialRuntime GetCurrentSpecialRuntime()
        {
            return BuildSpecialRuntime();
        }

        public int GetActiveSpecialAugmentCount()
        {
            int count = 0;

            if (poisonEnabled) count++;
            if (explosionEnabled) count++;
            if (homingEnabled) count++;
            if (pierceEnabled) count++;
            if (honeyEnabled) count++;
            if (mosquitoEnabled) count++;
            if (returnNeedleEnabled) count++;
            if (acupunctureFormationEnabled) count++;

            return count;
        }

        public float GetCloneDamage()
        {
            // 분신도 현재 플레이어와 같은 기본 침 데미지를 사용한다.
            // 분신 피해 증가 일반 증강은 SyringeCloneController 쪽에서 별도로 곱해진다.
            return GetEffectiveDamage();
        }
        public float GetAcupunctureFormationDamage()
        {
            // 침술진은 특수/전설 증강 효과를 제외하고,
            // 일반 스탯 기반 데미지 강화만 반영한다.
            float multiplier = 1f;

            if (playerCharacter != null)
            {
                multiplier *= playerCharacter.DamageMultiplier;
            }

            return damage.Value * multiplier;
        }

        public float GetAcupunctureFormationKnockback()
        {
            return knockback.Value;
        }

        public float GetAcupunctureFormationSpeed()
        {
            // 침술진은 특수 증강 효과 없이 일반 투사체 속도 강화만 반영한다.
            float multiplier = 1f;

            if (playerCharacter != null)
            {
                multiplier *= playerCharacter.ProjectileSpeedMultiplier;
            }

            return speed.Value * multiplier;
        }

        public int GetAcupunctureFormationProjectileCount()
        {
            // 생명연소 같은 전설 추가 투사체는 제외하고,
            // 기본 무기 성장 + 일반 추가 투사체만 반영한다.
            int totalCount = projectileCount.Value;

            if (playerCharacter != null)
            {
                totalCount += playerCharacter.AdditionalProjectiles;
            }

            return Mathf.Max(1, totalCount);
        }

        public float GetAcupunctureFormationProjectileSizeMultiplier()
        {
            return GetPlayerProjectileSizeMultiplier();
        }

        public float GetAcupunctureFormationMaxDistance()
        {
            return Mathf.Max(0.1f, baseSyringeMaxDistance) * GetPlayerRangeMultiplier();
        }
        public float GetCloneKnockback()
        {
            return GetEffectiveKnockback();
        }

        public float GetCloneSpeed()
        {
            return GetEffectiveSpeed();
        }

        public float GetCloneCooldown()
        {
            return GetEffectiveCooldown();
        }

        public int GetCloneProjectileCount()
        {
            // 분신도 현재 플레이어와 같은 발사체 개수를 사용한다.
            return GetEffectiveProjectileCount();
        }

        public float GetEffectiveProjectileSizeMultiplier()
        {
            return GetPlayerProjectileSizeMultiplier();
        }

        public float GetEffectiveRangeMultiplier()
        {
            return GetPlayerRangeMultiplier();
        }

        private float GetPlayerProjectileSizeMultiplier()
        {
            return GetPublicFloatProperty(playerCharacter, "ProjectileSizeMultiplier", 1f);
        }

        private float GetPlayerRangeMultiplier()
        {
            return GetPublicFloatProperty(playerCharacter, "RangeMultiplier", 1f);
        }

        private float GetPublicFloatProperty(object target, string propertyName, float defaultValue)
        {
            if (target == null)
            {
                return defaultValue;
            }

            PropertyInfo propertyInfo = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public
            );

            if (propertyInfo == null)
            {
                return defaultValue;
            }

            object value = propertyInfo.GetValue(target);

            if (value is float floatValue)
            {
                return floatValue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            return defaultValue;
        }

        public void EnablePoisonAugment() => poisonEnabled = true;
        public void EnableExplosionAugment() => explosionEnabled = true;
        public void EnableHomingAugment() => homingEnabled = true;

        public void EnablePierceAugment()
        {
            pierceEnabled = true;
            pierceCount = Mathf.Max(2, pierceCount);
        }

        public void EnableHoneyAugment() => honeyEnabled = true;
        public void EnableMosquitoAugment() => mosquitoEnabled = true;
        public void EnableReturnNeedleAugment() => returnNeedleEnabled = true;

        public void EnableAcupunctureFormationAugment()
        {
            if (acupunctureFormationEnabled)
            {
                return;
            }

            acupunctureFormationEnabled = true;

            if (playerCharacter != null)
            {
                playerCharacter.AddDashCharge(acupunctureFormationBonusDashCharges);
            }

            if (playerCharacter == null || entityManager == null)
            {
                Debug.LogWarning("[침술진] playerCharacter 또는 entityManager가 없어 침술진 컨트롤러를 생성하지 못했습니다.");
                return;
            }

            acupunctureFormationController = AcupunctureFormationController.Create(
                playerCharacter,
                entityManager,
                this,
                acupunctureFormationLifetime,
                acupunctureFormationNeedleCount,
                acupunctureFormationDamageMultiplier,
                acupunctureFormationVisualScale
            );

            Debug.Log("[침술진] 특수 증강 활성화. 대쉬 횟수 +1, 대쉬 시작 위치에 침술진을 생성합니다.");
        }

        public void AddPierceCount(int amount)
        {
            pierceCount += amount;
            pierceCount = Mathf.Max(0, pierceCount);

            if (pierceCount > 0)
            {
                pierceEnabled = true;
            }
        }

        public bool HasPoisonAugment() => poisonEnabled;
        public bool HasExplosionAugment() => explosionEnabled;
        public bool HasHomingAugment() => homingEnabled;
        public bool HasPierceAugment() => pierceEnabled;
        public bool HasHoneyAugment() => honeyEnabled;
        public bool HasMosquitoAugment() => mosquitoEnabled;
        public bool HasReturnNeedleAugment() => returnNeedleEnabled;
        public bool HasAcupunctureFormationAugment() => acupunctureFormationEnabled;

        public void EnableLifeBurnLegendary() => lifeBurnEnabled = true;
        public bool HasLifeBurnLegendary() => lifeBurnEnabled;

        public void MarkCloneLegendaryTaken() => cloneLegendaryTaken = true;
        public bool HasCloneLegendary() => cloneLegendaryTaken;

        public void EnableHedgehogNeedleLegendary()
        {
            if (hedgehogNeedleEnabled)
            {
                return;
            }

            hedgehogNeedleEnabled = true;

            if (playerCharacter == null || entityManager == null)
            {
                Debug.LogWarning("[고슴도침] playerCharacter 또는 entityManager가 없어 고슴도침을 생성하지 못했습니다.");
                return;
            }

            hedgehogNeedleController = HedgehogNeedleController.Create(
                playerCharacter,
                entityManager,
                this,
                hedgehogNeedleCount,
                hedgehogOrbitRadius,
                hedgehogRotationSpeed,
                hedgehogTouchFireCooldown,
                hedgehogDamageMultiplier
            );

            Debug.Log("[고슴도침] 접촉 반격형 침 결계 생성 완료");
        }

        public bool HasHedgehogNeedleLegendary()
        {
            return hedgehogNeedleEnabled;
        }

        public void EnableHeavySnipeLegendary()
        {
            if (heavySnipeEnabled)
            {
                return;
            }

            heavySnipeEnabled = true;
            isHeavyCharging = false;
            heavyChargeTimer = 0f;
            cursorHeavyChargeRatio = 0f;

            Debug.Log("[대물침] 전설 증강 활성화.");
        }

        public bool HasHeavySnipeLegendary()
        {
            return heavySnipeEnabled;
        }

        public void EnableCursorControlLegendary()
        {
            if (cursorControlEnabled)
            {
                return;
            }

            cursorControlEnabled = true;

            if (playerCharacter == null || entityManager == null)
            {
                Debug.LogWarning("[이기어침] playerCharacter 또는 entityManager가 없어 이기어침을 생성하지 못했습니다.");
                return;
            }

            cursorControlledNeedleController = CursorControlledNeedleController.Create(
                playerCharacter,
                entityManager,
                this,
                cursorNeedleFollowSpeed,
                cursorNeedleHitRadius,
                cursorNeedleDamageMultiplier,
                cursorNeedleDamageInterval,
                cursorNeedleVisualScale,
                cursorNeedleHomingHitRadiusBonus,
                cursorNeedleBackDisplayOffset,
                cursorNeedleBackDisplaySpacing,
                cursorNeedleBackDisplayArcHeight,
                cursorNeedleBackDisplayScale
            );

            Debug.Log("[이기어침] 전설 증강 활성화. 기본 자동 공격을 중지하고, 마우스 포인트를 따라다니는 조종 침을 생성했습니다.");
        }

        public bool HasCursorControlLegendary()
        {
            return cursorControlEnabled;
        }

        // 이기어침 + 대물침 조합용 공개 메서드들
        public float GetCursorHeavyChargeRatio()
        {
            if (!cursorControlEnabled || !heavySnipeEnabled)
            {
                return 0f;
            }

            return Mathf.Clamp01(cursorHeavyChargeRatio);
        }

        public bool IsCursorHeavyChargeActive()
        {
            return cursorControlEnabled && heavySnipeEnabled && cursorHeavyChargeRatio > 0.001f;
        }

        public float GetCursorNeedleHeavyDamageMultiplier()
        {
            if (!cursorControlEnabled || !heavySnipeEnabled)
            {
                return 1f;
            }

            return CalculateHeavySnipeChargeStats(cursorHeavyChargeRatio).damageMultiplier;
        }

        public float GetCursorNeedleHeavySizeMultiplier()
        {
            if (!cursorControlEnabled || !heavySnipeEnabled)
            {
                return 1f;
            }

            return CalculateHeavySnipeChargeStats(cursorHeavyChargeRatio).sizeMultiplier;
        }

        public float GetCursorNeedleHeavyKnockbackMultiplier()
        {
            if (!cursorControlEnabled || !heavySnipeEnabled)
            {
                return 1f;
            }

            return CalculateHeavySnipeChargeStats(cursorHeavyChargeRatio).knockbackMultiplier;
        }

        public bool IsCursorNeedleHeavyPierceUnlimited()
        {
            if (!cursorControlEnabled || !heavySnipeEnabled)
            {
                return false;
            }

            return CalculateHeavySnipeChargeStats(cursorHeavyChargeRatio).unlimitedPierce;
        }

        public int GetCursorNeedleHeavyPierceBonus()
        {
            if (!cursorControlEnabled || !heavySnipeEnabled)
            {
                return 0;
            }

            HeavySnipeChargeStats stats = CalculateHeavySnipeChargeStats(cursorHeavyChargeRatio);

            if (stats.unlimitedPierce)
            {
                return int.MaxValue;
            }

            return stats.pierceCount;
        }

        private struct HeavySnipeChargeStats
        {
            public float damageMultiplier;
            public int pierceCount;
            public bool unlimitedPierce;
            public float sizeMultiplier;
            public float rangeBonus;
            public float knockbackMultiplier;
        }
    }
}