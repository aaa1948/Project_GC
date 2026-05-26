using UnityEngine;

namespace Vampire
{
    [DefaultExecutionOrder(-10000)]
    public class LevelSpawnBalanceApplier : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private LevelBlueprint levelBlueprint;

        [SerializeField] private LevelSpawnBalanceProfile spawnBalanceProfile;

        [Header("Apply Timing")]
        [Tooltip("게임 시작 시 LevelManager보다 먼저 Spawn Balance Profile을 LevelBlueprint에 적용합니다.")]
        [SerializeField] private bool applyOnAwake = true;

        [Tooltip("적용 후 Console에 몬스터 flat index 목록을 출력합니다.")]
        [SerializeField] private bool logMonsterIndexTableAfterApply = true;

        private void Awake()
        {
            if (applyOnAwake)
            {
                Apply();
            }
        }

        [ContextMenu("Apply Spawn Balance To Level Blueprint")]
        public void Apply()
        {
            if (levelBlueprint == null)
            {
                Debug.LogWarning("[LevelSpawnBalanceApplier] LevelBlueprint가 비어 있습니다.", this);
                return;
            }

            if (spawnBalanceProfile == null)
            {
                Debug.LogWarning("[LevelSpawnBalanceApplier] SpawnBalanceProfile이 비어 있습니다.", this);
                return;
            }

            spawnBalanceProfile.ApplyTo(levelBlueprint);

            if (logMonsterIndexTableAfterApply)
            {
                LogMonsterIndexTable(levelBlueprint);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(levelBlueprint);
                UnityEditor.EditorUtility.SetDirty(spawnBalanceProfile);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log("[LevelSpawnBalanceApplier] 에디터에서 적용되어 LevelBlueprint Asset이 저장되었습니다.", this);
            }
#endif
        }

        private void LogMonsterIndexTable(LevelBlueprint blueprint)
        {
            if (blueprint == null || blueprint.monsters == null)
            {
                return;
            }

            Debug.Log("[LevelSpawnBalanceApplier] ===== Monster Flat Index Table Start =====", this);

            int flatIndex = 0;

            for (int poolIndex = 0; poolIndex < blueprint.monsters.Length; poolIndex++)
            {
                LevelBlueprint.MonstersContainer container = blueprint.monsters[poolIndex];

                if (container == null || container.monsterBlueprints == null)
                {
                    continue;
                }

                for (int blueprintIndex = 0; blueprintIndex < container.monsterBlueprints.Length; blueprintIndex++)
                {
                    MonsterBlueprint monsterBlueprint = container.monsterBlueprints[blueprintIndex];

                    string monsterName = monsterBlueprint != null ? monsterBlueprint.name : "NULL";
                    string prefabName = container.monstersPrefab != null ? container.monstersPrefab.name : "NULL PREFAB";

                    Debug.Log(
                        $"[SpawnBalanceIndex] flatIndex={flatIndex} | " +
                        $"poolIndex={poolIndex} | blueprintIndex={blueprintIndex} | " +
                        $"prefab={prefabName} | blueprint={monsterName}",
                        this
                    );

                    flatIndex++;
                }
            }

            Debug.Log("[LevelSpawnBalanceApplier] ===== Monster Flat Index Table End =====", this);
        }
    }
}