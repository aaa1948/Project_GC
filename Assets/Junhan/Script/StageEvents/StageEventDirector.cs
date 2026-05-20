using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    public class StageEventDirector : MonoBehaviour
    {
        [System.Serializable]
        public class EventStartTimeRange
        {
            [Tooltip("이벤트가 시작될 수 있는 최소 시간입니다. 단위는 분입니다. 예: 2 = 2분")]
            public float minStartMinute = 2f;

            [Tooltip("이벤트가 시작될 수 있는 최대 시간입니다. 단위는 분입니다. 예: 4 = 4분")]
            public float maxStartMinute = 4f;

            public float GetRandomStartTimeSeconds()
            {
                float min = Mathf.Min(minStartMinute, maxStartMinute);
                float max = Mathf.Max(minStartMinute, maxStartMinute);

                return Random.Range(min, max) * 60f;
            }
        }

        [System.Serializable]
        public class MonsterSurgeEvent
        {
            [Header("Event Info")]
            [Tooltip("이벤트 이름입니다. UI 알림과 디버그 로그에 표시됩니다.")]
            public string eventName = "감염 증식";

            [Tooltip("체크되어 있으면 이 이벤트가 발동됩니다.")]
            public bool enabled = true;

            [Header("Timing")]
            [Tooltip("랜덤 시간 범위를 사용하지 않을 때 사용할 고정 시작 시간입니다. 단위는 초입니다.")]
            public float startTime = 60f;

            [Tooltip("이벤트 지속 시간입니다. 단위는 초입니다.")]
            public float duration = 15f;

            [Tooltip("체크하면 아래 Start Time Ranges 중 하나를 골라 랜덤 시간에 이벤트를 시작합니다.")]
            public bool useRandomStartTime = true;

            [Tooltip("이벤트 시작 시간 후보 범위입니다. 단위는 분입니다. 예: 2~4분, 4~6분처럼 여러 개 추가할 수 있습니다.")]
            public List<EventStartTimeRange> startTimeRanges = new List<EventStartTimeRange>();

            [Header("Spawn Table Surge")]
            [Tooltip("현재 시간대의 Monster Spawn Table 비율을 그대로 따라가며, 스폰량을 몇 배로 늘릴지 정합니다. 2면 기존 스폰량만큼 추가 스폰되어 총 2배 느낌입니다.")]
            public float spawnMultiplier = 2f;

            [Tooltip("기본 스폰률이 너무 낮은 구간에서도 이벤트 체감이 나도록 하는 최소 기준 스폰률입니다.")]
            public float minimumReferenceSpawnRate = 1f;

            [Header("Runtime")]
            [HideInInspector] public bool started;
            [HideInInspector] public bool finished;
            [HideInInspector] public float resolvedStartTime;
            [HideInInspector] public float spawnAccumulator;

            public float EndTime => resolvedStartTime + duration;
        }

        [System.Serializable]
        public class GoldRushEvent
        {
            [Header("Event Info")]
            [Tooltip("이벤트 이름입니다. UI 알림과 디버그 로그에 표시됩니다.")]
            public string eventName = "골드 러시";

            [Tooltip("체크되어 있으면 이 이벤트가 발동됩니다.")]
            public bool enabled = true;

            [Header("Timing")]
            [Tooltip("랜덤 시간 범위를 사용하지 않을 때 사용할 고정 시작 시간입니다. 단위는 초입니다.")]
            public float startTime = 30f;

            [Tooltip("이벤트 지속 시간입니다. 단위는 초입니다.")]
            public float duration = 15f;

            [Tooltip("체크하면 아래 Start Time Ranges 중 하나를 골라 랜덤 시간에 이벤트를 시작합니다.")]
            public bool useRandomStartTime = true;

            [Tooltip("이벤트 시작 시간 후보 범위입니다. 단위는 분입니다. 예: 2~4분, 4~6분처럼 여러 개 추가할 수 있습니다.")]
            public List<EventStartTimeRange> startTimeRanges = new List<EventStartTimeRange>();

            [Header("Guaranteed Gold Drop")]
            [Tooltip("골드러쉬 중 일반 몬스터 처치 시 강제로 드랍할 코인 종류입니다. 5원짜리 골드는 Gold5입니다.")]
            public CoinType guaranteedCoinType = CoinType.Gold5;

            [Tooltip("골드러쉬 중 일반 몬스터 1마리당 강제로 드랍할 코인 개수입니다.")]
            public int guaranteedCoinCount = 1;

            [Tooltip("체크하면 EliteMonsterBlueprint가 아닌 일반 몬스터에게만 강제 골드 드랍을 적용합니다.")]
            public bool applyOnlyToNormalMonsters = true;

            [Tooltip("체크하면 골드러쉬 중 기존 coinLootTable 확률 드랍은 무시하고, 강제 골드만 드랍합니다.")]
            public bool suppressOriginalCoinDrops = true;

            [Header("Runtime")]
            [HideInInspector] public bool started;
            [HideInInspector] public bool finished;
            [HideInInspector] public float resolvedStartTime;

            public float EndTime => resolvedStartTime + duration;
        }

        [System.Serializable]
        public class AcidSecretionEvent
        {
            [Header("Event Info")]
            [Tooltip("이벤트 이름입니다. UI 알림과 디버그 로그에 표시됩니다.")]
            public string eventName = "위산분비";

            [Tooltip("체크되어 있으면 이 이벤트가 발동됩니다.")]
            public bool enabled = true;

            [Header("Timing")]
            [Tooltip("랜덤 시간 범위를 사용하지 않을 때 사용할 고정 시작 시간입니다. 단위는 초입니다.")]
            public float startTime = 45f;

            [Tooltip("이벤트 지속 시간입니다. 단위는 초입니다.")]
            public float duration = 15f;

            [Tooltip("체크하면 아래 Start Time Ranges 중 하나를 골라 랜덤 시간에 이벤트를 시작합니다.")]
            public bool useRandomStartTime = true;

            [Tooltip("이벤트 시작 시간 후보 범위입니다. 단위는 분입니다. 예: 2~4분, 4~6분처럼 여러 개 추가할 수 있습니다.")]
            public List<EventStartTimeRange> startTimeRanges = new List<EventStartTimeRange>();

            [Header("Acid Puddle")]
            [Tooltip("필드에 생성할 산성판 프리팹입니다.")]
            public GameObject acidPuddlePrefab;

            [Tooltip("산성판 웨이브가 생성되는 간격입니다. 단위는 초입니다.")]
            public float puddleSpawnInterval = 1f;

            [Tooltip("한 번에 생성할 산성판 개수입니다.")]
            public int puddlesPerWave = 3;

            [Tooltip("동시에 유지될 수 있는 산성판 최대 개수입니다.")]
            public int maxActivePuddles = 12;

            [Tooltip("플레이어로부터 산성판이 생성될 최소 거리입니다.")]
            public float minSpawnDistanceFromPlayer = 1.2f;

            [Tooltip("플레이어로부터 산성판이 생성될 최대 거리입니다.")]
            public float maxSpawnDistanceFromPlayer = 6f;

            [Tooltip("산성판 유지 시간입니다. 단위는 초입니다.")]
            public float puddleLifeTime = 6f;

            [Tooltip("산성판 틱당 데미지입니다.")]
            public float puddleDamagePerTick = 3f;

            [Tooltip("산성판 데미지 틱 간격입니다. 단위는 초입니다.")]
            public float puddleTickInterval = 0.5f;

            [Tooltip("산성판 크기 배율입니다.")]
            public float puddleScale = 1.6f;

            [Tooltip("산성판이 실제 데미지를 주기 전 경고 시간입니다. 단위는 초입니다.")]
            public float puddleWarningDuration = 0.4f;

            [Tooltip("체크하면 플레이어가 산성판에 들어가는 즉시 데미지 틱을 발생시킵니다.")]
            public bool damageImmediatelyOnEnter = false;

            [Header("Acid Slime Spawn")]
            [Tooltip("위산 슬라임으로 사용할 몬스터 flatIndex 목록입니다. 아직 위산 슬라임이 없다면 테스트용으로 저등급 몬스터 index를 넣어도 됩니다.")]
            public List<int> acidSlimeMonsterIndices = new List<int>();

            [Tooltip("위산 이벤트 중 추가 슬라임 스폰량 배수입니다.")]
            public float acidSlimeExtraSpawnMultiplier = 3f;

            [Tooltip("기본 스폰률이 너무 낮은 구간에서도 위산 슬라임이 나오도록 하는 최소 기준 스폰률입니다.")]
            public float acidSlimeMinimumReferenceSpawnRate = 1f;

            [Tooltip("위산 슬라임의 HP 보정값입니다. 기존 SpawnRandomMonsterFromFlatIndexList 로직을 그대로 사용합니다.")]
            public float acidSlimeHpMultiplier = 1f;

            [Header("Reward")]
            [Tooltip("이벤트 종료 시 보상을 지급할지 여부입니다.")]
            public bool rewardOnEnd = true;

            [Tooltip("체크하면 이벤트 종료 시 임시 아이템 보상용 Chest를 생성합니다.")]
            public bool spawnChestAsTemporaryItemReward = true;

            [Header("Runtime")]
            [HideInInspector] public bool started;
            [HideInInspector] public bool finished;
            [HideInInspector] public bool rewarded;
            [HideInInspector] public float resolvedStartTime;
            [HideInInspector] public float puddleSpawnTimer;
            [HideInInspector] public float acidSlimeAccumulator;
            [HideInInspector] public List<GameObject> activePuddles = new List<GameObject>();

            public float EndTime => resolvedStartTime + duration;
        }

        [Header("References")]
        [SerializeField] private LevelManager levelManager;

        [Tooltip("이벤트 시작 UI 알림을 담당하는 컴포넌트입니다. 비워두면 Debug.Log만 출력됩니다.")]
        [SerializeField] private StageEventToastUI eventToastUI;

        [Header("UI Message")]
        [Tooltip("{0} 위치에 이벤트 이름이 들어갑니다. 예: 골드 러시 이벤트가 시작됐습니다!")]
        [SerializeField] private string eventStartMessageFormat = "{0} 이벤트가 시작됐습니다!";

        [Header("1. Monster Surge Events")]
        [SerializeField] private List<MonsterSurgeEvent> monsterSurgeEvents = new List<MonsterSurgeEvent>();

        [Header("2. Gold Rush Events")]
        [SerializeField] private List<GoldRushEvent> goldRushEvents = new List<GoldRushEvent>();

        [Header("3. Acid Secretion Events")]
        [SerializeField] private List<AcidSecretionEvent> acidSecretionEvents = new List<AcidSecretionEvent>();

        [Header("Debug")]
        [SerializeField] private bool logEventState = true;
        [SerializeField] private bool logMonsterIndexTableOnStart = false;
        [SerializeField] private bool logExtraSpawn = false;
        [SerializeField] private bool logGoldRushModifier = false;
        [SerializeField] private bool logAcidEvent = false;

        private void Start()
        {
            if (levelManager == null)
            {
                levelManager = FindObjectOfType<LevelManager>();
            }

            if (eventToastUI == null)
            {
                eventToastUI = FindObjectOfType<StageEventToastUI>();
            }

            PrepareAllEventRuntimeValues();

            if (levelManager != null && logMonsterIndexTableOnStart)
            {
                levelManager.LogMonsterIndexTable();
            }
        }

        private void Update()
        {
            if (levelManager == null)
            {
                return;
            }

            float currentTime = levelManager.CurrentLevelTime;

            StageEventRuntimeModifiers.ResetCoinModifiers();

            foreach (MonsterSurgeEvent surgeEvent in monsterSurgeEvents)
            {
                UpdateMonsterSurgeEvent(surgeEvent, currentTime);
            }

            foreach (GoldRushEvent goldRushEvent in goldRushEvents)
            {
                UpdateGoldRushEvent(goldRushEvent, currentTime);
            }

            foreach (AcidSecretionEvent acidEvent in acidSecretionEvents)
            {
                UpdateAcidSecretionEvent(acidEvent, currentTime);
            }
        }

        private void PrepareAllEventRuntimeValues()
        {
            foreach (MonsterSurgeEvent surgeEvent in monsterSurgeEvents)
            {
                if (surgeEvent == null)
                {
                    continue;
                }

                surgeEvent.started = false;
                surgeEvent.finished = false;
                surgeEvent.spawnAccumulator = 0f;
                surgeEvent.resolvedStartTime = ResolveStartTime(
                    surgeEvent.startTime,
                    surgeEvent.useRandomStartTime,
                    surgeEvent.startTimeRanges
                );
            }

            foreach (GoldRushEvent goldRushEvent in goldRushEvents)
            {
                if (goldRushEvent == null)
                {
                    continue;
                }

                goldRushEvent.started = false;
                goldRushEvent.finished = false;
                goldRushEvent.resolvedStartTime = ResolveStartTime(
                    goldRushEvent.startTime,
                    goldRushEvent.useRandomStartTime,
                    goldRushEvent.startTimeRanges
                );
            }

            foreach (AcidSecretionEvent acidEvent in acidSecretionEvents)
            {
                if (acidEvent == null)
                {
                    continue;
                }

                acidEvent.started = false;
                acidEvent.finished = false;
                acidEvent.rewarded = false;
                acidEvent.puddleSpawnTimer = 0f;
                acidEvent.acidSlimeAccumulator = 0f;
                acidEvent.resolvedStartTime = ResolveStartTime(
                    acidEvent.startTime,
                    acidEvent.useRandomStartTime,
                    acidEvent.startTimeRanges
                );

                if (acidEvent.activePuddles == null)
                {
                    acidEvent.activePuddles = new List<GameObject>();
                }
                else
                {
                    acidEvent.activePuddles.Clear();
                }
            }
        }

        private float ResolveStartTime(
            float fallbackStartTime,
            bool useRandomStartTime,
            List<EventStartTimeRange> ranges)
        {
            if (!useRandomStartTime || ranges == null || ranges.Count == 0)
            {
                return Mathf.Max(0f, fallbackStartTime);
            }

            EventStartTimeRange selectedRange = ranges[Random.Range(0, ranges.Count)];

            if (selectedRange == null)
            {
                return Mathf.Max(0f, fallbackStartTime);
            }

            return Mathf.Max(0f, selectedRange.GetRandomStartTimeSeconds());
        }

        private void UpdateMonsterSurgeEvent(MonsterSurgeEvent surgeEvent, float currentTime)
        {
            if (surgeEvent == null || !surgeEvent.enabled || surgeEvent.finished)
            {
                return;
            }

            if (currentTime < surgeEvent.resolvedStartTime)
            {
                return;
            }

            if (!surgeEvent.started)
            {
                surgeEvent.started = true;
                surgeEvent.spawnAccumulator = 0f;
                ShowEventStartedUI(surgeEvent.eventName);

                if (logEventState)
                {
                    Debug.Log(
                        $"[StageEvent] Start: {surgeEvent.eventName} | " +
                        $"time={currentTime:F1}s | " +
                        $"planned={surgeEvent.resolvedStartTime:F1}s | " +
                        $"duration={surgeEvent.duration:F1}s"
                    );
                }
            }

            if (currentTime >= surgeEvent.EndTime)
            {
                surgeEvent.finished = true;

                if (logEventState)
                {
                    Debug.Log($"[StageEvent] End: {surgeEvent.eventName} | time={currentTime:F1}s");
                }

                return;
            }

            TickMonsterSurgeSpawn(surgeEvent);
        }

        private void TickMonsterSurgeSpawn(MonsterSurgeEvent surgeEvent)
        {
            float baseSpawnRate = levelManager.GetCurrentBaseMonsterSpawnRate();
            float referenceSpawnRate = Mathf.Max(baseSpawnRate, surgeEvent.minimumReferenceSpawnRate);

            float extraMultiplier = Mathf.Max(0f, surgeEvent.spawnMultiplier - 1f);
            float extraSpawnRate = referenceSpawnRate * extraMultiplier;

            if (extraSpawnRate <= 0f)
            {
                return;
            }

            surgeEvent.spawnAccumulator += Time.deltaTime * extraSpawnRate;

            while (surgeEvent.spawnAccumulator >= 1f)
            {
                levelManager.SpawnMonsterFromCurrentSpawnTable();
                surgeEvent.spawnAccumulator -= 1f;

                if (logExtraSpawn)
                {
                    Debug.Log(
                        $"[StageEvent] Monster Surge Extra Spawn | " +
                        $"baseRate={baseSpawnRate:F2} | " +
                        $"multiplier={surgeEvent.spawnMultiplier:F2} | " +
                        $"extraRate={extraSpawnRate:F2}"
                    );
                }
            }
        }

        private void UpdateGoldRushEvent(GoldRushEvent goldRushEvent, float currentTime)
        {
            if (goldRushEvent == null || !goldRushEvent.enabled || goldRushEvent.finished)
            {
                return;
            }

            if (currentTime < goldRushEvent.resolvedStartTime)
            {
                return;
            }

            if (!goldRushEvent.started)
            {
                goldRushEvent.started = true;
                ShowEventStartedUI(goldRushEvent.eventName);

                if (logEventState)
                {
                    Debug.Log(
                        $"[StageEvent] Start: {goldRushEvent.eventName} | " +
                        $"time={currentTime:F1}s | " +
                        $"planned={goldRushEvent.resolvedStartTime:F1}s | " +
                        $"duration={goldRushEvent.duration:F1}s"
                    );
                }
            }

            if (currentTime >= goldRushEvent.EndTime)
            {
                goldRushEvent.finished = true;

                if (logEventState)
                {
                    Debug.Log($"[StageEvent] End: {goldRushEvent.eventName} | time={currentTime:F1}s");
                }

                return;
            }

            StageEventRuntimeModifiers.ApplyGoldRushForcedCoinDrop(
                goldRushEvent.guaranteedCoinType,
                goldRushEvent.guaranteedCoinCount,
                goldRushEvent.applyOnlyToNormalMonsters,
                goldRushEvent.suppressOriginalCoinDrops,
                logGoldRushModifier
            );

            if (logGoldRushModifier)
            {
                Debug.Log(
                    $"[StageEvent] GoldRush Active | " +
                    $"coin={StageEventRuntimeModifiers.ForcedGoldRushCoinType} | " +
                    $"count={StageEventRuntimeModifiers.ForcedGoldRushCoinCount} | " +
                    $"normalOnly={StageEventRuntimeModifiers.ForceGoldRushOnlyNormalMonsters} | " +
                    $"suppressOriginal={StageEventRuntimeModifiers.SuppressOriginalCoinDropsDuringGoldRush}"
                );
            }
        }

        private void UpdateAcidSecretionEvent(AcidSecretionEvent acidEvent, float currentTime)
        {
            if (acidEvent == null || !acidEvent.enabled || acidEvent.finished)
            {
                return;
            }

            if (currentTime < acidEvent.resolvedStartTime)
            {
                return;
            }

            if (!acidEvent.started)
            {
                acidEvent.started = true;
                acidEvent.puddleSpawnTimer = 0f;
                acidEvent.acidSlimeAccumulator = 0f;
                ShowEventStartedUI(acidEvent.eventName);

                if (logEventState)
                {
                    Debug.Log(
                        $"[StageEvent] Start: {acidEvent.eventName} | " +
                        $"time={currentTime:F1}s | " +
                        $"planned={acidEvent.resolvedStartTime:F1}s | " +
                        $"duration={acidEvent.duration:F1}s"
                    );
                }
            }

            if (currentTime >= acidEvent.EndTime)
            {
                acidEvent.finished = true;

                if (logEventState)
                {
                    Debug.Log($"[StageEvent] End: {acidEvent.eventName} | time={currentTime:F1}s");
                }

                if (acidEvent.rewardOnEnd && !acidEvent.rewarded)
                {
                    acidEvent.rewarded = true;
                    GiveAcidSecretionReward(acidEvent);
                }

                return;
            }

            TickAcidPuddleSpawn(acidEvent);
            TickAcidSlimeSpawn(acidEvent);
        }

        private void TickAcidPuddleSpawn(AcidSecretionEvent acidEvent)
        {
            if (acidEvent.acidPuddlePrefab == null)
            {
                return;
            }

            CleanNullPuddles(acidEvent);

            acidEvent.puddleSpawnTimer += Time.deltaTime;

            if (acidEvent.puddleSpawnTimer < acidEvent.puddleSpawnInterval)
            {
                return;
            }

            acidEvent.puddleSpawnTimer = 0f;

            for (int i = 0; i < acidEvent.puddlesPerWave; i++)
            {
                CleanNullPuddles(acidEvent);

                if (acidEvent.activePuddles.Count >= acidEvent.maxActivePuddles)
                {
                    return;
                }

                SpawnAcidPuddle(acidEvent);
            }
        }

        private void SpawnAcidPuddle(AcidSecretionEvent acidEvent)
        {
            Vector3 spawnPosition = GetRandomPositionAroundPlayer(
                acidEvent.minSpawnDistanceFromPlayer,
                acidEvent.maxSpawnDistanceFromPlayer
            );

            GameObject puddleObject = Instantiate(
                acidEvent.acidPuddlePrefab,
                spawnPosition,
                Quaternion.identity
            );

            acidEvent.activePuddles.Add(puddleObject);

            AcidPuddle acidPuddle = puddleObject.GetComponent<AcidPuddle>();

            if (acidPuddle != null)
            {
                acidPuddle.Init(
                    acidEvent.puddleLifeTime,
                    acidEvent.puddleDamagePerTick,
                    acidEvent.puddleTickInterval,
                    acidEvent.puddleScale,
                    acidEvent.damageImmediatelyOnEnter,
                    acidEvent.puddleWarningDuration
                );
            }

            if (logAcidEvent)
            {
                Debug.Log($"[StageEvent] Acid puddle spawned at {spawnPosition}");
            }
        }

        private void TickAcidSlimeSpawn(AcidSecretionEvent acidEvent)
        {
            if (acidEvent.acidSlimeMonsterIndices == null ||
                acidEvent.acidSlimeMonsterIndices.Count == 0)
            {
                return;
            }

            float baseSpawnRate = levelManager.GetCurrentBaseMonsterSpawnRate();
            float referenceSpawnRate = Mathf.Max(
                baseSpawnRate,
                acidEvent.acidSlimeMinimumReferenceSpawnRate
            );

            float extraSpawnRate =
                referenceSpawnRate * Mathf.Max(0f, acidEvent.acidSlimeExtraSpawnMultiplier);

            if (extraSpawnRate <= 0f)
            {
                return;
            }

            acidEvent.acidSlimeAccumulator += Time.deltaTime * extraSpawnRate;

            while (acidEvent.acidSlimeAccumulator >= 1f)
            {
                levelManager.SpawnRandomMonsterFromFlatIndexList(
                    acidEvent.acidSlimeMonsterIndices,
                    acidEvent.acidSlimeHpMultiplier
                );

                acidEvent.acidSlimeAccumulator -= 1f;

                if (logAcidEvent)
                {
                    Debug.Log($"[StageEvent] Acid slime extra spawn | rate={extraSpawnRate:F2}");
                }
            }
        }

        private Vector3 GetRandomPositionAroundPlayer(float minDistance, float maxDistance)
        {
            Character playerCharacter = levelManager.PlayerCharacter;

            if (playerCharacter == null)
            {
                return transform.position;
            }

            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            if (randomDirection == Vector2.zero)
            {
                randomDirection = Vector2.up;
            }

            float distance = Random.Range(minDistance, maxDistance);

            return playerCharacter.transform.position + (Vector3)(randomDirection * distance);
        }

        private void GiveAcidSecretionReward(AcidSecretionEvent acidEvent)
        {
            if (!acidEvent.spawnChestAsTemporaryItemReward)
            {
                return;
            }

            if (levelManager == null ||
                levelManager.EntityManager == null ||
                levelManager.CurrentLevelBlueprint == null)
            {
                Debug.LogWarning("[StageEvent] 위산분비 보상 지급 실패: LevelManager 정보가 비어 있습니다.");
                return;
            }

            levelManager.EntityManager.SpawnChest(levelManager.CurrentLevelBlueprint.chestBlueprint);

            if (logEventState)
            {
                Debug.Log("[StageEvent] 위산분비 보상: 임시 아이템 보상용 Chest 생성");
            }
        }

        private void CleanNullPuddles(AcidSecretionEvent acidEvent)
        {
            if (acidEvent.activePuddles == null)
            {
                acidEvent.activePuddles = new List<GameObject>();
                return;
            }

            for (int i = acidEvent.activePuddles.Count - 1; i >= 0; i--)
            {
                if (acidEvent.activePuddles[i] == null)
                {
                    acidEvent.activePuddles.RemoveAt(i);
                }
            }
        }

        private void ShowEventStartedUI(string eventName)
        {
            string safeEventName = string.IsNullOrWhiteSpace(eventName)
                ? "스테이지"
                : eventName;

            string message = string.Format(eventStartMessageFormat, safeEventName);

            if (eventToastUI != null)
            {
                eventToastUI.Show(message);
            }

            if (logEventState)
            {
                Debug.Log($"[StageEvent UI] {message}");
            }
        }

        [ContextMenu("Reset Stage Events Runtime")]
        private void ResetStageEventsRuntime()
        {
            foreach (AcidSecretionEvent acidEvent in acidSecretionEvents)
            {
                if (acidEvent == null || acidEvent.activePuddles == null)
                {
                    continue;
                }

                foreach (GameObject puddle in acidEvent.activePuddles)
                {
                    if (puddle != null)
                    {
                        Destroy(puddle);
                    }
                }

                acidEvent.activePuddles.Clear();
            }

            StageEventRuntimeModifiers.ResetCoinModifiers();
            PrepareAllEventRuntimeValues();

            Debug.Log("[StageEvent] Runtime reset complete.");
        }
    }
}