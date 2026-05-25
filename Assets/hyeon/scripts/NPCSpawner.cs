using UnityEngine;
using System.Collections;

namespace Vampire
{
    public class NPCSpawner : MonoBehaviour
    {
        [Header("상인 스폰 설정")]
        [SerializeField] private GameObject merchantPrefab;
        [SerializeField] private float spawnInterval = 25f;
        [SerializeField] private int maxMerchants = 4;

        [Header("동적 성장 확률 레버")]
        [SerializeField] private float baseSpawnChance = 0.25f;
        [SerializeField] private float maxSpawnChance = 0.65f;
        [SerializeField] private float chanceIncreasePerMinute = 0.03f;

        private Character player;

        private void Start()
        {
            player = FindObjectOfType<Character>();

            if (player != null)
            {
                TrySpawnImmediatelyIfNoMerchant();
                StartCoroutine(SpawnRoutine());
            }
            else
            {
                Debug.LogError("[NPCSpawner] 플레이어를 찾을 수 없습니다!");
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);

                TrySpawnMerchantByChance();
            }
        }

        private void TrySpawnImmediatelyIfNoMerchant()
        {
            MerchantNPC[] currentMerchants = FindObjectsOfType<MerchantNPC>();

            if (currentMerchants.Length == 0)
            {
                TrySpawnMerchantByChance();
            }
        }

        private void TrySpawnMerchantByChance()
        {
            MerchantNPC[] currentMerchants = FindObjectsOfType<MerchantNPC>();

            if (currentMerchants.Length >= maxMerchants)
            {
                return;
            }

            float currentSpawnChance = GetCurrentSpawnChance();

            if (Random.value < currentSpawnChance)
            {
                SpawnMerchant(currentSpawnChance);
            }
            else
            {
                Debug.Log($"<color=gray>[NPCSpawner]</color> 아저씨 스폰 실패 (현재 동적 확률: {currentSpawnChance * 100f:0.#}%)");
            }
        }

        private float GetCurrentSpawnChance()
        {
            float minutesElapsed = Time.timeSinceLevelLoad / 60f;

            return Mathf.Min(
                maxSpawnChance,
                baseSpawnChance + (minutesElapsed * chanceIncreasePerMinute)
            );
        }

        private void SpawnMerchant(float currentChance)
        {
            Vector2 safeSpawnPos = GetRandomPositionOutsideScreen();

            Instantiate(merchantPrefab, safeSpawnPos, Quaternion.identity);

            Debug.Log($"<color=magenta>[시스템]</color> <b>{currentChance * 100f:0.#}%</b> 확률을 뚫고 수상한 아저씨 등장! (좌표: {safeSpawnPos})");
        }

        private Vector2 GetRandomPositionOutsideScreen()
        {
            Camera cam = Camera.main;

            if (cam == null || player == null)
            {
                return (Vector2)transform.position + Random.insideUnitCircle.normalized * 12f;
            }

            float screenHalfHeight = cam.orthographicSize;
            float screenHalfWidth = screenHalfHeight * cam.aspect;
            float margin = 4f;

            Vector2 spawnOffset = Vector2.zero;

            if (player.Velocity.sqrMagnitude > 0.01f)
            {
                Vector2 moveDir = player.Velocity.normalized;

                if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
                {
                    spawnOffset.x = Mathf.Sign(moveDir.x) * (screenHalfWidth + margin);
                    spawnOffset.y = Random.Range(-screenHalfHeight, screenHalfHeight);
                }
                else
                {
                    spawnOffset.x = Random.Range(-screenHalfWidth, screenHalfWidth);
                    spawnOffset.y = Mathf.Sign(moveDir.y) * (screenHalfHeight + margin);
                }
            }
            else
            {
                if (Random.value > 0.5f)
                {
                    spawnOffset.x = Mathf.Sign(Random.Range(-1f, 1f)) * (screenHalfWidth + margin);
                    spawnOffset.y = Random.Range(-screenHalfHeight, screenHalfHeight);
                }
                else
                {
                    spawnOffset.x = Random.Range(-screenHalfWidth, screenHalfWidth);
                    spawnOffset.y = Mathf.Sign(Random.Range(-1f, 1f)) * (screenHalfHeight + margin);
                }
            }

            return (Vector2)player.transform.position + spawnOffset;
        }
    }
}