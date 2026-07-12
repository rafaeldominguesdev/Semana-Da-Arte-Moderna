using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseumModerna
{
    /// <summary>
    /// Gera texturas PBR procedurais (Albedo + Normal + Metallic/Smoothness) e as aplica
    /// como materiais reais nas superfícies do museu (Piso, Parede, Teto, Rodapé).
    ///
    /// Motivo: os materiais criados por MuseumCompleteBuilder/MuseumCeilingBuilder são cores
    /// planas (sem textura) — funcional, mas "chapado". Como não há acesso à internet neste
    /// ambiente para baixar texturas prontas (Poly Haven / ambientCG), as texturas abaixo são
    /// GERADAS por código (ruído Perlin + Sobel para normal map) e salvas como PNG reais em
    /// Assets/Textures/, com import settings mobile (ASTC) já configurados.
    ///
    /// Acesse: MuseumModerna → Texturas e Materiais PBR
    ///
    /// ORDEM DE USO RECOMENDADA (rodar por último, depois da geometria já existir na cena):
    ///   1. MuseumModerna → Construir Museu Completo   (cria Museum_Geometry: Piso/Parede/Teto/Rodape)
    ///   2. MuseumModerna → Decoração do Museu          (opcional)
    ///   3. MuseumModerna → Iluminação Avançada          (opcional)
    ///   4. MuseumModerna → Texturas e Materiais PBR     (este arquivo — passos 1→2→3 abaixo)
    ///
    /// Passos deste tool:
    ///   1. Gerar Texturas PBR   → cria os PNGs em Assets/Textures/
    ///   2. Criar/Atualizar Materiais PBR → cria os .mat em Assets/Materials/ referenciando os PNGs
    ///   3. Aplicar na Cena      → troca o sharedMaterial dos objetos existentes (Piso/Parede/Teto/Rodape)
    /// </summary>
    public class MuseumTextureSetup : EditorWindow
    {
        // ─── Pastas e caminhos de asset ─────────────────────────────────────────
        private const string TextureFolder  = "Assets/Textures";
        private const string MaterialFolder = "Assets/Materials";

        private const string FloorAlbedoPath              = TextureFolder + "/Floor_Albedo.png";
        private const string FloorNormalPath              = TextureFolder + "/Floor_Normal.png";
        private const string FloorMetallicSmoothnessPath  = TextureFolder + "/Floor_MetallicSmoothness.png";

        private const string WallAlbedoPath               = TextureFolder + "/Wall_Albedo.png";
        private const string WallNormalPath               = TextureFolder + "/Wall_Normal.png";
        private const string WallMetallicSmoothnessPath   = TextureFolder + "/Wall_MetallicSmoothness.png";

        private const string CeilingAlbedoPath             = TextureFolder + "/Ceiling_Albedo.png";
        private const string CeilingNormalPath             = TextureFolder + "/Ceiling_Normal.png";
        private const string CeilingMetallicSmoothnessPath = TextureFolder + "/Ceiling_MetallicSmoothness.png";

        private const string BaseboardAlbedoPath             = TextureFolder + "/Baseboard_Albedo.png";
        private const string BaseboardNormalPath             = TextureFolder + "/Baseboard_Normal.png";
        private const string BaseboardMetallicSmoothnessPath = TextureFolder + "/Baseboard_MetallicSmoothness.png";

        private const string MatFloorPath     = MaterialFolder + "/Museo_PBR_Floor.mat";
        private const string MatWallPath      = MaterialFolder + "/Museo_PBR_Wall.mat";
        private const string MatCeilingPath   = MaterialFolder + "/Museo_PBR_Ceiling.mat";
        private const string MatBaseboardPath = MaterialFolder + "/Museo_PBR_Baseboard.mat";

        // ─── Parâmetros ajustáveis ──────────────────────────────────────────────
        private int   _floorResolution     = 1024;
        private int   _wallResolution      = 1024;
        private int   _ceilingResolution   = 1024;
        private int   _baseboardResolution = 1024;
        private int   _seed                = 1922; // ano da Semana de Arte Moderna
        private float  _floorTiling  = 6f;
        private float  _wallTiling   = 4f;
        private bool  _addTopMolding = true;

        private Vector2 _scroll;

        // ─── Menu ───────────────────────────────────────────────────────────────

        [MenuItem("MuseumModerna/Texturas e Materiais PBR")]
        public static void ShowWindow()
        {
            GetWindow<MuseumTextureSetup>("Texturas PBR").minSize = new Vector2(420, 600);
        }

        // ─── GUI ────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Texturas e Materiais PBR (procedural)", title);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Sem acesso à internet neste ambiente, as texturas PBR (Albedo/Normal/Metallic-Smoothness) " +
                "são geradas por código (ruído + Sobel) e salvas como PNG reais em Assets/Textures/.\n" +
                "Shader usado: Standard (Built-in RP), igual ao resto do projeto.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // ── Resoluções ──
            EditorGUILayout.LabelField("Resolução das Texturas", EditorStyles.boldLabel);
            _floorResolution = EditorGUILayout.IntPopup("Piso", _floorResolution,
                new[] { "1024 (mobile)", "2048 (alta)" }, new[] { 1024, 2048 });
            _wallResolution = EditorGUILayout.IntPopup("Parede", _wallResolution,
                new[] { "512", "1024 (mobile)", "2048 (alta)" }, new[] { 512, 1024, 2048 });
            _ceilingResolution = EditorGUILayout.IntPopup("Teto", _ceilingResolution,
                new[] { "512", "1024 (mobile)", "2048 (alta)" }, new[] { 512, 1024, 2048 });
            _baseboardResolution = EditorGUILayout.IntPopup("Rodapé / Moldura", _baseboardResolution,
                new[] { "512 (mobile)", "1024 (alta)" }, new[] { 512, 1024 });

            EditorGUILayout.Space(6);
            _seed = EditorGUILayout.IntField("Seed do ruído (varia o padrão)", _seed);
            _floorTiling = EditorGUILayout.Slider("Tiling do Piso (repetições)", _floorTiling, 1f, 12f);
            _wallTiling  = EditorGUILayout.Slider("Tiling da Parede/Teto (repetições)", _wallTiling, 1f, 12f);
            EditorGUILayout.HelpBox(
                "Tiling é aplicado de forma uniforme (mesmo valor para todas as salas, que têm " +
                "tamanhos diferentes). Ajuste fino por objeto pode ser feito manualmente no Inspector " +
                "(Renderer → Material → Tiling) se necessário.",
                MessageType.None);

            EditorGUILayout.Space(12);

            // ── Passo 1 ──
            EditorGUILayout.LabelField("1. Gerar Texturas PBR", EditorStyles.boldLabel);
            if (GUILayout.Button("Gerar Texturas (Piso, Parede, Teto, Rodapé)", GUILayout.Height(32)))
                GenerateAllTextures();

            EditorGUILayout.Space(8);

            // ── Passo 2 ──
            EditorGUILayout.LabelField("2. Criar/Atualizar Materiais", EditorStyles.boldLabel);
            if (GUILayout.Button("Criar/Atualizar Materiais PBR (Assets/Materials)", GUILayout.Height(32)))
                CreateOrUpdateAllMaterials();

            EditorGUILayout.Space(8);

            // ── Passo 3 ──
            EditorGUILayout.LabelField("3. Aplicar na Cena", EditorStyles.boldLabel);
            _addTopMolding = EditorGUILayout.Toggle("Adicionar friso no topo das paredes", _addTopMolding);
            EditorGUILayout.HelpBox(
                "Cria uma faixa fina (cubo esticado) no topo de cada segmento de parede encontrado " +
                "sob 'Museum_Geometry', usando o material de Rodapé/Moldura — evita que a parede " +
                "fique 'chapada' apenas com a base. Independente do teto caixotado do Hall " +
                "(MuseumCeilingBuilder), que já tem seu próprio friso.",
                MessageType.None);
            if (GUILayout.Button("Aplicar Materiais PBR nos Objetos da Cena", GUILayout.Height(32)))
                ApplyMaterialsToScene();

            EditorGUILayout.Space(16);

            GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
            if (GUILayout.Button("▶  FAZER TUDO (Gerar → Criar → Aplicar)", GUILayout.Height(46)))
                RunAll();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12);
            EditorGUILayout.HelpBox(
                "PRESSUPOSTOS DE NOMES (confirmados em MuseumCompleteBuilder.cs):\n" +
                "  • Raiz: 'Museum_Geometry'\n" +
                "  • Piso: objeto chamado 'Piso' · Teto: objeto chamado 'Teto'\n" +
                "  • Paredes: nomes iniciando com 'Parede_' (ex: Parede_Norte, Parede_Norte_E, Parede_Norte_Hdr)\n" +
                "  • Rodapés: nomes iniciando com 'Rodape_' (ex: Rodape_N, Rodape_S, Rodape_L, Rodape_O)\n\n" +
                "Se a geometria da sua cena foi renomeada manualmente ou construída por outro fluxo, " +
                "verifique os nomes na Hierarchy antes de rodar o passo 3 — objetos com nomes diferentes " +
                "não serão encontrados e não terão seu material trocado.",
                MessageType.Warning);

            EditorGUILayout.EndScrollView();
        }

        private void RunAll()
        {
            GenerateAllTextures();
            CreateOrUpdateAllMaterials();
            ApplyMaterialsToScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Texturas PBR Aplicadas",
                    "Texturas geradas em Assets/Textures/\n" +
                    "Materiais criados em Assets/Materials/\n" +
                    "Materiais aplicados aos objetos de 'Museum_Geometry'\n\n" +
                    "Salve a cena com Ctrl+S e confira o resultado na Scene View / Play.", "OK");

            Debug.Log("[MuseumModerna] Texturas e Materiais PBR: pipeline completo executado.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // PASSO 1 — GERAÇÃO DE TEXTURAS
        // ══════════════════════════════════════════════════════════════════════
        private void GenerateAllTextures()
        {
            EnsureFolder(TextureFolder);

            Texture2D floorAlbedo = null, floorNormal = null, floorMS = null;
            Texture2D wallAlbedo  = null, wallNormal  = null, wallMS  = null;
            Texture2D ceilAlbedo  = null, ceilNormal  = null, ceilMS  = null;
            Texture2D trimAlbedo  = null, trimNormal  = null, trimMS  = null;

            try
            {
                EditorUtility.DisplayProgressBar("Texturas PBR", "Gerando piso (madeira)...", 0.05f);
                GenerateWoodFloorMaps(_floorResolution, _seed, out floorAlbedo, out floorNormal, out floorMS);
                SaveTexturePNG(floorAlbedo, FloorAlbedoPath);
                SaveTexturePNG(floorNormal, FloorNormalPath);
                SaveTexturePNG(floorMS, FloorMetallicSmoothnessPath);

                EditorUtility.DisplayProgressBar("Texturas PBR", "Gerando parede (reboco)...", 0.35f);
                GeneratePlasterMaps(_wallResolution, _seed + 1,
                    new Color(0.90f, 0.88f, 0.83f), bumpStrength: 0.9f,
                    out wallAlbedo, out wallNormal, out wallMS);
                SaveTexturePNG(wallAlbedo, WallAlbedoPath);
                SaveTexturePNG(wallNormal, WallNormalPath);
                SaveTexturePNG(wallMS, WallMetallicSmoothnessPath);

                EditorUtility.DisplayProgressBar("Texturas PBR", "Gerando teto (reboco claro)...", 0.55f);
                GeneratePlasterMaps(_ceilingResolution, _seed + 2,
                    new Color(0.97f, 0.96f, 0.91f), bumpStrength: 0.55f,
                    out ceilAlbedo, out ceilNormal, out ceilMS);
                SaveTexturePNG(ceilAlbedo, CeilingAlbedoPath);
                SaveTexturePNG(ceilNormal, CeilingNormalPath);
                SaveTexturePNG(ceilMS, CeilingMetallicSmoothnessPath);

                EditorUtility.DisplayProgressBar("Texturas PBR", "Gerando rodapé/moldura...", 0.75f);
                GenerateTrimMaps(_baseboardResolution, _seed + 3, out trimAlbedo, out trimNormal, out trimMS);
                SaveTexturePNG(trimAlbedo, BaseboardAlbedoPath);
                SaveTexturePNG(trimNormal, BaseboardNormalPath);
                SaveTexturePNG(trimMS, BaseboardMetallicSmoothnessPath);

                EditorUtility.DisplayProgressBar("Texturas PBR", "Importando assets...", 0.92f);
                AssetDatabase.Refresh();

                ConfigureImporter(FloorAlbedoPath, isNormalMap: false, isSRGB: true, _floorResolution);
                ConfigureImporter(FloorNormalPath, isNormalMap: true, isSRGB: false, _floorResolution);
                ConfigureImporter(FloorMetallicSmoothnessPath, isNormalMap: false, isSRGB: false, _floorResolution);

                ConfigureImporter(WallAlbedoPath, isNormalMap: false, isSRGB: true, _wallResolution);
                ConfigureImporter(WallNormalPath, isNormalMap: true, isSRGB: false, _wallResolution);
                ConfigureImporter(WallMetallicSmoothnessPath, isNormalMap: false, isSRGB: false, _wallResolution);

                ConfigureImporter(CeilingAlbedoPath, isNormalMap: false, isSRGB: true, _ceilingResolution);
                ConfigureImporter(CeilingNormalPath, isNormalMap: true, isSRGB: false, _ceilingResolution);
                ConfigureImporter(CeilingMetallicSmoothnessPath, isNormalMap: false, isSRGB: false, _ceilingResolution);

                ConfigureImporter(BaseboardAlbedoPath, isNormalMap: false, isSRGB: true, _baseboardResolution);
                ConfigureImporter(BaseboardNormalPath, isNormalMap: true, isSRGB: false, _baseboardResolution);
                ConfigureImporter(BaseboardMetallicSmoothnessPath, isNormalMap: false, isSRGB: false, _baseboardResolution);

                AssetDatabase.SaveAssets();
                Debug.Log("[MuseumModerna] Texturas PBR geradas em " + TextureFolder + "/.");
            }
            finally
            {
                // As texturas geradas em memória já foram gravadas em disco como PNG — não são
                // assets (não foram criadas via AssetDatabase.CreateAsset), então liberamos a RAM.
                // (finally garante que isso roda mesmo se algo falhar no meio da geração.)
                if (floorAlbedo != null) DestroyImmediate(floorAlbedo);
                if (floorNormal != null) DestroyImmediate(floorNormal);
                if (floorMS     != null) DestroyImmediate(floorMS);
                if (wallAlbedo  != null) DestroyImmediate(wallAlbedo);
                if (wallNormal  != null) DestroyImmediate(wallNormal);
                if (wallMS      != null) DestroyImmediate(wallMS);
                if (ceilAlbedo  != null) DestroyImmediate(ceilAlbedo);
                if (ceilNormal  != null) DestroyImmediate(ceilNormal);
                if (ceilMS      != null) DestroyImmediate(ceilMS);
                if (trimAlbedo  != null) DestroyImmediate(trimAlbedo);
                if (trimNormal  != null) DestroyImmediate(trimNormal);
                if (trimMS      != null) DestroyImmediate(trimMS);

                EditorUtility.ClearProgressBar();
            }
        }

        // ── Piso: tábuas de madeira com sulco entre tábuas + veio de madeira ────
        private static void GenerateWoodFloorMaps(int size, int seed,
            out Texture2D albedo, out Texture2D normal, out Texture2D metallicSmooth)
        {
            float freq = 1024f / size; // normaliza a frequência do ruído entre resoluções

            float[,] height = new float[size, size];
            Color[] pixels = new Color[size * size];

            const int plankCount = 8;
            float plankPx = size / (float)plankCount;
            float seamPx  = Mathf.Max(1f, size * 0.003f);
            Color woodBase = new Color(0.33f, 0.20f, 0.10f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int plankIndex = Mathf.FloorToInt(x / plankPx);
                    float withinPlank = (x - plankIndex * plankPx) / plankPx;

                    float plankRand = Hash01(plankIndex, seed);
                    float tint = Mathf.Lerp(0.82f, 1.18f, plankRand);

                    float grain = Mathf.PerlinNoise(x * 0.015f * freq + seed * 11.3f, y * 0.12f * freq + plankIndex * 3.7f);
                    float fine  = Mathf.PerlinNoise(x * 0.18f * freq + seed * 5.1f, y * 0.18f * freq + seed * 7.9f);

                    float shade = tint * (0.88f + grain * 0.24f + fine * 0.06f);
                    Color c = woodBase * shade;

                    // sulco (grout) entre tábuas
                    float edgePx = Mathf.Min(withinPlank, 1f - withinPlank) * plankPx;
                    float seamFactor = Mathf.Clamp01(edgePx / seamPx);
                    c *= Mathf.Lerp(0.35f, 1f, seamFactor);
                    c.a = 1f;

                    pixels[y * size + x] = c;
                    height[x, y] = seamFactor * 0.55f + grain * 0.35f + fine * 0.10f;
                }
            }

            albedo = BuildTexture(size, pixels, sRGB: true);
            normal = BuildNormalFromHeight(size, height, strength: 2.2f);
            metallicSmooth = BuildMetallicSmoothness(size, height,
                metallicConst: 0.02f, smoothBase: 0.55f, smoothVariance: -0.30f);
        }

        // ── Parede / Teto: reboco/pintura sutil, baixo contraste, relevo suave ──
        private static void GeneratePlasterMaps(int size, int seed, Color baseColor, float bumpStrength,
            out Texture2D albedo, out Texture2D normal, out Texture2D metallicSmooth)
        {
            float freq = 1024f / size;

            float[,] height = new float[size, size];
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.010f * freq + seed * 3.1f, y * 0.010f * freq + seed * 2.7f);
                    float n2 = Mathf.PerlinNoise(x * 0.06f * freq + seed * 9.3f, y * 0.06f * freq + seed * 4.4f);
                    float relief = n1 * 0.7f + n2 * 0.3f;

                    // ruído de baixíssimo contraste — é reboco/pintura, não deve "gritar"
                    float shade = 0.94f + (relief - 0.5f) * 0.10f;
                    Color c = baseColor * shade;
                    c.a = 1f;

                    pixels[y * size + x] = c;
                    height[x, y] = relief;
                }
            }

            albedo = BuildTexture(size, pixels, sRGB: true);
            normal = BuildNormalFromHeight(size, height, strength: bumpStrength);
            metallicSmooth = BuildMetallicSmoothness(size, height,
                metallicConst: 0f, smoothBase: 0.22f, smoothVariance: 0.08f);
        }

        // ── Rodapé / Moldura: textura de acabamento distinta da parede lisa ────
        private static void GenerateTrimMaps(int size, int seed,
            out Texture2D albedo, out Texture2D normal, out Texture2D metallicSmooth)
        {
            float freq = 1024f / size;

            float[,] height = new float[size, size];
            Color[] pixels = new Color[size * size];
            Color baseColor = new Color(0.60f, 0.56f, 0.48f); // tom de madeira/gesso, distinto da parede e do piso

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // linhas horizontais finas — sugestão de veio de madeira pintada / friso
                    float lines = Mathf.PerlinNoise(x * 0.02f * freq + seed * 6.6f, y * 0.45f * freq + seed * 2.2f);
                    float fine  = Mathf.PerlinNoise(x * 0.3f * freq + seed, y * 0.3f * freq + seed);

                    float shade = 0.90f + lines * 0.14f + fine * 0.04f;
                    Color c = baseColor * shade;
                    c.a = 1f;

                    pixels[y * size + x] = c;
                    height[x, y] = lines * 0.6f + fine * 0.4f;
                }
            }

            albedo = BuildTexture(size, pixels, sRGB: true);
            normal = BuildNormalFromHeight(size, height, strength: 1.4f);
            metallicSmooth = BuildMetallicSmoothness(size, height,
                metallicConst: 0.05f, smoothBase: 0.50f, smoothVariance: 0.10f);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS — Construção de textura a partir de ruído/altura
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Hash determinístico [0,1) usado para dar tint aleatório por tábua.</summary>
        private static float Hash01(int n, int seed)
        {
            unchecked
            {
                int h = n * 374761393 + seed * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        /// <summary>Cria um Texture2D a partir de um array de cores. sRGB=true para albedo (cor), false para mapas lineares (normal/metallic-smoothness).</summary>
        private static Texture2D BuildTexture(int size, Color[] pixels, bool sRGB)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, linear: !sRGB);
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        /// <summary>Converte um campo de altura em normal map (estilo Sobel/diferenças centrais), com wrap para textura tileable.</summary>
        private static Texture2D BuildNormalFromHeight(int size, float[,] height, float strength)
        {
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                int y0 = (y - 1 + size) % size;
                int y1 = (y + 1) % size;

                for (int x = 0; x < size; x++)
                {
                    int x0 = (x - 1 + size) % size;
                    int x1 = (x + 1) % size;

                    float hl = height[x0, y];
                    float hr = height[x1, y];
                    float hd = height[x, y0];
                    float hu = height[x, y1];

                    float dx = (hl - hr) * strength;
                    float dy = (hd - hu) * strength;

                    Vector3 n = new Vector3(dx, dy, 1f).normalized;
                    pixels[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }

            return BuildTexture(size, pixels, sRGB: false);
        }

        /// <summary>Mapa Metallic/Smoothness no formato do Standard shader: R = metallic (constante), A = smoothness (varia com a altura/ruído).</summary>
        private static Texture2D BuildMetallicSmoothness(int size, float[,] heightOrNoise,
            float metallicConst, float smoothBase, float smoothVariance)
        {
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float h = heightOrNoise[x, y];
                    float smooth = Mathf.Clamp01(smoothBase + (h - 0.5f) * smoothVariance);
                    pixels[y * size + x] = new Color(metallicConst, metallicConst, metallicConst, smooth);
                }
            }

            return BuildTexture(size, pixels, sRGB: false);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS — Disco / AssetDatabase
        // ══════════════════════════════════════════════════════════════════════

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;

            string parent = assetFolderPath.Substring(0, assetFolderPath.LastIndexOf('/'));
            string leaf   = assetFolderPath.Substring(assetFolderPath.LastIndexOf('/') + 1);

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent); // "Assets" sempre é válido, então a recursão termina

            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SaveTexturePNG(Texture2D tex, string assetPath)
        {
            byte[] bytes = tex.EncodeToPNG();
            string abs = ToAbsolutePath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(abs));
            File.WriteAllBytes(abs, bytes);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            // assetPath sempre começa com "Assets/"
            string rel = assetPath.Substring("Assets/".Length);
            return Path.Combine(Application.dataPath, rel).Replace('\\', '/');
        }

        private static void ConfigureImporter(string assetPath, bool isNormalMap, bool isSRGB, int maxSize)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[MuseumModerna] TextureImporter não encontrado para '{assetPath}'.");
                return;
            }

            importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.convertToNormalmap = false; // já geramos o normal map real via Sobel, não a partir de grayscale
            if (!isNormalMap) importer.sRGBTexture = isSRGB; // NormalMap sempre trata como linear internamente
            importer.mipmapEnabled = true;
            importer.wrapMode      = TextureWrapMode.Repeat;
            importer.filterMode    = FilterMode.Bilinear;
            importer.maxTextureSize = maxSize;

            // Override Android: ASTC_6x6 — mesmo formato sugerido em MuseumAdvancedLightingSetup
            // (lá aplicado como MobileTextureSubtarget global; aqui como override por textura).
            // Nota: em algumas versões do Unity 2020+ esse enum combinado (RGB+RGBA) aparece marcado
            // [Obsolete] em favor de ASTC_RGB_6x6 / ASTC_RGBA_6x6 — ainda compila e funciona, apenas
            // gera um warning. Mantido assim porque o mapa Metallic/Smoothness usa o canal alpha
            // (smoothness) e o formato combinado preserva alpha. Troque para ASTC_RGBA_6x6 se quiser
            // eliminar o warning no seu Editor.
            var androidSettings = new TextureImporterPlatformSettings
            {
                name                = "Android",
                overridden          = true,
                maxTextureSize      = maxSize,
                format              = TextureImporterFormat.ASTC_6x6,
                textureCompression  = TextureImporterCompression.Compressed,
            };
            importer.SetPlatformTextureSettings(androidSettings);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PASSO 2 — MATERIAIS
        // ══════════════════════════════════════════════════════════════════════
        private void CreateOrUpdateAllMaterials()
        {
            EnsureFolder(MaterialFolder);

            var floorAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(FloorAlbedoPath);
            var floorNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(FloorNormalPath);
            var floorMS     = AssetDatabase.LoadAssetAtPath<Texture2D>(FloorMetallicSmoothnessPath);

            var wallAlbedo  = AssetDatabase.LoadAssetAtPath<Texture2D>(WallAlbedoPath);
            var wallNormal  = AssetDatabase.LoadAssetAtPath<Texture2D>(WallNormalPath);
            var wallMS      = AssetDatabase.LoadAssetAtPath<Texture2D>(WallMetallicSmoothnessPath);

            var ceilAlbedo  = AssetDatabase.LoadAssetAtPath<Texture2D>(CeilingAlbedoPath);
            var ceilNormal  = AssetDatabase.LoadAssetAtPath<Texture2D>(CeilingNormalPath);
            var ceilMS      = AssetDatabase.LoadAssetAtPath<Texture2D>(CeilingMetallicSmoothnessPath);

            var trimAlbedo  = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseboardAlbedoPath);
            var trimNormal  = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseboardNormalPath);
            var trimMS      = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseboardMetallicSmoothnessPath);

            if (floorAlbedo == null || wallAlbedo == null || ceilAlbedo == null || trimAlbedo == null)
            {
                Debug.LogWarning("[MuseumModerna] Texturas PBR não encontradas em " + TextureFolder +
                    "/. Clique primeiro em 'Gerar Texturas' (passo 1).");
                return;
            }

            CreateOrUpdateMaterial(MatFloorPath, "Museo_PBR_Floor",
                floorAlbedo, floorNormal, floorMS, normalScale: 1f, new Vector2(_floorTiling, _floorTiling));

            CreateOrUpdateMaterial(MatWallPath, "Museo_PBR_Wall",
                wallAlbedo, wallNormal, wallMS, normalScale: 0.4f, new Vector2(_wallTiling, _wallTiling));

            CreateOrUpdateMaterial(MatCeilingPath, "Museo_PBR_Ceiling",
                ceilAlbedo, ceilNormal, ceilMS, normalScale: 0.3f, new Vector2(_wallTiling, _wallTiling));

            CreateOrUpdateMaterial(MatBaseboardPath, "Museo_PBR_Baseboard",
                trimAlbedo, trimNormal, trimMS, normalScale: 0.6f, new Vector2(4f, 1f));

            AssetDatabase.SaveAssets();
            Debug.Log("[MuseumModerna] Materiais PBR criados/atualizados em " + MaterialFolder + "/.");
        }

        private static Material CreateOrUpdateMaterial(string path, string matName,
            Texture2D albedo, Texture2D normal, Texture2D metallicSmooth,
            float normalScale, Vector2 tiling)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = mat == null;
            if (isNew) mat = new Material(Shader.Find("Standard"));

            mat.name  = matName;
            mat.color = Color.white; // a cor real vem do albedo; tint neutro

            mat.SetTexture("_MainTex", albedo);
            mat.SetTextureScale("_MainTex", tiling);

            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetTextureScale("_BumpMap", tiling);
                mat.SetFloat("_BumpScale", normalScale);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (metallicSmooth != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallicSmooth);
                mat.SetTextureScale("_MetallicGlossMap", tiling);
                mat.SetFloat("_GlossMapScale", 1f); // smoothness lido integralmente do canal A do mapa
                mat.EnableKeyword("_METALLICGLOSSMAP");
            }

            if (isNew) AssetDatabase.CreateAsset(mat, path);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PASSO 3 — APLICAR NA CENA
        // ══════════════════════════════════════════════════════════════════════
        private void ApplyMaterialsToScene()
        {
            var matFloor     = AssetDatabase.LoadAssetAtPath<Material>(MatFloorPath);
            var matWall      = AssetDatabase.LoadAssetAtPath<Material>(MatWallPath);
            var matCeiling   = AssetDatabase.LoadAssetAtPath<Material>(MatCeilingPath);
            var matBaseboard = AssetDatabase.LoadAssetAtPath<Material>(MatBaseboardPath);

            if (matFloor == null || matWall == null || matCeiling == null || matBaseboard == null)
            {
                Debug.LogWarning("[MuseumModerna] Materiais PBR não encontrados em " + MaterialFolder +
                    "/. Rode os passos 1 e 2 antes de aplicar na cena.");
                return;
            }

            GameObject root = GameObject.Find("Museum_Geometry");
            if (root == null)
            {
                Debug.LogWarning(
                    "[MuseumModerna] GameObject 'Museum_Geometry' não encontrado na cena aberta. " +
                    "Abra a MuseumScene e rode 'MuseumModerna → Construir Museu Completo' (Etapa 1) " +
                    "antes de aplicar as texturas. Se a geometria foi criada/renomeada manualmente, " +
                    "verifique o nome do objeto raiz na Hierarchy.");
                return;
            }

            int floorCount = 0, wallCount = 0, ceilCount = 0, baseCount = 0;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                var mr = t.GetComponent<MeshRenderer>();
                if (mr == null) continue;

                string n = t.name;
                if (n == "Piso")
                {
                    Undo.RecordObject(mr, "Aplicar Material PBR");
                    mr.sharedMaterial = matFloor;
                    floorCount++;
                }
                else if (n == "Teto")
                {
                    Undo.RecordObject(mr, "Aplicar Material PBR");
                    mr.sharedMaterial = matCeiling;
                    ceilCount++;
                }
                else if (n.StartsWith("Parede_"))
                {
                    Undo.RecordObject(mr, "Aplicar Material PBR");
                    mr.sharedMaterial = matWall;
                    wallCount++;
                }
                else if (n.StartsWith("Rodape_"))
                {
                    Undo.RecordObject(mr, "Aplicar Material PBR");
                    mr.sharedMaterial = matBaseboard;
                    baseCount++;
                }
            }

            if (_addTopMolding)
                AddWallTopMoldingStrips(root.transform, matBaseboard);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[MuseumModerna] Materiais PBR aplicados — Piso:{floorCount} Parede:{wallCount} " +
                $"Teto:{ceilCount} Rodapé:{baseCount}. Nomes assumidos a partir de MuseumCompleteBuilder.cs " +
                "(Piso/Teto/Parede_*/Rodape_* sob 'Museum_Geometry'). Confira a Hierarchy se a geometria " +
                "tiver sido customizada.");

            if (floorCount == 0 && wallCount == 0 && ceilCount == 0 && baseCount == 0)
                Debug.LogWarning("[MuseumModerna] Nenhum objeto correspondente encontrado sob 'Museum_Geometry'. " +
                    "Verifique se os nomes Piso/Teto/Parede_*/Rodape_* batem com a Hierarchy da cena aberta.");
        }

        /// <summary>
        /// Cria uma faixa fina (moldura) no topo de cada segmento de parede encontrado, usando o
        /// material de rodapé/moldura. Assume que os objetos "Parede_*" são cubos sem rotação
        /// (verdade em MuseumCompleteBuilder.WallNS/WallEW: apenas posição/escala, sem transform.rotation),
        /// então transform.localScale ≈ (largura, altura, profundidade) e localPosition ≈ posição mundial,
        /// pois os GameObjects de sala (ex.: "Hall_Central") também não têm transform aplicada.
        /// </summary>
        private static void AddWallTopMoldingStrips(Transform museumGeometryRoot, Material moldingMat)
        {
            const float stripHeight = 0.10f;
            const float stripMargin = 0.02f; // levemente mais larga que a parede, evita z-fighting nas bordas

            int created = 0;

            foreach (var wallMr in museumGeometryRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                Transform wt = wallMr.transform;
                if (!wt.name.StartsWith("Parede_")) continue;
                if (wt.parent == null) continue;

                string stripName = "Moldura_Topo_" + wt.name;
                Transform existing = wt.parent.Find(stripName);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);

                Vector3 scale = wt.localScale;
                Vector3 pos   = wt.position;
                float topY = pos.y + scale.y * 0.5f - stripHeight * 0.5f;

                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = stripName;
                Undo.RegisterCreatedObjectUndo(strip, "Create Wall Top Molding");
                strip.transform.SetParent(wt.parent, worldPositionStays: false);
                strip.transform.position   = new Vector3(pos.x, topY, pos.z);
                strip.transform.rotation   = Quaternion.identity; // paredes não têm rotação (ver WallNS/WallEW)
                strip.transform.localScale = new Vector3(scale.x + stripMargin, stripHeight, scale.z + stripMargin);
                strip.GetComponent<MeshRenderer>().sharedMaterial = moldingMat;
                Object.DestroyImmediate(strip.GetComponent<BoxCollider>());

                created++;
            }

            Debug.Log($"[MuseumModerna] {created} faixa(s) de moldura criada(s) no topo das paredes.");
        }
    }
}
