using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// 상호작용 시 LevelBlueprint의 Final Boss를 즉시 소환하는 오브젝트.
    /// 소환 시점에 따라 보스 HP / 데미지 / 패턴 데미지를 강화한다.
    /// </summary>
    public class FinalBossSummonInteractable : InteractableEventObject
    {
        [Header("Boss Spawn")]
        [SerializeField] private bool spawnRelativeToPlayer = true;
        [SerializeField] private bool useRandomDirectionAroundPlayer = true;
        [SerializeField] private float spawnDistanceFromPlayer = 6f;
        [SerializeField] private Vector2 spawnOffsetFromPlayer = new Vector2(0f, 6f);

        [Tooltip("체크하면 지정된 위치에 보스를 소환합니다.")]
        [SerializeField] private bool useFixedSpawnPoint = false;

        [SerializeField] private Transform fixedSpawnPoint;

        [Header("Time Based Boss Scaling")]
        [Tooltip("체크하면 현재 플레이 시간에 따라 보스 HP와 데미지가 변경됩니다.")]
        [SerializeField] private bool applyTimeBasedScaling = true;

        [Tooltip("true면 LevelManager의 LevelDuration을 기준으로 분 단위를 계산합니다. 현재 20분 스테이지면 20분 기준이 됩니다.")]
        [SerializeField] private bool useLevelDurationAsMaxMinute = true;

        [Tooltip("useLevelDurationAsMaxMinute이 꺼져 있을 때 사용할 최대 기준 분입니다.")]
        [SerializeField] private float manualMaxScaleMinute = 20f;

        [Tooltip("현재 플레이 시간 기준 HP 배율입니다. X축은 분, Y축은 배율입니다.")]
        [SerializeField]
        private AnimationCurve hpMultiplierByMinute = new AnimationCurve(
            new Keyframe(0f, 0.70f),
            new Keyframe(5f, 0.85f),
            new Keyframe(10f, 1.00f),
            new Keyframe(15f, 1.30f),
            new Keyframe(20f, 1.70f)
        );

        [Tooltip("현재 플레이 시간 기준 데미지 배율입니다. 기본 공격, 접촉 피해, 패턴 피해에 적용됩니다.")]
        [SerializeField]
        private AnimationCurve damageMultiplierByMinute = new AnimationCurve(
            new Keyframe(0f, 0.70f),
            new Keyframe(5f, 0.85f),
            new Keyframe(10f, 1.00f),
            new Keyframe(15f, 1.20f),
            new Keyframe(20f, 1.45f)
        );

        [Tooltip("체크하면 보스 스케일링 로그를 출력합니다.")]
        [SerializeField] private bool debugBossScaling = true;

        [Header("Duplicate Prevention")]
        [Tooltip("true면 이미 보스가 존재할 때 추가 소환하지 않습니다.")]
        [SerializeField] private bool preventDuplicateBoss = true;

        [Tooltip("true면 상호작용으로 보스를 소환한 뒤 기존 BossLevelSpawner를 비활성화합니다.")]
        [SerializeField] private bool disableBossLevelSpawnersAfterSpawn = true;

        private static bool bossSummonedByInteraction = false;

        protected override bool ExecuteInteraction(Character player)
        {
            if (levelManager == null)
            {
                Debug.LogError("[FinalBossSummonInteractable] LevelManager를 찾지 못했습니다.", this);
                return false;
            }

            if (levelManager.EntityManager == null)
            {
                Debug.LogError("[FinalBossSummonInteractable] EntityManager가 비어 있습니다.", this);
                return false;
            }

            LevelBlueprint levelBlueprint = levelManager.CurrentLevelBlueprint;

            if (levelBlueprint == null)
            {
                Debug.LogError("[FinalBossSummonInteractable] CurrentLevelBlueprint가 비어 있습니다.", this);
                return false;
            }

            if (levelBlueprint.finalBoss == null ||
                levelBlueprint.finalBoss.bossBlueprint == null ||
                levelBlueprint.finalBoss.bossPrefab == null)
            {
                Debug.LogError("[FinalBossSummonInteractable] Final Boss 설정이 비어 있습니다.", this);
                return false;
            }

            if (preventDuplicateBoss && IsBossAlreadyPresent())
            {
                if (debugLog)
                {
                    Debug.Log("[FinalBossSummonInteractable] 이미 보스가 존재해서 소환하지 않습니다.", this);
                }

                return false;
            }

            float currentMinute = GetCurrentLevelMinute();
            float hpMultiplier = 1f;
            float damageMultiplier = 1f;

            if (applyTimeBasedScaling)
            {
                hpMultiplier = BossTimeScalingUtility.EvaluateMultiplier(
                    hpMultiplierByMinute,
                    currentMinute,
                    1f
                );

                damageMultiplier = BossTimeScalingUtility.EvaluateMultiplier(
                    damageMultiplierByMinute,
                    currentMinute,
                    1f
                );
            }

            float bossHpBuff = applyTimeBasedScaling
                ? BossTimeScalingUtility.CalculateMonsterHpBuff(levelBlueprint.finalBoss.bossBlueprint, hpMultiplier)
                : 0f;

            int bossPoolIndex = levelBlueprint.monsters.Length;
            Vector3 spawnPosition = GetSpawnPosition(player);

            Monster spawnedBoss = levelManager.EntityManager.SpawnMonster(
                bossPoolIndex,
                spawnPosition,
                levelBlueprint.finalBoss.bossBlueprint,
                bossHpBuff
            );

            if (spawnedBoss == null)
            {
                Debug.LogError("[FinalBossSummonInteractable] 보스 소환 실패: SpawnMonster가 null을 반환했습니다.", this);
                return false;
            }

            spawnedBoss.OnKilled.AddListener(levelManager.LevelPassed);

            BossController bossController = spawnedBoss.GetComponent<BossController>();

            if (bossController == null)
            {
                bossController = spawnedBoss.GetComponentInChildren<BossController>(true);
            }

            if (bossController != null)
            {
                bossController.SetPlayerCharacter(levelManager.PlayerCharacter);
            }

            if (applyTimeBasedScaling)
            {
                BossTimeScalingUtility.ApplyToSpawnedBoss(
                    spawnedBoss.gameObject,
                    hpMultiplier,
                    damageMultiplier,
                    debugBossScaling
                );
            }

            bossSummonedByInteraction = true;

            if (disableBossLevelSpawnersAfterSpawn)
            {
                DisableBossLevelSpawners();
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[FinalBossSummonInteractable] 최종보스 소환 완료 | " +
                    $"Minute={currentMinute:0.##} | HP x{hpMultiplier:0.##} | Damage x{damageMultiplier:0.##} | " +
                    $"HpBuff={bossHpBuff:0.##} | PoolIndex={bossPoolIndex} | Position={spawnPosition} | Boss={spawnedBoss.name}",
                    this
                );
            }

            return true;
        }

        private float GetCurrentLevelMinute()
        {
            if (levelManager == null)
            {
                return 0f;
            }

            float currentMinute = levelManager.CurrentLevelTime / 60f;

            float maxMinute = manualMaxScaleMinute;

            if (useLevelDurationAsMaxMinute && levelManager.LevelDuration > 0f)
            {
                maxMinute = levelManager.LevelDuration / 60f;
            }

            maxMinute = Mathf.Max(0.1f, maxMinute);

            return Mathf.Clamp(currentMinute, 0f, maxMinute);
        }

        private Vector3 GetSpawnPosition(Character player)
        {
            if (useFixedSpawnPoint && fixedSpawnPoint != null)
            {
                return fixedSpawnPoint.position;
            }

            Character targetPlayer = player != null ? player : levelManager.PlayerCharacter;

            if (spawnRelativeToPlayer && targetPlayer != null)
            {
                if (useRandomDirectionAroundPlayer)
                {
                    Vector2 randomDirection = Random.insideUnitCircle.normalized;

                    if (randomDirection == Vector2.zero)
                    {
                        randomDirection = Vector2.up;
                    }

                    return targetPlayer.transform.position +
                           (Vector3)(randomDirection * Mathf.Max(0.1f, spawnDistanceFromPlayer));
                }

                return targetPlayer.transform.position + (Vector3)spawnOffsetFromPlayer;
            }

            return transform.position;
        }

        private bool IsBossAlreadyPresent()
        {
            if (bossSummonedByInteraction)
            {
                return true;
            }

            BossMonster existingBossMonster = FindObjectOfType<BossMonster>();
            BossController existingBossController = FindObjectOfType<BossController>();

            return existingBossMonster != null || existingBossController != null;
        }

        private void DisableBossLevelSpawners()
        {
            BossLevelSpawner[] bossSpawners = FindObjectsOfType<BossLevelSpawner>();

            for (int i = 0; i < bossSpawners.Length; i++)
            {
                if (bossSpawners[i] != null)
                {
                    bossSpawners[i].enabled = false;
                }
            }

            if (debugLog && bossSpawners.Length > 0)
            {
                Debug.Log($"[FinalBossSummonInteractable] BossLevelSpawner {bossSpawners.Length}개 비활성화", this);
            }
        }
    }
}