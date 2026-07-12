using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Gera clipes de áudio SINTETIZADOS (placeholder) para o museu — passos e um
    /// loop ambiente — e atribui-os ao AmbientAudioManager na cena aberta.
    /// Acesse: MuseumModerna → Áudio Ambiente (Gerar Clipes)
    ///
    /// IMPORTANTE — isto é um placeholder:
    ///   Não há acesso à internet neste ambiente para baixar áudio real (freesound.org,
    ///   musopen.org etc.), então os clipes são gerados 100% por código (ruído filtrado
    ///   para os passos, senoides desafinadas para o "pad" ambiente). O resultado é
    ///   jogável e evita silêncio total, mas NÃO substitui gravações reais. Troque os
    ///   arquivos em Assets/Audio/ por áudio de verdade quando possível (ver sugestões
    ///   no rodapé desta janela).
    ///
    /// Como funciona:
    ///   1. Sintetiza amostras float (-1..1) em memória.
    ///   2. Grava um .wav PCM 16-bit mono (cabeçalho RIFF/WAVE escrito manualmente —
    ///      a Unity não tem API pronta para salvar AudioClip em disco).
    ///   3. Importa o asset (AssetDatabase.ImportAsset) e carrega o AudioClip resultante.
    ///   4. Atribui os clipes ao AmbientAudioManager da cena via SerializedObject
    ///      (os campos são [SerializeField] private) e marca a cena como suja.
    /// </summary>
    public class MuseumAudioSetup : EditorWindow
    {
        private const string AudioFolder = "Assets/Audio";
        private const int SampleRate = 44100;

        private float _ambientDuration = 30f;

        // ─── Menu ─────────────────────────────────────────────────────────────

        [MenuItem("MuseumModerna/Áudio Ambiente (Gerar Clipes)")]
        public static void ShowWindow()
        {
            GetWindow<MuseumAudioSetup>("Museum Audio Setup").minSize = new Vector2(400, 520);
        }

        // ─── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Áudio Ambiente do Museu", title);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Estes clipes são SINTETIZADOS por código (ruído filtrado + senoides), " +
                "não são gravações reais. Servem para o museu não ficar em silêncio " +
                "enquanto você não tem áudio de verdade. Substitua depois pelos arquivos " +
                "sugeridos no rodapé desta janela.",
                MessageType.Warning);

            EditorGUILayout.Space(12);

            // ── Passos ──
            EditorGUILayout.LabelField("Passos (Footsteps)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Gera 4 variações de ~80-150ms: estouro de ruído filtrado (passa-baixa) " +
                "com ataque rápido e decaimento exponencial, mais um leve 'thump' grave. " +
                "Cada variação tem corte de filtro e tom diferentes para não soar repetitivo.",
                MessageType.None);
            if (GUILayout.Button("Gerar 4 Clipes de Passos"))
                GenerateFootstepsAndAssign();

            EditorGUILayout.Space(10);

            // ── Ambiente ──
            EditorGUILayout.LabelField("Música de Fundo (Pad Ambiente)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Gera um loop suave de senoides desafinadas (cluster grave, ~110-220Hz) " +
                "com modulação lenta de volume (LFO) e fade in/out nas pontas para " +
                "encaixar em loop sem estalo. NÃO é música instrumental real — é um " +
                "'pad' de fundo discreto, volume baixo, apenas para preencher o silêncio.",
                MessageType.None);
            _ambientDuration = EditorGUILayout.Slider("Duração (segundos)", _ambientDuration, 20f, 40f);
            if (GUILayout.Button("Gerar Loop Ambiente"))
                GenerateAmbientAndAssign();

            EditorGUILayout.Space(16);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Gerar Tudo e Atribuir à Cena", GUILayout.Height(42)))
                GenerateAllAndAssign();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12);
            DrawRealAudioSuggestions();
        }

        // ─── Ações de alto nível ──────────────────────────────────────────────

        private void GenerateFootstepsAndAssign()
        {
            AudioClip[] clips = GenerateFootstepClips();
            AssignFootsteps(clips);
            Debug.Log($"[MuseumModerna] {clips.Length} clipe(s) de passo gerado(s) em {AudioFolder}/.");
        }

        private void GenerateAmbientAndAssign()
        {
            AudioClip clip = GenerateAmbientClip(_ambientDuration);
            AssignAmbient(clip);
            Debug.Log($"[MuseumModerna] Loop ambiente ({_ambientDuration:0}s) gerado em {AudioFolder}/.");
        }

        private void GenerateAllAndAssign()
        {
            AudioClip[] footsteps = GenerateFootstepClips();
            AudioClip ambient = GenerateAmbientClip(_ambientDuration);
            AssignFootsteps(footsteps);
            AssignAmbient(ambient);

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog(
                    "Áudio Ambiente Gerado",
                    "Clipes sintetizados criados em Assets/Audio/:\n" +
                    "  • Footstep_01..04.wav\n" +
                    "  • AmbientPad_Loop.wav\n\n" +
                    "Atribuídos ao AmbientAudioManager da cena aberta (se encontrado).\n\n" +
                    "Lembrete: são placeholders sintéticos. Troque por áudio real quando puder " +
                    "(veja sugestões no rodapé da janela) e salve a cena com Ctrl+S.",
                    "OK");
        }

        // ─── Geração — Passos ─────────────────────────────────────────────────

        private static AudioClip[] GenerateFootstepClips()
        {
            EnsureAudioFolder();

            // (seed, duração, corte do filtro passa-baixa [0-1, menor = mais abafado], freq. do "thump" grave em Hz)
            var variants = new (int seed, float duration, float lowpass, float thumpFreq)[]
            {
                (1001, 0.09f, 0.50f, 85f),
                (1002, 0.11f, 0.35f, 70f),
                (1003, 0.13f, 0.25f, 95f),
                (1004, 0.15f, 0.40f, 78f),
            };

            AudioClip[] clips = new AudioClip[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                float[] samples = SynthesizeFootstep(variants[i].seed, variants[i].duration,
                    variants[i].lowpass, variants[i].thumpFreq);

                string fileName = $"Footstep_{i + 1:00}.wav";
                string relativePath = $"{AudioFolder}/{fileName}";
                string absolutePath = Path.Combine(Application.dataPath, "Audio", fileName);

                SaveWav(absolutePath, samples, SampleRate);
                AssetDatabase.ImportAsset(relativePath);
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
            }

            AssetDatabase.SaveAssets();
            return clips;
        }

        /// <summary>
        /// Sintetiza um "passo": ruído branco passando por um filtro passa-baixa
        /// de um polo (IIR simples: y[n] = y[n-1] + alpha*(x[n]-y[n-1])) somado a
        /// um "thump" grave curto, com envelope de ataque rápido e decaimento exponencial.
        /// </summary>
        private static float[] SynthesizeFootstep(int seed, float durationSeconds, float lowpassAlpha, float thumpFreq)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(durationSeconds * SampleRate));
            float[] data = new float[n];
            System.Random rng = new System.Random(seed);

            float attackSamples = Mathf.Max(1, n * 0.06f); // ataque rápido: ~6% da duração
            float prevLp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;

                // Ruído branco -1..1 passando por filtro passa-baixa (suavização IIR de um polo)
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                prevLp += lowpassAlpha * (noise - prevLp);

                // "Thump" grave curto que dá corpo ao som (decai bem mais rápido que o ruído)
                float thump = Mathf.Sin(2f * Mathf.PI * thumpFreq * (i / (float)SampleRate)) * Mathf.Exp(-t * 22f) * 0.55f;

                // Envelope: ataque rápido linear + decaimento exponencial
                float envelope = i < attackSamples
                    ? i / attackSamples
                    : Mathf.Exp(-((i - attackSamples) / (n - attackSamples)) * 6.5f);

                data[i] = (prevLp * 0.8f + thump) * envelope;
            }

            NormalizeInPlace(data, 0.9f);
            return data;
        }

        // ─── Geração — Ambiente ───────────────────────────────────────────────

        private static AudioClip GenerateAmbientClip(float durationSeconds)
        {
            EnsureAudioFolder();

            float[] samples = SynthesizeAmbientPad(durationSeconds);

            const string fileName = "AmbientPad_Loop.wav";
            string relativePath = $"{AudioFolder}/{fileName}";
            string absolutePath = Path.Combine(Application.dataPath, "Audio", fileName);

            SaveWav(absolutePath, samples, SampleRate);
            AssetDatabase.ImportAsset(relativePath);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
        }

        /// <summary>
        /// Sintetiza um "pad" ambiente placeholder: senoides levemente desafinadas
        /// empilhadas num cluster grave (~110-220Hz), cada uma com uma LFO lenta de
        /// amplitude para dar movimento sutil, e um envelope geral com fade in/out
        /// nas pontas para permitir loop sem estalo (amplitude ~0 no início e no fim).
        /// NÃO é música instrumental real — é apenas um preenchimento discreto.
        /// </summary>
        private static float[] SynthesizeAmbientPad(float durationSeconds)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(durationSeconds * SampleRate));
            float[] data = new float[n];

            // Cluster grave suave (aprox. A2-C3-E3-A3), cada oscilador levemente desafinado
            float[] freqs   = { 110.00f, 130.81f, 164.81f, 220.00f };
            float[] detunes = { 0.00f, 0.35f, -0.28f, 0.22f };
            float[] lfoHz   = { 0.07f, 0.11f, 0.05f, 0.09f };  // modulação de amplitude bem lenta
            float[] amps    = { 0.30f, 0.22f, 0.18f, 0.14f };

            double[] phase = new double[freqs.Length];
            float fadeTime = Mathf.Min(3f, durationSeconds * 0.15f);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float sample = 0f;

                for (int o = 0; o < freqs.Length; o++)
                {
                    float freq = freqs[o] + detunes[o];
                    phase[o] += freq / SampleRate;
                    float lfo = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * lfoHz[o] * t); // 0.7-1.0
                    sample += Mathf.Sin((float)(2.0 * Math.PI * phase[o])) * amps[o] * lfo;
                }

                // Fade in/out nas pontas — garante loop sem estalo (amplitude ~0 nas bordas)
                float envelope = 1f;
                if (t < fadeTime) envelope = t / fadeTime;
                else if (t > durationSeconds - fadeTime) envelope = (durationSeconds - t) / fadeTime;

                data[i] = sample * Mathf.Clamp01(envelope);
            }

            NormalizeInPlace(data, 0.5f); // volume moderado — a manager ainda aplica musicVolume baixo por cima
            return data;
        }

        // ─── Atribuição ao AmbientAudioManager da cena ───────────────────────

        private static bool TryGetManager(out AmbientAudioManager manager, out SerializedObject serializedManager)
        {
            manager = FindAnyObjectByType<AmbientAudioManager>();
            serializedManager = manager != null ? new SerializedObject(manager) : null;
            return manager != null;
        }

        private static void AssignFootsteps(AudioClip[] clips)
        {
            if (!TryGetManager(out AmbientAudioManager manager, out SerializedObject so))
            {
                Debug.LogWarning("[MuseumModerna] Nenhum AmbientAudioManager encontrado na cena aberta. " +
                    "Os clipes foram gerados em Assets/Audio/, mas não atribuídos. Adicione o componente " +
                    "à cena e rode esta ferramenta novamente.");
                return;
            }

            SerializedProperty prop = so.FindProperty("footstepClips");
            prop.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            so.ApplyModifiedProperties();

            MarkManagerDirty(manager);
        }

        private static void AssignAmbient(AudioClip clip)
        {
            if (!TryGetManager(out AmbientAudioManager manager, out SerializedObject so))
            {
                Debug.LogWarning("[MuseumModerna] Nenhum AmbientAudioManager encontrado na cena aberta. " +
                    "O clipe foi gerado em Assets/Audio/, mas não atribuído. Adicione o componente " +
                    "à cena e rode esta ferramenta novamente.");
                return;
            }

            SerializedProperty prop = so.FindProperty("backgroundMusic");
            prop.objectReferenceValue = clip;
            so.ApplyModifiedProperties();

            MarkManagerDirty(manager);
        }

        private static void MarkManagerDirty(AmbientAudioManager manager)
        {
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        // ─── Utilitários ──────────────────────────────────────────────────────

        private static void EnsureAudioFolder()
        {
            if (!AssetDatabase.IsValidFolder(AudioFolder))
                AssetDatabase.CreateFolder("Assets", "Audio");
        }

        private static void NormalizeInPlace(float[] data, float targetPeak)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float abs = Mathf.Abs(data[i]);
                if (abs > max) max = abs;
            }
            if (max < 1e-6f) return;

            float scale = targetPeak / max;
            for (int i = 0; i < data.Length; i++)
                data[i] *= scale;
        }

        /// <summary>
        /// Escreve um .wav PCM 16-bit mono manualmente — a Unity não tem API pronta
        /// para salvar um AudioClip em disco. Layout do cabeçalho (44 bytes, little-endian):
        ///   "RIFF" + tamanho do chunk (4) + "WAVE"
        ///   "fmt " + tamanho do subchunk (16) + formato PCM(1) + canais(1) + sampleRate
        ///          + byteRate + blockAlign + bitsPerSample(16)
        ///   "data" + tamanho dos dados + amostras (int16 little-endian)
        /// </summary>
        private static void SaveWav(string absolutePath, float[] samples, int sampleRate)
        {
            string dir = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            const int channels = 1;
            const int bitsPerSample = 16;
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);
            int dataSize = samples.Length * 2; // 2 bytes por amostra (16-bit)
            int riffChunkSize = 36 + dataSize;  // 36 = tamanho fixo do resto do cabeçalho

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // ── RIFF/WAVE ──
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(riffChunkSize);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                // ── fmt subchunk ──
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);                    // tamanho do subchunk fmt (PCM = 16)
                writer.Write((short)1);               // audio format: 1 = PCM
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write((short)bitsPerSample);

                // ── data subchunk ──
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);
                for (int i = 0; i < samples.Length; i++)
                {
                    short s = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * short.MaxValue),
                        short.MinValue, short.MaxValue);
                    writer.Write(s);
                }
            }
        }

        // ─── Sugestões de Áudio Real ──────────────────────────────────────────

        private void DrawRealAudioSuggestions()
        {
            EditorGUILayout.LabelField("Para Substituir por Áudio Real", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Estes clipes sintetizados são só para não deixar o museu em silêncio. " +
                "Quando possível, troque por gravações reais:\n\n" +
                "PASSOS (madeira/museu):\n" +
                "• freesound.org → buscar 'footstep wood museum'\n\n" +
                "MÚSICA INSTRUMENTAL / AMBIENTE (baixo volume):\n" +
                "• musopen.org → clássica em domínio público (ex.: Villa-Lobos, 1922!)\n" +
                "• freemusicarchive.org → filtrar Instrumental/Ambient, licença CC\n\n" +
                "Basta arrastar o novo AudioClip para os campos 'Background Music' / " +
                "'Footstep Clips' do AmbientAudioManager no Inspector.",
                MessageType.None);
        }
    }
}
