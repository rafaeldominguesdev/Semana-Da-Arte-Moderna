using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// DEPRECATED — Este script foi desativado.
    /// Ele destruía o CharacterController e travava a posição, impedindo gravidade e colisão.
    /// Pode ser removido da hierarquia com segurança; o CharacterController em HeadGazeMovement
    /// já mantém o player apoiado no chão.
    /// </summary>
    public class LockPosition : MonoBehaviour
    {
        private void Awake()
        {
            Debug.LogWarning(
                "[MuseumModerna] LockPosition está DESATIVADO. " +
                "Remova este componente do GameObject — o CharacterController já cuida da física.",
                this);
            enabled = false;
        }
    }
}
