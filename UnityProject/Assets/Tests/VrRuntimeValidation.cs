#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MuseumModerna
{
    public sealed class VrRuntimeValidation : MonoBehaviour
    {
        private MobileVrMode mode;
        private VrWorldGuide guide;
        private Camera eye;
        private IEnumerator Start()
        {
            yield return null; yield return null;
            var routine = Tests();
            while (true)
            {
                object value;
                try { if (!routine.MoveNext()) break; value = routine.Current; }
                catch (Exception e) { Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); yield break; }
                yield return value;
            }
            Debug.Log("[VrValidation] Integração sem toque: PASS (simulação no Editor; sem lentes físicas)");
            UnityEditor.EditorApplication.Exit(0);
        }
        private IEnumerator Tests()
        {
            mode = MobileVrMode.Instance;
            Require(mode != null, "Bootstrap VR");
            guide = mode.WorldGuide;
            eye = FindAnyObjectByType<GazeDwellInteraction>().GetComponent<Camera>();
            foreach (var gyro in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) gyro.enabled = false;
            foreach (var movement in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) movement.enabled = false;
            foreach (var anchor in FindObjectsByType<PlayerSpawnAnchor>(FindObjectsSortMode.None)) anchor.enabled = false;
            foreach (var locked in FindObjectsByType<LockPosition>(FindObjectsSortMode.None)) locked.enabled = false;
            var player = FindAnyObjectByType<PlayerController>();
            var gaze = eye.GetComponent<GazeDwellInteraction>();
            var original = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None).First(e => e.PaintingData.uniqueId == "homem_amarelo");
            player.transform.position += original.transform.position + original.transform.forward * 3 - eye.transform.position;
            eye.transform.LookAt(original.transform.position);
            gaze.SetPanelsEnabled(true);
            mode.EnterEditorPreview();
            Require(mode.IsActive && guide.MenuOpen && !FindAnyObjectByType<UIManager>().GetComponent<Canvas>().enabled, "Canvas2D oculto em VR");
            Require(guide.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "Ficha em espaço3D para ambos os olhos");
            var initialPosition = guide.Board.position;
            PointAt(Button("Voltar à visita").transform);
            yield return new WaitForSecondsRealtime(.5f);
            Require(guide.MenuOpen, "Permanência parcial não aciona menu");
            Require(Vector3.Distance(initialPosition, guide.Board.position) < .001f, "Painel não foge ao girar cabeça");
            yield return new WaitForSecondsRealtime(.7f);
            Require(!guide.MenuOpen, "Voltar acionado apenas pelo olhar");
            eye.transform.LookAt(original.transform.position);
            yield return new WaitForSecondsRealtime(.5f);
            Require(gaze.Focus.Current == original.PaintingData && guide.GetComponent<CanvasGroup>().alpha > .9f, "Obra abre ficha VR");
            Require(guide.PageCount > 1, "Ficha longa paginada sem toque");
            PointAt(Button("Próxima").transform);
            yield return new WaitForSecondsRealtime(1.3f);
            Require(guide.PageIndex == 1 && gaze.Focus.Current == original.PaintingData, "Ler controles mantém ficha e avança página");
            yield return new WaitForSecondsRealtime(1.5f);
            Require(guide.PageIndex == 1, "Página não repete ao manter olhar");
            PointAt(Button("Fixar").transform);
            yield return new WaitForSecondsRealtime(1.3f);
            Require(gaze.Focus.Pinned, "Fixação pelo olhar");
            eye.transform.LookAt(original.transform.position);
            yield return new WaitForSecondsRealtime(.2f);
            Capture("museum-vr-guide-editor.png");
            PointAt(Button("Liberar").transform);
            yield return new WaitForSecondsRealtime(1.3f);
            Require(!gaze.Focus.Pinned, "Liberar pelo olhar");
            PointAt(Button("Fechar").transform);
            yield return new WaitForSecondsRealtime(1.6f);
            Require(gaze.Focus.Current == null && guide.GetComponent<CanvasGroup>().alpha == 0, "Fechar pelo olhar");
            eye.transform.rotation = Quaternion.LookRotation(new Vector3(0, 1, .6f));
            yield return new WaitForSecondsRealtime(1.5f);
            Require(guide.MenuOpen, "Olhar para cima abre opções sem controle");
            PointAt(Button("Próxima").transform);
            yield return new WaitForSecondsRealtime(1.3f);
            PointAt(guide.Board); yield return new WaitForSecondsRealtime(.1f);
            PointAt(Button("Próxima").transform); yield return new WaitForSecondsRealtime(1.3f);
            PointAt(Button("Sair do VR").transform); yield return new WaitForSecondsRealtime(1.3f);
            Require(!mode.IsActive && FindAnyObjectByType<UIManager>().GetComponent<Canvas>().enabled, "Saída restaura prévia2D");
        }
        private Button Button(string label) => guide.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>().text == label);
        private void PointAt(Transform target)
        {
            Canvas.ForceUpdateCanvases();
            eye.transform.LookAt(target.position);
        }
        private void Capture(string filename)
        {
            var target = new RenderTexture(1440, 900, 24);
            eye.targetTexture = target; Canvas.ForceUpdateCanvases(); eye.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); texture.Apply();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts"));
            Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder, filename), texture.EncodeToPNG());
            eye.targetTexture = null; RenderTexture.active = previous;
            Destroy(texture); target.Release(); Destroy(target);
            Debug.Log("[VrValidation] Captura mono da interface worldspace: " + filename);
        }
        private static void Require(bool ok, string message) { if (!ok) throw new Exception("[VrValidation] " + message); }
    }
}
#endif
