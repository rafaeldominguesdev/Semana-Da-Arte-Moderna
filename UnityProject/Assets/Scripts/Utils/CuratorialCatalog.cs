#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MuseumModerna
{
    public static class CuratorialCatalog
    {
        [Serializable] public class Entry
        {
            public string uniqueId, title, artist, periodo, tecnica, categoria, description, resumo;
            public string relacao_com_a_semana_de_1922, obra_historica_ou_reinterpretacao, fontes, movement, funFact;
            public int year;
        }
        [Serializable] private class Catalog { public Entry[] items; }
        public static Entry[] Read()
        {
            var json = Resources.Load<TextAsset>("MuseumCatalog");
            if (json == null) throw new InvalidOperationException("MuseumCatalog.json não encontrado.");
            return JsonUtility.FromJson<Catalog>(json.text).items;
        }
        public static void Apply(PaintingInfo info)
        {
            foreach (var entry in Read())
                if (entry.uniqueId == info.uniqueId)
                {
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(entry), info);
                    EditorUtility.SetDirty(info);
                    return;
                }
        }
        [MenuItem("MuseumModerna/Visita guiada/Atualizar fichas curatoriais")]
        public static void Install()
        {
            const string folder = "Assets/Resources/PaintingData";
            foreach (var entry in Read())
            {
                string path = folder + "/" + entry.uniqueId + ".asset";
                var info = AssetDatabase.LoadAssetAtPath<PaintingInfo>(path);
                bool create = info == null;
                if (create) info = ScriptableObject.CreateInstance<PaintingInfo>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(entry), info);
                if (create) AssetDatabase.CreateAsset(info, path);
                else EditorUtility.SetDirty(info);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
