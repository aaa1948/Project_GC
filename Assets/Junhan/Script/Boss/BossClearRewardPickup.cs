using UnityEngine;

namespace Vampire
{
    [RequireComponent(typeof(Collider2D))]
    public class BossClearRewardPickup : MonoBehaviour
    {
        [Header("Collect Settings")]
        [Tooltip("체크하면 플레이어가 이 보상에 닿았을 때 보상을 획득하고 보스 클리어가 처리됩니다.")]
        [SerializeField] private bool collectByPlayerTouch = true;

        [Tooltip("0보다 크면 플레이어가 이 거리 안으로 들어왔을 때 자동으로 보상을 획득합니다. 0이면 자동 거리 획득을 사용하지 않습니다.")]
        [SerializeField] private float autoCollectDistance = 0f;

        [Tooltip("체크하면 보상을 획득한 뒤 이 오브젝트를 삭제합니다.")]
        [SerializeField] private bool destroyAfterCollect = true;

        [Header("Debug")]
        [Tooltip("체크하면 보스 클리어 보상 획득 로그를 Console에 출력합니다.")]
        [SerializeField] private bool debugCollect = true;

        private BossMonster sourceBoss;
        private Character playerCharacter;
        private bool collected = false;
        private Collider2D rewardCollider;

        public void Init(BossMonster boss)
        {
            sourceBoss = boss;

            if (playerCharacter == null)
            {
                playerCharacter = FindObjectOfType<Character>();
            }
        }

        private void Awake()
        {
            rewardCollider = GetComponent<Collider2D>();

            if (rewardCollider != null)
            {
                rewardCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            if (playerCharacter == null)
            {
                playerCharacter = FindObjectOfType<Character>();
            }
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            if (autoCollectDistance <= 0f)
            {
                return;
            }

            if (playerCharacter == null)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, playerCharacter.transform.position);

            if (distance <= autoCollectDistance)
            {
                Collect();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!collectByPlayerTouch || collected)
            {
                return;
            }

            Character character = other.GetComponentInParent<Character>();

            if (character == null)
            {
                return;
            }

            Collect();
        }

        private void Collect()
        {
            if (collected)
            {
                return;
            }

            collected = true;

            if (debugCollect)
            {
                Debug.Log("[BossClearRewardPickup] Boss clear reward collected.");
            }

            if (sourceBoss != null)
            {
                sourceBoss.NotifyBossClearRewardCollected();
            }
            else
            {
                Debug.LogWarning("[BossClearRewardPickup] Source Boss가 비어 있습니다. 보상은 획득됐지만 클리어 처리가 연결되지 않았을 수 있습니다.");
            }

            if (destroyAfterCollect)
            {
                Destroy(gameObject);
            }
        }
    }
}