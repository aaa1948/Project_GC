using System.Collections;
using UnityEngine;

namespace Vampire
{
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
            [Tooltip("true면 MonsterBlueprint가 들어있는 LevelBlueprint의 poolIndex를 자동으로 찾습니다.")]
            public bool autoResolvePoolIndex = true;

            [Tooltip("autoResolvePoolIndex가 꺼져 있을 때만 사용하는 수동 poolIndex입니다.")]
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
        [SerializeField] private LevelManager levelManager;

        [Header("Schedule")]
        [SerializeField] private TimedSpawnEntry[] spawnEntries;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        private EntityManager entityManager;
        private LevelBlueprint levelBlueprint;

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
            if (entityManager == null || levelBlueprint == null)
            {
                ResolveReferences();

                if (entityManager == null || levelBlueprint == null)
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
                levelBlueprint = levelManager.CurrentLevelBlueprint;
            }

            if (entityManager == null && debugLog)
            {
                Debug.LogWarning(
                    "[TimedSpecialMonsterSpawner] EntityManager를 아직 찾지 못했습니다. " +
                    "씬의 LevelManager에 EntityManager가 제대로 연결되어 있는지 확인하세요.",
                    this
                );
            }

            if (levelBlueprint == null && debugLog)
            {
                Debug.LogWarning(
                    "[TimedSpecialMonsterSpawner] LevelBlueprint를 아직 찾지 못했습니다. " +
                    "LevelManager 초기화 순서를 확인하세요.",
                    this
                );
            }
        }

        private IEnumerator SpawnEntryRoutine(TimedSpawnEntry entry)
        {
            if (entityManager == null || levelBlueprint == null)
            {
                ResolveReferences();

                if (entityManager == null || levelBlueprint == null)
                {
                    Debug.LogError(
                        "[TimedSpecialMonsterSpawner] EntityManager 또는 LevelBlueprint가 없어 특수 몬스터를 스폰할 수 없습니다.",
                        this
                    );

                    yield break;
                }
            }

            if (entry.monsterBlueprint == null)
            {
                Debug.LogError(
                    $"[TimedSpecialMonsterSpawner] MonsterBlueprint가 비어 있습니다. Memo: {entry.memo}",
                    this
                );

                yield break;
            }

            int resolvedPoolIndex = entry.monsterPoolIndex;

            if (entry.autoResolvePoolIndex)
            {
                if (!TryFindPoolIndexByBlueprint(entry.monsterBlueprint, out resolvedPoolIndex))
                {
                    Debug.LogError(
                        $"[TimedSpecialMonsterSpawner] 자동 poolIndex 탐색 실패 | " +
                        $"Blueprint={entry.monsterBlueprint.name} | Memo={entry.memo}\n" +
                        $"Level 1 Blueprint의 Monster Settings에 해당 Blueprint가 등록되어 있는지 확인하세요.",
                        this
                    );

                    yield break;
                }
            }

            if (!IsValidPoolIndex(resolvedPoolIndex))
            {
                Debug.LogError(
                    $"[TimedSpecialMonsterSpawner] 잘못된 poolIndex입니다. " +
                    $"PoolIndex={resolvedPoolIndex} | Blueprint={entry.monsterBlueprint.name} | Memo={entry.memo}",
                    this
                );

                yield break;
            }

            int count = Mathf.Max(1, entry.spawnCount);

            if (debugLog)
            {
                string prefabName = levelBlueprint.monsters[resolvedPoolIndex].monstersPrefab != null
                    ? levelBlueprint.monsters[resolvedPoolIndex].monstersPrefab.name
                    : "NULL PREFAB";

                Debug.Log(
                    $"[TimedSpecialMonsterSpawner] 시간표 스폰 시작 | " +
                    $"현재 시간: {FormatTime(elapsedTime)} | " +
                    $"예약 시간: {FormatTime(entry.spawnTimeSeconds)} | " +
                    $"Memo: {entry.memo} | " +
                    $"RequestedPoolIndex: {entry.monsterPoolIndex} | " +
                    $"ResolvedPoolIndex: {resolvedPoolIndex} | " +
                    $"Prefab: {prefabName} | " +
                    $"Blueprint: {entry.monsterBlueprint.name} | " +
                    $"Count: {count}",
                    this
                );
            }

            for (int i = 0; i < count; i++)
            {
                Monster monster = entityManager.SpawnMonsterRandomPosition(
                    resolvedPoolIndex,
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

        private bool TryFindPoolIndexByBlueprint(MonsterBlueprint targetBlueprint, out int poolIndex)
        {
            poolIndex = -1;

            if (levelBlueprint == null || levelBlueprint.monsters == null || targetBlueprint == null)
            {
                return false;
            }

            for (int i = 0; i < levelBlueprint.monsters.Length; i++)
            {
                LevelBlueprint.MonstersContainer container = levelBlueprint.monsters[i];

                if (container == null || container.monsterBlueprints == null)
                {
                    continue;
                }

                for (int j = 0; j < container.monsterBlueprints.Length; j++)
                {
                    if (container.monsterBlueprints[j] == targetBlueprint)
                    {
                        poolIndex = i;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsValidPoolIndex(int poolIndex)
        {
            if (levelBlueprint == null || levelBlueprint.monsters == null)
            {
                return false;
            }

            if (poolIndex < 0 || poolIndex >= levelBlueprint.monsters.Length)
            {
                return false;
            }

            LevelBlueprint.MonstersContainer container = levelBlueprint.monsters[poolIndex];

            if (container == null || container.monstersPrefab == null)
            {
                return false;
            }

            return true;
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