using UnityEngine;

namespace Vampire
{
    public class SilverDebugCheat : MonoBehaviour
    {
        [Header("Debug Silver")]
        [SerializeField] private KeyCode addSilverKey = KeyCode.F9;
        [SerializeField] private KeyCode resetSilverKey = KeyCode.F10;
        [SerializeField] private int addAmount = 1000;

        [Header("Debug Log")]
        [SerializeField] private bool debugLog = true;

        private void Update()
        {
            if (Input.GetKeyDown(addSilverKey))
            {
                SilverWallet.Add(addAmount);

                if (debugLog)
                {
                    Debug.Log($"[SilverDebugCheat] 실버 +{addAmount} | 현재 실버 = {SilverWallet.Silver}");
                }
            }

            if (Input.GetKeyDown(resetSilverKey))
            {
                SilverWallet.ResetSilver();

                if (debugLog)
                {
                    Debug.Log("[SilverDebugCheat] 실버 초기화");
                }
            }
        }
    }
}