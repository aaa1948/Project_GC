using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vampire
{
    public class LobbySelectedLoadoutDisplay : MonoBehaviour
    {
        [System.Serializable]
        private class SlotView
        {
            public Image iconImage;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI emptyText;
        }

        [SerializeField] private SlotView[] slots = new SlotView[2];

        private void OnEnable()
        {
            LobbyLoadoutData.OnChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            LobbyLoadoutData.OnChanged -= Refresh;
        }

        public void Refresh()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                MerchantItemBlueprint item = i < LobbyLoadoutData.SelectedCarryItems.Count
                    ? LobbyLoadoutData.SelectedCarryItems[i]
                    : null;

                ApplySlot(slots[i], item);
            }
        }

        private void ApplySlot(SlotView slot, MerchantItemBlueprint item)
        {
            if (slot == null)
            {
                return;
            }

            bool hasItem = item != null;

            if (slot.iconImage != null)
            {
                slot.iconImage.enabled = hasItem && item.itemIcon != null;
                slot.iconImage.sprite = hasItem ? item.itemIcon : null;
            }

            if (slot.nameText != null)
            {
                slot.nameText.gameObject.SetActive(hasItem);
                slot.nameText.text = hasItem ? item.itemName : string.Empty;
            }

            if (slot.emptyText != null)
            {
                slot.emptyText.gameObject.SetActive(!hasItem);
                slot.emptyText.text = "빈 슬롯";
            }
        }

        public void ClearSelectedItems()
        {
            LobbyLoadoutData.Clear();
        }
    }
}