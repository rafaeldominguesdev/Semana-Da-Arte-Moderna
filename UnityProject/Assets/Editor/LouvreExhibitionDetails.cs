using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>
    /// Expografia original: perfis dourados, bronze fluido e mobiliário curvo.
    /// Fichas preservadas; o bronze recebe colliders que acompanham sua nova forma.
    /// </summary>
    public static class LouvreExhibitionDetails
    {
        private const string MeshFolder = "Assets/MuseumStyle/Meshes";

        public static void Apply(Transform root, Material gold, Material stone, Material dark, Material velvet)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            EnsureFolder("Assets/MuseumStyle");
            EnsureFolder(MeshFolder);
            var previous = root.Find("Expografia_Curva");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var details = Child(root, "Expografia_Curva");
            var sceneObjects = Resources.FindObjectsOfTypeAll<Transform>()
                .Where(t => t.gameObject.scene.IsValid() && t.gameObject.scene == root.gameObject.scene && !t.IsChildOf(root))
                .ToArray();
            var exhibits = Resources.FindObjectsOfTypeAll<PaintingExhibit>()
                .Where(e => e.gameObject.scene == root.gameObject.scene && e.PaintingData != null && !e.transform.IsChildOf(root))
                .OrderBy(e => e.PaintingData.uniqueId).ToArray();

            int panels = 0, sculptures = 0;
            var bronze = BronzeMaterial(gold);
            foreach (var exhibit in exhibits)
            {
                string id = exhibit.PaintingData.uniqueId;
                var legacy = sceneObjects.FirstOrDefault(t => t != null && t.name == "Expografia_" + id);
                if (legacy != null)
                {
                    Frame(details, exhibit, legacy, gold, dark);
                    var slab = sceneObjects.FirstOrDefault(t => t != null && t.name == "Moldura_" + id);
                    HideRenderers(slab);
                    panels++;
                }
                if (id.StartsWith("escultura_cenografica_", StringComparison.Ordinal))
                {
                    Sculpture(details, exhibit, sculptures++, bronze, stone, gold, sceneObjects);
                }
            }
            foreach (var source in sceneObjects.Where(t => t != null && t.name.StartsWith("Banco_", StringComparison.Ordinal)))
                if (source.GetComponentInChildren<PaintingExhibit>(true) == null)
                    source.gameObject.SetActive(false);

            var seatMesh = SaveMesh("CurvedBench_Seat", ArcSolid(2.05f, -48, 48, 24,
                RoundedProfile(.59f, .145f, .04f, .47f)));
            var backMesh = SaveMesh("CurvedBench_Back", ArcSolid(2.32f, -47, 47, 24,
                RoundedProfile(.14f, .34f, .045f, .705f)));
            var trimMesh = SaveMesh("CurvedBench_Trim", ArcSolid(2.05f, -48, 48, 24,
                RoundedProfile(.58f, .035f, .012f, .385f)));
            var legMesh = SaveMesh("CurvedBench_TurnedLeg", Lathe(new[]
            {
                new Vector2(0, 0), new Vector2(.046f, 0), new Vector2(.055f, .022f),
                new Vector2(.035f, .055f), new Vector2(.027f, .25f),
                new Vector2(.042f, .34f), new Vector2(.042f, .4f), new Vector2(0, .4f)
            }, 12));
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    Bench(details, new Vector3(x * 5, 0, z * 5), velvet, dark, gold,
                        seatMesh, backMesh, trimMesh, legMesh);

            AssetDatabase.SaveAssets();
            Debug.Log($"[LouvreStyle] Expografia: {panels} molduras perfiladas, {sculptures} bronzes originais e 4 bancos curvos. Fichas preservadas.");
        }

        private static void Frame(Transform details, PaintingExhibit exhibit, Transform legacy,
            Material gold, Material dark)
        {
            foreach (Transform child in legacy)
                if (child.name.StartsWith("Moldura_", StringComparison.Ordinal) ||
                    child.name == "Placa" || child.name == "Luminaria") HideRenderers(child);
            var oldFrame = exhibit.GetComponent<PaintingFrame>();
            if (oldFrame != null) oldFrame.enabled = false;
            foreach (Transform child in exhibit.transform)
                if (child.name.StartsWith("Frame_", StringComparison.Ordinal)) HideRenderers(child);

            string id = exhibit.PaintingData.uniqueId;
            var frame = Child(details, "Moldura_perfilada_" + id);
            frame.SetPositionAndRotation(legacy.position, legacy.rotation);
            float width = Mathf.Abs(exhibit.transform.lossyScale.x);
            float height = Mathf.Abs(exhibit.transform.lossyScale.y);
            if (width < .1f || height < .1f) { width = 1.4f; height = 1.1f; }
            // A sequência raio/profundidade modela filete, gola côncava e toro externo.
            // Há uma abertura real no centro: a geometria nunca cobre a tela original.
            var profile = new[]
            {
                new Vector2(.008f, .018f), new Vector2(.012f, .040f),
                new Vector2(.022f, .052f), new Vector2(.035f, .052f),
                new Vector2(.047f, .038f), new Vector2(.054f, .027f),
                new Vector2(.069f, .020f), new Vector2(.082f, .023f),
                new Vector2(.099f, .045f), new Vector2(.107f, .069f),
                new Vector2(.122f, .081f), new Vector2(.139f, .075f),
                new Vector2(.150f, .057f), new Vector2(.153f, .016f),
                new Vector2(.149f, -.020f)
            };
            MeshObject(frame, "Ouro_com_gola_e_toro", SaveMesh("ProfileFrame_" + id,
                FrameRing(width, height, .025f, profile, 5)), gold);
            MeshObject(frame, "Filete_de_sombra", SaveMesh("FrameInset_" + id,
                FrameRing(width, height, .022f, new[]
                {
                    new Vector2(.000f, .019f), new Vector2(.008f, .025f),
                    new Vector2(.011f, .025f), new Vector2(.013f, .019f)
                }, 5)), dark);
            var plaque = MeshObject(frame, "Cartela_com_cantos_suaves",
                SaveMesh("RoundedPlaque_" + id, RoundedPanel(width, .30f, .018f, .025f)), dark);
            plaque.transform.localPosition = new Vector3(0, -height / 2 - .28f, .022f);
            // Substitui a luminária-caixa por uma haste cilíndrica discreta.
            var lamp = MeshObject(frame, "Luminaria_tubular",
                SaveMesh("PictureLight_" + id, Lathe(new[]
                {
                    new Vector2(0, -.23f), new Vector2(.022f, -.23f),
                    new Vector2(.027f, -.215f), new Vector2(.027f, .215f),
                    new Vector2(.022f, .23f), new Vector2(0, .23f)
                }, 12)), gold);
            lamp.transform.localPosition = new Vector3(0, height / 2 + .28f, .23f);
            lamp.transform.localRotation = Quaternion.Euler(0, 0, 90);
            // Dois materiais por quadro, com culling local; evitamos um único lote do museu inteiro.
            CombineChildren(frame, gold, "Frame_Gold_" + id);
            CombineChildren(frame, dark, "Frame_Dark_" + id);
        }

        private static void Sculpture(Transform details, PaintingExhibit exhibit, int index,
            Material bronze, Material stone, Material gold, Transform[] sceneObjects)
        {
            // O componente PaintingExhibit permanece no objeto original. Depois de construir
            // a forma, seu proxy de interação substitui apenas os colliders dos volumes antigos.
            HideRenderers(exhibit.transform);
            var sculpture = Child(details, "Bronze_original_" + (index + 1));
            sculpture.SetPositionAndRotation(exhibit.transform.position, exhibit.transform.rotation);
            MeshObject(sculpture, "Forma_continua", SaveMesh("OriginalBronze_" + index,
                FlowForm(index, 34, 18)), bronze);
            if (index == 1)
            {
                var second = MeshObject(sculpture, "Contracurva", SaveMesh("OriginalBronze_Counterform",
                    FlowForm(3, 28, 14)), bronze);
                second.transform.localPosition = new Vector3(-.115f, .035f, .018f);
                second.transform.localScale = new Vector3(.62f, .83f, .7f);
                second.transform.localRotation = Quaternion.Euler(0, 0, -9);
                CombineChildren(sculpture, bronze, "OriginalBronze_Double");
            }
            SculptureGazeProxy(exhibit, sculpture, index);

            string pedestalName = new[] { "Pedestal_Moema", "Pedestal_Eva", "Pedestal_Cristo" }[Mathf.Min(index, 2)];
            var oldPedestal = sceneObjects.FirstOrDefault(t => t != null && t.name == pedestalName);
            HideRenderers(oldPedestal);
            var pedestal = Child(details, "Soclo_circular_" + (index + 1));
            pedestal.position = oldPedestal != null ? oldPedestal.position : exhibit.transform.position - Vector3.up * 1.17f;
            var profile = new[]
            {
                new Vector2(0, .01f), new Vector2(.345f, .01f), new Vector2(.365f, .035f),
                new Vector2(.365f, .09f), new Vector2(.315f, .135f),
                new Vector2(.255f, .17f), new Vector2(.23f, .22f),
                new Vector2(.205f, .98f), new Vector2(.23f, 1.025f),
                new Vector2(.30f, 1.055f), new Vector2(.33f, 1.085f),
                new Vector2(.33f, 1.135f), new Vector2(.31f, 1.16f), new Vector2(0, 1.16f)
            };
            MeshObject(pedestal, "Pierre_tournee", SaveMesh("Sculpture_Plinth", Lathe(profile, 40, true)), stone);
            MeshObject(pedestal, "Anneau_de_bronze", SaveMesh("Sculpture_PlinthRing", Lathe(new[]
            {
                new Vector2(.235f, 1.023f), new Vector2(.252f, 1.03f),
                new Vector2(.257f, 1.046f), new Vector2(.25f, 1.06f), new Vector2(.24f, 1.062f)
            }, 40)), gold);
            var obstacle = pedestal.gameObject.AddComponent<CapsuleCollider>();
            obstacle.center = new Vector3(0, .58f, 0); obstacle.radius = .33f; obstacle.height = 1.16f;
        }

        private static void SculptureGazeProxy(PaintingExhibit exhibit, Transform visual, int index)
        {
            const string proxyName = "Louvre_GazeProxy";
            var oldProxy = exhibit.transform.Find(proxyName);
            if (oldProxy != null) UnityEngine.Object.DestroyImmediate(oldProxy.gameObject);

            // Transforma todas as formas visuais para o espaço do PaintingExhibit, incluindo
            // a contracurva. O proxy sem Renderer pertence à ficha e alcança também o topo.
            var combine = visual.GetComponentsInChildren<MeshFilter>()
                .Select(f => new CombineInstance
                {
                    mesh = f.sharedMesh,
                    transform = exhibit.transform.worldToLocalMatrix * f.transform.localToWorldMatrix
                }).ToArray();
            if (combine.Length == 0) throw new InvalidOperationException("Escultura sem malha para interação.");
            var mesh = new Mesh(); mesh.CombineMeshes(combine, true, true);
            var savedMesh = SaveMesh("OriginalBronze_GazeProxy_" + index, mesh);
            var proxy = Child(exhibit.transform, proxyName);
            proxy.gameObject.layer = exhibit.gameObject.layer;
            var collider = proxy.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = savedMesh;
            collider.convex = false;

            foreach (Transform child in exhibit.transform)
                if (child.name == "Massa_bronze" || child.name == "Plano_bronze" || child.name == "Coroamento")
                    foreach (var oldCollider in child.GetComponents<Collider>()) oldCollider.enabled = false;
        }

        private static void Bench(Transform details, Vector3 position, Material velvet, Material dark,
            Material gold, Mesh seatMesh, Mesh backMesh, Mesh trimMesh, Mesh legMesh)
        {
            var bench = Child(details, "Banco_curvo_" + position.x + "_" + position.z);
            bench.position = position;
            // A face côncava olha para o centro do salão; eixos X e Z ficam livres.
            bench.rotation = Quaternion.LookRotation(position.normalized);
            MeshObject(bench, "Assento_estofado", seatMesh, velvet);
            var back = MeshObject(bench, "Encosto_curvo", backMesh, velvet);
            back.transform.localPosition = new Vector3(0, 0, .27f);
            MeshObject(bench, "Base_escura", trimMesh, dark);
            foreach (float degrees in new[] { -40f, 0f, 40f })
                foreach (float radius in new[] { 1.84f, 2.25f })
                {
                    float a = degrees * Mathf.Deg2Rad;
                    var leg = MeshObject(bench, "Pe_torneado", legMesh, gold);
                    leg.transform.localPosition = new Vector3(Mathf.Sin(a) * radius, 0, Mathf.Cos(a) * radius - 2.05f);
                }
            // Cinco volumes simples acompanham a curva sem atravancar o espaço côncavo.
            foreach (float degrees in new[] { -38.4f, -19.2f, 0, 19.2f, 38.4f })
            {
                float a = degrees * Mathf.Deg2Rad;
                var obstacle = Child(bench, "Colisao_do_banco");
                obstacle.localPosition = new Vector3(Mathf.Sin(a) * 2.05f, .455f, Mathf.Cos(a) * 2.05f - 2.05f);
                obstacle.localRotation = Quaternion.Euler(0, degrees, 0);
                var collider = obstacle.gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(.75f, .91f, .68f);
            }
            CombineChildren(bench, velvet, "Bench_Upholstery_" + position.x + "_" + position.z);
            CombineChildren(bench, gold, "Bench_Brass_" + position.x + "_" + position.z);
        }

        private static Mesh FlowForm(int variant, int heightSegments, int sides)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int j = 0; j <= heightSegments; j++)
            {
                float t = (float)j / heightSegments;
                float bulb = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), .68f);
                float x = variant == 0 ? .15f * Mathf.Sin(t * Mathf.PI * 1.7f) :
                    variant == 1 ? .16f * Mathf.Sin(t * Mathf.PI * 2.05f) :
                    variant == 2 ? -.16f * Mathf.Sin(t * Mathf.PI * 1.45f) : .23f * Mathf.Sin(t * Mathf.PI);
                float z = variant == 0 ? .055f * Mathf.Sin(t * Mathf.PI * 2) : .07f * Mathf.Sin(t * Mathf.PI * 1.5f);
                float y = -.01f + t * (variant == 2 ? 1.20f : 1.28f);
                float width = (.16f + .115f * Mathf.Sin(t * Mathf.PI)) * bulb;
                float depth = (.074f + .042f * Mathf.Cos(t * Mathf.PI)) * bulb;
                if (variant == 1) { width *= .71f; depth *= 1.13f; }
                if (variant == 2) { width *= 1.15f; depth *= .83f; }
                float twist = (t * 1.05f + variant * .25f) * Mathf.PI;
                for (int k = 0; k <= sides; k++)
                {
                    float angle = k * Mathf.PI * 2 / sides;
                    float px = Mathf.Cos(angle) * Mathf.Max(.001f, width);
                    float pz = Mathf.Sin(angle) * Mathf.Max(.001f, depth);
                    vertices.Add(new Vector3(x + px * Mathf.Cos(twist) - pz * Mathf.Sin(twist), y,
                        z + px * Mathf.Sin(twist) + pz * Mathf.Cos(twist)));
                }
            }
            GridTriangles(triangles, heightSegments, sides);
            return Finish(vertices, triangles);
        }

        private static Mesh FrameRing(float width, float height, float cornerRadius, Vector2[] profile, int cornerSegments)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            int sides = 4 * (cornerSegments + 1);
            foreach (var p in profile)
            {
                foreach (var point in RoundedOutline(width, height, cornerRadius, cornerSegments, p.x))
                    vertices.Add(new Vector3(point.x, point.y, p.y));
            }
            GridTriangles(triangles, profile.Length - 1, sides);
            return Finish(vertices, triangles);
        }

        private static List<Vector2> RoundedOutline(float width, float height, float radius, int segments, float expansion = 0)
        {
            var points = new List<Vector2>();
            for (int corner = 0; corner < 4; corner++)
            {
                float centerX = (corner == 0 || corner == 3 ? 1 : -1) * (width / 2 - radius);
                float centerY = (corner < 2 ? 1 : -1) * (height / 2 - radius);
                for (int segment = 0; segment <= segments; segment++)
                {
                    float angle = (corner * 90f + segment * 90f / segments) * Mathf.Deg2Rad;
                    points.Add(new Vector2(centerX + (radius + expansion) * Mathf.Cos(angle),
                        centerY + (radius + expansion) * Mathf.Sin(angle)));
                }
            }
            points.Add(points[0]); return points;
        }

        private static Mesh RoundedPanel(float width, float height, float depth, float radius)
        {
            var outline = RoundedOutline(width, height, radius, 5);
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            foreach (float z in new[] { -depth / 2, depth / 2 })
                foreach (var p in outline) vertices.Add(new Vector3(p.x, p.y, z));
            int count = outline.Count;
            // Front/back have independent vertices, keeping the rounded edge highlight crisp.
            for (int j = 0; j < count - 1; j++)
            {
                AddQuad(triangles, j, j + 1, count + j + 1, count + j);
            }
            int front = vertices.Count; vertices.Add(new Vector3(0, 0, depth / 2));
            int firstFront = vertices.Count;
            foreach (var p in outline) vertices.Add(new Vector3(p.x, p.y, depth / 2));
            int back = vertices.Count; vertices.Add(new Vector3(0, 0, -depth / 2));
            int firstBack = vertices.Count;
            foreach (var p in outline) vertices.Add(new Vector3(p.x, p.y, -depth / 2));
            for (int j = 0; j < count - 1; j++)
            {
                triangles.Add(front); triangles.Add(firstFront + j); triangles.Add(firstFront + j + 1);
                triangles.Add(back); triangles.Add(firstBack + j + 1); triangles.Add(firstBack + j);
            }
            return Finish(vertices, triangles);
        }

        private static Mesh Lathe(Vector2[] profile, int sides, bool fluted = false)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            foreach (var point in profile)
                for (int j = 0; j <= sides; j++)
                {
                    float angle = j * Mathf.PI * 2 / sides;
                    float radius = point.x;
                    if (fluted && point.y >= .22f && point.y <= .98f)
                        radius -= .008f * (.5f + .5f * Mathf.Cos(angle * 20));
                    vertices.Add(new Vector3(radius * Mathf.Cos(angle), point.y, radius * Mathf.Sin(angle)));
                }
            GridTriangles(triangles, profile.Length - 1, sides);
            return Finish(vertices, triangles);
        }

        private static Vector2[] RoundedProfile(float width, float height, float radius, float centerY)
        {
            var points = RoundedOutline(width, height, radius, 3);
            return points.Select(p => new Vector2(p.x, p.y + centerY)).ToArray();
        }

        private static Mesh ArcSolid(float radius, float startDegrees, float endDegrees, int segments, Vector2[] profile)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            int ring = profile.Length;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(startDegrees, endDegrees, (float)i / segments) * Mathf.Deg2Rad;
                foreach (var p in profile)
                    vertices.Add(new Vector3(Mathf.Sin(a) * (radius + p.x), p.y, Mathf.Cos(a) * (radius + p.x) - radius));
            }
            GridTriangles(triangles, segments, ring - 1);
            // Tampas em leque: o perfil é convexo e os extremos não ficam abertos.
            foreach (int end in new[] { 0, segments })
            {
                int first = end * ring;
                int center = vertices.Count; Vector3 mean = Vector3.zero;
                for (int j = 0; j < ring - 1; j++) mean += vertices[first + j];
                vertices.Add(mean / (ring - 1));
                for (int j = 0; j < ring - 1; j++)
                {
                    triangles.Add(center);
                    triangles.Add(first + (end == 0 ? j : j + 1));
                    triangles.Add(first + (end == 0 ? j + 1 : j));
                }
            }
            return Finish(vertices, triangles);
        }

        private static void GridTriangles(List<int> indices, int rows, int columns)
        {
            int stride = columns + 1;
            for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                {
                    int a = row * stride + column;
                    AddQuad(indices, a, a + stride, a + stride + 1, a + 1);
                }
        }

        private static void AddQuad(List<int> indices, int a, int b, int c, int d)
        {
            indices.Add(a); indices.Add(b); indices.Add(c);
            indices.Add(a); indices.Add(c); indices.Add(d);
        }

        private static Mesh Finish(List<Vector3> vertices, List<int> triangles)
        {
            if (vertices.Any(v => !float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z)))
                throw new InvalidOperationException("A expografia contém coordenadas de malha inválidas.");
            var mesh = new Mesh { indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = MeshFolder + "/LouvreDetail_" + name + ".asset";
            mesh.name = name;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing); return existing;
        }

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.isStatic = true; return go.transform;
        }

        private static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material material)
        {
            var t = Child(parent, name);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = t.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
            return t.gameObject;
        }

        private static void HideRenderers(Transform target)
        {
            if (target == null) return;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }

        private static void CombineChildren(Transform group, Material material, string assetName)
        {
            var filters = group.GetComponentsInChildren<MeshFilter>()
                .Where(f => f.GetComponent<MeshRenderer>().sharedMaterial == material).ToArray();
            if (filters.Length < 2) return;
            var combine = filters.Select(f => new CombineInstance
            {
                mesh = f.sharedMesh, transform = group.worldToLocalMatrix * f.transform.localToWorldMatrix
            }).ToArray();
            var mesh = new Mesh(); mesh.CombineMeshes(combine, true, true);
            MeshObject(group, material.name + "_agrupado", SaveMesh(assetName, mesh), material);
            foreach (var source in filters) UnityEngine.Object.DestroyImmediate(source.gameObject);
        }

        private static Material BronzeMaterial(Material source)
        {
            EnsureFolder("Assets/MuseumStyle/Materials");
            const string path = "Assets/MuseumStyle/Materials/Louvre_OriginalBronze.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(source); AssetDatabase.CreateAsset(material, path); }
            material.color = new Color(.35f, .205f, .09f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .76f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .53f);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material); return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, split));
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
    }
}
