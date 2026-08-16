using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace GoProImporter;

public sealed class Form1 : Form
{
    private readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = true })
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    private readonly AppSettings _settings = AppSettings.Load();
    private CancellationTokenSource? _cts;
    private StreamWriter? _sessionLog;
    private string? _sessionLogPath;

    private int _downloadedCount;
    private int _duplicateCount;
    private int _verifiedCount;
    private int _verificationFailureCount;
    private int _deletedCount;
    private int _deleteErrorCount;
    private int _errorCount;

    private readonly TextBox txtUrl = new();
    private readonly ComboBox cboFolder = new();
    private readonly Button btnBrowse = new();
    private readonly Button btnStart = new();
    private readonly Button btnStop = new();
    private readonly Button btnOpenFolder = new();
    private readonly TextBox txtLog = new();
    private readonly ProgressBar progress = new();
    private readonly Label lblStatus = new();
    private readonly Label lblDeleteWarning = new();
    private readonly CheckBox chkPhotos = new();
    private readonly CheckBox chkVideos = new();
    private readonly CheckBox chkLrv = new();
    private readonly CheckBox chkThm = new();
    private readonly CheckBox chkOther = new();
    private readonly CheckBox chkByDate = new();
    private readonly CheckBox chkVerify = new();
    private readonly CheckBox chkDelete = new();

    private static readonly Color Bg = Color.FromArgb(24, 26, 29);
    private static readonly Color Surface2 = Color.FromArgb(46, 50, 55);
    private static readonly Color TextPrimary = Color.FromArgb(238, 241, 244);
    private static readonly Color TextMuted = Color.FromArgb(169, 176, 184);
    private static readonly Color Accent = Color.FromArgb(0, 163, 224);
    private static readonly Color Danger = Color.FromArgb(194, 64, 64);
    private static readonly Color Warning = Color.FromArgb(235, 166, 52);

    private static readonly HashSet<string> PhotoExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tif", ".tiff", ".heic", ".heif"
    };

    private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv"
    };

    public Form1()
    {
        SetupUi();
        LoadSettingsIntoUi();
    }

    private void SetupUi()
    {
        Text = "GoPro Importer v1.6.2";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(940, 720);
        MinimumSize = new Size(850, 650);
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9F);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        var title = new Label
        {
            Text = "GoPro Importer",
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            ForeColor = TextPrimary,
            AutoSize = true,
            Left = 18,
            Top = 14
        };

        var version = new Label
        {
            Text = "v1.6.2  •  verified LAN media import",
            ForeColor = TextMuted,
            AutoSize = true,
            Left = 20,
            Top = 50
        };

        var lblUrl = MakeLabel("GoPro URL", 20, 86);
        txtUrl.SetBounds(20, 108, 900, 30);
        StyleTextBox(txtUrl);

        var lblFolder = MakeLabel("Save to", 20, 151);
        cboFolder.SetBounds(20, 173, 744, 30);
        cboFolder.DropDownStyle = ComboBoxStyle.DropDown;
        cboFolder.FlatStyle = FlatStyle.Flat;
        cboFolder.BackColor = Surface2;
        cboFolder.ForeColor = TextPrimary;
        cboFolder.Font = new Font("Segoe UI", 10F);

        btnBrowse.SetBounds(775, 172, 145, 32);
        btnBrowse.Text = "Browse…";
        StyleButton(btnBrowse, Surface2);
        btnBrowse.Click += (_, _) => BrowseForFolder();

        SetupCheck(chkPhotos, "Photos", 20, true);
        SetupCheck(chkVideos, "Videos", 108, true);
        SetupCheck(chkLrv, "LRV", 196, false);
        SetupCheck(chkThm, "THM", 266, false);
        SetupCheck(chkOther, "Other", 340, true);

        chkByDate.Text = "Store by date (Last-Modified)";
        chkByDate.SetBounds(435, 220, 245, 24);
        chkByDate.Checked = true;
        StyleCheckBox(chkByDate);

        chkVerify.Text = "Verify downloaded files";
        chkVerify.SetBounds(20, 258, 190, 24);
        chkVerify.Checked = true;
        StyleCheckBox(chkVerify);

        chkDelete.Text = "Delete from GoPro after successful verification";
        chkDelete.SetBounds(225, 258, 330, 24);
        chkDelete.Checked = false;
        StyleCheckBox(chkDelete);
        chkDelete.CheckedChanged += (_, _) =>
        {
            if (chkDelete.Checked)
                chkVerify.Checked = true;
            lblDeleteWarning.Visible = chkDelete.Checked;
        };

        lblDeleteWarning.Text = "Deletion is permanent. Camera files are only removed after verification succeeds.";
        lblDeleteWarning.SetBounds(20, 286, 800, 20);
        lblDeleteWarning.ForeColor = Warning;
        lblDeleteWarning.Visible = false;

        btnStart.SetBounds(20, 322, 130, 36);
        btnStart.Text = "Start Import";
        StyleButton(btnStart, Accent);
        btnStart.Click += async (_, _) => await StartAsync();

        btnStop.SetBounds(160, 322, 105, 36);
        btnStop.Text = "Stop";
        btnStop.Enabled = false;
        StyleButton(btnStop, Danger);
        btnStop.Click += (_, _) => _cts?.Cancel();

        btnOpenFolder.SetBounds(275, 322, 125, 36);
        btnOpenFolder.Text = "Open Folder";
        StyleButton(btnOpenFolder, Surface2);
        btnOpenFolder.Click += (_, _) => OpenDestinationFolder();

        progress.SetBounds(420, 329, 500, 22);
        progress.Style = ProgressBarStyle.Continuous;

        lblStatus.SetBounds(20, 371, 900, 22);
        lblStatus.Text = "Ready";
        lblStatus.ForeColor = TextMuted;

        txtLog.SetBounds(20, 402, 900, 298);
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.ReadOnly = true;
        txtLog.BackColor = Color.FromArgb(17, 19, 21);
        txtLog.ForeColor = Color.FromArgb(205, 213, 221);
        txtLog.BorderStyle = BorderStyle.FixedSingle;
        txtLog.Font = new Font("Cascadia Mono", 9F);

        Controls.AddRange(new Control[]
        {
            title, version, lblUrl, txtUrl, lblFolder, cboFolder, btnBrowse,
            chkPhotos, chkVideos, chkLrv, chkThm, chkOther, chkByDate,
            chkVerify, chkDelete, lblDeleteWarning,
            btnStart, btnStop, btnOpenFolder, progress, lblStatus, txtLog
        });

        Resize += (_, _) => LayoutResponsive();
        LayoutResponsive();
    }

    private void SetupCheck(CheckBox box, string text, int left, bool isChecked)
    {
        box.Text = text;
        box.SetBounds(left, 220, 82, 24);
        box.Checked = isChecked;
        StyleCheckBox(box);
    }

    private Label MakeLabel(string text, int left, int top) => new()
    {
        Text = text,
        Left = left,
        Top = top,
        AutoSize = true,
        ForeColor = TextMuted,
        Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
    };

    private static void StyleTextBox(TextBox box)
    {
        box.BackColor = Surface2;
        box.ForeColor = TextPrimary;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = new Font("Segoe UI", 10F);
    }

    private static void StyleCheckBox(CheckBox box)
    {
        box.ForeColor = TextPrimary;
        box.BackColor = Bg;
        box.FlatStyle = FlatStyle.Flat;
    }

    private static void StyleButton(Button button, Color color)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = color;
        button.ForeColor = TextPrimary;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
    }

    private void LayoutResponsive()
    {
        int right = ClientSize.Width - 20;
        txtUrl.Width = Math.Max(300, right - txtUrl.Left);
        btnBrowse.Left = Math.Max(625, right - btnBrowse.Width);
        cboFolder.Width = Math.Max(350, btnBrowse.Left - cboFolder.Left - 11);
        progress.Left = 420;
        progress.Width = Math.Max(220, right - progress.Left);
        lblStatus.Width = Math.Max(300, right - lblStatus.Left);
        txtLog.Width = Math.Max(500, right - txtLog.Left);
        txtLog.Height = Math.Max(180, ClientSize.Height - txtLog.Top - 20);
    }

    private void LoadSettingsIntoUi()
    {
        txtUrl.Text = string.IsNullOrWhiteSpace(_settings.GoProUrl)
            ? "http://10.5.5.9/videos/DCIM/100GOPRO/"
            : _settings.GoProUrl;
        RefreshRecentFolders();
        cboFolder.Text = _settings.RecentFolders.Count > 0 ? _settings.RecentFolders[0] : @"E:\Gopro";
        chkByDate.Checked = true;
        chkVerify.Checked = true;
        chkDelete.Checked = false;
    }

    private void RefreshRecentFolders()
    {
        string current = cboFolder.Text;
        cboFolder.BeginUpdate();
        cboFolder.Items.Clear();
        foreach (string folder in _settings.RecentFolders)
            cboFolder.Items.Add(folder);
        cboFolder.EndUpdate();
        if (!string.IsNullOrWhiteSpace(current))
            cboFolder.Text = current;
    }

    private void BrowseForFolder()
    {
        using var fbd = new FolderBrowserDialog
        {
            Description = "Choose where GoPro media should be saved",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(cboFolder.Text)
                ? cboFolder.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
        };

        if (fbd.ShowDialog() != DialogResult.OK)
            return;

        cboFolder.Text = fbd.SelectedPath;
        RememberSettings();
    }

    private void OpenDestinationFolder()
    {
        string folder = cboFolder.Text.Trim();
        if (!Directory.Exists(folder))
        {
            MessageBox.Show(this, "The destination folder does not exist yet.", "Open Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could Not Open Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RememberSettings()
    {
        _settings.GoProUrl = txtUrl.Text.Trim();
        if (!string.IsNullOrWhiteSpace(cboFolder.Text))
            _settings.AddRecentFolder(cboFolder.Text.Trim());
        _settings.Save();
        RefreshRecentFolders();
    }

    private bool PassesFilters(string fileName)
    {
        string ext = Path.GetExtension(fileName);
        bool isPhoto = PhotoExts.Contains(ext);
        bool isVideo = VideoExts.Contains(ext);
        bool isLrv = ext.Equals(".lrv", StringComparison.OrdinalIgnoreCase);
        bool isThm = ext.Equals(".thm", StringComparison.OrdinalIgnoreCase);
        bool isOther = !isPhoto && !isVideo && !isLrv && !isThm;

        return (isPhoto && chkPhotos.Checked)
            || (isVideo && chkVideos.Checked)
            || (isLrv && chkLrv.Checked)
            || (isThm && chkThm.Checked)
            || (isOther && chkOther.Checked);
    }

    private async Task StartAsync()
    {
        string baseUrl = txtUrl.Text.Trim();
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            MessageBox.Show(this, "Enter a valid GoPro HTTP URL.", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!baseUrl.EndsWith('/'))
            baseUrl += "/";

        string outDir = cboFolder.Text.Trim();
        if (string.IsNullOrWhiteSpace(outDir))
        {
            MessageBox.Show(this, "Choose a destination folder first.", "Destination Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (chkDelete.Checked)
        {
            chkVerify.Checked = true;
            var confirmation = MessageBox.Show(
                this,
                "Delete from GoPro is enabled.\n\nFiles will be permanently removed from the camera only after the local copy is successfully verified. Files that fail verification will remain on the camera.\n\nContinue?",
                "Confirm Camera Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
                return;
        }

        try
        {
            Directory.CreateDirectory(outDir);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"The destination folder could not be created.\n\n{ex.Message}", "Destination Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        txtUrl.Text = baseUrl;
        RememberSettings();
        txtLog.Clear();
        ResetCounters();
        btnStart.Enabled = false;
        btnStop.Enabled = true;
        progress.Value = 0;
        _cts = new CancellationTokenSource();

        _sessionLogPath = Path.Combine(outDir, $"GoProImporter-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        try
        {
            _sessionLog = new StreamWriter(_sessionLogPath, append: false) { AutoFlush = true };
        }
        catch
        {
            _sessionLog = null;
            _sessionLogPath = null;
        }

        try
        {
            Log("SESSION START");
            Log($"SOURCE {baseUrl}");
            Log($"DEST   {outDir}");
            Log($"MODE   Verify={chkVerify.Checked}, DeleteAfterVerify={chkDelete.Checked}");

            SetStatus("Scanning GoPro…");
            var files = await CrawlCollectFilesAsync(baseUrl, _cts.Token);
            files = files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            SetProgress(0, files.Count);
            SetStatus($"Found {files.Count} files");
            Log($"FOUND {files.Count} total files");

            int done = 0;
            foreach (string fileUrl in files)
            {
                _cts.Token.ThrowIfCancellationRequested();
                string name = Path.GetFileName(Uri.UnescapeDataString(new Uri(fileUrl).LocalPath));

                if (!PassesFilters(name))
                {
                    done++;
                    SetProgress(done, files.Count);
                    continue;
                }

                try
                {
                    await DownloadOneAsync(fileUrl, outDir, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _errorCount++;
                    Log($"ERROR {name}: {ex.Message}");
                }

                done++;
                SetProgress(done, files.Count);
                SetStatus($"Processed {done}/{files.Count}  •  Saved {_downloadedCount}  •  Verified {_verifiedCount}  •  Deleted {_deletedCount}");
            }

            bool clean = _errorCount == 0 && _verificationFailureCount == 0 && _deleteErrorCount == 0;
            SetStatus(clean ? "Done" : "Done with warnings");
            LogSummary();

            MessageBox.Show(
                this,
                BuildSummaryText(),
                "Import Complete",
                MessageBoxButtons.OK,
                clean ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Canceled");
            Log("CANCELED");
            LogSummary();
        }
        catch (Exception ex)
        {
            SetStatus("Failed");
            Log("FATAL: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            _cts?.Dispose();
            _cts = null;
            _sessionLog?.Dispose();
            _sessionLog = null;
        }
    }

    private void ResetCounters()
    {
        _downloadedCount = 0;
        _duplicateCount = 0;
        _verifiedCount = 0;
        _verificationFailureCount = 0;
        _deletedCount = 0;
        _deleteErrorCount = 0;
        _errorCount = 0;
    }

    private string BuildSummaryText()
    {
        string logLine = _sessionLogPath is null ? string.Empty : $"\nLog: {_sessionLogPath}";
        return $"Import complete.\n\nDownloaded: {_downloadedCount}\nDuplicates skipped: {_duplicateCount}\nVerified: {_verifiedCount}\nVerification failures: {_verificationFailureCount}\nDeleted from GoPro: {_deletedCount}\nDelete errors: {_deleteErrorCount}\nOther errors: {_errorCount}{logLine}";
    }

    private void LogSummary()
    {
        Log($"SUMMARY Downloaded={_downloadedCount}, DuplicatesSkipped={_duplicateCount}, Verified={_verifiedCount}, VerificationFailures={_verificationFailureCount}, Deleted={_deletedCount}, DeleteErrors={_deleteErrorCount}, Errors={_errorCount}");
        Log("SESSION END");
    }

    private async Task<List<string>> CrawlCollectFilesAsync(string baseUrl, CancellationToken ct)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        var found = new List<string>();
        queue.Enqueue(baseUrl);

        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            string page = queue.Dequeue();
            if (!visited.Add(page))
                continue;

            Log("CRAWL " + page);
            using var resp = await _http.GetAsync(page, ct);
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync(ct);

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var links = doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>();

            foreach (var node in links)
            {
                string href = node.GetAttributeValue("href", "");
                if (string.IsNullOrWhiteSpace(href) || href is "../" or "./" or "#")
                    continue;

                Uri full;
                try
                {
                    full = new Uri(new Uri(page), href);
                }
                catch
                {
                    continue;
                }

                string fullUrl = full.AbsoluteUri;
                if (!fullUrl.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (fullUrl.EndsWith('/'))
                    queue.Enqueue(fullUrl);
                else
                    found.Add(fullUrl);
            }
        }

        return found;
    }

    private async Task DownloadOneAsync(string fileUrl, string rootOutDir, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        string fileName = Path.GetFileName(Uri.UnescapeDataString(new Uri(fileUrl).LocalPath));
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        DateTimeOffset? modified = resp.Content.Headers.LastModified ?? resp.Headers.Date;
        string outDir = rootOutDir;

        if (chkByDate.Checked && modified.HasValue)
        {
            outDir = Path.Combine(rootOutDir, modified.Value.LocalDateTime.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(outDir);
        }

        string targetPath = Path.Combine(outDir, SanitizeFileName(fileName));
        long? remoteLength = resp.Content.Headers.ContentLength;

        if (File.Exists(targetPath))
        {
            var info = new FileInfo(targetPath);
            if (remoteLength.HasValue && info.Length == remoteLength.Value)
            {
                _duplicateCount++;
                Log($"SKIP  {fileName} (already exists, size matches camera)");

                if (chkVerify.Checked)
                {
                    _verifiedCount++;
                    Log($"VERIFY {fileName} OK (existing local copy, {info.Length:N0} bytes)");
                    if (chkDelete.Checked)
                        await TryDeleteCameraFileAsync(fileUrl, fileName, ct);
                }

                return;
            }

            targetPath = GetUniquePath(targetPath);
        }

        string tempPath = targetPath + ".part";
        try
        {
            await using (Stream source = await resp.Content.ReadAsStreamAsync(ct))
            await using (var destination = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 1024,
                useAsync: true))
            {
                await source.CopyToAsync(destination, 1024 * 1024, ct);
                await destination.FlushAsync(ct);
            }

            File.Move(tempPath, targetPath, overwrite: false);

            if (modified.HasValue)
                File.SetLastWriteTime(targetPath, modified.Value.LocalDateTime);

            _downloadedCount++;
            Log($"SAVE  {Path.GetRelativePath(rootOutDir, targetPath)}");

            if (!chkVerify.Checked)
                return;

            bool verified = VerifyLocalFile(targetPath, remoteLength, out string verificationMessage);
            if (!verified)
            {
                _verificationFailureCount++;
                Log($"VERIFY {fileName} FAILED: {verificationMessage}. Camera copy retained.");
                return;
            }

            _verifiedCount++;
            Log($"VERIFY {fileName} OK: {verificationMessage}");

            if (chkDelete.Checked)
                await TryDeleteCameraFileAsync(fileUrl, fileName, ct);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }

    private static bool VerifyLocalFile(string targetPath, long? remoteLength, out string message)
    {
        if (!File.Exists(targetPath))
        {
            message = "local file is missing";
            return false;
        }

        var info = new FileInfo(targetPath);
        if (!remoteLength.HasValue)
        {
            message = $"camera did not report a file size; local file is {info.Length:N0} bytes";
            return false;
        }

        if (info.Length != remoteLength.Value)
        {
            message = $"local size {info.Length:N0} bytes does not match camera size {remoteLength.Value:N0} bytes";
            return false;
        }

        message = $"{info.Length:N0} bytes match camera";
        return true;
    }

    private async Task TryDeleteCameraFileAsync(string fileUrl, string fileName, CancellationToken ct)
    {
        string? cameraPath = GetCameraMediaPath(fileUrl);
        if (string.IsNullOrWhiteSpace(cameraPath))
        {
            _deleteErrorCount++;
            Log($"DELETE {fileName} FAILED: could not determine camera media path. Camera copy retained.");
            return;
        }

        var sourceUri = new Uri(fileUrl);
        var apiBase = new UriBuilder(sourceUri.Scheme, sourceUri.Host, 8080);
        string encodedCameraPath = string.Join("/", cameraPath.Split('/').Select(Uri.EscapeDataString));
        string deleteUrl =
            $"{apiBase.Uri.GetLeftPart(UriPartial.Authority)}/gopro/media/delete/file" +
            $"?path={encodedCameraPath}";

        try
        {
            Log($"DELETE REQUEST {cameraPath} via {apiBase.Uri.GetLeftPart(UriPartial.Authority)}");
            using var deleteResp = await _http.GetAsync(deleteUrl, ct);
            string responseBody = await deleteResp.Content.ReadAsStringAsync(ct);

            if (!deleteResp.IsSuccessStatusCode)
            {
                _deleteErrorCount++;
                string body = string.IsNullOrWhiteSpace(responseBody) ? "<empty>" : responseBody.Trim();
                Log($"DELETE {fileName} FAILED: camera returned {(int)deleteResp.StatusCode} {deleteResp.ReasonPhrase}. Response: {body}. Camera copy retained.");
                return;
            }

            _deletedCount++;
            Log($"DELETE {fileName} OK ({cameraPath})");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _deleteErrorCount++;
            Log($"DELETE {fileName} FAILED: {ex.Message}. Camera copy retained.");
        }
    }

    private static string? GetCameraMediaPath(string fileUrl)
    {
        string localPath = Uri.UnescapeDataString(new Uri(fileUrl).LocalPath).Replace('\\', '/');
        const string marker = "/DCIM/";
        int index = localPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        string cameraPath = localPath[(index + marker.Length)..].TrimStart('/');
        return string.IsNullOrWhiteSpace(cameraPath) ? null : cameraPath;
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalid, '_');
        return fileName;
    }

    private static string GetUniquePath(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        int counter = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir ?? string.Empty, $"{name} ({counter++}){ext}");
        }
        while (File.Exists(candidate));
        return candidate;
    }

    private void Log(string msg)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Log(msg)));
            return;
        }

        string line = $"{DateTime.Now:HH:mm:ss}  {msg}";
        txtLog.AppendText(line + Environment.NewLine);
        try { _sessionLog?.WriteLine(line); } catch { }
    }

    private void SetStatus(string msg)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatus(msg)));
            return;
        }
        lblStatus.Text = msg;
    }

    private void SetProgress(int value, int max)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetProgress(value, max)));
            return;
        }
        progress.Maximum = Math.Max(1, max);
        progress.Value = Math.Min(value, progress.Maximum);
    }
}
