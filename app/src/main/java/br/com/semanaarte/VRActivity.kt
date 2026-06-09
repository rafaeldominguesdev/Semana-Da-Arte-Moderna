package br.com.semanaarte

import android.animation.ObjectAnimator
import android.animation.ValueAnimator
import android.opengl.GLSurfaceView
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.View
import android.view.Window
import android.view.WindowManager
import android.view.animation.DecelerateInterpolator
import android.widget.Button
import android.widget.FrameLayout
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.isVisible
import br.com.semanaarte.renderer.GalleryRenderer

/**
 * Activity principal da experiência VR do Museu da Semana de Arte Moderna.
 *
 * Responsabilidades:
 *   - Inicializa e gerencia o GLSurfaceView com o GalleryRenderer
 *   - Gerencia o ciclo de vida dos sensores (pause/resume)
 *   - Roda o loop de movimento (head-gaze walking) na thread principal via Handler
 *   - Mostra/esconde o painel de informações com animação de fade
 *   - Gerencia o indicador "olhe para baixo para caminhar"
 *   - Botão de calibração do giroscópio
 */
class VRActivity : AppCompatActivity() {

    // ─── Views ─────────────────────────────────────────────────────────────

    private lateinit var glSurfaceView: GLSurfaceView
    private lateinit var renderer: GalleryRenderer
    private lateinit var gazeIndicator: View
    private lateinit var gazeArrow: TextView
    private lateinit var paintingPanel: View
    private lateinit var paintingTitle: TextView
    private lateinit var paintingArtistYear: TextView
    private lateinit var paintingDescription: TextView
    private lateinit var paintingFunFact: TextView
    private lateinit var btnClose: Button
    private lateinit var btnCalibrate: Button
    private lateinit var btnCalibrateFloating: Button

    // ─── Lógica de Movimento ───────────────────────────────────────────────

    /** Responsável pelo movimento do player e detecção de quadros */
    private val gazeWalker = HeadGazeWalker(
        museumRadius = 6.0f,
        walkSpeed    = 2.5f,
        galleryRadius = 8.0f
    )

    // Handler na thread principal para o loop de movimento (60 fps ~ 16ms)
    private val movementHandler = Handler(Looper.getMainLooper())
    private var lastMovementTimeMs = System.currentTimeMillis()

    /** Intervalo do loop de movimento em ms */
    private val MOVEMENT_INTERVAL_MS = 16L

    // ─── Estado da UI ──────────────────────────────────────────────────────

    /** Índice do quadro atualmente exibido no painel (-1 = nenhum) */
    private var currentDisplayedPainting = -1

    /** Animador do painel de informações (fade in/out) */
    private var panelAnimator: ObjectAnimator? = null

    /** Animador de bounce da seta do indicador de gaze */
    private var arrowAnimator: ValueAnimator? = null

    // ─── Runnable de Movimento ─────────────────────────────────────────────

    /**
     * Loop de movimento executado na thread principal a ~60fps.
     * Lê o estado do renderer (giroscópio) e atualiza o walker,
     * depois passa a nova posição de volta ao renderer.
     */
    private val movementRunnable = object : Runnable {
        override fun run() {
            val now = System.currentTimeMillis()
            val deltaTime = ((now - lastMovementTimeMs) / 1000f).coerceIn(0f, 0.1f)
            lastMovementTimeMs = now

            // Atualiza posição do player com base no giroscópio
            gazeWalker.update(
                isLookingDown = renderer.isLookingDown,
                yawDegrees    = renderer.currentYawDegrees,
                deltaTime     = deltaTime,
                artworks      = MuseumCatalog.artworks
            )

            // Passa nova posição para o renderer (thread-safe via @Volatile)
            renderer.playerX = gazeWalker.playerX
            renderer.playerZ = gazeWalker.playerZ

            // Verifica se há quadro próximo e atualiza UI
            val nearIndex = gazeWalker.nearPaintingIndex
            renderer.highlightedPaintingIndex = nearIndex
            handleNearPainting(nearIndex)

            // Reagenda o próximo frame
            movementHandler.postDelayed(this, MOVEMENT_INTERVAL_MS)
        }
    }

    // ─── Ciclo de Vida Activity ────────────────────────────────────────────

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Configuração de tela cheia imersiva
        requestWindowFeature(Window.FEATURE_NO_TITLE)

        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.R) {
            // API 30+: usa WindowInsetsController (API moderna)
            window.setDecorFitsSystemWindows(false)
            window.insetsController?.let { controller ->
                controller.hide(
                    android.view.WindowInsets.Type.statusBars()
                    or android.view.WindowInsets.Type.navigationBars()
                )
                controller.systemBarsBehavior =
                    android.view.WindowInsetsController.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
            }
        } else {
            // API < 30: fallback para flags antigas (ainda funcional)
            @Suppress("DEPRECATION")
            window.setFlags(
                WindowManager.LayoutParams.FLAG_FULLSCREEN,
                WindowManager.LayoutParams.FLAG_FULLSCREEN
            )
            @Suppress("DEPRECATION")
            window.decorView.systemUiVisibility = (
                View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
                or View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                or View.SYSTEM_UI_FLAG_FULLSCREEN
            )
        }

        setContentView(R.layout.activity_vr)

        // Inicializa views
        bindViews()

        // Configura GLSurfaceView
        setupGLSurface()

        // Configura botões
        setupButtons()

        // Inicia animação do indicador de gaze
        startGazeIndicator()
    }

    override fun onResume() {
        super.onResume()
        glSurfaceView.onResume()
        renderer.onResume()
        // Inicia loop de movimento
        lastMovementTimeMs = System.currentTimeMillis()
        movementHandler.post(movementRunnable)
    }

    override fun onPause() {
        super.onPause()
        glSurfaceView.onPause()
        renderer.onPause()
        // Para loop de movimento
        movementHandler.removeCallbacks(movementRunnable)
    }

    override fun onDestroy() {
        super.onDestroy()
        // Cancela animações
        panelAnimator?.cancel()
        arrowAnimator?.cancel()
        movementHandler.removeCallbacksAndMessages(null)
    }

    // ─── Configuração ──────────────────────────────────────────────────────

    /** Vincula todas as views do layout ao código */
    private fun bindViews() {
        gazeIndicator        = findViewById(R.id.gaze_indicator)
        gazeArrow            = findViewById(R.id.gaze_arrow)
        paintingPanel        = findViewById(R.id.painting_panel)
        paintingTitle        = findViewById(R.id.painting_title)
        paintingArtistYear   = findViewById(R.id.painting_artist_year)
        paintingDescription  = findViewById(R.id.painting_description)
        paintingFunFact      = findViewById(R.id.painting_fun_fact)
        btnClose             = findViewById(R.id.btn_close)
        btnCalibrate         = findViewById(R.id.btn_calibrate)
        btnCalibrateFloating = findViewById(R.id.btn_calibrate_floating)
    }

    /** Configura o GLSurfaceView e inicializa o renderer */
    private fun setupGLSurface() {
        val container = findViewById<FrameLayout>(R.id.vr_container)

        renderer = GalleryRenderer(this)

        glSurfaceView = GLSurfaceView(this).apply {
            setEGLContextClientVersion(3)
            setRenderer(renderer)
            renderMode = GLSurfaceView.RENDERMODE_CONTINUOUSLY
        }

        container.addView(glSurfaceView)
        renderer.initSensors()
    }

    /** Configura ações dos botões */
    private fun setupButtons() {
        // Botão Fechar — esconde painel de informações
        btnClose.setOnClickListener {
            hidePaintingPanel()
        }

        // Botão Calibrar (dentro do painel)
        btnCalibrate.setOnClickListener {
            calibrateGyroscope()
        }

        // Botão Calibrar flutuante (sempre visível no canto)
        btnCalibrateFloating.setOnClickListener {
            calibrateGyroscope()
        }
    }

    // ─── Calibração ────────────────────────────────────────────────────────

    /**
     * Calibra o giroscópio: a posição atual do dispositivo vira o "centro".
     * Também reinicia o indicador de gaze por 2 segundos como feedback visual.
     */
    private fun calibrateGyroscope() {
        renderer.calibrate()
        // Feedback: mostra brevemente o indicador de gaze
        showGazeIndicator(durationMs = 2000)
    }

    // ─── Painel de Informações ─────────────────────────────────────────────

    /**
     * Verifica o índice do quadro próximo e mostra/esconde o painel conforme necessário.
     * Chamado a cada frame do loop de movimento (na thread principal).
     */
    private fun handleNearPainting(nearIndex: Int) {
        if (nearIndex == currentDisplayedPainting) return // Sem mudança

        currentDisplayedPainting = nearIndex

        if (nearIndex >= 0) {
            val artwork = MuseumCatalog.artworks[nearIndex]
            showPaintingPanel(artwork)
        } else {
            hidePaintingPanel()
        }
    }

    /**
     * Exibe o painel com as informações da obra, com fade in suave.
     */
    private fun showPaintingPanel(artwork: Artwork) {
        // Preenche os dados
        paintingTitle.text       = artwork.title
        paintingArtistYear.text  = "${artwork.artist}  •  ${artwork.year}"
        paintingDescription.text = artwork.description

        // Curiosidade (opcional)
        if (artwork.funFact.isNotEmpty()) {
            paintingFunFact.text = "💡 ${artwork.funFact}"
            paintingFunFact.visibility = View.VISIBLE
        } else {
            paintingFunFact.visibility = View.GONE
        }

        // Faz fade in
        fadePaintingPanel(visible = true)
    }

    /**
     * Esconde o painel de informações com fade out suave.
     */
    private fun hidePaintingPanel() {
        fadePaintingPanel(visible = false)
    }

    /**
     * Anima o alpha do painel entre 0 e 1 (fade in/out).
     */
    private fun fadePaintingPanel(visible: Boolean) {
        panelAnimator?.cancel()

        if (visible) {
            paintingPanel.visibility = View.VISIBLE
        }

        val targetAlpha = if (visible) 1f else 0f

        panelAnimator = ObjectAnimator.ofFloat(paintingPanel, "alpha", paintingPanel.alpha, targetAlpha).apply {
            duration = 350
            interpolator = DecelerateInterpolator()
            addListener(object : android.animation.AnimatorListenerAdapter() {
                override fun onAnimationEnd(animation: android.animation.Animator) {
                    if (!visible) {
                        paintingPanel.visibility = View.INVISIBLE
                    }
                }
            })
            start()
        }
    }

    // ─── Indicador de Gaze ─────────────────────────────────────────────────

    /**
     * Exibe o indicador "olhe para baixo" com animação de bounce,
     * e o esconde automaticamente após [durationMs] milissegundos.
     */
    private fun startGazeIndicator() {
        showGazeIndicator(durationMs = 4000)
    }

    private fun showGazeIndicator(durationMs: Long) {
        gazeIndicator.visibility = View.VISIBLE

        // Fade in do indicador
        gazeIndicator.animate()
            .alpha(1f)
            .setDuration(400)
            .start()

        // Animação de bounce da seta (oscila para baixo e para cima)
        arrowAnimator?.cancel()
        arrowAnimator = ValueAnimator.ofFloat(0f, -20f, 0f).apply {
            duration        = 700
            repeatCount     = ValueAnimator.INFINITE
            interpolator    = DecelerateInterpolator()
            addUpdateListener { animator ->
                gazeArrow.translationY = animator.animatedValue as Float
            }
            start()
        }

        // Esconde após o tempo definido
        movementHandler.postDelayed({
            hideGazeIndicator()
        }, durationMs)
    }

    private fun hideGazeIndicator() {
        arrowAnimator?.cancel()
        gazeIndicator.animate()
            .alpha(0f)
            .setDuration(600)
            .withEndAction { gazeIndicator.visibility = View.GONE }
            .start()
    }
}
