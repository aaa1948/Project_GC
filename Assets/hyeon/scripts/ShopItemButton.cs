using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Vampire
{
    public class ShopItemButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemCardImage;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI itemCostText;
        [SerializeField] private Button purchaseButton;

        [Header("Card Rarity Sprites")]
        [SerializeField] private Sprite commonCardSprite;
        [SerializeField] private Sprite uncommonCardSprite;
        [SerializeField] private Sprite rareCardSprite;
        [SerializeField] private Sprite legendaryCardSprite;

        private MerchantItemBlueprint currentItem;
        private bool isSoldOut = false;

        private void Awake()
        {
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }

        public void Setup(MerchantItemBlueprint item)
        {
            currentItem = item;
            isSoldOut = false;

            purchaseButton.interactable = true;

            itemIcon.sprite = item.itemIcon;
            itemNameText.text = item.itemName;
            itemDescriptionText.text = item.description;
            itemCostText.text = item.cost.ToString() + " G";
            itemCostText.color = Color.black;

            SetCardByRarity(item.itemRarity);
        }

        private void SetCardByRarity(MerchantItemBlueprint.Rarity rarity)
        {
            if (itemCardImage == null) return;

            switch (rarity)
            {
                case MerchantItemBlueprint.Rarity.Common:
                    itemCardImage.sprite = commonCardSprite;
                    break;

                case MerchantItemBlueprint.Rarity.Uncommon:
                    itemCardImage.sprite = uncommonCardSprite;
                    break;

                case MerchantItemBlueprint.Rarity.Rare:
                    itemCardImage.sprite = rareCardSprite;
                    break;

                case MerchantItemBlueprint.Rarity.Legendary:
                    itemCardImage.sprite = legendaryCardSprite;
                    break;
            }
        }

        private void OnPurchaseClicked()
        {
            if (currentItem == null || isSoldOut) return;

            MerchantUIManager.Instance.OnClickPurchaseItem(currentItem, this);
        }

        public void MarkAsSoldOut()
        {
            isSoldOut = true;
            purchaseButton.interactable = false;

            itemCostText.text = "SOLD OUT";
            itemCostText.color = Color.gray;
        }

        public void ShowNotEnoughGold()
        {
            itemCostText.text = "µ∑ ∫Œ¡∑!";
            itemCostText.color = Color.red;

            Invoke(nameof(ResetPriceText), 1f);
        }

        private void ResetPriceText()
        {
            if (!isSoldOut && currentItem != null)
            {
                itemCostText.text = currentItem.cost.ToString() + " G";
                itemCostText.color = Color.black;
            }
        }
    }
}