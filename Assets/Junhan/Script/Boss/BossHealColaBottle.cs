using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Vampire
{
    public class BossHealColaBottle : IDamageable
    {
        [Header("HP")]
        [Tooltip("콜라병 최대 체력입니다. 패턴 스크립트에서 Object HP 값을 넣으면 런타임에 이 값이 덮어씌워집니다.")]
        [SerializeField] private float maxHp = 60f;

        [Tooltip("콜라병 현재 체력입니다. 런타임 확인용입니다.")]
        [SerializeField] private float currentHp = 60f;

        [Header("References")]
        [Tooltip("콜라병 스프라이트 렌더러입니다. 비워두면 자식 오브젝트에서 자동으로 찾습니다.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("콜라병 피격 판정 콜라이더입니다. 가능하면 자식 Hitbox 오브젝트의 Collider2D를 넣으세요.")]
        [SerializeField] private Collider2D hitbox;

        [Tooltip("콜라병 Rigidbody2D입니다. 비워두면 현재 오브젝트에서 자동으로 찾거나 추가합니다.")]
        [SerializeField] private Rigidbody2D rb;

        [Header("Projectile Hit Detection")]
        [Tooltip("체크하면 투사체가 콜라병을 targetLayer로 인식하지 못해도, 콜라병 쪽에서 직접 투사체 충돌을 감지해 데미지를 받습니다.")]
        [SerializeField] private bool acceptProjectileTriggerDamage = true;

        [Tooltip("체크하면 콜라병을 맞춘 플레이어 투사체를 즉시 사라지게 합니다.")]
        [SerializeField] private bool consumeProjectileOnHit = true;

        [Tooltip("투사체에서 데미지 값을 읽지 못했을 때 사용할 기본 데미지입니다.")]
        [SerializeField] private float fallbackProjectileDamage = 1f;

        [Tooltip("콜라병이 투사체로 받을 데미지 배율입니다. 1이면 투사체 데미지를 그대로 받습니다.")]
        [SerializeField] private float projectileDamageMultiplier = 1f;

        [Header("Hitbox Auto Setup")]
        [Tooltip("체크하면 Awake에서 자식 Collider2D를 우선으로 찾아 Hitbox에 자동 연결합니다.")]
        [SerializeField] private bool autoFindChildHitbox = true;

        [Tooltip("체크하면 Hitbox 자식 오브젝트에 BossHealColaBottleHitboxForwarder를 자동으로 붙입니다.")]
        [SerializeField] private bool autoAddHitboxForwarder = true;

        [Header("Hit Flash")]
        [Tooltip("피격 시 잠깐 바꿀 흰색 머티리얼입니다. 없어도 동작합니다.")]
        [SerializeField] private Material whiteMaterial;

        [Tooltip("기본 머티리얼입니다. 비워두면 시작 시 SpriteRenderer의 현재 머티리얼을 자동 저장합니다.")]
        [SerializeField] private Material defaultMaterial;

        [Tooltip("피격 시 흰색으로 깜빡이는 시간입니다.")]
        [SerializeField] private float hitFlashSeconds = 0.08f;

        [Header("VFX")]
        [Tooltip("콜라병이 파괴될 때 생성할 이펙트 프리팹입니다. 패턴 스크립트에서 지정한 Destroy VFX가 있으면 그 값이 우선됩니다.")]
        [SerializeField] private GameObject destroyVfxPrefab;

        [Header("Debug")]
        [Tooltip("체크하면 콜라병 피격/파괴 로그를 Console에 출력합니다.")]
        [SerializeField] private bool debugLog = false;

        private BossColaBottleHealPattern owner;
        private Coroutine hitFlashCoroutine;
        private bool isDestroyed = false;
        private GameObject runtimeDestroyVfxPrefab;

        private readonly HashSet<int> processedProjectileIds = new HashSet<int>();

        public bool IsDestroyed => isDestroyed;

        private void Awake()
        {
            ResolveReferences();
            SetupPhysics();
            SetupHitboxForwarder();
        }

        private void ResolveReferences()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (hitbox == null)
            {
                if (autoFindChildHitbox)
                {
                    Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

                    foreach (Collider2D candidate in colliders)
                    {
                        if (candidate != null && candidate.gameObject != gameObject)
                        {
                            hitbox = candidate;
                            break;
                        }
                    }
                }

                if (hitbox == null)
                {
                    hitbox = GetComponent<Collider2D>();
                }
            }

            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            if (spriteRenderer != null && defaultMaterial == null)
            {
                defaultMaterial = spriteRenderer.sharedMaterial;
            }
        }

        private void SetupPhysics()
        {
            if (hitbox != null)
            {
                hitbox.isTrigger = true;
            }

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void SetupHitboxForwarder()
        {
            if (!autoAddHitboxForwarder || hitbox == null)
            {
                return;
            }

            BossHealColaBottleHitboxForwarder forwarder =
                hitbox.GetComponent<BossHealColaBottleHitboxForwarder>();

            if (forwarder == null)
            {
                forwarder = hitbox.gameObject.AddComponent<BossHealColaBottleHitboxForwarder>();
            }

            forwarder.Init(this);
        }

        public void Setup(
            BossColaBottleHealPattern ownerPattern,
            float hp,
            GameObject destroyVfxOverride,
            bool enableDebugLog)
        {
            owner = ownerPattern;

            maxHp = Mathf.Max(1f, hp);
            currentHp = maxHp;
            isDestroyed = false;
            debugLog = enableDebugLog;
            processedProjectileIds.Clear();

            runtimeDestroyVfxPrefab = destroyVfxOverride != null ? destroyVfxOverride : destroyVfxPrefab;

            ResolveReferences();
            SetupPhysics();
            SetupHitboxForwarder();
            ShowBottleVisualAndCollision();

            if (debugLog)
            {
                Debug.Log($"[BossHealColaBottle] 생성 완료 / HP={currentHp}");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProcessProjectileHit(other);
        }

        public void ProcessProjectileHit(Collider2D other)
        {
            if (!acceptProjectileTriggerDamage)
            {
                return;
            }

            if (isDestroyed || other == null)
            {
                return;
            }

            Projectile projectile = other.GetComponentInParent<Projectile>();

            if (projectile == null)
            {
                return;
            }

            if (IsProjectileDespawning(projectile))
            {
                return;
            }

            int projectileId = projectile.GetInstanceID();

            if (processedProjectileIds.Contains(projectileId))
            {
                return;
            }

            processedProjectileIds.Add(projectileId);

            float projectileDamage = GetFloatField(projectile, "damage", fallbackProjectileDamage);
            float projectileKnockback = GetFloatField(projectile, "knockback", 0f);
            Vector2 projectileDirection = GetVector2Field(projectile, "direction", Vector2.zero);

            float finalDamage = Mathf.Max(0f, projectileDamage) * Mathf.Max(0f, projectileDamageMultiplier);
            Vector2 finalKnockback = projectileKnockback * projectileDirection;

            if (debugLog)
            {
                Debug.Log($"[BossHealColaBottle] 투사체 직접 감지 / projectile={projectile.name}, damage={finalDamage}");
            }

            TakeDamage(finalDamage, finalKnockback);

            if (consumeProjectileOnHit)
            {
                TryConsumeProjectile(projectile);
            }
        }

        public override void TakeDamage(float damage, Vector2 knockback = default(Vector2))
        {
            if (isDestroyed)
            {
                return;
            }

            if (damage <= 0f)
            {
                return;
            }

            currentHp -= damage;

            if (debugLog)
            {
                Debug.Log($"[BossHealColaBottle] 피격 / damage={damage}, hp={currentHp}/{maxHp}");
            }

            if (currentHp <= 0f)
            {
                DestroyBottle();
                return;
            }

            PlayHitFlash();
        }

        public override void Knockback(Vector2 knockback)
        {
            // 콜라병은 건축물형 오브젝트라 넉백을 받지 않는다.
        }

        public void ForceDestroy()
        {
            if (isDestroyed)
            {
                return;
            }

            isDestroyed = true;
            HideBottleVisualAndCollision();
            Destroy(gameObject);
        }

        private void DestroyBottle()
        {
            if (isDestroyed)
            {
                return;
            }

            isDestroyed = true;

            HideBottleVisualAndCollision();

            if (runtimeDestroyVfxPrefab != null)
            {
                Instantiate(runtimeDestroyVfxPrefab, transform.position, Quaternion.identity);
            }

            if (debugLog)
            {
                Debug.Log("[BossHealColaBottle] 파괴됨");
            }

            owner?.NotifyBottleDestroyed(this);

            Destroy(gameObject);
        }

        private void ShowBottleVisualAndCollision()
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

            foreach (Collider2D collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                    collider.isTrigger = true;
                }
            }

            if (spriteRenderer != null && defaultMaterial != null)
            {
                spriteRenderer.sharedMaterial = defaultMaterial;
            }
        }

        private void HideBottleVisualAndCollision()
        {
            if (hitFlashCoroutine != null)
            {
                StopCoroutine(hitFlashCoroutine);
                hitFlashCoroutine = null;
            }

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

            foreach (Collider2D collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void PlayHitFlash()
        {
            if (spriteRenderer == null || whiteMaterial == null || defaultMaterial == null)
            {
                return;
            }

            if (hitFlashCoroutine != null)
            {
                StopCoroutine(hitFlashCoroutine);
            }

            hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            spriteRenderer.sharedMaterial = whiteMaterial;

            yield return new WaitForSeconds(hitFlashSeconds);

            if (!isDestroyed && spriteRenderer != null && defaultMaterial != null)
            {
                spriteRenderer.sharedMaterial = defaultMaterial;
            }

            hitFlashCoroutine = null;
        }

        private static bool IsProjectileDespawning(Projectile projectile)
        {
            return GetBoolField(projectile, "isDespawning", false);
        }

        private static void TryConsumeProjectile(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            MethodInfo destroyMethod = FindMethod(projectile.GetType(), "DestroyProjectile");

            if (destroyMethod != null)
            {
                destroyMethod.Invoke(projectile, null);
                return;
            }

            projectile.gameObject.SetActive(false);
        }

        private static float GetFloatField(object target, string fieldName, float fallback)
        {
            if (target == null)
            {
                return fallback;
            }

            FieldInfo field = FindField(target.GetType(), fieldName);

            if (field == null || field.FieldType != typeof(float))
            {
                return fallback;
            }

            return (float)field.GetValue(target);
        }

        private static bool GetBoolField(object target, string fieldName, bool fallback)
        {
            if (target == null)
            {
                return fallback;
            }

            FieldInfo field = FindField(target.GetType(), fieldName);

            if (field == null || field.FieldType != typeof(bool))
            {
                return fallback;
            }

            return (bool)field.GetValue(target);
        }

        private static Vector2 GetVector2Field(object target, string fieldName, Vector2 fallback)
        {
            if (target == null)
            {
                return fallback;
            }

            FieldInfo field = FindField(target.GetType(), fieldName);

            if (field == null || field.FieldType != typeof(Vector2))
            {
                return fallback;
            }

            return (Vector2)field.GetValue(target);
        }

        private static FieldInfo FindField(System.Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            System.Type currentType = type;

            while (currentType != null)
            {
                FieldInfo field = currentType.GetField(fieldName, flags);

                if (field != null)
                {
                    return field;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        private static MethodInfo FindMethod(System.Type type, string methodName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            System.Type currentType = type;

            while (currentType != null)
            {
                MethodInfo method = currentType.GetMethod(methodName, flags);

                if (method != null)
                {
                    return method;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }
    }

    public class BossHealColaBottleHitboxForwarder : MonoBehaviour
    {
        private BossHealColaBottle owner;

        public void Init(BossHealColaBottle colaBottle)
        {
            owner = colaBottle;
        }

        private void Awake()
        {
            if (owner == null)
            {
                owner = GetComponentInParent<BossHealColaBottle>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (owner == null)
            {
                owner = GetComponentInParent<BossHealColaBottle>();
            }

            if (owner != null)
            {
                owner.ProcessProjectileHit(other);
            }
        }
    }
}