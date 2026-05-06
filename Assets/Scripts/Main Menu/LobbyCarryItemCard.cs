using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vampire
{
    public class LobbyCarryItemCard : MonoBehaviour
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance;

        [Header("Data")]
        [SerializeField] private MerchantItemBlueprint itemData;

        [Header("Silver Cost")]
        [SerializeField] private bool useMerchantItemCost = true;
        [SerializeField] private int silverCostOverride = 100;

        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI tagText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Button buyButton;

        [Header("Optional")]
        [SerializeField] private SilverCoinDisplay silverCoinDisplay;

        private int SilverCost
        {
            get
            {
                if (itemData == null)
                {
                    return 0;
                }

                if (useMerchantItemCost)
                {
                    int merchantCost = GetInt(itemData, "cost", "silverCost", "price");

                    if (merchantCost > 0)
                    {
                        return merchantCost;
                    }
                }

                return Mathf.Max(0, silverCostOverride);
            }
        }

        private void Awake()
        {
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(OnClickBuy);
                buyButton.onClick.AddListener(OnClickBuy);
            }
        }

        private void OnEnable()
        {
            SilverWallet.OnChanged += OnSilverChanged;
            LobbyLoadoutData.OnChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            SilverWallet.OnChanged -= OnSilverChanged;
            LobbyLoadoutData.OnChanged -= Refresh;
        }

        private void Start()
        {
            Refresh();
        }

        public void Init(MerchantItemBlueprint item, SilverCoinDisplay display = null)
        {
            itemData = item;
            silverCoinDisplay = display;
            Refresh();
        }

        public void Refresh()
        {
            if (itemData == null)
            {
                SetEmpty();
                return;
            }

            bool equipped = LobbyLoadoutData.IsEquipped(itemData);
            bool full = LobbyLoadoutData.SelectedCarryItems.Count >= LobbyLoadoutData.MaxCarryItems;
            bool canAfford = SilverWallet.CanSpend(SilverCost);

            Sprite icon = GetObject<Sprite>(itemData, "itemIcon", "icon");

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
            {
                nameText.text = GetString(itemData, itemData.name, "itemName", "displayName", "name");
            }

            if (rarityText != null)
            {
                rarityText.text = GetStringFromAny(itemData, "등급 없음", "itemRarity", "rarity");
            }

            if (tagText != null)
            {
                tagText.text = GetStringFromAny(itemData, "태그 없음", "itemTag", "tag");
            }

            if (descriptionText != null)
            {
                descriptionText.text = GetString(itemData, string.Empty, "description", "desc");
            }

            if (costText != null)
            {
                costText.text = SilverCost + " 실버";
            }

            if (buttonText != null)
            {
                if (equipped)
                {
                    buttonText.text = "장착됨";
                }
                else if (full)
                {
                    buttonText.text = "슬롯 가득 참";
                }
                else if (!canAfford)
                {
                    buttonText.text = "실버 부족";
                }
                else
                {
                    buttonText.text = "구매";
                }
            }

            if (buyButton != null)
            {
                buyButton.interactable = !equipped && !full && canAfford;
            }
        }

        private void SetEmpty()
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }

            if (nameText != null)
            {
                nameText.text = "아이템 없음";
            }

            if (rarityText != null)
            {
                rarityText.text = string.Empty;
            }

            if (tagText != null)
            {
                tagText.text = string.Empty;
            }

            if (descriptionText != null)
            {
                descriptionText.text = string.Empty;
            }

            if (costText != null)
            {
                costText.text = string.Empty;
            }

            if (buttonText != null)
            {
                buttonText.text = "-";
            }

            if (buyButton != null)
            {
                buyButton.interactable = false;
            }
        }

        private void OnClickBuy()
        {
            if (itemData == null)
            {
                return;
            }

            if (LobbyLoadoutData.TryBuyAndEquip(itemData, SilverCost, out string message))
            {
                Debug.Log("[LobbyShop] " + message);

                if (silverCoinDisplay != null)
                {
                    silverCoinDisplay.UpdateDisplay();
                }
            }
            else
            {
                Debug.Log("[LobbyShop] " + message);
            }

            Refresh();
        }

        private void OnSilverChanged(int value)
        {
            Refresh();
        }

        private static object GetFieldValue(MerchantItemBlueprint item, params string[] fieldNames)
        {
            if (item == null || fieldNames == null)
            {
                return null;
            }

            Type type = item.GetType();

            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldName = fieldNames[i];

                if (string.IsNullOrEmpty(fieldName))
                {
                    continue;
                }

                FieldInfo field = type.GetField(fieldName, FieldFlags);

                if (field != null)
                {
                    return field.GetValue(item);
                }
            }

            return null;
        }

        private static string GetString(MerchantItemBlueprint item, string fallback, params string[] fieldNames)
        {
            object value = GetFieldValue(item, fieldNames);

            if (value == null)
            {
                return fallback;
            }

            string text = value as string;

            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            return value.ToString();
        }

        private static string GetStringFromAny(MerchantItemBlueprint item, string fallback, params string[] fieldNames)
        {
            object value = GetFieldValue(item, fieldNames);

            if (value == null)
            {
                return fallback;
            }

            return value.ToString();
        }

        private static int GetInt(MerchantItemBlueprint item, params string[] fieldNames)
        {
            object value = GetFieldValue(item, fieldNames);

            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private static T GetObject<T>(MerchantItemBlueprint item, params string[] fieldNames) where T : UnityEngine.Object
        {
            object value = GetFieldValue(item, fieldNames);

            if (value == null)
            {
                return null;
            }

            return value as T;
        }
    }
}