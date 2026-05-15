using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    // 엘리트 몬스터 전용 스포너
    //
    // 역할:
    // - 일반 Monster Spawn Table과 분리해서 엘리트 몬스터만 별도 시간표로 스폰한다.
    // - Level 1 Blueprint에 등록된 Monster Blueprints의 flat index를 사용한다.
    // - 별도 프리팹/풀을 만들지 않고, 기존 LevelBlueprint의 몬스터 풀을 그대로 사용한다.
    // - 시간대가 뒤로 갈수록 Spawn Count / Max Alive를 높여 엘리트 등장량을 늘릴 수 있다.
    public class EliteMonsterSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class EliteSpawnPhase
        {
            [Header("Phase Info")]
            [Tooltip("인스펙터에서 구분하기 위한 이름입니다. 예: 2분대 엘리트")]
            public string phaseName = "Elite Phase";

            [Tooltip("이 시간부터 엘리트 스폰을 시작합니다. 초 단위입니다.")]
            public float startTime = 120f;

            [Tooltip("이 시간 이후에는 이 Phase가 작동하지 않습니다. 0 이하로 두면 종료 시간 없이 계속 작동합니다.")]
            public float endTime = 210f;

            [Header("Spawn Timing")]
            [Tooltip("몇 초마다 엘리트를 스폰할지 설정합니다.")]
            public float spawnInterval = 30f;

            [Tooltip("Phase가 처음 시작되는 순간 바로 한 번 스폰할지 여부입니다.")]
            public bool spawnImmediatelyOnPhaseStart = true;

            [Header("Spawn Count")]
            [Tooltip("한 번 스폰될 때 최소 몇 마리 스폰할지 설정합니다.")]
            public int minSpawnCount = 1;

            [Tooltip("한 번 스폰될 때 최대 몇 마리 스폰할지 설정합니다.")]
            public int maxSpawnCount = 1;

            [Tooltip("이 Phase 동안 필드에 동시에 존재할 수 있는 엘리트 최대 수입니다.")]
            public int maxAliveInPhase = 1;

            [Header("Elite Pool")]
            [Tooltip("Level 1 Blueprint의 MonsterIndexTable 기준 flat index입니다. 여기에 등록된 엘리트 중 랜덤으로 스폰합니다.")]
            public int[] eliteMonsterFlatIndices;

            [Tooltip("한 번에 여러 마리 스폰할 때 가능한 한 같은 엘리트가 중복 선택되지 않게 합니다.")]
            public bool avoidDuplicateSelectionInOneWave = true;

            [Header("Extra HP Buff")]
            [Tooltip("추가 HP 보정입니다. 보통 0으로 두면 됩니다. 엘리트 체력 2배는 EliteMonsterBlueprint의 HP Multiplier에서 처리합니다.")]
            public float additionalHpBuff = 0f;
        }

        private struct FlatMonsterEntry
        {
            public int flatIndex;
            public int poolIndex;
            public int blueprintIndex;
            public MonsterBlueprint blueprint;
        }

        [Header("References")]
        [Tooltip("씬의 LevelManager입니다. 비워두면 자동으로 찾습니다.")]
        [SerializeField] private LevelManager levelManager;

        [Header("Global Settings")]
        [SerializeField] private bool spawnEnabled = true;

        [Tooltip("전체 Phase를 통틀어 필드에 동시에 존재할 수 있는 엘리트 최대 수입니다.")]
        [SerializeField] private int globalMaxAliveElites = 8;

        [Tooltip("체크하면 EliteMonsterBlueprint가 아닌 몬스터 인덱스는 스폰하지 않고 경고를 출력합니다.")]
        [SerializeField] private bool requireEliteMonsterBlueprint = true;

        [Tooltip("스폰된 엘리트가 죽거나 비활성화되었는지 매 프레임 정리합니다.")]
        [SerializeField] private bool cleanupInactiveElitesEveryFrame = true;

        [Header("Spawn Phases")]
        [SerializeField] private EliteSpawnPhase[] spawnPhases;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        [Tooltip("게임 시작 시 Level 1 Blueprint의 몬스터 flat index 목록을 Console에 출력합니다.")]
        [SerializeField] private bool logMonsterIndexTableOnStart = true;

        private readonly List<Monster> activeElites = new List<Monster>();

        private float[] phaseTimers;
        private bool[] phaseStarted;

        private bool ready = false;

        private void Awake()
        {
            EnsureRuntimeArrays();

            if (debugLog)
            {
                Debug.Log("[EliteMonsterSpawner] Awake 호출됨 - 컴포넌트 활성 상태 확인 완료", this);
            }
        }

        private IEnumerator Start()
        {
            ResolveReferences();

            // LevelManager.Start()에서 EntityManager.Init()이 끝날 시간을 준다.
            yield return null;
            yield return null;

            ResolveReferences();
            EnsureRuntimeArrays();

            ready = levelManager != null &&
                    levelManager.EntityManager != null &&
                    levelManager.CurrentLevelBlueprint != null;

            if (logMonsterIndexTableOnStart)
            {
                LogMonsterIndexTable();
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[EliteMonsterSpawner] Start 준비 완료 | " +
                    $"Ready: {ready} | " +
                    $"LevelManager: {levelManager != null} | " +
                    $"EntityManager: {(levelManager != null && levelManager.EntityManager != null)} | " +
                    $"LevelBlueprint: {(levelManager != null && levelManager.CurrentLevelBlueprint != null)} | " +
                    $"Phase Count: {(spawnPhases != null ? spawnPhases.Length : 0)}",
                    this
                );
            }
        }

        private void Update()
        {
            if (!spawnEnabled)
            {
                return;
            }

            ResolveReferences();

            if (levelManager == null ||
                levelManager.EntityManager == null ||
                levelManager.CurrentLevelBlueprint == null)
            {
                return;
            }

            ready = true;
            EnsureRuntimeArrays();

            if (cleanupInactiveElitesEveryFrame)
            {
                CleanupActiveElites();
            }

            float currentTime = levelManager.CurrentLevelTime;

            if (spawnPhases == null || spawnPhases.Length == 0)
            {
                return;
            }

            for (int i = 0; i < spawnPhases.Length; i++)
            {
                EliteSpawnPhase phase = spawnPhases[i];

                if (phase == null)
                {
                    continue;
                }

                if (!IsPhaseActive(phase, currentTime))
                {
                    continue;
                }

                if (!phaseStarted[i])
                {
                    phaseStarted[i] = true;
                    phaseTimers[i] = 0f;

                    if (debugLog)
                    {
                        Debug.Log(
                            $"[EliteMonsterSpawner] Phase 시작 | " +
                            $"Phase: {phase.phaseName} | Time: {currentTime:0.##}",
                            this
                        );
                    }

                    if (phase.spawnImmediatelyOnPhaseStart)
                    {
                        TrySpawnFromPhase(phase);
                    }
                }

                phaseTimers[i] += Time.deltaTime;

                float interval = Mathf.Max(0.05f, phase.spawnInterval);

                if (phaseTimers[i] >= interval)
                {
                    TrySpawnFromPhase(phase);
                    phaseTimers[i] = Mathf.Repeat(phaseTimers[i], interval);
                }
            }
        }

        private void ResolveReferences()
        {
            if (levelManager == null)
            {
                levelManager = FindObjectOfType<LevelManager>();
            }
        }

        private void EnsureRuntimeArrays()
        {
            int count = spawnPhases != null ? spawnPhases.Length : 0;

            if (phaseTimers == null || phaseTimers.Length != count)
            {
                phaseTimers = new float[count];
            }

            if (phaseStarted == null || phaseStarted.Length != count)
            {
                phaseStarted = new bool[count];
            }
        }

        private bool IsPhaseActive(EliteSpawnPhase phase, float currentTime)
        {
            if (currentTime < phase.startTime)
            {
                return false;
            }

            if (phase.endTime > 0f && currentTime >= phase.endTime)
            {
                return false;
            }

            return true;
        }

        private void TrySpawnFromPhase(EliteSpawnPhase phase)
        {
            if (!ready)
            {
                if (debugLog)
                {
                    Debug.LogWarning("[EliteMonsterSpawner] 아직 준비되지 않아 엘리트를 스폰하지 않습니다.", this);
                }

                return;
            }

            if (phase.eliteMonsterFlatIndices == null || phase.eliteMonsterFlatIndices.Length == 0)
            {
                if (debugLog)
                {
                    Debug.LogWarning(
                        $"[EliteMonsterSpawner] {phase.phaseName}: eliteMonsterFlatIndices가 비어 있습니다.",
                        this
                    );
                }

                return;
            }

            CleanupActiveElites();

            int currentAlive = activeElites.Count;

            int globalCapacity = Mathf.Max(0, globalMaxAliveElites - currentAlive);
            int phaseCapacity = Mathf.Max(0, phase.maxAliveInPhase - currentAlive);
            int finalCapacity = Mathf.Min(globalCapacity, phaseCapacity);

            if (finalCapacity <= 0)
            {
                if (debugLog)
                {
                    Debug.Log(
                        $"[EliteMonsterSpawner] 스폰 생략 | Phase: {phase.phaseName} | " +
                        $"Alive: {currentAlive} | GlobalMax: {globalMaxAliveElites} | PhaseMax: {phase.maxAliveInPhase}",
                        this
                    );
                }

                return;
            }

            int minCount = Mathf.Max(1, phase.minSpawnCount);
            int maxCount = Mathf.Max(minCount, phase.maxSpawnCount);

            int spawnCount = Random.Range(minCount, maxCount + 1);
            spawnCount = Mathf.Min(spawnCount, finalCapacity);

            List<int> candidateIndices = new List<int>(phase.eliteMonsterFlatIndices);

            int spawnedCount = 0;

            for (int i = 0; i < spawnCount; i++)
            {
                if (candidateIndices.Count <= 0)
                {
                    candidateIndices.AddRange(phase.eliteMonsterFlatIndices);
                }

                int selectedListIndex = Random.Range(0, candidateIndices.Count);
                int selectedFlatIndex = candidateIndices[selectedListIndex];

                if (phase.avoidDuplicateSelectionInOneWave)
                {
                    candidateIndices.RemoveAt(selectedListIndex);
                }

                Monster spawnedElite = TrySpawnEliteByFlatIndex(
                    selectedFlatIndex,
                    phase.additionalHpBuff
                );

                if (spawnedElite != null)
                {
                    spawnedCount++;
                }
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[EliteMonsterSpawner] 엘리트 웨이브 스폰 결과 | " +
                    $"Phase: {phase.phaseName} | Spawned: {spawnedCount}/{spawnCount} | Active: {activeElites.Count}",
                    this
                );
            }
        }

        private Monster TrySpawnEliteByFlatIndex(int flatIndex, float additionalHpBuff)
        {
            FlatMonsterEntry entry;

            if (!TryGetFlatMonsterEntry(flatIndex, out entry))
            {
                Debug.LogWarning($"[EliteMonsterSpawner] 잘못된 flat index입니다: {flatIndex}", this);
                return null;
            }

            if (entry.blueprint == null)
            {
                Debug.LogWarning($"[EliteMonsterSpawner] MonsterBlueprint가 비어 있습니다. flatIndex={flatIndex}", this);
                return null;
            }

            if (requireEliteMonsterBlueprint && !(entry.blueprint is EliteMonsterBlueprint))
            {
                Debug.LogWarning(
                    $"[EliteMonsterSpawner] flatIndex={flatIndex}는 EliteMonsterBlueprint가 아닙니다. " +
                    $"name={entry.blueprint.name}, type={entry.blueprint.GetType().Name}. 스폰하지 않습니다.",
                    this
                );

                return null;
            }

            Monster spawnedMonster = levelManager.EntityManager.SpawnMonsterRandomPosition(
                entry.poolIndex,
                entry.blueprint,
                additionalHpBuff
            );

            if (spawnedMonster == null)
            {
                Debug.LogWarning($"[EliteMonsterSpawner] 엘리트 스폰 실패. flatIndex={flatIndex}", this);
                return null;
            }

            activeElites.Add(spawnedMonster);
            spawnedMonster.OnKilled.AddListener(OnEliteKilled);

            if (debugLog)
            {
                Debug.Log(
                    $"[EliteMonsterSpawner] 엘리트 스폰 완료 | " +
                    $"flatIndex={flatIndex} | poolIndex={entry.poolIndex} | blueprintIndex={entry.blueprintIndex} | " +
                    $"name={entry.blueprint.name} | active={activeElites.Count}",
                    this
                );
            }

            return spawnedMonster;
        }

        private bool TryGetFlatMonsterEntry(int targetFlatIndex, out FlatMonsterEntry result)
        {
            result = default;

            if (levelManager == null || levelManager.CurrentLevelBlueprint == null)
            {
                return false;
            }

            LevelBlueprint levelBlueprint = levelManager.CurrentLevelBlueprint;

            if (levelBlueprint.monsters == null)
            {
                return false;
            }

            int flatIndex = 0;

            for (int poolIndex = 0; poolIndex < levelBlueprint.monsters.Length; poolIndex++)
            {
                LevelBlueprint.MonstersContainer container = levelBlueprint.monsters[poolIndex];

                if (container == null || container.monsterBlueprints == null)
                {
                    continue;
                }

                for (int blueprintIndex = 0; blueprintIndex < container.monsterBlueprints.Length; blueprintIndex++)
                {
                    MonsterBlueprint blueprint = container.monsterBlueprints[blueprintIndex];

                    if (flatIndex == targetFlatIndex)
                    {
                        result = new FlatMonsterEntry
                        {
                            flatIndex = flatIndex,
                            poolIndex = poolIndex,
                            blueprintIndex = blueprintIndex,
                            blueprint = blueprint
                        };

                        return true;
                    }

                    flatIndex++;
                }
            }

            return false;
        }

        private void OnEliteKilled(Monster killedMonster)
        {
            if (killedMonster != null)
            {
                killedMonster.OnKilled.RemoveListener(OnEliteKilled);
            }

            activeElites.Remove(killedMonster);

            if (debugLog)
            {
                Debug.Log(
                    $"[EliteMonsterSpawner] 엘리트 사망 감지 | Active: {activeElites.Count}",
                    this
                );
            }
        }

        private void CleanupActiveElites()
        {
            for (int i = activeElites.Count - 1; i >= 0; i--)
            {
                Monster monster = activeElites[i];

                if (monster == null || !monster.gameObject.activeInHierarchy || monster.HP <= 0f)
                {
                    if (monster != null)
                    {
                        monster.OnKilled.RemoveListener(OnEliteKilled);
                    }

                    activeElites.RemoveAt(i);
                }
            }
        }

        private void LogMonsterIndexTable()
        {
            if (levelManager == null || levelManager.CurrentLevelBlueprint == null)
            {
                Debug.LogWarning(
                    "[EliteMonsterSpawner] Monster Index Table 출력 실패: LevelManager 또는 LevelBlueprint가 없습니다.",
                    this
                );

                return;
            }

            LevelBlueprint levelBlueprint = levelManager.CurrentLevelBlueprint;

            if (levelBlueprint.monsters == null)
            {
                Debug.LogWarning("[EliteMonsterSpawner] LevelBlueprint.monsters가 비어 있습니다.", this);
                return;
            }

            Debug.Log("[EliteMonsterSpawner] ===== Monster Flat Index Table Start =====", this);

            int flatIndex = 0;

            for (int poolIndex = 0; poolIndex < levelBlueprint.monsters.Length; poolIndex++)
            {
                LevelBlueprint.MonstersContainer container = levelBlueprint.monsters[poolIndex];

                if (container == null || container.monsterBlueprints == null)
                {
                    continue;
                }

                for (int blueprintIndex = 0; blueprintIndex < container.monsterBlueprints.Length; blueprintIndex++)
                {
                    MonsterBlueprint blueprint = container.monsterBlueprints[blueprintIndex];

                    string blueprintName = blueprint != null ? blueprint.name : "NULL";
                    string blueprintType = blueprint != null ? blueprint.GetType().Name : "NULL";
                    bool isElite = blueprint is EliteMonsterBlueprint;

                    Debug.Log(
                        $"[EliteIndex] flatIndex={flatIndex} | poolIndex={poolIndex} | " +
                        $"blueprintIndex={blueprintIndex} | name={blueprintName} | type={blueprintType} | elite={isElite}",
                        this
                    );

                    flatIndex++;
                }
            }

            Debug.Log("[EliteMonsterSpawner] ===== Monster Flat Index Table End =====", this);
        }

        private void OnDisable()
        {
            for (int i = activeElites.Count - 1; i >= 0; i--)
            {
                if (activeElites[i] != null)
                {
                    activeElites[i].OnKilled.RemoveListener(OnEliteKilled);
                }
            }

            activeElites.Clear();
        }
    }
}