#if UNITY_EDITOR
using UnityEditor;
namespace MuseumModerna
{
    /// <summary>Mantém o menu antigo apontando ao catálogo curatorial revisado.</summary>
    public static class PaintingDataCreator
    {
        [MenuItem("Tools/Semana Arte Moderna/Criar Dados das Obras")]
        public static void CreateAllPaintingData() => CuratorialCatalog.Install();
    }
}
#endif
