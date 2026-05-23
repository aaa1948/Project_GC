using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vampire
{
    public class MerchantUIManager : MonoBehaviour
    {
        public static MerchantUIManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject shopUIContainer;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button rerollButton;
        [SerializeField] private TMP_Text rerollCostText;

        [Header("Shop Settings")]
        [SerializeField] private List<ShopItemButton> itemButtons;
        [SerializeField] private List<MerchantItemBlueprint> allAvailableItems;

        [Header("Base Rarity Chance")]
        [SerializeField] private int commonWeight = 55;
        [SerializeField] private int uncommonWeight = 32;
        [SerializeField] private int rareWeight = 11;
        [SerializeField] private int legendaryWeight = 2;

        [Header("Reroll Rarity Bonus")]
        [SerializeField] private int rareBonusPerReroll = 2;
        [SerializeField] private int legendaryBonusPerReroll = 1;
        [SerializeField] private int maxRareBonus = 8;
        [SerializeField] private int maxLegendaryBonus = 4;
        [SerializeField] private int minCommonWeight = 20;

        [Header("Reroll Settings")]
        [SerializeField] private int baseRerollCost = 10;
        [SerializeField] private int rerollCostIncrease = 5;

        [Header("Sound Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip rerollSound;

        private MerchantNPC currentInteractingNPC;

        private int currentRerollCost;
        private int rerollCount;

        private List<MerchantItemBlueprint> currentShopItems = new List<MerchantItemBlueprint>();
        private bool hasGeneratedShopItems = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            shopUIContainer.SetActive(false);

            closeButton.onClick.AddListener(CloseShop);

            if (rerollButton != null)
                rerollButton.onClick.AddListener(OnClickRerollItems);
        }

        public void OpenShop(MerchantNPC npc)
        {
            currentInteractingNPC = npc;
            shopUIContainer.SetActive(true);

            Time.timeScale = 0f;

            if (!hasGeneratedShopItems)
            {
                rerollCount = 0;
                currentRerollCost = baseRerollCost;

                GenerateNewShopItems(false);
                hasGeneratedShopItems = true;
            }

            UpdateRerollCostText();
            DisplayCurrentShopItems();
        }

        private void GenerateNewShopItems(bool useRerollBonus)
        {
            currentShopItems.Clear();

            int safetyCount = 0;

            while (currentShopItems.Count < itemButtons.Count && safetyCount < 100)
            {
                safetyCount++;

                MerchantItemBlueprint.Rarity selectedRarity = useRerollBonus
                    ? GetRandomRarityWithRerollBonus()
                    : GetRandomRarity();

                MerchantItemBlueprint selectedItem = GetRandomItemByRarity(selectedRarity);

                if (selectedItem != null && !currentShopItems.Contains(selectedItem))
                {
                    currentShopItems.Add(selectedItem);
                }
            }

            if (currentShopItems.Count < itemButtons.Count)
            {
                FillRemainingItemsRandomly();
            }
        }

        private MerchantItemBlueprint.Rarity GetRandomRarity()
        {
            return RollRarity(commonWeight, uncommonWeight, rareWeight, legendaryWeight);
        }

        private MerchantItemBlueprint.Rarity GetRandomRarityWithRerollBonus()
        {
            int rareBonus = Mathf.Min(rerollCount * rareBonusPerReroll, maxRareBonus);
            int legendaryBonus = Mathf.Min(rerollCount * legendaryBonusPerReroll, maxLegendaryBonus);

            int adjustedCommon = Mathf.Max(minCommonWeight, commonWeight - rareBonus - legendaryBonus);
            int adjustedUncommon = uncommonWeight;
            int adjustedRare = rareWeight + rareBonus;
            int adjustedLegendary = legendaryWeight + legendaryBonus;

            return RollRarity(adjustedCommon, adjustedUncommon, adjustedRare, adjustedLegendary);
        }

        private MerchantItemBlueprint.Rarity RollRarity(int common, int uncommon, int rare, int legendary)
        {
            int total = common + uncommon + rare + legendary;

            if (total <= 0)
                return MerchantItemBlueprint.Rarity.Common;

            int roll = Random.Range(0, total);

            if (roll < common)
                return MerchantItemBlueprint.Rarity.Common;

            roll -= common;

            if (roll < uncommon)
                return MerchantItemBlueprint.Rarity.Uncommon;

            roll -= uncommon;

            if (roll < rare)
                return MerchantItemBlueprint.Rarity.Rare;

            return MerchantItemBlueprint.Rarity.Legendary;
        }

        private MerchantItemBlueprint GetRandomItemByRarity(MerchantItemBlueprint.Rarity rarity)
        {
            List<MerchantItemBlueprint> candidates =
                allAvailableItems.FindAll(item => item.itemRarity == rarity);

            if (candidates.Count == 0)
                return null;

            return candidates[Random.Range(0, candidates.Count)];
        }

        private void FillRemainingItemsRandomly()
        {
            List<MerchantItemBlueprint> remainingItems = new List<MerchantItemBlueprint>();

            foreach (MerchantItemBlueprint item in allAvailableItems)
            {
                if (!currentShopItems.Contains(item))
                    remainingItems.Add(item);
            }

            while (currentShopItems.Count < itemButtons.Count && remainingItems.Count > 0)
            {
                int randomIndex = Random.Range(0, remainingItems.Count);
                currentShopItems.Add(remainingItems[randomIndex]);
                remainingItems.RemoveAt(randomIndex);
            }
        }

        private void DisplayCurrentShopItems()
        {
            for (int i = 0; i < itemButtons.Count; i++)
            {
                if (i < currentShopItems.Count)
                {
                    itemButtons[i].gameObject.SetActive(true);
                    itemButtons[i].Setup(currentShopItems[i]);
                }
                else
                {
                    itemButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public void OnClickRerollItems()
        {
            if (ProcessPayment(currentRerollCost))
            {
                rerollCount++;

                GenerateNewShopItems(true);
                DisplayCurrentShopItems();

                PlayRerollSound();

                currentRerollCost = baseRerollCost + (rerollCostIncrease * rerollCount);
                UpdateRerollCostText();

                Debug.Log($"[상점] 리롤 성공! 리롤 횟수: {rerollCount}, 다음 리롤 비용: {currentRerollCost}G");
            }
            else
            {
                Debug.LogWarning("[상점] 리롤할 골드가 부족합니다!");
            }
        }

        private void UpdateRerollCostText()
        {
            if (rerollCostText != null)
            {
                rerollCostText.text = $"Reroll - {currentRerollCost}G";
            }
        }

        private void PlayRerollSound()
        {
            if (audioSource != null && rerollSound != null)
            {
                audioSource.PlayOneShot(rerollSound);
            }
        }

        public void CloseShop()
        {
            shopUIContainer.SetActive(false);

            Time.timeScale = 1f;

            if (currentInteractingNPC != null)
            {
                currentInteractingNPC.CloseShopUI();
                currentInteractingNPC = null;
            }
        }

        public void OnClickPurchaseItem(MerchantItemBlueprint itemToBuy, ShopItemButton clickedButton)
        {
            if (ProcessPayment(itemToBuy.cost))
            {
                ShopStatApplier statApplier = FindObjectOfType<ShopStatApplier>();
                if (statApplier != null)
                {
                    statApplier.ApplyStats(itemToBuy);
                }

                MerchantNPC npcToDestroy = currentInteractingNPC;

                hasGeneratedShopItems = false;
                currentShopItems.Clear();

                CloseShop();

                if (npcToDestroy != null)
                {
                    Debug.Log("<color=magenta>[시스템]</color> 거래 완료! 아저씨가 퇴근했습니다.");
                    Destroy(npcToDestroy.gameObject);
                }
            }
        }

        private bool ProcessPayment(int cost)
        {
            StatsManager currentStats = FindObjectOfType<StatsManager>();

            if (currentStats != null)
            {
                if (currentStats.CoinsGained >= cost)
                {
                    currentStats.IncreaseCoinsGained(-cost);
                    return true;
                }
                else
                {
                    Debug.LogWarning("[상점] 골드가 부족합니다!");
                }
            }
            else
            {
                Debug.LogError("[MerchantUIManager] 맵에 StatsManager가 없습니다!");
            }

            return false;
        }
    }
}