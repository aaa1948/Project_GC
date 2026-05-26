using System.Collections;
using UnityEngine;

namespace Vampire
{
    public class BossHomingMissilePattern : BossPatternBase
    {
        [Header("Homing Missile Settings")]
        [Tooltip("보스가 발사할 호밍미사일 프리팹입니다. Project 창의 Boss_HomingMissile 프리팹을 여기에 넣으세요.")]
        [SerializeField] private GameObject missilePrefab;

        [Tooltip("1페이즈에서 한 번에 발사할 호밍미사일 개수입니다.")]
        [SerializeField] private int missileCountPhase1 = 4;

        [Tooltip("2페이즈에서 한 번에 발사할 호밍미사일 개수입니다.")]
        [SerializeField] private int missileCountPhase2 = 4;

        [Tooltip("3페이즈에서 한 번에 발사할 호밍미사일 개수입니다.")]
        [SerializeField] private int missileCountPhase3 = 5;

        [Tooltip("플레이어 방향을 중심으로 미사일이 퍼질 전체 각도입니다. 90이면 -45도부터 +45도까지 발사됩니다.")]
        [SerializeField] private float totalSpreadAngle = 90f;

        [Tooltip("호밍미사일 기본 이동 속도입니다. 실제 속도는 보스 현재 페이즈의 Projectile Speed Multiplier가 곱해집니다.")]
        [SerializeField] private float missileSpeed = 0.6f;

        [Tooltip("호밍미사일 기본 데미지입니다. 실제 데미지는 보스 현재 페이즈의 Damage Multiplier가 곱해집니다.")]
        [SerializeField] private float missileDamage = 12f;

        [Tooltip("호밍미사일이 자동으로 사라지기까지 걸리는 시간입니다.")]
        [SerializeField] private float missileLifeTime = 12f;

        [Header("Homing Movement")]
        [Tooltip("호밍미사일이 플레이어 방향으로 꺾이는 속도입니다.")]
        [SerializeField] private float turnSpeed = 2.2f;

        [Tooltip("여러 미사일이 서로 겹치지 않도록 좌우로 벌어지는 거리입니다.")]
        [SerializeField] private float laneOffsetDistance = 1.4f;

        [Tooltip("플레이어에게 가까워질수록 좌우 동선 보정을 줄이는 거리입니다.")]
        [SerializeField] private float laneFadeDistance = 2.2f;

        [Tooltip("발사 직후 호밍을 시작하기 전, 부채꼴 방향으로 살짝 퍼지는 시간입니다.")]
        [SerializeField] private float initialSpreadDuration = 0.35f;

        [Header("Spawn Position")]
        [Tooltip("보스 중심에서 미사일이 생성될 거리입니다.")]
        [SerializeField] private float muzzleOffsetFromBoss = 0.9f;

        [Header("Burst")]
        [Tooltip("1페이즈에서 미사일 부채꼴 발사를 몇 번 반복할지 정합니다.")]
        [SerializeField] private int burstCountPhase1 = 1;

        [Tooltip("2페이즈에서 미사일 부채꼴 발사를 몇 번 반복할지 정합니다.")]
        [SerializeField] private int burstCountPhase2 = 1;

        [Tooltip("3페이즈에서 미사일 부채꼴 발사를 몇 번 반복할지 정합니다.")]
        [SerializeField] private int burstCountPhase3 = 2;

        [Tooltip("Burst Count가 2 이상일 때, 다음 발사까지 기다리는 시간입니다.")]
        [SerializeField] private float burstInterval = 0.35f;

        [Header("Destroyed By Player Projectile")]
        [Tooltip("체크하면 플레이어의 침/투사체에 맞았을 때 호밍미사일이 파괴됩니다.")]
        [SerializeField] private bool destroyByPlayerProjectile = true;

        [Tooltip("호밍미사일을 파괴할 수 있는 플레이어 투사체 레이어입니다. 테스트 중에는 Everything으로 두면 편합니다.")]
        [SerializeField] private LayerMask playerProjectileLayerMask = ~0;

        [Tooltip("침과 직접 Trigger 충돌이 안 잡힐 때 보조로 검사하는 반경입니다.")]
        [SerializeField] private float projectileHitCheckRadius = 0.25f;

        [Header("Visual")]
        [Tooltip("체크하면 미사일 스프라이트가 이동 방향을 바라보도록 회전합니다.")]
        [SerializeField] private bool rotateToMoveDirection = true;

        [Tooltip("미사일 이미지의 기본 방향 보정값입니다. 이미지가 오른쪽을 보고 있으면 0, 위쪽이면 -90, 아래쪽이면 90, 왼쪽이면 180 정도로 조정하세요.")]
        [SerializeField] private float visualForwardAngleOffset = 0f;

        [Tooltip("미사일이 파괴될 때 생성할 이펙트 프리팹입니다. 없어도 동작합니다.")]
        [SerializeField] private GameObject destroyEffectPrefab;

        [Header("Visual Sorting")]
        [Tooltip("체크하면 미사일 SpriteRenderer의 Sorting Order를 아래 값으로 강제 설정합니다.")]
        [SerializeField] private bool forceMissileSortingOrder = true;

        [Tooltip("미사일의 화면 표시 순서입니다.")]
        [SerializeField] private int missileSortingOrder = 560;

        [Header("Debug")]
        [Tooltip("체크하면 미사일 패턴이 실행될 때 Console에 발사 로그를 출력합니다.")]
        [SerializeField] private bool debugShot = false;

        protected override IEnumerator ExecutePattern()
        {
            if (bossController == null || bossController.PlayerCharacter == null)
            {
                yield break;
            }

            int missileCount = GetPhaseMissileCount();
            int burstCount = GetPhaseBurstCount();

            missileCount = Mathf.Max(1, missileCount);
            burstCount = Mathf.Max(1, burstCount);

            for (int burst = 0; burst < burstCount; burst++)
            {
                FireHomingMissileFan(missileCount);

                if (burst < burstCount - 1)
                {
                    yield return new WaitForSeconds(burstInterval);
                }
            }
        }

        private int GetPhaseMissileCount()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return missileCountPhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return missileCountPhase2;
            }

            return missileCountPhase1;
        }

        private int GetPhaseBurstCount()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return burstCountPhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return burstCountPhase2;
            }

            return burstCountPhase1;
        }

        private void FireHomingMissileFan(int missileCount)
        {
            if (missilePrefab == null)
            {
                Debug.LogWarning("[BossHomingMissilePattern] Missile Prefab이 비어 있습니다.");
                return;
            }

            Character player = bossController.PlayerCharacter;

            Vector3 bossPosition = bossController.BossCenterPosition;

            Transform playerTarget = player.CenterTransform != null
                ? player.CenterTransform
                : player.transform;

            Vector2 baseDirection =
                ((Vector2)playerTarget.position - (Vector2)bossPosition).normalized;

            if (baseDirection == Vector2.zero)
            {
                baseDirection = Vector2.right;
            }

            float startAngle = -totalSpreadAngle * 0.5f;
            float angleStep = missileCount > 1
                ? totalSpreadAngle / (missileCount - 1)
                : 0f;

            for (int i = 0; i < missileCount; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector2 fireDirection = RotateVector(baseDirection, angle);

                Vector3 spawnPosition =
                    bossPosition +
                    (Vector3)(fireDirection.normalized * muzzleOffsetFromBoss);

                SpawnMissile(spawnPosition, fireDirection, i, missileCount);
            }

            if (debugShot)
            {
                Debug.Log($"[BossHomingMissilePattern] Fired {missileCount} homing missiles / phase={bossController.CurrentPhase}");
            }
        }

        private void SpawnMissile(
            Vector3 spawnPosition,
            Vector2 fireDirection,
            int laneIndex,
            int laneCount
        )
        {
            GameObject missileObject = Instantiate(
                missilePrefab,
                spawnPosition,
                Quaternion.identity
            );

            ApplyMissileSortingOrder(missileObject);

            BossHomingMissile missile = missileObject.GetComponent<BossHomingMissile>();

            if (missile == null)
            {
                missile = missileObject.GetComponentInChildren<BossHomingMissile>();
            }

            if (missile == null)
            {
                missile = missileObject.AddComponent<BossHomingMissile>();

                Debug.LogWarning("[BossHomingMissilePattern] Missile Prefab에 BossHomingMissile이 없어 자동 추가했습니다.");
            }

            float finalSpeed = bossController.GetModifiedProjectileSpeed(missileSpeed);
            float finalDamage = bossController.GetModifiedDamage(missileDamage);

            missile.Init(
                bossController.PlayerCharacter,
                fireDirection,
                finalSpeed,
                finalDamage,
                missileLifeTime,
                laneIndex,
                laneCount,
                turnSpeed,
                laneOffsetDistance,
                laneFadeDistance,
                initialSpreadDuration,
                destroyByPlayerProjectile,
                playerProjectileLayerMask,
                projectileHitCheckRadius,
                rotateToMoveDirection,
                visualForwardAngleOffset,
                destroyEffectPrefab
            );
        }

        private void ApplyMissileSortingOrder(GameObject missileObject)
        {
            if (!forceMissileSortingOrder || missileObject == null)
            {
                return;
            }

            SpriteRenderer[] renderers =
                missileObject.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.sortingOrder = missileSortingOrder;
            }
        }

        private Vector2 RotateVector(Vector2 vector, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            ).normalized;
        }
    }
}