#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MuseumModerna
{
    public sealed class GuideRuntimeValidation : MonoBehaviour
    {
        private GazeDwellInteraction gaze;
        private Camera cameraView;
        private PlayerController player;
        private UIManager ui;
        private IEnumerator Start()
        {
            yield return null; yield return null;
            var tests = Test();
            while (true)
            {
                object yielded;
                try { if (!tests.MoveNext()) break; yielded = tests.Current; }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    UnityEditor.EditorApplication.Exit(1);
                    yield break;
                }
                yield return yielded;
            }
            Debug.Log("[GuideValidation] Integração: PASS");
            UnityEditor.EditorApplication.Exit(0);
        }
        private IEnumerator Test()
        {
            gaze = FindAnyObjectByType<GazeDwellInteraction>();
            cameraView = gaze.GetComponent<Camera>(); player = FindAnyObjectByType<PlayerController>(); ui = FindAnyObjectByType<UIManager>();
            foreach (var gyro in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) gyro.enabled = false;
            foreach (var movement in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) movement.enabled = false;
            foreach (var anchor in FindObjectsByType<PlayerSpawnAnchor>(FindObjectsSortMode.None)) anchor.enabled = false;
            foreach (var locked in FindObjectsByType<LockPosition>(FindObjectsSortMode.None)) locked.enabled = false;
            Require(ui.Guide != null && ui.Guide.Group.alpha == 0, "Ficha começa oculta");
            gaze.SetPanelsEnabled(true);
            var exhibits = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            Require(exhibits.Length == 28, "28 objetos associados, encontrados " + exhibits.Length);
            // Todos os pontos de aproximação devem reconhecer a ficha correta por física real.
            foreach (var exhibit in exhibits)
            {
                Vector3 center = exhibit.transform.position;
                Vector3 front = exhibit.transform.forward;
                if (exhibit.PaintingData.categoria == "Escultura cenográfica") { center = LouvreRuntimeValidation.SculptureAim(exhibit); front = Vector3.left; }
                if (exhibit.PaintingData.categoria == "Vitrine documental") { center += Vector3.up * 1.1f; front = new Vector3(0, .3f, 1).normalized; }
                Position(center + front * 2.5f, center);
                Physics.SyncTransforms();
                var hit = gaze.RaycastExhibit(cameraView.ViewportPointToRay(new Vector3(.5f, .5f)));
                Require(hit == exhibit, "Raycast não alcançou " + exhibit.name + "; encontrado " + (hit != null ? hit.name : "sólido/nenhum"));
            }
            Debug.Log("[GuideValidation] Raycast nos 28 objetos: PASS");
            var a = exhibits.First(e => e.PaintingData.uniqueId == "homem_amarelo");
            Position(a.transform.position + a.transform.forward * 3, a.transform.position);
            yield return new WaitForSecondsRealtime(.6f);
            Require(player.CurrentNearPainting == a.PaintingData && ui.Guide.Group.alpha == 1, "Olhar abre ficha");
            Require(player.State == PlayerState.Walking, "Mediação não bloqueia caminhar");
            gaze.TogglePin(); cameraView.transform.rotation = Quaternion.LookRotation(Vector3.up);
            yield return new WaitForSecondsRealtime(.6f);
            Require(ui.Guide.Group.alpha == 1 && gaze.Focus.Pinned, "Fixação permanece ao desviar");
            gaze.TogglePin(); yield return new WaitForSecondsRealtime(.6f);
            Require(ui.Guide.Group.alpha == 0 && !ui.Guide.Panel.gameObject.activeSelf, "Fade-out desativa painel");
            Position(a.transform.position + a.transform.forward * 3, a.transform.position);
            yield return new WaitForSecondsRealtime(.6f);
            gaze.Close(); yield return new WaitForSecondsRealtime(.6f);
            Require(ui.Guide.Group.alpha == 0, "Fechar não reabre continuamente");
            cameraView.transform.rotation = Quaternion.LookRotation(Vector3.up);
            yield return new WaitForSecondsRealtime(.2f);
            Position(a.transform.position + a.transform.forward * 3, a.transform.position);
            yield return new WaitForSecondsRealtime(.6f);
            // Obstrução inserida entre câmera e obra: não pode atravessar a parede.
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = cameraView.transform.position + cameraView.transform.forward;
            wall.transform.localScale = Vector3.one;
            Physics.SyncTransforms();
            Require(gaze.RaycastExhibit(cameraView.ViewportPointToRay(new Vector3(.5f, .5f))) == null, "Parede bloqueia olhar");
            Destroy(wall);
            yield return null;
            gaze.Close();
            cameraView.transform.rotation = Quaternion.LookRotation(Vector3.up);
            yield return new WaitForSecondsRealtime(.2f);
            Position(a.transform.position + a.transform.forward * 3, a.transform.position);
            yield return new WaitForSecondsRealtime(.6f);
            Canvas.ForceUpdateCanvases();
            Require(ui.Guide.Panel.rect.width > 100 && ui.Guide.Panel.rect.height > 100, "Layout possui área de leitura");
            // Render real da cena + Canvas para evidência visual, sem depender de captura da área de trabalho.
            Capture("museum-guide-desktop.png", 1440, 900);
            Capture("museum-guide-mobile.png", 390, 844);
            Capture("museum-guide-landscape.png", 844, 390);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(MuseumGuidePanel).GetField("textStep", flags).SetValue(ui.Guide, 2);
            typeof(MuseumGuidePanel).GetField("contrast", flags).SetValue(ui.Guide, true);
            typeof(MuseumGuidePanel).GetField("summary", flags).SetValue(ui.Guide, true);
            typeof(MuseumGuidePanel).GetMethod("ApplyPreferences", flags).Invoke(ui.Guide, new object[] { false });
            Capture("museum-guide-accessibility.png", 390, 844);
            Require(ui.Guide.Panel.GetComponentsInChildren<Text>().Any(t => t.fontSize >= MuseumGuideTheme.Title + 6), "Texto ampliado aplicado");
            Require(ui.Guide.Panel.GetComponent<Image>().color == Color.black, "Contraste alto aplicado");
            Debug.Log("[GuideValidation] Texto maior, resumo e alto contraste: PASS");
            ui.HidePaintingPanel();
            yield return new WaitForSecondsRealtime(.4f);
            Position(new Vector3(10.5f, 1.7f, -3.5f), new Vector3(13f, 1.6f, 1));
            Capture("museum-sculpture-gallery.png", 1440, 900);
            yield return null;
        }
        private void Position(Vector3 eye, Vector3 target)
        {
            player.transform.position += eye - cameraView.transform.position;
            cameraView.transform.rotation = Quaternion.LookRotation(target - eye);
        }
        private void Capture(string file, int width, int height)
        {
            var canvas = ui.GetComponent<Canvas>();
            var original = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var render = new RenderTexture(width, height, 24);
            cameraView.targetTexture = render;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cameraView; canvas.planeDistance = .5f;
            // A área segura ocupa o alvo renderizado inteiro nesta captura de QA.
            var safe = ui.transform.Find("GuideSafeArea") as RectTransform;
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
            ui.Guide.ConfigureScale(width, height);
            // CanvasScaler atualiza por frame; para alvo de QA aplica seu mesmo cálculo agora.
            var scaler = canvas.GetComponent<CanvasScaler>();
            canvas.scaleFactor = Mathf.Sqrt(width / scaler.referenceResolution.x * height / scaler.referenceResolution.y);
            Canvas.ForceUpdateCanvases();
            ui.Guide.Panel.sizeDelta = new Vector2(Mathf.Min(MuseumGuideTheme.Width, safe.rect.width > safe.rect.height ? safe.rect.width * .42f : safe.rect.width - 40), safe.rect.height * (safe.rect.width > safe.rect.height ? .70f : .46f));
            Canvas.ForceUpdateCanvases();
            cameraView.Render();
            var old = RenderTexture.active; RenderTexture.active = render;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts"));
            Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, file), texture.EncodeToPNG());
            RenderTexture.active = old; cameraView.targetTexture = null;
            canvas.renderMode = original; canvas.worldCamera = previousCamera;
            Destroy(texture); render.Release(); Destroy(render);
            Debug.Log("[GuideValidation] Captura: " + file);
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("[GuideValidation] " + message);
        }
    }
}
#endif
