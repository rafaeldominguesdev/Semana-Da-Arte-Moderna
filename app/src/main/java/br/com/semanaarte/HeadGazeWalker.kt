package br.com.semanaarte

import kotlin.math.*

/**
 * Lógica de movimento do player no museu VR.
 *
 * O player se move para frente (na direção do yaw atual) quando
 * [isLookingDown] é verdadeiro — mecânica de "head-gaze walking".
 *
 * O movimento é restrito por:
 *   - Limites do museu (bounding box)
 *   - Detecção de proximidade com quadros
 *
 * @param museumRadius Raio do museu circular em metros. O player não pode sair deste raio.
 * @param walkSpeed Velocidade de caminhada em m/s
 * @param galleryRadius Raio do círculo de quadros em metros
 */
class HeadGazeWalker(
    private val museumRadius: Float = 6.0f,
    private val walkSpeed: Float = 2.5f,
    private val galleryRadius: Float = 8.0f
) {

    // ─── Estado do Player ──────────────────────────────────────────────────

    /** Posição X do player no mundo (metros) */
    var playerX: Float = 0f
        private set

    /** Posição Z do player no mundo (metros) */
    var playerZ: Float = 0f
        private set

    /** Velocidade atual (com inércia) */
    private var currentSpeed: Float = 0f

    /** Fator de aceleração */
    private val acceleration: Float = 8f

    /** Fator de desaceleração (inércia) */
    private val deceleration: Float = 5f

    // ─── Detecção de Quadros ───────────────────────────────────────────────

    /** Raio de detecção de proximidade com um quadro */
    private val paintingDetectionRadius: Float = 2.5f

    /** ID do quadro atualmente próximo (-1 se nenhum) */
    var nearPaintingIndex: Int = -1
        private set

    // ─── Atualização por Frame ─────────────────────────────────────────────

    /**
     * Atualiza a posição do player. Deve ser chamado a cada frame do renderer.
     *
     * @param isLookingDown True se o giroscópio detecta que o usuário olha para baixo.
     * @param yawDegrees Ângulo de rotação horizontal da câmera (0=frente, 90=direita, etc).
     * @param deltaTime Tempo em segundos desde o último frame.
     * @param artworks Lista de obras para detectar proximidade.
     */
    fun update(
        isLookingDown: Boolean,
        yawDegrees: Float,
        deltaTime: Float,
        artworks: List<Artwork>
    ) {
        // Atualiza velocidade com inércia (aceleração/desaceleração suave)
        val targetSpeed = if (isLookingDown) walkSpeed else 0f
        val factor = if (isLookingDown) acceleration else deceleration
        currentSpeed += (targetSpeed - currentSpeed) * factor * deltaTime

        // Evita velocidades residuais muito pequenas
        if (currentSpeed < 0.01f) currentSpeed = 0f

        if (currentSpeed > 0f) {
            // Converte yaw para vetor de direção no plano XZ
            val yawRad = Math.toRadians(yawDegrees.toDouble())
            val dirX = sin(yawRad).toFloat()
            val dirZ = -cos(yawRad).toFloat() // -Z = frente no OpenGL

            val newX = playerX + dirX * currentSpeed * deltaTime
            val newZ = playerZ + dirZ * currentSpeed * deltaTime

            // Aplica movimento apenas se dentro dos limites do museu
            val distFromCenter = sqrt(newX * newX + newZ * newZ)
            if (distFromCenter < museumRadius) {
                playerX = newX
                playerZ = newZ
            } else {
                // Desliza ao longo da parede (normaliza e restringe ao limite)
                val scale = museumRadius / distFromCenter * 0.98f
                playerX = newX * scale
                playerZ = newZ * scale
            }
        }

        // Detecta proximidade com quadros
        updateNearPainting(artworks)
    }

    /**
     * Detecta qual quadro está mais próximo do player.
     * Atualiza [nearPaintingIndex] (-1 se nenhum no raio).
     */
    private fun updateNearPainting(artworks: List<Artwork>) {
        var closestIndex = -1
        var closestDist = paintingDetectionRadius

        artworks.forEachIndexed { index, artwork ->
            // Posição do quadro no círculo da galeria
            val angleRad = Math.toRadians(artwork.angleInGallery.toDouble())
            val paintingX = (galleryRadius * sin(angleRad)).toFloat()
            val paintingZ = -(galleryRadius * cos(angleRad)).toFloat()

            val dist = sqrt(
                (playerX - paintingX).pow(2) + (playerZ - paintingZ).pow(2)
            )

            if (dist < closestDist) {
                closestDist = dist
                closestIndex = index
            }
        }

        nearPaintingIndex = closestIndex
    }

    /**
     * Teletransporta o player para a posição inicial (centro da sala).
     */
    fun resetToCenter() {
        playerX = 0f
        playerZ = 0f
        currentSpeed = 0f
    }

    /**
     * Retorna a velocidade atual do player em m/s.
     */
    val speed: Float get() = currentSpeed

    /**
     * Retorna verdadeiro se o player está em movimento.
     */
    val isMoving: Boolean get() = currentSpeed > 0.05f
}
