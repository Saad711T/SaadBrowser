package com.example.mybrowser

import android.annotation.SuppressLint
import android.os.Bundle
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.viewinterop.AndroidView
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import com.example.mybrowser.ui.theme.MyBrowserTheme

class MainActivity : ComponentActivity() {

    private var webView: WebView? = null
    private val homeUrl = "https://calm-daffodil-7a888b.netlify.app/" //

    @SuppressLint("SetJavaScriptEnabled")
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            MyBrowserTheme {
                WebViewScreen(homeUrl) {
                    webView = it
                }
            }
        }
    }

    override fun onBackPressed() {
        webView?.let {
            if (it.url != homeUrl) {
                it.loadUrl(homeUrl)
            } else {
                super.onBackPressed()
            }
        } ?: super.onBackPressed()
    }
}

@Composable
fun WebViewScreen(url: String, onWebViewReady: (WebView) -> Unit) {
    AndroidView(
        factory = { context ->
            WebView(context).apply {
                webViewClient = WebViewClient()
                settings.javaScriptEnabled = true
                loadUrl(url)
                onWebViewReady(this)
            }
        },
        modifier = Modifier.fillMaxSize()
    )
}
