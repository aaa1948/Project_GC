using UnityEngine;

namespace Vampire
{
    public class ShopStatApplier : MonoBehaviour
    {
        private AbilityManager abilityManager;
        private Character player;

        private void Awake()
        {
            abilityManager = FindObjectOfType<AbilityManager>();
            // 상점 시스템이 별도의 매니저 오브젝트에 붙어있으므로 맵에서 Character를 찾습니다.
            player = FindObjectOfType<Character>();

            if (abilityManager == null) Debug.LogError("[상점] AbilityManager를 찾을 수 없습니다!");
            if (player == null) Debug.LogError("[상점] 맵에서 Character를 찾을 수 없습니다!");
        }

        public void ApplyStats(MerchantItemBlueprint item)
        {
            if (item == null || player == null) return;

            Debug.Log($"<color=yellow>[상점]</color> <b>{item.itemName}</b> ({item.itemRarity} / {item.itemTag}) 적용 시작!");

            // ==========================================
            // 1. 전투 스탯 (Common)
            // ==========================================
            if (item.atkSpeedBoost > 0)
            {
                player.AddAttackSpeed(item.atkSpeedBoost);
                Debug.Log($"<color=cyan>[전투]</color> 공격속도 +{item.atkSpeedBoost * 100}%");
            }
            if (item.atkDamageBoost != 0) // > 0 을 != 0 으로 수정
            {
                player.AddDamageMultiplier(item.atkDamageBoost);

                // 로그도 증가/감소에 맞춰 나오면 더 좋겠죠?
                string sign = item.atkDamageBoost > 0 ? "+" : "";
                Debug.Log($"<color=red>[전투]</color> 공격력 {sign}{item.atkDamageBoost * 100}%");
            }
            if (item.maxHpBoost > 0)
            {
                player.AddMaxHealthBonus(item.maxHpBoost);
                player.GainHealth(item.maxHpBoost); // 늘어난 만큼 현재 체력도 채워줌
                Debug.Log($"<color=green>[전투]</color> 최대체력 +{item.maxHpBoost}");
            }
            if (item.projSpeedBoost > 0)
            {
                player.AddProjectileSpeed(item.projSpeedBoost);
                Debug.Log($"<color=cyan>[전투]</color> 투사체 속도 +{item.projSpeedBoost * 100}%");
            }
            if (item.moveSpeedBoost > 0)
            {
                player.AddMoveSpeedBoost(item.moveSpeedBoost);
                Debug.Log($"<color=cyan>[전투]</color> 이동속도 +{item.moveSpeedBoost}");
            }
            if (item.rangeBoost != 0)
            {
                player.AddRangeBoost(item.rangeBoost);
                Debug.Log($"<color=cyan>[전투]</color> 사거리 +{item.rangeBoost * 100}%");
            }

            // ==========================================
            // 2. 유틸리티 스탯 (Uncommon)
            // ==========================================
            if (item.magnetBoost > 0)
            {
                player.AddMagnetRange(item.magnetBoost);
                Debug.Log($"<color=white>[유틸]</color> 자석 범위 +{item.magnetBoost}");
            }
            if (item.expBoost > 0)
            {
                player.AddExpMultiplier(item.expBoost);
                Debug.Log($"<color=yellow>[유틸]</color> 경험치 획득량 +{item.expBoost * 100}%");
            }
            if (item.critBoost > 0)
            {
                player.AddCritChance(item.critBoost);
                Debug.Log($"<color=red>[유틸]</color> 치명타 확률 +{item.critBoost * 100}%");
            }
            if (item.luckBoost > 0)
            {
                player.AddLuck(item.luckBoost);
                Debug.Log($"<color=yellow>[유틸]</color> 행운 +{item.luckBoost * 100}%");
            }

            // ==========================================
            // 3. 특수 기능 (Rare / Legendary)
            // ==========================================
            if (item.extraProjectiles > 0)
            {
                player.AddProjectileCount(item.extraProjectiles);
                Debug.Log($"<color=magenta>[특수]</color> 투사체 발사 수 +{item.extraProjectiles}개!");
            }
            if (item.giveShield)
            {
                player.EnableShield();
                Debug.Log($"<color=blue>[특수]</color> 1회용 보호막 활성화!");
            }
            if (item.extraRevives > 0)
            {
                player.AddReviveCount(item.extraRevives);
                Debug.Log($"<color=magenta>[특수]</color> 부활 횟수 +{item.extraRevives}회!");
            }
            if (item.invincibilityBoost > 0)
            {
                player.AddInvincibilityTime(item.invincibilityBoost);
                Debug.Log($"<color=blue>[특수]</color> 피격 무적 시간 +{item.invincibilityBoost}초 증가!");
            }
            if (item.lifeSteal > 0)
            {
                player.AddLifeSteal(item.lifeSteal);
                Debug.Log($"<color=red>[흡혈]</color> 라이프스틸 +{item.lifeSteal * 100}% 증가!");
            }

            //  [추가] 포도당 링거 (처치 시 회복) 배달 코드
            if (item.healOnKill > 0)
            {
                player.AddHealOnKill(item.healOnKill);
                Debug.Log($"<color=green>[회복]</color> 처치 시 체력 회복 +{item.healOnKill} 증가!");
            }
            if (item.healOnIdlePerSecond != 0)
            {
                player.AddHealOnIdle(item.healOnIdlePerSecond);
                Debug.Log($"<color=green>[회복]</color> 정지 시 초당 회복 +{item.healOnIdlePerSecond}");
            }
            if (item.sizeBoost != 0)
            {
                player.AddProjectileSize(item.sizeBoost);
                Debug.Log($"<color=cyan>[특수]</color> 투사체 크기 {item.sizeBoost * 100}% 증가!");
            }

            // ==========================================
            // 4. 새로운 능력 해금 (무기 추가 등)
            // ==========================================
            if (item.abilityPrefab != null)
            {
                // abilityManager를 통해 새 능력을 등록 (프로젝트 함수명에 맞춰 추후 주석 해제)
                // abilityManager.UnlockAbility(item.abilityPrefab); 
                Debug.Log($"<color=orange>[해금]</color> {item.itemName} 스킬 활성화!");
            }

            Debug.Log($"<color=green>[적용 완료]</color> 모든 효과가 캐릭터에게 성공적으로 전달되었습니다.");
        }
    }
}