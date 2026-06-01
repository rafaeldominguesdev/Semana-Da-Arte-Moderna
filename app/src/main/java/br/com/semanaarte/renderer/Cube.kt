package br.com.semanaarte.renderer

import android.opengl.GLES30
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.FloatBuffer

class Cube {
    private val cubeCoords = floatArrayOf(
        -2.0f,  2.0f, 0.0f, // top left
        -2.0f, -2.0f, 0.0f, // bottom left
         2.0f, -2.0f, 0.0f, // bottom right
         2.0f,  2.0f, 0.0f  // top right
    )

    private val uvCoords = floatArrayOf(
        0.0f, 0.0f,
        0.0f, 1.0f,
        1.0f, 1.0f,
        1.0f, 0.0f
    )

    private val vertexBuffer: FloatBuffer = ByteBuffer.allocateDirect(cubeCoords.size * 4).run {
        order(ByteOrder.nativeOrder())
        asFloatBuffer().apply { put(cubeCoords); position(0) }
    }

    private val uvBuffer: FloatBuffer = ByteBuffer.allocateDirect(uvCoords.size * 4).run {
        order(ByteOrder.nativeOrder())
        asFloatBuffer().apply { put(uvCoords); position(0) }
    }

    fun draw(program: Int, mvpMatrix: FloatArray, textureId: Int) {
        val positionHandle = GLES30.glGetAttribLocation(program, "vPosition")
        val uvHandle = GLES30.glGetAttribLocation(program, "vTexCoord")
        val mvpMatrixHandle = GLES30.glGetUniformLocation(program, "uMVPMatrix")
        val textureHandle = GLES30.glGetUniformLocation(program, "uTexture")

        GLES30.glEnableVertexAttribArray(positionHandle)
        GLES30.glVertexAttribPointer(positionHandle, 3, GLES30.GL_FLOAT, false, 0, vertexBuffer)

        GLES30.glEnableVertexAttribArray(uvHandle)
        GLES30.glVertexAttribPointer(uvHandle, 2, GLES30.GL_FLOAT, false, 0, uvBuffer)

        // Bind Texture
        GLES30.glActiveTexture(GLES30.GL_TEXTURE0)
        GLES30.glBindTexture(GLES30.GL_TEXTURE_2D, textureId)
        GLES30.glUniform1i(textureHandle, 0)

        GLES30.glUniformMatrix4fv(mvpMatrixHandle, 1, false, mvpMatrix, 0)

        GLES30.glDrawArrays(GLES30.GL_TRIANGLE_FAN, 0, 4)

        GLES30.glDisableVertexAttribArray(positionHandle)
        GLES30.glDisableVertexAttribArray(uvHandle)
    }
}
