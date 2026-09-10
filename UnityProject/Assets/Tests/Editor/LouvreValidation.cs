using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    [InitializeOnLoad]
    public static class LouvreValidation
    {
        private const string RunningKey = "MuseumLouvreValidation";

        static LouvreValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false)) return;
                SessionState.SetBool(RunningKey, false);
                new GameObject("LouvreRuntimeValidation").AddComponent<LouvreRuntimeValidation>();
            };
        }

        // Run after LouvreMuseumSetup.Apply has saved the exhibition. No -quit: PlayMode exits the process.
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
                SessionState.SetBool(RunningKey, true);
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                SessionState.SetBool(RunningKey, false);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
