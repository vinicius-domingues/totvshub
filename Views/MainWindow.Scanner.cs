using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows;
using Microsoft.Win32;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        // =====================================================================
        // DESCOBERTA ASSINCRONA DE AMBIENTES E DETECCAO DE PID/SERVICOS/USUARIO
        // =====================================================================
        private void SelecionarPastaBase()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Selecione qualquer arquivo dentro da pasta raiz (ex: C:\\bases)";
            dlg.Filter = "Pastas|*.none";
            dlg.FileName = "Selecionar esta pasta";
            dlg.CheckFileExists = false;
            dlg.CheckPathExists = true;
            if (Directory.Exists(_rootPath))
                dlg.InitialDirectory = _rootPath;

            if (dlg.ShowDialog() == true)
            {
                string chosen = Path.GetDirectoryName(dlg.FileName);
                if (!string.IsNullOrEmpty(chosen) && Directory.Exists(chosen))
                {
                    _rootPath = chosen;
                    _needsOnboarding = false;
                    if (_rootPathLabel != null)
                        _rootPathLabel.Text = "Diretório: " + _rootPath;
                    File.WriteAllText(_configPath, _rootPath, Encoding.UTF8);
                    CarregarAmbientesAsync(true);
                }
            }
        }

        private void CarregarAmbientesAsync(bool animateBeam)
        {
            if (_isLoadingBases) return;
            _isLoadingBases = true;

            if (animateBeam)
                DispararAnimacaoLoadingHorizontal();

            string currentRoot = _rootPath;
            bool skipOld = _ignoreOldFolders;
            bool fetchUser = _showProcessUser;
            bool isFirstScan = !_isInitialScanCompleted;

            ThreadPool.QueueUserWorkItem(delegate
            {
                List<AmbienteInfo> novosAmbientes = new List<AmbienteInfo>();
                try
                {
                    if (isFirstScan)
                    {
                        Thread.Sleep(550);
                    }

                    if (Directory.Exists(currentRoot))
                    {
                        List<string> exesEncontrados = new List<string>();
                        BuscarExecutaveis(currentRoot, 0, 4, skipOld, exesEncontrados);

                        foreach (string exePath in exesEncontrados)
                        {
                            string appDir = Path.GetDirectoryName(exePath);
                            string iniPath = Path.Combine(appDir, "appserver.ini");
                            if (!File.Exists(iniPath))
                                continue;

                            AmbienteInfo amb = new AmbienteInfo();
                            amb.ExePath = NormalizePath(exePath);
                            amb.AppDir = NormalizePath(appDir);
                            amb.IniPath = iniPath;
                            AtualizarDadosIniAmbiente(amb);
                            novosAmbientes.Add(amb);
                        }
                    }

                    novosAmbientes = novosAmbientes.OrderBy(a => a.NomeIni).ToList();
                    SincronizarEstadoProcessosEServicos(novosAmbientes, fetchUser);
                }
                catch { }

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    try
                    {
                        _ambientes = novosAmbientes;
                        _isInitialScanCompleted = true;
                        _lastUpdatedTime = DateTime.Now;
                        if (_lastUpdateLabel != null)
                            _lastUpdateLabel.Text = string.Format("Atualizado em {0:dd/MM/yyyy HH:mm:ss}  •", _lastUpdatedTime);

                        RecalcularAlertas();
                        RenderizarCards();
                    }
                    finally
                    {
                        _isLoadingBases = false;
                    }
                }));
            });
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            try
            {
                return Path.GetFullPath(path.Trim().Trim('"')).TrimEnd('\\');
            }
            catch
            {
                return path.Trim().Trim('"').TrimEnd('\\');
            }
        }

        private static string ExtractExeFromServicePathName(string pathName)
        {
            if (string.IsNullOrWhiteSpace(pathName)) return "";
            string p = pathName.Trim();
            if (p.StartsWith("\""))
            {
                int endQuote = p.IndexOf('"', 1);
                if (endQuote > 1)
                    return NormalizePath(p.Substring(1, endQuote - 1));
            }
            int exeIdx = p.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIdx > 0)
            {
                return NormalizePath(p.Substring(0, exeIdx + 4));
            }
            return NormalizePath(p);
        }

        private void BuscarExecutaveis(string dir, int nivel, int maxNivel, bool ignoreOld, List<string> resultados)
        {
            if (nivel > maxNivel) return;

            try
            {
                string folderName = Path.GetFileName(dir.TrimEnd('\\'));
                if (ignoreOld && !string.IsNullOrEmpty(folderName) &&
                    folderName.EndsWith(".old", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string candidato = Path.Combine(dir, "appserver.exe");
                if (File.Exists(candidato))
                {
                    resultados.Add(candidato);
                    return;
                }

                string[] subDirs = Directory.GetDirectories(dir);
                foreach (string sub in subDirs)
                {
                    BuscarExecutaveis(sub, nivel + 1, maxNivel, ignoreOld, resultados);
                }
            }
            catch { }
        }

        private string ObterValorSecaoExata(AmbienteInfo info, string sectionName, string keyName)
        {
            Dictionary<string, string> secDict;
            string val;
            if (!string.IsNullOrEmpty(sectionName) &&
                info.IniSectionsMap.TryGetValue(sectionName, out secDict) &&
                secDict.TryGetValue(keyName, out val) &&
                !string.IsNullOrWhiteSpace(val))
            {
                return val;
            }
            return null;
        }

        private string ObterValorChaveIni(AmbienteInfo info, string preferredSection, string keyName)
        {
            string exact = ObterValorSecaoExata(info, preferredSection, keyName);
            if (!string.IsNullOrWhiteSpace(exact))
                return exact;

            string val;
            foreach (KeyValuePair<string, Dictionary<string, string>> kv in info.IniSectionsMap)
            {
                if (kv.Value.TryGetValue(keyName, out val) && !string.IsNullOrWhiteSpace(val))
                    return val;
            }
            return null;
        }

        private void AtualizarDadosIniAmbiente(AmbienteInfo info)
        {
            string subBase = ObterCaminhoSubpastaBase(info);
            if (!string.IsNullOrEmpty(subBase))
            {
                info.PastaBase = Path.GetFileName(subBase.TrimEnd('\\', '/'));
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(info.AppDir);
                if (di.Parent != null && di.Parent.Parent != null)
                    info.PastaBase = di.Parent.Parent.Name;
                else
                    info.PastaBase = di.Name;
            }

            info.WebAppPort = null;
            info.TcpPort = null;
            info.RestPort = null;
            info.WebServicesPort = null;
            info.SpecialKey = null;
            info.DBAlias = null;
            info.DBPort = null;
            info.IniSectionsMap.Clear();

            string primeiroBloco = null;
            string secaoAtual = "";

            string[] linhas = File.ReadAllLines(info.IniPath, Encoding.Default);
            foreach (string rawLine in linhas)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
                    continue;

                if (line.StartsWith("[") && line.Contains("]"))
                {
                    int endIdx = line.IndexOf(']');
                    string secName = line.Substring(1, endIdx - 1).Trim();
                    secaoAtual = secName;

                    if (!string.IsNullOrEmpty(secName))
                    {
                        if (primeiroBloco == null)
                            primeiroBloco = secName;

                        if (!info.IniSectionsMap.ContainsKey(secName))
                        {
                            info.IniSectionsMap[secName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                    }
                }
                else
                {
                    int eqIdx = line.IndexOf('=');
                    if (eqIdx > 0 && !string.IsNullOrEmpty(secaoAtual))
                    {
                        string key = line.Substring(0, eqIdx).Trim();
                        string val = line.Substring(eqIdx + 1);
                        int commentIdx = val.IndexOf(';');
                        if (commentIdx >= 0)
                            val = val.Substring(0, commentIdx);
                        val = val.Trim();

                        Dictionary<string, string> secDict;
                        if (info.IniSectionsMap.TryGetValue(secaoAtual, out secDict))
                        {
                            secDict[key] = val;
                        }

                        if (string.Equals(key, "PORT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val))
                        {
                            if (string.Equals(secaoAtual, "WEBAPP", StringComparison.OrdinalIgnoreCase))
                                info.WebAppPort = val;
                            else if (string.Equals(secaoAtual, "TCP", StringComparison.OrdinalIgnoreCase))
                                info.TcpPort = val;
                            else if (string.Equals(secaoAtual, "HTTPREST", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(secaoAtual, "REST", StringComparison.OrdinalIgnoreCase))
                            {
                                info.RestPort = val;
                            }
                            else if (string.Equals(secaoAtual, "WEBSERVICES", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(secaoAtual, "WEBSERVICE", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(secaoAtual, "WS", StringComparison.OrdinalIgnoreCase))
                            {
                                info.WebServicesPort = val;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(info.WebServicesPort))
            {
                if (info.IniSectionsMap.ContainsKey("WEBSERVICES") ||
                    info.IniSectionsMap.ContainsKey("WEBSERVICE") ||
                    info.IniSectionsMap.ContainsKey("WS"))
                {
                    info.WebServicesPort = ObterValorSecaoExata(info, "WEBSERVICES", "Port")
                                        ?? ObterValorSecaoExata(info, "WS", "Port")
                                        ?? ObterValorSecaoExata(info, "HTTP", "Port");
                }
                else if (!string.IsNullOrEmpty(info.RestPort))
                {
                    info.WebServicesPort = ObterValorSecaoExata(info, "HTTP", "Port");
                }
            }

            if (string.IsNullOrEmpty(info.RestPort))
            {
                string httpPort = ObterValorSecaoExata(info, "HTTP", "Port");
                if (!string.IsNullOrEmpty(httpPort) && string.IsNullOrEmpty(info.WebServicesPort))
                {
                    info.RestPort = httpPort;
                }
            }

            info.NomeIni = !string.IsNullOrEmpty(primeiroBloco) ? primeiroBloco : info.PastaBase;
            info.SpecialKey = ObterValorChaveIni(info, info.NomeIni, "SpecialKey");

            info.DBAlias = ObterValorSecaoExata(info, info.NomeIni, "DBAlias")
                        ?? ObterValorSecaoExata(info, "ENVIRONMENT", "DBAlias")
                        ?? ObterValorSecaoExata(info, "DBACCESS", "alias")
                        ?? ObterValorSecaoExata(info, "DBACCESS", "DBAlias")
                        ?? ObterValorSecaoExata(info, "TOPCONNECT", "alias")
                        ?? ObterValorSecaoExata(info, "TOPCONNECT", "DBAlias")
                        ?? ObterValorChaveIni(info, info.NomeIni, "DBAlias")
                        ?? ObterValorChaveIni(info, "DBACCESS", "alias");

            info.DBPort = ObterValorSecaoExata(info, "DBACCESS", "port")
                       ?? ObterValorSecaoExata(info, "DBACCESS", "DBPort")
                       ?? ObterValorSecaoExata(info, "TOPCONNECT", "port")
                       ?? ObterValorSecaoExata(info, "TOPCONNECT", "DBPort")
                       ?? ObterValorSecaoExata(info, info.NomeIni, "DBPort")
                       ?? ObterValorSecaoExata(info, "ENVIRONMENT", "DBPort")
                       ?? ObterValorChaveIni(info, "DBACCESS", "DBPort")
                       ?? ObterValorChaveIni(info, "TOPCONNECT", "DBPort");
        }

        private class ServiceMatchInfo
        {
            public string ServiceName;
            public string DisplayName;
            public string State;
            public int ProcessId;
        }

        private void SincronizarEstadoProcessosEServicos(List<AmbienteInfo> lista, bool fetchUser)
        {
            Dictionary<string, ServiceMatchInfo> serviceMap = new Dictionary<string, ServiceMatchInfo>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> processMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (ManagementObjectSearcher svcSearcher = new ManagementObjectSearcher(
                    "SELECT Name, DisplayName, PathName, State, ProcessId FROM Win32_Service WHERE PathName LIKE '%appserver%'"))
                {
                    foreach (ManagementObject obj in svcSearcher.Get())
                    {
                        string rawPath = obj["PathName"] != null ? obj["PathName"].ToString() : "";
                        string exeNorm = ExtractExeFromServicePathName(rawPath);
                        if (!string.IsNullOrEmpty(exeNorm) && !serviceMap.ContainsKey(exeNorm))
                        {
                            ServiceMatchInfo sm = new ServiceMatchInfo();
                            sm.ServiceName = obj["Name"] != null ? obj["Name"].ToString() : "";
                            sm.DisplayName = obj["DisplayName"] != null ? obj["DisplayName"].ToString() : sm.ServiceName;
                            sm.State = obj["State"] != null ? obj["State"].ToString() : "";
                            sm.ProcessId = obj["ProcessId"] != null ? Convert.ToInt32(obj["ProcessId"]) : 0;
                            serviceMap[exeNorm] = sm;
                        }
                    }
                }
            }
            catch { }

            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.ProcessName.IndexOf("appserver", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string fullPath = NativeWin32.GetProcessImagePathSafe(p.Id);
                            if (string.IsNullOrEmpty(fullPath))
                            {
                                try { fullPath = p.MainModule.FileName; } catch { }
                            }

                            if (!string.IsNullOrEmpty(fullPath))
                            {
                                string exeNorm = NormalizePath(fullPath);
                                if (!string.IsNullOrEmpty(exeNorm) && !processMap.ContainsKey(exeNorm))
                                    processMap[exeNorm] = p.Id;
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        p.Dispose();
                    }
                }
            }
            catch { }

            try
            {
                using (ManagementObjectSearcher procSearcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE Name LIKE 'appserver%.exe'"))
                {
                    foreach (ManagementObject obj in procSearcher.Get())
                    {
                        object pathObj = obj["ExecutablePath"];
                        object pidObj = obj["ProcessId"];
                        if (pathObj != null && pidObj != null)
                        {
                            string exeNorm = NormalizePath(pathObj.ToString());
                            int pid = Convert.ToInt32(pidObj);
                            if (!string.IsNullOrEmpty(exeNorm) && !processMap.ContainsKey(exeNorm))
                                processMap[exeNorm] = pid;
                        }
                    }
                }
            }
            catch { }

            foreach (AmbienteInfo amb in lista)
            {
                string key = NormalizePath(amb.ExePath);

                ServiceMatchInfo svc;
                if (serviceMap.TryGetValue(key, out svc))
                {
                    amb.IsServiceRegistered = true;
                    amb.ServiceName = svc.ServiceName;
                    amb.ServiceDisplayName = svc.DisplayName;
                }
                else
                {
                    amb.IsServiceRegistered = false;
                    amb.ServiceName = null;
                    amb.ServiceDisplayName = null;
                }

                if (amb.State == EnvRunState.Initializing || amb.State == EnvRunState.Stopping)
                    continue;

                EnvRunState previousState = amb.State;

                if (svc != null && string.Equals(svc.State, "Running", StringComparison.OrdinalIgnoreCase))
                {
                    amb.State = EnvRunState.Active;
                    amb.RunningAsService = true;
                    int pId;
                    amb.Pid = svc.ProcessId > 0 ? svc.ProcessId : (processMap.TryGetValue(key, out pId) ? pId : 0);
                    amb.ProcessUser = (fetchUser && amb.Pid > 0) ? NativeWin32.GetProcessOwnerUser(amb.Pid) : null;
                }
                else
                {
                    int pid;
                    if (processMap.TryGetValue(key, out pid))
                    {
                        amb.State = EnvRunState.Active;
                        amb.RunningAsService = (svc != null && svc.ProcessId == pid);
                        amb.Pid = pid;
                        amb.ProcessUser = (fetchUser && pid > 0) ? NativeWin32.GetProcessOwnerUser(pid) : null;
                    }
                    else
                    {
                        // Casamento inteligente por portas (detecta AppServer mesmo iniciado por outro usuário no servidor)
                        int portPid = 0;
                        int tcpP;
                        if (int.TryParse(amb.TcpPort, out tcpP) && tcpP > 0)
                            portPid = NativeWin32.GetProcessIdByTcpPort(tcpP);

                        if (portPid == 0)
                        {
                            int webP;
                            if (int.TryParse(amb.WebAppPort, out webP) && webP > 0)
                                portPid = NativeWin32.GetProcessIdByTcpPort(webP);
                        }

                        if (portPid == 0)
                        {
                            int restP;
                            if (int.TryParse(amb.RestPort, out restP) && restP > 0)
                                portPid = NativeWin32.GetProcessIdByTcpPort(restP);
                        }

                        if (portPid > 0)
                        {
                            amb.State = EnvRunState.Active;
                            amb.RunningAsService = (svc != null && svc.ProcessId == portPid);
                            amb.Pid = portPid;
                            amb.ProcessUser = (fetchUser && portPid > 0) ? NativeWin32.GetProcessOwnerUser(portPid) : null;
                        }
                        else
                        {
                            amb.State = EnvRunState.Stopped;
                            amb.RunningAsService = amb.IsServiceRegistered;
                            amb.Pid = 0;
                            amb.ProcessUser = null;
                        }
                    }
                }

                // Monitoramento de CPU e RAM (Item 11)
                if (amb.State == EnvRunState.Active && amb.Pid > 0)
                {
                    TimeSpan lastCpu = amb.LastCpuTime;
                    DateTime lastSample = amb.LastCpuSampleTime;
                    double cpu;
                    long ram;
                    NativeWin32.GetProcessCpuAndRam(amb.Pid, ref lastCpu, ref lastSample, out cpu, out ram);
                    amb.CpuPercent = cpu;
                    amb.RamMb = ram;
                    amb.LastCpuTime = lastCpu;
                    amb.LastCpuSampleTime = lastSample;
                }
                else
                {
                    amb.CpuPercent = 0;
                    amb.RamMb = 0;
                }

                // Notificação de queda de serviço monitorado (Item 3)
                if (previousState == EnvRunState.Active && amb.State == EnvRunState.Stopped)
                {
                    RegistrarAlertaQuedaAmbiente(amb);
                }
            }
        }

        private void DispararAtualizacaoProcessosAsync()
        {
            if (_isPollingProcesses) return;
            _isPollingProcesses = true;

            List<AmbienteInfo> snapshot = _ambientes.ToList();
            bool fetchUser = _showProcessUser;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    SincronizarEstadoProcessosEServicos(snapshot, fetchUser);
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        try
                        {
                            AtualizarResumoEVisualCards();
                        }
                        finally
                        {
                            _isPollingProcesses = false;
                        }
                    }));
                }
            });
        }

        private void RegistrarAlertaQuedaAmbiente(AmbienteInfo amb)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                AlertaConflito al = new AlertaConflito();
                al.Titulo = "Queda de Serviço: " + amb.NomeIni;
                al.Descricao = string.Format("O ambiente '{0}' parou inesperadamente ou caiu.", amb.NomeIni);
                al.Envolvidos.Add(amb);

                bool alreadyHas = _alertasAtivos.Any(a => a.Titulo == al.Titulo);
                if (!alreadyHas)
                {
                    _alertasAtivos.Add(al);
                    if (_alertBadge != null && _alertCountText != null)
                    {
                        _alertCountText.Text = _alertasAtivos.Count.ToString();
                        _alertBadge.Background = UiTheme.AccentRed;
                    }
                    if (_trayIcon != null)
                    {
                        try
                        {
                            _trayIcon.ShowBalloonTip(4000, "Ambiente Caiu", string.Format("O serviço do ambiente {0} parou.", amb.NomeIni), System.Windows.Forms.ToolTipIcon.Warning);
                        }
                        catch { }
                    }
                }
            }));
        }

        // =====================================================================
        // MOTOR DINAMICO DE ALERTAS (TITULO CURTO: "Duplicidade em <chave>")
        // =====================================================================
        private void RecalcularAlertas()
        {
            _alertasAtivos.Clear();

            List<AmbienteInfo> ativos = _ambientes
                .Where(a => !_hiddenDirs.Contains(a.AppDir))
                .ToList();

            foreach (ConflictRule rule in _conflictRules)
            {
                if (string.IsNullOrWhiteSpace(rule.SectionPattern))
                    continue;

                bool isEnvPlaceholder = string.Equals(rule.SectionPattern, "<ambiente>", StringComparison.OrdinalIgnoreCase);

                if (rule.IsSelfSection)
                {
                    if (isEnvPlaceholder)
                    {
                        foreach (var grp in ativos.Where(a => !string.IsNullOrEmpty(a.NomeIni))
                                                  .GroupBy(a => a.NomeIni, StringComparer.OrdinalIgnoreCase)
                                                  .Where(g => g.Count() > 1))
                        {
                            if (_ignoreSameSubfolderConflicts)
                            {
                                var envGroups = grp.GroupBy(a => ObterCaminhoSubpastaBase(a), StringComparer.OrdinalIgnoreCase).ToList();
                                if (envGroups.Count <= 1) continue;

                                AlertaConflito al = new AlertaConflito();
                                al.Titulo = string.Format("Duplicidade em [{0}]", grp.Key);
                                al.Descricao = string.Format("{0} com pastas diferentes possuem a mesma primeira seção [{1}].",
                                    UiTheme.Pluralize(envGroups.Count, "ambiente", "ambientes"), grp.Key);
                                al.Envolvidos = envGroups.Select(g => g.First()).ToList();
                                _alertasAtivos.Add(al);
                            }
                            else
                            {
                                AlertaConflito al = new AlertaConflito();
                                al.Titulo = string.Format("Duplicidade em [{0}]", grp.Key);
                                al.Descricao = string.Format("{0} possuem a mesma primeira seção [{1}].",
                                    UiTheme.Pluralize(grp.Count(), "base", "bases"), grp.Key);
                                al.Envolvidos = grp.ToList();
                                _alertasAtivos.Add(al);
                            }
                        }
                    }
                    else
                    {
                        List<AmbienteInfo> comSecao = ativos
                            .Where(a => a.IniSectionsMap.ContainsKey(rule.SectionPattern))
                            .ToList();
                        if (comSecao.Count > 1)
                        {
                            if (_ignoreSameSubfolderConflicts)
                            {
                                var envGroups = comSecao.GroupBy(a => ObterCaminhoSubpastaBase(a), StringComparer.OrdinalIgnoreCase).ToList();
                                if (envGroups.Count <= 1) continue;

                                AlertaConflito al = new AlertaConflito();
                                al.Titulo = string.Format("Duplicidade em [{0}]", rule.SectionPattern);
                                al.Descricao = string.Format("{0} com pastas diferentes possuem a seção [{1}].",
                                    UiTheme.Pluralize(envGroups.Count, "ambiente", "ambientes"), rule.SectionPattern);
                                al.Envolvidos = envGroups.Select(g => g.First()).ToList();
                                _alertasAtivos.Add(al);
                            }
                            else
                            {
                                AlertaConflito al = new AlertaConflito();
                                al.Titulo = string.Format("Duplicidade em [{0}]", rule.SectionPattern);
                                al.Descricao = string.Format("{0} possuem a seção [{1}].",
                                    UiTheme.Pluralize(comSecao.Count, "base", "bases"), rule.SectionPattern);
                                al.Envolvidos = comSecao;
                                _alertasAtivos.Add(al);
                            }
                        }
                    }
                }
                else
                {
                    Dictionary<string, List<AmbienteInfo>> valueGroups = new Dictionary<string, List<AmbienteInfo>>(StringComparer.OrdinalIgnoreCase);

                    foreach (AmbienteInfo amb in ativos)
                    {
                        string targetSection = isEnvPlaceholder ? amb.NomeIni : rule.SectionPattern;
                        if (string.IsNullOrEmpty(targetSection))
                            continue;

                        string foundVal = null;
                        Dictionary<string, string> secDict;
                        if (amb.IniSectionsMap.TryGetValue(targetSection, out secDict))
                        {
                            secDict.TryGetValue(rule.KeyName, out foundVal);
                        }
                        if (string.IsNullOrWhiteSpace(foundVal) && isEnvPlaceholder &&
                            string.Equals(rule.KeyName, "DBAlias", StringComparison.OrdinalIgnoreCase))
                        {
                            foundVal = amb.DBAlias;
                        }
                        if (!string.IsNullOrWhiteSpace(foundVal))
                        {
                            if (!valueGroups.ContainsKey(foundVal))
                                valueGroups[foundVal] = new List<AmbienteInfo>();
                            if (!valueGroups[foundVal].Contains(amb))
                                valueGroups[foundVal].Add(amb);
                        }
                    }

                    foreach (KeyValuePair<string, List<AmbienteInfo>> kv in valueGroups)
                    {
                        if (kv.Value.Count > 1)
                        {
                            if (_ignoreSameSubfolderConflicts)
                            {
                                var envGroups = kv.Value.GroupBy(a => ObterCaminhoSubpastaBase(a), StringComparer.OrdinalIgnoreCase).ToList();
                                if (envGroups.Count <= 1) continue;

                                AlertaConflito al = new AlertaConflito();
                                al.Titulo = string.Format("Duplicidade em {0}", rule.KeyName);
                                al.Descricao = string.Format("{0} com pastas diferentes possuem o mesmo valor \"{1}\" na tag [{2}] > {3}.",
                                    UiTheme.Pluralize(envGroups.Count, "ambiente", "ambientes"),
                                    kv.Key,
                                    rule.SectionPattern,
                                    rule.KeyName);
                                al.Envolvidos = envGroups.Select(g => g.First()).ToList();
                                _alertasAtivos.Add(al);
                            }
                            else
                            {
                                AlertaConflito al = new AlertaConflito();
                                al.Titulo = string.Format("Duplicidade em {0}", rule.KeyName);
                                al.Descricao = string.Format("{0} possuem o mesmo valor \"{1}\" na tag [{2}] > {3}.",
                                    UiTheme.Pluralize(kv.Value.Count, "base", "bases"),
                                    kv.Key,
                                    rule.SectionPattern,
                                    rule.KeyName);
                                al.Envolvidos = kv.Value;
                                _alertasAtivos.Add(al);
                            }
                        }
                    }
                }
            }

            if (_alertBadge != null && _alertCountText != null)
            {
                _alertCountText.Text = _alertasAtivos.Count.ToString();
                _alertBadge.Background = _alertasAtivos.Count > 0
                    ? UiTheme.AccentRed
                    : UiTheme.BrushFromHex("#475569");
            }
        }

        private void AbrirCentralAlertas()
        {
            AlertCenterWindow win = new AlertCenterWindow(_alertasAtivos);
            win.Owner = this;
            win.OnOpenIniRequested = delegate(AmbienteInfo amb)
            {
                ShowIniEditorView(amb);
            };
            win.ShowDialog();
        }
    }
}
