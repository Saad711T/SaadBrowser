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

        private ToolStripButton btnBack = new ToolStripButton("⟵") { ToolTipText = "رجوع / Back" };
        private ToolStripButton btnForward = new ToolStripButton("⟶") { ToolTipText = "تقدم / Next" };
        private ToolStripButton btnHome = new ToolStripButton("⌂") { ToolTipText = "الصفحة الرئيسية / Main menu" };
        private ToolStripButton btnNewTab = new ToolStripButton("+") { ToolTipText = "تبويب جديد / New Tab" };
        private ToolStripButton btnCloseTab = new ToolStripButton("×") { ToolTipText = "إغلاق التبويب / Exit Tab" };
        private ToolStripTextBox addressBar = new ToolStripTextBox { AutoSize = false, Width = 600 };


        private readonly string homeUrl = "https://calm-daffodil-7a888b.netlify.app/";

        public Form1()
        {
            InitializeComponent();




            toolStrip.Items.AddRange(new ToolStripItem[] {
                btnBack, btnForward, btnHome, new ToolStripSeparator(),
                btnNewTab, btnCloseTab, new ToolStripSeparator(), addressBar
            });



            Controls.Remove(webView21);


            Controls.Add(tabControl);
            Controls.Add(toolStrip);

            btnBack.Click += (s, e) => { var wv = CurrentWebView(); if (wv != null && wv.CanGoBack) wv.GoBack(); };
            btnForward.Click += (s, e) => { var wv = CurrentWebView(); if (wv != null && wv.CanGoForward) wv.GoForward(); };
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



                    string url = text.ToLower() == "home" ? homeUrl : text;

                    if (!text.StartsWith("http://" , StringComparison.OrdinalIgnoreCase) &&
                        
                        !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                        text.ToLower() != "home")
                    {
                        url = "https://" + text;
                    }

                    Navigate(url);
                }
            };

            tabControl.SelectedIndexChanged += (s, e) => SyncUIWithCurrentTab();

            var firstTab = new TabPage("Tab 1");
            tabControl.TabPages.Add(firstTab);

            webView21.Dock = DockStyle.Fill;
            firstTab.Controls.Add(webView21);

            WireWebViewEvents(webView21);

            webView21.Source = new Uri(homeUrl);
            addressBar.Text = "home";
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

        private void Navigate(string url)
        {
            var wv = CurrentWebView();
            if (wv == null) return;

            if (wv.CoreWebView2 != null)
                wv.CoreWebView2.Navigate(url);
            else
                wv.Source = new Uri(url);



                            //addressBar.Text = wv.Source?.AbsoluteUri ?? homeUrl;

            addressBar.Text = url == homeUrl ? "home" : url;
        }

        private void WireWebViewEvents(WebView2 wv)
        {
            wv.NavigationCompleted += (_, __) => SyncUIWithCurrentTab();

            wv.CoreWebView2InitializationCompleted += (_, e) =>
            {
                if (e.IsSuccess)
                {
                    wv.CoreWebView2.HistoryChanged += (_, __) => SyncUIWithCurrentTab();
                    wv.CoreWebView2.NavigationStarting += (_, args) =>
                    {
                        if (tabControl.SelectedTab?.Controls.Contains(wv) == true)
                            addressBar.Text = args.Uri == homeUrl ? "home" : args.Uri;
                    };
                }
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
