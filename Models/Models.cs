using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace TOTVSHub
{
    public enum EnvRunState
    {
        Stopped,
        Initializing,
        Active,
        Stopping
    }

    public class ConflictRule
    {
        public string SectionPattern { get; set; }
        public string KeyName { get; set; }

        public bool IsSelfSection
        {
            get { return string.IsNullOrWhiteSpace(KeyName); }
        }

        public ConflictRule(string sectionPattern, string keyName)
        {
            SectionPattern = (sectionPattern ?? "").Trim();
            KeyName = (keyName ?? "").Trim();
        }
    }

    public class CustomLinkButton
    {
        public string Label { get; set; }
        public string Formula { get; set; }
        public bool IsBuiltIn { get; set; }

        public CustomLinkButton(string label, string formula, bool isBuiltIn)
        {
            Label = (label ?? "").Trim();
            Formula = (formula ?? "").Trim();
            IsBuiltIn = isBuiltIn;
        }
    }

    public class AmbienteInfo
    {
        public string NomeIni { get; set; }
        public string PastaBase { get; set; }
        public string AppDir { get; set; }
        public string ExePath { get; set; }
        public string IniPath { get; set; }
        public string WebAppPort { get; set; }
        public string TcpPort { get; set; }
        public string RestPort { get; set; }
        public string SpecialKey { get; set; }
        public string DBAlias { get; set; }
        public string DBPort { get; set; }

        public Dictionary<string, Dictionary<string, string>> IniSectionsMap { get; set; }

        public bool IsServiceRegistered { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDisplayName { get; set; }
        public bool RunningAsService { get; set; }

        public EnvRunState State { get; set; }
        public int Pid { get; set; }
        public string ProcessUser { get; set; }

        public Border CardBorder { get; set; }
        public Border StatusBadge { get; set; }
        public TextBlock StatusText { get; set; }
        public Border StatusDot { get; set; }
        public TextBlock ModeTagText { get; set; }
        public Border UserTagBorder { get; set; }
        public TextBlock UserTagText { get; set; }
        public Button BtnStart { get; set; }
        public Button BtnRestart { get; set; }
        public Button BtnStop { get; set; }
        public Button BtnWeb { get; set; }
        public Button BtnRest { get; set; }
        public List<Button> CustomButtons { get; set; }

        public AmbienteInfo()
        {
            State = EnvRunState.Stopped;
            IniSectionsMap = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            CustomButtons = new List<Button>();
        }
    }

    public class AlertaConflito
    {
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public List<AmbienteInfo> Envolvidos { get; set; }

        public AlertaConflito()
        {
            Envolvidos = new List<AmbienteInfo>();
        }
    }

    public class IniEntry
    {
        public bool IsPureComment { get; set; }
        public bool IsCommented { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public Border RowContainer { get; set; }
        public TextBlock CommentPrefixLabel { get; set; }
        public TextBox KeyBox { get; set; }
        public TextBox ValueBox { get; set; }
        public Button BtnToggleComment { get; set; }
    }

    public class IniSection
    {
        public string Name { get; set; }
        public bool IsCommented { get; set; }
        public List<IniEntry> Entries { get; set; }

        public Border SectionCard { get; set; }
        public Border HeaderBar { get; set; }
        public TextBlock HeaderCommentPrefix { get; set; }
        public TextBox SectionNameBox { get; set; }
        public Border HiddenBadge { get; set; }
        public Button BtnToggleSection { get; set; }
        public StackPanel EntriesPanel { get; set; }

        public IniSection()
        {
            Entries = new List<IniEntry>();
        }
    }
}
