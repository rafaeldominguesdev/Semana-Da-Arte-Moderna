using System.Collections;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Gerencia o áudio ambiente do museu: música de fundo, reverberação da sala
    /// e passos do visitante sincronizados com o movimento.
    ///
    /// Como usar:
    ///   1. Adicione ao GameManager ou a um GameObject vazio "AudioManager".
    ///   2. Arraste os AudioClips nos campos do Inspector.
    ///   3. O reverb da sala é automático via AudioReverbZone.
    ///
    /// Assets gratuitos recomendados:
    ///   Música: freemusicarchive.org (CC0), musopen.org (músicas clássicas domínio público)
    ///   Passos em madeira: freesound.org (buscar "footstep wood museum")
    /// </summary>
    public class AmbientAudioManager : MonoBehaviour
    {
        public static AmbientAudioManager Instance { get; private set; }

        [Header("Música de Fundo")]
        [Tooltip("Música instrumental suave para tocar em loop")]
        [SerializeField] private AudioClip backgroundMusic;

        [Tooltip("Volume da música (baixo para não competir com o guia de áudio)")]
        [SerializeField][Range(0f, 1f)] private float musicVolume = 0.12f;

        [Tooltip("Duração do fade in da música ao iniciar (segundos)")]
        [SerializeField] private float musicFadeIn = 4f;

        [Header("Passos")]
        [Tooltip("Clips de som de passos (2-4 variações evitam repetição mecânica)")]
        [SerializeField] private AudioClip[] footstepClips;

        [Tooltip("Volume dos passos")]
        [SerializeField][Range(0f, 1f)] private float footstepVolume = 0.35f;

        [Tooltip("Intervalo entre passos em segundos (0.5 = caminhada normal)")]
        [SerializeField] private float footstepInterval = 0.52f;

        [Header("Reverb da Sala")]
        [Tooltip("Preset de reverberação — Hallway simula corredor de museu com eco")]
        [SerializeField] private AudioReverbPreset reverbPreset = AudioReverbPreset.Hallway;

        [Tooltip("Raio da zona de reverb (deve cobrir toda a sala)")]
        [SerializeField] private float reverbRadius = 25f;

        [Header("Referências")]
        [SerializeField] private HeadGazeMovement playerMovement;

        // ─── Privado ──────────────────────────────────────────────────────────

        private AudioSource _musicSource;
        private AudioSource _footstepSource;
        private AudioReverbZone _reverbZone;
        private float _footstepTimer;

        // ─── Ciclo de Vida ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            SetupMusicSource();
            SetupFootstepSource();
            SetupReverbZone();

            if (playerMovement == null)
                playerMovement = FindAnyObjectByType<HeadGazeMovement>();
        }

        private void Start()
        {
            if (backgroundMusic != null)
            {
                _musicSource.clip = backgroundMusic;
                _musicSource.Play();
                StartCoroutine(FadeMusic(0f, musicVolume, musicFadeIn));
            }
        }

        private void Update()
        {
            HandleFootsteps();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) _musicSource?.Pause();
            else _musicSource?.UnPause();
        }

        // ─── Setup ────────────────────────────────────────────────────────────

        private void SetupMusicSource()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;  // 2D — música não tem posição 3D
            _musicSource.volume = 0f;
            _musicSource.priority = 128;
        }

        private void SetupFootstepSource()
        {
            _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.loop = false;
            _footstepSource.playOnAwake = false;
            _footstepSource.spatialBlend = 0.2f; // levemente 3D para eco natural
            _footstepSource.volume = footstepVolume;
            _footstepSource.priority = 64;
        }

        private void SetupReverbZone()
        {
            _reverbZone = gameObject.AddComponent<AudioReverbZone>();
            _reverbZone.reverbPreset = reverbPreset;
            _reverbZone.minDistance = reverbRadius * 0.4f;
            _reverbZone.maxDistance = reverbRadius;
        }

        // ─── Passos ───────────────────────────────────────────────────────────

        private void HandleFootsteps()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;
            if (playerMovement == null || !playerMovement.IsMoving) return;

            _footstepTimer += Time.deltaTime;
            if (_footstepTimer < footstepInterval) return;

            _footstepTimer = 0f;
            int index = Random.Range(0, footstepClips.Length);
            // Varia levemente volume e pitch para soar natural
            float vol   = footstepVolume * Random.Range(0.85f, 1f);
            float pitch = Random.Range(0.92f, 1.08f);
            _footstepSource.pitch = pitch;
            _footstepSource.PlayOneShot(footstepClips[index], vol);
        }

        // ─── Utilitários ──────────────────────────────────────────────────────

        private IEnumerator FadeMusic(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (_musicSource != null)
                    _musicSource.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            if (_musicSource != null)
                _musicSource.volume = to;
        }

        /// <summary>Reduz o volume da música durante a narração de uma obra.</summary>
        public void DuckMusicForNarration(float duckVolume = 0.04f, float fadeTime = 1f)
        {
            StopAllCoroutines();
            StartCoroutine(FadeMusic(_musicSource.volume, duckVolume, fadeTime));
        }

        /// <summary>Restaura o volume da música após a narração.</summary>
        public void RestoreMusicVolume(float fadeTime = 1.5f)
        {
            StopAllCoroutines();
            StartCoroutine(FadeMusic(_musicSource.volume, musicVolume, fadeTime));
        }
    }
}
