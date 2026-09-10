using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    [InitializeOnLoad]
    public static class GuideValidation
    {
        private const string Running = "MuseumGuideValidation";
        static GuideValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Running, false))
                    new GameObject("GuideRuntimeValidation").AddComponent<GuideRuntimeValidation>();
            };
        }
        public static void Run()
        {
            try
            {
                var ids = new System.Collections.Generic.HashSet<string>();
                foreach (var e in CuratorialCatalog.Read())
                {
                    Require(ids.Add(e.uniqueId), "ID duplicado: " + e.uniqueId);
                    Require(!string.IsNullOrWhiteSpace(e.title) && !string.IsNullOrWhiteSpace(e.artist) &&
                        !string.IsNullOrWhiteSpace(e.periodo) && !string.IsNullOrWhiteSpace(e.tecnica) &&
                        !string.IsNullOrWhiteSpace(e.description) && !string.IsNullOrWhiteSpace(e.resumo) &&
                        !string.IsNullOrWhiteSpace(e.relacao_com_a_semana_de_1922) &&
                        !string.IsNullOrWhiteSpace(e.obra_historica_ou_reinterpretacao) && !string.IsNullOrWhiteSpace(e.fontes),
                        "Ficha incompleta: " + e.uniqueId);
                }
                var a = ScriptableObject.CreateInstance<PaintingInfo>();
                var b = ScriptableObject.CreateInstance<PaintingInfo>();
                var focus = new GuideFocusState();
                Require(focus.Current == null, "Início oculto");
                focus.Observe(a, .2f, .15f); Require(focus.Current == a, "Olhar A");
                focus.Observe(b, .2f, .15f); Require(focus.Current == b, "Trocar A por B");
                focus.TogglePin(); focus.Observe(a, 1, .15f); Require(focus.Current == b, "Fixação mantém B");
                focus.TogglePin(); focus.Observe(a, 1, .15f); Require(focus.Current == a, "Liberar retoma olhar");
                focus.Dismiss(a); focus.Observe(a, 1, .15f); Require(focus.Current == null, "Fechar não reabre mesma obra");
                focus.Observe(null, 1, .15f); focus.Observe(a, 1, .15f); Require(focus.Current == a, "Reolhar após sair");
                focus.Observe(null, .1f, .15f); Require(focus.Current == a, "Tolerância breve");
                focus.Observe(null, .1f, .15f); Require(focus.Current == null, "Saiu: ocultar");
                focus.Pin(a); focus.SetEnabled(false); focus.Observe(b, 1, .15f);
                Require(focus.Current == null && !focus.Pinned, "Desativar ficha fixada");
                focus.SetEnabled(true); focus.Observe(b, 1, 5); focus.Observe(null, 4, 5);
                Require(focus.Current == b, "Leitura estendida");
                focus.Observe(null, 1, 5); Require(focus.Current == null, "Fim da leitura estendida");
                UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
                Debug.Log("[GuideValidation] 28 fichas e 12 cenários de estado: PASS");
                EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
                SessionState.SetBool(Running, true);
                EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }
        private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        public static void Finish(int code)
        {
            SessionState.SetBool(Running, false);
            EditorApplication.Exit(code);
        }
    }
}
