using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseumModerna
{
    /// <summary>
    /// Gerenciador central do jogo. Inicializa todos os sistemas,
    /// gerencia o estado global e persiste entre cenas.
    ///
    /// Como usar:
    ///   1. Crie um GameObject vazio chamado "GameManager" na cena.
    ///   2. Adicione este script a ele.
    ///   3. Marque como DontDestroyOnLoad se houver múltiplas cenas.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // Singleton
        public static GameManager Instance { get; private set; }

        [Header("Referências da Cena")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private ExhibitManager exhibitManager;
        [SerializeField] private UIManager uiManager;

        [Header("Configurações")]
        [Tooltip("Manter tela ligada durante a experiência VR")]
        [SerializeField] private bool keepScreenOn = true;

        [Tooltip("Frame rate alvo (60 recomendado para VR)")]
        [SerializeField] private int targetFrameRate = 60;

        // ─── Ciclo de Vida ────────────────────────────────────────────────────

        private void Awake()
        {
            // Implementa Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Configura performance
            Application.targetFrameRate = targetFrameRate;

            if (keepScreenOn)
                Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Garante 60fps em mobile
            QualitySettings.vSyncCount = 0;

            // Registra callback de calibração
            GyroCalibration.RegisterSceneResetCallback();

            Debug.Log("[MuseumModerna] GameManager inicializado.");
        }

        private void Start()
        {
            // Busca referências automaticamente se não definidas no Inspector
            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (exhibitManager == null)
                exhibitManager = FindAnyObjectByType<ExhibitManager>();

            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();

            if (playerController == null)
                Debug.LogError("[MuseumModerna] GameManager: PlayerController não encontrado na cena!", this);

            Debug.Log("[MuseumModerna] Todos os sistemas inicializados com sucesso.");
        }

        private void OnDestroy()
        {
            GyroCalibration.UnregisterSceneResetCallback();
        }

        // ─── API Pública ──────────────────────────────────────────────────────

        /// <summary>Calibra o giroscópio do player.</summary>
        public void Calibrate()
        {
            playerController?.Calibrate();
        }

        /// <summary>Pausa ou despausa o jogo.</summary>
        public void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            playerController?.SetPaused(paused);
        }

        /// <summary>Reinicia a cena atual.</summary>
        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Retorna o progresso de visitas (0 a 1).</summary>
        public float GetVisitProgress()
        {
            return exhibitManager != null ? exhibitManager.VisitProgress : 0f;
        }
    }
}
