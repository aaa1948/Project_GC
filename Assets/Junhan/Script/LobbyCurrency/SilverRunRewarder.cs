using UnityEngine;

namespace Vampire
{
    public class SilverRunRewarder : MonoBehaviour
    {
        public static SilverRunRewarder Instance { get; private set; }

        [Header("Normal Monster Reward")]
        [SerializeField] private int normalMonsterSilverMin = 1;
        [SerializeField] private int normalMonsterSilverMax = 2;

        [Header("Boss Reward")]
        [SerializeField] private int bossMonsterBonusSilver = 100;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        public int RunEarnedSilver { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RunEarnedSilver = 0;
        }

        public static void RewardMonsterKill(MonsterBlueprint monsterBlueprint)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.AddReward(monsterBlueprint);
        }

        private void AddReward(MonsterBlueprint monsterBlueprint)
        {
            int amount = Random.Range(normalMonsterSilverMin, normalMonsterSilverMax + 1);

            if (monsterBlueprint is BossMonsterBlueprint)
            {
                amount += bossMonsterBonusSilver;
            }

            RunEarnedSilver += amount;
            SilverWallet.Add(amount);

            if (debugLog)
            {
                Debug.Log($"[Silver] +{amount} | Run Earned = {RunEarnedSilver} | Total = {SilverWallet.Silver}");
            }
        }
    }
}