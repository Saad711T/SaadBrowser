package com.example.mybrowser

import android.annotation.SuppressLint
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.webkit.WebChromeClient
import android.webkit.WebResourceRequest
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.activity.ComponentActivity
import androidx.activity.OnBackPressedCallback
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.viewinterop.AndroidView
import com.example.mybrowser.ui.theme.MyBrowserTheme

class MainActivity : ComponentActivity() {

    private var webView: WebView? = null
    private val homeUrl = "https://calm-daffodil-7a888b.netlify.app/"

    @SuppressLint("SetJavaScriptEnabled")
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() {
                val wv = webView
                if (wv != null && wv.canGoBack()) {
                    wv.goBack()
                } else {
                    isEnabled = false
                    onBackPressedDispatcher.onBackPressed()
                }
            }
        })

        setContent {
            MyBrowserTheme {
                BrowserScreen(homeUrl) { webView = it }
            }
        }
    }
}

@SuppressLint("SetJavaScriptEnabled")
@Composable
fun BrowserScreen(url: String, onWebViewReady: (WebView) -> Unit) {
    var progress by remember { mutableFloatStateOf(0f) }
    var loading by remember { mutableStateOf(false) }

    Box(modifier = Modifier.fillMaxSize()) {
        AndroidView(
            factory = { context ->
                WebView(context).apply {
                    settings.apply {
                        javaScriptEnabled = true
                        domStorageEnabled = true
                        databaseEnabled = true
                        loadWithOverviewMode = true
                        useWideViewPort = true
                        builtInZoomControls = true
                        displayZoomControls = false
                        setSupportZoom(true)
                        cacheMode = android.webkit.WebSettings.LOAD_DEFAULT
                    }

                    webViewClient = object : WebViewClient() {
                        override fun shouldOverrideUrlLoading(
                            view: WebView,
                            request: WebResourceRequest
                        ): Boolean {
                            val target = request.url
                            val scheme = target.scheme ?: return false
                            if (scheme == "http" || scheme == "https") {
                                return false
                            }
                            // Hand off mailto:, tel:, intent://, market://, etc. to the OS
                            return runCatching {
                                view.context.startActivity(
                                    Intent(Intent.ACTION_VIEW, target).addFlags(
                                        Intent.FLAG_ACTIVITY_NEW_TASK
                                    )
                                )
                                true
                            }.getOrDefault(false)
                        }
                    }

                    webChromeClient = object : WebChromeClient() {
                        override fun onProgressChanged(view: WebView?, newProgress: Int) {
                            progress = newProgress / 100f
                            loading = newProgress in 1..99
                        }
                    }

                    loadUrl(url)
                    onWebViewReady(this)
                }
            },
            modifier = Modifier.fillMaxSize()
        )

        if (loading) {
            LinearProgressIndicator(
                progress = { progress },
                modifier = Modifier
                    .fillMaxWidth()
                    .align(Alignment.TopCenter)
            )
        }
    }
}
