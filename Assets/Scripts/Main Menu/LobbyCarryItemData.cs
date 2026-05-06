using UnityEngine;

namespace Vampire
{
    public enum LobbyCarryEffectType
    {
        MaxHpBonus,
        DamageMultiplier,
        AttackSpeedMultiplier,
        ProjectileSpeedMultiplier,
        MoveSpeedBonus,
        ProjectileCount,
        CritChance,
        Luck,
        Shield
    }

    [CreateAssetMenu(fileName = "Lobby Carry Item", menuName = "Lobby/Lobby Carry Item", order = 1)]
    public class LobbyCarryItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Shop")]
        public int silverCost = 100;

        [Header("Effect")]
        public LobbyCarryEffectType effectType;
        public float amount = 0.1f;
        public int intAmount = 1;

        public void Apply(Character character)
        {
            if (character == null)
            {
                return;
            }

            switch (effectType)
            {
                case LobbyCarryEffectType.MaxHpBonus:
                    character.AddMaxHealthBonus(amount);
                    break;

                case LobbyCarryEffectType.DamageMultiplier:
                    character.AddDamageMultiplier(amount);
                    break;

                case LobbyCarryEffectType.AttackSpeedMultiplier:
                    character.AddAttackSpeed(amount);
                    break;

                case LobbyCarryEffectType.ProjectileSpeedMultiplier:
                    character.AddProjectileSpeed(amount);
                    break;

                case LobbyCarryEffectType.MoveSpeedBonus:
                    character.AddMoveSpeedBoost(amount);
                    break;

                case LobbyCarryEffectType.ProjectileCount:
                    character.AddProjectileCount(intAmount);
                    break;

                case LobbyCarryEffectType.CritChance:
                    character.AddCritChance(amount);
                    break;

                case LobbyCarryEffectType.Luck:
                    character.AddLuck(amount);
                    break;

                case LobbyCarryEffectType.Shield:
                    character.EnableShield();
                    break;
            }

            Debug.Log($"[LobbyItem] Applied: {displayName}");
        }
    }
}