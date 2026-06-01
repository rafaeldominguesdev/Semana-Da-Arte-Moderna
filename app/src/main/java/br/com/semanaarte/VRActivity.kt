package br.com.semanaarte

import android.opengl.GLSurfaceView
import android.os.Bundle
import android.view.Window
import android.view.WindowManager
import android.widget.FrameLayout
import androidx.appcompat.app.AppCompatActivity
import br.com.semanaarte.renderer.GalleryRenderer

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

        setContentView(R.layout.activity_vr)
        val container = findViewById<FrameLayout>(R.id.vr_container)

        glSurfaceView = GLSurfaceView(this).apply {
            setEGLContextClientVersion(3)
            renderer = GalleryRenderer(this@VRActivity)
            setRenderer(renderer)
        }
        
        container.addView(glSurfaceView)
        
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
