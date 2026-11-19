using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace TOTVSHub
{
    public enum HubActiveTab
    {
        Ambientes = 0,
        DbAccess = 1,
        SmartView = 2
    }

    public class DbAccessConfig
    {
        public string FolderPath { get; set; }
        public string IniPath { get; set; }
        public string MonitorIniPath { get; set; }
        public string LicenseServer { get; set; }
        public string LicensePort { get; set; }
        public string MonitorServer { get; set; }
        public int MonitorPort { get; set; }
        public bool IsRunning { get; set; }
        public bool IsPortOpen { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public double CpuPercent { get; set; }
        public long RamMb { get; set; }
        public TimeSpan LastCpuTime { get; set; }
        public DateTime LastCpuSampleTime { get; set; }
        public string ServiceName { get; set; }
        public bool IsService { get; set; }
        public long LatencyMs { get; set; }
        public string StatusSummary { get; set; }
        public List<DbAccessEnvironment> Environments { get; set; }
        public List<IniSection> RawSections { get; set; }

        public DbAccessConfig()
        {
            FolderPath = "";
            IniPath = "";
            MonitorIniPath = "";
            LicenseServer = "127.0.0.1";
            LicensePort = "5555";
            MonitorServer = "localhost";
            MonitorPort = 7890;
            StatusSummary = "Não inicializado";
            Environments = new List<DbAccessEnvironment>();
            RawSections = new List<IniSection>();
        }
    }

    public class DbAccessEnvironment
    {
        public string Dbms { get; set; }
        public string Name { get; set; }
        public string SectionName { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string TableSpace { get; set; }
        public string IndexSpace { get; set; }
        public bool IsDisabled { get; set; }
        public string Server { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string Alias { get; set; }
        public Dictionary<string, string> AdditionalKeys { get; set; }

        public Border CardBorder { get; set; }
        public Border StatusBadge { get; set; }
        public TextBlock StatusText { get; set; }

        public DbAccessEnvironment()
        {
            Dbms = "MSSQL";
            Name = "";
            SectionName = "";
            User = "";
            Password = "";
            TableSpace = "";
            IndexSpace = "";
            IsDisabled = false;
            Server = "";
            Port = "";
            Database = "";
            Alias = "";
            AdditionalKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class SmartViewPortStatus
    {
        public int Port { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string Host { get; set; }
        public bool IsOpen { get; set; }
        public long LatencyMs { get; set; }
        public bool HttpSuccess { get; set; }
        public int HttpStatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime LastChecked { get; set; }

        public SmartViewPortStatus(int port, string label, string description)
        {
            Port = port;
            Label = label;
            Description = description;
            Host = "localhost";
            IsOpen = false;
            LatencyMs = -1;
            HttpSuccess = false;
            HttpStatusCode = 0;
            ErrorMessage = "";
            LastChecked = DateTime.MinValue;
        }
    }

    public class SmartViewDiagnosticResult
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool DnsResolved { get; set; }
        public string ResolvedIp { get; set; }
        public bool TcpSuccess { get; set; }
        public long LatencyMs { get; set; }
        public bool HttpSuccess { get; set; }
        public int HttpStatusCode { get; set; }
        public string HttpStatusDescription { get; set; }
        public string ErrorMessage { get; set; }
        public string Conclusion { get; set; }
        public List<string> LogEntries { get; set; }
        public DateTime Timestamp { get; set; }

        public SmartViewDiagnosticResult()
        {
            Host = "localhost";
            Port = 7017;
            DnsResolved = false;
            ResolvedIp = "";
            TcpSuccess = false;
            LatencyMs = -1;
            HttpSuccess = false;
            HttpStatusCode = 0;
            HttpStatusDescription = "";
            ErrorMessage = "";
            Conclusion = "";
            LogEntries = new List<string>();
            Timestamp = DateTime.Now;
        }
    }
}
