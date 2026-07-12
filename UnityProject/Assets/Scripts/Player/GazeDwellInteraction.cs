using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Interação por olhar: o jogador olha para um quadro por dwellTime segundos
    /// para ativar o painel de informações e o guia de áudio.
    /// Funciona com o giroscópio — não precisa de botão ou toque.
    ///
    /// Como usar:
    ///   1. Adicione ao mesmo GameObject que tem a Camera (Player/Camera).
    ///   2. Configure paintingLayer para a layer "Painting".
    ///   3. Arraste o GazeDwellRing (Image radial) no UIManager.
    ///   4. Ative "Use Gaze Dwell" no PlayerController Inspector.
    /// </summary>
    public class GazeDwellInteraction : MonoBehaviour
    {
        [Header("Configuração")]
        [Tooltip("Segundos olhando para um quadro para ativá-lo")]
        [SerializeField] private float dwellTime = 2f;

        [Tooltip("Distância máxima do raycast em metros")]
        [SerializeField] private float maxRayDistance = 8f;

        [Tooltip("Segundos sem ver o quadro antes de fechar o painel automaticamente")]
        [SerializeField] private float autoCloseTimeout = 6f;

        [Tooltip("Layer dos quadros — deve ser a mesma do PlayerController")]
        [SerializeField] private LayerMask paintingLayer;

        [Header("Referências (auto-encontradas se vazias)")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private Camera vrCamera;

        // ─── Estado Interno ────────────────────────────────────────────────────

        private PaintingExhibit _currentTarget;
        private float _gazeTimer;
        private float _closeTimer;
        private bool _activated;

        // ─── Ciclo de Vida ────────────────────────────────────────────────────

        private void Awake()
        {
            if (vrCamera == null)
                vrCamera = GetComponent<Camera>() ?? Camera.main;

            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();

            // Herda a layer do PlayerController se não configurada
            if (paintingLayer.value == 0 && playerController != null)
                paintingLayer = playerController.PaintingLayer;

            if (vrCamera == null)
                Debug.LogError("[MuseumModerna] GazeDwellInteraction: Camera não encontrada!", this);
        }

        private void Update()
        {
            if (vrCamera == null || playerController == null) return;
            if (playerController.State == PlayerState.Paused) return;

            Ray ray = new Ray(vrCamera.transform.position, vrCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, paintingLayer))
            {
                PaintingExhibit exhibit = hit.collider.GetComponent<PaintingExhibit>()
                                       ?? hit.collider.GetComponentInParent<PaintingExhibit>();

                if (exhibit != null && exhibit.PaintingData != null)
                {
                    HandleGazeOnPainting(exhibit);
                    return;
                }
            }

            HandleGazeOff();
        }

        // ─── Lógica de Gaze ───────────────────────────────────────────────────

        private void HandleGazeOnPainting(PaintingExhibit exhibit)
        {
            _closeTimer = 0f;

            if (exhibit != _currentTarget)
            {
                // Mudou de quadro: fecha o anterior
                if (_activated)
                    playerController.TriggerLeavePainting();

                _currentTarget = exhibit;
                _gazeTimer = 0f;
                _activated = false;
            }

            if (_activated) return;

            _gazeTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_gazeTimer / dwellTime);
            uiManager?.SetGazeProgress(progress);

            if (_gazeTimer >= dwellTime)
            {
                _activated = true;
                uiManager?.SetGazeProgress(0f);
                playerController.TriggerNearPainting(exhibit.PaintingData);
            }
        }

        private void HandleGazeOff()
        {
            if (_currentTarget == null)
            {
                // Ainda pode estar reduzindo o anel suavemente
                if (_gazeTimer > 0f)
                {
                    _gazeTimer = Mathf.Max(0f, _gazeTimer - Time.deltaTime * 2f);
                    uiManager?.SetGazeProgress(Mathf.Clamp01(_gazeTimer / dwellTime));
                }
                return;
            }

            // Estava olhando, agora desviou
            if (!_activated)
            {
                // Não ativou ainda: decai o timer
                _gazeTimer = Mathf.Max(0f, _gazeTimer - Time.deltaTime * 2f);
                uiManager?.SetGazeProgress(Mathf.Clamp01(_gazeTimer / dwellTime));

                if (_gazeTimer <= 0f)
                    _currentTarget = null;
            }
            else
            {
                // Já ativou: conta timeout antes de fechar
                _closeTimer += Time.deltaTime;
                if (_closeTimer >= autoCloseTimeout)
                {
                    _closeTimer = 0f;
                    _activated = false;
                    _currentTarget = null;
                    playerController.TriggerLeavePainting();
                }
            }
        }

        // ─── API Pública ──────────────────────────────────────────────────────

        /// <summary>Cancela a interação atual (use ao pressionar botão Fechar no painel).</summary>
        public void CancelInteraction()
        {
            _currentTarget = null;
            _gazeTimer = 0f;
            _closeTimer = 0f;
            _activated = false;
            uiManager?.SetGazeProgress(0f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (vrCamera == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(vrCamera.transform.position, vrCamera.transform.forward * maxRayDistance);
        }
#endif
    }
}
