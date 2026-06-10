using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MuseumModerna
{
    /// <summary>
    /// Gerencia toda a interface do usuário do museu.
    ///
    /// Elementos da UI gerenciados:
    ///   - Painel de informações do quadro (título, artista, ano, descrição)
    ///   - Crosshair/reticle no centro da tela
    ///   - Indicador "olhe para baixo para caminhar" (seta animada)
    ///   - Botões: "Fechar painel" e "Calibrar Giroscópio"
    ///
    /// Como configurar no Unity Editor:
    ///   1. Crie um Canvas em Screen Space Overlay.
    ///   2. Atribua os elementos nos campos do Inspector.
    ///   3. O Canvas deve ter um CanvasScaler (Scale With Screen Size, 1080x1920).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ─── Painel de Informações ────────────────────────────────────────────

        [Header("Painel de Informações do Quadro")]
        [Tooltip("GameObject pai do painel de informações (ative/desative este)")]
        [SerializeField] private GameObject paintingPanel;

        [Tooltip("CanvasGroup do painel — usado para fade in/out suave")]
        [SerializeField] private CanvasGroup paintingPanelCanvasGroup;

        [Tooltip("Texto do título da obra")]
        [SerializeField] private Text titleText;

        [Tooltip("Texto do artista")]
        [SerializeField] private Text artistText;

        [Tooltip("Texto do ano")]
        [SerializeField] private Text yearText;

        [Tooltip("Texto da descrição (suporta scroll)")]
        [SerializeField] private Text descriptionText;

        [Tooltip("Imagem de miniatura da obra (opcional)")]
        [SerializeField] private Image thumbnailImage;

        // ─── Crosshair ────────────────────────────────────────────────────────

        [Header("Crosshair / Reticle")]
        [Tooltip("GameObject do crosshair central")]
        [SerializeField] private GameObject crosshair;

        [Tooltip("Image do crosshair — pode mudar de cor quando perto de um quadro")]
        [SerializeField] private Image crosshairImage;

        [Tooltip("Cor normal do crosshair")]
        [SerializeField] private Color crosshairNormalColor = new Color(1f, 1f, 1f, 0.8f);

        [Tooltip("Cor do crosshair quando perto de um quadro interativo")]
        [SerializeField] private Color crosshairActiveColor = new Color(1f, 0.9f, 0.2f, 1f);

        // ─── Indicador de Movimento ───────────────────────────────────────────

        [Header("Indicador de Head-Gaze")]
        [Tooltip("GameObject da seta indicadora 'olhe para baixo para caminhar'")]
        [SerializeField] private GameObject gazeIndicator;

        [Tooltip("RectTransform da seta para animação de bounce")]
        [SerializeField] private RectTransform gazeArrow;

        [Tooltip("Tempo em segundos que o indicador fica visível ao iniciar")]
        [SerializeField] private float indicatorShowDuration = 4f;

        [Tooltip("Amplitude do movimento de bounce da seta (pixels)")]
        [SerializeField] private float arrowBounceAmplitude = 15f;

        [Tooltip("Velocidade do bounce da seta")]
        [SerializeField] private float arrowBounceSpeed = 2f;

        // ─── Botões ───────────────────────────────────────────────────────────

        [Header("Botões")]
        [Tooltip("Botão para fechar o painel de informações")]
        [SerializeField] private Button closeButton;

        [Tooltip("Botão para calibrar o giroscópio")]
        [SerializeField] private Button calibrateButton;

        [Tooltip("Referência ao PlayerController para chamar Calibrate()")]
        [SerializeField] private PlayerController playerController;

        // ─── Animação ─────────────────────────────────────────────────────────

        [Header("Animação")]
        [Tooltip("Duração do fade in/out do painel em segundos")]
        [SerializeField] private float fadeDuration = 0.4f;

        // ─── Variáveis Privadas ────────────────────────────────────────────────

        // Corrotina atual de fade (para cancelar se necessário)
        private Coroutine _currentFadeCoroutine;

        // Posição original da seta para o bounce
        private Vector2 _arrowOriginalPos;

        // Flag: painel está visível?
        private bool _panelVisible = false;

        // ─── Ciclo de Vida Unity ──────────────────────────────────────────────

        private void Start()
        {
            // Inicializa o painel invisível
            if (paintingPanelCanvasGroup != null)
            {
                paintingPanelCanvasGroup.alpha = 0f;
                paintingPanelCanvasGroup.interactable = false;
                paintingPanelCanvasGroup.blocksRaycasts = false;
            }

            if (paintingPanel != null)
                paintingPanel.SetActive(false);

            // Salva posição original da seta
            if (gazeArrow != null)
                _arrowOriginalPos = gazeArrow.anchoredPosition;

            // Conecta botões
            closeButton?.onClick.AddListener(HidePaintingPanel);
            calibrateButton?.onClick.AddListener(OnCalibratePressed);

            // Mostra o indicador de gaze por alguns segundos no início
            if (gazeIndicator != null)
            {
                gazeIndicator.SetActive(true);
                if (gameObject.activeInHierarchy)
                    StartCoroutine(HideGazeIndicatorAfterDelay(indicatorShowDuration));
            }
        }

        private void Update()
        {
            // Anima a seta do indicador de gaze
            AnimateGazeArrow();
        }

        // ─── Painel de Informações ────────────────────────────────────────────

        /// <summary>
        /// Exibe o painel de informações com os dados de um quadro.
        /// Anima com fade in suave.
        /// </summary>
        /// <param name="paintingInfo">Dados do quadro a ser exibido.</param>
        public void ShowPaintingPanel(PaintingInfo paintingInfo)
        {
            if (paintingInfo == null) return;

            // Preenche os textos com os dados do quadro
            if (titleText != null)     titleText.text     = paintingInfo.title;
            if (artistText != null)    artistText.text    = paintingInfo.artist;
            if (yearText != null)      yearText.text      = paintingInfo.year.ToString();
            if (descriptionText != null) descriptionText.text = paintingInfo.description;

            // Define a miniatura se disponível
            if (thumbnailImage != null && paintingInfo.thumbnailSprite != null)
            {
                thumbnailImage.sprite = paintingInfo.thumbnailSprite;
                thumbnailImage.gameObject.SetActive(true);
            }
            else if (thumbnailImage != null)
            {
                thumbnailImage.gameObject.SetActive(false);
            }

            // Muda crosshair para ativo
            SetCrosshairActive(true);

            // Esconde indicador de gaze durante visualização
            if (gazeIndicator != null) gazeIndicator.SetActive(false);

            // Inicia fade in do painel
            FadePanel(true);
            _panelVisible = true;
        }

        /// <summary>
        /// Esconde o painel de informações com fade out suave.
        /// </summary>
        public void HidePaintingPanel()
        {
            if (!_panelVisible) return;

            SetCrosshairActive(false);
            FadePanel(false);
            _panelVisible = false;
        }

        // ─── Crosshair ────────────────────────────────────────────────────────

        /// <summary>
        /// Atualiza a cor do crosshair baseado no estado de interação.
        /// </summary>
        public void SetCrosshairActive(bool active)
        {
            if (crosshairImage != null)
            {
                crosshairImage.color = active ? crosshairActiveColor : crosshairNormalColor;
            }
        }

        // ─── Fade In/Out ──────────────────────────────────────────────────────

        /// <summary>
        /// Inicia uma animação de fade in ou fade out no painel.
        /// Cancela qualquer fade em andamento antes de iniciar o novo.
        /// </summary>
        private void FadePanel(bool fadeIn)
        {
            if (_currentFadeCoroutine != null)
                StopCoroutine(_currentFadeCoroutine);

            if (gameObject.activeInHierarchy)
            {
                _currentFadeCoroutine = StartCoroutine(FadePanelCoroutine(fadeIn));
            }
            else
            {
                // Fallback caso objeto esteja inativo
                if (paintingPanel != null) paintingPanel.SetActive(fadeIn);
                if (paintingPanelCanvasGroup != null)
                    paintingPanelCanvasGroup.alpha = fadeIn ? 1f : 0f;
            }
        }

        /// <summary>
        /// Corrotina que anima o alpha do CanvasGroup de 0 a 1 (ou 1 a 0).
        /// </summary>
        private IEnumerator FadePanelCoroutine(bool fadeIn)
        {
            if (paintingPanel != null) paintingPanel.SetActive(true);

            float startAlpha = paintingPanelCanvasGroup != null ? paintingPanelCanvasGroup.alpha : 0f;
            float endAlpha = fadeIn ? 1f : 0f;
            float elapsed = 0f;

            if (paintingPanelCanvasGroup != null)
            {
                paintingPanelCanvasGroup.interactable = fadeIn;
                paintingPanelCanvasGroup.blocksRaycasts = fadeIn;
            }

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                // Usa curva suave (smooth step)
                float smoothT = t * t * (3f - 2f * t);

                if (paintingPanelCanvasGroup != null)
                    paintingPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, smoothT);

                yield return null;
            }

            if (paintingPanelCanvasGroup != null)
                paintingPanelCanvasGroup.alpha = endAlpha;

            // Desativa o GameObject após o fade out
            if (!fadeIn && paintingPanel != null)
                paintingPanel.SetActive(false);
        }

        // ─── Indicador de Gaze ────────────────────────────────────────────────

        /// <summary>
        /// Anima a seta com efeito de bounce para baixo (chama atenção do usuário).
        /// </summary>
        private void AnimateGazeArrow()
        {
            if (gazeArrow == null || gazeIndicator == null || !gazeIndicator.activeSelf) return;

            float bounce = Mathf.Sin(Time.time * arrowBounceSpeed) * arrowBounceAmplitude;
            gazeArrow.anchoredPosition = _arrowOriginalPos + new Vector2(0f, -Mathf.Abs(bounce));
        }

        /// <summary>
        /// Esconde o indicador de gaze após um delay.
        /// </summary>
        private IEnumerator HideGazeIndicatorAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (gazeIndicator != null)
                gazeIndicator.SetActive(false);
        }

        // ─── Botões ───────────────────────────────────────────────────────────

        /// <summary>
        /// Chamado ao pressionar o botão de calibração.
        /// </summary>
        private void OnCalibratePressed()
        {
            playerController?.Calibrate();

            // Feedback visual: mostra brevemente o indicador de gaze novamente
            if (gazeIndicator != null)
            {
                gazeIndicator.SetActive(true);
                if (gameObject.activeInHierarchy)
                    StartCoroutine(HideGazeIndicatorAfterDelay(2f));
            }

            Debug.Log("[MuseumModerna] UIManager: Calibração solicitada pelo usuário.");
        }

        // ─── Cleanup ──────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveAllListeners();
            calibrateButton?.onClick.RemoveAllListeners();
        }
    }
}
