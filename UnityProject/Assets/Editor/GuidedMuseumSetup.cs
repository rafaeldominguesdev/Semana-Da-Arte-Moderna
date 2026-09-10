using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Melhoria aditiva da cena existente; não reconstrói arquitetura nem altera o Blender.</summary>
    public static class GuidedMuseumSetup
    {
        private const string ScenePath = "Assets/Scenes/MuseumScene.unity";
        private const string RootName = "Museum_GuidedExhibition";
        private static Material bronze, charcoal, paper, red, blue;
        private static Transform root;

        [MenuItem("MuseumModerna/Visita guiada/Aplicar à cena do museu")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            CuratorialCatalog.Install();
            var old = GameObject.Find(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            root = new GameObject(RootName).transform;
            bronze = Material("Guide_Bronze", new Color(.40f, .26f, .105f), .65f, .38f);
            charcoal = Material("Guide_Petroleo", new Color(.045f, .095f, .10f), .08f, .30f);
            paper = Material("Guide_Papel", new Color(.86f, .79f, .65f), 0, .12f);
            red = Material("Guide_Terracota", new Color(.57f, .12f, .065f), 0, .18f);
            blue = Material("Guide_Azul", new Color(.11f, .25f, .30f), 0, .18f);

            var exhibits = UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            Array.Sort(exhibits, (a, b) => string.CompareOrdinal(a.PaintingData?.uniqueId, b.PaintingData?.uniqueId));
            int index = 0;
            foreach (var exhibit in exhibits)
            {
                if (exhibit.PaintingData == null) continue;
                exhibit.name = "OBRA_" + exhibit.PaintingData.uniqueId.ToUpperInvariant();
                var oldFrame = exhibit.GetComponent<PaintingFrame>();
                if (oldFrame != null) oldFrame.enabled = false;
                foreach (Transform child in exhibit.transform)
                    if (child.name.StartsWith("Frame_")) child.gameObject.SetActive(false);
                DressPanel(exhibit, index++);
            }
            // Três expositores ausentes no percurso anterior, em espaços livres da ala de pintura.
            AddPanel("vicente_rego_monteiro", new Vector3(7.77f, 1.7f, 18), Quaternion.Euler(0, -90, 0), index++);
            AddPanel("zina_aita", new Vector3(-7.77f, 1.7f, 18), Quaternion.Euler(0, 90, 0), index++);
            AddPanel("ferrignac", new Vector3(-7.77f, 1.7f, 21.4f), Quaternion.Euler(0, 90, 0), index++);
            for (int i = 0; i < 3; i++) Sculpture(i);
            Vitrine();
            Sign(new Vector3(-4.8f, 2.75f, 9.65f), Quaternion.Euler(0, 180, 0), "01 / PINTURA\nCOR, FIGURA E RUPTURA");
            Sign(new Vector3(9.65f, 2.75f, 4.8f), Quaternion.Euler(0, -90, 0), "02 / ESCULTURA\nFORMA E ESPAÇO");
            Sign(new Vector3(-9.65f, 2.75f, -4.8f), Quaternion.Euler(0, 90, 0), "03 / LITERATURA\nA PALAVRA EM CENA");
            Sign(new Vector3(4.8f, 2.7f, -9.65f), Quaternion.identity, "1922 / MODERNISMOS\nUM MUSEU VIRTUAL");
            var gaze = UnityEngine.Object.FindAnyObjectByType<GazeDwellInteraction>();
            if (gaze == null) throw new InvalidOperationException("A câmera do visitante não possui GazeDwellInteraction.");
            var so = new SerializedObject(gaze);
            so.FindProperty("dwellTime").floatValue = .12f;
            so.FindProperty("autoCloseTimeout").floatValue = .15f;
            so.ApplyModifiedPropertiesWithoutUndo();
            var visitorCamera = gaze.GetComponent<Camera>();
            if (visitorCamera == null) throw new InvalidOperationException("Câmera do visitante ausente.");
            visitorCamera.tag = "MainCamera";
            // A câmera original do template também estava ativa e podia competir com a do visitante.
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (camera != visitorCamera)
                {
                    camera.enabled = false;
                    if (camera.CompareTag("MainCamera")) camera.tag = "Untagged";
                    var listener = camera.GetComponent<AudioListener>();
                    if (listener != null) listener.enabled = false;
                }
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var legacyPanel = GameObject.Find("PaintingPanel");
            if (legacyPanel != null) legacyPanel.SetActive(false);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.25f, .23f, .20f);
            RenderSettings.fog = false;
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) light.intensity = .45f;
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar cena.");
            Debug.Log("[GuidedMuseum] Cena salva com fichas, cenografia, esculturas, sinalização e vitrine.");
        }
        private static PaintingInfo Info(string id) => Resources.Load<PaintingInfo>("PaintingData/" + id);
        private static void AddPanel(string id, Vector3 position, Quaternion rotation, int index)
        {
            var go = Cube(root, "OBRA_" + id.ToUpperInvariant(), position, new Vector3(1.4f, 1.1f, .03f), paper, true);
            go.transform.rotation = rotation;
            go.layer = LayerMask.NameToLayer("Painting");
            var exhibit = go.AddComponent<PaintingExhibit>();
            exhibit.SetData(Info(id));
            DressPanel(exhibit, index);
        }
        private static void DressPanel(PaintingExhibit exhibit, int index)
        {
            var frame = new GameObject("Expografia_" + exhibit.PaintingData.uniqueId).transform;
            frame.SetParent(root, false);
            frame.SetPositionAndRotation(exhibit.transform.position, exhibit.transform.rotation);
            // As telas existentes são cubos; escala local fornece largura mesmo em paredes laterais.
            float w = Mathf.Abs(exhibit.transform.lossyScale.x), h = Mathf.Abs(exhibit.transform.lossyScale.y);
            if (w < .1f || h < .1f) { w = 1.4f; h = 1.1f; }
            exhibit.GetComponent<Renderer>().sharedMaterial = paper;
            Cube(frame, "Moldura_superior", new Vector3(0, h / 2 + .04f, .035f), new Vector3(w + .16f, .08f, .12f), bronze);
            Cube(frame, "Moldura_inferior", new Vector3(0, -h / 2 - .04f, .035f), new Vector3(w + .16f, .08f, .12f), bronze);
            Cube(frame, "Moldura_esquerda", new Vector3(-w / 2 - .04f, 0, .035f), new Vector3(.08f, h, .12f), charcoal);
            Cube(frame, "Moldura_direita", new Vector3(w / 2 + .04f, 0, .035f), new Vector3(.08f, h, .12f), charcoal);
            // Composição original em relevo; nunca apresentada como reprodução histórica.
            var art = new GameObject("Composicao_cenografica").transform;
            art.SetParent(frame, false);
            art.localPosition = new Vector3(0, 0, .022f);
            Cube(art, "Plano", new Vector3(-w * .17f, 0, .008f), new Vector3(w * .38f, h * .74f, .012f), index % 2 == 0 ? red : blue);
            var oval = Primitive(art, "Volume", PrimitiveType.Sphere, new Vector3(w * (.08f + (index % 3) * .07f), h * (.05f + (index % 4) * .04f), .013f), new Vector3(w * (.25f + (index % 3) * .055f), h * (.42f + (index % 4) * .045f), .012f), charcoal);
            oval.transform.localRotation = Quaternion.Euler(0, 0, index % 2 == 0 ? 18 : -18);
            Cube(art, "Linha", new Vector3(w * .13f, -h * .28f, .025f), new Vector3(w * .46f, .022f, .014f), bronze);
            WorldText(art, exhibit.PaintingData.year.ToString(), new Vector3(-w * .28f, h * .31f, .04f), .065f, MuseumGuideTheme.Text);
            Cube(frame, "Placa", new Vector3(0, -h / 2 - .28f, .022f), new Vector3(w, .30f, .045f), charcoal);
            WorldText(frame, exhibit.PaintingData.title + "\n" + exhibit.PaintingData.artist + " | " + exhibit.PaintingData.Periodo,
                new Vector3(0, -h / 2 - .22f, .05f), .026f, MuseumGuideTheme.Text);
            WorldText(frame, "PAINEL DIDÁTICO / COMPOSIÇÃO CENOGRÁFICA",
                new Vector3(0, -h / 2 - .36f, .051f), .021f, MuseumGuideTheme.Accent);
            Cube(frame, "Luminaria", new Vector3(0, h / 2 + .28f, .28f), new Vector3(.36f, .045f, .13f), charcoal);
            var existingSpot = GameObject.Find("Spotlight_" + exhibit.PaintingData.uniqueId);
            if (existingSpot != null)
            {
                var light = existingSpot.GetComponent<Light>();
                if (light != null)
                {
                    light.lightmapBakeType = LightmapBakeType.Realtime;
                    light.shadows = LightShadows.None;
                    light.intensity = 1.35f;
                }
            }
            else Spot(frame, new Vector3(0, h / 2 + .6f, .7f), Vector3.zero);
        }
        private static void Sculpture(int i)
        {
            var pedestal = GameObject.Find(new[] { "Pedestal_Moema", "Pedestal_Eva", "Pedestal_Cristo" }[i]);
            Vector3 origin = pedestal != null ? pedestal.transform.position : new Vector3(13, 0, 4 - i * 4);
            var go = new GameObject("ESCULTURA_INSPIRADA_BRECHERET_" + (i + 1));
            go.transform.SetParent(root, false); go.transform.position = origin + Vector3.up * 1.17f;
            var exhibit = go.AddComponent<PaintingExhibit>(); exhibit.SetData(Info("escultura_cenografica_" + (i + 1)));
            Primitive(go.transform, "Massa_bronze", PrimitiveType.Sphere, new Vector3(0, .30f, 0), new Vector3(.34f, .62f, .27f), bronze, true);
            var second = Primitive(go.transform, "Plano_bronze", PrimitiveType.Cube, new Vector3(.05f, .53f, 0), new Vector3(.16f, .55f, .20f), bronze, true);
            second.transform.localRotation = Quaternion.Euler(12 + i * 12, 0, 25 - i * 20);
            Primitive(go.transform, "Coroamento", PrimitiveType.Sphere, new Vector3(.02f, .84f, 0), new Vector3(.21f, .23f, .19f), bronze, true);
            var label = new GameObject("Identificacao").transform; label.SetParent(root, false);
            label.position = origin + new Vector3(-.3f, .8f, 0); label.rotation = Quaternion.Euler(0, -90, 0);
            Cube(label, "Placa", Vector3.zero, new Vector3(.65f, .24f, .025f), charcoal);
            WorldText(label, "ESTUDO DE VOLUMES " + (i + 1) + "\nCriação do projeto / Cenografia", new Vector3(0, .025f, .02f), .025f, MuseumGuideTheme.Text);
            Spot(go.transform, new Vector3(-1, 1.8f, 1), new Vector3(0, .45f, 0));
        }
        private static void Vitrine()
        {
            var go = new GameObject("VITRINE_DOCUMENTOS_CENOGRAFICOS");
            go.transform.SetParent(root, false); go.transform.position = new Vector3(-17, 0, 1.5f);
            go.AddComponent<PaintingExhibit>().SetData(Info("vitrine_documentos"));
            Cube(go.transform, "Base", new Vector3(0, .48f, 0), new Vector3(1.5f, .96f, .78f), charcoal, true);
            Cube(go.transform, "Mesa", new Vector3(0, .99f, 0), new Vector3(1.58f, .06f, .86f), bronze, true);
            for (int i = 0; i < 3; i++)
            {
                var book = Cube(go.transform, "Impresso_cenografico_" + i, new Vector3(-.47f + i * .46f, 1.05f, 0), new Vector3(.35f, .045f, .46f), paper, true);
                book.transform.localRotation = Quaternion.Euler(0, -12 + i * 12, 0);
            }
            // Vidro transparente com bordas finas; seu collider pertence à mesma ficha documental.
            var glass = Material("Guide_Vidro", new Color(.65f, .8f, .82f, .10f), 0, .65f);
            glass.SetFloat("_Mode", 3); glass.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); glass.SetInt("_ZWrite", 0);
            glass.EnableKeyword("_ALPHAPREMULTIPLY_ON"); glass.renderQueue = 3000;
            Cube(go.transform, "Tampa_vidro", new Vector3(0, 1.30f, 0), new Vector3(1.54f, .015f, .82f), glass, true);
            Cube(go.transform, "Vidro_frontal", new Vector3(0, 1.15f, -.40f), new Vector3(1.54f, .30f, .015f), glass, true);
            Cube(go.transform, "Vidro_traseiro", new Vector3(0, 1.15f, .40f), new Vector3(1.54f, .30f, .015f), glass, true);
            for (int i = -1; i <= 1; i += 2)
                Cube(go.transform, "Vidro_lateral", new Vector3(i * .765f, 1.15f, 0), new Vector3(.015f, .30f, .82f), glass, true);
            WorldText(go.transform, "DOCUMENTOS DE UMA SEMANA\nObjetos didáticos / Cenografia", new Vector3(0, .79f, .40f), .036f, MuseumGuideTheme.Text);
        }
        private static void Sign(Vector3 position, Quaternion rotation, string text)
        {
            var sign = new GameObject("Sinalizacao_" + text.Split('\n')[0]).transform;
            sign.SetParent(root, false); sign.SetPositionAndRotation(position, rotation);
            Cube(sign, "Fundo", Vector3.zero, new Vector3(2.5f, .64f, .06f), charcoal);
            Cube(sign, "Filete", new Vector3(0, -.28f, .04f), new Vector3(2.3f, .012f, .015f), bronze);
            WorldText(sign, text, new Vector3(0, .065f, .042f), .070f, MuseumGuideTheme.Text);
        }
        private static void Spot(Transform parent, Vector3 position, Vector3 target)
        {
            var go = new GameObject("Luz_de_acento"); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.LookAt(parent.TransformPoint(target));
            var light = go.AddComponent<Light>(); light.type = LightType.Spot; light.range = 4;
            light.spotAngle = 54; light.intensity = 1.1f; light.color = new Color(1, .88f, .70f); light.shadows = LightShadows.None;
        }
        private static void WorldText(Transform parent, string value, Vector3 position, float height, Color color)
        {
            var go = new GameObject("Legenda"); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var text = go.AddComponent<TextMesh>(); text.text = value; text.fontSize = 64;
            text.characterSize = height * .25f; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.color = color;
        }
        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider = false)
            => Primitive(parent, name, PrimitiveType.Cube, position, scale, material, collider);
        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static Material Material(string name, Color color, float metallic, float smooth)
        {
            string path = "Assets/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = color;
            material.SetColor("_EmissionColor", color * .16f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.EnableKeyword("_EMISSION");
            material.SetFloat("_Metallic", metallic); material.SetFloat("_Glossiness", smooth);
            EditorUtility.SetDirty(material); return material;
        }
    }
}
