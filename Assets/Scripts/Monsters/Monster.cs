using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Vampire
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Monster : IDamageable, ISpatialHashGridClient
    {
        [SerializeField] protected Material defaultMaterial, whiteMaterial, dissolveMaterial;
        [SerializeField] protected ParticleSystem deathParticles;
        [SerializeField] protected GameObject shadow;

        protected BoxCollider2D monsterHitbox;
        protected CircleCollider2D monsterLegsCollider;

        protected int monsterIndex;
        protected MonsterBlueprint monsterBlueprint;
        protected SpriteAnimator monsterSpriteAnimator;
        protected SpriteRenderer monsterSpriteRenderer;
        protected ZPositioner zPositioner;

        protected float currentHealth;
        protected EntityManager entityManager;
        protected Character playerCharacter;
        protected Rigidbody2D rb;

        protected int currWalkSequenceFrame = 0;
        protected bool knockedBack = false;
        protected Coroutine hitAnimationCoroutine = null;
        protected bool alive = true;
        protected Transform centerTransform;

        private Vector3 originalLocalScale = Vector3.one;

        public Transform CenterTransform { get => centerTransform; }
        public UnityEvent<Monster> OnKilled { get; } = new UnityEvent<Monster>();
        public float HP => currentHealth;
        public Vector2 Position => transform.position;
        public Vector2 Size => monsterLegsCollider != null ? monsterLegsCollider.bounds.size : Vector2.one;

        public Dictionary<int, int> ListIndexByCellIndex { get; set; }
        public int QueryID { get; set; } = -1;

        protected virtual void Awake()
        {
            originalLocalScale = transform.localScale;

            rb = GetComponent<Rigidbody2D>();
            monsterLegsCollider = GetComponent<CircleCollider2D>();
            monsterSpriteAnimator = GetComponentInChildren<SpriteAnimator>();
            monsterSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

            zPositioner = gameObject.AddComponent<ZPositioner>();

            if (monsterSpriteRenderer != null)
            {
                monsterHitbox = monsterSpriteRenderer.gameObject.GetComponent<BoxCollider2D>();

                if (monsterHitbox == null)
                {
                    monsterHitbox = monsterSpriteRenderer.gameObject.AddComponent<BoxCollider2D>();
                }

                monsterHitbox.isTrigger = true;
            }
        }

        public virtual void Init(
            EntityManager entityManager,
            Character playerCharacter)
        {
            this.entityManager = entityManager;
            this.playerCharacter = playerCharacter;

            if (zPositioner != null && playerCharacter != null)
            {
                zPositioner.Init(playerCharacter.transform);
            }
        }

        public virtual void Setup(
            int monsterIndex,
            Vector2 position,
            MonsterBlueprint monsterBlueprint,
            float hpBuff = 0)
        {
            this.monsterIndex = monsterIndex;
            this.monsterBlueprint = monsterBlueprint;

            EliteMonsterBlueprint eliteBlueprint = monsterBlueprint as EliteMonsterBlueprint;

            float scaleMultiplier = 1f;

            if (eliteBlueprint != null)
            {
                scaleMultiplier = Mathf.Max(0.1f, eliteBlueprint.scaleMultiplier);
            }

            transform.localScale = originalLocalScale * scaleMultiplier;

            rb.position = position;
            transform.position = position;

            float finalHp = monsterBlueprint.hp + hpBuff;

            if (eliteBlueprint != null)
            {
                finalHp *= Mathf.Max(0.01f, eliteBlueprint.hpMultiplier);
            }

            currentHealth = finalHp;
            alive = true;

            entityManager.LivingMonsters.Add(this);

            if (monsterSpriteAnimator != null &&
                monsterBlueprint.walkSpriteSequence != null &&
                monsterBlueprint.walkSpriteSequence.Length > 0)
            {
                monsterSpriteAnimator.Init(
                    monsterBlueprint.walkSpriteSequence,
                    monsterBlueprint.walkFrameTime,
                    true
                );

                monsterSpriteAnimator.StartAnimating(true);
            }

            if (monsterHitbox != null && monsterSpriteRenderer != null)
            {
                monsterHitbox.enabled = true;
                monsterHitbox.size = monsterSpriteRenderer.bounds.size;
                monsterHitbox.offset = Vector2.up * monsterHitbox.size.y / 2f;
            }

            if (monsterLegsCollider != null && monsterHitbox != null)
            {
                monsterLegsCollider.radius = monsterHitbox.size.x / 2.5f;
            }

            if (centerTransform == null)
            {
                centerTransform = new GameObject("Center Transform").transform;
                centerTransform.SetParent(transform);
            }

            if (monsterHitbox != null)
            {
                centerTransform.position =
                    transform.position + (Vector3)monsterHitbox.offset;
            }
            else
            {
                centerTransform.position = transform.position;
            }

            float spd = Random.Range(
                monsterBlueprint.movespeed - 0.1f,
                monsterBlueprint.movespeed + 0.1f
            );

            spd = Mathf.Max(0.05f, spd);

            if (rb != null)
            {
                rb.drag = monsterBlueprint.acceleration / (spd * spd);
                rb.velocity = Vector2.zero;
            }

            StopAllCoroutines();

            if (eliteBlueprint != null && eliteBlueprint.debugLog)
            {
                Debug.Log(
                    $"[EliteMonster] Spawned | name={monsterBlueprint.name} | " +
                    $"scale={scaleMultiplier} | hp={currentHealth:0.##}"
                );
            }
        }

        protected virtual void Update()
        {
            if (playerCharacter == null || monsterSpriteRenderer == null)
            {
                return;
            }

            monsterSpriteRenderer.flipX =
                ((playerCharacter.transform.position.x - rb.position.x) < 0);
        }

        protected virtual void FixedUpdate()
        {
        }

        public override void Knockback(Vector2 knockback)
        {
            if (rb == null)
            {
                return;
            }

            rb.velocity += knockback * Mathf.Sqrt(rb.drag);
        }

        public override void TakeDamage(
            float damage,
            Vector2 knockback = default(Vector2))
        {
            if (!alive)
            {
                return;
            }

            if (entityManager != null && monsterHitbox != null)
            {
                entityManager.SpawnDamageText(monsterHitbox.transform.position, damage);
            }

            currentHealth -= damage;

            if (hitAnimationCoroutine != null)
            {
                StopCoroutine(hitAnimationCoroutine);
            }

            if (knockback != default(Vector2) && rb != null)
            {
                rb.velocity += knockback * Mathf.Sqrt(rb.drag);
                knockedBack = true;
            }

            if (currentHealth > 0)
            {
                hitAnimationCoroutine = StartCoroutine(HitAnimation());
            }
            else
            {
                StartCoroutine(Killed());
            }
        }

        protected IEnumerator HitAnimation()
        {
            if (monsterSpriteRenderer != null && whiteMaterial != null)
            {
                monsterSpriteRenderer.sharedMaterial = whiteMaterial;
            }

            yield return new WaitForSeconds(0.15f);

            if (monsterSpriteRenderer != null && defaultMaterial != null)
            {
                monsterSpriteRenderer.sharedMaterial = defaultMaterial;
            }

            knockedBack = false;
        }

        public virtual IEnumerator Killed(bool killedByPlayer = true)
        {
            alive = false;

            if (monsterHitbox != null)
            {
                monsterHitbox.enabled = false;
            }

            if (entityManager != null)
            {
                entityManager.LivingMonsters.Remove(this);
            }

            if (killedByPlayer)
            {
                if (playerCharacter != null && playerCharacter.HealOnKill > 0)
                {
                    playerCharacter.GainHealth(playerCharacter.HealOnKill);
                }

                RewardSilver();
                DropLoot();
            }

            if (deathParticles != null)
            {
                deathParticles.Play();
            }

            yield return HitAnimation();

            if (deathParticles != null)
            {
                if (monsterSpriteRenderer != null)
                {
                    monsterSpriteRenderer.enabled = false;
                }

                if (shadow != null)
                {
                    shadow.SetActive(false);
                }

                yield return new WaitForSeconds(deathParticles.main.duration - 0.15f);

                if (monsterSpriteRenderer != null)
                {
                    monsterSpriteRenderer.enabled = true;
                }

                if (shadow != null)
                {
                    shadow.SetActive(true);
                }
            }

            OnKilled.Invoke(this);
            OnKilled.RemoveAllListeners();

            if (entityManager != null)
            {
                entityManager.DespawnMonster(monsterIndex, this, true);
            }
        }

        protected virtual void RewardSilver()
        {
            EliteMonsterBlueprint eliteBlueprint = monsterBlueprint as EliteMonsterBlueprint;

            int rewardCalls = 1;

            if (eliteBlueprint != null)
            {
                rewardCalls = Mathf.Max(1, eliteBlueprint.silverRewardCalls);
            }

            for (int i = 0; i < rewardCalls; i++)
            {
                SilverRunRewarder.RewardMonsterKill(monsterBlueprint);
            }

            if (eliteBlueprint != null && eliteBlueprint.debugLog)
            {
                Debug.Log(
                    $"[EliteMonster] Silver reward calls={rewardCalls} | name={monsterBlueprint.name}"
                );
            }
        }

        protected virtual void DropLoot()
        {
            if (monsterBlueprint.gemLootTable != null &&
                monsterBlueprint.gemLootTable.TryDropLoot(out GemType gemType))
            {
                entityManager.SpawnExpGem((Vector2)transform.position, gemType);
            }

            TryDropCoinsWithStageEventModifier();
            TryDropEliteExtraCoins();
        }

        private void TryDropCoinsWithStageEventModifier()
        {
            int dropAttempts = StageEventRuntimeModifiers.GetCoinDropAttemptCount();
            int droppedCoinCount = 0;

            for (int i = 0; i < dropAttempts; i++)
            {
                if (monsterBlueprint.coinLootTable != null &&
                    monsterBlueprint.coinLootTable.TryDropLoot(out CoinType coinType))
                {
                    entityManager.SpawnCoin((Vector2)transform.position, coinType);
                    droppedCoinCount++;
                }
            }

            if (StageEventRuntimeModifiers.ShouldDropAdditionalCoin())
            {
                for (int i = 0; i < StageEventRuntimeModifiers.AdditionalCoinDropCount; i++)
                {
                    entityManager.SpawnCoin(
                        (Vector2)transform.position,
                        StageEventRuntimeModifiers.AdditionalCoinType
                    );

                    droppedCoinCount++;
                }
            }

            if (StageEventRuntimeModifiers.DebugGoldRush && droppedCoinCount > 0)
            {
                Debug.Log($"[GoldRush] Coin dropped | count={droppedCoinCount}");
            }
        }

        private void TryDropEliteExtraCoins()
        {
            EliteMonsterBlueprint eliteBlueprint = monsterBlueprint as EliteMonsterBlueprint;

            if (eliteBlueprint == null)
            {
                return;
            }

            int extraCoinCount = Mathf.Max(0, eliteBlueprint.guaranteedExtraCoinCount);

            if (extraCoinCount <= 0)
            {
                return;
            }

            float scatterRadius = Mathf.Max(0f, eliteBlueprint.extraCoinScatterRadius);

            for (int i = 0; i < extraCoinCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
                Vector2 spawnPosition = (Vector2)transform.position + randomOffset;

                entityManager.SpawnCoin(
                    spawnPosition,
                    eliteBlueprint.guaranteedExtraCoinType
                );
            }

            if (eliteBlueprint.debugLog)
            {
                Debug.Log(
                    $"[EliteMonster] Extra coins dropped | count={extraCoinCount} | " +
                    $"type={eliteBlueprint.guaranteedExtraCoinType}"
                );
            }
        }
    }
}