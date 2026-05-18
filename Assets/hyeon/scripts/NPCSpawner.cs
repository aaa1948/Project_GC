using UnityEngine;
using System.Collections;

namespace Vampire
{
    public class NPCSpawner : MonoBehaviour
    {
        [Header("상인 스폰 설정")]
        [SerializeField] private GameObject merchantPrefab;
        [SerializeField] private float spawnInterval = 25f; //  25초마다 초고속 체크!
        [SerializeField] private int maxMerchants = 4;      //  최대 4명까지 도배 허용!

        [Header(" 동적 성장 확률 레버")]
        [SerializeField] private float baseSpawnChance = 0.25f;          //  시작부터 25% 확률
        [SerializeField] private float maxSpawnChance = 0.65f;           // 후반엔 65% 확정
        [SerializeField] private float chanceIncreasePerMinute = 0.03f; //  분당 3%씩 초고속 성장;

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
                    //  현재 스테이지 진행 시간(분 단위)을 계산합니다.
                    float minutesElapsed = Time.timeSinceLevelLoad / 60f;

                    //  실시간 확률 계산: 기본 30% + (지난 시간 * 5%) ➔ 최대 80%까지만 가두기
                    float currentSpawnChance = Mathf.Min(
                        maxSpawnChance,
                        baseSpawnChance + (minutesElapsed * chanceIncreasePerMinute)
                    );

                    // 업그레이드된 동적 확률 주사위 굴리기!
                    if (Random.value < currentSpawnChance)
                    {
                        SpawnMerchant(currentSpawnChance);
                    }
                    else
                    {
                        Debug.Log($"<color=gray>[NPCSpawner]</color> 아저씨 스폰 실패 (현재 동적 확률: {currentSpawnChance * 100f:0.#}%)");
                    }
                }
            }
        }

        private void SpawnMerchant(float currentChance)
        {
            Vector2 safeSpawnPos = GetRandomPositionOutsideScreen();

            Instantiate(merchantPrefab, safeSpawnPos, Quaternion.identity);

            // 콘솔창에서 실시간으로 확률이 성장하는 것을 감시할 수 있게 로그 업그레이드!
            Debug.Log($"<color=magenta>[시스템]</color> <b>{currentChance * 100f:0.#}%</b> 확률을 뚫고 수상한 아저씨 등장! (좌표: {safeSpawnPos})");
        }

        //  플레이어가 진행하는 방향의 화면 밖 시야에 아저씨를 소환하는 스마트 스폰 함수
        private Vector2 GetRandomPositionOutsideScreen()
        {
            Camera cam = Camera.main;

            // 예외 처리: 카메라나 플레이어가 없으면 안전하게 플레이어 주변 랜덤 스폰
            if (cam == null || player == null)
            {
                return (Vector2)transform.position + Random.insideUnitCircle.normalized * 12f;
            }

            // 1. 메인 카메라 해상도 기반 화면 절반 크기 및 여백 계산
            float screenHalfHeight = cam.orthographicSize;
            float screenHalfWidth = screenHalfHeight * cam.aspect;
            float margin = 4f; //  화면에서 살짝 더 벗어난 자연스러운 거리

            Vector2 spawnOffset = Vector2.zero;

            // 2.  [핵심 알고리즘] 플레이어가 멈춰있지 않고 움직이고 있다면 진행 방향을 저격!
            if (player.Velocity.sqrMagnitude > 0.01f)
            {
                Vector2 moveDir = player.Velocity.normalized;

                // 대각선 이동 시 가로/세로 중 주된 방향을 판별하여 시야 정면에 배치
                if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
                {
                    //  가로로 이동 중  전방 화면 밖에 배치하고, 세로는 약간의 무작위 오차 부여
                    spawnOffset.x = Mathf.Sign(moveDir.x) * (screenHalfWidth + margin);
                    spawnOffset.y = Random.Range(-screenHalfHeight, screenHalfHeight);
                }
                else
                {
                    //  세로로 이동 중  전방 화면 위에 배치하고, 가로는 약간의 무작위 오차 부여
                    spawnOffset.x = Random.Range(-screenHalfWidth, screenHalfWidth);
                    spawnOffset.y = Mathf.Sign(moveDir.y) * (screenHalfHeight + margin);
                }
            }
            // 3. 만약 플레이어가 가만히 서 있다면, 기존처럼 사방의 4대 테두리 중 한 곳에 무작위 배치
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

            // 4. 최종적으로 플레이어 캐릭터의 현재 월드 좌표에 오프셋을 더해 화면 전방 밖에 안착!
            return (Vector2)player.transform.position + spawnOffset;
        }
    }
}