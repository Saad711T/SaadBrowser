package com.example.mybrowser

import android.annotation.SuppressLint
import android.content.Intent
import android.graphics.Bitmap
import android.os.Bundle
import android.webkit.CookieManager
import android.webkit.WebChromeClient
import android.webkit.WebResourceRequest
import android.webkit.WebSettings
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
import androidx.webkit.WebViewCompat
import androidx.webkit.WebViewFeature
import com.example.mybrowser.ui.theme.MyBrowserTheme

class MainActivity : ComponentActivity() {

    private var webView: WebView? = null
    private val homeUrl = "https://calm-daffodil-7a888b.netlify.app/"
    private val antiFingerprintScript: String by lazy {
        runCatching {
            assets.open("anti-fingerprint.js").bufferedReader().use { it.readText() }
        }.getOrDefault("")
    }

    @SuppressLint("SetJavaScriptEnabled")
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        // Persist cookies across launches so logins survive app restart.
        CookieManager.getInstance().setAcceptCookie(true)

        // Opt into Safe Browsing globally where supported.
        if (WebViewFeature.isFeatureSupported(WebViewFeature.START_SAFE_BROWSING)) {
            androidx.webkit.WebViewCompat.startSafeBrowsing(applicationContext) { /* ready */ }
        }

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
                BrowserScreen(homeUrl, antiFingerprintScript) { webView = it }
            }
        }
    }

    override fun onPause() {
        super.onPause()
        // Force cookies (auth tokens) to disk so we don't lose logins on kill.
        CookieManager.getInstance().flush()
        webView?.onPause()
    }

    override fun onResume() {
        super.onResume()
        webView?.onResume()
    }

    override fun onDestroy() {
        webView?.destroy()
        webView = null
        super.onDestroy()
    }
}

@SuppressLint("SetJavaScriptEnabled")
@Composable
fun BrowserScreen(
    url: String,
    antiFingerprintScript: String,
    onWebViewReady: (WebView) -> Unit
) {
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
                        cacheMode = WebSettings.LOAD_DEFAULT
                        // Privacy: refuse downgraded resources.
                        mixedContentMode = WebSettings.MIXED_CONTENT_NEVER_ALLOW
                        // Privacy: keep app sandbox closed off from web content.
                        allowFileAccess = false
                        allowContentAccess = false
                        // Send DNT alongside the JS-side navigator.doNotTrack shim.
                        if (WebViewFeature.isFeatureSupported(
                                WebViewFeature.REQUESTED_WITH_HEADER_ALLOW_LIST
                            )
                        ) {
                            androidx.webkit.WebSettingsCompat.setRequestedWithHeaderOriginAllowList(
                                this,
                                emptySet()
                            )
                        }
                        if (WebViewFeature.isFeatureSupported(WebViewFeature.SAFE_BROWSING_ENABLE)) {
                            androidx.webkit.WebSettingsCompat.setSafeBrowsingEnabled(this, true)
                        }
                    }

                    // Cookies (per-WebView) — keep first-party logins working,
                    // and let the user stay signed-in after restart.
                    CookieManager.getInstance().setAcceptThirdPartyCookies(this, true)

                    // Inject the anti-fingerprint shim before page scripts run.
                    if (antiFingerprintScript.isNotEmpty() &&
                        WebViewFeature.isFeatureSupported(WebViewFeature.DOCUMENT_START_SCRIPT)
                    ) {
                        WebViewCompat.addDocumentStartJavaScript(
                            this,
                            antiFingerprintScript,
                            setOf("*")
                        )
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

                        override fun onPageStarted(
                            view: WebView,
                            url: String?,
                            favicon: Bitmap?
                        ) {
                            super.onPageStarted(view, url, favicon)
                            // Fallback when DOCUMENT_START_SCRIPT is unsupported.
                            if (antiFingerprintScript.isNotEmpty() &&
                                !WebViewFeature.isFeatureSupported(
                                    WebViewFeature.DOCUMENT_START_SCRIPT
                                )
                            ) {
                                view.evaluateJavascript(antiFingerprintScript, null)
                            }
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
