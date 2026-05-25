using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// 보스 전용 워크프레임 애니메이터입니다.
    /// 기존 BossController, BossMonster, SpriteAnimator 코드를 수정하지 않고
    /// 보스 SpriteRenderer의 sprite만 주기적으로 교체합니다.
    /// </summary>
    public class BossWalkFrameAnimator : MonoBehaviour
    {
        [Header("Target")]

        [Tooltip("걷기 프레임을 실제로 보여줄 SpriteRenderer입니다. 비워두면 이 오브젝트 또는 자식에서 자동으로 찾습니다.")]
        [SerializeField] private SpriteRenderer targetRenderer;

        [Tooltip("체크하면 Awake 때 targetRenderer가 비어 있을 경우 자동으로 SpriteRenderer를 찾습니다.")]
        [SerializeField] private bool autoFindRenderer = true;

        [Header("Walk Frames")]

        [Tooltip("보스 걷기 애니메이션 프레임입니다. 0번부터 순서대로 재생됩니다.")]
        [SerializeField] private Sprite[] walkFrames;

        [Tooltip("한 프레임이 유지되는 시간입니다. 값이 작을수록 걷기 애니메이션이 빨라집니다.")]
        [SerializeField] private float frameTime = 0.12f;

        [Tooltip("체크하면 오브젝트가 켜질 때 자동으로 걷기 애니메이션을 시작합니다.")]
        [SerializeField] private bool playOnEnable = true;

        [Tooltip("체크하면 게임 시간이 멈춰도 애니메이션이 재생됩니다. 일반적으로 꺼두는 것을 추천합니다.")]
        [SerializeField] private bool useUnscaledTime = false;

        [Tooltip("체크하면 OnEnable 때 항상 0번 프레임부터 다시 시작합니다.")]
        [SerializeField] private bool resetToFirstFrameOnEnable = true;

        [Tooltip("체크하면 첫 프레임을 즉시 SpriteRenderer에 적용합니다.")]
        [SerializeField] private bool applyFirstFrameImmediately = true;

        [Header("Debug")]

        [Tooltip("체크하면 프레임 설정이 잘못되었을 때 경고 로그를 출력합니다.")]
        [SerializeField] private bool showWarnings = true;

        private int currentFrameIndex;
        private float timer;
        private bool isPlaying;

        private void Awake()
        {
            TryFindRendererIfNeeded();

            if (applyFirstFrameImmediately)
            {
                ApplyFrame(0);
            }
        }

        private void OnEnable()
        {
            TryFindRendererIfNeeded();

            if (resetToFirstFrameOnEnable)
            {
                ResetFrame();
            }

            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            if (!IsReady())
            {
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer += deltaTime;

            float safeFrameTime = Mathf.Max(0.01f, frameTime);

            while (timer >= safeFrameTime)
            {
                timer -= safeFrameTime;
                currentFrameIndex++;

                if (currentFrameIndex >= walkFrames.Length)
                {
                    currentFrameIndex = 0;
                }

                ApplyFrame(currentFrameIndex);
            }
        }

        public void Play()
        {
            if (!IsReady())
            {
                if (showWarnings)
                {
                    Debug.LogWarning("[BossWalkFrameAnimator] 재생할 수 없습니다. Target Renderer 또는 Walk Frames를 확인하세요.", this);
                }

                return;
            }

            isPlaying = true;
        }

        public void Stop()
        {
            isPlaying = false;
        }

        public void ResetFrame()
        {
            timer = 0f;
            currentFrameIndex = 0;
            ApplyFrame(currentFrameIndex);
        }

        public void SetTargetRenderer(SpriteRenderer renderer)
        {
            targetRenderer = renderer;

            if (applyFirstFrameImmediately)
            {
                ApplyFrame(currentFrameIndex);
            }
        }

        public void SetWalkFrames(Sprite[] frames, bool resetFrame = true)
        {
            walkFrames = frames;

            if (resetFrame)
            {
                ResetFrame();
            }
        }

        private void ApplyFrame(int frameIndex)
        {
            if (targetRenderer == null)
            {
                return;
            }

            if (walkFrames == null || walkFrames.Length == 0)
            {
                return;
            }

            frameIndex = Mathf.Clamp(frameIndex, 0, walkFrames.Length - 1);

            Sprite frame = walkFrames[frameIndex];

            if (frame == null)
            {
                return;
            }

            targetRenderer.sprite = frame;
        }

        private bool IsReady()
        {
            if (targetRenderer == null)
            {
                return false;
            }

            if (walkFrames == null || walkFrames.Length == 0)
            {
                return false;
            }

            return true;
        }

        private void TryFindRendererIfNeeded()
        {
            if (!autoFindRenderer)
            {
                return;
            }

            if (targetRenderer != null)
            {
                return;
            }

            targetRenderer = GetComponent<SpriteRenderer>();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (frameTime < 0.01f)
            {
                frameTime = 0.01f;
            }

            if (autoFindRenderer && targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();

                if (targetRenderer == null)
                {
                    targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
                }
            }

            if (applyFirstFrameImmediately && targetRenderer != null && walkFrames != null && walkFrames.Length > 0 && walkFrames[0] != null)
            {
                targetRenderer.sprite = walkFrames[0];
            }
        }
#endif
    }
}