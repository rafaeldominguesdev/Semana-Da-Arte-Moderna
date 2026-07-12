using UnityEditor;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Cria objetos decorativos típicos de museu usando primitivas Unity.
    /// Acesse: MuseumModerna → Decoração do Museu
    ///
    /// Objetos disponíveis:
    ///  - Banco de visitante (assento + 4 pernas)
    ///  - Pedestal para escultura
    ///  - Barreira de isolamento (poste + corda)
    ///  - Placa de sinalização (direcional ou informativa)
    ///  - Rodapé nas paredes
    ///  - Moldura nos quadros (via PaintingFrame.cs)
    ///  - Planta decorativa (vaso + folhagem)
    ///  - Tapete/carpete direcional (indica caminho de circulação)
    ///  - Lixeira discreta (corpo + tampa recuada)
    ///
    /// Todos os objetos são criados no centro da cena (origem).
    /// Posicione manualmente após criar.
    /// </summary>
    public class MuseumDecorationSetup : EditorWindow
    {
        // ─── Materiais reutilizáveis ───────────────────────────────────────────
        private static Material _woodMat;
        private static Material _whiteMat;
        private static Material _goldMat;
        private static Material _redMat;
        private static Material _darkMetalMat;
        private static Material _potMat;
        private static Material _leafMat;
        private static Material _rugMat;
        private static Material _trashLidMat;

        private Vector2 _scroll;
        private int _benchCount  = 3;
        private int _barrierCount = 2;
        private int _plantCount = 3;
        private int _rugCount = 2;
        private int _trashBinCount = 3;
        private bool _addFramesToPaintings = true;

        // ─── Menu ─────────────────────────────────────────────────────────────

        [MenuItem("MuseumModerna/Decoração do Museu")]
        public static void ShowWindow()
        {
            GetWindow<MuseumDecorationSetup>("Museum Decoration").minSize = new Vector2(380, 520);
        }

        // ─── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Decoração do Museu", title);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Cria mobiliário e elementos decorativos usando primitivas Unity.\n" +
                "Posicione os objetos na hierarquia após criar.\n" +
                "Para substituir: delete os GameObjects e recrie.",
                MessageType.Info);

            EditorGUILayout.Space(12);

            // ── Bancos ──
            EditorGUILayout.LabelField("Bancos", EditorStyles.boldLabel);
            _benchCount = EditorGUILayout.IntSlider("Quantidade de bancos", _benchCount, 1, 8);
            if (GUILayout.Button("Criar Bancos de Visitante"))
                CreateBenches(_benchCount);

            EditorGUILayout.Space(6);

            // ── Pedestais ──
            EditorGUILayout.LabelField("Pedestais", EditorStyles.boldLabel);
            if (GUILayout.Button("Criar Pedestal para Escultura"))
                CreatePedestal();

            EditorGUILayout.Space(6);

            // ── Barreiras ──
            EditorGUILayout.LabelField("Barreiras de Isolamento", EditorStyles.boldLabel);
            _barrierCount = EditorGUILayout.IntSlider("Quantidade de barreiras", _barrierCount, 1, 6);
            if (GUILayout.Button("Criar Barreiras (poste + corda)"))
                CreateBarriers(_barrierCount);

            EditorGUILayout.Space(6);

            // ── Placas ──
            EditorGUILayout.LabelField("Placas de Sinalização", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Placa → Saída")) CreateSign("SAÍDA →");
            if (GUILayout.Button("Placa ← Entrada")) CreateSign("← ENTRADA");
            if (GUILayout.Button("Placa SILÊNCIO")) CreateSign("SILÊNCIO");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ── Plantas ──
            EditorGUILayout.LabelField("Plantas Decorativas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Vasos com folhagem baixa-poli. Posicione perto de paredes e cantos.",
                MessageType.None);
            _plantCount = EditorGUILayout.IntSlider("Quantidade de plantas", _plantCount, 1, 8);
            if (GUILayout.Button("Criar Plantas Decorativas"))
                CreatePlant(_plantCount);

            EditorGUILayout.Space(6);

            // ── Tapetes ──
            EditorGUILayout.LabelField("Tapetes / Carpetes Direcionais", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Faixas longas e estreitas no piso, sugerindo o caminho de circulação dos visitantes.",
                MessageType.None);
            _rugCount = EditorGUILayout.IntSlider("Quantidade de tapetes", _rugCount, 1, 6);
            if (GUILayout.Button("Criar Tapetes Direcionais"))
                CreateRug(_rugCount);

            EditorGUILayout.Space(6);

            // ── Lixeiras ──
            EditorGUILayout.LabelField("Lixeiras Discretas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Lixeiras pequenas e discretas (corpo + tampa recuada). Posicione perto de cantos e saídas.",
                MessageType.None);
            _trashBinCount = EditorGUILayout.IntSlider("Quantidade de lixeiras", _trashBinCount, 1, 8);
            if (GUILayout.Button("Criar Lixeiras Discretas"))
                CreateTrashBin(_trashBinCount);

            EditorGUILayout.Space(6);

            // ── Molduras ──
            EditorGUILayout.LabelField("Molduras 3D nos Quadros", EditorStyles.boldLabel);
            _addFramesToPaintings = EditorGUILayout.Toggle("Adicionar a todos os PaintingExhibits", _addFramesToPaintings);
            if (GUILayout.Button("Adicionar Molduras nos Quadros"))
                AddFramesToAllPaintings();

            EditorGUILayout.Space(16);

            // ── Tudo de uma vez ──
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Criar Decoração Completa (tudo)", GUILayout.Height(42)))
                CreateAll();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12);
            DrawAssetSuggestions();

            EditorGUILayout.EndScrollView();
        }

        // ─── Criar Tudo ───────────────────────────────────────────────────────

        private void CreateAll()
        {
            CreateBenches(_benchCount);
            CreatePedestal();
            CreateBarriers(_barrierCount);
            CreateSign("SAÍDA →");
            CreatePlant(_plantCount);
            CreateRug(_rugCount);
            CreateTrashBin(_trashBinCount);
            if (_addFramesToPaintings) AddFramesToAllPaintings();
            Debug.Log("[MuseumModerna] Decoração completa criada. Posicione os objetos na hierarquia.");
        }

        // ─── Banco ────────────────────────────────────────────────────────────

        private static void CreateBenches(int count)
        {
            GameObject group = new GameObject("Bancos");
            Undo.RegisterCreatedObjectUndo(group, "Create Benches");

            float spacing = 1.8f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3(startX + i * spacing, 0, 0);
                GameObject bench = CreateBench();
                bench.transform.SetParent(group.transform);
                bench.transform.localPosition = pos;
                bench.name = $"Banco_{i + 1}";
            }

            Selection.activeGameObject = group;
            Debug.Log($"[MuseumModerna] {count} banco(s) criado(s). Posicione o grupo 'Bancos' na cena.");
        }

        private static GameObject CreateBench()
        {
            Material woodMat = GetOrCreate(ref _woodMat, new Color(0.28f, 0.17f, 0.07f), 0.1f, 0.3f, "Wood_Dark");
            Material metalMat = GetOrCreate(ref _darkMetalMat, new Color(0.12f, 0.12f, 0.13f), 0.8f, 0.5f, "Metal_Dark");

            GameObject bench = new GameObject("Banco");

            // Assento
            AddPrimitive(bench, "Assento",
                new Vector3(0f, 0.44f, 0f),
                new Vector3(1.3f, 0.07f, 0.42f),
                woodMat);

            // Encosto
            AddPrimitive(bench, "Encosto",
                new Vector3(0f, 0.72f, -0.17f),
                new Vector3(1.3f, 0.05f, 0.38f),
                woodMat);

            // Pernas (4x)
            float[] xs = { -0.55f, 0.55f };
            float[] zs = { -0.17f, 0.17f };
            int leg = 1;
            foreach (float x in xs)
                foreach (float z in zs)
                {
                    AddPrimitive(bench, $"Perna_{leg++}",
                        new Vector3(x, 0.22f, z),
                        new Vector3(0.05f, 0.44f, 0.05f),
                        metalMat);
                }

            return bench;
        }

        // ─── Pedestal ─────────────────────────────────────────────────────────

        private static void CreatePedestal()
        {
            Material whiteMat = GetOrCreate(ref _whiteMat, new Color(0.92f, 0.92f, 0.9f), 0f, 0.4f, "Pedestal_White");

            GameObject pedestal = new GameObject("Pedestal");
            Undo.RegisterCreatedObjectUndo(pedestal, "Create Pedestal");

            AddPrimitive(pedestal, "Base",   new Vector3(0, 0.06f, 0), new Vector3(0.45f, 0.12f, 0.45f), whiteMat);
            AddPrimitive(pedestal, "Coluna", new Vector3(0, 0.62f, 0), new Vector3(0.28f, 0.96f, 0.28f), whiteMat);
            AddPrimitive(pedestal, "Topo",   new Vector3(0, 1.12f, 0), new Vector3(0.45f, 0.08f, 0.45f), whiteMat);

            Selection.activeGameObject = pedestal;
            Debug.Log("[MuseumModerna] Pedestal criado. Coloque uma escultura em cima e posicione na cena.");
        }

        // ─── Barreira de Isolamento ───────────────────────────────────────────

        private static void CreateBarriers(int count)
        {
            Material goldMat   = GetOrCreate(ref _goldMat,   new Color(0.72f, 0.58f, 0.12f), 0.9f, 0.8f, "Gold");
            Material redMat    = GetOrCreate(ref _redMat,    new Color(0.7f,  0.05f, 0.05f), 0f,   0.2f, "Rope_Red");

            GameObject group = new GameObject("Barreiras");
            Undo.RegisterCreatedObjectUndo(group, "Create Barriers");

            float spacing = 1.5f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float xPos = startX + i * spacing;
                GameObject barrier = CreateSingleBarrier(goldMat, redMat);
                barrier.transform.SetParent(group.transform);
                barrier.transform.localPosition = new Vector3(xPos, 0, 0);
                barrier.name = $"Barreira_{i + 1}";
            }

            Selection.activeGameObject = group;
            Debug.Log($"[MuseumModerna] {count} barreira(s) criada(s). Posicione em frente às obras.");
        }

        private static GameObject CreateSingleBarrier(Material postMat, Material ropeMat)
        {
            GameObject barrier = new GameObject("Barreira");

            // Poste esquerdo
            CreatePost(barrier, "Poste_Esq", new Vector3(-0.65f, 0, 0), postMat);
            // Poste direito
            CreatePost(barrier, "Poste_Dir", new Vector3(0.65f, 0, 0), postMat);

            // Corda horizontal (cilindro deitado)
            GameObject rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rope.name = "Corda";
            rope.transform.SetParent(barrier.transform, false);
            rope.transform.localPosition = new Vector3(0, 0.82f, 0);
            rope.transform.localRotation = Quaternion.Euler(0, 0, 90f); // deitado
            rope.transform.localScale = new Vector3(0.022f, 0.65f, 0.022f);
            rope.GetComponent<MeshRenderer>().sharedMaterial = ropeMat;
            DestroyImmediate(rope.GetComponent<CapsuleCollider>());

            return barrier;
        }

        private static void CreatePost(GameObject parent, string name, Vector3 localPos, Material mat)
        {
            Material goldMat = GetOrCreate(ref _goldMat, new Color(0.72f, 0.58f, 0.12f), 0.9f, 0.8f, "Gold");

            // Haste
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = name;
            post.transform.SetParent(parent.transform, false);
            post.transform.localPosition = localPos + new Vector3(0, 0.45f, 0);
            post.transform.localScale = new Vector3(0.045f, 0.45f, 0.045f);
            post.GetComponent<MeshRenderer>().sharedMaterial = mat;
            DestroyImmediate(post.GetComponent<CapsuleCollider>());

            // Esfera no topo
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            top.name = "Topo";
            top.transform.SetParent(post.transform, false);
            top.transform.localPosition = new Vector3(0, 1f, 0);
            top.transform.localScale = new Vector3(2.5f, 0.25f, 2.5f);
            top.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
            DestroyImmediate(top.GetComponent<SphereCollider>());
        }

        // ─── Placa ────────────────────────────────────────────────────────────

        private static void CreateSign(string label)
        {
            Material whiteMat    = GetOrCreate(ref _whiteMat,    new Color(0.95f, 0.94f, 0.9f), 0f, 0.3f, "Sign_Panel");
            Material darkMetalMat = GetOrCreate(ref _darkMetalMat, new Color(0.12f, 0.12f, 0.13f), 0.8f, 0.5f, "Metal_Dark");

            GameObject sign = new GameObject($"Placa_{label.Replace(" ", "_").Replace("→", "").Replace("←", "").Trim()}");
            Undo.RegisterCreatedObjectUndo(sign, "Create Sign");

            // Haste
            AddPrimitive(sign, "Haste", new Vector3(0, 1.0f, 0), new Vector3(0.04f, 2.0f, 0.04f), darkMetalMat);
            // Painel
            AddPrimitive(sign, "Painel", new Vector3(0, 2.1f, 0), new Vector3(0.55f, 0.28f, 0.04f), whiteMat);

            Debug.Log($"[MuseumModerna] Placa '{label}' criada. Adicione um TextMeshPro 3D no painel para o texto.");
            Selection.activeGameObject = sign;
        }

        // ─── Planta Decorativa ────────────────────────────────────────────────

        private static void CreatePlant(int count)
        {
            GameObject group = new GameObject("Plantas");
            Undo.RegisterCreatedObjectUndo(group, "Create Plants");

            float spacing = 2.2f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                GameObject plant = CreateSinglePlant();
                plant.transform.SetParent(group.transform);
                plant.transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
                plant.name = $"Planta_{i + 1}";
            }

            Selection.activeGameObject = group;
            Debug.Log($"[MuseumModerna] {count} planta(s) criada(s). Posicione perto de paredes e cantos.");
        }

        private static GameObject CreateSinglePlant()
        {
            Material potMat  = GetOrCreate(ref _potMat,  new Color(0.55f, 0.27f, 0.12f), 0f, 0.25f, "Pot_Terracotta");
            Material leafMat = GetOrCreate(ref _leafMat, new Color(0.13f, 0.4f,  0.16f), 0f, 0.25f, "Plant_Green");

            GameObject plant = new GameObject("Planta");

            // Vaso
            AddPrimitive(plant, "Vaso", PrimitiveType.Cylinder,
                new Vector3(0, 0.15f, 0), new Vector3(0.32f, 0.15f, 0.32f), potMat);
            AddPrimitive(plant, "Vaso_Borda", PrimitiveType.Cylinder,
                new Vector3(0, 0.31f, 0), new Vector3(0.36f, 0.02f, 0.36f), potMat);

            // Caule
            AddPrimitive(plant, "Caule", PrimitiveType.Cylinder,
                new Vector3(0, 0.55f, 0), new Vector3(0.04f, 0.28f, 0.04f), leafMat);

            // Folhagem (cluster de esferas achatadas)
            Vector3[] folhas =
            {
                new Vector3( 0f,    0.85f,  0f),
                new Vector3( 0.18f, 0.72f,  0.1f),
                new Vector3(-0.16f, 0.7f,  -0.12f),
                new Vector3( 0.05f, 0.68f, -0.18f),
                new Vector3(-0.1f,  0.9f,   0.15f),
            };
            for (int i = 0; i < folhas.Length; i++)
                AddPrimitive(plant, $"Folha_{i + 1}", PrimitiveType.Sphere,
                    folhas[i], new Vector3(0.3f, 0.22f, 0.3f), leafMat);

            return plant;
        }

        // ─── Tapete Direcional ────────────────────────────────────────────────

        private static void CreateRug(int count)
        {
            GameObject group = new GameObject("Tapetes");
            Undo.RegisterCreatedObjectUndo(group, "Create Rugs");

            float spacing = 2.5f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                GameObject rug = CreateSingleRug();
                rug.transform.SetParent(group.transform);
                rug.transform.localPosition = new Vector3(startX + i * spacing, 0.01f, 0);
                rug.name = $"Tapete_{i + 1}";
            }

            Selection.activeGameObject = group;
            Debug.Log($"[MuseumModerna] {count} tapete(s) criado(s). Alinhe com o caminho de circulação dos visitantes.");
        }

        private static GameObject CreateSingleRug()
        {
            Material rugMat = GetOrCreate(ref _rugMat, new Color(0.45f, 0.03f, 0.05f), 0f, 0.15f, "Rug_Red");

            GameObject rug = new GameObject("Tapete");

            // Faixa longa e estreita, achatada, indicando direção de caminhada
            AddPrimitive(rug, "Tapete_Base", new Vector3(0, 0, 0), new Vector3(1.2f, 0.01f, 4.5f), rugMat);

            return rug;
        }

        // ─── Lixeira Discreta ─────────────────────────────────────────────────

        private static void CreateTrashBin(int count)
        {
            GameObject group = new GameObject("Lixeiras");
            Undo.RegisterCreatedObjectUndo(group, "Create Trash Bins");

            float spacing = 1.6f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                GameObject bin = CreateSingleTrashBin();
                bin.transform.SetParent(group.transform);
                bin.transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
                bin.name = $"Lixeira_{i + 1}";
            }

            Selection.activeGameObject = group;
            Debug.Log($"[MuseumModerna] {count} lixeira(s) criada(s). Posicione discretamente perto de cantos e saídas.");
        }

        private static GameObject CreateSingleTrashBin()
        {
            Material bodyMat = GetOrCreate(ref _darkMetalMat, new Color(0.12f, 0.12f, 0.13f), 0.8f, 0.5f, "Metal_Dark");
            Material lidMat  = GetOrCreate(ref _trashLidMat,  new Color(0.08f, 0.08f, 0.09f), 0.6f, 0.3f, "Metal_Dark_Lid");

            GameObject bin = new GameObject("Lixeira");

            // Corpo (discreto, baixo)
            AddPrimitive(bin, "Corpo", PrimitiveType.Cylinder,
                new Vector3(0, 0.25f, 0), new Vector3(0.22f, 0.25f, 0.22f), bodyMat);

            // Tampa levemente recuada (mais escura, disco fino)
            AddPrimitive(bin, "Tampa", PrimitiveType.Cylinder,
                new Vector3(0, 0.485f, 0), new Vector3(0.2f, 0.015f, 0.2f), lidMat);

            return bin;
        }

        // ─── Molduras nos Quadros ─────────────────────────────────────────────

        private static void AddFramesToAllPaintings()
        {
            PaintingExhibit[] exhibits = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            int count = 0;

            foreach (PaintingExhibit exhibit in exhibits)
            {
                PaintingFrame frame = exhibit.GetComponent<PaintingFrame>();
                if (frame == null)
                {
                    frame = Undo.AddComponent<PaintingFrame>(exhibit.gameObject);
                    count++;
                }
                frame.BuildFrame();
            }

            Debug.Log($"[MuseumModerna] Molduras adicionadas a {count} quadro(s).");
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static GameObject AddPrimitive(GameObject parent, string name,
            Vector3 localPos, Vector3 localScale, Material mat)
        {
            return AddPrimitive(parent, name, PrimitiveType.Cube, localPos, localScale, mat);
        }

        private static GameObject AddPrimitive(GameObject parent, string name, PrimitiveType type,
            Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Collider col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            return go;
        }

        private static Material GetOrCreate(ref Material mat, Color color, float metallic, float smoothness, string matName)
        {
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Standard"))
            {
                name = matName,
                color = color
            };
            mat.SetFloat("_Metallic",   metallic);
            mat.SetFloat("_Glossiness", smoothness);
            return mat;
        }

        // ─── Sugestões de Assets ──────────────────────────────────────────────

        private void DrawAssetSuggestions()
        {
            EditorGUILayout.LabelField("Assets Gratuitos Recomendados", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "TEXTURAS PBR (CC0 — grátis, uso comercial):\n" +
                "• polyhaven.com → buscar: 'wood floor', 'marble', 'painted plaster'\n" +
                "• ambientcg.com → buscar: 'WoodFloor', 'Marble', 'Plaster'\n" +
                "• Baixar: Albedo (Color) + Normal + Roughness + AO\n\n" +
                "MODELOS 3D DE MUSEU (Unity Asset Store grátis):\n" +
                "• 'Museum Kit' (Kenney) — kenney.nl/assets\n" +
                "• 'Free Furniture Set' (buscar no Asset Store)\n\n" +
                "PLANTAS E TAPETES (opcional, mais realismo que as primitivas):\n" +
                "• kenney.nl/assets → 'Nature Kit' (vasos e plantas low-poly)\n" +
                "• polyhaven.com → buscar 'rug', 'carpet' (para texturizar o tapete)\n\n" +
                "MÚSICA CLÁSSICA (domínio público):\n" +
                "• musopen.org → Villa-Lobos, Heitor (1922 era!)\n" +
                "• freemusicarchive.org → filtrar Classical\n\n" +
                "PASSOS EM MADEIRA:\n" +
                "• freesound.org → buscar 'footsteps wooden floor museum'",
                MessageType.None);
        }
    }
}
