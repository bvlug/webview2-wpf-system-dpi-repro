using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace WV2DpiRepro
{
    public partial class MainWindow : Window
    {
        bool _isOpen;
        WebView2 _wv;
        CoreWebView2Environment _env;

        readonly string _logPath = Path.Combine(Path.GetTempPath(), "wv2-dpi-repro.log");

        public MainWindow() => InitializeComponent();

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var sdkVer = typeof(CoreWebView2Environment).Assembly.GetName().Version;
            Log($"=== WV2DpiRepro  SDK {sdkVer}  PID {System.Diagnostics.Process.GetCurrentProcess().Id} ===");
            Log($"Log file → {_logPath}");
            Log("Start at 100 % scale, open WebView2, change to 150 %, close & reopen, then click cells.");
        }

        async void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            btnToggle.IsEnabled = false;
            try
            {
                if (_isOpen) await CloseWebView2();
                else         await OpenWebView2();
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                btnToggle.IsEnabled = true;
            }
        }

        async Task OpenWebView2()
        {
            Log("─── Open ──────────────────────────────────────────────────");
            LogDpiState("OPEN");

            if (_env == null)
            {
                var udf  = Path.Combine(Path.GetTempPath(), "wv2-dpi-repro-udf");
                var opts = new CoreWebView2EnvironmentOptions { Language = "nl-NL" };
                _env = await CoreWebView2Environment.CreateAsync(null, udf, opts);
                Log($"  Env created  runtime={_env.BrowserVersionString}");
            }

            _wv = new WebView2();
            wvContainer.Child = _wv;
            await _wv.EnsureCoreWebView2Async(_env);
            Log("  Stock WPF WebView2 ready");
            LogBounds("OPEN");
            _wv.CoreWebView2.NavigateToString(GridHtml);

            _isOpen = true;
            btnToggle.Content = "Close WebView2";
        }

        Task CloseWebView2()
        {
            Log("─── Close ─────────────────────────────────────────────────");
            wvContainer.Child = null;
            _wv.Dispose();
            _wv = null;
            _isOpen = false;
            btnToggle.Content = "Open WebView2";
            return Task.CompletedTask;
        }

        void LogDpiState(string tag)
        {
            var src = PresentationSource.FromVisual(this);
            double m11 = (src?.CompositionTarget != null)
                ? src.CompositionTarget.TransformToDevice.M11
                : double.NaN;

            var hwnd = new WindowInteropHelper(this).Handle;
            var hmon = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            NativeMethods.GetDpiForMonitor(hmon, 0 /* MDT_EFFECTIVE_DPI */, out uint effX, out uint effY);
            NativeMethods.GetDpiForMonitor(hmon, 2 /* MDT_RAW_DPI       */, out uint rawX, out uint rawY);

            var sb = new StringBuilder();
            sb.AppendLine($"  PresentationSource M11  = {m11:F4}");
            sb.AppendLine($"  MDT_EFFECTIVE_DPI       = {effX}x{effY} dpi  ({effX / 96.0:F3}×)");
            sb.AppendLine($"  MDT_RAW_DPI             = {rawX}x{rawY} dpi");

            string text = sb.ToString().TrimEnd();
            Log($"[{tag}]\n{text}");
            AppendToFile($"[{tag}] @ {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n{text}\n");
        }

        void LogBounds(string tag)
        {
            var sz = wvContainer.RenderSize;
            string line = $"  wvContainer.RenderSize  = {sz.Width:F1} × {sz.Height:F1} px";
            Log(line);
            AppendToFile($"[{tag}] {line.TrimStart()}\n");
        }

        void AppendToFile(string text)
        {
            try { File.AppendAllText(_logPath, text + "\n"); }
            catch { }
        }

        void BtnClearLog_Click(object sender, RoutedEventArgs e) => logText.Text = string.Empty;

        void Log(string msg)
        {
            logText.Text += $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n";
            logScroll.ScrollToEnd();
        }

        static readonly string GridHtml =
@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
body {
  font-family: 'Segoe UI', sans-serif;
  height: 100vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: #1a1a1a;
}
#status {
  padding: 7px 10px;
  background: #0d0d0d;
  color: #4ec9b0;
  font-family: 'Courier New', monospace;
  font-size: 12px;
  flex-shrink: 0;
  border-bottom: 1px solid #333;
}
#grid {
  display: grid;
  grid-template-columns: repeat(10, 1fr);
  grid-template-rows: repeat(10, 1fr);
  gap: 3px;
  padding: 6px;
  flex: 1;
}
.cell {
  background: #2d3a44;
  border-radius: 3px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 10px;
  font-weight: 700;
  color: #8ba0ae;
  user-select: none;
}
.cell:hover { background: #3c5060; color: #cde; }
.cell.hit  { background: #2ea043; color: #fff; }
.cell.prev { background: #5a6e78; color: #ccc; }
</style>
</head>
<body>
<div id=""status"">Click any cell — observe which cell VISUALLY highlights vs. which label is logged.</div>
<div id=""grid""></div>
<script>
var g = document.getElementById('grid');
var s = document.getElementById('status');
var last = null, prev = null;
for (var r = 1; r <= 10; r++) {
  for (var c = 1; c <= 10; c++) {
    (function(row, col) {
      var d = document.createElement('div');
      d.className = 'cell';
      d.textContent = 'R' + row + 'C' + col;
      d.addEventListener('click', function(e) {
        if (prev) prev.classList.remove('prev');
        if (last) { last.classList.remove('hit'); last.classList.add('prev'); prev = last; }
        d.classList.add('hit'); last = d;
        s.textContent =
          'HIT: R' + row + 'C' + col +
          '  |  client(' + e.clientX + ', ' + e.clientY + ')' +
          '  screen(' + e.screenX + ', ' + e.screenY + ')' +
          '  offset(' + e.offsetX + ', ' + e.offsetY + ')';
      });
      g.appendChild(d);
    })(r, c);
  }
}
</script>
</body>
</html>";
    }
}
