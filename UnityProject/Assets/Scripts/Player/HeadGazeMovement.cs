using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Controla o movimento do player baseado em "olhar para baixo".
    /// Quando GyroscopeController.IsLookingDown == true, o player caminha
    /// para frente no plano XZ (sem subir/descer).
    ///
    /// Como usar:
    ///   1. Adicione este script ao GameObject raiz do Player.
    ///   2. O Player deve ter um CharacterController anexado.
    ///   3. Arraste o GyroscopeController para o campo gyroController no Inspector.
    /// </summary>
    public class HeadGazeMovement : MonoBehaviour
    {
        // ─── Configurações no Inspector ───────────────────────────────────────

        [Header("Movimento")]
        [Tooltip("Velocidade de caminhada em m/s quando IsLookingDown = true")]
        [SerializeField] private float walkSpeed = 2.5f;

        [Tooltip("Fator de desaceleração ao parar de olhar para baixo (maior = para mais rápido)")]
        [SerializeField] private float deceleration = 5f;

        [Tooltip("Fator de aceleração ao começar a olhar para baixo (maior = acelera mais rápido)")]
        [SerializeField] private float acceleration = 8f;

        [Header("Gravidade")]
        [Tooltip("Força da gravidade aplicada ao CharacterController")]
        [SerializeField] private float gravity = -9.81f;

        [Header("Trava Anti-Queda")]
        [Tooltip("Trava o Y do player. Impede queda no vazio quando não há colisores no chão.")]
        [SerializeField] private bool lockYPosition = true;

        [Tooltip("Y fixo do player. 0 = usa automaticamente a posição inicial da cena.")]
        [SerializeField] private float lockedY = 0f;

        [Header("Referências")]
        [Tooltip("Referência ao GyroscopeController da câmera do player")]
        [SerializeField] private GyroscopeController gyroController;

        [Tooltip("Transform da câmera — usado para obter a direção de olhar horizontal")]
        [SerializeField] private Transform cameraTransform;

        // ─── Variáveis Privadas ────────────────────────────────────────────────

        // Referência ao CharacterController deste objeto
        private CharacterController _characterController;

        // Velocidade atual no plano XZ (com inércia)
        private float _currentSpeed = 0f;

        // Velocidade vertical para gravidade
        private float _verticalVelocity = 0f;

        // Y travado (calculado no Start a partir da posição inicial)
        private float _lockedYValue;

        // Flag para permitir/bloquear movimento (ex: durante cutscenes)
        private bool _movementEnabled = true;

        // ─── Ciclo de Vida Unity ──────────────────────────────────────────────

        private void Start()
        {
            // Registra o Y inicial como referência — é a posição correta dentro da sala
            _lockedYValue = (lockYPosition && lockedY == 0f)
                ? transform.position.y
                : lockedY;

            if (lockYPosition)
                Debug.Log($"[MuseumModerna] Y travado em {_lockedYValue:F2}. Player não pode cair.");
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            // Cria o CharacterController automaticamente se não existir na cena
            if (_characterController == null)
            {
                _characterController = gameObject.AddComponent<CharacterController>();
                _characterController.height = 1.8f;
                _characterController.radius = 0.3f;
                _characterController.center = new Vector3(0f, 0.9f, 0f);
                _characterController.slopeLimit = 45f;
                _characterController.stepOffset = 0.3f;
                Debug.Log("[MuseumModerna] CharacterController adicionado automaticamente ao Player.");
            }

            // Se a câmera não foi definida, tenta encontrar a câmera principal
            if (cameraTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                }
                else
                {
                    Debug.LogError("[MuseumModerna] HeadGazeMovement: nenhum cameraTransform definido!");
                }
            }

            // Tenta encontrar GyroscopeController automaticamente se não definido
            if (gyroController == null)
            {
                gyroController = GetComponentInChildren<GyroscopeController>();
                if (gyroController == null)
                {
                    Debug.LogError("[MuseumModerna] HeadGazeMovement: GyroscopeController não encontrado!");
                }
            }
        }

        private void Update()
        {
            // Aplica gravidade sempre (CharacterController não tem física automática)
            ApplyGravity();

            // Atualiza movimento horizontal somente se habilitado
            if (_movementEnabled)
            {
                UpdateHorizontalMovement();
            }
            else
            {
                // Desacelera até parar quando movimento está desabilitado
                DecelerateToStop();
            }
        }

        // ─── Lógica de Movimento ──────────────────────────────────────────────

        /// <summary>
        /// Atualiza a velocidade e posição horizontal baseada no estado do giroscópio.
        /// </summary>
        private void UpdateHorizontalMovement()
        {
            if (gyroController == null || cameraTransform == null) return;

            bool isWalking = gyroController.IsLookingDown;

            if (isWalking)
            {
                // Acelera suavemente até walkSpeed
                _currentSpeed = Mathf.Lerp(_currentSpeed, walkSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                // Desacelera suavemente até zero (inércia)
                _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, deceleration * Time.deltaTime);

                // Evita vibração em velocidades muito baixas (threshold)
                if (_currentSpeed < 0.01f) _currentSpeed = 0f;
            }

            // Calcula a direção de movimento: usa a câmera, mas projeta no plano XZ
            // (ignora o componente Y para não voar quando olha para cima/baixo)
            Vector3 lookDir = cameraTransform.forward;
            lookDir.y = 0f; // Projeta no plano horizontal
            lookDir.Normalize();

            // Calcula o deslocamento final (movimento horizontal + gravidade vertical)
            Vector3 horizontalMove = lookDir * (_currentSpeed * Time.deltaTime);
            Vector3 totalMove = new Vector3(horizontalMove.x, _verticalVelocity * Time.deltaTime, horizontalMove.z);

            // Move usando CharacterController (resolve colisões automaticamente)
            if (_characterController != null)
            {
                _characterController.Move(totalMove);
            }
        }

        /// <summary>
        /// Desacelera o player até parar (quando movimento está desabilitado).
        /// </summary>
        private void DecelerateToStop()
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, deceleration * Time.deltaTime);
            if (_currentSpeed < 0.01f) _currentSpeed = 0f;

            // Ainda aplica gravidade
            if (_characterController != null)
            {
                Vector3 gravMove = new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f);
                _characterController.Move(gravMove);
            }
        }

        /// <summary>
        /// Aplica gravidade OU trava Y dependendo da configuração.
        /// Com lockYPosition ativo, o player NUNCA cai — independente de colisores.
        /// </summary>
        private void ApplyGravity()
        {
            if (lockYPosition)
            {
                _verticalVelocity = 0f;

                // Corrige Y se o player desviou por qualquer razão
                float currentY = transform.position.y;
                if (Mathf.Abs(currentY - _lockedYValue) > 0.02f)
                {
                    if (_characterController != null) _characterController.enabled = false;
                    transform.position = new Vector3(transform.position.x, _lockedYValue, transform.position.z);
                    if (_characterController != null) _characterController.enabled = true;
                }
                return;
            }

            if (_characterController != null && _characterController.isGrounded)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }
        }

        // ─── API Pública ──────────────────────────────────────────────────────

        /// <summary>
        /// Habilita ou desabilita o movimento do player.
        /// Use para travar durante cutscenes, menus, ou ao visualizar um quadro.
        /// </summary>
        /// <param name="enabled">True para permitir movimento, false para bloquear.</param>
        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;

            if (!enabled)
            {
                Debug.Log("[MuseumModerna] Movimento do player desabilitado.");
            }
        }

        /// <summary>
        /// Retorna verdadeiro se o player está ativamente se movendo.
        /// </summary>
        public bool IsMoving => _currentSpeed > 0.05f;

        /// <summary>
        /// Retorna a velocidade atual do player em m/s.
        /// </summary>
        public float CurrentSpeed => _currentSpeed;

        // ─── Debug Visual ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (cameraTransform == null) return;

            // Mostra a direção de movimento projetada no plano XZ
            Vector3 flatForward = cameraTransform.forward;
            flatForward.y = 0f;
            flatForward.Normalize();

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, flatForward * 2f);
        }
#endif
    }
}
