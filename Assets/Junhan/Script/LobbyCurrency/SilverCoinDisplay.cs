using TMPro;
using UnityEngine;

namespace Vampire
{
    public class SilverCoinDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI silverText;
        [SerializeField] private string format = "{0}";

        private void OnEnable()
        {
            SilverWallet.OnChanged += UpdateDisplay;
            UpdateDisplay(SilverWallet.Silver);
        }

        private void OnDisable()
        {
            SilverWallet.OnChanged -= UpdateDisplay;
        }

        public void UpdateDisplay()
        {
            UpdateDisplay(SilverWallet.Silver);
        }

        private void UpdateDisplay(int value)
        {
            if (silverText == null)
            {
                return;
            }

            silverText.text = string.Format(format, value);
        }
    }
}