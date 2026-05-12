using System;

namespace Vampire
{
    [Serializable]
    public struct SyringeSpecialRuntime
    {
        // Poison
        public bool poisonEnabled;
        public float poisonDuration;
        public float poisonTickInterval;
        public float poisonTickDamage;

        // Explosion
        public bool explosionEnabled;
        public float explosionRadius;
        public float explosionDamage;

        // Homing
        public bool homingEnabled;
        public float homingRange;
        public float homingLerpSpeed;

        // Pierce
        // pierceCount는 "추가로 관통 가능한 횟수"입니다.
        // 예: pierceCount = 2라면 첫 적중 후 추가로 2번 더 관통 가능.
        public bool pierceEnabled;
        public int pierceCount;

        // Honey Needle
        // 꿀침: 적중한 몬스터를 일정 시간 둔화시킨다.
        public bool honeyEnabled;
        public float honeyDuration;
        public float honeySlowMultiplier;

        // Mosquito Needle
        // 모기침: 적중 시 플레이어 HP를 회복한다.
        public bool mosquitoEnabled;
        public float mosquitoHealPerHit;
        public float mosquitoBossHealMultiplier;

        // Return Needle / 침귀환
        // 침이 적중한 뒤 플레이어에게 되돌아오며, 귀환 경로의 적에게 피해를 준다.
        public bool returnNeedleEnabled;
        public float returnNeedleSpeedMultiplier;
        public float returnNeedleDamageMultiplier;
        public float returnNeedleArriveDistance;
        public float returnNeedleMaxDuration;

        // Healing Block
        // HP 1 전설 증강처럼 회복이 금지되는 상태일 때 true.
        // true이면 모기침 회복이 발동하지 않는다.
        public bool healingBlocked;

        // Legendary bonus
        public float rangeBonus;
    }
}