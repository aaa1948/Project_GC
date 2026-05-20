using UnityEngine;

namespace Vampire
{
    public enum BossHealPatternType
    {
        None,
        ColaBottle,
        AbsorbMinion
    }

    public class BossHealPatternLock : MonoBehaviour
    {
        [Header("Runtime State")]
        [Tooltip("현재 실행 중인 보스 회복 패턴입니다. None이면 회복 패턴이 실행 중이 아닙니다.")]
        [SerializeField] private BossHealPatternType currentPattern = BossHealPatternType.None;

        [Header("Debug")]
        [Tooltip("체크하면 회복 패턴 잠금/해제 로그를 Console에 출력합니다.")]
        [SerializeField] private bool debugLog = false;

        public bool IsLocked => currentPattern != BossHealPatternType.None;
        public BossHealPatternType CurrentPattern => currentPattern;

        public bool TryBegin(BossHealPatternType patternType)
        {
            if (patternType == BossHealPatternType.None)
            {
                return false;
            }

            if (IsLocked)
            {
                if (debugLog)
                {
                    Debug.Log($"[BossHealPatternLock] 이미 회복 패턴 실행 중: {currentPattern} / 요청: {patternType}");
                }

                return false;
            }

            currentPattern = patternType;

            if (debugLog)
            {
                Debug.Log($"[BossHealPatternLock] 회복 패턴 시작: {currentPattern}");
            }

            return true;
        }

        public void End(BossHealPatternType patternType)
        {
            if (currentPattern != patternType)
            {
                return;
            }

            if (debugLog)
            {
                Debug.Log($"[BossHealPatternLock] 회복 패턴 종료: {currentPattern}");
            }

            currentPattern = BossHealPatternType.None;
        }

        public void ForceClear()
        {
            if (debugLog)
            {
                Debug.Log($"[BossHealPatternLock] 회복 패턴 강제 해제: {currentPattern}");
            }

            currentPattern = BossHealPatternType.None;
        }
    }
}