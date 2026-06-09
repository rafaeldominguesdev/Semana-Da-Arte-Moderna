#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace MuseumModerna
{
    /// <summary>
    /// Script de Editor para criar automaticamente todos os ScriptableObjects
    /// de dados das obras da Semana de Arte Moderna de 1922.
    ///
    /// Como usar:
    ///   No menu do Unity: Tools → Semana Arte Moderna → Criar Dados das Obras
    /// </summary>
    public static class PaintingDataCreator
    {
        [MenuItem("Tools/Semana Arte Moderna/Criar Dados das Obras")]
        public static void CreateAllPaintingData()
        {
            // Garante que a pasta existe
            string folder = "Assets/Resources/PaintingData";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "PaintingData");
                Debug.Log("[MuseumModerna] Pasta PaintingData criada.");
            }

            CreateOrUpdatePainting(folder, new PaintingDataTemplate
            {
                fileName    = "Abaporu",
                title       = "O Abaporu",
                artist      = "Tarsila do Amaral",
                year        = 1928,
                movement    = "Antropofagismo",
                uniqueId    = "abaporu",
                description =
                    "Pintado como presente de aniversário para Oswald de Andrade, " +
                    "O Abaporu tornou-se o símbolo máximo do Modernismo brasileiro. " +
                    "A palavra 'Abaporu' vem do tupi: 'homem que come gente'. " +
                    "A figura tem membros desproporcionais, com pés enormes fincados " +
                    "na terra e uma pequena cabeça sob um sol escaldante.",
                funFact =
                    "Esta obra inspirou o Manifesto Antropófago de Oswald de Andrade " +
                    "e hoje é a pintura brasileira mais valiosa do mundo."
            });

            CreateOrUpdatePainting(folder, new PaintingDataTemplate
            {
                fileName    = "AEstudante",
                title       = "A Estudante",
                artist      = "Anita Malfatti",
                year        = 1915,
                movement    = "Expressionismo",
                uniqueId    = "a_estudante",
                description =
                    "Pintada durante os estudos de Anita Malfatti na Alemanha, " +
                    "A Estudante demonstra a influência do Expressionismo alemão " +
                    "com suas cores vibrantes e distorções intencionais da forma. " +
                    "A obra foi exibida na polêmica exposição de 1917, que dividiu " +
                    "opiniões e marcou o início do Modernismo no Brasil.",
                funFact =
                    "Monteiro Lobato criticou ferozmente esta obra no artigo " +
                    "'Paranoia ou Mistificação?', o que paradoxalmente impulsionou " +
                    "o interesse pelo Modernismo brasileiro."
            });

            CreateOrUpdatePainting(folder, new PaintingDataTemplate
            {
                fileName    = "Operarios",
                title       = "Operários",
                artist      = "Tarsila do Amaral",
                year        = 1933,
                movement    = "Modernismo Social",
                uniqueId    = "operarios",
                description =
                    "Uma das obras mais importantes do período social de Tarsila, " +
                    "Operários retrata um mosaico de rostos de trabalhadores de diversas " +
                    "etnias — reflexo da São Paulo industrializada dos anos 1930. " +
                    "A composição geométrica e as cores terrosas remetem às raízes " +
                    "do povo brasileiro.",
                funFact =
                    "Tarsila doou esta obra ao governo do estado de São Paulo, " +
                    "onde está exposta até hoje no Palácio Boa Vista em Campos do Jordão."
            });

            CreateOrUpdatePainting(folder, new PaintingDataTemplate
            {
                fileName    = "CarnavalMadureira",
                title       = "Carnaval em Madureira",
                artist      = "Di Cavalcanti",
                year        = 1924,
                movement    = "Modernismo",
                uniqueId    = "carnaval_madureira",
                description =
                    "Di Cavalcanti foi um dos principais artistas da Semana de Arte " +
                    "Moderna de 1922 e até criou a capa do catálogo do evento. " +
                    "Em Carnaval em Madureira, retrata a alegria popular do Rio de Janeiro " +
                    "com traços expressivos e cores quentes.",
                funFact =
                    "Di Cavalcanti foi o principal organizador da Semana de 1922 " +
                    "ao lado de Mário de Andrade e Graça Aranha."
            });

            CreateOrUpdatePainting(folder, new PaintingDataTemplate
            {
                fileName    = "ANegra",
                title       = "A Negra",
                artist      = "Tarsila do Amaral",
                year        = 1923,
                movement    = "Pau-Brasil",
                uniqueId    = "a_negra",
                description =
                    "Considerada um autorretrato simbólico, A Negra é uma das primeiras " +
                    "obras do período Pau-Brasil de Tarsila. A figura monumental, de lábios " +
                    "proeminentes e seios expostos, foi inspirada nas memórias de infância " +
                    "da artista na fazenda da família.",
                funFact =
                    "A Negra foi exibida em Paris em 1926, onde causou grande impressão " +
                    "entre os artistas modernistas europeus, incluindo Fernand Léger."
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MuseumModerna] Todos os dados das obras foram criados/atualizados com sucesso!");
            EditorUtility.DisplayDialog(
                "Semana Arte Moderna",
                "5 obras criadas em Assets/Resources/PaintingData/\n\nAgora configure os quadros na cena!",
                "OK"
            );
        }

        private static void CreateOrUpdatePainting(string folder, PaintingDataTemplate template)
        {
            string path = $"{folder}/{template.fileName}.asset";

            PaintingInfo asset = AssetDatabase.LoadAssetAtPath<PaintingInfo>(path);
            bool isNew = asset == null;

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<PaintingInfo>();
            }

            asset.title       = template.title;
            asset.artist      = template.artist;
            asset.year        = template.year;
            asset.movement    = template.movement;
            asset.uniqueId    = template.uniqueId;
            asset.description = template.description;
            asset.funFact     = template.funFact;

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[MuseumModerna] Criado: {path}");
            }
            else
            {
                EditorUtility.SetDirty(asset);
                Debug.Log($"[MuseumModerna] Atualizado: {path}");
            }
        }

        private struct PaintingDataTemplate
        {
            public string fileName;
            public string title;
            public string artist;
            public int    year;
            public string movement;
            public string uniqueId;
            public string description;
            public string funFact;
        }
    }
}
#endif
