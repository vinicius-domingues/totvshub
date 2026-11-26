using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private void ExecutarRestaurarConfiguracoesPadrao()
        {
            StyledDialog.DialogChoice confirm = StyledDialog.AskWithToggle(
                this,
                "Restaurar Padrão",
                "É isso mesmo que você deseja fazer?",
                "Esta ação irá desocultar todas as bases e redefinir tema, tempos, regras de alerta, links personalizados e preferências para o padrão original.",
                "Sim, tenho certeza",
                false,
                "Confirmar",
                "#DC2626",
                "#EF4444");

            if (confirm != StyledDialog.DialogChoice.Yes)
                return;

            _hiddenDirs.Clear();
            SalvarOcultosAppData();

            _rootPath = @"C:\bases";
            _pollIntervalSeconds = 60;
            _scanBasesMinutes = 60;
            _browserDelaySeconds = 3;
            _ignoreOldFolders = true;
            _showProcessUser = true;
            _showIniComments = false;
            _ignoreSameSubfolderConflicts = true;
            _updateDriveUrl = UiTheme.DefaultUpdateDriveUrl;
            _conflictRules = ObterRegrasPadrao();
            _customLinks.Clear();

            try { if (File.Exists(_configPath)) File.Delete(_configPath); } catch { }
            File.WriteAllText(_timersPrefPath, "60;60;4", Encoding.UTF8);
            File.WriteAllText(_startupPrefPath, "1", Encoding.UTF8);
            AplicarRegistroWindowsStartup(true);
            File.WriteAllText(_themePrefPath, "dark", Encoding.UTF8);
            File.WriteAllText(_ignoreOldPrefPath, "1", Encoding.UTF8);
            File.WriteAllText(_showUserPrefPath, "1", Encoding.UTF8);
            File.WriteAllText(_showCommentsPrefPath, "0", Encoding.UTF8);
            File.WriteAllText(_ignoreSameSubfolderPrefPath, "1", Encoding.UTF8);
            File.WriteAllText(_driveUrlPrefPath, _updateDriveUrl, Encoding.UTF8);
            SalvarRegrasAppData();
            SalvarCustomLinksAppData();

            UiTheme.ApplyTheme(true);
            Background = UiTheme.BgWindow;

            _monitorTimer.Interval = TimeSpan.FromSeconds(_pollIntervalSeconds);
            _basesRefreshTimer.Interval = TimeSpan.FromMinutes(_scanBasesMinutes);

            if (Directory.Exists(@"C:\bases"))
            {
                _needsOnboarding = false;
                ShowSettingsView();
                CarregarAmbientesAsync(false);
            }
            else
            {
                _needsOnboarding = true;
                _isInitialScanCompleted = false;
                ShowOnboardingView();
            }
        }

        private void ExecutarSalvarConfiguracoes(
            string rawPollSec,
            string rawScanMin,
            string rawBrowserSec,
            bool tempDark,
            bool tempAutoStart,
            bool tempIgnoreOld,
            bool tempShowUser,
            bool tempShowComments,
            bool tempIgnoreSameSubfolder,
            string tempRootPath,
            string tempDbAccessPath,
            string tempSmartViewPath,
            List<ConflictRule> tempRules,
            List<CustomLinkButton> tempLinks)
        {
            int newPoll, newScan, newDelay;
            if (!int.TryParse((rawPollSec ?? "").Trim(), out newPoll) || newPoll < 5) newPoll = 60;
            if (!int.TryParse((rawScanMin ?? "").Trim(), out newScan) || newScan < 1) newScan = 60;
            if (!int.TryParse((rawBrowserSec ?? "").Trim(), out newDelay) || newDelay < 0) newDelay = 4;

            bool oldIgnoreChanged = (_ignoreOldFolders != tempIgnoreOld);

            _pollIntervalSeconds = newPoll;
            _scanBasesMinutes = newScan;
            _browserDelaySeconds = newDelay;
            _ignoreOldFolders = tempIgnoreOld;
            _showProcessUser = tempShowUser;
            _showIniComments = tempShowComments;
            _ignoreSameSubfolderConflicts = tempIgnoreSameSubfolder;
            _conflictRules = tempRules;
            _customLinks = tempLinks;

            bool rootChanged = false;
            if (!string.IsNullOrEmpty(tempRootPath) && !string.Equals(tempRootPath, _rootPath, StringComparison.OrdinalIgnoreCase))
            {
                _rootPath = tempRootPath;
                File.WriteAllText(_configPath, _rootPath, Encoding.UTF8);
                rootChanged = true;
            }

            if (!string.IsNullOrEmpty(tempDbAccessPath) && !string.Equals(tempDbAccessPath, _dbAccessConfig.FolderPath, StringComparison.OrdinalIgnoreCase))
            {
                _dbAccessConfig.FolderPath = tempDbAccessPath;
                _dbAccessConfig.IniPath = Path.Combine(tempDbAccessPath, "dbaccess.ini");
                SalvarDbAccessPathAppData(tempDbAccessPath);
            }

            if (!string.IsNullOrEmpty(tempSmartViewPath) && !string.Equals(tempSmartViewPath, _smartViewDetectedFolder, StringComparison.OrdinalIgnoreCase))
            {
                _smartViewDetectedFolder = tempSmartViewPath;
                SalvarSmartViewPrefFolder(tempSmartViewPath);
            }

            File.WriteAllText(_timersPrefPath, string.Format("{0};{1};{2}", _pollIntervalSeconds, _scanBasesMinutes, _browserDelaySeconds), Encoding.UTF8);
            File.WriteAllText(_startupPrefPath, tempAutoStart ? "1" : "0", Encoding.UTF8);
            AplicarRegistroWindowsStartup(tempAutoStart);

            File.WriteAllText(_themePrefPath, tempDark ? "dark" : "light", Encoding.UTF8);
            File.WriteAllText(_ignoreOldPrefPath, _ignoreOldFolders ? "1" : "0", Encoding.UTF8);
            File.WriteAllText(_showUserPrefPath, _showProcessUser ? "1" : "0", Encoding.UTF8);
            File.WriteAllText(_showCommentsPrefPath, _showIniComments ? "1" : "0", Encoding.UTF8);
            File.WriteAllText(_ignoreSameSubfolderPrefPath, _ignoreSameSubfolderConflicts ? "1" : "0", Encoding.UTF8);
            SalvarRegrasAppData();
            SalvarCustomLinksAppData();

            UiTheme.ApplyTheme(tempDark);
            Background = UiTheme.BgWindow;

            _monitorTimer.Interval = TimeSpan.FromSeconds(_pollIntervalSeconds);
            _basesRefreshTimer.Interval = TimeSpan.FromMinutes(_scanBasesMinutes);

            RecalcularAlertas();
            if (_needsOnboarding)
            {
                ShowOnboardingView();
            }
            else
            {
                ShowDashboardView();
                if (oldIgnoreChanged || rootChanged)
                {
                    CarregarAmbientesAsync(true);
                }
            }
        }

        private void SelecionarPastaSetting(string title, TextBox txtPath, Action<string> onPathChanged)
        {
            using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Selecione: " + title;
                if (!string.IsNullOrEmpty(txtPath.Text) && Directory.Exists(txtPath.Text))
                    fbd.SelectedPath = txtPath.Text;

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    txtPath.Text = fbd.SelectedPath;
                    if (onPathChanged != null) onPathChanged(fbd.SelectedPath);
                }
            }
        }

        private void TentarAdicionarCustomLink(TextBox txtNewBtnLabel, TextBox txtNewBtnFormula, List<CustomLinkButton> tempLinks, Action renderUserLinks)
        {
            string lbl = txtNewBtnLabel.Text.Trim();
            string form = txtNewBtnFormula.Text.Trim();
            if (string.IsNullOrEmpty(lbl) || string.IsNullOrEmpty(form))
                return;

            if (tempLinks.Count >= 5)
            {
                StyledDialog.Info(this, "TOTVSHub", "Limite atingido: Você pode cadastrar no máximo 5 botões de acesso.");
                return;
            }

            tempLinks.Add(new CustomLinkButton(lbl, form, false));
            renderUserLinks();
            txtNewBtnLabel.Text = "Digite aqui";
            txtNewBtnFormula.Text = "";
        }

        private void TentarAdicionarRegraConflito(TextBox txtNewSec, TextBox txtNewKey, List<ConflictRule> tempRules, Action renderRulesList)
        {
            string sec = txtNewSec.Text.Trim().Trim('[', ']');
            string key = txtNewKey.Text.Trim();
            if (string.IsNullOrEmpty(sec))
                return;

            if (tempRules.Count >= 10)
            {
                StyledDialog.Info(this, "TOTVSHub", "Limite atingido: Você pode cadastrar no máximo 10 regras/tags de monitoramento.");
                return;
            }

            bool exists = tempRules.Any(r =>
                string.Equals(r.SectionPattern, sec, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.KeyName ?? "", key, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                tempRules.Add(new ConflictRule(sec, key));
                renderRulesList();
            }
            txtNewSec.Text = "";
            txtNewKey.Text = "";
        }
    }
}
