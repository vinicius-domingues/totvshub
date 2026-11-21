using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private List<IniSection> ParseIniFile(string iniPath)
                {
                    List<IniSection> sections = new List<IniSection>();
                    if (!File.Exists(iniPath)) return sections;
        
                    IniSection currentSection = null;
                    string[] lines = File.ReadAllLines(iniPath, Encoding.Default);
        
                    foreach (string raw in lines)
                    {
                        string trimmed = raw.Trim();
                        if (string.IsNullOrEmpty(trimmed))
                            continue;
        
                        bool isCommented = false;
                        string content = trimmed;
        
                        if (content.StartsWith(";"))
                        {
                            isCommented = true;
                            content = content.TrimStart(';').Trim();
                        }
        
                        if (content.StartsWith("[") && content.Contains("]"))
                        {
                            int closeIdx = content.IndexOf(']');
                            string secName = content.Substring(1, closeIdx - 1).Trim();
                            if (!string.IsNullOrEmpty(secName))
                            {
                                currentSection = new IniSection();
                                currentSection.Name = secName;
                                currentSection.IsCommented = isCommented;
                                sections.Add(currentSection);
                                continue;
                            }
                        }
        
                        if (currentSection == null)
                        {
                            currentSection = new IniSection();
                            currentSection.Name = "GERAL";
                            currentSection.IsCommented = false;
                            sections.Add(currentSection);
                        }
        
                        int eqIdx = content.IndexOf('=');
                        if (eqIdx > 0)
                        {
                            string possibleKey = content.Substring(0, eqIdx).Trim();
                            if (!possibleKey.Contains(" ") && !possibleKey.Contains(":"))
                            {
                                string val = content.Substring(eqIdx + 1).Trim();
                                IniEntry entry = new IniEntry();
                                entry.IsPureComment = false;
                                entry.IsCommented = isCommented;
                                entry.Key = possibleKey;
                                entry.Value = val;
                                currentSection.Entries.Add(entry);
                                continue;
                            }
                        }
        
                        IniEntry commentEntry = new IniEntry();
                        commentEntry.IsPureComment = true;
                        commentEntry.IsCommented = true;
                        commentEntry.Key = "";
                        commentEntry.Value = trimmed.TrimStart(';').Trim();
                        currentSection.Entries.Add(commentEntry);
                    }
        
                    return sections;
                }

        private HashSet<string> IdentifyRestSectionNames(List<IniSection> allSections)
                {
                    HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "HTTPV11", "HTTPREST", "HTTPURI", "HTTPJOB"
                    };
        
                    foreach (IniSection s in allSections)
                    {
                        string sName = (s.Name ?? "").Trim();
                        if (string.IsNullOrEmpty(sName)) continue;
        
                        if (string.Equals(sName, "HTTPV11", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (IniEntry e in s.Entries)
                            {
                                if (!e.IsPureComment && string.Equals(e.Key, "Sockets", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Value))
                                {
                                    foreach (string part in e.Value.Split(','))
                                    {
                                        string clean = part.Trim();
                                        if (!string.IsNullOrEmpty(clean)) names.Add(clean);
                                    }
                                }
                            }
                        }
        
                        foreach (IniEntry e in s.Entries)
                        {
                            if (!e.IsPureComment && string.Equals(e.Key, "MAIN", StringComparison.OrdinalIgnoreCase) &&
                                string.Equals((e.Value ?? "").Trim(), "HTTP_START", StringComparison.OrdinalIgnoreCase))
                            {
                                names.Add(sName);
                            }
                        }
                    }
        
                    List<string> snapshot = names.ToList();
                    foreach (IniSection s in allSections)
                    {
                        string sName = (s.Name ?? "").Trim();
                        if (string.IsNullOrEmpty(sName) || !snapshot.Contains(sName, StringComparer.OrdinalIgnoreCase)) continue;
        
                        foreach (IniEntry e in s.Entries)
                        {
                            if (!e.IsPureComment && string.Equals(e.Key, "URIs", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Value))
                            {
                                foreach (string part in e.Value.Split(','))
                                {
                                    string clean = part.Trim();
                                    if (!string.IsNullOrEmpty(clean)) names.Add(clean);
                                }
                            }
                        }
                    }
        
                    return names;
                }

        private string ObterNomeJobRest(List<IniSection> allSections)
                {
                    foreach (IniSection s in allSections)
                    {
                        string sName = (s.Name ?? "").Trim();
                        if (string.IsNullOrEmpty(sName)) continue;
        
                        foreach (IniEntry e in s.Entries)
                        {
                            if (!e.IsPureComment && string.Equals(e.Key, "MAIN", StringComparison.OrdinalIgnoreCase) &&
                                string.Equals((e.Value ?? "").Trim(), "HTTP_START", StringComparison.OrdinalIgnoreCase))
                            {
                                return sName;
                            }
                        }
                    }
                    return "HTTPJOB";
                }

        private bool VerificarRestAtivoEmOnStart(List<IniSection> allSections, out string restJobName)
                {
                    restJobName = ObterNomeJobRest(allSections);
                    foreach (IniSection s in allSections)
                    {
                        if (s.IsCommented) continue;
                        if (!string.Equals((s.Name ?? "").Trim(), "ONSTART", StringComparison.OrdinalIgnoreCase))
                            continue;
        
                        foreach (IniEntry e in s.Entries)
                        {
                            if (e.IsPureComment || e.IsCommented) continue;
                            if (string.Equals((e.Key ?? "").Trim(), "jobs", StringComparison.OrdinalIgnoreCase))
                            {
                                string[] items = (e.Value ?? "").Split(',');
                                foreach (string item in items)
                                {
                                    string clean = item.Trim();
                                    if (string.Equals(clean, restJobName, StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(clean, "HTTPJOB", StringComparison.OrdinalIgnoreCase))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    return false;
                }

        private void AlternarEstadoRestEmOnStart(List<IniSection> allSections, bool ativar)
                {
                    string jobName = ObterNomeJobRest(allSections);
                    IniSection onStartSec = allSections.FirstOrDefault(s =>
                        string.Equals((s.Name ?? "").Trim(), "ONSTART", StringComparison.OrdinalIgnoreCase));
        
                    if (!ativar)
                    {
                        
                        if (onStartSec == null) return;
        
                        List<IniEntry> activeJobsEntries = onStartSec.Entries.Where(e =>
                            !e.IsPureComment && !e.IsCommented &&
                            string.Equals((e.Key ?? "").Trim(), "jobs", StringComparison.OrdinalIgnoreCase)).ToList();
        
                        foreach (IniEntry entry in activeJobsEntries)
                        {
                            List<string> jobsList = (entry.Value ?? "")
                                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(j => j.Trim())
                                .Where(j => !string.IsNullOrEmpty(j))
                                .ToList();
        
                            bool hasRestJob = jobsList.Any(j =>
                                string.Equals(j, jobName, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(j, "HTTPJOB", StringComparison.OrdinalIgnoreCase));
        
                            if (!hasRestJob) continue;
        
                            List<string> remaining = jobsList.Where(j =>
                                !string.Equals(j, jobName, StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(j, "HTTPJOB", StringComparison.OrdinalIgnoreCase)).ToList();
        
                            if (remaining.Count == 0)
                            {
                                
                                entry.IsCommented = true;
                            }
                            else
                            {
                                
                                entry.Value = string.Join(",", remaining);
                            }
                        }
                    }
                    else
                    {
                        
                        if (onStartSec == null)
                        {
                            onStartSec = new IniSection();
                            onStartSec.Name = "ONSTART";
                            onStartSec.IsCommented = false;
        
                            IniEntry newJobEntry = new IniEntry();
                            newJobEntry.IsPureComment = false;
                            newJobEntry.IsCommented = false;
                            newJobEntry.Key = "jobs";
                            newJobEntry.Value = jobName;
                            onStartSec.Entries.Add(newJobEntry);
        
                            IniEntry refreshEntry = new IniEntry();
                            refreshEntry.IsPureComment = false;
                            refreshEntry.IsCommented = false;
                            refreshEntry.Key = "RefreshRate";
                            refreshEntry.Value = "120";
                            onStartSec.Entries.Add(refreshEntry);
        
                            allSections.Add(onStartSec);
                            return;
                        }
        
                        if (onStartSec.IsCommented)
                            onStartSec.IsCommented = false;
        
                        IniEntry activeJobs = onStartSec.Entries.FirstOrDefault(e =>
                            !e.IsPureComment && !e.IsCommented &&
                            string.Equals((e.Key ?? "").Trim(), "jobs", StringComparison.OrdinalIgnoreCase));
        
                        List<IniEntry> commentedJobs = onStartSec.Entries.Where(e =>
                            !e.IsPureComment && e.IsCommented &&
                            string.Equals((e.Key ?? "").Trim(), "jobs", StringComparison.OrdinalIgnoreCase)).ToList();
        
                        if (activeJobs != null)
                        {
                            List<string> currentJobs = (activeJobs.Value ?? "")
                                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(j => j.Trim())
                                .Where(j => !string.IsNullOrEmpty(j))
                                .ToList();
        
                            if (!currentJobs.Any(j => string.Equals(j, jobName, StringComparison.OrdinalIgnoreCase)))
                                currentJobs.Add(jobName);
        
                            activeJobs.Value = string.Join(",", currentJobs);
        
                            
                            foreach (IniEntry cEntry in commentedJobs)
                            {
                                List<string> cList = (cEntry.Value ?? "")
                                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(j => j.Trim())
                                    .Where(j => !string.IsNullOrEmpty(j))
                                    .ToList();
                                if (cList.Count == 1 && (string.Equals(cList[0], jobName, StringComparison.OrdinalIgnoreCase) || string.Equals(cList[0], "HTTPJOB", StringComparison.OrdinalIgnoreCase)))
                                {
                                    onStartSec.Entries.Remove(cEntry);
                                }
                            }
                        }
                        else if (commentedJobs.Count > 0)
                        {
                            IniEntry target = commentedJobs[0];
                            target.IsCommented = false;
                            List<string> currentJobs = (target.Value ?? "")
                                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(j => j.Trim())
                                .Where(j => !string.IsNullOrEmpty(j))
                                .ToList();
        
                            if (!currentJobs.Any(j => string.Equals(j, jobName, StringComparison.OrdinalIgnoreCase)))
                                currentJobs.Add(jobName);
        
                            target.Value = string.Join(",", currentJobs);
                        }
                        else
                        {
                            IniEntry newJobEntry = new IniEntry();
                            newJobEntry.IsPureComment = false;
                            newJobEntry.IsCommented = false;
                            newJobEntry.Key = "jobs";
                            newJobEntry.Value = jobName;
                            onStartSec.Entries.Insert(0, newJobEntry);
                        }
                    }
                }

        private string VerificarConflitoValorIni(AmbienteInfo currentAmb, string secName, string keyName, string candidateValue)
                {
                    string val = (candidateValue ?? "").Trim();
                    string sName = (secName ?? "").Trim();
                    string kName = (keyName ?? "").Trim();
                    if (string.IsNullOrEmpty(val) || string.IsNullOrEmpty(sName) || string.IsNullOrEmpty(kName))
                        return null;
        
                    bool isWebAppPort = string.Equals(sName, "WEBAPP", StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(kName, "Port", StringComparison.OrdinalIgnoreCase);
                    bool isTcpPort = string.Equals(sName, "TCP", StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(kName, "Port", StringComparison.OrdinalIgnoreCase);
                    bool isRestPort = (string.Equals(sName, "HTTPREST", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(sName, "HTTP", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(sName, "REST", StringComparison.OrdinalIgnoreCase)) &&
                                      string.Equals(kName, "Port", StringComparison.OrdinalIgnoreCase);
                    bool isDbPort = (string.Equals(sName, "DBACCESS", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(sName, "TOPCONNECT", StringComparison.OrdinalIgnoreCase)) &&
                                    (string.Equals(kName, "Port", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(kName, "DBPort", StringComparison.OrdinalIgnoreCase));
        
                    List<ConflictRule> matchingRules = new List<ConflictRule>();
                    foreach (ConflictRule r in _conflictRules)
                    {
                        if (r.IsSelfSection) continue;
                        if (!string.Equals(r.KeyName, kName, StringComparison.OrdinalIgnoreCase)) continue;
        
                        bool isEnvPlaceholder = string.Equals(r.SectionPattern, "<ambiente>", StringComparison.OrdinalIgnoreCase);
                        if (isEnvPlaceholder && string.Equals(sName, currentAmb.NomeIni ?? "", StringComparison.OrdinalIgnoreCase))
                        {
                            matchingRules.Add(r);
                        }
                        else if (!isEnvPlaceholder && string.Equals(r.SectionPattern, sName, StringComparison.OrdinalIgnoreCase))
                        {
                            matchingRules.Add(r);
                        }
                    }
        
                    if (!isWebAppPort && !isTcpPort && !isRestPort && !isDbPort && matchingRules.Count == 0)
                        return null;
        
                    List<string> conflitos = new List<string>();
                    foreach (AmbienteInfo other in _ambientes)
                    {
                        if (other == null || other == currentAmb) continue;
                        if (string.Equals(other.AppDir, currentAmb.AppDir, StringComparison.OrdinalIgnoreCase)) continue;
                        if (_hiddenDirs.Contains(other.AppDir)) continue;
                        if (_ignoreSameSubfolderConflicts && string.Equals(ObterCaminhoSubpastaBase(other), ObterCaminhoSubpastaBase(currentAmb), StringComparison.OrdinalIgnoreCase)) continue;
        
                        bool matchOther = false;
        
                        if (isWebAppPort && string.Equals(other.WebAppPort, val, StringComparison.OrdinalIgnoreCase))
                            matchOther = true;
                        else if (isTcpPort && string.Equals(other.TcpPort, val, StringComparison.OrdinalIgnoreCase))
                            matchOther = true;
                        else if (isRestPort && string.Equals(other.RestPort, val, StringComparison.OrdinalIgnoreCase))
                            matchOther = true;
                        else if (isDbPort && string.Equals(other.DBPort, val, StringComparison.OrdinalIgnoreCase))
                            matchOther = true;
        
                        if (!matchOther && other.IniSectionsMap != null)
                        {
                            Dictionary<string, string> otherSec;
                            string otherVal;
                            if (other.IniSectionsMap.TryGetValue(sName, out otherSec) &&
                                otherSec.TryGetValue(kName, out otherVal) &&
                                string.Equals((otherVal ?? "").Trim(), val, StringComparison.OrdinalIgnoreCase))
                            {
                                matchOther = true;
                            }
        
                            if (!matchOther)
                            {
                                foreach (ConflictRule r in matchingRules)
                                {
                                    bool isEnvPlaceholder = string.Equals(r.SectionPattern, "<ambiente>", StringComparison.OrdinalIgnoreCase);
                                    string targetSec = isEnvPlaceholder ? other.NomeIni : r.SectionPattern;
                                    if (!string.IsNullOrEmpty(targetSec) &&
                                        other.IniSectionsMap.TryGetValue(targetSec, out otherSec) &&
                                        otherSec.TryGetValue(r.KeyName, out otherVal) &&
                                        string.Equals((otherVal ?? "").Trim(), val, StringComparison.OrdinalIgnoreCase))
                                    {
                                        matchOther = true;
                                        break;
                                    }
                                }
                            }
                        }
        
                        if (matchOther)
                        {
                            string label = !string.IsNullOrEmpty(other.PastaBase) && !string.Equals(other.NomeIni, other.PastaBase, StringComparison.OrdinalIgnoreCase)
                                ? string.Format("{0} ({1})", other.NomeIni, other.PastaBase)
                                : (other.NomeIni ?? other.PastaBase);
                            if (!conflitos.Contains(label, StringComparer.OrdinalIgnoreCase))
                                conflitos.Add(label);
                        }
                    }
        
                    if (conflitos.Count > 0)
                    {
                        return "⚠ Porta/valor já utilizado em: " + string.Join(", ", conflitos);
                    }
                    return null;
                }

        private string ResolverDiretorioTagIni(AmbienteInfo amb, IniSection sec, string currentVal)
                {
                    string resolvedDir = amb.AppDir;
                    try
                    {
                        string rootPathFromSec = null;
                        if (sec != null)
                        {
                            foreach (IniEntry e in sec.Entries)
                            {
                                if (!e.IsPureComment && !e.IsCommented && string.Equals(e.Key, "RootPath", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Value))
                                {
                                    rootPathFromSec = e.Value.Trim().Trim('"');
                                    break;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(rootPathFromSec))
                        {
                            rootPathFromSec = ObterValorChaveIni(amb, amb.NomeIni, "RootPath") ?? amb.AppDir;
                        }
        
                        if (!string.IsNullOrEmpty(currentVal))
                        {
                            string candidate = currentVal.Trim().Trim('"');
                            if (candidate.StartsWith("\\") && !candidate.StartsWith("\\\\"))
                            {
                                string withRoot = Path.Combine(rootPathFromSec.TrimEnd('\\', '/'), candidate.TrimStart('\\', '/'));
                                if (Directory.Exists(withRoot) || File.Exists(withRoot))
                                    candidate = withRoot;
                                else
                                    candidate = Path.Combine(amb.AppDir.TrimEnd('\\', '/'), candidate.TrimStart('\\', '/'));
                            }
                            else if (!Path.IsPathRooted(candidate))
                            {
                                candidate = Path.GetFullPath(Path.Combine(amb.AppDir, candidate.TrimStart('\\', '/')));
                            }
        
                            if (File.Exists(candidate))
                                resolvedDir = Path.GetDirectoryName(candidate);
                            else if (Directory.Exists(candidate))
                                resolvedDir = candidate;
                            else
                            {
                                string parent = Path.GetDirectoryName(candidate);
                                if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                                    resolvedDir = parent;
                                else if (Directory.Exists(rootPathFromSec))
                                    resolvedDir = rootPathFromSec;
                            }
                        }
                    }
                    catch { }
                    return resolvedDir;
                }

        private void AbrirPastaNoExplorerDireto(AmbienteInfo amb, IniSection sec, string rawVal)
                {
                    try
                    {
                        string dir = ResolverDiretorioTagIni(amb, sec, rawVal);
                        if (Directory.Exists(dir))
                            Process.Start("explorer.exe", dir);
                    }
                    catch { }
                }

        private void AbrirSeletorCaminhoPreCarregado(AmbienteInfo amb, IniSection sec, string keyName, TextBox targetBox)
                {
                    string currentVal = (targetBox.Text ?? "").Trim();
                    bool isFileKey = string.Equals(keyName, "ConsoleFile", StringComparison.OrdinalIgnoreCase);
                    string resolvedDir = ResolverDiretorioTagIni(amb, sec, currentVal);
        
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Title = string.Format("Selecionar {0}", keyName);
                    dlg.CheckPathExists = true;
                    if (Directory.Exists(resolvedDir))
                        dlg.InitialDirectory = resolvedDir;
        
                    if (isFileKey)
                    {
                        dlg.Filter = "Arquivos de Log (*.log)|*.log|Todos os arquivos (*.*)|*.*";
                        dlg.CheckFileExists = false;
                        if (!string.IsNullOrEmpty(currentVal))
                            dlg.FileName = Path.GetFileName(currentVal);
                    }
                    else
                    {
                        dlg.Filter = "Pastas|*.none";
                        dlg.FileName = "Selecionar esta pasta";
                        dlg.CheckFileExists = false;
                    }
        
                    if (dlg.ShowDialog() == true)
                    {
                        if (isFileKey)
                        {
                            targetBox.Text = dlg.FileName;
                        }
                        else
                        {
                            string chosenFolder = Path.GetDirectoryName(dlg.FileName);
                            if (!string.IsNullOrEmpty(chosenFolder))
                                targetBox.Text = chosenFolder;
                        }
                    }
                }

        private bool GravarArquivoIni(string iniPath, List<IniSection> sections, bool criarCopia)
                {
                    try
                    {
                        if (criarCopia && File.Exists(iniPath))
                        {
                            File.Copy(iniPath, iniPath + ".bak", true);
                        }
        
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < sections.Count; i++)
                        {
                            IniSection sec = sections[i];
                            if (string.IsNullOrWhiteSpace(sec.Name))
                                continue;
        
                            if (i > 0)
                                sb.AppendLine();
        
                            string secPrefix = sec.IsCommented ? ";" : "";
                            sb.AppendLine(string.Format("{0}[{1}]", secPrefix, sec.Name.Trim()));
        
                            foreach (IniEntry entry in sec.Entries)
                            {
                                if (entry.IsPureComment)
                                {
                                    if (!string.IsNullOrWhiteSpace(entry.Value))
                                        sb.AppendLine("; " + entry.Value.Trim());
                                    continue;
                                }
        
                                if (string.IsNullOrWhiteSpace(entry.Key))
                                    continue;
        
                                bool commentLine = sec.IsCommented || entry.IsCommented;
                                string linePrefix = commentLine ? ";" : "";
                                sb.AppendLine(string.Format("{0}{1}={2}", linePrefix, entry.Key.Trim(), entry.Value ?? ""));
                            }
                        }
        
                        File.WriteAllText(iniPath, sb.ToString(), Encoding.Default);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        StyledDialog.Info(this, "Erro ao Salvar", "Não foi possível gravar o arquivo .ini.", ex.Message);
                        return false;
                    }
                }
    }
}
