using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    public class BossChargePattern : BossPatternBase
    {
        [Header("Charge Telegraph")]
        [Tooltip("돌진 전 경고선을 표시할 프리팹입니다. 비워두면 경고선 없이 돌진합니다.")]
        [SerializeField] private GameObject warningLinePrefab;

        [Tooltip("첫 번째 돌진 전에 경고선을 표시한 뒤 실제 돌진하기까지 기다리는 시간입니다.")]
        [SerializeField] private float warningTime = 0.9f;

        [Tooltip("경고선의 두께입니다.")]
        [SerializeField] private float warningWidth = 1.5f;

        [Tooltip("경고선 색상입니다.")]
        [SerializeField] private Color warningColor = new Color(0.4f, 0.85f, 1f, 0.45f);

        [Header("Charge Combo")]
        [Tooltip("1페이즈에서 연속으로 돌진하는 횟수입니다.")]
        [SerializeField] private int chargeCountPhase1 = 1;

        [Tooltip("2페이즈에서 연속으로 돌진하는 횟수입니다.")]
        [SerializeField] private int chargeCountPhase2 = 2;

        [Tooltip("3페이즈에서 연속으로 돌진하는 횟수입니다.")]
        [SerializeField] private int chargeCountPhase3 = 3;

        [Tooltip("2페이즈에서 돌진 후 다음 돌진까지의 텀입니다. 이 시간 동안 다음 돌진 경고선이 표시됩니다.")]
        [SerializeField] private float betweenChargeDelayPhase2 = 0.5f;

        [Tooltip("3페이즈에서 돌진 후 다음 돌진까지의 텀입니다. 이 시간 동안 다음 돌진 경고선이 표시됩니다.")]
        [SerializeField] private float betweenChargeDelayPhase3 = 0.2f;

        [Tooltip("체크하면 매 돌진 직전에 플레이어의 현재 위치를 다시 조준합니다.")]
        [SerializeField] private bool reAimBeforeEachCharge = true;

        [Tooltip("2회 이상 연속 돌진할 때 좌우로 살짝 꺾는 각도입니다. 0이면 정확히 플레이어 방향으로만 돌진합니다.")]
        [SerializeField] private float zigzagAngle = 12f;

        [Tooltip("체크하면 첫 지그재그 방향을 매번 랜덤으로 정합니다. 꺼두면 왼쪽/오른쪽 순서가 고정됩니다.")]
        [SerializeField] private bool randomizeFirstZigzagSide = true;

        [Header("Charge Distance")]
        [Tooltip("1페이즈에서 보스가 돌진하는 거리입니다.")]
        [SerializeField] private float chargeDistancePhase1 = 6f;

        [Tooltip("2페이즈에서 보스가 돌진하는 거리입니다.")]
        [SerializeField] private float chargeDistancePhase2 = 8f;

        [Tooltip("3페이즈에서 보스가 돌진하는 거리입니다.")]
        [SerializeField] private float chargeDistancePhase3 = 9f;

        [Header("Charge Speed")]
        [Tooltip("1페이즈 돌진 기본 속도입니다. 실제 속도는 BossController의 현재 페이즈 Movement Speed Multiplier가 곱해집니다.")]
        [SerializeField] private float chargeSpeedPhase1 = 14f;

        [Tooltip("2페이즈 돌진 기본 속도입니다. 실제 속도는 BossController의 현재 페이즈 Movement Speed Multiplier가 곱해집니다.")]
        [SerializeField] private float chargeSpeedPhase2 = 18f;

        [Tooltip("3페이즈 돌진 기본 속도입니다. 실제 속도는 BossController의 현재 페이즈 Movement Speed Multiplier가 곱해집니다.")]
        [SerializeField] private float chargeSpeedPhase3 = 20f;

        [Header("Charge Damage")]
        [Tooltip("1페이즈 돌진 기본 데미지입니다. 실제 데미지는 BossController의 현재 페이즈 Damage Multiplier가 곱해집니다.")]
        [SerializeField] private float chargeDamagePhase1 = 15f;

        [Tooltip("2페이즈 돌진 기본 데미지입니다. 실제 데미지는 BossController의 현재 페이즈 Damage Multiplier가 곱해집니다.")]
        [SerializeField] private float chargeDamagePhase2 = 25f;

        [Tooltip("3페이즈 돌진 기본 데미지입니다. 실제 데미지는 BossController의 현재 페이즈 Damage Multiplier가 곱해집니다.")]
        [SerializeField] private float chargeDamagePhase3 = 30f;

        [Tooltip("마지막 돌진이 끝난 뒤 보스가 잠깐 멈춰 있는 시간입니다.")]
        [SerializeField] private float endLag = 0.4f;

        [Header("Hit Settings")]
        [Tooltip("돌진 중 플레이어를 맞추는 히트박스 크기입니다.")]
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.2f, 1.2f);

        [Tooltip("플레이어가 속한 레이어입니다. 돌진 히트 판정에 사용됩니다. Player 레이어를 지정하세요.")]
        [SerializeField] private LayerMask playerLayer;

        [Tooltip("체크하면 한 번의 돌진 패턴 안에서 플레이어가 돌진마다 각각 한 번씩 맞을 수 있습니다.")]
        [SerializeField] private bool canHitPlayerOncePerCharge = true;

        [Tooltip("체크하면 Scene 뷰에서 돌진 히트박스 크기를 Gizmo로 표시합니다.")]
        [SerializeField] private bool showDebugHitbox = false;

        [Header("Debug")]
        [Tooltip("체크하면 돌진 패턴 실행, 속도, 데미지 로그를 Console에 출력합니다.")]
        [SerializeField] private bool debugCharge = false;

        protected override IEnumerator ExecutePattern()
        {
            if (bossController == null ||
                bossController.PlayerCharacter == null ||
                bossController.IsDead)
            {
                yield break;
            }

            bossController.SetExternalMovementLock(true);
            bossController.SetSuppressContactDamage(true);

            int chargeCount = Mathf.Max(1, GetPhaseChargeCount());
            float chargeDistance = GetPhaseChargeDistance();
            float chargeSpeed = bossController.GetModifiedMovementSpeed(GetPhaseChargeSpeed());
            float chargeDamage = bossController.GetModifiedDamage(GetPhaseChargeDamage());

            int firstZigzagSide = randomizeFirstZigzagSide && Random.value < 0.5f ? -1 : 1;

            HashSet<Character> sharedHitTargets = new HashSet<Character>();

            for (int i = 0; i < chargeCount; i++)
            {
                if (bossController == null ||
                    bossController.PlayerCharacter == null ||
                    bossController.IsDead ||
                    bossController.IsPhaseTransitioning)
                {
                    break;
                }

                Vector2 startPosition = bossController.BossCenterPosition;
                Vector2 direction = GetChargeDirection(startPosition, i, chargeCount, firstZigzagSide);

                float telegraphTime = i == 0
                    ? warningTime
                    : GetPhaseBetweenChargeDelay();

                GameObject warningObject = CreateWarningLine(startPosition, direction, chargeDistance);

                if (telegraphTime > 0f)
                {
                    yield return new WaitForSeconds(telegraphTime);
                }

                if (warningObject != null)
                {
                    Destroy(warningObject);
                }

                HashSet<Character> hitTargets = canHitPlayerOncePerCharge
                    ? new HashSet<Character>()
                    : sharedHitTargets;

                yield return StartCoroutine(
                    ChargeForward(
                        direction,
                        chargeDistance,
                        chargeSpeed,
                        chargeDamage,
                        hitTargets
                    )
                );

                if (debugCharge)
                {
                    Debug.Log(
                        $"[BossChargePattern] Charge {i + 1}/{chargeCount} complete / " +
                        $"phase={bossController.CurrentPhase}, " +
                        $"speed={chargeSpeed}, damage={chargeDamage}"
                    );
                }
            }

            yield return new WaitForSeconds(endLag);

            bossController.SetSuppressContactDamage(false);
            bossController.SetExternalMovementLock(false);
        }

        private int GetPhaseChargeCount()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return chargeCountPhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return chargeCountPhase2;
            }

            return chargeCountPhase1;
        }

        private float GetPhaseBetweenChargeDelay()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return betweenChargeDelayPhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return betweenChargeDelayPhase2;
            }

            return 0f;
        }

        private float GetPhaseChargeDistance()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return chargeDistancePhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return chargeDistancePhase2;
            }

            return chargeDistancePhase1;
        }

        private float GetPhaseChargeSpeed()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return chargeSpeedPhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return chargeSpeedPhase2;
            }

            return chargeSpeedPhase1;
        }

        private float GetPhaseChargeDamage()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return chargeDamagePhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return chargeDamagePhase2;
            }

            return chargeDamagePhase1;
        }

        private Vector2 GetChargeDirection(
            Vector2 startPosition,
            int chargeIndex,
            int totalChargeCount,
            int firstZigzagSide)
        {
            Vector2 targetPosition;

            if (reAimBeforeEachCharge && bossController.PlayerCharacter != null)
            {
                targetPosition = bossController.PlayerCharacter.transform.position;
            }
            else
            {
                targetPosition = startPosition + Vector2.right;
            }

            Vector2 direction = (targetPosition - startPosition).normalized;

            if (direction == Vector2.zero)
            {
                direction = Vector2.right;
            }

            if (totalChargeCount > 1 && Mathf.Abs(zigzagAngle) > 0.01f)
            {
                int side = chargeIndex % 2 == 0 ? firstZigzagSide : -firstZigzagSide;
                direction = RotateVector(direction, zigzagAngle * side);
            }

            return direction.normalized;
        }

        private GameObject CreateWarningLine(
            Vector2 startPosition,
            Vector2 direction,
            float chargeDistance)
        {
            if (warningLinePrefab == null)
            {
                if (debugCharge)
                {
                    Debug.LogWarning("[BossChargePattern] Warning Line Prefab이 비어 있어 경고선 없이 돌진합니다.");
                }

                return null;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float bossBodyOffset = 0.9f;
            float visualLength = Mathf.Max(0.1f, chargeDistance - bossBodyOffset);
            Vector2 visualStart = startPosition + direction * bossBodyOffset;
            Vector2 centerPosition = visualStart + direction * (visualLength * 0.5f);

            GameObject warning = Instantiate(
                warningLinePrefab,
                centerPosition,
                Quaternion.Euler(0f, 0f, angle)
            );

            warning.transform.localScale = new Vector3(visualLength, warningWidth, 1f);

            SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();

            if (sr == null)
            {
                sr = warning.GetComponentInChildren<SpriteRenderer>();
            }

            if (sr != null)
            {
                sr.color = warningColor;
                sr.sortingOrder = 100;
            }

            return warning;
        }

        private IEnumerator ChargeForward(
            Vector2 direction,
            float chargeDistance,
            float chargeSpeed,
            float chargeDamage,
            HashSet<Character> hitTargets)
        {
            Rigidbody2D rb = bossController.Rigidbody;
            float traveledDistance = 0f;

            while (traveledDistance < chargeDistance &&
                   !bossController.IsDead &&
                   !bossController.IsPhaseTransitioning)
            {
                float step = chargeSpeed * Time.fixedDeltaTime;

                Vector2 currentPosition = rb != null
                    ? rb.position
                    : (Vector2)bossController.transform.position;

                Vector2 nextPosition = currentPosition + direction * step;

                if (rb != null)
                {
                    rb.MovePosition(nextPosition);
                }
                else
                {
                    bossController.transform.position = nextPosition;
                }

                traveledDistance += step;

                CheckChargeHit(nextPosition, chargeDamage, hitTargets);

                yield return new WaitForFixedUpdate();
            }
        }

        private void CheckChargeHit(
            Vector2 center,
            float damage,
            HashSet<Character> hitTargets)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f, playerLayer);

            for (int i = 0; i < hits.Length; i++)
            {
                Character character = hits[i].GetComponentInParent<Character>();

                if (character == null)
                {
                    continue;
                }

                if (character != bossController.PlayerCharacter)
                {
                    continue;
                }

                if (hitTargets.Contains(character))
                {
                    continue;
                }

                hitTargets.Add(character);
                character.TakeDamage(damage);

                if (debugCharge)
                {
                    Debug.Log($"[BossChargePattern] Player hit / damage={damage}");
                }
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

        private void OnDrawGizmosSelected()
        {
            if (!showDebugHitbox)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(transform.position, hitboxSize);
        }
    }
}