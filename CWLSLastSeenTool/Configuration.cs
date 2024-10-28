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

    public string CWLSCSVData { get; set; } = ""; //new storage for 7 column table with cwls name and date and presence
    public string CWLSCSVList { get; set; } = ""; //create list of known cwls when caching members
    public string CWLSCSVListDate { get; set; } = ""; //list of cache dates corresponding to CWLSCSVList
    public int CWLSListIndex { get; set; } = 0; //used for drop down cwls list memory
    public int CWLSMemberCount { get; set; } = 0; //used for showing current member count for actively showing list
    public int CWLSOnlineCount { get; set; } = 0; //used for showing current online member count for actively showing list
    public string CWLSCSVDataBACKUP { get; set; } = ""; //mostly for testing, will implement proper backup system later
    public string CWLSCSVListBACKUP { get; set; } = ""; //mostly for testing, will implement proper backup system later
    public string CWLSCSVListDateBACKUP { get; set; } = ""; //mostly for testing, will implement proper backup system later


    

    // values below this can probably be removed in 1.1.0.0

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
