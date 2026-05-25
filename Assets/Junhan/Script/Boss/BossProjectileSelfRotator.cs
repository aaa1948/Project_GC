using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// 보스 발사체가 자기 중심을 기준으로 회전하도록 만드는 추가 컴포넌트입니다.
    /// 기존 BossController, BossSimpleBullet 코드를 수정하지 않고 발사체 프리팹에 붙여서 사용합니다.
    /// </summary>
    public class BossProjectileSelfRotator : MonoBehaviour
    {
        [Header("Rotation Settings")]

        [Tooltip("체크하면 발사체가 자기 중심을 기준으로 회전합니다.")]
        [SerializeField] private bool enableRotation = true;

        [Tooltip("초당 회전 속도입니다. 양수는 반시계 방향, 음수는 시계 방향으로 회전합니다. 예: 90 = 1초에 90도 회전")]
        [SerializeField] private float rotationSpeed = 120f;

        [Tooltip("체크하면 발사체가 생성될 때 시작 각도를 랜덤으로 섞습니다.")]
        [SerializeField] private bool randomizeStartRotation = true;

        [Tooltip("체크하면 게임이 일시정지되어 Time.timeScale이 0이어도 회전합니다. 일반 전투 발사체는 꺼두는 것을 추천합니다.")]
        [SerializeField] private bool useUnscaledTime = false;

        private void OnEnable()
        {
            if (randomizeStartRotation)
            {
                float randomZ = Random.Range(0f, 360f);
                transform.rotation = Quaternion.Euler(0f, 0f, randomZ);
            }
        }

        private void Update()
        {
            if (!enableRotation)
            {
                return;
            }

            if (Mathf.Approximately(rotationSpeed, 0f))
            {
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(0f, 0f, rotationSpeed * deltaTime, Space.Self);
        }

        public void SetRotationSpeed(float newRotationSpeed)
        {
            rotationSpeed = newRotationSpeed;
        }

        public void SetRotationEnabled(bool value)
        {
            enableRotation = value;
        }
    }
}