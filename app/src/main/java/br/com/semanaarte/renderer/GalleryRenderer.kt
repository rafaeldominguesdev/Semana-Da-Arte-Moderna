package br.com.semanaarte.renderer

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import android.opengl.GLES30
import android.opengl.GLSurfaceView
import android.opengl.Matrix
import javax.microedition.khronos.egl.EGLConfig
import javax.microedition.khronos.opengles.GL10

class GalleryRenderer(private val context: Context) : GLSurfaceView.Renderer, SensorEventListener {

    private val viewMatrix = FloatArray(16)
    private val projectionMatrix = FloatArray(16)
    private val modelMatrix = FloatArray(16)
    private val mvpMatrix = FloatArray(16)
    private val rotationMatrix = FloatArray(16)

    private lateinit var sensorManager: SensorManager
    private var rotationVectorSensor: Sensor? = null

    private var surfaceWidth = 0
    private var surfaceHeight = 0

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
        void main() {
            gl_FragColor = texture2D(uTexture, fTexCoord);
        }
    """.trimIndent()

    private var shaderProgram: Int = 0
    private lateinit var frameCube: Cube
    
    // Textures for 5 artworks
    private val textures = IntArray(5)
    private val artworkTitles = arrayOf(
        "O Abaporu (1928)",
        "A Estudante (1921)",
        "Autorretrato (1923)",
        "Paisagem c/ Torre",
        "Composicao (1922)"
    )

    fun initSensors() {
        sensorManager = context.getSystemService(Context.SENSOR_SERVICE) as SensorManager
        rotationVectorSensor = sensorManager.getDefaultSensor(Sensor.TYPE_ROTATION_VECTOR)
        Matrix.setIdentityM(rotationMatrix, 0)
    }

    fun onResume() {
        rotationVectorSensor?.also { sensor ->
            sensorManager.registerListener(this, sensor, SensorManager.SENSOR_DELAY_GAME)
        }
    }

    fun onPause() {
        sensorManager.unregisterListener(this)
    }

    override fun onSurfaceCreated(gl: GL10?, config: EGLConfig?) {
        GLES30.glClearColor(0.2f, 0.2f, 0.2f, 1.0f)
        GLES30.glEnable(GLES30.GL_DEPTH_TEST)

        val vertexShader = loadShader(GLES30.GL_VERTEX_SHADER, vertexShaderCode)
        val fragmentShader = loadShader(GLES30.GL_FRAGMENT_SHADER, fragmentShaderCode)

        shaderProgram = GLES30.glCreateProgram().also {
            GLES30.glAttachShader(it, vertexShader)
            GLES30.glAttachShader(it, fragmentShader)
            GLES30.glLinkProgram(it)
        }

        frameCube = Cube()

        // Generate textures for the 5 artworks
        for (i in 0 until 5) {
            textures[i] = createTextureFromString(artworkTitles[i])
        }
    }

    private fun createTextureFromString(text: String): Int {
        val bitmap = Bitmap.createBitmap(512, 512, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        canvas.drawColor(Color.parseColor("#E0E0E0")) // Canvas background
        
        val paint = Paint().apply {
            color = Color.parseColor("#3E2723") // Frame color
            style = Paint.Style.STROKE
            strokeWidth = 30f
        }
        canvas.drawRect(15f, 15f, 497f, 497f, paint) // Draw frame
        
        paint.apply {
            style = Paint.Style.FILL
            textSize = 40f
            textAlign = Paint.Align.CENTER
        }
        canvas.drawText(text, 256f, 256f, paint)

        val textureHandle = IntArray(1)
        GLES30.glGenTextures(1, textureHandle, 0)
        GLES30.glBindTexture(GLES30.GL_TEXTURE_2D, textureHandle[0])
        GLES30.glTexParameteri(GLES30.GL_TEXTURE_2D, GLES30.GL_TEXTURE_MIN_FILTER, GLES30.GL_LINEAR)
        GLES30.glTexParameteri(GLES30.GL_TEXTURE_2D, GLES30.GL_TEXTURE_MAG_FILTER, GLES30.GL_LINEAR)
        android.opengl.GLUtils.texImage2D(GLES30.GL_TEXTURE_2D, 0, bitmap, 0)
        bitmap.recycle()
        return textureHandle[0]
    }

    override fun onSurfaceChanged(gl: GL10?, width: Int, height: Int) {
        surfaceWidth = width
        surfaceHeight = height
        // Projection matrix is set inside drawSceneForEye per viewport aspect ratio
    }

    override fun onDrawFrame(gl: GL10?) {
        if (surfaceWidth == 0 || surfaceHeight == 0) return

        GLES30.glClear(GLES30.GL_COLOR_BUFFER_BIT or GLES30.GL_DEPTH_BUFFER_BIT)
        
        val width = surfaceWidth
        val height = surfaceHeight
        
        // Render Left Eye
        GLES30.glViewport(0, 0, width / 2, height)
        drawSceneForEye(-0.05f, width / 2, height)

        // Render Right Eye
        GLES30.glViewport(width / 2, 0, width / 2, height)
        drawSceneForEye(0.05f, width / 2, height)
        
        // Restore viewport
        GLES30.glViewport(0, 0, width, height)
    }

    private fun drawSceneForEye(eyeOffsetX: Float, width: Int, height: Int) {
        val ratio = width.toFloat() / height.toFloat()
        Matrix.perspectiveM(projectionMatrix, 0, 70f, ratio, 0.1f, 100f)

        Matrix.setIdentityM(viewMatrix, 0)
        // Apply sensor rotation
        val scratch = FloatArray(16)
        Matrix.multiplyMM(scratch, 0, viewMatrix, 0, rotationMatrix, 0)
        System.arraycopy(scratch, 0, viewMatrix, 0, 16)
        
        // Move camera back a bit and add eye offset
        Matrix.translateM(viewMatrix, 0, eyeOffsetX, 0f, 0f)
        
        GLES30.glUseProgram(shaderProgram)

        // Draw 5 frames
        for (i in 0 until 5) {
            Matrix.setIdentityM(modelMatrix, 0)
            Matrix.rotateM(modelMatrix, 0, i * 72f, 0f, 1f, 0f)
            Matrix.translateM(modelMatrix, 0, 0f, 0f, -8f) // Move out to form a circle
            
            val tempMatrix = FloatArray(16)
            Matrix.multiplyMM(tempMatrix, 0, viewMatrix, 0, modelMatrix, 0)
            Matrix.multiplyMM(mvpMatrix, 0, projectionMatrix, 0, tempMatrix, 0)
            
            frameCube.draw(shaderProgram, mvpMatrix, textures[i])
        }
    }

    override fun onSensorChanged(event: SensorEvent?) {
        if (event?.sensor?.type == Sensor.TYPE_ROTATION_VECTOR) {
            SensorManager.getRotationMatrixFromVector(rotationMatrix, event.values)
        }
    }

    override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) {}

    private fun loadShader(type: Int, shaderCode: String): Int {
        return GLES30.glCreateShader(type).also { shader ->
            GLES30.glShaderSource(shader, shaderCode)
            GLES30.glCompileShader(shader)
        }
    }
}
