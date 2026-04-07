using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace CWLSLastSeenTool;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // Debug and testing stuff
    public bool EnableDEBUGInfo { get; set; } = false;
    public string DEBUGString { get; set; } = "";
    public int DEBUGInt0 { get; set; } = 0;
    public int DEBUGInt1 { get; set; } = 0;
    public int DEBUGInt2 { get; set; } = 0;
    public int DEBUGInt3 { get; set; } = 0;

    //CSV Data Layout Version
    public int CSVDataVersion { get; set; } = 0;

    //CWLS Data Storage
    public int CWLSCSVDataVersion { get; set; } = 0;
    public string CWLSCSVData { get; set; } = ""; //new storage for 7 column table with cwls name and date and presence
    public string CWLSCSVList { get; set; } = ""; //create list of known cwls when caching members
    public string CWLSCSVListDate { get; set; } = ""; //list of cache dates corresponding to CWLSCSVList
    public int CWLSListIndex { get; set; } = 0; //used for drop down cwls list memory
    // public int CWLSMemberCount { get; set; } = 0; //used for showing current member count for actively showing list
    // public int CWLSOnlineCount { get; set; } = 0; //used for showing current online member count for actively showing list

    //CWLS Backup Storage - Simple internal backup copy, should replace with something more robust
    public int CWLSCSVDataVersionBACKUP { get; set; } = 0;
    public string CWLSCSVDataBACKUP { get; set; } = "";
    public string CWLSCSVListBACKUP { get; set; } = "";
    public string CWLSCSVListDateBACKUP { get; set; } = "";

    //LS Storage
    public int LSCSVDataVersion { get; set; } = 0;
    public string LSCSVData { get; set; } = ""; //new storage for 7 column table with cwls name and date and presence
    public string LSCSVList { get; set; } = ""; //create list of known cwls when caching members
    public string LSCSVListDate { get; set; } = ""; //list of cache dates corresponding to CWLSCSVList
    public int LSListIndex { get; set; } = 0; //used for drop down cwls list memory
    // public int LSMemberCount { get; set; } = 0; //used for showing current member count for actively showing list
    // public int LSOnlineCount { get; set; } = 0; //used for showing current online member count for actively showing list

    //LS Backup Storage - Simple internal backup copy, should replace with something more robust
    public int LSCSVDataVersionBACKUP { get; set; } = 0;
    public string LSCSVDataBACKUP { get; set; } = "";
    public string LSCSVListBACKUP { get; set; } = "";
    public string LSCSVListDateBACKUP { get; set; } = "";

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
