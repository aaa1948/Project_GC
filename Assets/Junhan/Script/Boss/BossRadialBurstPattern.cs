using System.Collections;
using UnityEngine;

namespace Vampire
{
    public class BossRadialBurstPattern : BossPatternBase
    {
        [Header("Radial Burst Settings")]
        [Tooltip("원형 탄막에 사용할 보스 탄환 프리팹입니다.")]
        [SerializeField] private GameObject bulletPrefab;

        [Tooltip("원형 탄막의 기본 탄속입니다. 실제 탄속은 보스 현재 페이즈의 Projectile Speed Multiplier가 곱해집니다.")]
        [SerializeField] private float bulletSpeed = 6f;

        [Tooltip("원형 탄막의 기본 데미지입니다. 실제 데미지는 보스 현재 페이즈의 Damage Multiplier가 곱해집니다.")]
        [SerializeField] private float bulletDamage = 8f;

        [Tooltip("1페이즈에서 한 원에 발사할 탄환 수입니다.")]
        [SerializeField] private int bulletCountPhase1 = 12;

        [Tooltip("2페이즈에서 한 원에 발사할 탄환 수입니다.")]
        [SerializeField] private int bulletCountPhase2 = 20;

        [Tooltip("3페이즈에서 한 원에 발사할 탄환 수입니다.")]
        [SerializeField] private int bulletCountPhase3 = 24;

        [Tooltip("1페이즈에서 원형 탄막을 몇 겹 발사할지 정합니다.")]
        [SerializeField] private int ringCountPhase1 = 1;

        [Tooltip("2페이즈에서 원형 탄막을 몇 겹 발사할지 정합니다.")]
        [SerializeField] private int ringCountPhase2 = 2;

        [Tooltip("3페이즈에서 원형 탄막을 몇 겹 발사할지 정합니다.")]
        [SerializeField] private int ringCountPhase3 = 2;

        [Tooltip("원형 탄막을 여러 겹 발사할 때 다음 원 발사까지 기다리는 시간입니다.")]
        [SerializeField] private float ringInterval = 0.25f;

        [Header("Visual Sorting")]
        [Tooltip("체크하면 탄환 SpriteRenderer의 Sorting Order를 강제로 설정합니다.")]
        [SerializeField] private bool forceBulletSortingOrder = true;

        [Tooltip("탄환의 화면 표시 순서입니다.")]
        [SerializeField] private int bulletSortingOrder = 550;

        [Header("Debug")]
        [Tooltip("체크하면 발사 로그를 Console에 출력합니다.")]
        [SerializeField] private bool debugShot = false;

        protected override IEnumerator ExecutePattern()
        {
            int bulletCount = GetPhaseBulletCount();
            int ringCount = GetPhaseRingCount();

            for (int ring = 0; ring < ringCount; ring++)
            {
                FireRadialBurst(bulletCount, ring * 8f);

                if (ring < ringCount - 1)
                {
                    yield return new WaitForSeconds(ringInterval);
                }
            }
        }

        private int GetPhaseBulletCount()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return bulletCountPhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return bulletCountPhase2;
            }

            return bulletCountPhase1;
        }

        private int GetPhaseRingCount()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return ringCountPhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return ringCountPhase2;
            }

            return ringCountPhase1;
        }

        private void FireRadialBurst(int bulletCount, float angleOffset)
        {
            if (bulletPrefab == null)
            {
                Debug.LogWarning("[BossRadialBurstPattern] Bullet Prefab이 비어 있습니다.");
                return;
            }

            Vector3 spawnPosition = bossController.BossCenterPosition;

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = angleOffset + (360f / bulletCount) * i;
                Vector2 direction = AngleToDirection(angle);

                SpawnBullet(spawnPosition, direction);
            }

            if (debugShot)
            {
                Debug.Log($"[BossRadialBurstPattern] Fired {bulletCount} bullets / phase={bossController.CurrentPhase}");
            }
        }

        private void SpawnBullet(Vector3 spawnPosition, Vector2 direction)
        {
            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (forceBulletSortingOrder)
            {
                SpriteRenderer[] renderers = bullet.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (SpriteRenderer renderer in renderers)
                {
                    renderer.sortingOrder = bulletSortingOrder;
                }
            }

            BossSimpleBullet simpleBullet = bullet.GetComponent<BossSimpleBullet>();

            if (simpleBullet == null)
            {
                simpleBullet = bullet.GetComponentInChildren<BossSimpleBullet>();
            }

            if (simpleBullet == null)
            {
                simpleBullet = bullet.AddComponent<BossSimpleBullet>();
                Debug.LogWarning("[BossRadialBurstPattern] Bullet Prefab 루트에 BossSimpleBullet이 없어 자동 추가했습니다.");
            }

            float finalSpeed = bossController.GetModifiedProjectileSpeed(bulletSpeed);
            float finalDamage = bossController.GetModifiedDamage(bulletDamage);

            simpleBullet.Init(direction, finalSpeed, finalDamage);
        }

        private Vector2 AngleToDirection(float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        }
    }
}