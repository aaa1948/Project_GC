using System.Collections;
using UnityEngine;

namespace Vampire
{
    public class BossBombLobPattern : BossPatternBase
    {
        [Header("Bomb Lob Settings")]
        [Tooltip("폭발 전 바닥에 표시할 경고 원 프리팹입니다.")]
        [SerializeField] private GameObject warningCirclePrefab;

        [Tooltip("경고 원이 표시된 뒤 실제 폭발하기까지 기다리는 시간입니다.")]
        [SerializeField] private float warningDuration = 1f;

        [Tooltip("폭발 반경입니다.")]
        [SerializeField] private float explosionRadius = 2f;

        [Tooltip("폭발 기본 데미지입니다. 실제 데미지는 보스 현재 페이즈의 Damage Multiplier가 곱해집니다.")]
        [SerializeField] private float explosionDamage = 20f;

        [Tooltip("1페이즈에서 떨어뜨릴 폭탄 수입니다.")]
        [SerializeField] private int bombCountPhase1 = 1;

        [Tooltip("2페이즈에서 떨어뜨릴 폭탄 수입니다.")]
        [SerializeField] private int bombCountPhase2 = 3;

        [Tooltip("3페이즈에서 떨어뜨릴 폭탄 수입니다.")]
        [SerializeField] private int bombCountPhase3 = 4;

        [Tooltip("플레이어 위치 주변으로 폭탄 위치를 흩뿌리는 랜덤 반경입니다.")]
        [SerializeField] private float randomOffsetRadius = 1.5f;

        [Header("Debug")]
        [Tooltip("체크하면 폭탄 패턴 로그를 Console에 출력합니다.")]
        [SerializeField] private bool debugBomb = false;

        protected override IEnumerator ExecutePattern()
        {
            if (bossController == null || bossController.PlayerCharacter == null)
            {
                yield break;
            }

            int bombCount = GetPhaseBombCount();

            for (int i = 0; i < bombCount; i++)
            {
                Vector2 targetPosition =
                    (Vector2)bossController.PlayerCharacter.transform.position +
                    Random.insideUnitCircle * randomOffsetRadius;

                yield return StartCoroutine(SpawnBombWarningAndExplode(targetPosition));
            }
        }

        private int GetPhaseBombCount()
        {
            if (bossController.CurrentPhase >= 3)
            {
                return bombCountPhase3;
            }

            if (bossController.CurrentPhase == 2)
            {
                return bombCountPhase2;
            }

            return bombCountPhase1;
        }

        private IEnumerator SpawnBombWarningAndExplode(Vector2 targetPosition)
        {
            GameObject warning = null;

            if (warningCirclePrefab != null)
            {
                warning = Instantiate(warningCirclePrefab, targetPosition, Quaternion.identity);
                warning.transform.localScale = Vector3.one * explosionRadius;
            }

            yield return new WaitForSeconds(warningDuration);

            if (warning != null)
            {
                Destroy(warning);
            }

            float finalDamage = bossController.GetModifiedDamage(explosionDamage);

            Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, explosionRadius);

            foreach (Collider2D hit in hits)
            {
                Character character = hit.GetComponentInParent<Character>();

                if (character == null)
                {
                    continue;
                }

                if (character != bossController.PlayerCharacter)
                {
                    continue;
                }

                character.TakeDamage(finalDamage);

                if (debugBomb)
                {
                    Debug.Log($"[BossBombLobPattern] Player hit / damage={finalDamage}, phase={bossController.CurrentPhase}");
                }
            }
        }
    }
}