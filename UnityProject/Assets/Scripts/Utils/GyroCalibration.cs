using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseumModerna
{
    /// <summary>
    /// Utilitário estático para salvar e carregar o offset de calibração do giroscópio.
    /// Os dados são persistidos via PlayerPrefs entre sessões.
    ///
    /// Chaves do PlayerPrefs utilizadas:
    ///   MuseumModerna_CalibX  — Componente X do Quaternion de offset
    ///   MuseumModerna_CalibY  — Componente Y do Quaternion de offset
    ///   MuseumModerna_CalibZ  — Componente Z do Quaternion de offset
    ///   MuseumModerna_CalibW  — Componente W do Quaternion de offset
    /// </summary>
    public static class GyroCalibration
    {
        // Prefixo para todas as chaves do PlayerPrefs deste módulo
        private const string PREFIX = "MuseumModerna_Calib";

        /// <summary>
        /// Retorna o Quaternion de calibração salvo.
        /// Se não houver calibração salva, retorna Quaternion.identity.
        /// </summary>
        public static Quaternion GetCalibrationOffset()
        {
            if (!PlayerPrefs.HasKey(PREFIX + "W"))
            {
                return Quaternion.identity;
            }

            return new Quaternion(
                PlayerPrefs.GetFloat(PREFIX + "X", 0f),
                PlayerPrefs.GetFloat(PREFIX + "Y", 0f),
                PlayerPrefs.GetFloat(PREFIX + "Z", 0f),
                PlayerPrefs.GetFloat(PREFIX + "W", 1f)
            );
        }

        /// <summary>
        /// Salva o Quaternion de calibração no PlayerPrefs e persiste em disco.
        /// </summary>
        /// <param name="offset">Quaternion representando o offset de calibração atual.</param>
        public static void SaveCalibrationOffset(Quaternion offset)
        {
            PlayerPrefs.SetFloat(PREFIX + "X", offset.x);
            PlayerPrefs.SetFloat(PREFIX + "Y", offset.y);
            PlayerPrefs.SetFloat(PREFIX + "Z", offset.z);
            PlayerPrefs.SetFloat(PREFIX + "W", offset.w);
            PlayerPrefs.Save(); // Persiste imediatamente em disco
        }

        /// <summary>
        /// Remove todos os dados de calibração salvos (útil para reset de fábrica).
        /// </summary>
        public static void ResetCalibration()
        {
            PlayerPrefs.DeleteKey(PREFIX + "X");
            PlayerPrefs.DeleteKey(PREFIX + "Y");
            PlayerPrefs.DeleteKey(PREFIX + "Z");
            PlayerPrefs.DeleteKey(PREFIX + "W");
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Registra o callback para resetar calibração ao carregar uma nova cena.
        /// Chame este método no Start() de um objeto persistente (ex: GameManager).
        /// </summary>
        public static void RegisterSceneResetCallback()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Remove o callback de reset ao trocar de cena (para evitar vazamento de memória).
        /// </summary>
        public static void UnregisterSceneResetCallback()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Callback chamado pelo SceneManager quando uma cena é carregada
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Opcional: descomente a linha abaixo se quiser resetar calibração ao trocar de cena
            // ResetCalibration();
            Debug.Log($"[MuseumModerna] Cena '{scene.name}' carregada. Calibração mantida.");
        }
    }
}
