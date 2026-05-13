using UnityEngine;

namespace Vampire
{
    [DisallowMultipleComponent]
    public class PlayerTrapBindRuntime : MonoBehaviour
    {
        private Character ownerCharacter;
        private Rigidbody2D cachedRigidbody;

        private TrapMonster currentTrap;
        private bool isBound = false;
        private Vector3 lockedWorldPosition;

        public bool IsBound => isBound;
        public TrapMonster CurrentTrap => currentTrap;

        public static PlayerTrapBindRuntime GetOrCreate(Character character)
        {
            if (character == null)
            {
                return null;
            }

            PlayerTrapBindRuntime runtime = character.GetComponent<PlayerTrapBindRuntime>();

            if (runtime == null)
            {
                runtime = character.gameObject.AddComponent<PlayerTrapBindRuntime>();
            }

            runtime.Init(character);
            return runtime;
        }

        public void Init(Character character)
        {
            ownerCharacter = character;

            if (cachedRigidbody == null)
            {
                cachedRigidbody = character.GetComponent<Rigidbody2D>();
            }
        }

        public bool TryBind(TrapMonster trap, Vector3 worldPosition)
        {
            if (trap == null)
            {
                return false;
            }

            if (isBound && currentTrap != null && currentTrap != trap)
            {
                return false;
            }

            currentTrap = trap;
            isBound = true;
            lockedWorldPosition = worldPosition;

            ForceLockNow();

            Debug.Log("[PlayerTrapBindRuntime] 플레이어 구속 시작");
            return true;
        }

        public void Release(TrapMonster trap)
        {
            if (!isBound)
            {
                return;
            }

            if (trap != null && currentTrap != trap)
            {
                return;
            }

            isBound = false;
            currentTrap = null;

            if (cachedRigidbody != null)
            {
                cachedRigidbody.velocity = Vector2.zero;
                cachedRigidbody.angularVelocity = 0f;
            }

            Debug.Log("[PlayerTrapBindRuntime] 플레이어 구속 해제");
        }

        private void LateUpdate()
        {
            if (!isBound)
            {
                return;
            }

            ForceLockNow();
        }

        private void ForceLockNow()
        {
            transform.position = lockedWorldPosition;

            if (cachedRigidbody != null)
            {
                cachedRigidbody.velocity = Vector2.zero;
                cachedRigidbody.angularVelocity = 0f;
                cachedRigidbody.position = lockedWorldPosition;
            }
        }

        private void OnDisable()
        {
            isBound = false;
            currentTrap = null;
        }
    }
}