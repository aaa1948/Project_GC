using System.Collections;
using UnityEngine;

public class AcidRainScreenVFXController : MonoBehaviour
{
    [Header("파티클 설정")]
    [Tooltip("위산비 효과에 사용할 파티클 시스템들입니다. 비 파티클, 튐 파티클 등을 여러 개 넣을 수 있습니다. 비워두면 자식 오브젝트에서 자동으로 찾습니다.")]
    [SerializeField] private ParticleSystem[] particleSystems;

    [Tooltip("위산비가 시작될 때 파티클 방출량이 서서히 증가하는 시간입니다.")]
    [SerializeField] private float fadeInTime = 0.25f;

    [Tooltip("위산비가 종료될 때 파티클 방출량이 서서히 감소하는 시간입니다.")]
    [SerializeField] private float fadeOutTime = 0.8f;

    [Tooltip("위산비 종료 후 남아 있는 파티클이 자연스럽게 사라질 때까지 기다리는 시간입니다.")]
    [SerializeField] private float waitBeforeClearTime = 1.2f;

    [Header("시작 옵션")]
    [Tooltip("게임 시작과 동시에 위산비를 재생할지 여부입니다. 위산분비 이벤트 때만 켤 예정이면 꺼두세요.")]
    [SerializeField] private bool playOnStart = false;

    [Tooltip("게임 시작 시 파티클을 완전히 정지하고 비워둘지 여부입니다.")]
    [SerializeField] private bool clearOnStart = true;

    [Header("테스트 옵션")]
    [Tooltip("체크하면 플레이 모드에서 테스트 키로 위산비를 켜고 끌 수 있습니다.")]
    [SerializeField] private bool useKeyboardTest = true;

    [Tooltip("위산비 테스트에 사용할 키입니다.")]
    [SerializeField] private KeyCode testToggleKey = KeyCode.V;

    private float[] originalEmissionRates;
    private Coroutine fadeCoroutine;
    private bool isPlaying;

    private void Awake()
    {
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        originalEmissionRates = new float[particleSystems.Length];

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null)
                continue;

            ParticleSystem.EmissionModule emission = particleSystems[i].emission;
            originalEmissionRates[i] = emission.rateOverTime.constant;
        }
    }

    private void Start()
    {
        if (clearOnStart)
        {
            StopImmediately();
        }

        if (playOnStart)
        {
            PlayRain();
        }
    }

    private void Update()
    {
        if (!useKeyboardTest)
            return;

        if (Input.GetKeyDown(testToggleKey))
        {
            if (isPlaying)
            {
                StopRain();
            }
            else
            {
                PlayRain();
            }
        }
    }

    [ContextMenu("Play Acid Rain")]
    public void PlayRain()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(PlayRainRoutine());
    }

    [ContextMenu("Stop Acid Rain")]
    public void StopRain()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(StopRainRoutine());
    }

    [ContextMenu("Stop Immediately")]
    public void StopImmediately()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];

            if (ps == null)
                continue;

            SetEmissionRate(ps, 0f);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        isPlaying = false;
    }

    private IEnumerator PlayRainRoutine()
    {
        isPlaying = true;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];

            if (ps == null)
                continue;

            SetEmissionRate(ps, 0f);
            ps.Play(true);
        }

        float timer = 0f;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;

            float t = fadeInTime <= 0f ? 1f : Mathf.Clamp01(timer / fadeInTime);

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];

                if (ps == null)
                    continue;

                float targetRate = originalEmissionRates[i];
                float currentRate = Mathf.Lerp(0f, targetRate, t);

                SetEmissionRate(ps, currentRate);
            }

            yield return null;
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];

            if (ps == null)
                continue;

            SetEmissionRate(ps, originalEmissionRates[i]);
        }

        fadeCoroutine = null;
    }

    private IEnumerator StopRainRoutine()
    {
        isPlaying = false;

        float timer = 0f;
        float[] startRates = new float[particleSystems.Length];

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];

            if (ps == null)
                continue;

            ParticleSystem.EmissionModule emission = ps.emission;
            startRates[i] = emission.rateOverTime.constant;
        }

        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;

            float t = fadeOutTime <= 0f ? 1f : Mathf.Clamp01(timer / fadeOutTime);

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];

                if (ps == null)
                    continue;

                float currentRate = Mathf.Lerp(startRates[i], 0f, t);
                SetEmissionRate(ps, currentRate);
            }

            yield return null;
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];

            if (ps == null)
                continue;

            SetEmissionRate(ps, 0f);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(waitBeforeClearTime);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];

            if (ps == null)
                continue;

            ps.Clear(true);
        }

        fadeCoroutine = null;
    }

    private void SetEmissionRate(ParticleSystem ps, float rate)
    {
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
    }
}