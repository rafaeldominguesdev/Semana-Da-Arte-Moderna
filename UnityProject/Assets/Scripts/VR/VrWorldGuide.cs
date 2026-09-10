using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MuseumModerna
{
    /// <summary>Ficha binocular ancorada no mundo: a cabeça pode alcançar os controles pelo olhar.</summary>
    [DefaultExecutionOrder(75)]
    public sealed class VrWorldGuide : MonoBehaviour
    {
        public bool IsEnabled { get; private set; }
        public bool ConsumesGaze { get; private set; }
        public bool MenuOpen { get; private set; }
        public RectTransform Board { get; private set; }
        public int PageIndex => page;
        public int PageCount => pages.Count;
        public float DwellProgress => activation.Progress;
        private Camera eye;
        private MuseumGuidePanel preferences;
        private MobileVrMode mode;
        private CanvasGroup group;
        private Image background, ring;
        private RectTransform reticle;
        private Text heading, title, body, pageLabel;
        private Button previous, next;
        private readonly Button[] actions = new Button[3];
        private readonly List<string> pages = new List<string>();
        private readonly GazeActivation activation = new GazeActivation();
        private PaintingInfo current;
        private GazeDwellInteraction gaze;
        private Material overlayMaterial;
        private Texture2D ringTexture;
        private Sprite ringSprite;
        private bool visible, lookupArmed = true;
        private float lookupTimer;
        private int page, menuPage;
        private const float DwellSeconds = 1.0f;
        private const float Distance = 2.2f, Scale = .00155f;

        public void Build(Camera camera, MuseumGuidePanel settings, MobileVrMode owner)
        {
            eye = camera; preferences = settings; mode = owner;
            gaze = eye.GetComponent<GazeDwellInteraction>();
            var shader = Resources.Load<Shader>("Shaders/VrGuideUI");
            if (shader == null) shader = Shader.Find("Museum/VR Guide UI");
            overlayMaterial = new Material(shader);
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = eye; canvas.sortingOrder = 100;
            Board = gameObject.GetComponent<RectTransform>();
            Board.sizeDelta = new Vector2(780, 700); Board.localScale = Vector3.one * Scale;
            background = gameObject.AddComponent<Image>(); background.color = MuseumGuideTheme.Surface;
            background.sprite = preferences.RoundedSurface; background.type = Image.Type.Sliced;
            var border = gameObject.AddComponent<Outline>();
            border.effectColor = MuseumGuideTheme.Accent; border.effectDistance = new Vector2(1.3f, -1.3f);
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false;
            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 26, 22, 22); layout.spacing = 8;
            layout.childForceExpandHeight = false;
            heading = MakeText(Board, "", 22, 30);
            heading.color = MuseumGuideTheme.Accent;
            title = MakeText(Board, "", 34, 88); title.fontStyle = FontStyle.Bold;
            body = MakeText(Board, "", 28, 310);
            pageLabel = MakeText(Board, "", 22, 30);
            var pagesRow = Row(Board, "Paginas");
            previous = MakeButton(pagesRow, "Anterior", () => ChangePage(-1));
            next = MakeButton(pagesRow, "Próxima", () => ChangePage(1));
            var actionsRow = Row(Board, "Acoes");
            for (int i = 0; i < actions.Length; i++)
            {
                int index = i;
                actions[i] = MakeButton(actionsRow, "", () => InvokeAction(index));
            }
            MakeText(Board, "Olhe para um botão por 1 segundo para acionar.", 22, 32);
            foreach (var button in GetComponentsInChildren<Button>())
            {
                var surface = button.GetComponent<Image>();
                surface.sprite = preferences.RoundedSurface; surface.type = Image.Type.Sliced;
            }
            foreach (var graphic in GetComponentsInChildren<Graphic>()) graphic.material = overlayMaterial;
            // A mira pertence à cabeça; o painel permanece no lugar quando a cabeça se move.
            var reticleGO = new GameObject("MiraVR", typeof(RectTransform), typeof(Canvas));
            reticle = (RectTransform)reticleGO.transform;
            reticle.SetParent(eye.transform, false); reticle.localPosition = Vector3.forward * 1.5f;
            reticle.localScale = Vector3.one * .0006f; reticle.sizeDelta = new Vector2(32, 32);
            reticleGO.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            reticleGO.GetComponent<Canvas>().sortingOrder = 200;
            ringSprite = MakeRing();
            ring = reticleGO.AddComponent<Image>(); ring.material = overlayMaterial; ring.sprite = ringSprite;
            ring.color = MuseumGuideTheme.Accent; ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360;
            var dot = new GameObject("Ponto", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(reticle, false); ((RectTransform)dot.transform).sizeDelta = new Vector2(5, 5);
            dot.GetComponent<Image>().color = MuseumGuideTheme.Text; dot.GetComponent<Image>().material = overlayMaterial;
            SetVrEnabled(false);
        }
        public void SetVrEnabled(bool enabled)
        {
            IsEnabled = enabled; visible = false; MenuOpen = false; ConsumesGaze = false;
            current = null; group.alpha = 0; activation.Reset();
            reticle.gameObject.SetActive(enabled);
            gameObject.SetActive(enabled);
        }
        public void Show(PaintingInfo info)
        {
            if (!IsEnabled || info == null || !preferences.PanelsEnabled) return;
            if (MenuOpen) return;
            bool changed = current != info || !visible;
            current = info; visible = true;
            if (changed) { page = 0; PlaceBoard(false); RebuildPages(); }
            RenderPage();
        }
        public void Hide()
        {
            if (MenuOpen) return;
            visible = false; ConsumesGaze = false; current = null;
        }
        public void OpenMenu()
        {
            if (!IsEnabled) return;
            MenuOpen = true; visible = true; menuPage = 0;
            PlaceBoard(true); RenderMenu();
        }
        public void CloseMenu()
        {
            MenuOpen = false; ConsumesGaze = false;
            current = gaze.Focus.Current;
            if (current != null && preferences.PanelsEnabled)
            {
                page = 0; RebuildPages(); PlaceBoard(false); RenderPage();
            }
            else visible = false;
        }
        private void PlaceBoard(bool centered)
        {
            var forward = Vector3.ProjectOnPlane(eye.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            var rotation = Quaternion.LookRotation(forward);
            Board.SetPositionAndRotation(eye.transform.position + rotation * new Vector3(centered ? 0 : .91f, -.10f, Distance), rotation);
            activation.Reset();
        }
        private void RebuildPages()
        {
            pages.Clear();
            body.fontSize = 28 + preferences.TextStep * 3;
            var description = preferences.Summary ? current.resumo : current.description;
            string text = current.artist + " — " + current.Periodo + "\n" + current.tecnica + "\n\n" +
                current.obra_historica_ou_reinterpretacao + "\n\n" + description + "\n\nRELAÇÃO COM 1922\n" +
                current.relacao_com_a_semana_de_1922;
            if (!preferences.Summary) text += "\n\nFONTES\n" + current.fontes;
            Canvas.ForceUpdateCanvases();
            Paginate(text, body, pages);
            page = Mathf.Clamp(page, 0, pages.Count - 1);
        }
        public static void Paginate(string text, Text target, List<string> output)
        {
            output.Clear();
            var generator = new TextGenerator();
            var settings = target.GetGenerationSettings(new Vector2(target.rectTransform.rect.width, 0));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            int start = 0;
            while (start < text.Length)
            {
                int end = start + 1, fit = end;
                while (end <= text.Length)
                {
                    float height = generator.GetPreferredHeight(text.Substring(start, end - start), settings) / target.pixelsPerUnit;
                    if (height > target.rectTransform.rect.height - 4) break;
                    fit = end; end++;
                }
                if (fit < text.Length)
                {
                    int boundary = text.LastIndexOf(' ', fit - 1, fit - start);
                    if (boundary > start) fit = boundary + 1;
                }
                output.Add(text.Substring(start, fit - start).Trim()); start = fit;
            }
            if (output.Count == 0) output.Add("");
        }
        public void RefreshPin() { if (current != null && !MenuOpen) SetButton(actions[0], gaze.Focus.Pinned ? "Liberar" : "Fixar"); }
        private void RenderPage()
        {
            if (current == null || MenuOpen) return;
            heading.text = "ACERVO / MEDIAÇÃO DIGITAL";
            title.text = current.title;
            body.text = pages[page];
            pageLabel.text = "Página " + (page + 1) + " de " + pages.Count;
            previous.interactable = page > 0; next.interactable = page + 1 < pages.Count;
            SetButton(actions[0], gaze.Focus.Pinned ? "Liberar" : "Fixar");
            SetButton(actions[1], "Opções"); SetButton(actions[2], "Fechar");
            ApplyContrast();
        }
        private void RenderMenu()
        {
            heading.text = "VISITA / CONTROLES PELO OLHAR";
            title.text = "Opções do visor";
            body.fontSize = 28;
            body.text = "Mantenha a mira sobre um botão até completar o círculo.\n\n" +
                "Olhe para baixo para caminhar; levante o olhar para parar. Olhe para cima por 1,2 segundo para abrir este menu.\n\n" +
                "Durante a leitura da ficha, o movimento fica suspenso.";
            pageLabel.text = "Opções " + (menuPage + 1) + " de 3";
            previous.interactable = menuPage > 0; next.interactable = menuPage < 2;
            if (menuPage == 0)
            {
                SetButton(actions[0], "Voltar à visita");
                SetButton(actions[1], "Texto: " + new[] { "padrão", "grande", "maior" }[preferences.TextStep]);
                SetButton(actions[2], "Resumo: " + (preferences.Summary ? "sim" : "não"));
            }
            else if (menuPage == 1)
            {
                SetButton(actions[0], "Fichas: " + (preferences.PanelsEnabled ? "sim" : "não"));
                SetButton(actions[1], "Contraste: " + (preferences.HighContrast ? "alto" : "padrão"));
                SetButton(actions[2], "Animação: " + (preferences.ReducedMotion ? "não" : "sim"));
            }
            else
            {
                SetButton(actions[0], "Leitura: " + (preferences.LongerReading ? "longa" : "breve"));
                SetButton(actions[1], "Centralizar"); SetButton(actions[2], "Sair do VR");
            }
            ApplyContrast();
        }
        private void ApplyContrast()
        {
            background.color = preferences.HighContrast ? Color.black : MuseumGuideTheme.Surface;
            body.color = title.color = preferences.HighContrast ? Color.white : MuseumGuideTheme.Text;
        }
        private void ChangePage(int delta)
        {
            if (MenuOpen) { menuPage = Mathf.Clamp(menuPage + delta, 0, 2); RenderMenu(); }
            else { page = Mathf.Clamp(page + delta, 0, pages.Count - 1); RenderPage(); }
        }
        private void InvokeAction(int index)
        {
            if (!MenuOpen)
            {
                if (index == 0) { gaze.TogglePin(); RenderPage(); }
                else if (index == 1) OpenMenu();
                else gaze.Close();
                return;
            }
            if (menuPage == 0)
            {
                if (index == 0) { CloseMenu(); return; }
                if (index == 1) preferences.CycleTextSize(); else preferences.ToggleSummary();
            }
            else if (menuPage == 1)
            {
                if (index == 0) preferences.TogglePanels();
                else if (index == 1) preferences.ToggleContrast(); else preferences.ToggleMotion();
            }
            else
            {
                if (index == 0) preferences.ToggleLinger();
                else if (index == 1) { mode.Recenter(); return; }
                else { mode.ExitVR(); return; }
            }
            RenderMenu();
        }
        private void Update()
        {
            if (!IsEnabled) return;
            bool paused = FindPlayerPaused();
            if (paused) { activation.Reset(); ring.fillAmount = 0; return; }
            group.alpha = preferences.ReducedMotion ? (visible ? 1 : 0) :
                Mathf.MoveTowards(group.alpha, visible ? 1 : 0, Time.unscaledDeltaTime / MuseumGuideTheme.Fade);
            var ray = new Ray(eye.transform.position, eye.transform.forward);
            ConsumesGaze = visible && HitRect(Board, ray);
            Button selected = null;
            if (ConsumesGaze)
            {
                if (previous.interactable && HitRect((RectTransform)previous.transform, ray)) selected = previous;
                else if (next.interactable && HitRect((RectTransform)next.transform, ray)) selected = next;
                else foreach (var button in actions)
                    if (button.interactable && HitRect((RectTransform)button.transform, ray)) { selected = button; break; }
            }
            bool fire = activation.Step(selected, Time.unscaledDeltaTime, DwellSeconds);
            ring.fillAmount = activation.Progress;
            if (fire && selected != null) selected.onClick.Invoke();
            float pitch = Mathf.Asin(Mathf.Clamp(eye.transform.forward.y, -1, 1)) * Mathf.Rad2Deg;
            if (pitch < 35) { lookupArmed = true; lookupTimer = 0; }
            else if (pitch > 48 && lookupArmed && !MenuOpen)
            {
                lookupTimer += Time.unscaledDeltaTime;
                if (lookupTimer > 1.2f) { lookupArmed = false; lookupTimer = 0; OpenMenu(); }
            }
            else lookupTimer = 0;
        }
        private bool FindPlayerPaused() => gaze.IsPaused;
        public static bool HitRect(RectTransform rect, Ray ray)
        {
            if (!rect.gameObject.activeInHierarchy) return false;
            var plane = new Plane(rect.forward, rect.position);
            if (!plane.Raycast(ray, out float distance) || distance < 0) return false;
            var local = rect.InverseTransformPoint(ray.GetPoint(distance));
            return rect.rect.Contains(new Vector2(local.x, local.y));
        }
        public static Text MakeText(Transform parent, string value, int fontSize, float height)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>(MuseumGuideTheme.FontResource);
            text.text = value; text.fontSize = fontSize; text.color = MuseumGuideTheme.Text;
            text.supportRichText = false; text.raycastTarget = false; text.lineSpacing = 1.12f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            var layout = go.GetComponent<LayoutElement>(); layout.preferredHeight = layout.minHeight = height; layout.flexibleHeight = 0;
            return text;
        }
        public static Button MakeButton(Transform parent, string value, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Botao_" + value, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false); go.GetComponent<Image>().color = MuseumGuideTheme.Control;
            var button = go.GetComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(action);
            var layout = go.GetComponent<LayoutElement>(); layout.minHeight = layout.preferredHeight = 54; layout.flexibleHeight = 0; layout.flexibleWidth = 1;
            var label = MakeText(go.transform, value, 24, 54); label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(6, 2); label.rectTransform.offsetMax = new Vector2(-6, -2);
            return button;
        }
        private static Transform Row(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var row = go.GetComponent<HorizontalLayoutGroup>(); row.spacing = 10; row.childForceExpandHeight = false;
            var layout = go.GetComponent<LayoutElement>(); layout.minHeight = layout.preferredHeight = 54; layout.flexibleHeight = 0;
            return go.transform;
        }
        private static void SetButton(Button button, string value) => button.GetComponentInChildren<Text>().text = value;
        private Sprite MakeRing()
        {
            const int size = 64;
            ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32));
                ringTexture.SetPixel(x, y, new Color(1, 1, 1, distance >= 25 && distance <= 30 ? 1 : 0));
            }
            ringTexture.Apply();
            return Sprite.Create(ringTexture, new Rect(0, 0, size, size), Vector2.one * .5f);
        }
        private void OnApplicationPause(bool paused) { activation.Reset(); lookupTimer = 0; }
        private void OnDestroy()
        {
            if (reticle != null) Destroy(reticle.gameObject);
            if (overlayMaterial != null) Destroy(overlayMaterial);
            if (ringSprite != null) Destroy(ringSprite);
            if (ringTexture != null) Destroy(ringTexture);
        }
    }
}
