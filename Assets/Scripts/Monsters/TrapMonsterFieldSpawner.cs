using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Vampire
{
    // 함정 몬스터를 필드에 일정 수 유지하는 전용 스포너.
    //
    // 게임 시작 시 maxActiveTraps만큼 함정을 랜덤 배치하고,
    // 함정이 죽으면 respawnDelay초 뒤에 다시 랜덤 위치에 스폰한다.
    public class TrapMonsterFieldSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("씬에 있는 LevelManager를 넣어주세요. 비워도 자동으로 찾습니다.")]
        [SerializeField] private LevelManager levelManager;

        [Header("Trap Spawn")]
        [Tooltip("LevelBlueprint의 Monster Settings에서 TrapMonster가 몇 번째 Monsters Element인지 입력합니다.")]
        [SerializeField] private int trapMonsterPoolIndex = 5;

        [Tooltip("스폰할 함정 몬스터 블루프린트입니다.")]
        [SerializeField] private TrapMonsterBlueprint trapMonsterBlueprint;

        [Tooltip("필드에 동시에 존재할 수 있는 최대 함정 수입니다.")]
        [SerializeField] private int maxActiveTraps = 4;

        [Tooltip("함정이 죽은 뒤 새 함정이 다시 스폰되기까지의 시간입니다.")]
        [SerializeField] private float respawnDelay = 60f;

        [Tooltip("게임 시작 시 바로 최대 개수만큼 함정을 스폰합니다.")]
        [SerializeField] private bool spawnOnStart = true;

        [Header("Spawn Position")]
        [Tooltip("플레이어 기준 최소 스폰 거리입니다.")]
        [SerializeField] private float minDistanceFromPlayer = 3f;

        [Tooltip("플레이어 기준 최대 스폰 거리입니다.")]
        [SerializeField] private float maxDistanceFromPlayer = 8f;

        [Tooltip("함정끼리 최소한 이 정도 거리를 두고 배치합니다.")]
        [SerializeField] private float minDistanceBetweenTraps = 2.5f;

        [Tooltip("적절한 위치를 찾기 위해 몇 번까지 재시도할지 설정합니다.")]
        [SerializeField] private int spawnPositionTryCount = 20;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        private EntityManager entityManager;
        private Character playerCharacter;

        private readonly List<Monster> activeTraps = new List<Monster>();
        private readonly List<Coroutine> respawnCoroutines = new List<Coroutine>();

        private MethodInfo spawnMonsterMethod;

        private void Start()
        {
            ResolveReferences();

            if (spawnOnStart)
            {
                SpawnInitialTraps();
            }
        }

        private void ResolveReferences()
        {
            if (levelManager == null)
            {
                levelManager = FindObjectOfType<LevelManager>();
            }

            if (levelManager != null)
            {
                entityManager = levelManager.EntityManager;
            }

            playerCharacter = FindPlayerCharacter();

            if (entityManager != null)
            {
                spawnMonsterMethod = entityManager.GetType().GetMethod(
                    "SpawnMonster",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new System.Type[]
                    {
                        typeof(int),
                        typeof(Vector2),
                        typeof(MonsterBlueprint),
                        typeof(float)
                    },
                    null
                );
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[TrapMonsterFieldSpawner] 참조 확인 | " +
                    $"LevelManager: {levelManager != null} | " +
                    $"EntityManager: {entityManager != null} | " +
                    $"Player: {playerCharacter != null} | " +
                    $"SpawnMonster Method: {spawnMonsterMethod != null}",
                    this
                );
            }
        }

        private void SpawnInitialTraps()
        {
            if (!CanSpawn())
            {
                return;
            }

            int targetCount = Mathf.Max(0, maxActiveTraps);

            for (int i = 0; i < targetCount; i++)
            {
                SpawnOneTrap();
            }
        }

        private bool CanSpawn()
        {
            if (entityManager == null || spawnMonsterMethod == null)
            {
                ResolveReferences();
            }

            if (entityManager == null)
            {
                Debug.LogError("[TrapMonsterFieldSpawner] EntityManager가 없어 함정을 스폰할 수 없습니다.", this);
                return false;
            }

            if (spawnMonsterMethod == null)
            {
                Debug.LogError("[TrapMonsterFieldSpawner] EntityManager의 SpawnMonster 메서드를 찾지 못했습니다.", this);
                return false;
            }

            if (trapMonsterBlueprint == null)
            {
                Debug.LogError("[TrapMonsterFieldSpawner] Trap Monster Blueprint가 비어 있습니다.", this);
                return false;
            }

            if (playerCharacter == null)
            {
                playerCharacter = FindPlayerCharacter();

                if (playerCharacter == null)
                {
                    Debug.LogError("[TrapMonsterFieldSpawner] Player Character를 찾지 못했습니다.", this);
                    return false;
                }
            }

            return true;
        }

        private void SpawnOneTrap()
        {
            if (!CanSpawn())
            {
                return;
            }

            CleanupNullTraps();

            if (activeTraps.Count >= maxActiveTraps)
            {
                if (debugLog)
                {
                    Debug.Log("[TrapMonsterFieldSpawner] 최대 함정 수에 도달해서 스폰하지 않음", this);
                }

                return;
            }

            Vector2 spawnPosition = GetRandomTrapSpawnPosition();

            Monster spawnedMonster = SpawnTrapAtPosition(spawnPosition);

            if (spawnedMonster == null)
            {
                Debug.LogWarning("[TrapMonsterFieldSpawner] 함정 스폰 실패", this);
                return;
            }

            activeTraps.Add(spawnedMonster);
            spawnedMonster.OnKilled.AddListener(OnTrapKilled);

            if (debugLog)
            {
                Debug.Log(
                    $"[TrapMonsterFieldSpawner] 함정 스폰 완료 | " +
                    $"현재 함정 수 {activeTraps.Count}/{maxActiveTraps} | " +
                    $"위치 {spawnPosition}",
                    this
                );
            }
        }

        private Monster SpawnTrapAtPosition(Vector2 spawnPosition)
        {
            if (spawnMonsterMethod == null)
            {
                return null;
            }

            object result = spawnMonsterMethod.Invoke(
                entityManager,
                new object[]
                {
                    trapMonsterPoolIndex,
                    spawnPosition,
                    trapMonsterBlueprint,
                    0f
                }
            );

            return result as Monster;
        }

        private void OnTrapKilled(Monster killedTrap)
        {
            if (killedTrap != null)
            {
                killedTrap.OnKilled.RemoveListener(OnTrapKilled);
            }

            activeTraps.Remove(killedTrap);
            CleanupNullTraps();

            if (debugLog)
            {
                Debug.Log(
                    $"[TrapMonsterFieldSpawner] 함정 사망 감지 | " +
                    $"현재 함정 수 {activeTraps.Count}/{maxActiveTraps} | " +
                    $"{respawnDelay:0.##}초 뒤 재스폰 예약",
                    this
                );
            }

            Coroutine coroutine = StartCoroutine(RespawnAfterDelay());
            respawnCoroutines.Add(coroutine);
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, respawnDelay));

            CleanupNullTraps();

            if (activeTraps.Count < maxActiveTraps)
            {
                SpawnOneTrap();
            }

            // 완료된 코루틴 정리는 엄밀히 필요 없지만 리스트가 계속 커지는 것을 막기 위해 정리한다.
            for (int i = respawnCoroutines.Count - 1; i >= 0; i--)
            {
                if (respawnCoroutines[i] == null)
                {
                    respawnCoroutines.RemoveAt(i);
                }
            }
        }

        private Vector2 GetRandomTrapSpawnPosition()
        {
            Vector2 playerPosition = playerCharacter != null
                ? (Vector2)playerCharacter.transform.position
                : Vector2.zero;

            Vector2 bestPosition = playerPosition + Vector2.right * minDistanceFromPlayer;
            float bestDistanceScore = -1f;

            int tryCount = Mathf.Max(1, spawnPositionTryCount);

            for (int i = 0; i < tryCount; i++)
            {
                Vector2 candidate = GetRandomPointAroundPlayer(playerPosition);

                float nearestTrapDistance = GetNearestTrapDistance(candidate);

                if (nearestTrapDistance >= minDistanceBetweenTraps)
                {
                    return candidate;
                }

                if (nearestTrapDistance > bestDistanceScore)
                {
                    bestDistanceScore = nearestTrapDistance;
                    bestPosition = candidate;
                }
            }

            // 완벽한 위치를 못 찾으면, 그나마 기존 함정과 가장 먼 위치를 사용한다.
            return bestPosition;
        }

        private Vector2 GetRandomPointAroundPlayer(Vector2 playerPosition)
        {
            float minDistance = Mathf.Max(0.1f, minDistanceFromPlayer);
            float maxDistance = Mathf.Max(minDistance, maxDistanceFromPlayer);

            Vector2 direction = Random.insideUnitCircle.normalized;

            if (direction == Vector2.zero)
            {
                direction = Vector2.right;
            }

            float distance = Random.Range(minDistance, maxDistance);

            return playerPosition + direction * distance;
        }

        private float GetNearestTrapDistance(Vector2 position)
        {
            CleanupNullTraps();

            if (activeTraps.Count <= 0)
            {
                return float.MaxValue;
            }

            float nearestDistance = float.MaxValue;

            for (int i = 0; i < activeTraps.Count; i++)
            {
                Monster trap = activeTraps[i];

                if (trap == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(position, trap.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return nearestDistance;
        }

        private void CleanupNullTraps()
        {
            for (int i = activeTraps.Count - 1; i >= 0; i--)
            {
                if (activeTraps[i] == null || !activeTraps[i].gameObject.activeInHierarchy)
                {
                    activeTraps.RemoveAt(i);
                }
            }
        }

        private Character FindPlayerCharacter()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                Character character = playerObject.GetComponent<Character>();

                if (character != null)
                {
                    return character;
                }

                return playerObject.GetComponentInParent<Character>();
            }

            return FindObjectOfType<Character>();
        }

        private void OnDisable()
        {
            for (int i = 0; i < activeTraps.Count; i++)
            {
                if (activeTraps[i] != null)
                {
                    activeTraps[i].OnKilled.RemoveListener(OnTrapKilled);
                }
            }

            activeTraps.Clear();

            for (int i = 0; i < respawnCoroutines.Count; i++)
            {
                if (respawnCoroutines[i] != null)
                {
                    StopCoroutine(respawnCoroutines[i]);
                }
            }

            respawnCoroutines.Clear();
        }
    }
}