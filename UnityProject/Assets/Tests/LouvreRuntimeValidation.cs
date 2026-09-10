#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Checks the saved museum using real colliders and renders its actual geometry.</summary>
    public sealed class LouvreRuntimeValidation : MonoBehaviour
    {
        private readonly List<string> runtimeErrors = new List<string>();
        private Camera eye;
        private PlayerController player;
        private GazeDwellInteraction gaze;

        [Serializable]
        private sealed class GeometryMetrics
        {
            public int activeCameras;
            public int activeRenderers;
            public int renderedMeshInstances;
            public long renderedTriangles;
            public long sharedMeshVertices;
            public int uniqueMeshes;
            public int uniqueMaterials;
            public int activeLights;
            public int shadowCastingLights;
            public int colliders;
            public int exhibitRaycastsPassed;
            public int doorwaysPassed;
            public string captureSettings = "Mono Editor camera renders at 1600 x 1000, 62-degree vertical FOV and 4x MSAA; these settings are for visual inspection, not a mobile performance measurement.";
            public SculptureHitEvidence[] sculptureTopHits;
            public string scope = "Active saved scene geometry. Triangles count only each renderer's assigned submeshes, including its static-batch offset; vertices count once per shared mesh. These are not measured FPS, device draw calls or GPU time.";
        }

        [Serializable]
        private sealed class SculptureHitEvidence
        {
            public string exhibitId;
            public string collider;
            public Vector3 localSurfaceAim;
            public Vector3 localPhysicsHit;
            public float rayDistance;
        }

        private void Awake() => Application.logMessageReceived += RecordError;
        private void OnDestroy() => Application.logMessageReceived -= RecordError;
        private void RecordError(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                runtimeErrors.Add(message);
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            var routine = Tests();
            while (true)
            {
                object next;
                try
                {
                    if (!routine.MoveNext()) break;
                    next = routine.Current;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    UnityEditor.EditorApplication.Exit(1);
                    yield break;
                }
                yield return next;
            }
            Debug.Log("[LouvreValidation] Scene integration and real scene captures: PASS (mono Editor preview; physical VR remains untested)");
            UnityEditor.EditorApplication.Exit(0);
        }

        private IEnumerator Tests()
        {
            gaze = FindAnyObjectByType<GazeDwellInteraction>();
            Require(gaze != null, "The saved scene has the gaze interaction camera");
            eye = gaze.GetComponent<Camera>();
            player = FindAnyObjectByType<PlayerController>();
            Require(eye != null && player != null, "Player and camera are available");
            foreach (var gyro in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) gyro.enabled = false;
            foreach (var movement in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) movement.enabled = false;
            foreach (var anchor in FindObjectsByType<PlayerSpawnAnchor>(FindObjectsSortMode.None)) anchor.enabled = false;
            foreach (var locked in FindObjectsByType<LockPosition>(FindObjectsSortMode.None)) locked.enabled = false;

            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled).ToArray();
            Require(cameras.Length == 1 && cameras[0] == eye,
                "Exactly one active player camera; found " + string.Join(", ", cameras.Select(c => c.name)));
            var architecture = GameObject.Find("Museum_LouvreStyle");
            Require(architecture != null && architecture.GetComponentsInChildren<MeshFilter>().Length > 0,
                "The redesigned architecture is saved and active");

            var exhibits = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            Require(exhibits.Length == 28, "28 associated exhibits; found " + exhibits.Length);
            var ids = new HashSet<string>();
            foreach (var exhibit in exhibits)
            {
                Require(exhibit.PaintingData != null && ids.Add(exhibit.PaintingData.uniqueId), "Exhibit data is present and unique: " + exhibit.name);
                var center = exhibit.transform.position;
                var front = exhibit.transform.forward;
                if (exhibit.PaintingData.categoria == "Escultura cenográfica")
                {
                    center = SculptureAim(exhibit);
                    front = Vector3.left;
                }
                if (exhibit.PaintingData.categoria == "Vitrine documental")
                {
                    center += Vector3.up * 1.1f;
                    front = new Vector3(0, .3f, 1).normalized;
                }
                Position(center + front * 2.5f, center);
                Physics.SyncTransforms();
                var found = gaze.RaycastExhibit(new Ray(eye.transform.position, eye.transform.forward));
                Require(found == exhibit, "Unobstructed approach to " + exhibit.name + "; first exhibit: " + (found != null ? found.name : "solid/no hit"));
            }
            Debug.Log("[LouvreValidation] Raycasts to all 28 exhibits: PASS");
            var sculptureTopHits = exhibits.Where(exhibit => exhibit.PaintingData.categoria == "Escultura cenográfica")
                .Select(CheckSculptureTop).ToArray();
            Require(sculptureTopHits.Length == 3, "Upper surfaces of all three sculptures were checked");
            Debug.Log("[LouvreValidation] Real surface hits at local sculpture height 1.1 m: PASS");

            var first = exhibits.First(e => e.PaintingData.uniqueId == "homem_amarelo");
            gaze.SetPanelsEnabled(true);
            Position(first.transform.position + first.transform.forward * 2.5f, first.transform.position);
            yield return new WaitForSecondsRealtime(.5f);
            Require(gaze.Focus.Current == first.PaintingData, "Looking at the repositioned exhibition still opens its correct guide");
            gaze.SetPanelsEnabled(false);
            var ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.HidePaintingPanel();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;

            // Cross each portal at head height and with a pedestrian clearance radius.
            // These short segments avoid treating a sculpture displayed deeper in a room as a blocked doorway.
            Position(new Vector3(0, 1.7f, -16), new Vector3(0, 1.7f, 0));
            Physics.SyncTransforms();
            CheckDoorway("painting gallery", new Vector3(0, 1.7f, 7), new Vector3(0, 1.7f, 11.4f));
            CheckDoorway("sculpture gallery", new Vector3(7, 1.7f, 0), new Vector3(11.4f, 1.7f, 0));
            CheckDoorway("literature gallery", new Vector3(-7, 1.7f, 0), new Vector3(-11.4f, 1.7f, 0));
            CheckDoorway("foyer", new Vector3(0, 1.7f, -7), new Vector3(0, 1.7f, -11.4f));
            foreach (var point in new[]
            {
                new Vector3(0, 1.7f, -8), new Vector3(-7, 1.7f, -6),
                new Vector3(-4.5f, 1.7f, 11.8f), new Vector3(11.5f, 1.7f, -5),
                new Vector3(-11.5f, 1.7f, -5), new Vector3(0, 1.7f, -16)
            })
            {
                var floor = Physics.RaycastAll(point, Vector3.down, 2.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    .FirstOrDefault(hit => !BelongsToPlayer(hit.collider) && hit.normal.y > .8f);
                Require(floor.collider != null, "A solid walkable floor supports " + point);
            }
            Debug.Log("[LouvreValidation] Four doorway clearance tests and six floor positions: PASS");

            var metrics = MeasureGeometry();
            metrics.activeCameras = cameras.Length;
            metrics.exhibitRaycastsPassed = exhibits.Length;
            metrics.doorwaysPassed = 4;
            metrics.sculptureTopHits = sculptureTopHits;
            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllText(Path.Combine(OutputDirectory, "louvre-geometry-metrics.json"), JsonUtility.ToJson(metrics, true) + "\n");
            Debug.Log("[LouvreValidation] Geometry: " + metrics.activeRenderers + " renderers, " + metrics.renderedTriangles +
                " triangles, " + metrics.uniqueMaterials + " unique materials, " + metrics.activeLights + " active lights");

            Capture("louvre-hall.png", new Vector3(0, 1.7f, -8), new Vector3(0, 3, 4));
            Capture("louvre-hall-corner.png", new Vector3(-7, 1.7f, -6), new Vector3(3, 3, 4));
            Capture("louvre-painting-gallery.png", new Vector3(-4.5f, 1.7f, 11.8f), new Vector3(3, 2.8f, 22));
            Capture("louvre-sculpture-gallery.png", new Vector3(11.5f, 1.7f, -5), new Vector3(19, 2.5f, 3));
            Capture("louvre-ceiling.png", new Vector3(-2, 1.7f, -3), new Vector3(0, 7, 0));
            var frameCenter = first.transform.position - Vector3.up * .10f;
            Capture("louvre-frame-detail.png", frameCenter + first.transform.forward * 1.8f + first.transform.right * .28f, frameCenter);
            yield return null;
            Require(runtimeErrors.Count == 0, "No runtime errors: " + string.Join(" | ", runtimeErrors));
        }

        private void CheckDoorway(string label, Vector3 from, Vector3 to)
        {
            var delta = to - from;
            var hits = Physics.SphereCastAll(from, .25f, delta.normalized, delta.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Where(hit => !BelongsToPlayer(hit.collider)).ToArray();
            Require(hits.Length == 0, "Doorway to " + label + " is blocked by " + string.Join(", ", hits.Select(hit => hit.collider.name)));
        }

        private bool BelongsToPlayer(Collider collider) => collider != null && collider.transform.IsChildOf(player.transform);

        public static Vector3 SculptureAim(PaintingExhibit exhibit)
        {
            var collider = exhibit.GetComponentsInChildren<MeshCollider>().FirstOrDefault(c => c.enabled && c.sharedMesh != null);
            if (collider == null) return exhibit.transform.position + Vector3.up * .5f;
            var vertices = collider.sharedMesh.vertices;
            var triangles = collider.sharedMesh.triangles;
            float best = float.PositiveInfinity;
            Vector3 target = Vector3.zero;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a).normalized;
                if (normal.x > -.2f) continue;
                var point = (a + b + c) / 3;
                float score = Mathf.Abs(point.y - .5f) * 100 + point.x;
                if (score < best) { best = score; target = point; }
            }
            Require(!float.IsInfinity(best), "A left-facing sculpture surface is available");
            return collider.transform.TransformPoint(target);
        }

        private SculptureHitEvidence CheckSculptureTop(PaintingExhibit exhibit)
        {
            var proxy = exhibit.GetComponentsInChildren<MeshCollider>()
                .FirstOrDefault(collider => collider.enabled && !collider.isTrigger && collider.sharedMesh != null);
            Require(proxy != null, "Sculpture has an active mesh collider: " + exhibit.name);
            const float localHeight = 1.1f;
            var mesh = proxy.sharedMesh;
            Require(mesh.bounds.min.y < localHeight && mesh.bounds.max.y > localHeight,
                "The sculpture surface reaches the upper test section: " + exhibit.name);

            // A ray aimed at (0, 1.1, 0) can miss a curved bronze. Intersect the actual
            // mesh triangles with that height and aim at the leftmost section midpoint.
            // This selects an actual face point, not a guessed point inside its AABB.
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var section = new List<Vector3>(3);
            var surface = new Vector3(float.PositiveInfinity, localHeight, 0);
            for (int triangle = 0; triangle < triangles.Length; triangle += 3)
            {
                section.Clear();
                for (int edge = 0; edge < 3; edge++)
                {
                    var a = vertices[triangles[triangle + edge]];
                    var b = vertices[triangles[triangle + (edge + 1) % 3]];
                    if ((a.y <= localHeight && b.y > localHeight) || (b.y <= localHeight && a.y > localHeight))
                        section.Add(Vector3.Lerp(a, b, (localHeight - a.y) / (b.y - a.y)));
                }
                if (section.Count != 2) continue;
                var midpoint = (section[0] + section[1]) * .5f;
                if (midpoint.x < surface.x) surface = midpoint;
            }
            Require(!float.IsInfinity(surface.x), "A real upper triangle section exists: " + exhibit.name);
            var target = proxy.transform.TransformPoint(surface);
            var approach = proxy.transform.TransformDirection(Vector3.left).normalized;
            Position(target + approach * 2.5f, target);
            Physics.SyncTransforms();
            var ray = new Ray(eye.transform.position, eye.transform.forward);
            var hit = Physics.RaycastAll(ray, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                .Where(candidate => !BelongsToPlayer(candidate.collider)).OrderBy(candidate => candidate.distance).FirstOrDefault();
            Require(hit.collider == proxy && gaze.RaycastExhibit(ray) == exhibit,
                "Upper bronze surface selects its own guide: " + exhibit.name + "; hit " + (hit.collider != null ? hit.collider.name : "none"));
            var localHit = proxy.transform.InverseTransformPoint(hit.point);
            Require(Mathf.Abs(localHit.y - localHeight) < .005f, "The physics hit is on the tested upper section");
            Debug.Log("[LouvreValidation] Sculpture upper hit: " + exhibit.PaintingData.uniqueId + " at " + localHit.ToString("F4"));
            return new SculptureHitEvidence
            {
                exhibitId = exhibit.PaintingData.uniqueId,
                collider = hit.collider.name,
                localSurfaceAim = surface,
                localPhysicsHit = localHit,
                rayDistance = hit.distance
            };
        }

        private static GeometryMetrics MeasureGeometry()
        {
            var result = new GeometryMetrics();
            var meshes = new HashSet<Mesh>();
            var materials = new HashSet<Material>();
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(renderer => renderer.enabled).ToArray();
            result.activeRenderers = renderers.Length;
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials) if (material != null) materials.Add(material);
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : (renderer as SkinnedMeshRenderer)?.sharedMesh;
                if (mesh == null) continue;
                if (meshes.Add(mesh)) result.sharedMeshVertices += mesh.vertexCount;
                result.renderedMeshInstances++;
                // Static batching shares a combined mesh across many renderers. Each renderer
                // owns a slice starting at subMeshStartIndex, not the entire combined mesh.
                int firstSubmesh = renderer is MeshRenderer meshRenderer ? meshRenderer.subMeshStartIndex : 0;
                int endSubmesh = Mathf.Min(mesh.subMeshCount, firstSubmesh + renderer.sharedMaterials.Length);
                for (int submesh = firstSubmesh; submesh < endSubmesh; submesh++)
                    if (mesh.GetTopology(submesh) == UnityEngine.MeshTopology.Triangles)
                        result.renderedTriangles += mesh.GetIndexCount(submesh) / 3;
            }
            result.uniqueMeshes = meshes.Count;
            result.uniqueMaterials = materials.Count;
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None).Where(light => light.enabled).ToArray();
            result.activeLights = lights.Length;
            result.shadowCastingLights = lights.Count(light => light.shadows != LightShadows.None);
            result.colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None).Count(collider => collider.enabled && !collider.isTrigger);
            return result;
        }

        private void Position(Vector3 position, Vector3 target)
        {
            player.transform.position += position - eye.transform.position;
            eye.transform.rotation = Quaternion.LookRotation(target - position);
        }

        private static string OutputDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts"));

        private void Capture(string filename, Vector3 position, Vector3 target)
        {
            const int width = 1600, height = 1000;
            Position(position, target);
            var previousTarget = eye.targetTexture;
            var previousActive = RenderTexture.active;
            var previousAspect = eye.aspect;
            var previousFov = eye.fieldOfView;
            var render = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                eye.targetTexture = render;
                eye.aspect = width / (float)height;
                eye.fieldOfView = 62;
                eye.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(OutputDirectory, filename), texture.EncodeToPNG());
            }
            finally
            {
                eye.targetTexture = previousTarget;
                eye.aspect = previousAspect;
                eye.fieldOfView = previousFov;
                RenderTexture.active = previousActive;
                Destroy(texture);
                render.Release();
                Destroy(render);
            }
            Debug.Log("[LouvreValidation] Actual scene capture: " + filename);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("[LouvreValidation] " + message);
        }
    }
}
#endif
