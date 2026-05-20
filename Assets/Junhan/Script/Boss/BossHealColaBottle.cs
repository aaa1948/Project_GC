using System.Collections;
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

        [Tooltip("콜라병 피격 판정 콜라이더입니다. 가능하면 루트가 아니라 자식 Hitbox 오브젝트의 Collider2D를 넣으세요.")]
        [SerializeField] private Collider2D hitbox;

        [Tooltip("콜라병 Rigidbody2D입니다. 비워두면 현재 오브젝트에서 자동으로 찾거나 추가합니다.")]
        [SerializeField] private Rigidbody2D rb;

        [Header("Hitbox Auto Setup")]
        [Tooltip("체크하면 Awake에서 자식 Collider2D를 우선으로 찾아 Hitbox에 자동 연결합니다.")]
        [SerializeField] private bool autoFindChildHitbox = true;

        [Tooltip("체크하면 Hitbox가 루트 오브젝트에 붙어 있을 때 경고 로그를 출력합니다. 플레이어 투사체 인식을 위해 자식 Hitbox를 권장합니다.")]
        [SerializeField] private bool warnIfHitboxIsRoot = true;

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

        public bool IsDestroyed => isDestroyed;

        private void Awake()
        {
            ResolveReferences();
            SetupPhysics();
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

            if (warnIfHitboxIsRoot && hitbox != null && hitbox.gameObject == gameObject)
            {
                Debug.LogWarning(
                    "[BossHealColaBottle] Hitbox가 루트 오브젝트에 붙어 있습니다. " +
                    "일부 투사체는 Collider의 부모에서 IDamageable을 찾기 때문에, " +
                    "Boss_Heal_ColaBottle 아래에 자식 Hitbox 오브젝트를 만들고 그쪽에 Collider2D를 두는 것을 권장합니다."
                );
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

            runtimeDestroyVfxPrefab = destroyVfxOverride != null ? destroyVfxOverride : destroyVfxPrefab;

            ResolveReferences();
            SetupPhysics();

            if (hitbox != null)
            {
                hitbox.enabled = true;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;

                if (defaultMaterial != null)
                {
                    spriteRenderer.sharedMaterial = defaultMaterial;
                }
            }

            if (debugLog)
            {
                Debug.Log($"[BossHealColaBottle] 생성 완료 / HP={currentHp}");
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

            if (hitbox != null)
            {
                hitbox.enabled = false;
            }

            Destroy(gameObject);
        }

        private void DestroyBottle()
        {
            if (isDestroyed)
            {
                return;
            }

            isDestroyed = true;

            if (hitbox != null)
            {
                hitbox.enabled = false;
            }

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

            if (spriteRenderer != null && defaultMaterial != null)
            {
                spriteRenderer.sharedMaterial = defaultMaterial;
            }

            hitFlashCoroutine = null;
        }
    }
}