using System.Collections;
using UnityEngine;

namespace Vampire
{
    // 특정 시간에 특수 몬스터를 정해진 수만큼 스폰하는 컨트롤러.
    // 예: 150초에 저격수 1마리, 240초에 3마리, 420초에 5마리.
    public class TimedSpecialMonsterSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class TimedSpawnEntry
        {
            [Header("Spawn Timing")]
            [Tooltip("게임 시작 후 몇 초에 스폰할지 설정합니다. 예: 150 = 2분 30초")]
            public float spawnTimeSeconds = 150f;

            [Tooltip("이 시간에 몇 마리를 스폰할지 설정합니다.")]
            public int spawnCount = 1;

            [Tooltip("여러 마리를 스폰할 때 한 마리씩 나오는 간격입니다. 0이면 동시에 스폰됩니다.")]
            public float intervalBetweenSpawns = 0.15f;

            [Header("Monster")]
            [Tooltip("LevelBlueprint의 Monster Settings에서 몇 번째 Monsters Element인지 입력합니다. 예: SniperMonster가 Element 4면 4")]
            public int monsterPoolIndex = 4;

            [Tooltip("스폰할 몬스터 블루프린트입니다. 예: Sniper Monster Blueprint")]
            public MonsterBlueprint monsterBlueprint;

            [Header("Optional")]
            [Tooltip("추가 HP 보정값입니다. 보통 0으로 둡니다.")]
            public float hpBuff = 0f;

            [Tooltip("디버그용 이름입니다. 예: 2분30초 저격수 1마리")]
            public string memo = "Sniper Spawn";
        }

        [Header("References")]
        [Tooltip("씬에 있는 LevelManager를 넣어주세요. EntityManager는 LevelManager 안에서 자동으로 가져옵니다.")]
        [SerializeField] private LevelManager levelManager;

        [Header("Schedule")]
        [SerializeField] private TimedSpawnEntry[] spawnEntries;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        private EntityManager entityManager;
        private float elapsedTime = 0f;
        private bool[] spawnedFlags;

        private void Awake()
        {
            if (spawnEntries == null)
            {
                spawnEntries = new TimedSpawnEntry[0];
            }

            spawnedFlags = new bool[spawnEntries.Length];
        }

        private void Start()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (entityManager == null)
            {
                ResolveReferences();

                if (entityManager == null)
                {
                    return;
                }
            }

            elapsedTime += Time.deltaTime;

            for (int i = 0; i < spawnEntries.Length; i++)
            {
                if (spawnedFlags[i])
                {
                    continue;
                }

                TimedSpawnEntry entry = spawnEntries[i];

                if (entry == null)
                {
                    spawnedFlags[i] = true;
                    continue;
                }

                if (elapsedTime >= entry.spawnTimeSeconds)
                {
                    spawnedFlags[i] = true;
                    StartCoroutine(SpawnEntryRoutine(entry));
                }
            }
        }

        private void ResolveReferences()
        {
            if (levelManager == null)
            {
                levelManager = FindObjectOfType<LevelManager>();

                if (levelManager != null && debugLog)
                {
                    Debug.Log("[TimedSpecialMonsterSpawner] LevelManager를 자동으로 찾았습니다.", this);
                }
            }

            if (levelManager != null)
            {
                entityManager = levelManager.EntityManager;
            }

            if (entityManager == null && debugLog)
            {
                Debug.LogWarning(
                    "[TimedSpecialMonsterSpawner] EntityManager를 아직 찾지 못했습니다. " +
                    "씬의 LevelManager에 EntityManager가 제대로 연결되어 있는지 확인하세요.",
                    this
                );
            }
        }

        private IEnumerator SpawnEntryRoutine(TimedSpawnEntry entry)
        {
            if (entityManager == null)
            {
                ResolveReferences();

                if (entityManager == null)
                {
                    Debug.LogError("[TimedSpecialMonsterSpawner] EntityManager가 없어 특수 몬스터를 스폰할 수 없습니다.", this);
                    yield break;
                }
            }

            if (entry.monsterBlueprint == null)
            {
                Debug.LogError($"[TimedSpecialMonsterSpawner] MonsterBlueprint가 비어 있습니다. Memo: {entry.memo}", this);
                yield break;
            }

            int count = Mathf.Max(1, entry.spawnCount);

            if (debugLog)
            {
                Debug.Log(
                    $"[TimedSpecialMonsterSpawner] 시간표 스폰 시작 | " +
                    $"현재 시간: {FormatTime(elapsedTime)} | " +
                    $"예약 시간: {FormatTime(entry.spawnTimeSeconds)} | " +
                    $"Memo: {entry.memo} | " +
                    $"MonsterPoolIndex: {entry.monsterPoolIndex} | " +
                    $"Blueprint: {entry.monsterBlueprint.name} | " +
                    $"Count: {count}",
                    this
                );
            }

            for (int i = 0; i < count; i++)
            {
                Monster monster = entityManager.SpawnMonsterRandomPosition(
                    entry.monsterPoolIndex,
                    entry.monsterBlueprint,
                    entry.hpBuff
                );

                if (debugLog)
                {
                    string monsterName = monster != null ? monster.name : "NULL";

                    Debug.Log(
                        $"[TimedSpecialMonsterSpawner] 스폰 완료 " +
                        $"({i + 1}/{count}) | Monster: {monsterName} | Memo: {entry.memo}",
                        this
                    );
                }

                if (entry.intervalBetweenSpawns > 0f && i < count - 1)
                {
                    yield return new WaitForSeconds(entry.intervalBetweenSpawns);
                }
            }
        }

        private string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int minutes = totalSeconds / 60;
            int remainSeconds = totalSeconds % 60;

            return $"{minutes:00}:{remainSeconds:00}";
        }
    }
}