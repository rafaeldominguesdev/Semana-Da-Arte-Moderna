using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Mantém o objeto (Câmera) estritamente travado na sua posição inicial,
    /// impedindo qualquer tipo de queda, gravidade ou física.
    /// </summary>
    public class LockPosition : MonoBehaviour
    {
        private Vector3 _startPosition;

        private void Start()
        {
            _startPosition = transform.position;
            
            // Tenta destruir qualquer Rigidbody ou CharacterController que possa causar queda
            var rb = GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            var cc = GetComponent<CharacterController>();
            if (cc != null) Destroy(cc);
        }

        private void LateUpdate()
        {
            // Garante que a posição nunca mude do ponto inicial (apenas a rotação muda)
            transform.position = _startPosition;
        }
    }
}
