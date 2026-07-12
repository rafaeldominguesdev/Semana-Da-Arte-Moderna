using System;
using UnityEngine;

namespace MuseumModerna
{
    [Serializable]
    public class SaveData
    {
        public float posX;
        public float posY;
        public float posZ;
        public string savedAt;
    }

    /// <summary>
    /// Sistema de autosave da posição do player via PlayerPrefs (JSON).
    ///
    /// Como usar:
    ///   1. Adicione este script ao GameManager ou a um GameObject persistente.
    ///   2. O playerTransform é encontrado automaticamente via PlayerController.
    ///   3. O save ocorre a cada autoSaveInterval segundos, ao pausar e ao sair.
    ///   4. A posição é restaurada no Start(); a rotação não é restaurada
    ///      pois o giroscópio assume o controle ao iniciar.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Header("Configurações")]
        [Tooltip("Intervalo em segundos entre cada salvamento automático")]
        [SerializeField] private float autoSaveInterval = 30f;

        [Tooltip("Posição inicial padrão usada quando não há save (spawn)")]
        [SerializeField] private Vector3 defaultSpawnPosition = new Vector3(0f, 1f, 0f);

        [Header("Referência (preenchida automaticamente)")]
        [SerializeField] private Transform playerTransform;

        private const string SAVE_KEY = "MuseumModerna_SaveData";
        private float _timer;

        // ─── Ciclo de Vida ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                PlayerController pc = FindAnyObjectByType<PlayerController>();
                if (pc != null)
                    playerTransform = pc.transform;
                else
                    Debug.LogWarning("[MuseumModerna] SaveSystem: PlayerController não encontrado. " +
                        "Arraste o Player para o campo playerTransform no Inspector.", this);
            }

            LoadGame();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= autoSaveInterval)
            {
                _timer = 0f;
                SaveGame();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        // ─── Salvar ───────────────────────────────────────────────────────────

        public void SaveGame()
        {
            if (playerTransform == null) return;

            var data = new SaveData
            {
                posX = playerTransform.position.x,
                posY = playerTransform.position.y,
                posZ = playerTransform.position.z,
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log($"[MuseumModerna] Jogo salvo em {data.savedAt}");
        }

        // ─── Carregar ─────────────────────────────────────────────────────────

        public void LoadGame()
        {
            if (playerTransform == null) return;

            if (!PlayerPrefs.HasKey(SAVE_KEY))
            {
                TeleportPlayer(defaultSpawnPosition);
                Debug.Log("[MuseumModerna] SaveSystem: nenhum save encontrado. Usando posição padrão.");
                return;
            }

            string json = PlayerPrefs.GetString(SAVE_KEY);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null)
            {
                TeleportPlayer(defaultSpawnPosition);
                return;
            }

            TeleportPlayer(new Vector3(data.posX, data.posY, data.posZ));
            Debug.Log($"[MuseumModerna] Jogo carregado (save de {data.savedAt})");
        }

        // ─── API Pública ──────────────────────────────────────────────────────

        /// <summary>Deleta o save atual (útil para botão "Recomeçar").</summary>
        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[MuseumModerna] Save deletado.");
        }

        /// <summary>Retorna true se existe um save válido.</summary>
        public bool HasSave() => PlayerPrefs.HasKey(SAVE_KEY);

        // ─── Helpers Privados ─────────────────────────────────────────────────

        private void TeleportPlayer(Vector3 position)
        {
            // Desabilita o CC temporariamente para evitar colisão ao teleportar
            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = position;

            if (cc != null) cc.enabled = true;
        }
    }
}
