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

        // Healing Block
        // HP 1 전설 증강처럼 회복이 금지되는 상태일 때 true.
        // true이면 모기침 회복이 발동하지 않는다.
        public bool healingBlocked;

        // Legendary bonus
        public float rangeBonus;
    }
}