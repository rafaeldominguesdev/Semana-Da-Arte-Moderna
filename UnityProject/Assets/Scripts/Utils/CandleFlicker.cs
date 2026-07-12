using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Simula o tremulo de uma vela ou tocha em uma Light componente.
    /// Usa Perlin Noise para variação orgânica de intensidade e cor.
    ///
    /// Como usar:
    ///   Adicione a qualquer GameObject que tenha um componente Light (Point ou Spot).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class CandleFlicker : MonoBehaviour
    {
        [Header("Intensidade")]
        [Tooltip("Intensidade mínima (fração da intensidade base)")]
        [SerializeField] private float minIntensityFactor = 0.6f;

        [Tooltip("Intensidade máxima (fração da intensidade base)")]
        [SerializeField] private float maxIntensityFactor = 1.3f;

        [Tooltip("Velocidade do ruído de intensidade")]
        [SerializeField] private float flickerSpeed = 6f;

        [Header("Cor")]
        [Tooltip("Cor mais fria da vela (chama baixa)")]
        [SerializeField] private Color coldColor = new Color(1f, 0.55f, 0.15f);

        [Tooltip("Cor mais quente da vela (chama alta)")]
        [SerializeField] private Color hotColor = new Color(1f, 0.82f, 0.5f);

        [Tooltip("Velocidade da variação de cor")]
        [SerializeField] private float colorSpeed = 3f;

        [Header("Posição (micro-tremor)")]
        [Tooltip("Habilita micro-oscilação na posição (simula chama balançando)")]
        [SerializeField] private bool enablePositionJitter = true;

        [Tooltip("Amplitude do tremor de posição em metros")]
        [SerializeField] private float jitterAmplitude = 0.015f;

        // ─── Privado ──────────────────────────────────────────────────────────

        private Light _light;
        private float _baseIntensity;
        private Vector3 _basePosition;

        // Offsets únicos por instância para que velas diferentes não sincronizem
        private float _offsetI;
        private float _offsetC;
        private float _offsetX;
        private float _offsetZ;

        // ─── Ciclo de Vida ────────────────────────────────────────────────────

        private void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            _basePosition  = transform.localPosition;

            _offsetI = Random.Range(0f, 100f);
            _offsetC = Random.Range(0f, 100f);
            _offsetX = Random.Range(0f, 100f);
            _offsetZ = Random.Range(0f, 100f);
        }

        private void Update()
        {
            float t = Time.time;

            // Intensidade
            float noiseI = Mathf.PerlinNoise(t * flickerSpeed + _offsetI, 0f);
            _light.intensity = _baseIntensity * Mathf.Lerp(minIntensityFactor, maxIntensityFactor, noiseI);

            // Cor
            float noiseC = Mathf.PerlinNoise(t * colorSpeed + _offsetC, 1f);
            _light.color = Color.Lerp(coldColor, hotColor, noiseC);

            // Tremor de posição
            if (enablePositionJitter)
            {
                float jx = (Mathf.PerlinNoise(t * flickerSpeed * 0.7f + _offsetX, 2f) - 0.5f) * 2f;
                float jz = (Mathf.PerlinNoise(t * flickerSpeed * 0.7f + _offsetZ, 3f) - 0.5f) * 2f;
                transform.localPosition = _basePosition + new Vector3(jx, 0f, jz) * jitterAmplitude;
            }
        }

        private void OnDisable()
        {
            // Restaura valores originais ao desabilitar
            if (_light != null)
            {
                _light.intensity = _baseIntensity;
                _light.color = hotColor;
            }
            transform.localPosition = _basePosition;
        }
    }
}
