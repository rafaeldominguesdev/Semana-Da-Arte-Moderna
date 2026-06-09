package br.com.semanaarte.renderer

import android.opengl.GLES30
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.FloatBuffer
import java.nio.ShortBuffer

/**
 * Quad texturizado (retângulo plano) para renderizar os quadros e o chão.
 *
 * Geometria: 2 triângulos formando um retângulo centrado na origem.
 * Usa índices (EBO) para evitar duplicação de vértices.
 *
 * @param width  Largura do quad em metros
 * @param height Altura do quad em metros
 */
class Quad(private val width: Float = 2.0f, private val height: Float = 2.0f) {

    // Vértices: 4 cantos do retângulo (X, Y, Z)
    private val vertices: FloatArray = floatArrayOf(
        -width / 2f,  height / 2f, 0f, // Superior esquerdo
        -width / 2f, -height / 2f, 0f, // Inferior esquerdo
         width / 2f, -height / 2f, 0f, // Inferior direito
         width / 2f,  height / 2f, 0f  // Superior direito
    )

    // Coordenadas UV (textura)
    private val uvCoords: FloatArray = floatArrayOf(
        0f, 0f, // Superior esquerdo
        0f, 1f, // Inferior esquerdo
        1f, 1f, // Inferior direito
        1f, 0f  // Superior direito
    )

    // Índices: 2 triângulos (0-1-2 e 0-2-3)
    private val indices: ShortArray = shortArrayOf(0, 1, 2, 0, 2, 3)

    // Buffers nativos
    private val vertexBuffer: FloatBuffer = ByteBuffer
        .allocateDirect(vertices.size * 4)
        .order(ByteOrder.nativeOrder())
        .asFloatBuffer()
        .apply { put(vertices); position(0) }

    private val uvBuffer: FloatBuffer = ByteBuffer
        .allocateDirect(uvCoords.size * 4)
        .order(ByteOrder.nativeOrder())
        .asFloatBuffer()
        .apply { put(uvCoords); position(0) }

    private val indexBuffer: ShortBuffer = ByteBuffer
        .allocateDirect(indices.size * 2)
        .order(ByteOrder.nativeOrder())
        .asShortBuffer()
        .apply { put(indices); position(0) }

    /**
     * Renderiza o quad com a textura e MVP matrix fornecidas.
     *
     * @param program   ID do programa shader compilado
     * @param mvpMatrix Matriz MVP (Model × View × Projection)
     * @param textureId ID da textura OpenGL a usar
     */
    fun draw(program: Int, mvpMatrix: FloatArray, textureId: Int) {
        // Obtém localizações dos atributos e uniforms no shader
        val positionHandle  = GLES30.glGetAttribLocation(program, "vPosition")
        val uvHandle        = GLES30.glGetAttribLocation(program, "vTexCoord")
        val mvpHandle       = GLES30.glGetUniformLocation(program, "uMVPMatrix")
        val textureHandle   = GLES30.glGetUniformLocation(program, "uTexture")

        // Ativa e define os vértices
        GLES30.glEnableVertexAttribArray(positionHandle)
        GLES30.glVertexAttribPointer(positionHandle, 3, GLES30.GL_FLOAT, false, 0, vertexBuffer)

        // Ativa e define as coordenadas UV
        GLES30.glEnableVertexAttribArray(uvHandle)
        GLES30.glVertexAttribPointer(uvHandle, 2, GLES30.GL_FLOAT, false, 0, uvBuffer)

        // Associa textura
        GLES30.glActiveTexture(GLES30.GL_TEXTURE0)
        GLES30.glBindTexture(GLES30.GL_TEXTURE_2D, textureId)
        GLES30.glUniform1i(textureHandle, 0)

        // Define MVP
        GLES30.glUniformMatrix4fv(mvpHandle, 1, false, mvpMatrix, 0)

        // Desenha usando índices
        GLES30.glDrawElements(GLES30.GL_TRIANGLES, indices.size, GLES30.GL_UNSIGNED_SHORT, indexBuffer)

        // Limpa estado
        GLES30.glDisableVertexAttribArray(positionHandle)
        GLES30.glDisableVertexAttribArray(uvHandle)
    }
}
