using Dalamud.Configuration;
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
    public int CSVDataVersion { get; set; } = 0; //version number for csv/tsv column layout, set/update this at plugin init in plugin.cs constructor

    //CWLS Data Storage
    public int CWLSCSVDataVersion { get; set; } = 0;
    public string CWLSCSVData { get; set; } = "";
    public string CWLSCSVList { get; set; } = ""; //create list of known cwls when caching members
    public string CWLSCSVListDate { get; set; } = ""; //list of cache dates corresponding to CWLSCSVList
    public int CWLSListIndex { get; set; } = 0; //used for drop down cwls list memory

    //CWLS Backup Storage - Simple internal backup copy, should replace with something more robust
    public int CWLSCSVDataVersionBACKUP { get; set; } = 0;
    public string CWLSCSVDataBACKUP { get; set; } = "";
    public string CWLSCSVListBACKUP { get; set; } = "";
    public string CWLSCSVListDateBACKUP { get; set; } = "";

    //LS Storage
    public int LSCSVDataVersion { get; set; } = 0;
    public string LSCSVData { get; set; } = "";
    public string LSCSVList { get; set; } = ""; //create list of known ls when caching members
    public string LSCSVListDate { get; set; } = ""; //list of cache dates corresponding to LSCSVList
    public int LSListIndex { get; set; } = 0; //used for drop down ls list memory

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
