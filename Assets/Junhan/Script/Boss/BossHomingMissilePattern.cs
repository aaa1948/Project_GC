using System.Collections;
using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// 보스 호밍미사일 패턴입니다.
    ///
    /// 기존 BossFanShotPattern, BossRadialBurstPattern처럼
    /// Boss_Real 오브젝트에 직접 붙여서 사용합니다.
    ///
    /// BossController가 BossPatternBase를 상속한 컴포넌트를 수집하고,
    /// 거리별 Weight와 Cooldown을 기준으로 이 패턴을 선택합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossHomingMissilePattern : BossPatternBase
    {
        [Header("Homing Missile Settings")]
        [Tooltip("보스가 발사할 호밍미사일 프리팹입니다. Project 창의 Boss_HomingMissile 프리팹을 여기에 드래그해서 넣으세요.")]
        [SerializeField] private GameObject missilePrefab;

        [Tooltip("1페이즈에서 한 번에 발사할 호밍미사일 개수입니다. 요청한 기본값은 4발입니다.")]
        [SerializeField] private int missileCountPhase1 = 4;

        [Tooltip("2페이즈에서 한 번에 발사할 호밍미사일 개수입니다. 2페이즈를 더 어렵게 만들고 싶으면 5~6으로 올려도 됩니다.")]
        [SerializeField] private int missileCountPhase2 = 4;

        [Tooltip("플레이어 방향을 중심으로 미사일을 퍼뜨릴 전체 각도입니다. 90이면 -45도부터 +45도까지 부채꼴로 발사됩니다.")]
        [SerializeField] private float totalSpreadAngle = 90f;

        [Tooltip("호밍미사일의 이동 속도입니다. 플레이어 속도 1 기준 느린 탄환을 원하면 0.6 정도를 사용하세요.")]
        [SerializeField] private float missileSpeed = 0.6f;

        [Tooltip("호밍미사일이 플레이어에게 닿았을 때 입히는 데미지입니다.")]
        [SerializeField] private float missileDamage = 12f;

        [Tooltip("호밍미사일이 자동으로 사라지기까지 걸리는 시간입니다. 너무 오래 남으면 화면에 많이 쌓일 수 있습니다.")]
        [SerializeField] private float missileLifeTime = 12f;

        [Header("Homing Movement")]
        [Tooltip("호밍미사일이 플레이어 방향으로 꺾이는 속도입니다. 값이 높을수록 더 빠르게 추적합니다. 추천값: 2.2")]
        [SerializeField] private float turnSpeed = 2.2f;

        [Tooltip("여러 미사일이 서로 겹치지 않도록 좌우로 벌어지는 거리입니다. 값이 높을수록 각 미사일의 동선 차이가 커집니다. 추천값: 1.4")]
        [SerializeField] private float laneOffsetDistance = 1.4f;

        [Tooltip("플레이어에게 가까워질수록 좌우 동선 보정을 줄이는 거리입니다. 값이 높을수록 더 오래 서로 다른 경로로 날아갑니다. 추천값: 2.2")]
        [SerializeField] private float laneFadeDistance = 2.2f;

        [Tooltip("발사 직후 호밍을 시작하기 전, 부채꼴 방향으로 살짝 퍼지는 시간입니다. 값이 높을수록 처음에 더 크게 퍼진 뒤 추적합니다. 추천값: 0.35")]
        [SerializeField] private float initialSpreadDuration = 0.35f;

        [Header("Spawn Position")]
        [Tooltip("보스 중심에서 미사일이 생성될 거리입니다. 값이 너무 작으면 보스 몸 안에서 생성되고, 너무 크면 갑자기 튀어나온 것처럼 보입니다.")]
        [SerializeField] private float muzzleOffsetFromBoss = 0.9f;

        [Header("Burst")]
        [Tooltip("1페이즈에서 미사일 부채꼴 발사를 몇 번 반복할지 정합니다. 1이면 한 번만 4발 발사합니다.")]
        [SerializeField] private int burstCountPhase1 = 1;

        [Tooltip("2페이즈에서 미사일 부채꼴 발사를 몇 번 반복할지 정합니다. 2로 올리면 2페이즈에서 4발씩 2번 발사합니다.")]
        [SerializeField] private int burstCountPhase2 = 1;

        [Tooltip("Burst Count가 2 이상일 때, 다음 발사까지 기다리는 시간입니다.")]
        [SerializeField] private float burstInterval = 0.35f;

        [Header("Destroyed By Player Projectile")]
        [Tooltip("체크하면 플레이어의 침/투사체에 맞았을 때 호밍미사일이 파괴됩니다.")]
        [SerializeField] private bool destroyByPlayerProjectile = true;

        [Tooltip("호밍미사일을 파괴할 수 있는 플레이어 투사체 레이어입니다. 테스트 중에는 Everything으로 두면 편합니다. 나중에 PlayerProjectile 레이어가 있으면 그것만 선택하세요.")]
        [SerializeField] private LayerMask playerProjectileLayerMask = ~0;

        [Tooltip("침과 직접 Trigger 충돌이 안 잡힐 때 보조로 검사하는 반경입니다. 값이 클수록 침 근처에서도 미사일이 잘 파괴됩니다. 추천 테스트값: 0.25")]
        [SerializeField] private float projectileHitCheckRadius = 0.25f;

        [Header("Visual")]
        [Tooltip("체크하면 미사일 스프라이트가 이동 방향을 바라보도록 회전합니다.")]
        [SerializeField] private bool rotateToMoveDirection = true;

        [Tooltip("미사일 이미지의 기본 방향 보정값입니다. 이미지가 오른쪽을 보고 있으면 0, 위쪽이면 -90, 아래쪽이면 90, 왼쪽이면 180 정도로 조정하세요.")]
        [SerializeField] private float visualForwardAngleOffset = 0f;

        [Tooltip("미사일이 파괴될 때 생성할 이펙트 프리팹입니다. 없어도 동작하므로 처음에는 비워둬도 됩니다.")]
        [SerializeField] private GameObject destroyEffectPrefab;

        [Header("Visual Sorting")]
        [Tooltip("체크하면 미사일 SpriteRenderer의 Sorting Order를 아래 값으로 강제 설정합니다.")]
        [SerializeField] private bool forceMissileSortingOrder = true;

        [Tooltip("미사일의 화면 표시 순서입니다. 보스 몸보다 앞에 보여야 하면 보스보다 높은 값을, 뒤에 보여야 하면 낮은 값을 사용하세요.")]
        [SerializeField] private int missileSortingOrder = 560;

        [Header("Debug")]
        [Tooltip("체크하면 미사일 패턴이 실행될 때 Console에 발사 로그를 출력합니다. 테스트할 때만 켜세요.")]
        [SerializeField] private bool debugShot = false;

        protected override IEnumerator ExecutePattern()
        {
            if (bossController == null)
            {
                Debug.LogWarning("[BossHomingMissilePattern] bossController가 없습니다. Boss_Real에 BossController가 있는지 확인하세요.");
                yield break;
            }

            if (bossController.PlayerCharacter == null)
            {
                Debug.LogWarning("[BossHomingMissilePattern] PlayerCharacter가 없습니다. BossController가 플레이어를 찾았는지 확인하세요.");
                yield break;
            }

            int missileCount = bossController.CurrentPhase == 1
                ? missileCountPhase1
                : missileCountPhase2;

            int burstCount = bossController.CurrentPhase == 1
                ? burstCountPhase1
                : burstCountPhase2;

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

        private void FireHomingMissileFan(int missileCount)
        {
            if (missilePrefab == null)
            {
                Debug.LogWarning("[BossHomingMissilePattern] Missile Prefab이 비어 있습니다. Boss_Real의 BossHomingMissilePattern에 Boss_HomingMissile 프리팹을 넣어주세요.");
                return;
            }

            Character player = bossController.PlayerCharacter;

            if (player == null)
            {
                return;
            }

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

            /*
             * missileCount가 4이고 totalSpreadAngle이 90이면
             * -45도, -15도, +15도, +45도 방향으로 발사됩니다.
             */
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
                Debug.Log($"[BossHomingMissilePattern] Fired {missileCount} homing missiles");
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

            BossHomingMissile missile =
                missileObject.GetComponent<BossHomingMissile>();

            if (missile == null)
            {
                missile = missileObject.GetComponentInChildren<BossHomingMissile>();
            }

            if (missile == null)
            {
                missile = missileObject.AddComponent<BossHomingMissile>();

                Debug.LogWarning(
                    "[BossHomingMissilePattern] Missile Prefab에 BossHomingMissile이 없어 자동 추가했습니다. Boss_HomingMissile 프리팹에 BossHomingMissile 스크립트를 미리 붙이는 것을 추천합니다."
                );
            }

            missile.Init(
                bossController.PlayerCharacter,
                fireDirection,
                missileSpeed,
                missileDamage,
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