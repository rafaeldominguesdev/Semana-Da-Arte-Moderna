using UnityEditor;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Ferramenta de Editor para configurar a física da sala do museu.
    /// Acesse pelo menu: MuseumModerna → Configurar Física da Sala
    ///
    /// O que faz:
    ///  - Adiciona BoxCollider no chão, paredes e teto (cria teto se não existir)
    ///  - Garante que o Player tenha CharacterController configurado
    ///  - Exibe instruções de configuração no painel
    /// </summary>
    public class RoomPhysicsSetup : EditorWindow
    {
        // Nomes possíveis para cada superfície (PT e EN)
        private static readonly string[] FloorNames  = { "Chao", "Chão", "Floor", "Piso", "Ground" };
        private static readonly string[] WallNames   = { "Parede", "Wall", "Muro" };
        private static readonly string[] CeilingNames = { "Teto", "Ceiling", "Techo", "Telhado" };
        private static readonly string[] PlayerNames  = { "Player", "Jogador", "PlayerCamera", "Main Camera", "Camera" };

        private Vector2 _scroll;

        // ─── Menu ─────────────────────────────────────────────────────────────

        [MenuItem("MuseumModerna/Configurar Física da Sala")]
        public static void ShowWindow()
        {
            GetWindow<RoomPhysicsSetup>("Room Physics Setup").minSize = new Vector2(420, 500);
        }

        // ─── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(8);
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField("Configurar Física da Sala", title);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Esta ferramenta adiciona BoxColliders no chão, paredes e teto, " +
                "e configura o CharacterController no Player.\n\n" +
                "Nomeie os GameObjects da sala com: Chão/Floor, Parede/Wall, Teto/Ceiling.",
                MessageType.Info);

            EditorGUILayout.Space(12);

            // ── Botão principal ──
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Configurar Tudo Automaticamente", GUILayout.Height(40)))
                RunFullSetup();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Ou configure por partes:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Colisores no Chão"))    AddCollidersToSurface(FloorNames,   "Chão");
            if (GUILayout.Button("+ Colisores nas Paredes")) AddCollidersToSurface(WallNames,   "Paredes");
            if (GUILayout.Button("+ Colisores no Teto"))    AddCollidersOrCreateCeiling();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Configurar Player (CharacterController)"))
                SetupPlayer();

            EditorGUILayout.Space(16);
            DrawInstructions();

            EditorGUILayout.EndScrollView();
        }

        // ─── Setup Completo ───────────────────────────────────────────────────

        private static void RunFullSetup()
        {
            int changes = 0;

            changes += AddCollidersToSurface(FloorNames,   "Chão");
            changes += AddCollidersToSurface(WallNames,    "Paredes");
            changes += AddCollidersOrCreateCeiling();
            changes += SetupPlayer();

            if (changes > 0)
                Debug.Log($"[MuseumModerna] Setup concluído: {changes} componentes adicionados/configurados.");
            else
                Debug.LogWarning("[MuseumModerna] Nenhum objeto encontrado. Verifique os nomes na hierarquia.");

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog(
                    "MuseumModerna — Física Configurada",
                    $"{changes} componentes adicionados/configurados.\n\n" +
                    "Verifique o Console para detalhes.\n\n" +
                    "Lembre-se de salvar a cena (Ctrl+S).",
                    "OK");
        }

        // ─── Superfícies ──────────────────────────────────────────────────────

        private static int AddCollidersToSurface(string[] names, string label)
        {
            int count = 0;
            GameObject[] all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject go in all)
            {
                foreach (string name in names)
                {
                    if (go.name.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    {
                        Undo.RecordObject(go, $"Add Collider {label}");
                        if (go.GetComponent<Collider>() == null)
                        {
                            BoxCollider bc = Undo.AddComponent<BoxCollider>(go);
                            Debug.Log($"[MuseumModerna] BoxCollider adicionado em '{go.name}' ({label}).");
                            count++;
                        }
                        else
                        {
                            Debug.Log($"[MuseumModerna] '{go.name}' já possui Collider. Pulando.");
                        }
                        break;
                    }
                }
            }

            if (count == 0)
                Debug.LogWarning($"[MuseumModerna] Nenhum objeto '{label}' encontrado na cena. " +
                    $"Nomes buscados: {string.Join(", ", names)}");

            return count;
        }

        private static int AddCollidersOrCreateCeiling()
        {
            // Tenta encontrar teto existente
            int found = AddCollidersToSurface(CeilingNames, "Teto");
            if (found > 0) return found;

            // Cria um teto baseado nos bounds do chão
            GameObject floor = FindByNames(FloorNames);
            if (floor == null)
            {
                Debug.LogWarning("[MuseumModerna] Teto e Chão não encontrados. Crie-os manualmente.");
                return 0;
            }

            Bounds floorBounds = GetObjectBounds(floor);

            float ceilingY   = floorBounds.center.y + 3f;  // 3 metros acima do centro do chão
            float thickness  = 0.2f;

            GameObject ceiling = new GameObject("Teto");
            Undo.RegisterCreatedObjectUndo(ceiling, "Create Ceiling");

            BoxCollider bc = ceiling.AddComponent<BoxCollider>();
            ceiling.transform.position = new Vector3(
                floorBounds.center.x,
                ceilingY,
                floorBounds.center.z);

            bc.size = new Vector3(floorBounds.size.x, thickness, floorBounds.size.z);

            // Adiciona um MeshRenderer invisível para visualização no Editor
            ceiling.AddComponent<MeshFilter>();
            MeshRenderer mr = ceiling.AddComponent<MeshRenderer>();
            mr.enabled = false;

            Debug.Log($"[MuseumModerna] Teto criado em Y={ceilingY:F2}, " +
                $"tamanho {floorBounds.size.x:F2}x{floorBounds.size.z:F2}.");
            return 1;
        }

        // ─── Player Setup ─────────────────────────────────────────────────────

        private static int SetupPlayer()
        {
            GameObject player = FindByNames(PlayerNames);
            if (player == null)
            {
                Debug.LogWarning("[MuseumModerna] Player não encontrado. " +
                    "Nomes buscados: " + string.Join(", ", PlayerNames));
                return 0;
            }

            int count = 0;

            // Desabilita LockPosition se existir
            LockPosition lp = player.GetComponent<LockPosition>();
            if (lp != null)
            {
                Undo.RecordObject(lp, "Disable LockPosition");
                lp.enabled = false;
                Debug.Log($"[MuseumModerna] LockPosition desabilitado em '{player.name}'.");
                count++;
            }

            // Adiciona CharacterController se não existir
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = Undo.AddComponent<CharacterController>(player);
                cc.height = 1.8f;
                cc.radius = 0.3f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.3f;
                Debug.Log($"[MuseumModerna] CharacterController adicionado em '{player.name}'.");
                count++;
            }
            else
            {
                Debug.Log($"[MuseumModerna] '{player.name}' já possui CharacterController.");
            }

            return count;
        }

        // ─── Utilitários ──────────────────────────────────────────────────────

        private static GameObject FindByNames(string[] names)
        {
            foreach (string n in names)
            {
                GameObject go = GameObject.Find(n);
                if (go != null) return go;
            }
            return null;
        }

        private static Bounds GetObjectBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one * 10f);

            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // ─── Instruções ───────────────────────────────────────────────────────

        private void DrawInstructions()
        {
            GUIStyle header = new GUIStyle(EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Configuração Manual no Inspector", header);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "PLAYER  (GameObject com Camera + scripts)\n" +
                "  • CharacterController  ← adicionado automaticamente acima\n" +
                "    - Height: 1.8   Radius: 0.3   Center Y: 0.9\n" +
                "    - Slope Limit: 45   Step Offset: 0.3\n" +
                "  • HeadGazeMovement   ← arraste GyroscopeController + cameraTransform\n" +
                "  • PlayerController   ← já existente\n" +
                "  • GyroscopeController ← já existente (requer Camera)\n" +
                "  • LockPosition       ← REMOVA ou deixe desabilitado\n\n" +
                "CHÃO  (Plane ou Cube achatado)\n" +
                "  • BoxCollider  (adicionado automaticamente)\n\n" +
                "PAREDES  (Cube ou mesh da sala)\n" +
                "  • BoxCollider em cada parede\n\n" +
                "TETO\n" +
                "  • BoxCollider  (criado automaticamente se não existir)",
                MessageType.None);

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "DICA: Se a sala vier de um modelo .blend importado,\n" +
                "selecione o asset do modelo → aba Model → ative 'Generate Colliders'.\n" +
                "Isso cria MeshColliders automáticos em toda a geometria.",
                MessageType.Info);
        }
    }
}
