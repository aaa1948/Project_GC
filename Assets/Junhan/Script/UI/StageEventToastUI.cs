using System.Collections;
using TMPro;
using UnityEngine;

namespace Vampire
{
    public class StageEventToastUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("이벤트 알림 전체 패널입니다. 비워두면 이 스크립트가 붙은 오브젝트를 사용합니다.")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("알림 UI를 부드럽게 보이고 숨기기 위한 CanvasGroup입니다.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("이벤트 시작 문구를 표시할 TextMeshProUGUI 텍스트입니다.")]
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Timing")]
        [Tooltip("알림이 완전히 보인 상태로 유지되는 시간입니다. 2로 두면 약 2초 동안 표시됩니다.")]
        [SerializeField] private float visibleDuration = 2f;

        [Tooltip("알림이 나타나는 페이드 인 시간입니다.")]
        [SerializeField] private float fadeInDuration = 0.15f;

        [Tooltip("알림이 사라지는 페이드 아웃 시간입니다.")]
        [SerializeField] private float fadeOutDuration = 0.35f;

        [Header("Debug")]
        [Tooltip("체크하면 알림 표시 로그를 Console에 출력합니다.")]
        [SerializeField] private bool debugLog = false;

        private Coroutine showRoutine;

        private void Awake()
        {
            InitializeReferences();
            HideImmediate();
        }

        public void Show(string message)
        {
            InitializeReferences();

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning(
                    "[StageEventToastUI] 이 오브젝트가 비활성화되어 있어 알림 코루틴을 실행할 수 없습니다. " +
                    "StageEventToastUI가 붙은 오브젝트는 항상 활성화 상태로 두세요.",
                    this
                );

                return;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
            }

            showRoutine = StartCoroutine(ShowRoutine());

            if (debugLog)
            {
                Debug.Log($"[StageEventToastUI] Show: {message}", this);
            }
        }

        private void InitializeReferences()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }
        }

        private IEnumerator ShowRoutine()
        {
            if (panelRoot != null && !panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
            }

            yield return Fade(0f, 1f, fadeInDuration);
            yield return new WaitForSeconds(visibleDuration);
            yield return Fade(1f, 0f, fadeOutDuration);

            HideImmediate();
            showRoutine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }

            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                SetAlpha(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = alpha;
            canvasGroup.interactable = alpha > 0.01f;
            canvasGroup.blocksRaycasts = false;
        }

        private void HideImmediate()
        {
            SetAlpha(0f);

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }
    }
}