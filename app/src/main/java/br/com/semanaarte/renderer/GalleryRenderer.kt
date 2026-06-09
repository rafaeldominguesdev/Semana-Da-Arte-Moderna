package br.com.semanaarte.renderer

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Typeface
import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import android.opengl.GLES30
import android.opengl.GLSurfaceView
import android.opengl.Matrix
import br.com.semanaarte.Artwork
import br.com.semanaarte.MuseumCatalog
import javax.microedition.khronos.egl.EGLConfig
import javax.microedition.khronos.opengles.GL10
import kotlin.math.abs

/**
 * Renderer OpenGL ES 3.0 para o Museu VR da Semana de Arte Moderna.
 *
 * Funcionalidades:
 *   - Renderização estereoscópica (olho esquerdo + olho direito)
 *   - Controle de câmera via sensor TYPE_ROTATION_VECTOR (giroscópio)
 *   - Detecção de "olhar para baixo" = head-gaze walking
 *   - Calibração (zera a rotação na posição atual do dispositivo)
 *   - Movimento do player com inércia (via HeadGazeWalker)
 *   - 5 quadros dispostos em círculo na galeria
 *   - Parede circular (cilindro) e teto/chão simplificados
 *   - Highlighting do quadro próximo
 *
 * Compensação de eixos:
 *   O Android usa um sistema de coordenadas onde Z aponta para cima quando
 *   o dispositivo está deitado. Unity/OpenGL usam Y para cima.
 *   A compensação é feita no remapCoordinateSystem com AXIS_X e AXIS_Z.
 */
class GalleryRenderer(private val context: Context) : GLSurfaceView.Renderer, SensorEventListener {

    // ─── Matrizes de Transformação ─────────────────────────────────────────

    private val viewMatrix        = FloatArray(16)
    private val projectionMatrix  = FloatArray(16)
    private val modelMatrix       = FloatArray(16)
    private val mvpMatrix         = FloatArray(16)
    private val tempMatrix        = FloatArray(16)

    // Matriz de rotação atual (do sensor)
    private val rotationMatrix    = FloatArray(16).also { Matrix.setIdentityM(it, 0) }

    // Matriz de calibração (offset de referência zero)
    private val calibrationMatrix = FloatArray(16).also { Matrix.setIdentityM(it, 0) }

    // Inversa da calibração (calculada uma vez ao calibrar)
    private val invCalibration    = FloatArray(16).also { Matrix.setIdentityM(it, 0) }

    // Rotação final (calibração aplicada)
    private val calibratedRotation = FloatArray(16).also { Matrix.setIdentityM(it, 0) }

    // ─── Sensores ──────────────────────────────────────────────────────────

    private lateinit var sensorManager: SensorManager
    private var rotationVectorSensor: Sensor? = null

    // Dados brutos do sensor (copiados de forma thread-safe)
    @Volatile private var sensorValues: FloatArray? = null

    // ─── Estado da Câmera ──────────────────────────────────────────────────

    /** Pitch atual em graus (negativo = olhando para baixo) */
    @Volatile var currentPitchDegrees: Float = 0f
        private set

    /**
     * True quando o usuário olha para baixo além do limiar.
     * Lido pelo VRActivity para acionar o movimento.
     */
    @Volatile var isLookingDown: Boolean = false
        private set

    /** Limiar de pitch para ativar o movimento (graus negativos = abaixo do horizonte) */
    var lookDownThreshold: Float = -30f

    /** Yaw atual em graus (0=norte, 90=leste) — usado para direção do movimento */
    @Volatile var currentYawDegrees: Float = 0f
        private set

    // ─── Player e Movimento ────────────────────────────────────────────────

    /** Posição X do player no mundo */
    @Volatile var playerX: Float = 0f

    /** Posição Z do player no mundo */
    @Volatile var playerZ: Float = 0f

    // Timestamp do último frame para cálculo de deltaTime
    private var lastFrameTimeNs: Long = 0L

    // ─── Superfície ────────────────────────────────────────────────────────

    private var surfaceWidth  = 0
    private var surfaceHeight = 0

    // ─── OpenGL — Shaders ──────────────────────────────────────────────────

    private val vertexShaderCode = """
        uniform mat4 uMVPMatrix;
        attribute vec4 vPosition;
        attribute vec2 vTexCoord;
        varying vec2 fTexCoord;
        void main() {
            gl_Position = uMVPMatrix * vPosition;
            fTexCoord = vTexCoord;
        }
    """.trimIndent()

    private val fragmentShaderCode = """
        precision mediump float;
        varying vec2 fTexCoord;
        uniform sampler2D uTexture;
        uniform float uHighlight;
        void main() {
            vec4 color = texture2D(uTexture, fTexCoord);
            // Adiciona brilho de highlight no quadro focado
            color.rgb += vec3(uHighlight * 0.3);
            gl_FragColor = color;
        }
    """.trimIndent()

    private var shaderProgram: Int = 0
    private var highlightUniform: Int = -1

    // ─── Geometria ─────────────────────────────────────────────────────────

    // Quad para os quadros (plano texturizado)
    private lateinit var paintingQuad: Quad

    // Quad para o chão
    private lateinit var floorQuad: Quad

    // ─── Texturas ──────────────────────────────────────────────────────────

    private val paintingTextures = IntArray(MuseumCatalog.artworks.size)
    private var floorTexture: Int = 0
    private var wallTexture: Int  = 0

    // ─── Highlight ─────────────────────────────────────────────────────────

    /** Índice do quadro atualmente destacado (-1 = nenhum) */
    var highlightedPaintingIndex: Int = -1

    // Intensidade atual do highlight (animado suavemente no renderer)
    private var currentHighlight: Float = 0f

    // ─── Callbacks para a Activity ─────────────────────────────────────────

    /** Chamado quando um quadro é detectado por proximidade */
    var onPaintingNear: ((Artwork) -> Unit)? = null

    /** Chamado quando o player se afasta de todos os quadros */
    var onPaintingLeft: (() -> Unit)? = null

    // ─── Inicialização de Sensores ─────────────────────────────────────────

    /**
     * Inicializa o SensorManager e o sensor de vetor de rotação.
     * Deve ser chamado da Activity (thread principal).
     */
    fun initSensors() {
        sensorManager = context.getSystemService(Context.SENSOR_SERVICE) as SensorManager
        rotationVectorSensor = sensorManager.getDefaultSensor(Sensor.TYPE_ROTATION_VECTOR)

        if (rotationVectorSensor == null) {
            android.util.Log.w("GalleryRenderer", "Sensor TYPE_ROTATION_VECTOR não disponível!")
        }
    }

    fun onResume() {
        rotationVectorSensor?.let { sensor ->
            sensorManager.registerListener(this, sensor, SensorManager.SENSOR_DELAY_GAME)
        }
    }

    fun onPause() {
        sensorManager.unregisterListener(this)
    }

    // ─── Calibração ────────────────────────────────────────────────────────

    /**
     * Captura a rotação atual como ponto zero de referência.
     * Deve ser chamado quando o usuário pressiona o botão "Calibrar".
     */
    fun calibrate() {
        // Copia a rotação atual como calibração
        System.arraycopy(rotationMatrix, 0, calibrationMatrix, 0, 16)
        // Calcula a inversa (para subtrair a calibração do input)
        Matrix.invertM(invCalibration, 0, calibrationMatrix, 0)
        android.util.Log.d("GalleryRenderer", "Giroscópio calibrado.")
    }

    // ─── GLSurfaceView.Renderer ────────────────────────────────────────────

    override fun onSurfaceCreated(gl: GL10?, config: EGLConfig?) {
        // Cor de fundo: branco-creme de museu
        GLES30.glClearColor(0.96f, 0.94f, 0.88f, 1.0f)
        GLES30.glEnable(GLES30.GL_DEPTH_TEST)
        GLES30.glEnable(GLES30.GL_BLEND)
        GLES30.glBlendFunc(GLES30.GL_SRC_ALPHA, GLES30.GL_ONE_MINUS_SRC_ALPHA)

        // Compila shaders e linka programa
        val vertexShader   = compileShader(GLES30.GL_VERTEX_SHADER, vertexShaderCode)
        val fragmentShader = compileShader(GLES30.GL_FRAGMENT_SHADER, fragmentShaderCode)

        shaderProgram = GLES30.glCreateProgram().also { prog ->
            GLES30.glAttachShader(prog, vertexShader)
            GLES30.glAttachShader(prog, fragmentShader)
            GLES30.glLinkProgram(prog)
        }

        highlightUniform = GLES30.glGetUniformLocation(shaderProgram, "uHighlight")

        // Cria geometria
        paintingQuad = Quad(width = 2.0f, height = 2.8f) // Proporção de tela
        floorQuad    = Quad(width = 12f, height = 12f)

        // Gera texturas dos quadros
        MuseumCatalog.artworks.forEachIndexed { i, artwork ->
            paintingTextures[i] = createPaintingTexture(artwork)
        }

        // Cria texturas de ambiente
        floorTexture = createSolidTexture(Color.parseColor("#D4C5A0"))
        wallTexture  = createSolidTexture(Color.parseColor("#F5F0E8"))

        lastFrameTimeNs = System.nanoTime()
    }

    override fun onSurfaceChanged(gl: GL10?, width: Int, height: Int) {
        surfaceWidth  = width
        surfaceHeight = height
    }

    override fun onDrawFrame(gl: GL10?) {
        if (surfaceWidth == 0 || surfaceHeight == 0) return

        // Calcula deltaTime (reservado para uso futuro em animações internas)
        val now = System.nanoTime()
        @Suppress("UNUSED_VARIABLE")
        val deltaTime = ((now - lastFrameTimeNs) / 1_000_000_000f).coerceIn(0f, 0.1f)
        lastFrameTimeNs = now

        // Consome dados do sensor de forma thread-safe
        val values = sensorValues
        if (values != null) {
            updateRotationMatrix(values)
        }

        // Aplica calibração: rotFinal = inv(calibração) * rotBruta
        Matrix.multiplyMM(calibratedRotation, 0, invCalibration, 0, rotationMatrix, 0)

        // Extrai yaw e pitch da matriz calibrada
        extractCameraAngles(calibratedRotation)

        // Limpa framebuffer
        GLES30.glClear(GLES30.GL_COLOR_BUFFER_BIT or GLES30.GL_DEPTH_BUFFER_BIT)

        val w = surfaceWidth
        val h = surfaceHeight

        // Renderiza olho esquerdo
        GLES30.glViewport(0, 0, w / 2, h)
        drawScene(eyeOffsetX = -0.032f, viewWidth = w / 2, viewHeight = h)

        // Renderiza olho direito
        GLES30.glViewport(w / 2, 0, w / 2, h)
        drawScene(eyeOffsetX = +0.032f, viewWidth = w / 2, viewHeight = h)

        // Restaura viewport
        GLES30.glViewport(0, 0, w, h)
    }

    // ─── Desenho da Cena ───────────────────────────────────────────────────

    private fun drawScene(eyeOffsetX: Float, viewWidth: Int, viewHeight: Int) {
        val ratio = viewWidth.toFloat() / viewHeight.toFloat()

        // Matriz de projeção perspectiva (FOV 100° para VR imersivo)
        Matrix.perspectiveM(projectionMatrix, 0, 100f, ratio, 0.05f, 100f)

        // Constrói View Matrix:
        // O truque para câmera VR é usar a TRANSPOSTA da matriz de rotação como view.
        // A câmera olha para onde o dispositivo aponta.
        Matrix.setIdentityM(viewMatrix, 0)

        // A rotação calibrada define para onde a câmera olha.
        // Transposta da rotação = inversa (para matriz ortonormal) = view matrix da câmera.
        val transposedRot = FloatArray(16)
        Matrix.transposeM(transposedRot, 0, calibratedRotation, 0)

        // Tradução: posição do player na cena (nega porque movemos o mundo, não a câmera)
        Matrix.multiplyMM(viewMatrix, 0, transposedRot, 0,
            translationMatrix(-playerX, -1.6f, -playerZ), 0)

        // Offset entre os olhos (IPD = 64mm)
        Matrix.translateM(viewMatrix, 0, eyeOffsetX, 0f, 0f)

        GLES30.glUseProgram(shaderProgram)

        // Desenha chão
        drawFloor()

        // Desenha os 5 quadros em círculo
        MuseumCatalog.artworks.forEachIndexed { i, artwork ->
            drawPainting(i, artwork)
        }
    }

    private fun drawFloor() {
        Matrix.setIdentityM(modelMatrix, 0)
        Matrix.translateM(modelMatrix, 0, 0f, 0f, 0f)     // No centro
        Matrix.rotateM(modelMatrix, 0, -90f, 1f, 0f, 0f)  // Deita o quad

        Matrix.multiplyMM(tempMatrix, 0, viewMatrix, 0, modelMatrix, 0)
        Matrix.multiplyMM(mvpMatrix, 0, projectionMatrix, 0, tempMatrix, 0)

        GLES30.glUniform1f(highlightUniform, 0f)
        floorQuad.draw(shaderProgram, mvpMatrix, floorTexture)
    }

    private fun drawPainting(index: Int, artwork: Artwork) {
        // angleRad calculado diretamente nas funções sin/cos abaixo
        val radius   = 8f        // Raio do círculo de quadros
        val height   = 1.5f      // Altura do centro do quadro

        val posX = (radius * Math.sin(artwork.angleInGallery.toDouble() * Math.PI / 180)).toFloat()
        val posZ = -(radius * Math.cos(artwork.angleInGallery.toDouble() * Math.PI / 180)).toFloat()

        Matrix.setIdentityM(modelMatrix, 0)
        Matrix.translateM(modelMatrix, 0, posX, height, posZ)

        // Rotaciona o quadro para ficar de frente para o centro da sala
        Matrix.rotateM(modelMatrix, 0, artwork.angleInGallery, 0f, 1f, 0f)

        Matrix.multiplyMM(tempMatrix, 0, viewMatrix, 0, modelMatrix, 0)
        Matrix.multiplyMM(mvpMatrix, 0, projectionMatrix, 0, tempMatrix, 0)

        // Define intensidade do highlight
        val isHighlighted = (index == highlightedPaintingIndex)
        val targetHighlight = if (isHighlighted) 1f else 0f
        currentHighlight += (targetHighlight - currentHighlight) * 0.1f
        val hl = if (isHighlighted) currentHighlight else 0f

        GLES30.glUniform1f(highlightUniform, hl)
        paintingQuad.draw(shaderProgram, mvpMatrix, paintingTextures[index])
    }

    // ─── Processamento do Sensor ───────────────────────────────────────────

    override fun onSensorChanged(event: SensorEvent?) {
        if (event?.sensor?.type == Sensor.TYPE_ROTATION_VECTOR) {
            // Copia os valores em uma nova array (thread-safe para o GL thread)
            sensorValues = event.values.copyOf()
        }
    }

    override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) { /* Não utilizado */ }

    /**
     * Converte os valores do sensor para uma matriz de rotação 4x4.
     * Usa remapCoordinateSystem para compensar a orientação landscape do dispositivo VR.
     *
     * Em modo landscape (VR Cardboard):
     *   - O dispositivo é segurado de lado
     *   - Precisamos remap: AXIS_X → AXIS_X, AXIS_Y → AXIS_MINUS_Z
     */
    private fun updateRotationMatrix(values: FloatArray) {
        val inR = FloatArray(16)
        SensorManager.getRotationMatrixFromVector(inR, values)

        // Remap para orientação landscape (VR Cardboard)
        // AXIS_X permanece X, AXIS_MINUS_Z vira Y (para que "frente" seja para onde o dispositivo aponta)
        SensorManager.remapCoordinateSystem(
            inR,
            SensorManager.AXIS_X,
            SensorManager.AXIS_Z,
            rotationMatrix
        )
    }

    /**
     * Extrai ângulos de yaw e pitch da matriz de rotação calibrada.
     *
     * A matriz de rotação 4x4 do Android tem a seguinte estrutura:
     *   [0]  [1]  [2]  [3]
     *   [4]  [5]  [6]  [7]
     *   [8]  [9]  [10] [11]
     *   [12] [13] [14] [15]
     *
     * Pitch = arcsin(-M[9]) — ângulo para cima/baixo
     * Yaw   = atan2(M[8], M[10]) — ângulo horizontal
     */
    private fun extractCameraAngles(matrix: FloatArray) {
        // Pitch: componente [9] = -sin(pitch) na matriz de rotação do Android
        val pitchRad  = -Math.asin(matrix[9].toDouble()).toFloat()
        val pitchDeg  = Math.toDegrees(pitchRad.toDouble()).toFloat()

        // Limita pitch entre -80° e +80°
        currentPitchDegrees = pitchDeg.coerceIn(-80f, 80f)

        // Yaw: atan2 do componente lateral e frontal
        val yawRad = Math.atan2(matrix[8].toDouble(), matrix[10].toDouble()).toFloat()
        currentYawDegrees = Math.toDegrees(yawRad.toDouble()).toFloat()

        // Detecção de "olhar para baixo"
        isLookingDown = currentPitchDegrees < lookDownThreshold
    }

    // ─── Criação de Texturas ───────────────────────────────────────────────

    /**
     * Cria uma textura OpenGL renderizada com Bitmap do Android.
     * Desenha o título e artista sobre um fundo colorido simulando a obra.
     */
    private fun createPaintingTexture(artwork: Artwork): Int {
        val size   = 512
        val bitmap = Bitmap.createBitmap(size, size, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)

        // Fundo com cor única por obra (baseado no índice)
        val bgColors = listOf(
            "#F0E8D0", "#E8D0C0", "#D0C0E8", "#C0D0E8", "#D0E8C0"
        )
        val index = MuseumCatalog.artworks.indexOfFirst { it.id == artwork.id }
        canvas.drawColor(Color.parseColor(bgColors[index.coerceIn(0, 4)]))

        // Moldura dourada
        val framePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color       = Color.parseColor("#8B6914")
            style       = Paint.Style.STROKE
            strokeWidth = 24f
        }
        canvas.drawRect(12f, 12f, size - 12f, size - 12f, framePaint)

        // Moldura interna (dupla)
        framePaint.strokeWidth = 6f
        framePaint.color = Color.parseColor("#C9A227")
        canvas.drawRect(30f, 30f, size - 30f, size - 30f, framePaint)

        // Texto do título
        val titlePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color     = Color.parseColor("#2C1810")
            textSize  = 38f
            textAlign = Paint.Align.CENTER
            typeface  = Typeface.create(Typeface.SERIF, Typeface.BOLD)
        }
        // Quebra o título em linhas se for muito longo
        val titleLines = wrapText(artwork.title, titlePaint, size - 80f)
        val titleY = size / 2f - (titleLines.size * 45f) / 2f + 20f
        titleLines.forEachIndexed { i, line ->
            canvas.drawText(line, size / 2f, titleY + i * 45f, titlePaint)
        }

        // Texto do artista
        val artistPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color     = Color.parseColor("#5C3D1E")
            textSize  = 26f
            textAlign = Paint.Align.CENTER
            typeface  = Typeface.create(Typeface.SERIF, Typeface.ITALIC)
        }
        canvas.drawText(artwork.artist, size / 2f, titleY + titleLines.size * 45f + 30f, artistPaint)

        // Ano
        val yearPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color     = Color.parseColor("#7A6045")
            textSize  = 22f
            textAlign = Paint.Align.CENTER
        }
        canvas.drawText("(${artwork.year})", size / 2f, titleY + titleLines.size * 45f + 65f, yearPaint)

        return uploadBitmapToTexture(bitmap)
    }

    /** Cria uma textura de cor sólida simples */
    private fun createSolidTexture(color: Int): Int {
        val bitmap = Bitmap.createBitmap(64, 64, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        canvas.drawColor(color)
        // Adiciona grid sutil para o chão
        val gridPaint = Paint().apply {
            this.color = Color.argb(30, 0, 0, 0)
            strokeWidth = 1f
        }
        for (i in 0..64 step 16) {
            canvas.drawLine(i.toFloat(), 0f, i.toFloat(), 64f, gridPaint)
            canvas.drawLine(0f, i.toFloat(), 64f, i.toFloat(), gridPaint)
        }
        return uploadBitmapToTexture(bitmap)
    }

    /** Faz upload de um Bitmap para uma textura OpenGL e o recicla */
    private fun uploadBitmapToTexture(bitmap: Bitmap): Int {
        val handle = IntArray(1)
        GLES30.glGenTextures(1, handle, 0)
        GLES30.glBindTexture(GLES30.GL_TEXTURE_2D, handle[0])
        GLES30.glTexParameteri(GLES30.GL_TEXTURE_2D, GLES30.GL_TEXTURE_MIN_FILTER, GLES30.GL_LINEAR)
        GLES30.glTexParameteri(GLES30.GL_TEXTURE_2D, GLES30.GL_TEXTURE_MAG_FILTER, GLES30.GL_LINEAR)
        GLES30.glTexParameteri(GLES30.GL_TEXTURE_2D, GLES30.GL_TEXTURE_WRAP_S, GLES30.GL_CLAMP_TO_EDGE)
        GLES30.glTexParameteri(GLES30.GL_TEXTURE_2D, GLES30.GL_TEXTURE_WRAP_T, GLES30.GL_CLAMP_TO_EDGE)
        android.opengl.GLUtils.texImage2D(GLES30.GL_TEXTURE_2D, 0, bitmap, 0)
        bitmap.recycle()
        return handle[0]
    }

    // ─── Utilitários ───────────────────────────────────────────────────────

    /** Compila um shader e retorna o ID */
    private fun compileShader(type: Int, code: String): Int {
        return GLES30.glCreateShader(type).also { shader ->
            GLES30.glShaderSource(shader, code)
            GLES30.glCompileShader(shader)

            // Verifica erros de compilação em debug
            val status = IntArray(1)
            GLES30.glGetShaderiv(shader, GLES30.GL_COMPILE_STATUS, status, 0)
            if (status[0] == GLES30.GL_FALSE) {
                android.util.Log.e("GalleryRenderer",
                    "Erro no shader: ${GLES30.glGetShaderInfoLog(shader)}")
            }
        }
    }

    /** Cria uma matriz de translação 4x4 */
    private fun translationMatrix(x: Float, y: Float, z: Float): FloatArray {
        return FloatArray(16).also { m ->
            Matrix.setIdentityM(m, 0)
            Matrix.translateM(m, 0, x, y, z)
        }
    }

    /** Quebra um texto em linhas que caibam na largura máxima */
    private fun wrapText(text: String, paint: Paint, maxWidth: Float): List<String> {
        val words = text.split(" ")
        val lines = mutableListOf<String>()
        var current = ""
        for (word in words) {
            val test = if (current.isEmpty()) word else "$current $word"
            if (paint.measureText(test) <= maxWidth) {
                current = test
            } else {
                if (current.isNotEmpty()) lines.add(current)
                current = word
            }
        }
        if (current.isNotEmpty()) lines.add(current)
        return lines
    }
}
