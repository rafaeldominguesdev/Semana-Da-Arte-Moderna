package br.com.semanaarte.renderer

import android.content.Context
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

    // Sensor for head tracking
    private lateinit var sensorManager: SensorManager
    private var rotationVectorSensor: Sensor? = null

    // Simple shaders for colored frames
    private val vertexShaderCode = """
        uniform mat4 uMVPMatrix;
        attribute vec4 vPosition;
        attribute vec4 vColor;
        varying vec4 fColor;
        void main() {
            gl_Position = uMVPMatrix * vPosition;
            fColor = vColor;
        }
    """.trimIndent()

    private val fragmentShaderCode = """
        precision mediump float;
        varying vec4 fColor;
        void main() {
            gl_FragColor = fColor;
        }
    """.trimIndent()

    private var shaderProgram: Int = 0
    private var isVRMode = true

    // Cube for frames
    private lateinit var frameCube: Cube

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
        GLES30.glClearColor(0.1f, 0.1f, 0.1f, 1.0f)
        GLES30.glEnable(GLES30.GL_DEPTH_TEST)

        val vertexShader = loadShader(GLES30.GL_VERTEX_SHADER, vertexShaderCode)
        val fragmentShader = loadShader(GLES30.GL_FRAGMENT_SHADER, fragmentShaderCode)

        shaderProgram = GLES30.glCreateProgram().also {
            GLES30.glAttachShader(it, vertexShader)
            GLES30.glAttachShader(it, fragmentShader)
            GLES30.glLinkProgram(it)
        }

        frameCube = Cube()
    }

    override fun onSurfaceChanged(gl: GL10?, width: Int, height: Int) {
        // Handled in onDrawFrame for VR split screen
    }

    override fun onDrawFrame(gl: GL10?) {
        GLES30.glClear(GLES30.GL_COLOR_BUFFER_BIT or GLES30.GL_DEPTH_BUFFER_BIT)

        val viewport = IntArray(4)
        GLES30.glGetIntegerv(GLES30.GL_VIEWPORT, viewport, 0)
        
        val width = viewport[2]
        val height = viewport[3]
        
        // Render Left Eye
        GLES30.glViewport(0, 0, width / 2, height)
        drawSceneForEye(-0.1f) // slight offset for left eye

        // Render Right Eye
        GLES30.glViewport(width / 2, 0, width / 2, height)
        drawSceneForEye(0.1f) // slight offset for right eye
        
        // Restore viewport
        GLES30.glViewport(0, 0, width, height)
    }

    private fun drawSceneForEye(eyeOffsetX: Float) {
        Matrix.setIdentityM(viewMatrix, 0)
        // Apply sensor rotation
        val scratch = FloatArray(16)
        Matrix.multiplyMM(scratch, 0, viewMatrix, 0, rotationMatrix, 0)
        System.arraycopy(scratch, 0, viewMatrix, 0, 16)
        // Move camera back a bit and add eye offset
        Matrix.translateM(viewMatrix, 0, eyeOffsetX, 0f, -1.0f)
        
        GLES30.glUseProgram(shaderProgram)

        // Draw 5 frames
        for (i in 0 until 5) {
            Matrix.setIdentityM(modelMatrix, 0)
            Matrix.rotateM(modelMatrix, 0, i * 72f, 0f, 1f, 0f)
            Matrix.translateM(modelMatrix, 0, 0f, 0f, -5f)
            
            Matrix.multiplyMM(mvpMatrix, 0, viewMatrix, 0, modelMatrix, 0)
            
            // Note: properly we should apply projectionMatrix here.
            // For a basic MVP without projection, it might look distorted,
            // but it serves as a basic proof of concept.
            
            frameCube.draw(shaderProgram, mvpMatrix)
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
