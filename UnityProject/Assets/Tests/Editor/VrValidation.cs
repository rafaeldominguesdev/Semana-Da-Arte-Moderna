using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    [InitializeOnLoad]
    public static class VrValidation
    {
        private const string Key = "MuseumVrValidation";
        static VrValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                {
                    SessionState.SetBool(Key, false);
                    new GameObject("VrRuntimeValidation").AddComponent<VrRuntimeValidation>();
                }
            };
        }
        public static void Run()
        {
            try
            {
                MobileVrSetup.ValidateConfiguration();
                var dwell = new GazeActivation();
                var a = new object(); var b = new object();
                Require(!dwell.Step(a, .5f, 1), "Botão não ativa antes do prazo");
                Require(dwell.Step(a, .5f, 1), "Botão ativa ao completar 1 segundo");
                Require(!dwell.Step(a, 5, 1), "Não repete mantendo o olhar");
                Require(!dwell.Step(b, .2f, 1), "Trocar alvo reinicia prazo");
                Require(!dwell.Step(null, 3, 1) && dwell.Progress == 0, "Sair cancela progresso");
                Require(dwell.Step(a, 1, 1), "Reolhar permite nova ativação");
                Debug.Log("[VrValidation] Configuração e 6 cenários de dwell: PASS");
                EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
                SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    }
}
