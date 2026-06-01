package br.com.semanaarte.renderer

import android.opengl.GLES30
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.FloatBuffer

class Cube {
    // Simple square frame
    private val cubeCoords = floatArrayOf(
        -1.0f,  1.0f, 0.0f,
        -1.0f, -1.0f, 0.0f,
         1.0f, -1.0f, 0.0f,
         1.0f,  1.0f, 0.0f
    )

    private val colors = floatArrayOf(
        0.8f, 0.8f, 0.8f, 1.0f,
        0.8f, 0.8f, 0.8f, 1.0f,
        0.8f, 0.8f, 0.8f, 1.0f,
        0.8f, 0.8f, 0.8f, 1.0f
    )

    private val vertexBuffer: FloatBuffer = ByteBuffer.allocateDirect(cubeCoords.size * 4).run {
        order(ByteOrder.nativeOrder())
        asFloatBuffer().apply {
            put(cubeCoords)
            position(0)
        }
    }

    private val colorBuffer: FloatBuffer = ByteBuffer.allocateDirect(colors.size * 4).run {
        order(ByteOrder.nativeOrder())
        asFloatBuffer().apply {
            put(colors)
            position(0)
        }
    }

    fun draw(program: Int, mvpMatrix: FloatArray) {
        val positionHandle = GLES30.glGetAttribLocation(program, "vPosition")
        val colorHandle = GLES30.glGetAttribLocation(program, "vColor")
        val mvpMatrixHandle = GLES30.glGetUniformLocation(program, "uMVPMatrix")

        GLES30.glEnableVertexAttribArray(positionHandle)
        GLES30.glVertexAttribPointer(positionHandle, 3, GLES30.GL_FLOAT, false, 0, vertexBuffer)

        GLES30.glEnableVertexAttribArray(colorHandle)
        GLES30.glVertexAttribPointer(colorHandle, 4, GLES30.GL_FLOAT, false, 0, colorBuffer)

        GLES30.glUniformMatrix4fv(mvpMatrixHandle, 1, false, mvpMatrix, 0)

        GLES30.glDrawArrays(GLES30.GL_TRIANGLE_FAN, 0, 4)

        GLES30.glDisableVertexAttribArray(positionHandle)
        GLES30.glDisableVertexAttribArray(colorHandle)
    }
}
