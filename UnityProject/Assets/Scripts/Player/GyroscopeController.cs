using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Controlador do giroscópio do dispositivo.
    /// Gerencia a rotação da câmera usando o sensor giroscópio (mobile)
    /// com fallback para mouse/touch em dispositivos sem o sensor.
    ///
    /// Como usar:
    ///   1. Adicione este script ao GameObject da câmera (ou a um pivô de câmera).
    ///   2. Chame Calibrate() via botão de UI para zerar a rotação atual.
    ///   3. Leia IsLookingDown para saber quando o player olha para baixo.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GyroscopeController : MonoBehaviour
    {
        // ─── Configurações no Inspector ───────────────────────────────────────

        [Header("Suavização")]
        [Tooltip("Fator de suavização da rotação (0 = instantâneo, 1 = nunca move). Recomendado: 0.05 - 0.15")]
        [SerializeField] private float smoothFactor = 0.1f;

        [Header("Limite de Olhar")]
        [Tooltip("Ângulo de pitch (em graus) abaixo do qual IsLookingDown = true. Ex: -30 = olhando 30° abaixo do horizonte")]
        [SerializeField] private float lookDownThreshold = -30f;

        [Tooltip("Pitch máximo para cima (graus positivos = olhar para cima)")]
        [SerializeField] private float pitchMax = 80f;

        [Tooltip("Pitch mínimo para baixo (graus negativos = olhar para baixo)")]
        [SerializeField] private float pitchMin = -80f;

        [Header("Fallback Touch/Mouse")]
        [Tooltip("Sensibilidade do arraste por touch/mouse quando não há giroscópio")]
        [SerializeField] private float touchSensitivity = 0.2f;

        // ─── Propriedades Públicas ─────────────────────────────────────────────

        /// <summary>
        /// Verdadeiro quando o usuário está olhando para baixo além do limiar definido.
        /// Usado por HeadGazeMovement para iniciar o caminhar.
        /// </summary>
        public bool IsLookingDown { get; private set; }

        /// <summary>
        /// Direção atual para onde a câmera aponta (vetor normalizado no espaço do mundo).
        /// </summary>
        public Vector3 CurrentLookDirection { get; private set; }

        /// <summary>
        /// Ângulo de pitch atual em graus (-80 a +80).
        /// Negativo = olhando para baixo, Positivo = olhando para cima.
        /// </summary>
        public float CurrentPitchDegrees { get; private set; }

        /// <summary>
        /// Verdadeiro se o dispositivo possui giroscópio e está sendo usado.
        /// </summary>
        public bool IsUsingGyroscope { get; private set; }

        // ─── Variáveis Privadas ────────────────────────────────────────────────

        // Rotação suavizada atual
        private Quaternion _targetRotation = Quaternion.identity;
        private Quaternion _currentRotation = Quaternion.identity;

        // Offset de calibração (zerado ao chamar Calibrate())
        private Quaternion _calibrationOffset = Quaternion.identity;

        // Correção de eixos: o giroscópio do Unity usa sistema de coordenadas diferente
        // É necessário rotacionar -90° no X para compensar
        private static readonly Quaternion GyroToUnity = Quaternion.Euler(90f, 0f, 0f);

        // Para fallback touch/mouse
        private float _touchYaw = 0f;    // Rotação horizontal (mouse/touch)
        private float _touchPitch = 0f;  // Rotação vertical (mouse/touch)
        private Vector2 _lastTouchPos;
        private bool _isDragging = false;

        // ─── Ciclo de Vida Unity ──────────────────────────────────────────────

        private void Start()
        {
            InitGyro();

            // Carrega calibração salva anteriormente
            _calibrationOffset = GyroCalibration.GetCalibrationOffset();
        }

        private void Update()
        {
            if (IsUsingGyroscope)
            {
                UpdateGyroscope();
            }
            else
            {
                UpdateFallbackInput();
            }

            // Aplica suavização
            _currentRotation = Quaternion.Slerp(_currentRotation, _targetRotation, smoothFactor);

            // Extrai o pitch atual para verificar "olhar para baixo"
            CurrentPitchDegrees = GetPitchFromQuaternion(_currentRotation);

            // Limita pitch entre pitchMin e pitchMax
            _currentRotation = ClampPitch(_currentRotation, pitchMin, pitchMax);

            // Atualiza propriedades públicas
            IsLookingDown = CurrentPitchDegrees < lookDownThreshold;
            CurrentLookDirection = _currentRotation * Vector3.forward;

            // Aplica rotação ao transform da câmera
            transform.localRotation = _currentRotation;
        }

        // ─── Inicialização ─────────────────────────────────────────────────────

        /// <summary>
        /// Tenta inicializar o giroscópio. Se não disponível, usa fallback.
        /// </summary>
        private void InitGyro()
        {
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                IsUsingGyroscope = true;
                Debug.Log("[MuseumModerna] Giroscópio ativado com sucesso.");
            }
            else
            {
                IsUsingGyroscope = false;
                Debug.LogWarning("[MuseumModerna] Giroscópio não disponível. Usando fallback touch/mouse.");
                FallbackToTouch();
            }
        }

        /// <summary>
        /// Configura estado inicial para o modo de fallback touch/mouse.
        /// </summary>
        private void FallbackToTouch()
        {
            _touchYaw = transform.localEulerAngles.y;
            _touchPitch = 0f;
        }

        // ─── Leitura do Giroscópio ─────────────────────────────────────────────

        /// <summary>
        /// Lê e processa a rotação do giroscópio físico do dispositivo.
        /// Compensa a diferença de eixos entre o sistema de coordenadas do
        /// giroscópio Android e o sistema de coordenadas do Unity.
        /// </summary>
        private void UpdateGyroscope()
        {
            // Lê a atitude bruta do giroscópio
            Quaternion rawGyro = Input.gyro.attitude;

            // Converte do sistema de coordenadas do giroscópio para o Unity:
            // O giroscópio usa: X=direita, Y=cima, Z=tela (para dentro)
            // Unity usa:        X=direita, Y=cima, Z=frente
            // A correção é: inverter Z e W, depois rotacionar 90° no X
            Quaternion gyroConverted = new Quaternion(rawGyro.x, rawGyro.y, -rawGyro.z, -rawGyro.w);
            gyroConverted = GyroToUnity * gyroConverted;

            // Aplica offset de calibração (remove a rotação inicial do dispositivo)
            _targetRotation = Quaternion.Inverse(_calibrationOffset) * gyroConverted;
        }

        // ─── Fallback Touch/Mouse ─────────────────────────────────────────────

        /// <summary>
        /// Processa entrada de touch ou mouse para rotação da câmera
        /// quando o giroscópio não está disponível (Editor, PCs, etc).
        /// </summary>
        private void UpdateFallbackInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            // No Editor/PC: usa o mouse com botão direito pressionado
            if (Input.GetMouseButtonDown(1))
            {
                _isDragging = true;
                _lastTouchPos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _lastTouchPos;
                _touchYaw += delta.x * touchSensitivity;
                _touchPitch -= delta.y * touchSensitivity;
                _lastTouchPos = Input.mousePosition;
            }
#else
            // No Mobile sem giroscópio: usa arraste de touch
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    _lastTouchPos = touch.position;
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 delta = touch.position - _lastTouchPos;
                    _touchYaw   += delta.x * touchSensitivity;
                    _touchPitch -= delta.y * touchSensitivity;
                    _lastTouchPos = touch.position;
                }
            }
#endif
            // Limita pitch do fallback também
            _touchPitch = Mathf.Clamp(_touchPitch, pitchMin, pitchMax);
            _targetRotation = Quaternion.Euler(_touchPitch, _touchYaw, 0f);
        }

        // ─── Calibração ───────────────────────────────────────────────────────

        /// <summary>
        /// Zera a rotação atual — a posição onde o usuário está segurando o
        /// dispositivo agora se torna o "centro" (olhar para frente).
        /// Salva o offset no PlayerPrefs para persistir entre sessões.
        /// </summary>
        public void Calibrate()
        {
            if (IsUsingGyroscope)
            {
                // A calibração captura a rotação atual e a define como "neutro"
                Quaternion rawGyro = Input.gyro.attitude;
                Quaternion gyroConverted = new Quaternion(rawGyro.x, rawGyro.y, -rawGyro.z, -rawGyro.w);
                _calibrationOffset = GyroToUnity * gyroConverted;

                // Persiste a calibração
                GyroCalibration.SaveCalibrationOffset(_calibrationOffset);
                Debug.Log("[MuseumModerna] Giroscópio calibrado e salvo.");
            }
            else
            {
                // No modo fallback, zera para olhar para frente
                _touchYaw = 0f;
                _touchPitch = 0f;
                Debug.Log("[MuseumModerna] Modo fallback: rotação zerada.");
            }
        }

        // ─── Utilitários ──────────────────────────────────────────────────────

        /// <summary>
        /// Extrai o ângulo de pitch (rotação no eixo X) de um Quaternion.
        /// Retorna valor em graus: negativo = olhando para baixo.
        /// </summary>
        private static float GetPitchFromQuaternion(Quaternion q)
        {
            // Converte para ângulos de Euler e normaliza entre -180 e +180
            float pitch = q.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            return -pitch; // Inverte: Unity considera X+ = olhar para baixo
        }

        /// <summary>
        /// Limita o pitch de um Quaternion entre os valores mínimo e máximo.
        /// </summary>
        private static Quaternion ClampPitch(Quaternion rotation, float minPitch, float maxPitch)
        {
            Vector3 euler = rotation.eulerAngles;
            // Normaliza o ângulo X para -180 a +180
            float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            // Limita (nota: no Unity, pitch negativo = olhar para cima, positivo = baixo)
            float clampedPitch = Mathf.Clamp(-pitch, minPitch, maxPitch);
            euler.x = -clampedPitch;
            return Quaternion.Euler(euler);
        }

        // ─── Cleanup ──────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (IsUsingGyroscope)
            {
                Input.gyro.enabled = false;
            }
        }

#if UNITY_EDITOR
        // Gizmo visual no Editor para debugar a direção de olhar
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsLookingDown ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position, CurrentLookDirection * 3f);
        }
#endif
    }
}
