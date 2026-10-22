using UnityEngine;
namespace Vampire
{
    // Stage-one exit visuals and rewards; no next-stage content is referenced.
    public sealed class StageExitDefinition:ScriptableObject
    {
        public Sprite crater;
        public ChestBlueprint rewardChest;
    }
}
