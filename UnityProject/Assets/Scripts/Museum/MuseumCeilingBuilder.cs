using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Constrói um teto de museu clássico em runtime usando primitivas Unity.
    /// Estilo: teto caixotado (coffered ceiling) com molduras, frisos e medallion central.
    ///
    /// Como usar:
    ///   Adicione a qualquer GameObject ativo na cena (ex: GameManager).
    ///   O teto e o piso de segurança são criados automaticamente no Start.
    /// </summary>
    public class MuseumCeilingBuilder : MonoBehaviour
    {
        [Header("Dimensões da Sala")]
        [Tooltip("Largura total da sala em metros")]
        [SerializeField] private float roomWidth  = 14f;

        [Tooltip("Profundidade total da sala em metros")]
        [SerializeField] private float roomDepth  = 14f;

        [Tooltip("Altura do teto em metros")]
        [SerializeField] private float ceilingHeight = 3.8f;

        [Header("Caixotões (Coffers)")]
        [Tooltip("Quantidade de caixotões na horizontal")]
        [SerializeField] private int cofferCols = 4;

        [Tooltip("Quantidade de caixotões na vertical")]
        [SerializeField] private int cofferRows = 4;

        [Tooltip("Largura das vigas/molduras entre os caixotões")]
        [SerializeField] private float beamWidth = 0.28f;

        [Tooltip("Profundidade das vigas que pendem do teto")]
        [SerializeField] private float beamDepth = 0.12f;

        [Tooltip("Recesso dos painéis (quanto o fundo do caixotão fica pra cima)")]
        [SerializeField] private float cofferRecess = 0.05f;

        [Header("Friso / Crown Molding")]
        [Tooltip("Altura do friso nas bordas do teto")]
        [SerializeField] private float moldingHeight = 0.22f;

        [Tooltip("Profundidade do friso")]
        [SerializeField] private float moldingDepth  = 0.14f;

        [Header("Cores")]
        [Tooltip("Cor da laje principal do teto")]
        [SerializeField] private Color ceilingColor  = new Color(0.94f, 0.91f, 0.85f);

        [Tooltip("Cor das vigas e molduras")]
        [SerializeField] private Color beamColor     = new Color(0.98f, 0.97f, 0.93f);

        [Tooltip("Cor do fundo dos caixotões (levemente mais escuro)")]
        [SerializeField] private Color cofferColor   = new Color(0.82f, 0.79f, 0.73f);

        [Tooltip("Cor do medallion central")]
        [SerializeField] private Color medallionColor = new Color(0.96f, 0.92f, 0.80f);

        [Header("Piso de Segurança")]
        [Tooltip("Adiciona collisor invisível no chão para o player nunca atravessar")]
        [SerializeField] private bool addSafetyFloor = true;

        [Tooltip("Y do piso de segurança (0 = chão da sala)")]
        [SerializeField] private float floorY = 0f;

        // ─── Materiais ────────────────────────────────────────────────────────

        private Material _matCeiling;
        private Material _matBeam;
        private Material _matCoffer;
        private Material _matMedallion;

        // ─── Ciclo de Vida ────────────────────────────────────────────────────

        private void Start()
        {
            CreateMaterials();
            BuildCeiling();
            if (addSafetyFloor) AddSafetyFloor();
        }

        // ─── Materiais ────────────────────────────────────────────────────────

        private void CreateMaterials()
        {
            _matCeiling   = CreateMat("Ceil_Base",      ceilingColor,   0f,   0.35f);
            _matBeam      = CreateMat("Ceil_Beam",      beamColor,      0f,   0.55f);
            _matCoffer    = CreateMat("Ceil_Coffer",    cofferColor,    0f,   0.25f);
            _matMedallion = CreateMat("Ceil_Medallion", medallionColor, 0.1f, 0.7f);
        }

        private static Material CreateMat(string name, Color color, float metallic, float smoothness)
        {
            var mat = new Material(Shader.Find("Standard")) { name = name, color = color };
            mat.SetFloat("_Metallic",   metallic);
            mat.SetFloat("_Glossiness", smoothness);
            return mat;
        }

        // ─── Construção do Teto ───────────────────────────────────────────────

        private void BuildCeiling()
        {
            GameObject root = new GameObject("Teto_Museu");
            root.transform.position = Vector3.zero;

            float y = ceilingHeight;

            // ── 1. Laje principal ─────────────────────────────────────────────
            float slabThick = 0.18f;
            MakeCube(root, "Laje",
                new Vector3(0f, y + slabThick * 0.5f, 0f),
                new Vector3(roomWidth + 0.4f, slabThick, roomDepth + 0.4f),
                _matCeiling);

            // ── 2. Vigas da grade de caixotões ────────────────────────────────
            BuildCofferGrid(root, y);

            // ── 3. Friso (crown molding) nas 4 bordas ─────────────────────────
            BuildCrownMolding(root, y);

            // ── 4. Medallion central ──────────────────────────────────────────
            BuildMedallion(root, y);

            // ── 5. Collisor do teto (impede sair pelo topo) ───────────────────
            GameObject ceilCol = new GameObject("Teto_Collider");
            ceilCol.transform.SetParent(root.transform);
            ceilCol.transform.position = new Vector3(0f, y + 0.05f, 0f);
            var bc = ceilCol.AddComponent<BoxCollider>();
            bc.size = new Vector3(roomWidth + 1f, 0.1f, roomDepth + 1f);

            Debug.Log($"[MuseumModerna] Teto construído em Y={y:F2} ({cofferCols}x{cofferRows} caixotões).");
        }

        // ─── Grade de Caixotões ───────────────────────────────────────────────

        private void BuildCofferGrid(GameObject parent, float y)
        {
            float halfW = roomWidth  * 0.5f;
            float halfD = roomDepth  * 0.5f;

            // Espaço disponível para painéis e vigas
            float usableW = roomWidth  - beamWidth; // subtrai meia viga em cada extremidade
            float usableD = roomDepth  - beamWidth;

            float stepW = usableW / cofferCols;
            float stepD = usableD / cofferRows;

            float panelW = stepW - beamWidth;
            float panelD = stepD - beamWidth;

            float hangY = y - beamDepth * 0.5f; // centro das vigas

            // Vigas horizontais (ao longo de X, separando linhas)
            for (int r = 0; r <= cofferRows; r++)
            {
                float z = -halfD + beamWidth * 0.5f + r * stepD;
                MakeCube(parent, $"Viga_H_{r}",
                    new Vector3(0f, hangY, z),
                    new Vector3(roomWidth, beamDepth, beamWidth),
                    _matBeam);
            }

            // Vigas verticais (ao longo de Z, separando colunas)
            for (int c = 0; c <= cofferCols; c++)
            {
                float x = -halfW + beamWidth * 0.5f + c * stepW;
                MakeCube(parent, $"Viga_V_{c}",
                    new Vector3(x, hangY, 0f),
                    new Vector3(beamWidth, beamDepth, roomDepth),
                    _matBeam);
            }

            // Fundo dos caixotões (painéis recuados)
            float panelY = y - cofferRecess;

            for (int r = 0; r < cofferRows; r++)
            {
                for (int c = 0; c < cofferCols; c++)
                {
                    float cx = -halfW + beamWidth + beamWidth * 0.5f + c * stepW + panelW * 0.5f;
                    float cz = -halfD + beamWidth + beamWidth * 0.5f + r * stepD + panelD * 0.5f;

                    // Recalcula centro
                    cx = -halfW + beamWidth * 0.5f + c * stepW + beamWidth * 0.5f + panelW * 0.5f;
                    cz = -halfD + beamWidth * 0.5f + r * stepD + beamWidth * 0.5f + panelD * 0.5f;

                    MakeCube(parent, $"Painel_{r}_{c}",
                        new Vector3(cx, panelY, cz),
                        new Vector3(panelW, 0.02f, panelD),
                        _matCoffer);
                }
            }
        }

        // ─── Friso / Crown Molding ────────────────────────────────────────────

        private void BuildCrownMolding(GameObject parent, float y)
        {
            float hw = roomWidth  * 0.5f;
            float hd = roomDepth  * 0.5f;
            float moldY = y - moldingHeight * 0.5f;

            // Friso Norte
            MakeCube(parent, "Friso_N",
                new Vector3(0f, moldY, hd - moldingDepth * 0.5f),
                new Vector3(roomWidth, moldingHeight, moldingDepth),
                _matBeam);

            // Friso Sul
            MakeCube(parent, "Friso_S",
                new Vector3(0f, moldY, -hd + moldingDepth * 0.5f),
                new Vector3(roomWidth, moldingHeight, moldingDepth),
                _matBeam);

            // Friso Leste
            MakeCube(parent, "Friso_E",
                new Vector3(hw - moldingDepth * 0.5f, moldY, 0f),
                new Vector3(moldingDepth, moldingHeight, roomDepth - moldingDepth * 2f),
                _matBeam);

            // Friso Oeste
            MakeCube(parent, "Friso_O",
                new Vector3(-hw + moldingDepth * 0.5f, moldY, 0f),
                new Vector3(moldingDepth, moldingHeight, roomDepth - moldingDepth * 2f),
                _matBeam);

            // Friso secundário (chanfro interno) — linha fina logo abaixo
            float trim = 0.04f;
            float trimY = y - moldingHeight - trim * 0.5f;
            float trimD = moldingDepth * 0.6f;

            string[] trimNames = { "Trim_N", "Trim_S", "Trim_E", "Trim_O" };
            Vector3[] trimPos =
            {
                new Vector3(0f,  trimY,  hd - trimD * 0.5f),
                new Vector3(0f,  trimY, -hd + trimD * 0.5f),
                new Vector3( hw - trimD * 0.5f, trimY, 0f),
                new Vector3(-hw + trimD * 0.5f, trimY, 0f),
            };
            Vector3[] trimScale =
            {
                new Vector3(roomWidth, trim, trimD),
                new Vector3(roomWidth, trim, trimD),
                new Vector3(trimD, trim, roomDepth - trimD * 2f),
                new Vector3(trimD, trim, roomDepth - trimD * 2f),
            };

            for (int i = 0; i < 4; i++)
                MakeCube(parent, trimNames[i], trimPos[i], trimScale[i], _matCeiling);
        }

        // ─── Medallion Central ────────────────────────────────────────────────

        private void BuildMedallion(GameObject parent, float y)
        {
            float mY = y - 0.015f;

            // Disco principal
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Medallion_Disco";
            disc.transform.SetParent(parent.transform);
            disc.transform.position = new Vector3(0f, mY, 0f);
            disc.transform.localScale = new Vector3(1.2f, 0.015f, 1.2f);
            disc.GetComponent<MeshRenderer>().material = _matMedallion;
            Object.Destroy(disc.GetComponent<CapsuleCollider>());

            // Anel externo
            BuildMedallionRing(parent, y, 0.58f, 0.06f, 0.025f);
            // Anel interno
            BuildMedallionRing(parent, y, 0.38f, 0.04f, 0.018f);

            // Ponto central (onde penderia o lustre)
            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            center.name = "Medallion_Centro";
            center.transform.SetParent(parent.transform);
            center.transform.position = new Vector3(0f, mY - 0.04f, 0f);
            center.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
            center.GetComponent<MeshRenderer>().material = _matMedallion;
            Object.Destroy(center.GetComponent<SphereCollider>());
        }

        private void BuildMedallionRing(GameObject parent, float y, float radius, float width, float height)
        {
            int segments = 24;
            float angleStep = 360f / segments;
            float segLength = 2f * Mathf.PI * radius / segments + 0.005f;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;

                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"Ring_{radius:F2}_{i}";
                seg.transform.SetParent(parent.transform);
                seg.transform.position = new Vector3(x, y - height * 0.5f - 0.01f, z);
                seg.transform.LookAt(new Vector3(0f, y - height * 0.5f - 0.01f, 0f));
                seg.transform.localScale = new Vector3(width, height, segLength);
                seg.GetComponent<MeshRenderer>().material = _matMedallion;
                Object.Destroy(seg.GetComponent<BoxCollider>());
            }
        }

        // ─── Piso de Segurança ────────────────────────────────────────────────

        private void AddSafetyFloor()
        {
            GameObject floor = new GameObject("Piso_Seguranca");
            floor.transform.position = new Vector3(0f, floorY, 0f);
            var bc = floor.AddComponent<BoxCollider>();
            bc.size = new Vector3(roomWidth * 4f, 0.1f, roomDepth * 4f);
            bc.center = Vector3.zero;
            Debug.Log($"[MuseumModerna] Piso de segurança adicionado em Y={floorY}.");
        }

        // ─── Helper ───────────────────────────────────────────────────────────

        private static GameObject MakeCube(GameObject parent, string name,
            Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().material = mat;
            Object.Destroy(go.GetComponent<BoxCollider>());
            return go;
        }
    }
}
