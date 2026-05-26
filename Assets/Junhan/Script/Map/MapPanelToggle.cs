using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Vampire
{
    /// <summary>
    /// Tab 키로 전체 지도 패널을 열고 닫는 컨트롤러.
    ///
    /// 현재는 지도 페이지만 열지만,
    /// 나중에 아이템/증강/스탯/전체 지도 탭을 관리하는 패널 컨트롤러로 확장할 수 있다.
    /// </summary>
    public class MapPanelToggle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private ExplorationMapSystem mapSystem;

        [Header("Input")]
        [SerializeField] private Key toggleKey = Key.Tab;
        [SerializeField] private bool holdToOpen = false;
        [SerializeField] private bool closeWithEscape = true;

        [Header("Pause Option")]
        [Tooltip("true면 지도 패널이 열렸을 때 게임 시간을 멈춥니다. 현재는 false 추천입니다.")]
        [SerializeField] private bool pauseGameWhileOpen = false;

        [Header("Start State")]
        [SerializeField] private bool openOnStart = false;

        private bool isOpen = false;
        private float previousTimeScale = 1f;

        private void Awake()
        {
            if (mapSystem == null)
            {
                mapSystem = FindObjectOfType<ExplorationMapSystem>();
            }

            SetOpen(openOnStart, false);
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            KeyControl keyControl = Keyboard.current[toggleKey];

            if (keyControl == null)
            {
                return;
            }

            if (holdToOpen)
            {
                SetOpen(keyControl.isPressed);
                return;
            }

            if (keyControl.wasPressedThisFrame)
            {
                SetOpen(!isOpen);
            }

            if (isOpen && closeWithEscape && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
            }
        }

        public void SetOpen(bool open)
        {
            SetOpen(open, true);
        }

        private void SetOpen(bool open, bool applyPause)
        {
            if (isOpen == open && panelRoot != null && panelRoot.activeSelf == open)
            {
                return;
            }

            isOpen = open;

            if (panelRoot != null)
            {
                panelRoot.SetActive(isOpen);
            }

            if (mapSystem != null)
            {
                mapSystem.SetFullMapPanelVisible(isOpen);
            }

            if (!applyPause || !pauseGameWhileOpen)
            {
                return;
            }

            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = previousTimeScale;
            }
        }

        public void Toggle()
        {
            SetOpen(!isOpen);
        }

        public bool IsOpen()
        {
            return isOpen;
        }
    }
}