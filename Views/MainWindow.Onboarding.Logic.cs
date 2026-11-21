using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private void ConfirmarOnboardingTudo(string rawAppDir, string rawDbDir, string rawSvDir, TextBlock lblGeneralError)
        {
            if (lblGeneralError != null)
                lblGeneralError.Visibility = Visibility.Collapsed;

            string appDir = (rawAppDir ?? "").Trim().Trim('"');
            if (string.IsNullOrEmpty(appDir) || !Directory.Exists(appDir))
            {
                if (lblGeneralError != null)
                {
                    lblGeneralError.Text = "A pasta raiz dos ambientes Protheus é obrigatória e deve existir.";
                    lblGeneralError.Visibility = Visibility.Visible;
                }
                return;
            }

            _rootPath = appDir;
            _needsOnboarding = false;
            _isInitialScanCompleted = false;
            try
            {
                File.WriteAllText(_configPath, _rootPath, Encoding.UTF8);
            }
            catch { }

            string dbDir = (rawDbDir ?? "").Trim().Trim('"');
            if (!string.IsNullOrEmpty(dbDir) && Directory.Exists(dbDir))
            {
                _dbAccessConfig.FolderPath = dbDir;
                string candidateIni = Path.Combine(dbDir, "dbaccess.ini");
                if (File.Exists(candidateIni))
                    _dbAccessConfig.IniPath = candidateIni;
                SalvarDbAccessPathAppData(dbDir);
            }

            string svDir = (rawSvDir ?? "").Trim().Trim('"');
            if (!string.IsNullOrEmpty(svDir) && Directory.Exists(svDir))
            {
                _smartViewDetectedFolder = svDir;
                SalvarSmartViewPrefFolder(svDir);
            }

            ShowDashboardView();
            CarregarAmbientesAsync(false);
        }

        private void SelecionarPastaOnboarding(string labelText, TextBox txt)
        {
            using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Selecione o diretório para " + labelText;
                string currentTyped = (txt.Text ?? "").Trim().Trim('"');
                if (!string.IsNullOrEmpty(currentTyped) && Directory.Exists(currentTyped))
                    fbd.SelectedPath = currentTyped;

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txt.Text = fbd.SelectedPath;
                }
            }
        }

        private void ConcluirOnboardingDiretorio(string chosenDir)
        {
            _rootPath = chosenDir;
            _needsOnboarding = false;
            _isInitialScanCompleted = false;

            try
            {
                File.WriteAllText(_configPath, _rootPath, Encoding.UTF8);
            }
            catch { }

            ShowDashboardView();
            CarregarAmbientesAsync(false);
        }
    }
}
