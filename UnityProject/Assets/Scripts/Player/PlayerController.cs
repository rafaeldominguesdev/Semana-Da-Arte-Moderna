using UnityEngine;
using UnityEngine.Events;

namespace MuseumModerna
{
    /// <summary>
    /// Estado atual do player no museu.
    /// </summary>
    public enum PlayerState
    {
        /// <summary>Player está caminhando pelo museu.</summary>
        Walking,
        /// <summary>Player está parado visualizando um quadro de perto.</summary>
        Viewing,
        /// <summary>Jogo pausado (menu, loading, etc).</summary>
        Paused
    }

    /// <summary>
    /// Script mestre do Player. Gerencia o estado do player, detecta proximidade
    /// com quadros e coordena os sistemas de giroscópio e movimento.
    ///
    /// Como usar:
    ///   1. Adicione ao GameObject raiz do Player (que tem CharacterController).
    ///   2. Arraste as referências de GyroscopeController e HeadGazeMovement.
    ///   3. Chame Calibrate() via botão de UI para recalibrar o giroscópio.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // ─── Configurações no Inspector ───────────────────────────────────────

        [Header("Referências dos Sistemas")]
        [Tooltip("Controlador do giroscópio (deve estar na câmera ou seu pai)")]
        [SerializeField] private GyroscopeController gyroController;

        [Tooltip("Sistema de movimento por head-gaze (deve estar neste mesmo objeto)")]
        [SerializeField] private HeadGazeMovement headGazeMovement;

        [Header("Detecção de Quadros")]
        [Tooltip("Raio em metros para detectar quadros próximos")]
        [SerializeField] private float paintingDetectionRadius = 2f;

        [Tooltip("Layer Mask dos quadros (configure a layer 'Painting' no Editor)")]
        [SerializeField] private LayerMask paintingLayer;

        [Tooltip("Intervalo em segundos entre cada verificação de proximidade (0 = todo frame)")]
        [SerializeField] private float detectionInterval = 0.2f;

        [Header("Eventos")]
        [Tooltip("Disparado quando o player se aproxima de um quadro")]
        public UnityEvent<PaintingInfo> OnNearPainting;

        [Tooltip("Disparado quando o player se afasta de um quadro")]
        public UnityEvent OnLeavePainting;

        // ─── Propriedades Públicas ─────────────────────────────────────────────

        /// <summary>Estado atual do player.</summary>
        public PlayerState State { get; private set; } = PlayerState.Walking;

        /// <summary>O quadro mais próximo atual (null se nenhum).</summary>
        public PaintingInfo CurrentNearPainting { get; private set; }

        // ─── Variáveis Privadas ────────────────────────────────────────────────

        // Timer para controlar o intervalo de detecção
        private float _detectionTimer = 0f;

        // Referência ao último quadro próximo (para detectar mudança de estado)
        private GameObject _lastNearPaintingObject = null;

        // Colliders reutilizáveis para OverlapSphere (evita alocação de array todo frame)
        private readonly Collider[] _overlapResults = new Collider[5];

        // ─── Ciclo de Vida Unity ──────────────────────────────────────────────

        private void Awake()
        {
            // Busca referências automáticas se não foram definidas no Inspector
            if (gyroController == null)
                gyroController = GetComponentInChildren<GyroscopeController>();

            if (headGazeMovement == null)
                headGazeMovement = GetComponent<HeadGazeMovement>();

            // Valida referências críticas
            if (gyroController == null)
                Debug.LogError("[MuseumModerna] PlayerController: GyroscopeController não encontrado!");

            if (headGazeMovement == null)
                Debug.LogError("[MuseumModerna] PlayerController: HeadGazeMovement não encontrado!");
        }

        private void Update()
        {
            if (State == PlayerState.Paused) return;

            // Atualiza timer de detecção
            _detectionTimer += Time.deltaTime;

            // Só verifica quadros no intervalo definido (evita overhead)
            if (_detectionTimer >= detectionInterval)
            {
                _detectionTimer = 0f;
                CheckNearbyPaintings();
            }

            // Atualiza o estado de movimento baseado no estado do player
            UpdateMovementBasedOnState();
        }

        // ─── Detecção de Quadros ──────────────────────────────────────────────

        /// <summary>
        /// Verifica se há quadros dentro do raio de detecção usando OverlapSphere.
        /// Reutiliza array para evitar garbage collection.
        /// </summary>
        private void CheckNearbyPaintings()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                paintingDetectionRadius,
                _overlapResults,
                paintingLayer
            );

            if (count > 0)
            {
                // Encontrou quadro(s) próximo(s)
                GameObject nearestPainting = FindNearestPainting(count);

                if (nearestPainting != _lastNearPaintingObject)
                {
                    _lastNearPaintingObject = nearestPainting;

                    // Tenta obter o PaintingInfo do quadro encontrado
                    PaintingExhibit exhibit = nearestPainting.GetComponent<PaintingExhibit>();
                    if (exhibit != null && exhibit.PaintingData != null)
                    {
                        CurrentNearPainting = exhibit.PaintingData;
                        SetState(PlayerState.Viewing);
                        OnNearPainting?.Invoke(CurrentNearPainting);
                        Debug.Log($"[MuseumModerna] Próximo ao quadro: {CurrentNearPainting.title}");
                    }
                }
            }
            else
            {
                // Nenhum quadro próximo
                if (_lastNearPaintingObject != null)
                {
                    _lastNearPaintingObject = null;
                    CurrentNearPainting = null;
                    SetState(PlayerState.Walking);
                    OnLeavePainting?.Invoke();
                    Debug.Log("[MuseumModerna] Player se afastou do quadro.");
                }
            }
        }

        /// <summary>
        /// Dentre os colisores encontrados, retorna o mais próximo do player.
        /// </summary>
        private GameObject FindNearestPainting(int count)
        {
            GameObject nearest = _overlapResults[0].gameObject;
            float nearestDist = Vector3.Distance(transform.position, nearest.transform.position);

            for (int i = 1; i < count; i++)
            {
                float dist = Vector3.Distance(transform.position, _overlapResults[i].transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = _overlapResults[i].gameObject;
                }
            }

            return nearest;
        }

        // ─── Gerenciamento de Estado ──────────────────────────────────────────

        /// <summary>
        /// Define o novo estado do player e atualiza os sistemas relacionados.
        /// </summary>
        private void SetState(PlayerState newState)
        {
            if (State == newState) return;

            State = newState;

            switch (newState)
            {
                case PlayerState.Walking:
                    // Permite movimento ao caminhar
                    headGazeMovement?.SetMovementEnabled(true);
                    break;

                case PlayerState.Viewing:
                    // Trava movimento ao visualizar um quadro
                    headGazeMovement?.SetMovementEnabled(false);
                    break;

                case PlayerState.Paused:
                    // Trava tudo ao pausar
                    headGazeMovement?.SetMovementEnabled(false);
                    break;
            }

            Debug.Log($"[MuseumModerna] Estado do player: {newState}");
        }

        /// <summary>
        /// Garante que o movimento só ocorre no estado Walking.
        /// </summary>
        private void UpdateMovementBasedOnState()
        {
            // O HeadGazeMovement já é controlado via SetMovementEnabled,
            // mas aqui podemos adicionar lógica extra se necessário.
        }

        // ─── API Pública ──────────────────────────────────────────────────────

        /// <summary>
        /// Realiza a calibração do giroscópio.
        /// Conecte este método ao botão "Calibrar" da UI.
        /// </summary>
        public void Calibrate()
        {
            gyroController?.Calibrate();
            Debug.Log("[MuseumModerna] Calibração realizada via PlayerController.");
        }

        /// <summary>
        /// Pausa ou despausa o player.
        /// </summary>
        public void SetPaused(bool paused)
        {
            SetState(paused ? PlayerState.Paused : PlayerState.Walking);
        }

        // ─── Debug Visual ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualiza o raio de detecção de quadros
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, paintingDetectionRadius);
            Gizmos.color = new Color(1f, 0.8f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, paintingDetectionRadius);
        }
#endif
    }

    // ─── Componente Auxiliar ──────────────────────────────────────────────────

    /// <summary>
    /// Componente que deve ser adicionado ao GameObject de cada quadro na cena.
    /// Associa o objeto 3D com seus dados (PaintingInfo).
    /// </summary>
    public class PaintingExhibit : MonoBehaviour
    {
        [Tooltip("ScriptableObject com as informações deste quadro")]
        [SerializeField] private PaintingInfo paintingData;

        /// <summary>Dados deste quadro (título, artista, descrição, etc).</summary>
        public PaintingInfo PaintingData => paintingData;
    }
}
