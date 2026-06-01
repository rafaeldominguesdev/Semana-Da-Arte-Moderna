package br.com.semanaarte

import android.opengl.GLSurfaceView
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import br.com.semanaarte.renderer.GalleryRenderer
import android.view.Window
import android.view.WindowManager

class VRActivity : AppCompatActivity() {

    private lateinit var glSurfaceView: GLSurfaceView
    private lateinit var renderer: GalleryRenderer

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        requestWindowFeature(Window.FEATURE_NO_TITLE)
        window.setFlags(
            WindowManager.LayoutParams.FLAG_FULLSCREEN,
            WindowManager.LayoutParams.FLAG_FULLSCREEN
        )

        glSurfaceView = GLSurfaceView(this).apply {
            setEGLContextClientVersion(3)
            renderer = GalleryRenderer(this@VRActivity)
            setRenderer(renderer)
        }
        
        setContentView(glSurfaceView)
        
        renderer.initSensors()
    }

    override fun onResume() {
        super.onResume()
        glSurfaceView.onResume()
        renderer.onResume()
    }

    override fun onPause() {
        super.onPause()
        glSurfaceView.onPause()
        renderer.onPause()
    }
}
