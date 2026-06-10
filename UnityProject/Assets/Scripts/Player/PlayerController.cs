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

        private float _detectionTimer = 0f;
        private GameObject _lastNearPaintingObject = null;

        // Reutiliza array para evitar garbage collection por frame
        private readonly Collider[] _overlapResults = new Collider[5];

        // ─── Ciclo de Vida Unity ──────────────────────────────────────────────

        private void Awake()
        {
            // Busca referências automáticas se não foram definidas no Inspector
            if (gyroController == null)
                gyroController = GetComponentInChildren<GyroscopeController>();

            if (headGazeMovement == null)
                headGazeMovement = GetComponent<HeadGazeMovement>();

            if (gyroController == null)
                Debug.LogError("[MuseumModerna] PlayerController: GyroscopeController não encontrado! Adicione-o à câmera.", this);

            if (headGazeMovement == null)
                Debug.LogError("[MuseumModerna] PlayerController: HeadGazeMovement não encontrado!", this);

            // Se paintingLayer não foi configurado, tenta encontrar automaticamente
            if (paintingLayer.value == 0)
            {
                int layerIndex = LayerMask.NameToLayer("Painting");
                if (layerIndex >= 0)
                    paintingLayer = 1 << layerIndex;
                else
                    Debug.LogWarning("[MuseumModerna] Layer 'Painting' não encontrada! Crie-a em Edit → Project Settings → Tags and Layers.", this);
            }
        }

        private void Update()
        {
            if (State == PlayerState.Paused) return;

            _detectionTimer += Time.deltaTime;
            if (_detectionTimer >= detectionInterval)
            {
                _detectionTimer = 0f;
                CheckNearbyPaintings();
            }
        }

        // ─── Detecção de Quadros ──────────────────────────────────────────────

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
                GameObject nearestPainting = FindNearestPainting(count);

                if (nearestPainting != _lastNearPaintingObject)
                {
                    _lastNearPaintingObject = nearestPainting;

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

        private GameObject FindNearestPainting(int count)
        {
            GameObject nearest = _overlapResults[0].gameObject;
            float nearestDist = Vector3.Distance(transform.position, nearest.transform.position);

            for (int i = 1; i < count; i++)
            {
                if (_overlapResults[i] == null) continue;
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

        private void SetState(PlayerState newState)
        {
            if (State == newState) return;

            State = newState;

            switch (newState)
            {
                case PlayerState.Walking:
                    headGazeMovement?.SetMovementEnabled(true);
                    break;
                case PlayerState.Viewing:
                    headGazeMovement?.SetMovementEnabled(false);
                    break;
                case PlayerState.Paused:
                    headGazeMovement?.SetMovementEnabled(false);
                    break;
            }

            Debug.Log($"[MuseumModerna] Estado do player: {newState}");
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

        /// <summary>Pausa ou despausa o player.</summary>
        public void SetPaused(bool paused)
        {
            SetState(paused ? PlayerState.Paused : PlayerState.Walking);
        }

        // ─── Debug Visual ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, paintingDetectionRadius);
            Gizmos.color = new Color(1f, 0.8f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, paintingDetectionRadius);
        }
#endif
    }
}
