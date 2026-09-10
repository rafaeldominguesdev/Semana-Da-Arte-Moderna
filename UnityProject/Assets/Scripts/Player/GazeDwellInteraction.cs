using UnityEngine;
using UnityEngine.EventSystems;

namespace MuseumModerna
{
    /// <summary>Raycast central com oclusão real, ficha automática e fixação opcional.</summary>
    [DefaultExecutionOrder(100)]
    public class GazeDwellInteraction : MonoBehaviour
    {
        [SerializeField, Min(0)] private float dwellTime = 0.12f;
        [SerializeField, Min(0.1f)] private float maxRayDistance = 8f;
        [SerializeField, Min(0)] private float autoCloseTimeout = 0.15f;
        [SerializeField] private LayerMask paintingLayer; // compatibilidade das cenas existentes
        [SerializeField] private PlayerController playerController;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private Camera vrCamera;
        public GuideFocusState Focus { get; } = new GuideFocusState();
        public bool LongerReading { get; set; }
        public bool IsPaused => playerController != null && playerController.State == PlayerState.Paused;
        private PaintingInfo candidate;
        private PaintingInfo published;
        private float candidateTime;
        private Vector2 touchStart;
        private float touchTime;
        private bool touchOnUI;
        private int rayMask;

        private void Awake()
        {
            vrCamera = vrCamera != null ? vrCamera : GetComponent<Camera>() ?? Camera.main;
            playerController = playerController != null ? playerController : FindAnyObjectByType<PlayerController>();
            uiManager = uiManager != null ? uiManager : FindAnyObjectByType<UIManager>();
            rayMask = Physics.DefaultRaycastLayers;
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) rayMask &= ~(1 << playerLayer);
        }

        public PaintingExhibit RaycastExhibit(Ray ray)
        {
            // Primeiro sólido atingido: paredes, vitrines e pedestais também bloqueiam o olhar.
            if (Physics.Raycast(ray, out var hit, maxRayDistance, rayMask, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<PaintingExhibit>();
            return null;
        }

        private void Update()
        {
            if (vrCamera == null || playerController == null) return;
            if (playerController.State == PlayerState.Paused) return;
            var vrMode = MobileVrMode.Instance;
            bool inVr = vrMode != null && vrMode.IsActive;
            if (inVr && (vrMode.WorldGuide.ConsumesGaze || vrMode.WorldGuide.MenuOpen))
            {
                Publish();
                return; // Ler e acionar a ficha não deve encerrá-la ou selecionar a parede ao fundo.
            }
            var target = RaycastExhibit(new Ray(vrCamera.transform.position, vrCamera.transform.forward));
            var data = target != null ? target.PaintingData : null;
            if (candidate != data) { candidate = data; candidateTime = 0; }
            candidateTime += Time.unscaledDeltaTime;
            Focus.Observe(candidateTime >= dwellTime ? data : null, Time.unscaledDeltaTime,
                LongerReading ? 5f : inVr ? 1.2f : autoCloseTimeout);

            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            if (Input.GetKeyDown(KeyCode.F)) TogglePin();
            if (!inVr && Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    touchStart = touch.position;
                    touchTime = Time.unscaledTime;
                    touchOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
                }
                if (touch.phase == TouchPhase.Ended && !touchOnUI &&
                    Vector2.Distance(touchStart, touch.position) < 18 && Time.unscaledTime - touchTime < .35f)
                    PinAt(touch.position);
            }
            else if (!inVr && Input.GetMouseButtonDown(0) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                PinAt(Input.mousePosition);

            Publish();
            uiManager?.SetCrosshairActive(data != null && Focus.Enabled);
        }

        private void PinAt(Vector2 position)
        {
            var exhibit = RaycastExhibit(vrCamera.ScreenPointToRay(position));
            if (exhibit != null) Focus.Pin(exhibit.PaintingData);
        }
        public void TogglePin() { Focus.TogglePin(); Publish(); }
        public void Close() { Focus.Dismiss(candidate); Publish(); }
        public void SetPanelsEnabled(bool value) { Focus.SetEnabled(value); Publish(); }
        public void CancelInteraction() => Close();
        private void Publish()
        {
            if (published != Focus.Current)
            {
                if (published != null) playerController?.TriggerLeavePainting();
                published = Focus.Current;
                if (published != null) playerController?.TriggerNearPainting(published);
            }
            uiManager?.SetGuidePinned(Focus.Pinned);
        }
        private void OnDisable()
        {
            Focus.Dismiss(null);
            Publish();
        }
    }
}
