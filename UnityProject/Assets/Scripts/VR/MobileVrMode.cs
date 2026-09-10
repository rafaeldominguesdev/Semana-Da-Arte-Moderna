using System.Collections;
using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace MuseumModerna
{
    /// <summary>Cardboard nativo: projeção por olho, correção das lentes e rastreamento de cabeça.</summary>
    public sealed class MobileVrMode : MonoBehaviour
    {
        public static MobileVrMode Instance { get; private set; }
        public bool IsActive { get; private set; }
        public bool HasHeadTracking => IsActive && driver != null && driver.enabled;
        public VrWorldGuide WorldGuide { get; private set; }
        private UIManager ui;
        private Camera eye;
        private PlayerController player;
        private Canvas desktopCanvas;
        private GameObject preparation;
        private Text status;
        private Button enterButton, scanButton;
        private TrackedPoseDriver driver;
        private XRManagerSettings manager;
        private bool prepared, preparing;

        public void Initialize(UIManager managerUI)
        {
            Instance = this;
            ui = managerUI;
            player = FindAnyObjectByType<PlayerController>();
            eye = player.GetComponentInChildren<GazeDwellInteraction>().GetComponent<Camera>();
            desktopCanvas = ui.GetComponent<Canvas>();
            WorldGuide = new GameObject("FichaVR").AddComponent<VrWorldGuide>();
            WorldGuide.Build(eye, ui.Guide, this);
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            CreatePreparation();
            player.SetPaused(true);
            StartCoroutine(PrepareXR());
#endif
        }

        private IEnumerator PrepareXR()
        {
            if (prepared || preparing) yield break;
            preparing = true;
            if (!SystemInfo.supportsGyroscope)
            {
                status.text = "Este celular não informou um giroscópio. Use a prévia sem óculos.";
                preparing = false;
                yield break;
            }
            manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null)
            {
                status.text = "VR não configurado neste aplicativo. Use a prévia sem óculos.";
                preparing = false;
                yield break;
            }
            yield return manager.InitializeLoader();
            prepared = manager.activeLoader is Google.XR.Cardboard.XRLoader &&
                manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>() != null;
            preparing = false;
            if (!prepared)
            {
                status.text = "Não foi possível iniciar o visor VR. A prévia continua disponível.";
                yield break;
            }
            driver = eye.gameObject.GetComponent<TrackedPoseDriver>() ?? eye.gameObject.AddComponent<TrackedPoseDriver>();
            driver.enabled = false;
            driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            UpdatePreparation();
        }
        private void UpdatePreparation()
        {
            bool hasProfile = prepared && Api.HasDeviceParams();
            scanButton.interactable = prepared;
            enterButton.interactable = hasProfile;
            status.text = hasProfile
                ? "Perfil de lentes salvo. Coloque o celular na horizontal e entre em VR."
                : "Leia o QR de configuração do seu visor para ajustar as imagens às lentes. Depois, encaixe o celular.";
        }
        public void ScanViewer()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (prepared) Api.ScanDeviceParams();
#endif
        }
        public void EnterVR()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!prepared || !Api.HasDeviceParams()) return;
            if (Api.HasNewDeviceParams()) Api.ReloadDeviceParams();
            manager.StartSubsystems();
            var display = manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
            var input = manager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>();
            if (display == null || !display.running || input == null || !input.running)
            {
                manager.StopSubsystems();
                status.text = "A renderização VR não iniciou. Tente novamente ou use a prévia.";
                return;
            }
            driver.enabled = true;
            eye.stereoTargetEye = StereoTargetEyeMask.Both;
            eye.nearClipPlane = .05f;
            Api.Recenter();
            ActivateInterface(true);
#endif
        }
        private void ActivateInterface(bool waitForPose = false)
        {
            player.GetComponentInChildren<GazeDwellInteraction>().Close();
            IsActive = true;
            desktopCanvas.enabled = false;
            if (preparation != null) preparation.SetActive(false);
            WorldGuide.SetVrEnabled(true);
            if (waitForPose) StartCoroutine(FinishEntry());
            else { WorldGuide.OpenMenu(); player.SetPaused(false); }
        }
        private IEnumerator FinishEntry()
        {
            yield return null;
            if (!IsActive) yield break;
            WorldGuide.OpenMenu();
            player.SetPaused(false);
        }
#if UNITY_EDITOR
        // Simula apenas a interação no Editor. A estéreo real exige o SDK no Android.
        public void EnterEditorPreview() => ActivateInterface();
#endif
        public void ExitVR()
        {
            if (!IsActive) return;
            WorldGuide.SetVrEnabled(false);
            IsActive = false;
            var lastRotation = eye.transform.localRotation;
            if (driver != null) driver.enabled = false;
            eye.transform.localRotation = lastRotation;
#if UNITY_ANDROID && !UNITY_EDITOR
            manager?.StopSubsystems();
#endif
            eye.ResetAspect(); eye.ResetProjectionMatrix();
            desktopCanvas.enabled = true;
            player.GetComponentInChildren<GyroscopeController>()?.ResumeFromCurrentPose();
            player.GetComponentInChildren<GazeDwellInteraction>().Close();
            if (preparation != null)
            {
                preparation.SetActive(true);
                player.SetPaused(true);
                UpdatePreparation();
            }
        }
        public void Recenter()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (IsActive) Api.Recenter();
#endif
            StartCoroutine(RepositionMenu());
        }
        private IEnumerator RepositionMenu()
        {
            yield return null;
            if (IsActive) WorldGuide.OpenMenu();
        }
        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!prepared) return;
            if (Api.HasNewDeviceParams())
            {
                Api.ReloadDeviceParams();
                if (!IsActive) UpdatePreparation();
            }
            if (IsActive)
            {
                Api.UpdateScreenParams();
                if (Api.IsCloseButtonPressed || Input.GetKeyDown(KeyCode.Escape)) ExitVR();
                else if (Api.IsGearButtonPressed) { ExitVR(); ScanViewer(); }
                else if (Api.IsTriggerHeldPressed) Recenter();
            }
#endif
        }
        private void CreatePreparation()
        {
            preparation = new GameObject("PrepararVisor", typeof(RectTransform), typeof(Image));
            preparation.transform.SetParent(ui.transform, false);
            var rect = (RectTransform)preparation.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            preparation.GetComponent<Image>().color = MuseumGuideTheme.Surface;
            var box = new GameObject("Preparacao", typeof(RectTransform), typeof(VerticalLayoutGroup));
            box.transform.SetParent(rect, false);
            var content = (RectTransform)box.transform;
            content.anchorMin = new Vector2(.12f, .08f); content.anchorMax = new Vector2(.88f, .92f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = box.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10; layout.childForceExpandHeight = false;
            VrWorldGuide.MakeText(content, "MUSEU 1922 / VISOR VR", 28, 42);
            status = VrWorldGuide.MakeText(content, "Preparando o visor…", 20, 65);
            scanButton = VrWorldGuide.MakeButton(content, "1. Ler QR das lentes", ScanViewer);
            enterButton = VrWorldGuide.MakeButton(content, "2. Entrar em VR", EnterVR);
            scanButton.interactable = enterButton.interactable = false;
            VrWorldGuide.MakeButton(content, "Prévia sem óculos", () => { preparation.SetActive(false); player.SetPaused(false); });
            VrWorldGuide.MakeText(content, "No visor: olhe por 1 segundo para acionar botões. Olhe para cima para abrir opções.", 18, 50);
        }
        public void ShowPreparation()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (preparation != null) { preparation.SetActive(true); player.SetPaused(true); }
#endif
        }
        private void OnDestroy()
        {
            if (driver != null) driver.enabled = false;
            if (WorldGuide != null) Destroy(WorldGuide.gameObject);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (manager != null && manager.isInitializationComplete)
            {
                manager.StopSubsystems(); manager.DeinitializeLoader();
            }
#endif
            if (Instance == this) Instance = null;
        }
    }
}
