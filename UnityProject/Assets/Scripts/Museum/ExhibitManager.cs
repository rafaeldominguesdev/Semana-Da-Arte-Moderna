using System.Collections.Generic;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Gerencia todas as exposições do museu.
    /// Responsável por: mapeamento quadro→dados, highlighting e rastreamento de progresso.
    ///
    /// Como usar:
    ///   1. Adicione este script a um GameObject vazio chamado "ExhibitManager".
    ///   2. Cada quadro na cena deve ter o componente PaintingExhibit.
    ///   3. Conecte os eventos do PlayerController aqui via Inspector ou código.
    /// </summary>
    public class ExhibitManager : MonoBehaviour
    {
        // ─── Configurações no Inspector ───────────────────────────────────────

        [Header("Referências")]
        [Tooltip("Referência ao PlayerController do player")]
        [SerializeField] private PlayerController playerController;

        [Tooltip("Referência ao UIManager para mostrar painéis")]
        [SerializeField] private UIManager uiManager;

        [Header("Highlighting")]
        [Tooltip("Intensidade do brilho (emission) no quadro focado")]
        [SerializeField] private float highlightIntensity = 0.4f;

        [Tooltip("Cor do brilho de destaque")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.7f);

        [Tooltip("Velocidade de transição do highlight (fade in/out)")]
        [SerializeField] private float highlightSpeed = 3f;

        // ─── Variáveis Privadas ────────────────────────────────────────────────

        // Lista de todos os quadros visitados (por uniqueId)
        private readonly HashSet<string> _visitedPaintings = new HashSet<string>();

        // Quadro atualmente destacado (null se nenhum)
        private Renderer _highlightedRenderer = null;
        private MaterialPropertyBlock _propBlock;

        // Intensidade atual do highlight (para animação suave)
        private float _currentHighlight = 0f;
        private float _targetHighlight = 0f;

        // Todos os exhibits na cena (cacheado no Start)
        private PaintingExhibit[] _allExhibits;

        // ─── Ciclo de Vida Unity ──────────────────────────────────────────────

        private void Awake()
        {
            // Inicializa o MaterialPropertyBlock (não cria cópia do material)
            _propBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            // Cacheia todos os exhibits na cena — evita FindObjectOfType no Update
            _allExhibits = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            Debug.Log($"[MuseumModerna] ExhibitManager: {_allExhibits.Length} quadros encontrados na cena.");

            // Conecta eventos do PlayerController
            if (playerController != null)
            {
                playerController.OnNearPainting.AddListener(OnPlayerNearPainting);
                playerController.OnLeavePainting.AddListener(OnPlayerLeavePainting);
            }
            else
            {
                Debug.LogWarning("[MuseumModerna] ExhibitManager: PlayerController não definido!");
            }
        }

        private void Update()
        {
            // Anima o highlight suavemente
            AnimateHighlight();
        }

        private void OnDestroy()
        {
            // Remove listeners para evitar vazamento de memória
            if (playerController != null)
            {
                playerController.OnNearPainting.RemoveListener(OnPlayerNearPainting);
                playerController.OnLeavePainting.RemoveListener(OnPlayerLeavePainting);
            }
        }

        // ─── Handlers de Eventos ──────────────────────────────────────────────

        /// <summary>
        /// Chamado quando o player se aproxima de um quadro.
        /// </summary>
        private void OnPlayerNearPainting(PaintingInfo paintingInfo)
        {
            // Mostra o painel de informações na UI
            uiManager?.ShowPaintingPanel(paintingInfo);

            // Encontra o renderer do quadro e aplica highlight
            PaintingExhibit exhibit = FindExhibitByInfo(paintingInfo);
            if (exhibit != null)
            {
                Renderer rend = exhibit.GetComponent<Renderer>();
                if (rend != null)
                {
                    ApplyHighlight(rend);
                }
            }

            // Registra como visitado
            if (!string.IsNullOrEmpty(paintingInfo.uniqueId))
            {
                bool isFirstVisit = _visitedPaintings.Add(paintingInfo.uniqueId);
                if (isFirstVisit)
                {
                    Debug.Log($"[MuseumModerna] Primeira visita à obra: {paintingInfo.title}");
                    // TODO: pode disparar um achievement/animação especial aqui
                }
            }
        }

        /// <summary>
        /// Chamado quando o player se afasta de um quadro.
        /// </summary>
        private void OnPlayerLeavePainting()
        {
            // Esconde o painel de informações
            uiManager?.HidePaintingPanel();

            // Remove highlight
            RemoveHighlight();
        }

        // ─── Highlighting ─────────────────────────────────────────────────────

        /// <summary>
        /// Define o renderer que receberá o highlight.
        /// Usa MaterialPropertyBlock para não criar instâncias do material.
        /// </summary>
        private void ApplyHighlight(Renderer targetRenderer)
        {
            if (targetRenderer == null) return;
            _highlightedRenderer = targetRenderer;
            _targetHighlight = highlightIntensity;
        }

        /// <summary>
        /// Remove o highlight do renderer atual.
        /// </summary>
        private void RemoveHighlight()
        {
            _targetHighlight = 0f;
        }

        /// <summary>
        /// Anima suavemente a intensidade do highlight usando Lerp no Update.
        /// </summary>
        private void AnimateHighlight()
        {
            _currentHighlight = Mathf.Lerp(_currentHighlight, _targetHighlight, highlightSpeed * Time.deltaTime);

            if (_highlightedRenderer != null)
            {
                _highlightedRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_EmissionColor", highlightColor * _currentHighlight);
                _highlightedRenderer.SetPropertyBlock(_propBlock);

                // Quando o fade out terminar, limpa a referência
                if (_currentHighlight < 0.001f && _targetHighlight <= 0f)
                {
                    // Garante que emission vai a zero absoluto
                    _propBlock.SetColor("_EmissionColor", Color.black);
                    _highlightedRenderer.SetPropertyBlock(_propBlock);
                    _highlightedRenderer = null;
                }
            }
        }

        // ─── Utilitários ──────────────────────────────────────────────────────

        /// <summary>
        /// Encontra o PaintingExhibit correspondente a um PaintingInfo.
        /// </summary>
        private PaintingExhibit FindExhibitByInfo(PaintingInfo info)
        {
            foreach (PaintingExhibit exhibit in _allExhibits)
            {
                if (exhibit.PaintingData == info)
                    return exhibit;
            }
            return null;
        }

        // ─── API Pública ──────────────────────────────────────────────────────

        /// <summary>
        /// Retorna quantos quadros únicos foram visitados nesta sessão.
        /// </summary>
        public int VisitedCount => _visitedPaintings.Count;

        /// <summary>
        /// Retorna o total de quadros na cena.
        /// </summary>
        public int TotalPaintings => _allExhibits?.Length ?? 0;

        /// <summary>
        /// Retorna o progresso de visitas como valor entre 0 e 1.
        /// </summary>
        public float VisitProgress => TotalPaintings > 0 ? (float)VisitedCount / TotalPaintings : 0f;

        /// <summary>
        /// Verifica se uma obra específica já foi visitada.
        /// </summary>
        public bool HasVisited(string uniqueId) => _visitedPaintings.Contains(uniqueId);
    }
}
