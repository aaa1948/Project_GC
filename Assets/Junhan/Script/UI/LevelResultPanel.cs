using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Vampire
{
    /// <summary>
    /// 클리어/실패 결과 패널.
    /// 현재는 다시하기, 메인메뉴 버튼만 담당한다.
    /// 나중에 획득 증강, 아이템, 처치 수, 획득 재화 표시를 확장하면 된다.
    /// </summary>
    public class LevelResultPanel : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private string clearTitle = "정화 완료";
        [SerializeField] private string failTitle = "사투 실패";

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Scene")]
        [SerializeField] private int mainMenuSceneIndex = 0;

        private bool initialized;

        private void Awake()
        {
            InitializeIfNeeded();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(Restart);
                retryButton.onClick.AddListener(Restart);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        public void Open(bool levelPassed)
        {
            InitializeIfNeeded();

            Time.timeScale = 0f;

            if (titleText != null)
            {
                titleText.text = levelPassed ? clearTitle : failTitle;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Close()
        {
            InitializeIfNeeded();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
    }
}