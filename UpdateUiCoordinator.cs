using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace InputStitch
{
    internal sealed class UpdateUiCoordinator
    {
        private readonly Form mainForm;
        private readonly Label statusLabel;
        private readonly Action<string> emergencyStop;
        private readonly Action saveConfig;
        private bool busy;

        internal UpdateUiCoordinator(Form main, Label status, Action<string> stopAction, Action saveAction)
        {
            if (main == null) throw new ArgumentNullException("main");
            if (stopAction == null) throw new ArgumentNullException("stopAction");
            if (saveAction == null) throw new ArgumentNullException("saveAction");
            mainForm = main;
            statusLabel = status;
            emergencyStop = stopAction;
            saveConfig = saveAction;
        }

        internal bool Busy { get { return busy; } }

        internal async void CheckForUpdatesAsync(IWin32Window owner, bool automatic)
        {
            if (!ReleaseInfo.AutomaticChecksAllowed)
            {
                if (!automatic)
                {
                    string message = Localizer.IsEnglish
                        ? "Beta updates are downloaded manually. Open the official GitHub Releases page? Back up your configuration before testing; stable releases are not replaced."
                        : "测试版通过手动下载更新。是否打开官方 GitHub Releases 页面？测试前请备份配置；正式版不会被替换。";
                    if (MessageBox.Show(owner, message, AppInfo.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                    {
                        try { Process.Start(new ProcessStartInfo(ReleaseInfo.ReleasesUrl) { UseShellExecute = true }); }
                        catch (Exception ex) { MessageBox.Show(owner, ex.Message, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                    }
                }
                return;
            }
            if (busy) return;
            busy = true;
            string previousStatus = statusLabel == null ? "" : statusLabel.Text;
            string updateStage = "check";
            try
            {
                if (statusLabel != null) statusLabel.Text = Localizer.T("正在检查更新…");
                UpdateCheckResult update = await UpdateManager.CheckAsync();
                if (!update.IsAvailable)
                {
                    if (!automatic)
                        LocalizedMessageBox.Show(owner, Localizer.T("已是最新版本。"), AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string prompt = Localizer.T("发现可用更新。是否下载并安装？") +
                    "\r\n\r\n" + (Localizer.IsEnglish ? "Architecture: " : "架构：") + update.Asset.Architecture +
                    "\r\n" + (Localizer.IsEnglish ? "Release: " : "发布版本：") + update.Manifest.Version;
                DialogResult choice = LocalizedMessageBox.Show(owner, prompt, Localizer.T("发现可用更新"),
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (choice != DialogResult.OK) return;

                updateStage = "download";
                if (statusLabel != null) statusLabel.Text = Localizer.T("正在下载并验证更新…");
                string downloaded = await UpdateManager.DownloadAsync(update);
                updateStage = "install";
                emergencyStop("software-update");
                saveConfig();
                UpdateManager.BeginInstall(downloaded, update.Asset.Sha256);
                Form ownerForm = owner as Form;
                if (ownerForm != null && ownerForm != mainForm && !ownerForm.IsDisposed) ownerForm.Close();
                mainForm.Close();
            }
            catch (Exception ex)
            {
                AppLog.Write("Update stage failed: " + updateStage, ex);
                if (!automatic || updateStage != "check")
                {
                    string heading = updateStage == "download" ? Localizer.T("下载更新失败。") :
                        updateStage == "install" ? Localizer.T("准备安装更新失败。") : Localizer.T("检查更新失败。");
                    LocalizedMessageBox.Show(owner, heading + "\r\n\r\n" + ex.Message,
                        AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                busy = false;
                if (statusLabel != null && !statusLabel.IsDisposed && !mainForm.IsDisposed && !mainForm.Disposing)
                    statusLabel.Text = previousStatus;
            }
        }
    }
}
