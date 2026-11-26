using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private DbAccessConfig _dbAccessConfig = new DbAccessConfig();

        private string _dbAccessPathPrefPath;

        private string _dbAccessHiddenPrefPath;

        private DateTime _dbAccessLastUpdatedTime = DateTime.Now;

        private HashSet<string> _hiddenDbAccessEnvs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private string _dbAccessNote = "";

        private string _dbAccessNotePrefPath;

        private string _dbAccessFilterText = "";

        private bool _dbAccessShowingHidden = false;

        private void CarregarNotaDbAccessAppData()
                {
                    if (string.IsNullOrEmpty(_dbAccessNotePrefPath))
                        _dbAccessNotePrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_nota.txt");
        
                    try
                    {
                        if (File.Exists(_dbAccessNotePrefPath))
                            _dbAccessNote = File.ReadAllText(_dbAccessNotePrefPath, Encoding.UTF8).Trim();
                        else
                            _dbAccessNote = "";
                    }
                    catch { _dbAccessNote = ""; }
                }

        private void SalvarNotaDbAccessAppData()
                {
                    if (string.IsNullOrEmpty(_dbAccessNotePrefPath))
                        _dbAccessNotePrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_nota.txt");
        
                    try
                    {
                        File.WriteAllText(_dbAccessNotePrefPath, _dbAccessNote ?? "", Encoding.UTF8);
                    }
                    catch { }
                }

        private void CarregarOcultosDbAccessAppData()
                {
                    if (string.IsNullOrEmpty(_dbAccessHiddenPrefPath))
                        _dbAccessHiddenPrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_ocultos.txt");
        
                    _hiddenDbAccessEnvs.Clear();
                    if (File.Exists(_dbAccessHiddenPrefPath))
                    {
                        try
                        {
                            string[] lines = File.ReadAllLines(_dbAccessHiddenPrefPath, Encoding.UTF8);
                            foreach (string l in lines)
                            {
                                string trimmed = l.Trim();
                                if (!string.IsNullOrEmpty(trimmed))
                                    _hiddenDbAccessEnvs.Add(trimmed);
                            }
                        }
                        catch { }
                    }
                }

        private void SalvarOcultosDbAccessAppData()
                {
                    if (string.IsNullOrEmpty(_dbAccessHiddenPrefPath))
                        _dbAccessHiddenPrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_ocultos.txt");
        
                    try
                    {
                        List<string> list = new List<string>(_hiddenDbAccessEnvs);
                        File.WriteAllLines(_dbAccessHiddenPrefPath, list.ToArray(), Encoding.UTF8);
                    }
                    catch { }
                }

        private void InicializarDbAccessConfig()
                {
                    CarregarOcultosDbAccessAppData();
        
                    if (string.IsNullOrEmpty(_dbAccessPathPrefPath))
                    {
                        _dbAccessPathPrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_path.txt");
                    }
        
                    if (File.Exists(_dbAccessPathPrefPath))
                    {
                        string savedPath = File.ReadAllText(_dbAccessPathPrefPath, Encoding.UTF8).Trim();
                        if (!string.IsNullOrEmpty(savedPath))
                        {
                            if (File.Exists(savedPath))
                            {
                                _dbAccessConfig.IniPath = savedPath;
                                _dbAccessConfig.FolderPath = System.IO.Path.GetDirectoryName(savedPath);
                            }
                            else if (Directory.Exists(savedPath))
                            {
                                _dbAccessConfig.FolderPath = savedPath;
                                string iniCandidate = System.IO.Path.Combine(savedPath, "dbaccess.ini");
                                if (File.Exists(iniCandidate))
                                {
                                    _dbAccessConfig.IniPath = iniCandidate;
                                }
                            }
                        }
                    }
        
                    
                    if (string.IsNullOrEmpty(_dbAccessConfig.FolderPath) || !Directory.Exists(_dbAccessConfig.FolderPath))
                    {
                        string[] commonPaths = new string[]
                        {
                            @"C:\totvs\dbaccess",
                            @"C:\dbaccess",
                            @"C:\totvs\dbaccess64",
                            @"D:\totvs\dbaccess",
                            @"D:\dbaccess"
                        };
        
                        foreach (string path in commonPaths)
                        {
                            if (Directory.Exists(path) && File.Exists(System.IO.Path.Combine(path, "dbaccess.ini")))
                            {
                                _dbAccessConfig.FolderPath = path;
                                _dbAccessConfig.IniPath = System.IO.Path.Combine(path, "dbaccess.ini");
                                SalvarDbAccessPathAppData(path);
                                break;
                            }
                        }
                    }
                }

        private void SalvarDbAccessPathAppData(string path)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(_dbAccessPathPrefPath))
                        {
                            _dbAccessPathPrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_path.txt");
                        }
                        File.WriteAllText(_dbAccessPathPrefPath, path ?? "", Encoding.UTF8);
                    }
                    catch { }
                }

        private void CarregarDadosDbAccessAsync()
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        
                        ParseDbAccessIni();
        
                        
                        ParseDbMonitorIni();
        
                        
                        TestarStatusDbAccess();
        
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            AtualizarBannerDbAccess();
                            RenderizarCardsDbAccess();
                        }));
                    });
                }

        private void ParseDbAccessIni()
                {
                    if (string.IsNullOrEmpty(_dbAccessConfig.IniPath) || !File.Exists(_dbAccessConfig.IniPath))
                        return;
        
                    try
                    {
                        List<IniSection> sections = ParseIniFile(_dbAccessConfig.IniPath);
                        _dbAccessConfig.RawSections = sections;
                        _dbAccessConfig.Environments.Clear();
        
                        
                        IniSection generalSec = sections.FirstOrDefault(s => string.Equals(s.Name, "General", StringComparison.OrdinalIgnoreCase));
                        if (generalSec != null)
                        {
                            IniEntry licServerEntry = generalSec.Entries.FirstOrDefault(e => string.Equals(e.Key, "LicenseServer", StringComparison.OrdinalIgnoreCase));
                            if (licServerEntry != null && !string.IsNullOrEmpty(licServerEntry.Value))
                                _dbAccessConfig.LicenseServer = licServerEntry.Value;
        
                            IniEntry licPortEntry = generalSec.Entries.FirstOrDefault(e => string.Equals(e.Key, "LicensePort", StringComparison.OrdinalIgnoreCase));
                            if (licPortEntry != null && !string.IsNullOrEmpty(licPortEntry.Value))
                                _dbAccessConfig.LicensePort = licPortEntry.Value;
                        }
        
                        
                        string[] knownDbms = new string[] { "MSSQL", "ORACLE", "POSTGRES", "POSTGRESQL", "MYSQL", "INFORMIX", "DB2", "PROGRESS" };
                        HashSet<string> processedSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
                        foreach (IniSection sec in sections)
                        {
                            if (sec.IsCommented) continue;
        
                            string secName = sec.Name.Trim();
                            bool isKnownDbms = knownDbms.Any(k => string.Equals(k, secName, StringComparison.OrdinalIgnoreCase));
        
                            IniEntry envsEntry = sec.Entries.FirstOrDefault(e => string.Equals(e.Key, "environments", StringComparison.OrdinalIgnoreCase));
                            if (envsEntry != null && !string.IsNullOrEmpty(envsEntry.Value))
                            {
                                string dbmsName = secName.ToUpperInvariant();
                                string[] envNames = envsEntry.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        
                                foreach (string rawEnv in envNames)
                                {
                                    string envName = rawEnv.Trim();
                                    if (string.IsNullOrEmpty(envName)) continue;
        
                                    string targetSecName = dbmsName + "/" + envName;
                                    IniSection envSec = sections.FirstOrDefault(s => string.Equals(s.Name, targetSecName, StringComparison.OrdinalIgnoreCase));
                                    if (envSec == null)
                                    {
                                        envSec = sections.FirstOrDefault(s => string.Equals(s.Name, envName, StringComparison.OrdinalIgnoreCase));
                                    }
        
                                    DbAccessEnvironment env = CriarAmbienteAPartirDaSecao(dbmsName, envName, targetSecName, envSec);
                                    _dbAccessConfig.Environments.Add(env);
                                    if (envSec != null)
                                        processedSections.Add(envSec.Name);
                                }
                            }
                        }
        
                        
                        foreach (IniSection sec in sections)
                        {
                            if (sec.IsCommented || processedSections.Contains(sec.Name))
                                continue;
        
                            int slashIdx = sec.Name.IndexOf('/');
                            if (slashIdx > 0)
                            {
                                string dbms = sec.Name.Substring(0, slashIdx).Trim().ToUpperInvariant();
                                string envName = sec.Name.Substring(slashIdx + 1).Trim();
        
                                if (!string.IsNullOrEmpty(envName))
                                {
                                    DbAccessEnvironment env = CriarAmbienteAPartirDaSecao(dbms, envName, sec.Name, sec);
                                    _dbAccessConfig.Environments.Add(env);
                                    processedSections.Add(sec.Name);
                                }
                            }
                        }
                    }
                    catch { }
                }

        private DbAccessEnvironment CriarAmbienteAPartirDaSecao(string dbms, string envName, string sectionName, IniSection sec)
                {
                    DbAccessEnvironment env = new DbAccessEnvironment();
                    env.Dbms = dbms;
                    env.Name = envName;
                    env.SectionName = sectionName;
        
                    if (sec != null)
                    {
                        foreach (IniEntry e in sec.Entries)
                        {
                            if (e.IsPureComment || string.IsNullOrEmpty(e.Key)) continue;
        
                            string key = e.Key.Trim();
                            string val = e.Value != null ? e.Value.Trim() : "";
        
                            if (string.Equals(key, "user", StringComparison.OrdinalIgnoreCase))
                                env.User = val;
                            else if (string.Equals(key, "password", StringComparison.OrdinalIgnoreCase))
                                env.Password = val;
                            else if (string.Equals(key, "TableSpace", StringComparison.OrdinalIgnoreCase))
                                env.TableSpace = val;
                            else if (string.Equals(key, "IndexSpace", StringComparison.OrdinalIgnoreCase))
                                env.IndexSpace = val;
                            else if (string.Equals(key, "Disable", StringComparison.OrdinalIgnoreCase))
                            {
                                env.IsDisabled = string.Equals(val, "1", StringComparison.OrdinalIgnoreCase) ||
                                                 string.Equals(val, "true", StringComparison.OrdinalIgnoreCase) ||
                                                 string.Equals(val, "yes", StringComparison.OrdinalIgnoreCase);
                            }
                            else if (string.Equals(key, "Server", StringComparison.OrdinalIgnoreCase))
                                env.Server = val;
                            else if (string.Equals(key, "Port", StringComparison.OrdinalIgnoreCase))
                                env.Port = val;
                            else if (string.Equals(key, "Database", StringComparison.OrdinalIgnoreCase))
                                env.Database = val;
                            else if (string.Equals(key, "Alias", StringComparison.OrdinalIgnoreCase))
                                env.Alias = val;
                            else
                                env.AdditionalKeys[key] = val;
                        }
                    }
        
                    return env;
                }

        private void ParseDbMonitorIni()
                {
                    if (string.IsNullOrEmpty(_dbAccessConfig.FolderPath)) return;
        
                    string monIni = System.IO.Path.Combine(_dbAccessConfig.FolderPath, "dbmonitor.ini");
                    _dbAccessConfig.MonitorIniPath = monIni;
        
                    if (File.Exists(monIni))
                    {
                        try
                        {
                            List<IniSection> sections = ParseIniFile(monIni);
                            IniSection monSec = sections.FirstOrDefault(s => string.Equals(s.Name, "Monitor", StringComparison.OrdinalIgnoreCase));
                            if (monSec != null)
                            {
                                IniEntry srv = monSec.Entries.FirstOrDefault(e => string.Equals(e.Key, "Server", StringComparison.OrdinalIgnoreCase));
                                if (srv != null && !string.IsNullOrEmpty(srv.Value))
                                    _dbAccessConfig.MonitorServer = srv.Value.Trim();
        
                                IniEntry prt = monSec.Entries.FirstOrDefault(e => string.Equals(e.Key, "Port", StringComparison.OrdinalIgnoreCase));
                                int portVal;
                                if (prt != null && int.TryParse(prt.Value.Trim(), out portVal) && portVal > 0)
                                    _dbAccessConfig.MonitorPort = portVal;
                            }
                        }
                        catch { }
                    }
                }

        private void TestarStatusDbAccess()
                {
                    
                    try
                    {
                        Process[] procs = Process.GetProcessesByName("dbaccess");
                        if (procs.Length == 0)
                            procs = Process.GetProcessesByName("dbaccess64");
        
                        int dbPid = 0;
                        if (procs.Length > 0)
                        {
                            dbPid = procs[0].Id;
                            _dbAccessConfig.ProcessName = procs[0].ProcessName;
                        }
                        else
                        {
                            int mPort = _dbAccessConfig.MonitorPort > 0 ? _dbAccessConfig.MonitorPort : 7890;
                            dbPid = NativeWin32.GetProcessIdByTcpPort(mPort);
                            if (dbPid > 0) _dbAccessConfig.ProcessName = "dbaccess";
                        }
        
                        if (dbPid > 0)
                        {
                            _dbAccessConfig.IsRunning = true;
                            _dbAccessConfig.ProcessId = dbPid;
                            string owner = NativeWin32.GetProcessOwnerUser(dbPid);
                            if (string.IsNullOrWhiteSpace(owner) || string.Equals(owner, "N/A", StringComparison.OrdinalIgnoreCase) || owner.IndexOf("SYSTEM", StringComparison.OrdinalIgnoreCase) >= 0)
                                owner = "system";
                            _dbAccessConfig.ProcessUser = owner;
        
                            TimeSpan lastCpu = _dbAccessConfig.LastCpuTime;
                            DateTime lastSample = _dbAccessConfig.LastCpuSampleTime;
                            double cpu;
                            long ram;
                            NativeWin32.GetProcessCpuAndRam(_dbAccessConfig.ProcessId, ref lastCpu, ref lastSample, out cpu, out ram);
                            _dbAccessConfig.CpuPercent = cpu;
                            _dbAccessConfig.RamMb = ram;
                            _dbAccessConfig.LastCpuTime = lastCpu;
                            _dbAccessConfig.LastCpuSampleTime = lastSample;
                        }
                        else
                        {
                            _dbAccessConfig.IsRunning = false;
                            _dbAccessConfig.ProcessId = 0;
                            _dbAccessConfig.ProcessName = "";
                            _dbAccessConfig.ProcessUser = null;
                            _dbAccessConfig.CpuPercent = 0;
                            _dbAccessConfig.RamMb = 0;
                        }
                    }
                    catch
                    {
                        _dbAccessConfig.IsRunning = false;
                        _dbAccessConfig.CpuPercent = 0;
                        _dbAccessConfig.RamMb = 0;
                    }
        
                    
                    int targetPort = _dbAccessConfig.MonitorPort > 0 ? _dbAccessConfig.MonitorPort : 7890;
                    string targetHost = !string.IsNullOrEmpty(_dbAccessConfig.MonitorServer) ? _dbAccessConfig.MonitorServer : "127.0.0.1";
                    if (string.Equals(targetHost, "localhost", StringComparison.OrdinalIgnoreCase))
                        targetHost = "127.0.0.1";
        
                    Stopwatch sw = Stopwatch.StartNew();
                    bool portOpen = false;
                    try
                    {
                        using (TcpClient client = new TcpClient())
                        {
                            IAsyncResult ar = client.BeginConnect(targetHost, targetPort, null, null);
                            bool connected = ar.AsyncWaitHandle.WaitOne(750, true);
                            if (connected && client.Connected)
                            {
                                client.EndConnect(ar);
                                portOpen = true;
                            }
                        }
                    }
                    catch
                    {
                        portOpen = false;
                    }
                    sw.Stop();
        
                    _dbAccessConfig.IsPortOpen = portOpen;
                    _dbAccessConfig.LatencyMs = portOpen ? sw.ElapsedMilliseconds : -1;
        
                    if (_dbAccessConfig.IsPortOpen && _dbAccessConfig.IsRunning)
                    {
                        _dbAccessConfig.StatusSummary = string.Format("Ativo (PID {0}, Porta {1} respondendo em {2}ms)",
                            _dbAccessConfig.ProcessId, targetPort, _dbAccessConfig.LatencyMs);
                    }
                    else if (_dbAccessConfig.IsPortOpen)
                    {
                        _dbAccessConfig.StatusSummary = string.Format("Ativo (Porta {0} respondendo em {1}ms)",
                            targetPort, _dbAccessConfig.LatencyMs);
                    }
                    else if (_dbAccessConfig.IsRunning)
                    {
                        _dbAccessConfig.StatusSummary = string.Format("Processo em execução (PID {0}), porta {1} fechada",
                            _dbAccessConfig.ProcessId, targetPort);
                    }
                    else
                    {
                        _dbAccessConfig.StatusSummary = string.Format("Inativo (Porta {0} inacessível)", targetPort);
                    }
                }

        private void IniciarDbAccessAsync()
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        try
                        {
                            if (_dbAccessConfig.IsService && !string.IsNullOrEmpty(_dbAccessConfig.ServiceName))
                            {
                                using (ServiceController sc = new ServiceController(_dbAccessConfig.ServiceName))
                                {
                                    if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                                    {
                                        sc.Start();
                                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                                    }
                                }
                            }
                            else
                            {
                                string exe = Path.Combine(_dbAccessConfig.FolderPath, "dbaccess64.exe");
                                if (!File.Exists(exe)) exe = Path.Combine(_dbAccessConfig.FolderPath, "dbaccess.exe");
                                if (File.Exists(exe))
                                {
                                    ProcessStartInfo psi = new ProcessStartInfo(exe);
                                    psi.WorkingDirectory = _dbAccessConfig.FolderPath;
                                    psi.UseShellExecute = true;
                                    Process.Start(psi);
                                }
                            }
                            Thread.Sleep(1200);
                            CarregarDadosDbAccessAsync();
                        }
                        catch { }
                    });
                }

        private void PararDbAccessAsync()
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        try
                        {
                            if (_dbAccessConfig.IsService && !string.IsNullOrEmpty(_dbAccessConfig.ServiceName))
                            {
                                using (ServiceController sc = new ServiceController(_dbAccessConfig.ServiceName))
                                {
                                    if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                                    {
                                        sc.Stop();
                                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
                                    }
                                }
                            }
        
                            if (_dbAccessConfig.ProcessId > 0)
                            {
                                using (Process p = Process.GetProcessById(_dbAccessConfig.ProcessId))
                                {
                                    if (!p.HasExited)
                                    {
                                        p.Kill();
                                        p.WaitForExit(3000);
                                    }
                                }
                            }
                            Thread.Sleep(800);
                            CarregarDadosDbAccessAsync();
                        }
                        catch { }
                    });
                }

        private void ReiniciarDbAccessAsync()
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        try
                        {
                            PararDbAccessAsync();
                            Thread.Sleep(1500);
                            IniciarDbAccessAsync();
                        }
                        catch { }
                    });
                }

        private void CopiarDadosAmbienteDbAccessParaClipboard(DbAccessEnvironment env)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("[" + (env.SectionName ?? (env.Dbms + "/" + env.Name)) + "]");
                        if (!string.IsNullOrEmpty(env.User)) sb.AppendLine("user=" + env.User);
                        if (!string.IsNullOrEmpty(env.TableSpace)) sb.AppendLine("TableSpace=" + env.TableSpace);
                        if (!string.IsNullOrEmpty(env.IndexSpace)) sb.AppendLine("IndexSpace=" + env.IndexSpace);
                        sb.AppendLine("Disable=" + (env.IsDisabled ? "1" : "0"));
                        if (!string.IsNullOrEmpty(env.Server)) sb.AppendLine("Server=" + env.Server);
                        if (!string.IsNullOrEmpty(env.Port)) sb.AppendLine("Port=" + env.Port);
                        if (!string.IsNullOrEmpty(env.Database)) sb.AppendLine("Database=" + env.Database);
                        if (!string.IsNullOrEmpty(env.Alias)) sb.AppendLine("Alias=" + env.Alias);
                        foreach (KeyValuePair<string, string> kv in env.AdditionalKeys)
                            sb.AppendLine(kv.Key + "=" + kv.Value);
        
                        Clipboard.SetText(sb.ToString());
                    }
                    catch { }
                }

        private void AlternarEstadoAmbienteDbAccess(DbAccessEnvironment env)
                {
                    if (string.IsNullOrEmpty(_dbAccessConfig.IniPath) || !File.Exists(_dbAccessConfig.IniPath))
                        return;
        
                    try
                    {
                        bool novoEstadoDisabled = !env.IsDisabled;
                        string secTarget = env.SectionName ?? (env.Dbms + "/" + env.Name);
        
                        List<IniSection> sections = ParseIniFile(_dbAccessConfig.IniPath);
                        IniSection sec = sections.FirstOrDefault(s => string.Equals(s.Name, secTarget, StringComparison.OrdinalIgnoreCase));
                        if (sec == null)
                            sec = sections.FirstOrDefault(s => string.Equals(s.Name, env.Name, StringComparison.OrdinalIgnoreCase));
        
                        if (sec != null)
                        {
                            IniEntry disEntry = sec.Entries.FirstOrDefault(e => string.Equals(e.Key, "Disable", StringComparison.OrdinalIgnoreCase));
                            if (disEntry != null)
                            {
                                disEntry.Value = novoEstadoDisabled ? "1" : "0";
                                disEntry.IsCommented = false;
                            }
                            else
                            {
                                IniEntry newEntry = new IniEntry();
                                newEntry.Key = "Disable";
                                newEntry.Value = novoEstadoDisabled ? "1" : "0";
                                sec.Entries.Add(newEntry);
                            }
        
                            SalvarArquivoIni(_dbAccessConfig.IniPath, sections);
                            env.IsDisabled = novoEstadoDisabled;
        
                            
                            if (env.StatusBadge != null && env.StatusText != null)
                            {
                                env.StatusBadge.Background = env.IsDisabled ? UiTheme.BrushFromHex("#475569") : UiTheme.AccentGreen;
                                env.StatusText.Text = env.IsDisabled ? "Desabilitado" : "Habilitado";
                            }
        
                            AtualizarBannerDbAccess();
                            RenderizarCardsDbAccess();
                        }
                    }
                    catch (Exception ex)
                    {
                        StyledDialog.Info(this, "Erro", "Falha ao atualizar dbaccess.ini: " + ex.Message);
                    }
                }

        private static void AtualizarOuAdicionarChave(IniSection sec, string key, string value)
                {
                    IniEntry e = sec.Entries.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));
                    if (e != null)
                    {
                        e.Value = value ?? "";
                        e.IsCommented = false;
                    }
                    else
                    {
                        IniEntry newEntry = new IniEntry();
                        newEntry.Key = key;
                        newEntry.Value = value ?? "";
                        sec.Entries.Add(newEntry);
                    }
                }

        private void SalvarArquivoIni(string path, List<IniSection> sections)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (IniSection sec in sections)
                    {
                        if (string.IsNullOrEmpty(sec.Name)) continue;
        
                        if (sec.IsCommented)
                            sb.AppendLine(";" + "[" + sec.Name + "]");
                        else
                            sb.AppendLine("[" + sec.Name + "]");
        
                        foreach (IniEntry e in sec.Entries)
                        {
                            if (e.IsPureComment)
                            {
                                sb.AppendLine(";" + (e.Value ?? ""));
                            }
                            else
                            {
                                string line = string.Format("{0}={1}", e.Key, e.Value ?? "");
                                if (e.IsCommented)
                                    sb.AppendLine(";" + line);
                                else
                                    sb.AppendLine(line);
                            }
                        }
                        sb.AppendLine();
                    }
        
                    File.WriteAllText(path, sb.ToString(), Encoding.Default);
                }
    }
}
