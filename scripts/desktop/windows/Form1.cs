using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace SaadBrowser
{
    public partial class Form1 : Form
    {


        private TabControl tabControl = new TabControl { Dock = DockStyle.Fill };
        private ToolStrip toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };
        private StatusStrip statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        private ToolStripStatusLabel statusLabel = new ToolStripStatusLabel("Ready");
        private ToolStripProgressBar progressBar = new ToolStripProgressBar { Visible = false, Style = ProgressBarStyle.Marquee };

        private ToolStripButton btnBack = new ToolStripButton("⟵") { ToolTipText = "رجوع / Back" };
        private ToolStripButton btnForward = new ToolStripButton("⟶") { ToolTipText = "تقدم / Next" };
        private ToolStripButton btnReload = new ToolStripButton("⟳") { ToolTipText = "تحديث / Reload (F5)" };
        private ToolStripButton btnStop = new ToolStripButton("✕") { ToolTipText = "إيقاف / Stop (Esc)" };
        private ToolStripButton btnHome = new ToolStripButton("⌂") { ToolTipText = "الصفحة الرئيسية / Main menu" };
        private ToolStripButton btnNewTab = new ToolStripButton("+") { ToolTipText = "تبويب جديد / New Tab (Ctrl+T)" };
        private ToolStripButton btnCloseTab = new ToolStripButton("×") { ToolTipText = "إغلاق التبويب / Exit Tab (Ctrl+W)" };
        private ToolStripButton btnClearData = new ToolStripButton("🛡") { ToolTipText = "مسح بيانات التصفح / Clear browsing data" };
        private ToolStripTextBox addressBar = new ToolStripTextBox { AutoSize = false, Width = 600 };


        private readonly string homeUrl = "https://calm-daffodil-7a888b.netlify.app/";
        private readonly string searchUrlTemplate = "https://www.google.com/search?q={0}";

        // Persistent profile storage so cookies / logins survive restart and
        // self-extracting single-file deploys.
        private static readonly string UserDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaadBrowser", "WebView2");

        // Anti-fingerprint shim injected into every document at creation time.
        // Mirrors scripts/phone/android/app/src/main/assets/anti-fingerprint.js.
        private const string AntiFingerprintScript = """
        (function () {
          'use strict';
          function safeDefine(obj, prop, value) {
            try {
              Object.defineProperty(obj, prop, { get: function () { return value; }, configurable: true });
            } catch (e) {}
          }
          safeDefine(navigator, 'hardwareConcurrency', 8);
          safeDefine(navigator, 'deviceMemory', 8);
          safeDefine(navigator, 'doNotTrack', '1');
          safeDefine(navigator, 'webdriver', false);
          safeDefine(navigator, 'languages', Object.freeze(['en-US', 'en']));
          try {
            safeDefine(navigator, 'plugins', Object.freeze([]));
            safeDefine(navigator, 'mimeTypes', Object.freeze([]));
          } catch (e) {}
          try {
            var origToDataURL = HTMLCanvasElement.prototype.toDataURL;
            HTMLCanvasElement.prototype.toDataURL = function () {
              try {
                var ctx = this.getContext('2d');
                if (ctx && this.width > 0 && this.height > 0) {
                  var img = ctx.getImageData(0, 0, this.width, this.height);
                  for (var i = 0; i < img.data.length; i += 4) {
                    if (Math.random() < 0.003) img.data[i] ^= 1;
                  }
                  ctx.putImageData(img, 0, 0);
                }
              } catch (e) {}
              return origToDataURL.apply(this, arguments);
            };
          } catch (e) {}
          try {
            var spoof = function (orig) {
              return function (p) {
                if (p === 37445) return 'Intel Inc.';
                if (p === 37446) return 'Intel Iris OpenGL Engine';
                return orig.call(this, p);
              };
            };
            if (window.WebGLRenderingContext) {
              WebGLRenderingContext.prototype.getParameter = spoof(WebGLRenderingContext.prototype.getParameter);
            }
            if (window.WebGL2RenderingContext) {
              WebGL2RenderingContext.prototype.getParameter = spoof(WebGL2RenderingContext.prototype.getParameter);
            }
          } catch (e) {}
          try {
            if (window.AudioBuffer) {
              var origGetChannelData = AudioBuffer.prototype.getChannelData;
              AudioBuffer.prototype.getChannelData = function () {
                var data = origGetChannelData.apply(this, arguments);
                for (var i = 0; i < data.length; i += 100) {
                  data[i] = data[i] + (Math.random() - 0.5) * 1e-7;
                }
                return data;
              };
            }
          } catch (e) {}
        })();
        """;

        public Form1()
        {
            InitializeComponent();

            // Pin the WebView2 user-data folder before the control auto-initialises
            // so cookies/sessions land in a known persistent location.
            webView21.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = UserDataFolder
            };

            KeyPreview = true;


            toolStrip.Items.AddRange(new ToolStripItem[] {
                btnBack, btnForward, btnReload, btnStop, btnHome, new ToolStripSeparator(),
                btnNewTab, btnCloseTab, new ToolStripSeparator(), addressBar, new ToolStripSeparator(),
                btnClearData
            });

            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(progressBar);


            Controls.Remove(webView21);


            Controls.Add(tabControl);
            Controls.Add(toolStrip);
            Controls.Add(statusStrip);

            btnBack.Click += (s, e) => { var wv = CurrentWebView(); if (wv != null && wv.CanGoBack) wv.GoBack(); };
            btnForward.Click += (s, e) => { var wv = CurrentWebView(); if (wv != null && wv.CanGoForward) wv.GoForward(); };
            btnReload.Click += (s, e) => CurrentWebView()?.Reload();
            btnStop.Click += (s, e) => CurrentWebView()?.CoreWebView2?.Stop();
            btnHome.Click += (s, e) => Navigate(homeUrl);
            btnNewTab.Click += (s, e) => CreateTab(homeUrl);
            btnCloseTab.Click += (s, e) => CloseCurrentTab();
            btnClearData.Click += async (s, e) => await ClearBrowsingDataAsync();

            addressBar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    var text = addressBar.Text.Trim();
                    if (string.IsNullOrEmpty(text)) return;

                    Navigate(ResolveUserInput(text));
                }
            };

            KeyDown += Form1_KeyDown;

            tabControl.SelectedIndexChanged += (s, e) => SyncUIWithCurrentTab();

            var firstTab = new TabPage("Tab 1");
            tabControl.TabPages.Add(firstTab);

            webView21.Dock = DockStyle.Fill;
            firstTab.Controls.Add(webView21);

            WireWebViewEvents(webView21);

            webView21.Source = new Uri(homeUrl);
            addressBar.Text = "home";
        }

        private string ResolveUserInput(string text)
        {
            if (text.Equals("home", StringComparison.OrdinalIgnoreCase))
                return homeUrl;

            if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return text;

            // Treat as URL when it contains a dot and no spaces (e.g. example.com, sub.domain.tld/path)
            if (!text.Contains(' ') && text.Contains('.'))
                return "https://" + text;

            // Otherwise, search the query
            return string.Format(searchUrlTemplate, Uri.EscapeDataString(text));
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T) { CreateTab(homeUrl); e.Handled = true; return; }
            if (e.Control && e.KeyCode == Keys.W) { CloseCurrentTab(); e.Handled = true; return; }
            if (e.Control && e.KeyCode == Keys.L) { addressBar.Focus(); addressBar.SelectAll(); e.Handled = true; return; }
            if (e.Control && e.KeyCode == Keys.R) { CurrentWebView()?.Reload(); e.Handled = true; return; }
            if (e.KeyCode == Keys.F5) { CurrentWebView()?.Reload(); e.Handled = true; return; }
            if (e.KeyCode == Keys.Escape) { CurrentWebView()?.CoreWebView2?.Stop(); e.Handled = true; return; }
        }

        private void CreateTab(string url)
        {
            var page = new TabPage($"Tab {tabControl.TabPages.Count + 1}");
            var wv = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = System.Drawing.Color.White,
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = UserDataFolder
                }
            };
            page.Controls.Add(wv);
            tabControl.TabPages.Add(page);
            tabControl.SelectedTab = page;

            WireWebViewEvents(wv);

            wv.CoreWebView2InitializationCompleted += (_, __) => wv.CoreWebView2.Navigate(url);
            _ = wv.EnsureCoreWebView2Async();
            addressBar.Text = url == homeUrl ? "home" : url;
        }

        private void CloseCurrentTab()
        {
            if (tabControl.TabPages.Count <= 1) return;
            var current = tabControl.SelectedTab;
            var wv = CurrentWebView();
            wv?.Dispose();
            tabControl.TabPages.Remove(current);
        }

        private async System.Threading.Tasks.Task ClearBrowsingDataAsync()
        {
            var wv = CurrentWebView();
            if (wv?.CoreWebView2?.Profile == null)
            {
                statusLabel.Text = "Profile not ready";
                return;
            }

            var confirmed = MessageBox.Show(
                "هل تريد حذف ملفات الكوكيز والتاريخ والكاش؟\nClear cookies, history and cache for this profile?",
                "Saad Browser",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmed != DialogResult.OK) return;

            statusLabel.Text = "Clearing...";
            await wv.CoreWebView2.Profile.ClearBrowsingDataAsync();
            statusLabel.Text = "Browsing data cleared";
        }

        private WebView2? CurrentWebView()
        {
            var page = tabControl.SelectedTab;
            return page?.Controls.OfType<WebView2>().FirstOrDefault();
        }

        private TabPage? TabPageOf(WebView2 wv)
        {
            foreach (TabPage page in tabControl.TabPages)
            {
                if (page.Controls.Contains(wv)) return page;
            }
            return null;
        }

        private void Navigate(string url)
        {
            var wv = CurrentWebView();
            if (wv == null) return;

            if (wv.CoreWebView2 != null)
                wv.CoreWebView2.Navigate(url);
            else
                wv.Source = new Uri(url);

            addressBar.Text = url == homeUrl ? "home" : url;
        }

        private void WireWebViewEvents(WebView2 wv)
        {
            wv.NavigationCompleted += (_, e) =>
            {
                progressBar.Visible = false;
                statusLabel.Text = e.IsSuccess ? "Done" : "Failed to load";
                SyncUIWithCurrentTab();
            };

            wv.CoreWebView2InitializationCompleted += async (_, e) =>
            {
                if (!e.IsSuccess) return;

                ApplyPrivacySettings(wv.CoreWebView2);

                try
                {
                    await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(AntiFingerprintScript);
                }
                catch
                {
                    // Older WebView2 runtimes may reject some shim features; ignore.
                }

                wv.CoreWebView2.HistoryChanged += (_, __) => SyncUIWithCurrentTab();

                wv.CoreWebView2.NavigationStarting += (_, args) =>
                {
                    progressBar.Visible = true;
                    statusLabel.Text = "Loading " + args.Uri;
                    if (tabControl.SelectedTab?.Controls.Contains(wv) == true)
                        addressBar.Text = args.Uri == homeUrl ? "home" : args.Uri;
                };

                wv.CoreWebView2.DocumentTitleChanged += (_, __) =>
                {
                    var page = TabPageOf(wv);
                    if (page == null) return;
                    var title = wv.CoreWebView2.DocumentTitle;
                    if (string.IsNullOrWhiteSpace(title)) return;
                    page.Text = title.Length > 24 ? title.Substring(0, 24) + "…" : title;
                };

                // Open new-window requests in a new tab instead of an external window
                wv.CoreWebView2.NewWindowRequested += (_, args) =>
                {
                    args.Handled = true;
                    CreateTab(args.Uri);
                };
            };

            _ = wv.EnsureCoreWebView2Async();
        }

        private void ApplyPrivacySettings(CoreWebView2 core)
        {
            try
            {
                // Strict tracking prevention blocks known cross-site trackers.
                core.Profile.PreferredTrackingPreventionLevel =
                    CoreWebView2TrackingPreventionLevel.Strict;
            }
            catch
            {
                // Property not available on older WebView2 runtimes.
            }

            try
            {
                var s = core.Settings;
                s.IsPasswordAutosaveEnabled = true;       // login UX
                s.IsGeneralAutofillEnabled = true;        // form UX
                s.AreDefaultContextMenusEnabled = true;
                s.IsStatusBarEnabled = true;
                s.IsSwipeNavigationEnabled = true;
                // Send DNT signal alongside the in-page navigator.doNotTrack shim.
                s.IsReputationCheckingRequired = true;
            }
            catch
            {
            }
        }

        private void SyncUIWithCurrentTab()
        {
            var wv = CurrentWebView();
            if (wv == null) return;

            try
            {
                if (wv.CoreWebView2 != null)
                {
                    btnBack.Enabled = wv.CanGoBack;
                    btnForward.Enabled = wv.CanGoForward;
                    var currentUrl = wv.Source?.AbsoluteUri ?? wv.CoreWebView2.Source;
                    addressBar.Text = currentUrl == homeUrl ? "home" : currentUrl;
                }
                else
                {
                    btnBack.Enabled = btnForward.Enabled = false;
                    addressBar.Text = "home";
                }
            }
            catch
            {
            }
        }



    }




}
