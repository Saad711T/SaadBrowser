using System;
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
        private ToolStripTextBox addressBar = new ToolStripTextBox { AutoSize = false, Width = 600 };


        private readonly string homeUrl = "https://calm-daffodil-7a888b.netlify.app/";
        private readonly string searchUrlTemplate = "https://www.google.com/search?q={0}";

        public Form1()
        {
            InitializeComponent();

            KeyPreview = true;


            toolStrip.Items.AddRange(new ToolStripItem[] {
                btnBack, btnForward, btnReload, btnStop, btnHome, new ToolStripSeparator(),
                btnNewTab, btnCloseTab, new ToolStripSeparator(), addressBar
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
                DefaultBackgroundColor = System.Drawing.Color.White
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

            wv.CoreWebView2InitializationCompleted += (_, e) =>
            {
                if (!e.IsSuccess) return;

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
