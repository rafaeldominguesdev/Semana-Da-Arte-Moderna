using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MuseumModerna
{
    /// <summary>Ficha única no canto, área segura, rolagem e preferências persistentes.</summary>
    public sealed class MuseumGuidePanel : MonoBehaviour
    {
        private const string Pref = "MuseumGuide_";
        private RectTransform safe, panel, settings;
        private CanvasGroup group;
        private Image background;
        private Text title, author, technique, notice, description, relation, source;
        private Text pinLabel, enabledLabel, sizeLabel, summaryLabel, contrastLabel, lingerLabel, motionLabel;
        private ScrollRect scroll;
        private Font font;
        private Sprite rounded;
        private Texture2D roundedTexture;
        private PaintingInfo current;
        private GazeDwellInteraction gaze;
        private bool visible, pinned, summary, contrast, linger, reducedMotion, panelsEnabled;
        private int textStep;
        private Rect lastSafe;
        private Vector2 lastSize;
        public bool IsVisible => visible;
        public bool PanelsEnabled => panelsEnabled;
        public bool Summary => summary;
        public bool HighContrast => contrast;
        public bool LongerReading => linger;
        public bool ReducedMotion => reducedMotion;
        public int TextStep => textStep;
        public Sprite RoundedSurface => rounded;
        public void CycleTextSize() { textStep = (textStep + 1) % 3; ApplyPreferences(); }
        public void ToggleSummary() { summary = !summary; ApplyPreferences(); }
        public void ToggleContrast() { contrast = !contrast; ApplyPreferences(); }
        public void TogglePanels() { panelsEnabled = !panelsEnabled; ApplyPreferences(); }
        public void ToggleLinger() { linger = !linger; ApplyPreferences(); }
        public void ToggleMotion() { reducedMotion = !reducedMotion; ApplyPreferences(); }
        public CanvasGroup Group => group;
        public RectTransform Panel => panel;
        public void Build(Transform parent)
        {
            font = Resources.GetBuiltinResource<Font>(MuseumGuideTheme.FontResource);
            gaze = FindAnyObjectByType<GazeDwellInteraction>();
            panelsEnabled = PlayerPrefs.GetInt(Pref + "enabled", 1) != 0;
            summary = PlayerPrefs.GetInt(Pref + "summary", 0) != 0;
            contrast = PlayerPrefs.GetInt(Pref + "contrast", 0) != 0;
            linger = PlayerPrefs.GetInt(Pref + "linger", 0) != 0;
            reducedMotion = PlayerPrefs.GetInt(Pref + "motion", 0) != 0;
            textStep = Mathf.Clamp(PlayerPrefs.GetInt(Pref + "size", 0), 0, 2);
            rounded = CreateRoundedSprite();
            safe = Rect("GuideSafeArea", parent);
            Stretch(safe);
            panel = Rect("FichaDaObra", safe);
            background = Surface(panel);
            var border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = MuseumGuideTheme.Accent;
            border.effectDistance = new Vector2(1, -1);
            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;
            var layout = Column(panel, MuseumGuideTheme.Padding);
            layout.childForceExpandHeight = false;
            var eyebrow = Label(panel, "ACERVO / MEDIAÇÃO DIGITAL", MuseumGuideTheme.Label);
            eyebrow.color = MuseumGuideTheme.Accent;

            var viewport = Rect("LeituraComRolagem", panel);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpLayout = viewport.gameObject.AddComponent<LayoutElement>();
            vpLayout.flexibleHeight = 1;
            vpLayout.minHeight = 40;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport;
            var content = Rect("Conteudo", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            Column(content, 2);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            title = Label(content, "", MuseumGuideTheme.Title, FontStyle.Bold);
            author = Label(content, "", MuseumGuideTheme.Body, FontStyle.Bold);
            technique = Label(content, "", MuseumGuideTheme.Label);
            notice = Label(content, "", MuseumGuideTheme.Label);
            notice.color = MuseumGuideTheme.Accent;
            description = Label(content, "", MuseumGuideTheme.Body);
            relation = Label(content, "", MuseumGuideTheme.Body);
            source = Label(content, "", MuseumGuideTheme.Label);
            var readHint = Label(panel, "Role para ler • F fixa/libera • Esc fecha", MuseumGuideTheme.Label);
            readHint.gameObject.name = "InstrucoesDaFicha";
            var actions = Rect("Acoes", panel);
            var actionSize = actions.gameObject.AddComponent<LayoutElement>();
            actionSize.minHeight = actionSize.preferredHeight = MuseumGuideTheme.ButtonHeight;
            actionSize.flexibleHeight = 0;
            var row = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = MuseumGuideTheme.Gap; row.childForceExpandWidth = true; row.childForceExpandHeight = false;
            pinLabel = Button(actions, "Fixar", () => gaze?.TogglePin());
            Button(actions, "Fechar", () => gaze?.Close());

            var toggle = Rect("OpcoesDaVisita", safe);
            toggle.anchorMin = toggle.anchorMax = toggle.pivot = new Vector2(0, 1);
            toggle.anchoredPosition = new Vector2(MuseumGuideTheme.Padding, -MuseumGuideTheme.Padding);
            toggle.sizeDelta = new Vector2(210, MuseumGuideTheme.ButtonHeight);
            var trigger = Button(toggle, "Opções da visita", ToggleSettings);
            Stretch((RectTransform)trigger.transform.parent);
            settings = Rect("PreferenciasDeLeitura", safe);
            Surface(settings);
            // ScrollRect permite acessar todas as opções mesmo em telas baixas.
            settings.gameObject.AddComponent<RectMask2D>();
            var optionsScroll = settings.gameObject.AddComponent<ScrollRect>();
            optionsScroll.horizontal = false; optionsScroll.movementType = ScrollRect.MovementType.Clamped;
            optionsScroll.viewport = settings;
            var options = Rect("Opcoes", settings);
            options.anchorMin = new Vector2(0, 1); options.anchorMax = Vector2.one;
            options.pivot = new Vector2(.5f, 1); options.sizeDelta = Vector2.zero;
            Column(options, MuseumGuideTheme.Padding);
            options.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            optionsScroll.content = options;
            enabledLabel = Button(options, "", () => { panelsEnabled = !panelsEnabled; ApplyPreferences(); });
            sizeLabel = Button(options, "", () => { textStep = (textStep + 1) % 3; ApplyPreferences(); });
            summaryLabel = Button(options, "", () => { summary = !summary; ApplyPreferences(); });
            contrastLabel = Button(options, "", () => { contrast = !contrast; ApplyPreferences(); });
            lingerLabel = Button(options, "", () => { linger = !linger; ApplyPreferences(); });
            motionLabel = Button(options, "", () => { reducedMotion = !reducedMotion; ApplyPreferences(); });
            var visitor = FindAnyObjectByType<PlayerController>();
            Button(options, "Calibrar câmera", () => visitor?.Calibrate());
#if UNITY_ANDROID && !UNITY_EDITOR
            Button(options, "Preparar visor VR", () => MobileVrMode.Instance?.ShowPreparation());
#endif
            Button(options, "Fechar opções", ToggleSettings);
            settings.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
            ApplyPreferences(false);
            UpdateSafeArea();
        }
        private void ToggleSettings()
        {
            settings.gameObject.SetActive(!settings.gameObject.activeSelf);
            if (settings.gameObject.activeSelf && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(enabledLabel.transform.parent.gameObject);
        }
        public void Show(PaintingInfo info)
        {
            if (info == null || !panelsEnabled) return;
            current = info; visible = true;
            panel.gameObject.SetActive(true);
            RefreshText();
            scroll.verticalNormalizedPosition = 1;
            group.interactable = true; group.blocksRaycasts = true;
        }
        public void Hide()
        {
            visible = false;
            if (group == null) return;
            group.interactable = false; group.blocksRaycasts = false;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.transform.IsChildOf(panel))
                EventSystem.current.SetSelectedGameObject(null);
        }
        public void SetPinned(bool value)
        {
            pinned = value;
            if (pinLabel != null) pinLabel.text = pinned ? "Liberar (F)" : "Fixar (F)";
        }
        private void RefreshText()
        {
            if (current == null) return;
            title.text = current.title;
            author.text = current.artist + " — " + current.Periodo;
            technique.text = current.tecnica;
            technique.gameObject.SetActive(!string.IsNullOrWhiteSpace(current.tecnica));
            notice.text = current.obra_historica_ou_reinterpretacao;
            notice.gameObject.SetActive(!string.IsNullOrWhiteSpace(notice.text));
            description.text = summary && !string.IsNullOrWhiteSpace(current.resumo) ? current.resumo : current.description;
            relation.text = "RELAÇÃO COM 1922\n" + current.relacao_com_a_semana_de_1922;
            source.text = "FONTES DA FICHA\n" + current.fontes;
            source.gameObject.SetActive(!summary && !string.IsNullOrWhiteSpace(current.fontes));
        }
        private void ApplyPreferences(bool save = true)
        {
            enabledLabel.text = "Painéis: " + (panelsEnabled ? "ativados" : "desativados");
            sizeLabel.text = "Texto: " + new[] { "padrão", "grande", "maior" }[textStep];
            summaryLabel.text = "Versão resumida: " + (summary ? "sim" : "não");
            contrastLabel.text = "Alto contraste: " + (contrast ? "sim" : "não");
            lingerLabel.text = "Tempo após olhar: " + (linger ? "5 segundos" : "breve");
            motionLabel.text = "Reduzir animação: " + (reducedMotion ? "sim" : "não");
            background.color = contrast ? Color.black : MuseumGuideTheme.Surface;
            foreach (var label in new[] { title, author, technique, notice, description, relation, source })
            {
                int size = label == title ? MuseumGuideTheme.Title :
                    label == technique || label == notice || label == source ? MuseumGuideTheme.Label : MuseumGuideTheme.Body;
                label.fontSize = size + textStep * 3;
                label.color = contrast ? Color.white : label == notice ? MuseumGuideTheme.Accent : MuseumGuideTheme.Text;
            }
            if (gaze != null)
            {
                gaze.LongerReading = linger;
                gaze.SetPanelsEnabled(panelsEnabled);
            }
            if (!panelsEnabled) Hide();
            RefreshText();
            foreach (var button in safe.GetComponentsInChildren<Button>(true))
            {
                var colors = button.colors;
                colors.fadeDuration = reducedMotion ? 0 : MuseumGuideTheme.Fade;
                button.colors = colors;
            }
            if (!save) return;
            PlayerPrefs.SetInt(Pref + "enabled", panelsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(Pref + "size", textStep);
            PlayerPrefs.SetInt(Pref + "summary", summary ? 1 : 0);
            PlayerPrefs.SetInt(Pref + "contrast", contrast ? 1 : 0);
            PlayerPrefs.SetInt(Pref + "linger", linger ? 1 : 0);
            PlayerPrefs.SetInt(Pref + "motion", reducedMotion ? 1 : 0);
            PlayerPrefs.Save();
        }
        private void Update()
        {
            if (group == null) return;
            UpdateSafeArea();
            group.alpha = reducedMotion ? (visible ? 1 : 0) :
                Mathf.MoveTowards(group.alpha, visible ? 1 : 0, Time.unscaledDeltaTime / MuseumGuideTheme.Fade);
            if (!visible && group.alpha == 0 && panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
            if (settings.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape)) ToggleSettings();
            if (visible && Input.GetKey(KeyCode.PageDown)) scroll.verticalNormalizedPosition -= Time.unscaledDeltaTime;
            if (visible && Input.GetKey(KeyCode.PageUp)) scroll.verticalNormalizedPosition += Time.unscaledDeltaTime;
        }
        private void UpdateSafeArea()
        {
            var size = new Vector2(Screen.width, Screen.height);
            if (lastSafe == Screen.safeArea && lastSize == size) return;
            lastSafe = Screen.safeArea; lastSize = size;
            safe.anchorMin = new Vector2(lastSafe.xMin / size.x, lastSafe.yMin / size.y);
            safe.anchorMax = new Vector2(lastSafe.xMax / size.x, lastSafe.yMax / size.y);
            ConfigureScale(size.x, size.y);
            Canvas.ForceUpdateCanvases();
            float width = safe.rect.width, height = safe.rect.height;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 0);
            panel.anchoredPosition = new Vector2(-MuseumGuideTheme.Padding, MuseumGuideTheme.Padding);
            panel.sizeDelta = new Vector2(Mathf.Min(MuseumGuideTheme.Width, width > height ? width * .42f : width - 40), height * (width > height ? .70f : .46f));
            settings.anchorMin = settings.anchorMax = settings.pivot = new Vector2(0, 1);
            settings.anchoredPosition = new Vector2(20, -76);
            settings.sizeDelta = new Vector2(Mathf.Min(320, width - 40), Mathf.Min(420, height - 96));
        }
        public void ConfigureScale(float width, float height)
        {
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.referenceResolution = height > width ? new Vector2(390, 844) :
                height < 600 ? new Vector2(844, 390) : new Vector2(1280, 800);
        }
        private RectTransform Rect(string objectName, Transform parent)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        private Image Surface(RectTransform rt)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = rounded; image.type = Image.Type.Sliced;
            image.color = MuseumGuideTheme.Surface;
            return image;
        }
        private static VerticalLayoutGroup Column(RectTransform rt, int padding)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = MuseumGuideTheme.Gap;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return layout;
        }
        private Text Label(Transform parent, string value, int size, FontStyle style = FontStyle.Normal)
        {
            var label = Rect("Texto", parent).gameObject.AddComponent<Text>();
            label.font = font; label.text = value; label.fontSize = size; label.fontStyle = style;
            label.color = MuseumGuideTheme.Text; label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.lineSpacing = 1.16f; label.raycastTarget = false;
            return label;
        }
        private Text Button(Transform parent, string value, UnityEngine.Events.UnityAction action)
        {
            var rt = Rect("Botao_" + value, parent);
            var image = Surface(rt); image.color = MuseumGuideTheme.Control;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = colors.selectedColor = new Color(1.7f, 1.7f, 1.7f);
            colors.pressedColor = MuseumGuideTheme.Accent;
            colors.fadeDuration = MuseumGuideTheme.Fade;
            button.colors = colors;
            button.onClick.AddListener(action);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = MuseumGuideTheme.ButtonHeight;
            le.flexibleWidth = 1; le.flexibleHeight = 0;
            var text = Label(rt, value, MuseumGuideTheme.Label);
            text.alignment = TextAnchor.MiddleCenter; Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(8, 2); text.rectTransform.offsetMax = new Vector2(-8, -2);
            return text;
        }
        private Sprite CreateRoundedSprite()
        {
            const int size = 32;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(MuseumGuideTheme.Radius - x, x - (size - 1 - MuseumGuideTheme.Radius));
                float dy = Mathf.Max(MuseumGuideTheme.Radius - y, y - (size - 1 - MuseumGuideTheme.Radius));
                float distance = new Vector2(Mathf.Max(0, dx), Mathf.Max(0, dy)).magnitude;
                roundedTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(MuseumGuideTheme.Radius - distance)));
            }
            roundedTexture.Apply();
            return Sprite.Create(roundedTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
        }
        private void OnDestroy()
        {
            if (rounded != null) Destroy(rounded);
            if (roundedTexture != null) Destroy(roundedTexture);
        }
    }
}
