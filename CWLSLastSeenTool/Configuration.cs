using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace CWLSLastSeenTool;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool UseWorldNames { get; set; } = true;

    public string DEBUGString { get; set; } = "";
    public int DEBUGInt0 { get; set; } = 0;
    public int DEBUGInt1 { get; set; } = 0;
    public int DEBUGInt2 { get; set; } = 0;
    public int DEBUGInt3 { get; set; } = 0;

    public string CWLSCSVMasterDate { get; set; } = "";
    public string CWLSCSVMaster { get; set; } = "";
    public string CWLSCSVMasterBackupDate { get; set; } = "";
    public string CWLSCSVMasterBackup { get; set; } = "";

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
