using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private string ResolverFormulaLink(AmbienteInfo amb, string formula)
                {
                    if (amb == null || string.IsNullOrWhiteSpace(formula))
                        return null;
        
                    bool missingKey = false;
                    string result = Regex.Replace(formula.Trim(), @"(\{([^}]+)\}|<([^>]+)>)", delegate(Match m)
                    {
                        string inner = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
                        inner = (inner ?? "").Trim();
                        if (string.IsNullOrEmpty(inner))
                        {
                            missingKey = true;
                            return "";
                        }
        
                        string secName = null;
                        string keyName = null;
        
                        char[] seps = new char[] { ':', '>', '/', '.' };
                        int sepIdx = inner.IndexOfAny(seps);
                        if (sepIdx > 0)
                        {
                            secName = inner.Substring(0, sepIdx).Trim().Trim('[', ']');
                            keyName = inner.Substring(sepIdx + 1).Trim();
                        }
                        else
                        {
                            keyName = inner.Trim();
                        }
        
                        if (string.Equals(secName, "ambiente", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(secName, "<ambiente>", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(secName, "environment", StringComparison.OrdinalIgnoreCase))
                        {
                            secName = amb.NomeIni;
                        }
        
                        string val = null;
                        if (!string.IsNullOrEmpty(secName) && !string.IsNullOrEmpty(keyName))
                        {
                            Dictionary<string, string> secDict;
                            if (amb.IniSectionsMap.TryGetValue(secName, out secDict))
                            {
                                secDict.TryGetValue(keyName, out val);
                            }
                            if (string.IsNullOrWhiteSpace(val) && string.Equals(keyName, "Port", StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.Equals(secName, "HTTPREST", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(secName, "REST", StringComparison.OrdinalIgnoreCase))
                                {
                                    val = amb.RestPort;
                                }
                                else if (string.Equals(secName, "WEBSERVICES", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(secName, "WS", StringComparison.OrdinalIgnoreCase))
                                {
                                    val = amb.WebServicesPort;
                                }
                                else if (string.Equals(secName, "WEBAPP", StringComparison.OrdinalIgnoreCase))
                                {
                                    val = amb.WebAppPort;
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(keyName))
                        {
                            if (string.Equals(keyName, "WebAppPort", StringComparison.OrdinalIgnoreCase))
                                val = amb.WebAppPort;
                            else if (string.Equals(keyName, "RestPort", StringComparison.OrdinalIgnoreCase))
                                val = amb.RestPort;
                            else if (string.Equals(keyName, "WebServicesPort", StringComparison.OrdinalIgnoreCase))
                                val = amb.WebServicesPort;
                            else if (string.Equals(keyName, "TcpPort", StringComparison.OrdinalIgnoreCase))
                                val = amb.TcpPort;
                            else if (string.Equals(keyName, "SpecialKey", StringComparison.OrdinalIgnoreCase))
                                val = amb.SpecialKey;
                            else if (string.Equals(keyName, "DBAlias", StringComparison.OrdinalIgnoreCase))
                                val = amb.DBAlias;
                            else if (string.Equals(keyName, "DBPort", StringComparison.OrdinalIgnoreCase))
                                val = amb.DBPort;
                            else if (string.Equals(keyName, "ambiente", StringComparison.OrdinalIgnoreCase))
                                val = amb.NomeIni;
                            else
                                val = ObterValorChaveIni(amb, amb.NomeIni, keyName);
                        }
        
                        if (string.IsNullOrWhiteSpace(val))
                        {
                            missingKey = true;
                            return "";
                        }
                        return val.Trim();
                    });
        
                    if (missingKey || string.IsNullOrWhiteSpace(result))
                        return null;
        
                    if (!result.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !result.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        result = "http://" + result;
                    }
                    return result;
                }

        private void AbrirUrlNoNavegador(string url, int delaySeconds)
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        if (delaySeconds > 0)
                            Thread.Sleep(delaySeconds * 1000);
                        try
                        {
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = url;
                            psi.UseShellExecute = true;
                            Process.Start(psi);
                        }
                        catch { }
                    });
                }

        private void AbrirNavegador(AmbienteInfo amb)
                {
                    if (string.IsNullOrEmpty(amb.WebAppPort))
                    {
                        StyledDialog.Info(this, "Porta WebApp Ausente", "Este ambiente não possui porta configurada na seção [WEBAPP].", amb.IniPath);
                        return;
                    }
        
                    string url = string.Format("http://localhost:{0}", amb.WebAppPort);
                    if (amb.State != EnvRunState.Active)
                    {
                        int delay = _browserDelaySeconds;
                        IniciarAmbienteAsync(amb, delegate { AbrirUrlNoNavegador(url, delay); });
                    }
                    else
                    {
                        AbrirUrlNoNavegador(url, 0);
                    }
                }

        private void AbrirRestNavegador(AmbienteInfo amb)
                {
                    if (string.IsNullOrEmpty(amb.RestPort))
                    {
                        StyledDialog.Info(this, "Porta REST Ausente", "Este ambiente não possui porta configurada na seção [HTTPREST].", amb.IniPath);
                        return;
                    }
        
                    string url = string.Format("http://localhost:{0}/rest", amb.RestPort);
                    if (amb.State != EnvRunState.Active)
                    {
                        int delay = _browserDelaySeconds;
                        IniciarAmbienteAsync(amb, delegate { AbrirUrlNoNavegador(url, delay); });
                    }
                    else
                    {
                        AbrirUrlNoNavegador(url, 0);
                    }
                }

        private void AbrirWebServicesNavegador(AmbienteInfo amb)
                {
                    if (string.IsNullOrEmpty(amb.WebServicesPort))
                    {
                        StyledDialog.Info(this, "Porta WebServices Ausente", "Este ambiente não possui porta configurada para WebServices.", amb.IniPath);
                        return;
                    }
        
                    string url = string.Format("http://localhost:{0}/ws", amb.WebServicesPort);
                    if (amb.State != EnvRunState.Active)
                    {
                        int delay = _browserDelaySeconds;
                        IniciarAmbienteAsync(amb, delegate { AbrirUrlNoNavegador(url, delay); });
                    }
                    else
                    {
                        AbrirUrlNoNavegador(url, 0);
                    }
                }

        private void AbrirLinkCustomizado(AmbienteInfo amb, CustomLinkButton linkDef)
                {
                    string url = ResolverFormulaLink(amb, linkDef.Formula);
                    if (string.IsNullOrEmpty(url))
                    {
                        StyledDialog.Info(
                            this,
                            "Chave Não Encontrada",
                            string.Format("Não foi possível montar o link \"{0}\" para o ambiente {1}.", linkDef.Label, amb.NomeIni),
                            string.Format("Verifique se as seções/chaves da fórmula ({0}) existem no appserver.ini.", linkDef.Formula));
                        return;
                    }
        
                    if (amb.State != EnvRunState.Active)
                    {
                        int delay = _browserDelaySeconds;
                        IniciarAmbienteAsync(amb, delegate { AbrirUrlNoNavegador(url, delay); });
                    }
                    else
                    {
                        AbrirUrlNoNavegador(url, 0);
                    }
                }

        private void IniciarPorLinhaDeComandoOuServico(AmbienteInfo amb)
                {
                    bool startedService = false;
                    if (amb.IsServiceRegistered && !string.IsNullOrEmpty(amb.ServiceName))
                    {
                        try
                        {
                            using (ServiceController sc = new ServiceController(amb.ServiceName))
                            {
                                if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.Paused)
                                {
                                    sc.Start();
                                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                                    startedService = true;
                                }
                                else if (sc.Status == ServiceControllerStatus.Running)
                                {
                                    startedService = true;
                                }
                            }
                        }
                        catch { }
                    }
        
                    
                    string shortcutPath = null;
                    if (!startedService && Directory.Exists(amb.AppDir))
                    {
                        try
                        {
                            string[] lnkFiles = Directory.GetFiles(amb.AppDir, "*.lnk", SearchOption.TopDirectoryOnly);
                            shortcutPath = lnkFiles.FirstOrDefault(f => Path.GetFileName(f).IndexOf("appserver", StringComparison.OrdinalIgnoreCase) >= 0)
                                        ?? lnkFiles.FirstOrDefault();
                        }
                        catch { }
                    }
        
                    if (!startedService && !string.IsNullOrEmpty(shortcutPath) && File.Exists(shortcutPath))
                    {
                        try
                        {
                            ProcessStartInfo psiLnk = new ProcessStartInfo();
                            psiLnk.FileName = shortcutPath;
                            psiLnk.WorkingDirectory = amb.AppDir;
                            psiLnk.UseShellExecute = true;
                            Process.Start(psiLnk);
                            return;
                        }
                        catch { }
                    }
        
                    
                    if (!startedService && File.Exists(amb.ExePath))
                    {
                        try
                        {
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = amb.ExePath;
                            psi.Arguments = "-console";
                            psi.WorkingDirectory = amb.AppDir;
                            psi.UseShellExecute = true;
                            psi.Verb = "runas";
                            Process.Start(psi);
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            
                            try
                            {
                                ProcessStartInfo fallback = new ProcessStartInfo();
                                fallback.FileName = amb.ExePath;
                                fallback.Arguments = "-console";
                                fallback.WorkingDirectory = amb.AppDir;
                                fallback.UseShellExecute = true;
                                Process.Start(fallback);
                            }
                            catch { }
                        }
                        catch { }
                    }
                }

        private void PararPorPidOuServico(AmbienteInfo amb)
                {
                    if (amb.RunningAsService && !string.IsNullOrEmpty(amb.ServiceName))
                    {
                        try
                        {
                            using (ServiceController sc = new ServiceController(amb.ServiceName))
                            {
                                if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                                {
                                    sc.Stop();
                                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
                                }
                            }
                        }
                        catch { }
                    }
        
                    if (amb.Pid > 0)
                    {
                        try
                        {
                            using (Process p = Process.GetProcessById(amb.Pid))
                            {
                                if (!p.HasExited)
                                {
                                    p.Kill();
                                    p.WaitForExit(4000);
                                }
                            }
                        }
                        catch { }
                    }
                }

        private void IniciarAmbienteAsync(AmbienteInfo amb, Action onStartedCallback)
                {
                    if (amb.State == EnvRunState.Initializing || amb.State == EnvRunState.Stopping)
                        return;
        
                    if (amb.State == EnvRunState.Active)
                    {
                        if (onStartedCallback != null)
                            onStartedCallback();
                        return;
                    }
        
                    amb.State = EnvRunState.Initializing;
                    AtualizarResumoEVisualCards();
                    bool fetchUser = _showProcessUser;
        
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        try
                        {
                            IniciarPorLinhaDeComandoOuServico(amb);
                            Thread.Sleep(1200);
                            SincronizarEstadoProcessosEServicos(_ambientes.ToList(), fetchUser);
                        }
                        finally
                        {
                            Dispatcher.BeginInvoke(new Action(delegate
                            {
                                if (amb.State == EnvRunState.Initializing)
                                    amb.State = EnvRunState.Stopped;
                                SincronizarEstadoProcessosEServicos(new List<AmbienteInfo> { amb }, fetchUser);
                                AtualizarResumoEVisualCards();
                                if (amb.State == EnvRunState.Active && onStartedCallback != null)
                                    onStartedCallback();
                            }));
                        }
                    });
                }

        private void EncerrarAmbienteAsync(AmbienteInfo amb)
                {
                    if (amb.State == EnvRunState.Stopped || amb.State == EnvRunState.Stopping)
                        return;
        
                    amb.State = EnvRunState.Stopping;
                    AtualizarResumoEVisualCards();
                    bool fetchUser = _showProcessUser;
        
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        try
                        {
                            PararPorPidOuServico(amb);
                            Thread.Sleep(900);
                        }
                        finally
                        {
                            Dispatcher.BeginInvoke(new Action(delegate
                            {
                                amb.State = EnvRunState.Stopped;
                                amb.Pid = 0;
                                amb.ProcessUser = null;
                                SincronizarEstadoProcessosEServicos(new List<AmbienteInfo> { amb }, fetchUser);
                                AtualizarResumoEVisualCards();
                            }));
                        }
                    });
                }

        private void ReiniciarAmbienteAsync(AmbienteInfo amb)
                {
                    if (amb.State == EnvRunState.Initializing || amb.State == EnvRunState.Stopping)
                        return;
        
                    amb.State = EnvRunState.Initializing;
                    AtualizarResumoEVisualCards();
                    bool fetchUser = _showProcessUser;
        
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        try
                        {
                            PararPorPidOuServico(amb);
                            Thread.Sleep(1000);
                            IniciarPorLinhaDeComandoOuServico(amb);
                            Thread.Sleep(1200);
                        }
                        finally
                        {
                            Dispatcher.BeginInvoke(new Action(delegate
                            {
                                amb.State = EnvRunState.Stopped;
                                SincronizarEstadoProcessosEServicos(new List<AmbienteInfo> { amb }, fetchUser);
                                AtualizarResumoEVisualCards();
                            }));
                        }
                    });
                }

        private readonly string _d1StateFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TOTVSHub",
                    "d1_state.txt");

        private HashSet<string> _d1ActiveEnvs;

        private HashSet<string> ObterD1Ativos()
                {
                    if (_d1ActiveEnvs != null)
                        return _d1ActiveEnvs;
        
                    _d1ActiveEnvs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        if (File.Exists(_d1StateFilePath))
                        {
                            string[] lines = File.ReadAllLines(_d1StateFilePath, Encoding.UTF8);
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string tr = (lines[i] ?? "").Trim();
                                if (!string.IsNullOrEmpty(tr))
                                    _d1ActiveEnvs.Add(tr);
                            }
                        }
                    }
                    catch { }
                    return _d1ActiveEnvs;
                }

        private void SalvarD1Ativos()
                {
                    try
                    {
                        string dir = Path.GetDirectoryName(_d1StateFilePath);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        File.WriteAllLines(_d1StateFilePath, ObterD1Ativos().ToArray(), Encoding.UTF8);
                    }
                    catch { }
                }

        private string ObterChaveIniQualquerSecao(AmbienteInfo amb, string chaveProcurada)
                {
                    if (amb == null || amb.IniSectionsMap == null) return null;
        
                    Dictionary<string, string> secDict;
                    if (!string.IsNullOrEmpty(amb.NomeIni) && amb.IniSectionsMap.TryGetValue(amb.NomeIni, out secDict))
                    {
                        string val;
                        if (secDict.TryGetValue(chaveProcurada, out val) && !string.IsNullOrWhiteSpace(val))
                            return val.Trim();
                    }
        
                    if (amb.IniSectionsMap.TryGetValue("ENVIRONMENT", out secDict))
                    {
                        string val;
                        if (secDict.TryGetValue(chaveProcurada, out val) && !string.IsNullOrWhiteSpace(val))
                            return val.Trim();
                    }
        
                    if (amb.IniSectionsMap.TryGetValue("GENERAL", out secDict))
                    {
                        string val;
                        if (secDict.TryGetValue(chaveProcurada, out val) && !string.IsNullOrWhiteSpace(val))
                            return val.Trim();
                    }
        
                    foreach (KeyValuePair<string, Dictionary<string, string>> pair in amb.IniSectionsMap)
                    {
                        string val;
                        if (pair.Value.TryGetValue(chaveProcurada, out val) && !string.IsNullOrWhiteSpace(val))
                            return val.Trim();
                    }
        
                    return null;
                }

        private string LocalizarPastaApo(AmbienteInfo amb)
                {
                    if (amb == null) return null;
        
                    
                    string sourcePath = ObterChaveIniQualquerSecao(amb, "SOURCEPATH");
                    if (!string.IsNullOrWhiteSpace(sourcePath))
                    {
                        string spClean = sourcePath.Trim().Trim('"').Trim();
                        if (Directory.Exists(spClean))
                            return NormalizePath(spClean);
        
                        if (!string.IsNullOrEmpty(amb.AppDir))
                        {
                            string combined = Path.Combine(amb.AppDir, spClean.TrimStart('\\', '/'));
                            if (Directory.Exists(combined))
                                return NormalizePath(combined);
        
                            try
                            {
                                string rootDrive = Path.GetPathRoot(amb.AppDir);
                                if (!string.IsNullOrEmpty(rootDrive))
                                {
                                    string combinedDrive = Path.Combine(rootDrive, spClean.TrimStart('\\', '/'));
                                    if (Directory.Exists(combinedDrive))
                                        return NormalizePath(combinedDrive);
                                }
                            }
                            catch { }
                        }
        
                        string rootPathVal = ObterChaveIniQualquerSecao(amb, "ROOTPATH");
                        if (!string.IsNullOrWhiteSpace(rootPathVal))
                        {
                            string rpClean = rootPathVal.Trim().Trim('"').Trim();
                            string combinedRp = Path.Combine(rpClean, spClean.TrimStart('\\', '/'));
                            if (Directory.Exists(combinedRp))
                                return NormalizePath(combinedRp);
        
                            string parentRp = Path.GetDirectoryName(rpClean);
                            if (!string.IsNullOrEmpty(parentRp))
                            {
                                string combinedParentRp = Path.Combine(parentRp, spClean.TrimStart('\\', '/'));
                                if (Directory.Exists(combinedParentRp))
                                    return NormalizePath(combinedParentRp);
                            }
                        }
        
                        if (!string.IsNullOrEmpty(amb.PastaBase))
                        {
                            string combinedPb = Path.Combine(amb.PastaBase, spClean.TrimStart('\\', '/'));
                            if (Directory.Exists(combinedPb))
                                return NormalizePath(combinedPb);
                        }
                    }
        
                    
                    string rpoCustomVal = ObterChaveIniQualquerSecao(amb, "RPOCUSTOM");
                    if (!string.IsNullOrWhiteSpace(rpoCustomVal))
                    {
                        string rcClean = rpoCustomVal.Trim().Trim('"').Trim();
                        string dirRc = Path.GetDirectoryName(rcClean);
                        if (!string.IsNullOrEmpty(dirRc) && Directory.Exists(dirRc))
                            return NormalizePath(dirRc);
                    }
        
                    
                    if (!string.IsNullOrEmpty(amb.AppDir))
                    {
                        string directApo = Path.Combine(amb.AppDir, "apo");
                        if (Directory.Exists(directApo))
                            return NormalizePath(directApo);
        
                        DirectoryInfo cur = new DirectoryInfo(amb.AppDir);
                        for (int i = 0; i < 4 && cur != null; i++)
                        {
                            string candApo = Path.Combine(cur.FullName, "apo");
                            if (Directory.Exists(candApo))
                                return NormalizePath(candApo);
        
                            string candProtheusApo = Path.Combine(cur.FullName, "protheus", "apo");
                            if (Directory.Exists(candProtheusApo))
                                return NormalizePath(candProtheusApo);
        
                            string candDataApo = Path.Combine(cur.FullName, "protheus_data", "apo");
                            if (Directory.Exists(candDataApo))
                                return NormalizePath(candDataApo);
        
                            cur = cur.Parent;
                        }
                    }
        
                    
                    if (!string.IsNullOrEmpty(amb.PastaBase))
                    {
                        string pbApo = Path.Combine(amb.PastaBase, "apo");
                        if (Directory.Exists(pbApo))
                            return NormalizePath(pbApo);
        
                        string pbProtheusApo = Path.Combine(amb.PastaBase, "protheus", "apo");
                        if (Directory.Exists(pbProtheusApo))
                            return NormalizePath(pbProtheusApo);
                    }
        
                    
                    if (!string.IsNullOrEmpty(amb.AppDir) && Directory.Exists(amb.AppDir))
                    {
                        try
                        {
                            if (Directory.GetFiles(amb.AppDir, "*.rpo").Length > 0)
                                return NormalizePath(amb.AppDir);
                        }
                        catch { }
                    }
        
                    return null;
                }

        private string LocalizarRpoBaseAtual(string apoFolder, string preferredName)
                {
                    if (string.IsNullOrEmpty(apoFolder) || !Directory.Exists(apoFolder))
                        return null;
        
                    if (!string.IsNullOrEmpty(preferredName))
                    {
                        string direct = Path.Combine(apoFolder, preferredName);
                        if (File.Exists(direct))
                            return direct;
                    }
        
                    string[] files = Directory.GetFiles(apoFolder, "*.rpo", SearchOption.TopDirectoryOnly);
                    if (files.Length == 0) return null;
        
                    for (int i = 0; i < files.Length; i++)
                    {
                        string name = Path.GetFileName(files[i]);
                        if (!name.StartsWith("custom", StringComparison.OrdinalIgnoreCase) &&
                            !name.StartsWith("mycustom", StringComparison.OrdinalIgnoreCase) &&
                            !name.StartsWith("tlpp", StringComparison.OrdinalIgnoreCase) &&
                            !name.StartsWith("appserver", StringComparison.OrdinalIgnoreCase))
                        {
                            return files[i];
                        }
                    }
        
                    return files[0];
                }

        private string SanitizePathName(string name)
                {
                    if (string.IsNullOrEmpty(name)) return "ambiente";
                    char[] invalids = Path.GetInvalidFileNameChars();
                    StringBuilder sb = new StringBuilder();
                    foreach (char c in name)
                    {
                        if (Array.IndexOf(invalids, c) < 0 && c != ' ' && c != '.' && c != ':')
                            sb.Append(c);
                        else
                            sb.Append('_');
                    }
                    return sb.ToString().Trim('_');
                }

        private string ObterPastaD1AppData(AmbienteInfo amb)
                {
                    string hash = Math.Abs((amb.AppDir ?? "").ToLowerInvariant().GetHashCode()).ToString("X8");
                    string safeName = SanitizePathName(amb.NomeIni) + "_" + hash;
                    string path = Path.Combine(_appDataDir, "d1", safeName);
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    return path;
                }

        private string ObterPastaNormalAppData(AmbienteInfo amb)
                {
                    string hash = Math.Abs((amb.AppDir ?? "").ToLowerInvariant().GetHashCode()).ToString("X8");
                    string safeName = SanitizePathName(amb.NomeIni) + "_" + hash;
                    string path = Path.Combine(_appDataDir, "normal", safeName);
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    return path;
                }

        private string DetectarVersaoDiagnosticsJson(AmbienteInfo amb)
                {
                    if (amb == null || string.IsNullOrEmpty(amb.AppDir)) return null;
        
                    string[] candidatePaths = new string[]
                    {
                        Path.Combine(amb.AppDir, "tec", "appserver", "diagnostics.json"),
                        Path.Combine(amb.AppDir, "diagnostics.json"),
                        !string.IsNullOrEmpty(amb.ExePath) ? Path.Combine(Path.GetDirectoryName(amb.ExePath), "tec", "appserver", "diagnostics.json") : null,
                        !string.IsNullOrEmpty(amb.ExePath) ? Path.Combine(Path.GetDirectoryName(amb.ExePath), "diagnostics.json") : null,
                        Directory.GetParent(amb.AppDir) != null ? Path.Combine(Directory.GetParent(amb.AppDir).FullName, "tec", "appserver", "diagnostics.json") : null
                    };
        
                    foreach (string path in candidatePaths)
                    {
                        if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                        try
                        {
                            string json = File.ReadAllText(path);
                            var match = System.Text.RegularExpressions.Regex.Match(json, @"""app_environment""\s*:\s*""([^""]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                string val = match.Groups[1].Value.Replace("_", ".");
                                if (val.Contains("2310")) return "12.1.2310";
                                if (val.Contains("2410")) return "12.1.2410";
                                if (val.Contains("2510")) return "12.1.2510";
                                if (val.Contains("2610")) return "12.1.2610";
                                return val;
                            }
                        }
                        catch { }
                    }
                    return null;
                }

        private void AlternarEstadoD1(AmbienteInfo amb, Button btnD1)
                {
                    try
                    {
                        string apoFolder = LocalizarPastaApo(amb);
                        if (string.IsNullOrEmpty(apoFolder) || !Directory.Exists(apoFolder))
                        {
                            MessageBox.Show(this, "Não foi possível localizar o diretório de repositórios (/apo) deste ambiente.", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
        
                        string normalFolder = ObterPastaNormalAppData(amb);
                        string d1Folder = ObterPastaD1AppData(amb);
        
                        
                        string legacyD1 = Path.Combine(apoFolder, "totvshub_d1");
                        if (Directory.Exists(legacyD1))
                        {
                            try
                            {
                                foreach (string f in Directory.GetFiles(legacyD1))
                                {
                                    string dest = Path.Combine(d1Folder, Path.GetFileName(f));
                                    if (!File.Exists(dest)) File.Move(f, dest);
                                }
                                Directory.Delete(legacyD1, true);
                            }
                            catch { }
                        }
        
                        string legacyNormal = Path.Combine(apoFolder, "totvshub_normal");
                        if (Directory.Exists(legacyNormal))
                        {
                            try
                            {
                                foreach (string f in Directory.GetFiles(legacyNormal))
                                {
                                    string dest = Path.Combine(normalFolder, Path.GetFileName(f));
                                    if (!File.Exists(dest)) File.Move(f, dest);
                                }
                                Directory.Delete(legacyNormal, true);
                            }
                            catch { }
                        }
        
                        bool isCurrentlyActive = ObterD1Ativos().Contains(amb.AppDir);
        
                        if (isCurrentlyActive)
                        {
                            
                            string[] normalFiles = Directory.GetFiles(normalFolder, "*.rpo");
                            if (normalFiles.Length == 0)
                            {
                                MessageBox.Show(this, "Não há arquivo de RPO padrão de backup no armazenamento do TOTVSHub para restaurar.", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
        
                            string normalRpoSource = normalFiles[0];
                            string normalFileName = Path.GetFileName(normalRpoSource);
                            string targetInApo = Path.Combine(apoFolder, normalFileName);
        
                            
                            if (File.Exists(targetInApo))
                            {
                                try { File.Delete(targetInApo); } catch { }
                            }
        
                            
                            File.Copy(normalRpoSource, targetInApo, true);
        
                            ObterD1Ativos().Remove(amb.AppDir);
                            SalvarD1Ativos();
        
                            UiTheme.ApplyRpoButtonStyle(btnD1, false);
                            btnD1.ToolTip = "D-1 Inativo (Clique para ativar D-1)";
                        }
                        else
                        {
                            
                            string[] d1Files = Directory.GetFiles(d1Folder, "*.rpo");
        
                            if (d1Files.Length == 0)
                            {
                                var res = MessageBox.Show(this, "O repositório D-1 deste ambiente ainda não foi baixado na pasta AppData.\nDeseja iniciar o download agora?", "TOTVSHub - Repositório D-1", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                if (res == MessageBoxResult.Yes)
                                {
                                    AbrirModalDownloadD1(amb);
                                }
                                return;
                            }
        
                            string d1SourcePath = d1Files[0];
                            string d1FileName = Path.GetFileName(d1SourcePath);
        
                            string currentRpoInApo = LocalizarRpoBaseAtual(apoFolder, d1FileName);
                            if (!string.IsNullOrEmpty(currentRpoInApo))
                            {
                                string normalTargetPath = Path.Combine(normalFolder, Path.GetFileName(currentRpoInApo));
                                if (!File.Exists(normalTargetPath))
                                    File.Copy(currentRpoInApo, normalTargetPath, true);
                            }
        
                            string activeTargetPath = Path.Combine(apoFolder, d1FileName);
                            File.Copy(d1SourcePath, activeTargetPath, true);
        
                            ObterD1Ativos().Add(amb.AppDir);
                            SalvarD1Ativos();
        
                            UiTheme.ApplyRpoButtonStyle(btnD1, true);
                            btnD1.ToolTip = "D-1 Ativo (Clique para desativar e restaurar RPO padrão)";
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Erro ao alterar RPO D-1: " + ex.Message + "\n\nSe o ambiente TOTVS estiver em execução, encerre-o antes de alterar os arquivos RPO.", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

        private bool VerificarRpoCustomAtivo(AmbienteInfo amb)
                {
                    try
                    {
                        string apoFolder = LocalizarPastaApo(amb);
                        if (string.IsNullOrEmpty(apoFolder) || !Directory.Exists(apoFolder))
                        {
                            if (!string.IsNullOrEmpty(amb.AppDir) && Directory.Exists(amb.AppDir))
                                apoFolder = amb.AppDir;
                            else
                                return false;
                        }
        
                        if (!string.IsNullOrEmpty(amb.AppDir) && Directory.Exists(amb.AppDir))
                        {
                            string customInApo = Path.Combine(apoFolder, "custom.rpo");
                            string customFolderInApo = Path.Combine(apoFolder, "totvshub_customrpo");
                            string customInApp = Path.Combine(amb.AppDir, "custom.rpo");
                            string customFolderInApp = Path.Combine(amb.AppDir, "totvshub_customrpo");
        
                            if (!File.Exists(customInApo) && !Directory.Exists(customFolderInApo) && (File.Exists(customInApp) || Directory.Exists(customFolderInApp)))
                            {
                                apoFolder = amb.AppDir;
                            }
                        }
        
                        string rpoCustomVal = ObterChaveIniQualquerSecao(amb, "RPOCUSTOM");
                        if (!string.IsNullOrWhiteSpace(rpoCustomVal))
                        {
                            string fn = Path.GetFileName(rpoCustomVal.Trim().Trim('"'));
                            if (!string.IsNullOrEmpty(fn) && File.Exists(Path.Combine(apoFolder, fn)))
                                return true;
                        }
        
                        if (File.Exists(Path.Combine(apoFolder, "custom.rpo")))
                            return true;
                    }
                    catch { }
                    return false;
                }

        private string ObterTamanhoCustomRpoFormatado(AmbienteInfo amb)
                {
                    try
                    {
                        string apoFolder = LocalizarPastaApo(amb);
                        if (string.IsNullOrEmpty(apoFolder) || !Directory.Exists(apoFolder))
                        {
                            if (!string.IsNullOrEmpty(amb.AppDir) && Directory.Exists(amb.AppDir))
                                apoFolder = amb.AppDir;
                            else
                                return "-";
                        }
        
                        string customFolder = Path.Combine(apoFolder, "totvshub_customrpo");
        
                        List<string> candidatePaths = new List<string>();
        
                        string rpoCustomVal = ObterChaveIniQualquerSecao(amb, "RPOCUSTOM");
                        if (!string.IsNullOrWhiteSpace(rpoCustomVal))
                        {
                            string fn = Path.GetFileName(rpoCustomVal.Trim().Trim('"'));
                            if (!string.IsNullOrEmpty(fn))
                            {
                                candidatePaths.Add(Path.Combine(apoFolder, fn));
                                candidatePaths.Add(Path.Combine(customFolder, fn));
                            }
                        }
        
                        candidatePaths.Add(Path.Combine(apoFolder, "custom.rpo"));
                        candidatePaths.Add(Path.Combine(customFolder, "custom.rpo"));
        
                        if (Directory.Exists(customFolder))
                        {
                            string[] inCustom = Directory.GetFiles(customFolder, "*.rpo");
                            for (int i = 0; i < inCustom.Length; i++)
                                candidatePaths.Add(inCustom[i]);
                        }
        
                        for (int i = 0; i < candidatePaths.Count; i++)
                        {
                            string cp = candidatePaths[i];
                            if (!string.IsNullOrEmpty(cp) && File.Exists(cp))
                            {
                                FileInfo fi = new FileInfo(cp);
                                long kb = fi.Length == 0 ? 0 : (fi.Length + 1023) / 1024;
                                return string.Format("{0:N0} KB", kb);
                            }
                        }
                    }
                    catch { }
                    return "-";
                }

        private void AlternarEstadoRpoCustom(AmbienteInfo amb, Button btnCustom, TextBlock lblSize)
                {
                    try
                    {
                        string apoFolder = LocalizarPastaApo(amb);
                        if (string.IsNullOrEmpty(apoFolder) || !Directory.Exists(apoFolder))
                        {
                            if (!string.IsNullOrEmpty(amb.AppDir) && Directory.Exists(amb.AppDir))
                                apoFolder = amb.AppDir;
                            else
                            {
                                MessageBox.Show(this, "Não foi possível localizar o diretório de repositórios (/apo) deste ambiente.", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
        
                        if (!string.IsNullOrEmpty(amb.AppDir) && Directory.Exists(amb.AppDir))
                        {
                            string customInApo = Path.Combine(apoFolder, "custom.rpo");
                            string customFolderInApo = Path.Combine(apoFolder, "totvshub_customrpo");
                            string customInApp = Path.Combine(amb.AppDir, "custom.rpo");
                            string customFolderInApp = Path.Combine(amb.AppDir, "totvshub_customrpo");
        
                            if (!File.Exists(customInApo) && !Directory.Exists(customFolderInApo) && (File.Exists(customInApp) || Directory.Exists(customFolderInApp)))
                            {
                                apoFolder = amb.AppDir;
                            }
                        }
        
                        string customFolder = Path.Combine(apoFolder, "totvshub_customrpo");
        
                        string customFileName = "custom.rpo";
                        string rpoCustomVal = ObterChaveIniQualquerSecao(amb, "RPOCUSTOM");
                        if (!string.IsNullOrWhiteSpace(rpoCustomVal))
                        {
                            string fn = Path.GetFileName(rpoCustomVal.Trim().Trim('"'));
                            if (!string.IsNullOrEmpty(fn))
                                customFileName = fn;
                        }
        
                        if (Directory.Exists(customFolder))
                        {
                            string[] inCustom = Directory.GetFiles(customFolder, "*.rpo");
                            if (inCustom.Length > 0 && !File.Exists(Path.Combine(apoFolder, customFileName)))
                                customFileName = Path.GetFileName(inCustom[0]);
                        }
        
                        string activePath = Path.Combine(apoFolder, customFileName);
                        string savedPath = Path.Combine(customFolder, customFileName);
        
                        bool isActive = File.Exists(activePath);
        
                        if (isActive)
                        {
                            
                            Directory.CreateDirectory(customFolder);
                            if (File.Exists(savedPath))
                                File.Delete(savedPath);
        
                            File.Move(activePath, savedPath);
        
                            UiTheme.ApplyRpoButtonStyle(btnCustom, false);
                            btnCustom.ToolTip = "RPOCustom Inativo (Clique para reativar)";
                            if (lblSize != null)
                                lblSize.Text = ObterTamanhoCustomRpoFormatado(amb);
                        }
                        else
                        {
                            
                            if (!File.Exists(savedPath))
                            {
                                MessageBox.Show(this, "Arquivo " + customFileName + " não foi encontrado em " + customFolder + " para restaurar.", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
        
                            MessageBoxResult res = MessageBox.Show(this, "Isso irá restaurar/sobrescrever o custom.rpo pelo arquivo de totvshub_customrpo. Deseja continuar?", "TOTVSHub", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (res != MessageBoxResult.Yes)
                                return;
        
                            if (File.Exists(activePath))
                                File.Delete(activePath);
        
                            File.Move(savedPath, activePath);
        
                            UiTheme.ApplyRpoButtonStyle(btnCustom, true);
                            btnCustom.ToolTip = "RPOCustom Ativo (Clique para desativar)";
                            if (lblSize != null)
                                lblSize.Text = ObterTamanhoCustomRpoFormatado(amb);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Erro ao alternar RPOCustom: " + ex.Message + "\n\nSe o ambiente TOTVS estiver em execução, encerre-o antes de alterar os arquivos RPO.", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

        private void CopiarDadosCardParaClipboard(AmbienteInfo amb)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(string.Format("Ambiente: {0}", amb.NomeIni));
                        sb.AppendLine(string.Format("Status: {0}", amb.State));
                        if (amb.Pid > 0)
                        {
                            sb.AppendLine(string.Format("PID: {0} ({1})", amb.Pid, amb.RunningAsService ? "Serviço" : "Console"));
                            if (!string.IsNullOrEmpty(amb.ProcessUser))
                                sb.AppendLine(string.Format("Usuário: {0}", amb.ProcessUser));
                        }
                        sb.AppendLine(string.Format("Caminho: {0}", amb.AppDir));
                        if (!string.IsNullOrEmpty(amb.WebAppPort))
                            sb.AppendLine(string.Format("WebApp: {0}", amb.WebAppPort));
                        if (!string.IsNullOrEmpty(amb.TcpPort))
                            sb.AppendLine(string.Format("TCP: {0}", amb.TcpPort));
                        if (!string.IsNullOrEmpty(amb.RestPort))
                            sb.AppendLine(string.Format("REST: {0}", amb.RestPort));
                        if (!string.IsNullOrEmpty(amb.WebServicesPort))
                            sb.AppendLine(string.Format("WebServices: {0}", amb.WebServicesPort));
                        if (!string.IsNullOrEmpty(amb.SpecialKey))
                            sb.AppendLine(string.Format("SpecialKey: {0}", amb.SpecialKey));
                        if (!string.IsNullOrEmpty(amb.DBAlias))
                            sb.AppendLine(string.Format("DBAlias: {0}", amb.DBAlias));
                        if (!string.IsNullOrEmpty(amb.DBPort))
                            sb.AppendLine(string.Format("DBPort: {0}", amb.DBPort));
        
                        string nota;
                        if (_cardNotes.TryGetValue(amb.AppDir, out nota) && !string.IsNullOrWhiteSpace(nota))
                            sb.AppendLine(string.Format("Comentário: {0}", nota));
        
                        bool d1 = ObterD1Ativos().Contains(amb.AppDir);
                        sb.AppendLine(string.Format("D-1: {0}", d1 ? "Ativo" : "Inativo"));
        
                        bool custom = VerificarRpoCustomAtivo(amb);
                        sb.AppendLine(string.Format("RPOCustom: {0}", custom ? "Ativo" : "Inativo"));
        
                        Clipboard.SetDataObject(sb.ToString().TrimEnd(), true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Erro ao copiar dados para a área de transferência: " + ex.Message, "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

        private void AbrirNoExplorer(string path)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(path)) return;
                        path = path.Trim().Trim('"');
        
                        if (Directory.Exists(path))
                        {
                            Process.Start("explorer.exe", path);
                        }
                        else if (File.Exists(path))
                        {
                            Process.Start("explorer.exe", "/select,\"" + path + "\"");
                        }
                        else
                        {
                            string parent = Path.GetDirectoryName(path);
                            if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                                Process.Start("explorer.exe", parent);
                            else
                                MessageBox.Show(this, "Caminho não encontrado no sistema: " + path, "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Erro ao abrir o Windows Explorer: " + ex.Message, "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
    }
}
