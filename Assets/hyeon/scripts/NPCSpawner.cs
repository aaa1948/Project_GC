using UnityEngine;
using System.Collections;

namespace Vampire
{
    public class NPCSpawner : MonoBehaviour
    {
        [Header("상인 스폰 설정")]
        [SerializeField] private GameObject merchantPrefab; // 수상한 아저씨 프리팹
        [SerializeField] private float spawnInterval = 60f; // 몇 초마다 소환할지
        [SerializeField] private int maxMerchants = 1;      // 맵에 동시에 존재할 최대 상인 수

        private Character player;

        private void Start()
        {
            player = FindObjectOfType<Character>();

            if (player != null)
            {
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

                MerchantNPC[] currentMerchants = FindObjectsOfType<MerchantNPC>();

                if (currentMerchants.Length < maxMerchants)
                {
                    SpawnMerchant();
                }
            }
        }

        private void SpawnMerchant()
        {
            // NPCSpawner가 직접 화면 밖 좌표를 계산합니다!
            Vector2 safeSpawnPos = GetRandomPositionOutsideScreen();

            Instantiate(merchantPrefab, safeSpawnPos, Quaternion.identity);

            Debug.Log($"<color=magenta>[시스템]</color> 수상한 아저씨가 나타났습니다! (좌표: {safeSpawnPos})");
        }

        // 메인 카메라의 해상도를 계산해 화면 밖 테두리 좌표를 뽑아내는 핵심 함수
        private Vector2 GetRandomPositionOutsideScreen()
        {
            Camera cam = Camera.main;

            // 만약 카메라를 못 찾으면 임시로 플레이어 주변에 스폰
            if (cam == null) return (Vector2)player.transform.position + Random.insideUnitCircle.normalized * 15f;

            // 카메라의 세로/가로 절반 크기 계산
            float screenHalfHeight = cam.orthographicSize;
            float screenHalfWidth = screenHalfHeight * cam.aspect;

            float margin = 3f; // 화면 밖으로 떨어뜨릴 여백 (숫자가 클수록 더 멀리 스폰됨)

            float randomX = Random.Range(-screenHalfWidth - margin, screenHalfWidth + margin);
            float randomY = Random.Range(-screenHalfHeight - margin, screenHalfHeight + margin);

            // 화면 '안쪽'에 스폰되는 것을 막기 위해 상하좌우 끝자락으로 밀어버림
            if (Random.value > 0.5f)
            {
                randomX = Mathf.Sign(Random.Range(-1f, 1f)) * (screenHalfWidth + margin);
            }
            else
            {
                randomY = Mathf.Sign(Random.Range(-1f, 1f)) * (screenHalfHeight + margin);
            }

            // 카메라의 현재 위치를 기준으로 좌표 반환
            return new Vector2(cam.transform.position.x + randomX, cam.transform.position.y + randomY);
        }
    }
}