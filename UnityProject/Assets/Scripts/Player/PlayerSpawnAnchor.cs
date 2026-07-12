using System.Collections;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Garante que o player nasça dentro da sala, mesmo que a sala esteja
    /// posicionada em qualquer altura no mundo.
    ///
    /// Estratégia:
    ///   1. Tenta raycast de cima para baixo para achar o chão com collider.
    ///   2. Se não achar collider, usa os bounds de todos os Renderers da cena.
    ///   3. Teleporta o player para cima do chão encontrado.
    ///   4. Invalida o save se a posição salva estava no vazio.
    ///
    /// Como usar:
    ///   Adicione ao mesmo GameObject do PlayerController (ou qualquer objeto ativo).
    ///   Executa automaticamente no Start.
    /// </summary>
    [DefaultExecutionOrder(200)] // roda depois do SaveSystem (order 0) e HeadGazeMovement
    public class PlayerSpawnAnchor : MonoBehaviour
    {
        [Header("Ajustes de Spawn")]
        [Tooltip("Altura acima do chão onde o player vai aparecer")]
        [SerializeField] private float heightAboveFloor = 1.0f;

        [Tooltip("Se o player estiver mais que este valor abaixo do chão da sala, é teleportado")]
        [SerializeField] private float fallThreshold = 3f;

        [Tooltip("Layers consideradas chão (deixe 'Everything' para pegar qualquer coisa)")]
        [SerializeField] private LayerMask floorLayers = ~0;

        // ─── Ciclo de Vida ────────────────────────────────────────────────────

        private IEnumerator Start()
        {
            // Espera 1 frame para SaveSystem carregar a posição salva primeiro
            yield return null;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null) yield break;

            Vector3 floorPosition = FindRoomFloor(player.transform.position);

            float playerY  = player.transform.position.y;
            float floorY   = floorPosition.y;

            bool isInVoid = playerY < floorY - fallThreshold;

            if (isInVoid)
            {
                Debug.Log($"[MuseumModerna] Player no vazio (Y={playerY:F1}, chão em Y={floorY:F1}). Teleportando...");

                // Limpa save inválido para não carregar posição ruim na próxima vez
                SaveSystem save = FindAnyObjectByType<SaveSystem>();
                save?.DeleteSave();

                // Teleporta para cima do chão da sala
                Vector3 spawnPos = new Vector3(floorPosition.x, floorY + heightAboveFloor, floorPosition.z);
                TeleportPlayer(player, spawnPos);

                Debug.Log($"[MuseumModerna] Player reposicionado para {spawnPos}.");
            }
        }

        // ─── Encontrar o Chão ─────────────────────────────────────────────────

        private Vector3 FindRoomFloor(Vector3 playerPos)
        {
            // Método 1: raycast de Y muito alto para baixo — funciona se houver colliders
            Vector3 rayOrigin = new Vector3(playerPos.x, 5000f, playerPos.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10000f, floorLayers))
            {
                Debug.Log($"[MuseumModerna] Chão encontrado via raycast em Y={hit.point.y:F2}");
                return new Vector3(hit.point.x, hit.point.y, hit.point.z);
            }

            // Método 2: bounds de todos os Renderers — funciona mesmo sem colliders
            return FindFloorFromRenderers();
        }

        private Vector3 FindFloorFromRenderers()
        {
            Renderer[] all = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            if (all.Length == 0)
            {
                Debug.LogWarning("[MuseumModerna] Nenhum Renderer encontrado. Usando Y=0.");
                return Vector3.zero;
            }

            // Ignora câmera e UI
            Bounds combined = new Bounds();
            bool first = true;

            foreach (Renderer r in all)
            {
                if (r == null) continue;
                if (r.GetComponent<Camera>()         != null) continue;
                if (r.GetComponentInParent<Camera>() != null) continue;
                if (r is CanvasRenderer)                      continue;

                if (first) { combined = r.bounds; first = false; }
                else         combined.Encapsulate(r.bounds);
            }

            if (first)
            {
                Debug.LogWarning("[MuseumModerna] Só encontrei Renderers de câmera/UI. Usando Y=0.");
                return Vector3.zero;
            }

            Debug.Log($"[MuseumModerna] Chão encontrado via Renderer bounds: " +
                      $"center={combined.center}, min.y={combined.min.y:F2}");

            return new Vector3(combined.center.x, combined.min.y, combined.center.z);
        }

        // ─── Teleportar ───────────────────────────────────────────────────────

        private static void TeleportPlayer(PlayerController player, Vector3 position)
        {
            CharacterController cc = player.GetComponent<CharacterController>();

            if (cc != null) cc.enabled = false;
            player.transform.position = position;
            if (cc != null) cc.enabled = true;
        }
    }
}
