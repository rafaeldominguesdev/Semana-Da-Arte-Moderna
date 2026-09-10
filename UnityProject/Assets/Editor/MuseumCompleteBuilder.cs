using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MuseumModerna
{
    /// <summary>
    /// Constrói o museu completo da Semana de Arte Moderna de 1922 em 3 etapas.
    ///
    /// Etapa 1 — Geometria: Foyer + Hall Central + Ala Pintura + Ala Escultura + Ala Literatura.
    /// Etapa 2 — Obras: 22 exhibits (PaintingInfo ScriptableObjects + GameObjects na cena).
    /// Etapa 3 — Cena: Player com giroscópio/head-gaze, iluminação dramática, UI, managers.
    ///
    /// Menu: MuseumModerna → Construir Museu Completo
    /// </summary>
    public class MuseumCompleteBuilder : EditorWindow
    {
        // ─── Constantes de geometria ──────────────────────────────────────────
        const float RH  = 4.00f;   // room height
        const float WT  = 0.25f;   // wall thickness
        const float DW  = 3.00f;   // doorway width
        const float DH  = 2.60f;   // doorway height
        const float FT  = 0.15f;   // floor/ceiling slab thickness
        const float PEH = 1.70f;   // painting eye height (center Y)
        const float PW  = 1.40f;   // painting face width
        const float PHT = 1.10f;   // painting face height

        // ─── Layout das salas (chão em Y = 0) ────────────────────────────────
        static readonly Vector3 C_FOYER   = new Vector3(  0f, 0f, -15f);
        static readonly Vector3 C_HALL    = new Vector3(  0f, 0f,   0f);
        static readonly Vector3 C_PINTURA = new Vector3(  0f, 0f,  17f);
        static readonly Vector3 C_ESC     = new Vector3( 17f, 0f,   0f);
        static readonly Vector3 C_LIT     = new Vector3(-17f, 0f,   0f);

        const float W_FOYER = 10f, D_FOYER = 10f;
        const float W_HALL  = 20f, D_HALL  = 20f;
        const float W_PIN   = 16f, D_PIN   = 14f;
        const float W_ESC   = 14f, D_ESC   = 16f;
        const float W_LIT   = 14f, D_LIT   = 16f;

        // ─── Cache de materiais (sessão do editor) ────────────────────────────
        static Material s_wall, s_floor, s_ceiling, s_baseboard, s_moldura;

        // ─── GUI ──────────────────────────────────────────────────────────────
        bool _s1 = true, _s2 = true, _s3 = true;
        Vector2 _scroll;

        // ─── Menu ─────────────────────────────────────────────────────────────
        [MenuItem("MuseumModerna/Construir Museu Completo")]
        static void Open()
        {
            var w = GetWindow<MuseumCompleteBuilder>("Museu 1922");
            w.minSize = new Vector2(440, 520);
        }

        // ─── GUI ──────────────────────────────────────────────────────────────
        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            var subStyle = new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleCenter, wordWrap = true };

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Museu — Semana de Arte Moderna 1922", titleStyle);
            EditorGUILayout.LabelField("Construtor completo: Geometria + Obras + Cena", subStyle);
            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "LAYOUT (planta em cruz):\n" +
                "  Foyer (entrada sul) → Hall Central (20×20 m)\n" +
                "  Ala Norte: PINTURA  (Anita Malfatti, Di Cavalcanti, Segall…)\n" +
                "  Ala Leste: ESCULTURA  (Victor Brecheret — 4 obras + pedestais)\n" +
                "  Ala Oeste: LITERATURA & MÚSICA  (Mário, Oswald, Graça, Villa-Lobos…)",
                MessageType.Info);

            EditorGUILayout.Space(10);
            _s1 = EditorGUILayout.Toggle("Etapa 1 — Geometria das 5 salas",         _s1);
            _s2 = EditorGUILayout.Toggle("Etapa 2 — 22 obras com dados históricos",  _s2);
            _s3 = EditorGUILayout.Toggle("Etapa 3 — Player, iluminação, UI, managers", _s3);
            EditorGUILayout.Space(14);

            GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
            if (GUILayout.Button("▶  CONSTRUIR MUSEU COMPLETO  (Etapas 1 → 3)",
                GUILayout.Height(52)))
                Run();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Só Etapa 1\nGeometria",  GUILayout.Height(38))) RunOnly(1);
            if (GUILayout.Button("Só Etapa 2\nObras",      GUILayout.Height(38))) RunOnly(2);
            if (GUILayout.Button("Só Etapa 3\nCena",       GUILayout.Height(38))) RunOnly(3);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Após construir:\n" +
                "• Salve com Ctrl+S\n" +
                "• Pressione Play para testar\n" +
                "• Para molduras douradas: MuseumModerna → Decoração do Museu\n" +
                "• Para névoa e spotlights: MuseumModerna → Iluminação Dramática",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        void RunOnly(int step) { _s1 = step == 1; _s2 = step == 2; _s3 = step == 3; Run(); }

        void Run()
        {
            s_wall = s_floor = s_ceiling = s_baseboard = s_moldura = null;

            if (_s1) Step1_Geometry();
            if (_s2) Step2_Exhibits();
            if (_s3) Step3_Scene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[MuseumModerna] Museu construído. Salve com Ctrl+S.");

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Museu Construído!",
                    (_s1 ? "✓ Etapa 1: 5 salas criadas\n" : "") +
                    (_s2 ? "✓ Etapa 2: 22 obras + ScriptableObjects\n" : "") +
                    (_s3 ? "✓ Etapa 3: Player, luzes, UI, managers\n" : "") +
                    "\nSalve a cena com Ctrl+S e pressione Play!", "OK");
        }

        /// <summary>
        /// Ponto de entrada para Unity batch mode.
        /// Uso: Unity -batchmode -projectPath ... -executeMethod MuseumModerna.MuseumCompleteBuilder.RunBatch -quit
        /// </summary>
        public static void RunBatch()
        {
            Debug.Log("[MuseumModerna] RunBatch iniciado.");

            // Garante que a pasta Scenes existe
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // Cria cena nova limpa
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Executa as 3 etapas
            var builder = ScriptableObject.CreateInstance<MuseumCompleteBuilder>();
            // _s1/_s2/_s3 já são true por padrão
            builder.Run();

            // Salva a cena
            const string scenePath = "Assets/Scenes/MuseumScene.unity";
            bool saved = EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[MuseumModerna] Cena salva em {scenePath}: {saved}");

            // Registra no Build Settings
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DestroyImmediate(builder);
            Debug.Log("[MuseumModerna] RunBatch concluído com sucesso!");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ETAPA 1 — GEOMETRIA
        // ══════════════════════════════════════════════════════════════════════
        void Step1_Geometry()
        {
            var old = GameObject.Find("Museum_Geometry");
            if (old != null) { Undo.DestroyObjectImmediate(old); }

            var root = new GameObject("Museum_Geometry");
            Undo.RegisterCreatedObjectUndo(root, "Create Museum Geometry");

            BuildRoom(NewChild(root.transform, "Foyer_Entrada"),
                C_FOYER, W_FOYER, D_FOYER, doorN: true);
            BuildRoom(NewChild(root.transform, "Hall_Central"),
                C_HALL, W_HALL, D_HALL, doorN: true, doorS: true, doorE: true, doorW: true);
            BuildRoom(NewChild(root.transform, "Ala_Pintura"),
                C_PINTURA, W_PIN, D_PIN, doorS: true);
            BuildRoom(NewChild(root.transform, "Ala_Escultura"),
                C_ESC, W_ESC, D_ESC, doorW: true);
            BuildRoom(NewChild(root.transform, "Ala_Literatura"),
                C_LIT, W_LIT, D_LIT, doorE: true);

            Debug.Log("[MuseumModerna] Etapa 1: 5 salas criadas.");
        }

        void BuildRoom(Transform p, Vector3 c, float w, float d,
            bool doorN = false, bool doorS = false,
            bool doorE = false, bool doorW = false)
        {
            float cx = c.x, cz = c.z;
            float hw = w * 0.5f, hd = d * 0.5f;

            // Piso
            MakeCube(p, "Piso",
                new Vector3(cx, -FT * 0.5f, cz),
                new Vector3(w + WT * 2f, FT, d + WT * 2f),
                MatFloor());

            // Teto laje base (coffered ceiling vem do MuseumCeilingBuilder em runtime)
            MakeCube(p, "Teto",
                new Vector3(cx, RH + FT * 0.5f, cz),
                new Vector3(w + WT * 2f, FT, d + WT * 2f),
                MatCeiling());

            // Paredes
            WallNS(p, "Parede_Norte", cx, cz + hd, w, doorN);
            WallNS(p, "Parede_Sul",   cx, cz - hd, w, doorS);
            WallEW(p, "Parede_Leste", cx + hw, cz,  d, doorE);
            WallEW(p, "Parede_Oeste", cx - hw, cz,  d, doorW);

            // Rodapés
            AddBaseboards(p, cx, cz, w, d);
        }

        void WallNS(Transform p, string name, float cx, float wz, float rw, bool door)
        {
            if (!door)
            {
                MakeCube(p, name, new Vector3(cx, RH * 0.5f, wz), new Vector3(rw, RH, WT), MatWall());
                return;
            }
            float seg = rw * 0.5f - DW * 0.5f;
            if (seg > 0.05f)
            {
                MakeCube(p, name + "_E",
                    new Vector3(cx - DW * 0.5f - seg * 0.5f, RH * 0.5f, wz),
                    new Vector3(seg, RH, WT), MatWall());
                MakeCube(p, name + "_D",
                    new Vector3(cx + DW * 0.5f + seg * 0.5f, RH * 0.5f, wz),
                    new Vector3(seg, RH, WT), MatWall());
            }
            float hdr = RH - DH;
            if (hdr > 0.05f)
                MakeCube(p, name + "_Hdr",
                    new Vector3(cx, DH + hdr * 0.5f, wz),
                    new Vector3(DW, hdr, WT), MatWall());
        }

        void WallEW(Transform p, string name, float wx, float cz, float rd, bool door)
        {
            if (!door)
            {
                MakeCube(p, name, new Vector3(wx, RH * 0.5f, cz), new Vector3(WT, RH, rd), MatWall());
                return;
            }
            float seg = rd * 0.5f - DW * 0.5f;
            if (seg > 0.05f)
            {
                MakeCube(p, name + "_N",
                    new Vector3(wx, RH * 0.5f, cz + DW * 0.5f + seg * 0.5f),
                    new Vector3(WT, RH, seg), MatWall());
                MakeCube(p, name + "_S",
                    new Vector3(wx, RH * 0.5f, cz - DW * 0.5f - seg * 0.5f),
                    new Vector3(WT, RH, seg), MatWall());
            }
            float hdr = RH - DH;
            if (hdr > 0.05f)
                MakeCube(p, name + "_Hdr",
                    new Vector3(wx, DH + hdr * 0.5f, cz),
                    new Vector3(WT, hdr, DW), MatWall());
        }

        void AddBaseboards(Transform p, float cx, float cz, float rw, float rd)
        {
            const float bh = 0.15f, bt = 0.03f;
            var m = MatBaseboard();
            float hw = rw * 0.5f - bt, hd = rd * 0.5f - bt;
            MakeCube(p, "Rodape_N", new Vector3(cx, bh * 0.5f, cz + hd),  new Vector3(rw, bh, bt), m);
            MakeCube(p, "Rodape_S", new Vector3(cx, bh * 0.5f, cz - hd),  new Vector3(rw, bh, bt), m);
            MakeCube(p, "Rodape_L", new Vector3(cx + hw, bh * 0.5f, cz),  new Vector3(bt, bh, rd), m);
            MakeCube(p, "Rodape_O", new Vector3(cx - hw, bh * 0.5f, cz),  new Vector3(bt, bh, rd), m);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ETAPA 2 — OBRAS DE ARTE
        // ══════════════════════════════════════════════════════════════════════
        void Step2_Exhibits()
        {
            const string folder = "Assets/Resources/PaintingData";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "PaintingData");

            var templates = AllTemplates();
            foreach (var t in templates) SaveAsset(folder, t);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var old = GameObject.Find("Museum_Exhibits");
            if (old != null) { Undo.DestroyObjectImmediate(old); }

            var root = new GameObject("Museum_Exhibits");
            Undo.RegisterCreatedObjectUndo(root, "Create Museum Exhibits");

            PlaceExhibits(root.transform, folder, templates);
            PlacePedestals(root.transform);

            Debug.Log($"[MuseumModerna] Etapa 2: {templates.Count} obras posicionadas.");
        }

        // ─── Template de obra ──────────────────────────────────────────────────
        struct ExhibitTpl
        {
            public string id, title, artist, movement, description, funFact;
            public int    year;
            public Color  color;
        }

        List<ExhibitTpl> AllTemplates()
        {
            var templates = new List<ExhibitTpl>();
            foreach (var entry in CuratorialCatalog.Read())
                templates.Add(new ExhibitTpl {
                    id = entry.uniqueId, title = entry.title, artist = entry.artist,
                    year = entry.year, movement = entry.movement,
                    description = entry.description, funFact = entry.funFact,
                    color = new Color(.86f, .79f, .65f)
                });
            return templates;
        }

        void SaveAsset(string folder, ExhibitTpl t)
        {
            string path = $"{folder}/{t.id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<PaintingInfo>(path);
            bool isNew = asset == null;
            if (isNew) asset = ScriptableObject.CreateInstance<PaintingInfo>();

            asset.title       = t.title;
            asset.artist      = t.artist;
            asset.year        = t.year;
            asset.movement    = t.movement;
            asset.description = t.description;
            asset.funFact     = t.funFact;
            asset.uniqueId    = t.id;

            CuratorialCatalog.Apply(asset);
            if (isNew) AssetDatabase.CreateAsset(asset, path);
            else       EditorUtility.SetDirty(asset);
        }

        // ─── Posicionamento das obras ─────────────────────────────────────────
        void PlaceExhibits(Transform root, string folder, List<ExhibitTpl> templates)
        {
            int layer = EnsureLayer("Painting");

            // Posição da face interna de cada parede:
            //   NWallZ: parede norte  (pintura fica em Z menor)
            //   SWallZ: parede sul    (pintura fica em Z maior)
            //   EWallX: parede leste  (pintura fica em X menor)
            //   WWallX: parede oeste  (pintura fica em X maior)
            float NWallZ(float cz, float d) => cz + d * 0.5f - WT * 0.5f - 0.02f;
            float SWallZ(float cz, float d) => cz - d * 0.5f + WT * 0.5f + 0.02f;
            float EWallX(float cx, float w) => cx + w * 0.5f - WT * 0.5f - 0.02f;
            float WWallX(float cx, float w) => cx - w * 0.5f + WT * 0.5f + 0.02f;

            // Rotações: a pintura "olha" para o interior da sala
            var faceS = Quaternion.Euler(0, 180,   0);  // na parede norte, olha para sul
            var faceN = Quaternion.identity;             // na parede sul,   olha para norte
            var faceW = Quaternion.Euler(0, -90,   0);  // na parede leste, olha para oeste
            var faceE = Quaternion.Euler(0,  90,   0);  // na parede oeste, olha para leste

            // ── FOYER ─────────────────────────────────────────────────────────
            float ex = EWallX(C_FOYER.x, W_FOYER);
            float wx = WWallX(C_FOYER.x, W_FOYER);
            MakeExhibit(root, folder, "semana_1922",       layer, new Vector3(ex, PEH, -13f), faceW, templates);
            MakeExhibit(root, folder, "organizadores",     layer, new Vector3(ex, PEH, -17f), faceW, templates);
            MakeExhibit(root, folder, "villa_lobos_dancas",layer, new Vector3(wx, PEH, -13f), faceE, templates);
            MakeExhibit(root, folder, "villa_lobos_lenda", layer, new Vector3(wx, PEH, -17f), faceE, templates);

            // ── HALL — lados da porta sul ─────────────────────────────────────
            float hallSZ = SWallZ(C_HALL.z, D_HALL);
            MakeExhibit(root, folder, "teatro_municipal",   layer, new Vector3(-7f, PEH, hallSZ), faceN, templates);
            MakeExhibit(root, folder, "manifesto_pau_brasil", layer, new Vector3( 7f, PEH, hallSZ), faceN, templates);

            // ── ALA PINTURA ───────────────────────────────────────────────────
            float pNZ = NWallZ(C_PINTURA.z, D_PIN);
            float pEX = EWallX(C_PINTURA.x, W_PIN);
            float pWX = WWallX(C_PINTURA.x, W_PIN);
            MakeExhibit(root, folder, "homem_amarelo",        layer, new Vector3(-5.0f, PEH, pNZ), faceS, templates);
            MakeExhibit(root, folder, "estudante_russa",      layer, new Vector3(-1.5f, PEH, pNZ), faceS, templates);
            MakeExhibit(root, folder, "tropical",             layer, new Vector3( 2.0f, PEH, pNZ), faceS, templates);
            MakeExhibit(root, folder, "di_cavalcanti_pierrot",layer, new Vector3( 5.5f, PEH, pNZ), faceS, templates);
            MakeExhibit(root, folder, "lasar_segall",         layer, new Vector3(pEX, PEH, 13f),   faceW, templates);
            MakeExhibit(root, folder, "john_graz",            layer, new Vector3(pWX, PEH, 13f),   faceE, templates);

            // ── ALA ESCULTURA ─────────────────────────────────────────────────
            float eEX = EWallX(C_ESC.x, W_ESC);
            float eNZ = NWallZ(C_ESC.z, D_ESC);
            float eSZ = SWallZ(C_ESC.z, D_ESC);
            MakeExhibit(root, folder, "brecheret_cabeca_cristo", layer, new Vector3(eEX, PEH,  3f), faceW, templates);
            MakeExhibit(root, folder, "brecheret_eva",           layer, new Vector3(eEX, PEH, -1f), faceW, templates);
            MakeExhibit(root, folder, "brecheret_bandeiras",     layer, new Vector3(14f, PEH, eNZ), faceS, templates);
            MakeExhibit(root, folder, "brecheret_moema",         layer, new Vector3(20f, PEH, eSZ), faceN, templates);

            // ── ALA LITERATURA ────────────────────────────────────────────────
            float lWX = WWallX(C_LIT.x, W_LIT);
            float lNZ = NWallZ(C_LIT.z, D_LIT);
            MakeExhibit(root, folder, "mario_ode_burgues",     layer, new Vector3(lWX, PEH,  4f),  faceE, templates);
            MakeExhibit(root, folder, "mario_pauliceia",       layer, new Vector3(lWX, PEH,  0f),  faceE, templates);
            MakeExhibit(root, folder, "oswald_poemas",         layer, new Vector3(lWX, PEH, -4f),  faceE, templates);
            MakeExhibit(root, folder, "graca_aranha_discurso", layer, new Vector3(-14f, PEH, lNZ),  faceS, templates);
            MakeExhibit(root, folder, "ronald_carvalho",       layer, new Vector3(-20f, PEH, lNZ),  faceS, templates);
        }

        void MakeExhibit(Transform root, string folder, string id, int layer,
            Vector3 pos, Quaternion rot, List<ExhibitTpl> templates)
        {
            string assetPath = $"{folder}/{id}.asset";
            var info = AssetDatabase.LoadAssetAtPath<PaintingInfo>(assetPath);

            Color col = new Color(0.5f, 0.5f, 0.5f);
            foreach (var t in templates) if (t.id == id) { col = t.color; break; }

            // Backing (moldura escura, levemente maior que a tela)
            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = $"Moldura_{id}";
            Undo.RegisterCreatedObjectUndo(backing, "Create Frame");
            backing.transform.SetParent(root, false);
            backing.transform.SetPositionAndRotation(pos, rot);
            backing.transform.localScale = new Vector3(PW + 0.10f, PHT + 0.10f, 0.04f);
            backing.GetComponent<MeshRenderer>().sharedMaterial = MatMoldura();
            DestroyImmediate(backing.GetComponent<BoxCollider>());

            // Tela (superfície colorida com PaintingExhibit)
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = id;
            Undo.RegisterCreatedObjectUndo(surface, "Create Painting Surface");
            surface.transform.SetParent(root, false);

            // A tela está levemente à frente do backing (em direção ao interior da sala)
            // localOffset (0,0,1) na rotação do backing = "frente" da tela = dentro da sala
            Vector3 surfacePos = pos + rot * new Vector3(0f, 0f, 0.03f);
            surface.transform.SetPositionAndRotation(surfacePos, rot);
            surface.transform.localScale = new Vector3(PW, PHT, 0.03f);

            var mat = new Material(Shader.Find("Standard")) { color = col };
            mat.name = $"Pintura_{id}";
            mat.SetFloat("_Glossiness", 0.04f);
            mat.SetFloat("_Metallic",   0f);
            surface.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // BoxCollider permanece na tela para raycasting pelo GazeDwellInteraction
            surface.layer = layer;

            var exhibit = surface.AddComponent<PaintingExhibit>();
            if (info != null)
            {
                var so = new SerializedObject(exhibit);
                so.FindProperty("paintingData").objectReferenceValue = info;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[MuseumModerna] PaintingInfo não encontrado para id='{id}' em {assetPath}");
            }
        }

        void PlacePedestals(Transform root)
        {
            var pedParent = new GameObject("Pedestais_Escultura");
            Undo.RegisterCreatedObjectUndo(pedParent, "Create Pedestals");
            pedParent.transform.SetParent(root, false);

            MakePedestal(pedParent.transform, new Vector3(13f, 0,  4f), "Pedestal_Moema");
            MakePedestal(pedParent.transform, new Vector3(13f, 0,  0f), "Pedestal_Eva");
            MakePedestal(pedParent.transform, new Vector3(13f, 0, -4f), "Pedestal_Cristo");
        }

        void MakePedestal(Transform parent, Vector3 pos, string name)
        {
            var mat = new Material(Shader.Find("Standard"))
                { color = new Color(0.92f, 0.91f, 0.88f) };
            mat.SetFloat("_Glossiness", 0.35f);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            MakeCube(go.transform, "Base",   new Vector3(0, 0.06f,  0), new Vector3(0.46f, 0.12f, 0.46f), mat);
            MakeCube(go.transform, "Coluna", new Vector3(0, 0.62f,  0), new Vector3(0.28f, 0.96f, 0.28f), mat);
            MakeCube(go.transform, "Topo",   new Vector3(0, 1.12f,  0), new Vector3(0.46f, 0.08f, 0.46f), mat);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ETAPA 3 — CONFIGURAÇÃO DE CENA
        // ══════════════════════════════════════════════════════════════════════
        void Step3_Scene()
        {
            SetupLighting();
            SetupPlayer();
            SetupUI();        // antes dos managers para poder ser referenciada
            SetupManagers();
            Debug.Log("[MuseumModerna] Etapa 3: cena configurada.");
        }

        // ─── Iluminação ───────────────────────────────────────────────────────
        void SetupLighting()
        {
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.09f, 0.07f);
            RenderSettings.fog          = true;
            RenderSettings.fogMode      = FogMode.ExponentialSquared;
            RenderSettings.fogColor     = new Color(0.06f, 0.04f, 0.03f);
            RenderSettings.fogDensity   = 0.022f;

            // Atenua luzes direcionais existentes
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.intensity = 0.05f;

            // Spotlights sobre cada obra
            var lRoot = GetOrCreateGO("Museum_Lighting");
            foreach (var exhibit in FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None))
            {
                CreateSpotlight(lRoot.transform, exhibit.transform);
            }

            // Luzes de preenchimento suaves em cada sala
            CreateFillLight(lRoot.transform, new Vector3(   0f, 3.5f,    0f), "Fill_Hall");
            CreateFillLight(lRoot.transform, new Vector3(   0f, 3.5f,   17f), "Fill_Pintura");
            CreateFillLight(lRoot.transform, new Vector3(  17f, 3.5f,    0f), "Fill_Escultura");
            CreateFillLight(lRoot.transform, new Vector3( -17f, 3.5f,    0f), "Fill_Literatura");
            CreateFillLight(lRoot.transform, new Vector3(   0f, 3.5f,  -15f), "Fill_Foyer");
        }

        void CreateSpotlight(Transform parent, Transform painting)
        {
            string sName = $"Spot_{painting.gameObject.name}";
            var old = parent.Find(sName);
            if (old != null) DestroyImmediate(old.gameObject);

            var go = new GameObject(sName);
            Undo.RegisterCreatedObjectUndo(go, "Create Spotlight");
            go.transform.SetParent(parent, false);

            Vector3 pFwd = painting.forward;
            go.transform.position = painting.position + Vector3.up * 1.8f + pFwd * 1.0f;
            go.transform.LookAt(painting.position);

            var l = go.AddComponent<Light>();
            l.type        = LightType.Spot;
            l.color       = new Color(1f, 0.82f, 0.50f);
            l.intensity   = 2.5f;
            l.range       = 5f;
            l.spotAngle   = 38f;
            l.shadows     = LightShadows.Soft;
            l.shadowStrength = 0.6f;
        }

        void CreateFillLight(Transform parent, Vector3 pos, string name)
        {
            var old = parent.Find(name);
            if (old != null) DestroyImmediate(old.gameObject);

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Fill Light");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var l = go.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = new Color(1f, 0.90f, 0.75f);
            l.intensity = 0.55f;
            l.range     = 18f;
            l.shadows   = LightShadows.None;
        }

        // ─── Player ───────────────────────────────────────────────────────────
        void SetupPlayer()
        {
            var old = GameObject.Find("Player");
            if (old != null) { Undo.DestroyObjectImmediate(old); }

            var player = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            player.transform.position = new Vector3(0f, 0f, -13f); // entrada do foyer

            var cc = player.AddComponent<CharacterController>();
            cc.height     = 1.75f;
            cc.radius     = 0.30f;
            cc.center     = new Vector3(0f, 0.875f, 0f);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.30f;

            // Câmera (altura dos olhos)
            var camGO = new GameObject("PlayerCamera");
            camGO.transform.SetParent(player.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.60f, 0f);

            var cam = camGO.AddComponent<Camera>();
            cam.tag             = "MainCamera";
            cam.fieldOfView     = 80f;
            cam.nearClipPlane   = 0.01f;
            cam.farClipPlane    = 150f;
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.02f, 0.02f);

            camGO.AddComponent<AudioListener>();
            var gyro  = camGO.AddComponent<GyroscopeController>();
            camGO.AddComponent<GazeDwellInteraction>();

            // Scripts do player
            var hgm = player.AddComponent<HeadGazeMovement>();
            var pc  = player.AddComponent<PlayerController>();

            // Conecta referências via SerializedObject (campos privados [SerializeField])
            var soHGM = new SerializedObject(hgm);
            soHGM.FindProperty("cameraTransform").objectReferenceValue = camGO.transform;
            soHGM.FindProperty("gyroController").objectReferenceValue  = gyro;
            soHGM.ApplyModifiedPropertiesWithoutUndo();

            var soPC = new SerializedObject(pc);
            soPC.FindProperty("gyroController").objectReferenceValue   = gyro;
            soPC.FindProperty("headGazeMovement").objectReferenceValue = hgm;
            soPC.FindProperty("useGazeDwell").boolValue                = true;
            int paintLayer = LayerMask.NameToLayer("Painting");
            if (paintLayer >= 0)
                soPC.FindProperty("paintingLayer").intValue = 1 << paintLayer;
            soPC.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = player;
            Debug.Log("[MuseumModerna] Player criado em " + player.transform.position);
        }

        // ─── UI ───────────────────────────────────────────────────────────────
        void SetupUI()
        {
            var old = GameObject.Find("MuseumUI");
            if (old != null) { Undo.DestroyObjectImmediate(old); }

            var canvasGO = new GameObject("MuseumUI");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Museum UI");

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1080, 1920);
            scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight   = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Painel de informações ─────────────────────────────────────────
            var panelGO = CreateRectGO(canvasGO.transform, "PaintingPanel",
                new Vector2(0f, -370f), new Vector2(920f, 580f));
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0.04f, 0.02f, 0.01f, 0.92f);
            var cg = panelGO.AddComponent<CanvasGroup>();
            cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

            var titleTxt  = MakeText(panelGO.transform, "TitleText",
                new Vector2(0f, 232f), new Vector2(860f, 60f), 24, FontStyle.Bold,   "Título");
            var artistTxt = MakeText(panelGO.transform, "ArtistText",
                new Vector2(0f, 182f), new Vector2(860f, 40f), 18, FontStyle.Italic,  "Artista — Ano");
            var yearTxt   = MakeText(panelGO.transform, "YearText",
                new Vector2(0f, 145f), new Vector2(860f, 30f), 14, FontStyle.Normal,  "");
            var descTxt   = MakeText(panelGO.transform, "DescriptionText",
                new Vector2(0f,  10f), new Vector2(860f, 185f), 15, FontStyle.Normal, "Descrição...");

            var closeGO  = CreateRectGO(panelGO.transform, "CloseButton",
                new Vector2(400f, 258f), new Vector2(80f, 40f));
            closeGO.AddComponent<Image>().color = new Color(0.3f, 0.1f, 0.05f, 0.9f);
            var closeBtn = closeGO.AddComponent<Button>();
            MakeText(closeGO.transform, "Text", Vector2.zero, new Vector2(80f, 40f),
                22, FontStyle.Bold, "✕");

            // ── Crosshair ─────────────────────────────────────────────────────
            var crossGO = CreateRectGO(canvasGO.transform, "Crosshair",
                Vector2.zero, new Vector2(18f, 18f));
            var crossImg = crossGO.AddComponent<Image>();
            crossImg.color = new Color(1f, 1f, 1f, 0.85f);

            // ── Gaze dwell ring ───────────────────────────────────────────────
            var ringGO = CreateRectGO(canvasGO.transform, "GazeDwellRing",
                Vector2.zero, new Vector2(62f, 62f));
            var ringImg = ringGO.AddComponent<Image>();
            ringImg.color      = new Color(1f, 0.88f, 0.20f, 0.92f);
            ringImg.type       = Image.Type.Filled;
            ringImg.fillMethod = Image.FillMethod.Radial360;
            ringImg.fillAmount = 0f;
            ringGO.SetActive(false);

            // ── Botão calibrar ────────────────────────────────────────────────
            var calGO = CreateRectGO(canvasGO.transform, "CalibrateButton",
                new Vector2(0f, -870f), new Vector2(260f, 68f));
            calGO.AddComponent<Image>().color = new Color(0.14f, 0.10f, 0.06f, 0.9f);
            var calBtn = calGO.AddComponent<Button>();
            MakeText(calGO.transform, "Text", Vector2.zero, new Vector2(260f, 68f),
                17, FontStyle.Normal, "Calibrar Giroscópio");

            // ── UIManager ─────────────────────────────────────────────────────
            var uiMgr = canvasGO.AddComponent<UIManager>();
            var soUI  = new SerializedObject(uiMgr);
            soUI.FindProperty("paintingPanel").objectReferenceValue           = panelGO;
            soUI.FindProperty("paintingPanelCanvasGroup").objectReferenceValue = cg;
            soUI.FindProperty("titleText").objectReferenceValue               = titleTxt;
            soUI.FindProperty("artistText").objectReferenceValue              = artistTxt;
            soUI.FindProperty("yearText").objectReferenceValue                = yearTxt;
            soUI.FindProperty("descriptionText").objectReferenceValue         = descTxt;
            soUI.FindProperty("crosshair").objectReferenceValue               = crossGO;
            soUI.FindProperty("crosshairImage").objectReferenceValue          = crossImg;
            soUI.FindProperty("gazeDwellRing").objectReferenceValue           = ringImg;
            soUI.FindProperty("closeButton").objectReferenceValue             = closeBtn;
            soUI.FindProperty("calibrateButton").objectReferenceValue         = calBtn;

            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                soUI.FindProperty("playerController").objectReferenceValue = pc;

            soUI.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[MuseumModerna] UI configurada.");
        }

        // ─── Managers ─────────────────────────────────────────────────────────
        void SetupManagers()
        {
            var pc  = FindAnyObjectByType<PlayerController>();
            var ui  = FindAnyObjectByType<UIManager>();

            // ExhibitManager
            var emGO = GetOrCreateGO("ExhibitManager");
            if (emGO.GetComponent<ExhibitManager>() == null)
                emGO.AddComponent<ExhibitManager>();
            var em   = emGO.GetComponent<ExhibitManager>();
            var soEM = new SerializedObject(em);
            if (pc != null) soEM.FindProperty("playerController").objectReferenceValue = pc;
            if (ui != null) soEM.FindProperty("uiManager").objectReferenceValue        = ui;
            soEM.ApplyModifiedPropertiesWithoutUndo();

            // GameManager
            var gmGO = GetOrCreateGO("GameManager");
            if (gmGO.GetComponent<GameManager>() == null)
                gmGO.AddComponent<GameManager>();
            var gm   = gmGO.GetComponent<GameManager>();
            var soGM = new SerializedObject(gm);
            if (pc != null) soGM.FindProperty("playerController").objectReferenceValue  = pc;
            if (em != null) soGM.FindProperty("exhibitManager").objectReferenceValue    = em;
            if (ui != null) soGM.FindProperty("uiManager").objectReferenceValue         = ui;
            soGM.ApplyModifiedPropertiesWithoutUndo();

            // AmbientAudioManager
            var aaGO = GetOrCreateGO("AmbientAudioManager");
            if (aaGO.GetComponent<AmbientAudioManager>() == null)
                aaGO.AddComponent<AmbientAudioManager>();

            // MuseumCeilingBuilder (para AutoBootstrap não criar duplicata em runtime)
            var ceilGO = GetOrCreateGO("MuseumCeilingBuilder");
            if (ceilGO.GetComponent<MuseumCeilingBuilder>() == null)
            {
                var cb = ceilGO.AddComponent<MuseumCeilingBuilder>();
                var soCB = new SerializedObject(cb);
                soCB.FindProperty("roomWidth").floatValue    = W_HALL;
                soCB.FindProperty("roomDepth").floatValue    = D_HALL;
                soCB.FindProperty("ceilingHeight").floatValue = RH;
                soCB.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("[MuseumModerna] Managers configurados.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS — Geometria
        // ══════════════════════════════════════════════════════════════════════
        static GameObject MakeCube(Transform parent, string name,
            Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // Mantém BoxCollider para colisão do CharacterController com paredes
            return go;
        }

        static Transform NewChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject GetOrCreateGO(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            }
            return go;
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS — UI
        // ══════════════════════════════════════════════════════════════════════
        static GameObject CreateRectGO(Transform parent, string name,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            return go;
        }

        static Text MakeText(Transform parent, string name,
            Vector2 pos, Vector2 size, int fontSize, FontStyle style, string placeholder)
        {
            var go = CreateRectGO(parent, name, pos, size);
            var txt = go.AddComponent<Text>();
            txt.text      = placeholder;
            txt.fontSize  = fontSize;
            txt.fontStyle = style;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.UpperCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return txt;
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS — Materiais
        // ══════════════════════════════════════════════════════════════════════
        static Material MatWall()     => s_wall      ??= NewMat("Museo_Wall",     new Color(0.91f, 0.89f, 0.85f), 0f,   0.28f);
        static Material MatFloor()    => s_floor     ??= NewMat("Museo_Floor",    new Color(0.22f, 0.13f, 0.07f), 0f,   0.48f);
        static Material MatCeiling()  => s_ceiling   ??= NewMat("Museo_Ceiling",  new Color(0.96f, 0.94f, 0.90f), 0f,   0.18f);
        static Material MatBaseboard()=> s_baseboard ??= NewMat("Museo_Baseboard",new Color(0.83f, 0.80f, 0.74f), 0f,   0.38f);
        static Material MatMoldura()  => s_moldura   ??= NewMat("Museo_Frame",    new Color(0.26f, 0.18f, 0.06f), 0.3f, 0.52f);

        static Material NewMat(string matName, Color color, float metallic, float gloss)
        {
            var m = new Material(Shader.Find("Standard")) { name = matName, color = color };
            m.SetFloat("_Metallic",   metallic);
            m.SetFloat("_Glossiness", gloss);
            return m;
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS — Layers
        // ══════════════════════════════════════════════════════════════════════
        static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogError("[MuseumModerna] TagManager.asset não encontrado.");
                return 0;
            }

            var tagManager = new SerializedObject(tagManagerAssets[0]);
            var layersProp = tagManager.FindProperty("layers");

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                var prop = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(prop.stringValue))
                {
                    prop.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[MuseumModerna] Layer '{layerName}' criada no slot {i}.");
                    return i;
                }
            }

            Debug.LogError("[MuseumModerna] Nenhum slot disponível para a layer 'Painting'.");
            return 0;
        }
    }
}
