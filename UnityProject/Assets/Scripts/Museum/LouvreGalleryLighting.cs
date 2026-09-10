using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Luz difusa de galeria sem depender dos probes da arquitetura anterior.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class LouvreGalleryLighting : MonoBehaviour
    {
        private void Awake() => ApplyDaylight();

        public static void ApplyDaylight()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.82f, .80f, .74f);
            RenderSettings.ambientIntensity = 1;
            RenderSettings.fog = false;
            var diffuse = new SphericalHarmonicsL2();
            diffuse.AddAmbientLight(new Color(.72f, .72f, .68f));
            RenderSettings.ambientProbe = diffuse;
            QualitySettings.antiAliasing = 2;
        }
    }
}
