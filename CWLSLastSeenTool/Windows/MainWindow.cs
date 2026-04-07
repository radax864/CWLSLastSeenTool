using System;
using System.Collections;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Dalamud.Bindings.ImGui;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using static System.Net.Mime.MediaTypeNames;
using static FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCrossWorldLinkshell.Delegates;
using CWLSLastSeenTool.Utils;
//using System.Collections.Generic;

namespace CWLSLastSeenTool.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private ShellTools ShellTools;
    private int memberCount = 0;
    private int onlineCount = 0;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Linkshell Tools")//, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(580, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
        ShellTools = new ShellTools(plugin);
        ShellTools.ClampShellListIndex("CWLS");
        ShellTools.ClampShellListIndex("LS");
    }

    public void Dispose()
    {
        ShellTools.Dispose();
    }

    // private string GetWorldName(string rawname)
    // {
    //     string[] splitname;
    //     splitname = rawname.Split(new char[] { ' ' });
    //     string newname = splitname[0] + " " + splitname[1];

    //     if (Configuration.UseWorldNames == true)
    //     {
            
    //         uint worldid = uint.Parse(splitname[2]);
    //         string worldname = "World ID " + splitname[2] + " Not Found";

    //         if (Plugin.DataManager.GetExcelSheet<World>().HasRow(worldid))
    //         {
    //             worldname = Plugin.DataManager.GetExcelSheet<World>().GetRow(worldid).Name.ToString();
    //         }

    //         newname += " (" + worldname + ")";

    //         return newname;
    //     }

    //     else
    //     {
    //         newname += " (" + splitname[2] + ")";

    //         return newname;
    //     }
    // }

    // private string WorldIdToName(int worldid)
    // {
    //     // string[] splitname;
    //     // splitname = rawname.Split(new char[] { ' ' });
    //     // string newname = splitname[0] + " " + splitname[1];

    //     // if (Configuration.UseWorldNames == true)
    //     // {
            
    //         // uint worldid = uint.Parse(splitname[2]);
    //         string worldname = "??" + worldid.ToString() + "??";

    //         if (Plugin.DataManager.GetExcelSheet<World>().HasRow(Convert.ToUInt32(worldid)))
    //         {
    //             worldname = Plugin.DataManager.GetExcelSheet<World>().GetRow(Convert.ToUInt32(worldid)).Name.ToString();
    //         }

    //         //newname += " (" + worldname + ")";

    //         return worldname;
    //     //}

    //     // else
    //     // {
    //     //     newname += " (" + splitname[2] + ")";

    //     //     return newname;
    //     // }
    // }



    // private void CacheCWLSMembers()
    // {
    //     //Fetch CWLS Data and Create Cache Table

    //     DateTime dateToday = DateTime.Now;
    //     string selectedcwlsname = "";
    //     int cwlsloaded = 0;

    //     DataTable cachetable = new DataTable();
    //     cachetable.Columns.Add("member", typeof(string));
    //     cachetable.Columns.Add("state", typeof(string));
    //     cachetable.Columns.Add("lastseen", typeof(DateTime));
    //     cachetable.Columns.Add("seendays", typeof(int));
    //     cachetable.Columns.Add("cwls", typeof(string));
    //     cachetable.Columns.Add("cachedate", typeof(string));
    //     cachetable.Columns.Add("ispresent", typeof(int));

    //     unsafe
    //     {
    //         if (InfoProxyCrossWorldLinkshellMember.Instance() != null && AgentCrossWorldLinkshell.Instance() != null && InfoProxyCrossWorldLinkshell.Instance() != null)
    //         {
    //             uint selectedcwlsindex = AgentCrossWorldLinkshell.Instance()->SelectedCWLSIndex;
    //             selectedcwlsname = InfoProxyCrossWorldLinkshell.Instance()->GetCrossworldLinkshellName(selectedcwlsindex)->ToString();
    //             //string selectedcwlsname = InfoProxyCrossWorldLinkshell.Instance()->GetCrossworldLinkshellName(selectedcwlsindex)->ToString();

    //             foreach (var characterData in InfoProxyCrossWorldLinkshellMember.Instance()->CharDataSpan)
    //             {
    //                 DataRow row = cachetable.NewRow();
    //                 row["member"] = characterData.NameString + " " + characterData.HomeWorld;
    //                 row["state"] = characterData.State;
    //                 row["lastseen"] = dateToday;

    //                 if (characterData.State > 0)
    //                 {
    //                     row["seendays"] = 0;
    //                 }
    //                 else
    //                 {
    //                     row["seendays"] = 40000;
    //                 }
                    
    //                 row["cwls"] = selectedcwlsname;
    //                 row["cachedate"] = dateToday.ToString();
    //                 row["ispresent"] = 0;

    //                 cachetable.Rows.Add(row);
    //             }
    //             cwlsloaded = 1;
    //         }
    //     }

    //     if (cwlsloaded == 1)
    //     {
    //         //Create Master Table
    //         DataTable mastertable = new DataTable();
    //         mastertable.Columns.Add("member", typeof(string));
    //         mastertable.Columns.Add("state", typeof(string));
    //         mastertable.Columns.Add("lastseen", typeof(DateTime));
    //         mastertable.Columns.Add("seendays", typeof(int));
    //         mastertable.Columns.Add("cwls", typeof(string));
    //         mastertable.Columns.Add("cachedate", typeof(string));
    //         mastertable.Columns.Add("ispresent", typeof(int));

    //         string[] masterLines;
    //         masterLines = Plugin.Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
    //         string[] masterFields;
    //         for (int i = 1; i < masterLines.GetLength(0); i++)
    //         {
    //             masterFields = masterLines[i].Split(new char[] { ',' });
    //             DataRow Row = mastertable.NewRow();
    //             Row["member"] = masterFields[0];
    //             Row["state"] = masterFields[1];
    //             Row["lastseen"] = masterFields[2];
    //             Row["seendays"] = masterFields[3];
    //             Row["cwls"] = masterFields[4];
    //             Row["cachedate"] = masterFields[5];
    //             Row["ispresent"] = masterFields[6];
    //             mastertable.Rows.Add(Row);
    //         }

    //         //Do Compare and Update
    //         foreach (DataRow cacheRow in cachetable.Rows)
    //         {
    //             string cacheMember = cacheRow.Field<string>("member");
    //             string cacheState = cacheRow.Field<string>("state");
    //             DateTime cacheLastseen = cacheRow.Field<DateTime>("lastseen");
    //             int cacheSeendays = cacheRow.Field<int>("seendays");
    //             string cacheCWLS = cacheRow.Field<string>("cwls");
    //             string cacheCacheDate = cacheRow.Field<string>("cachedate");
    //             int foundMember = 0;

    //             foreach (DataRow masterRow in mastertable.Rows)
    //             {
    //                 string masterMember = masterRow.Field<string>("member");
    //                 DateTime masterLastseen = masterRow.Field<DateTime>("lastseen");
    //                 int masterSeendays = masterRow.Field<int>("seendays");
    //                 string masterCWLS = masterRow.Field<string>("cwls");

    //                 if (string.Equals(cacheCWLS, masterCWLS)) //should prevent members leaving cwls orphaning csv entry cachedates
    //                 {
    //                     if (string.Equals(cacheMember, masterMember))
    //                     {
    //                         foundMember++;
    //                         if (string.Equals(cacheState, "Online"))
    //                         {
    //                             masterRow["lastseen"] = dateToday;
    //                             masterRow["seendays"] = 0;
    //                         }
    //                         else if (masterSeendays != 40000)
    //                         {
    //                             masterRow["seendays"] = (dateToday.Date - masterLastseen.Date).Days; //Difference between date last seen online and today
    //                         }
    //                         masterRow["state"] = cacheState;
    //                         //masterRow["cachedate"] = cacheCacheDate;
    //                     }

    //                     masterRow["cachedate"] = cacheCacheDate;
    //                 }

    //             }

    //             if (foundMember == 0) //if member has not been found, will = 0, then write that member to master
    //             {
    //                 DataRow Row = mastertable.NewRow();
    //                 Row["member"] = cacheMember;
    //                 Row["state"] = cacheState;
    //                 Row["lastseen"] = cacheLastseen;
    //                 Row["seendays"] = cacheSeendays;
    //                 Row["cwls"] = cacheCWLS;
    //                 Row["cachedate"] = cacheCacheDate;
    //                 Row["ispresent"] = 0;
    //                 mastertable.Rows.Add(Row);
    //             }
    //         }

    //         //compare master to cached to get presence
    //         foreach (DataRow mRow in mastertable.Rows)
    //         {
    //             if (string.Equals(mRow["cwls"], selectedcwlsname))
    //             {
    //                 mRow["ispresent"] = 0;
    //                 mRow["state"] = "Not Found";

    //                 foreach (DataRow cRow in cachetable.Rows)
    //                 {
    //                     if (string.Equals(mRow["member"], cRow["member"]))
    //                     {
    //                         mRow["ispresent"] = 1;
    //                         mRow["state"] = cRow["state"];
    //                     }
    //                 }
    //             }
    //         }

    //         //Write Back Updated Master Table CSV
    //         StringBuilder sb0 = new StringBuilder();
    //         string[] columnNames0 = mastertable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
    //         sb0.AppendLine(string.Join(",", columnNames0));

    //         foreach (DataRow row in mastertable.Rows)
    //         {
    //             string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
    //             sb0.AppendLine(string.Join(",", fields));
    //         }

    //         //make CSV list of known cwls names
    //         StringBuilder sb2 = new StringBuilder();
    //         string[] cwlslist = mastertable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cwls")).Distinct().ToArray();
    //         sb2.AppendLine(string.Join(",", cwlslist));

    //         //make CSV list of cwls cache dates
    //         StringBuilder sb4 = new StringBuilder();
    //         string[] cwlslistdates = mastertable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
    //         sb4.AppendLine(string.Join(",", cwlslistdates));

    //         Plugin.Configuration.CWLSCSVData = sb0.ToString();
    //         Plugin.Configuration.CWLSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
    //         Plugin.Configuration.CWLSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
    //         Configuration.Save();
    //     }
    // }

    // private void RemoveCWLSMember(string remcwls, string remname)
    // {
    //     //this works by rebuilding the main csv while excluding the selected member
    //     DataTable remtable = new DataTable();
    //     remtable.Columns.Add("member", typeof(string));
    //     remtable.Columns.Add("state", typeof(string));
    //     remtable.Columns.Add("lastseen", typeof(DateTime));
    //     remtable.Columns.Add("seendays", typeof(int));
    //     remtable.Columns.Add("cwls", typeof(string));
    //     remtable.Columns.Add("cachedate", typeof(string));
    //     remtable.Columns.Add("ispresent", typeof(int));

    //     string[] remLines;
    //     remLines = Plugin.Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
    //     string[] remFields;

    //     for (int i = 1; i < remLines.GetLength(0); i++)
    //     {
    //         remFields = remLines[i].Split(new char[] { ',' });
    //         if (remFields[4].Equals(remcwls) && remFields[0].Equals(remname))
    //         {
    //             //throw the name you want to remove into the void
    //         }
    //         else
    //         {
    //             DataRow Row = remtable.NewRow();
    //             Row["member"] = remFields[0];
    //             Row["state"] = remFields[1];
    //             Row["lastseen"] = remFields[2];
    //             Row["seendays"] = remFields[3];
    //             Row["cwls"] = remFields[4];
    //             Row["cachedate"] = remFields[5];
    //             Row["ispresent"] = remFields[6];
    //             remtable.Rows.Add(Row);
    //         }
    //     }

    //     StringBuilder sb1 = new StringBuilder();
    //     string[] columnNames0 = remtable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
    //     sb1.AppendLine(string.Join(",", columnNames0));

    //     foreach (DataRow row in remtable.Rows)
    //     {
    //         string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
    //         sb1.AppendLine(string.Join(",", fields));
    //     }

    //     //update CSV list of known cwls names
    //     StringBuilder sb2 = new StringBuilder();
    //     string[] cwlslist = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cwls")).Distinct().ToArray(); //this removes the \r\n from the end of the string
    //     sb2.AppendLine(string.Join(",", cwlslist));

    //     //update CSV list of cwls cache dates
    //     StringBuilder sb4 = new StringBuilder();
    //     string[] cwlslistdates = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray(); //this removes the \r\n from the end of the string
    //     sb4.AppendLine(string.Join(",", cwlslistdates));

    //     Plugin.Configuration.CWLSCSVData = sb1.ToString();
    //     Plugin.Configuration.CWLSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
    //     Plugin.Configuration.CWLSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
    //     Configuration.Save();
    // }


    // private void CacheLSMembers()
    // {
    //     //Fetch LS Data and Create Cache Table

    //     DateTime dateToday = DateTime.Now;
    //     string selectedlsname = "";
    //     string lsname = "";
    //     int worldid = 0;
    //     int lsloaded = 0;

    //     DataTable cachetable = new DataTable();
    //     cachetable.Columns.Add("member", typeof(string));
    //     cachetable.Columns.Add("state", typeof(string));
    //     cachetable.Columns.Add("lastseen", typeof(DateTime));
    //     cachetable.Columns.Add("seendays", typeof(int));
    //     cachetable.Columns.Add("linkshell", typeof(string));
    //     cachetable.Columns.Add("cachedate", typeof(string));
    //     cachetable.Columns.Add("ispresent", typeof(int));
    //     cachetable.Columns.Add("worldid", typeof(int));
    //     cachetable.Columns.Add("lslist", typeof(string));

    //     unsafe
    //     {
    //         if (InfoProxyLinkshellMember.Instance() != null && AgentLinkshell.Instance() != null && InfoProxyLinkshell.Instance() != null)
    //         {
    //             uint selectedlsindex = AgentLinkshell.Instance()->SelectedLSIndex;
    //             ulong lsid = InfoProxyLinkshell.Instance()->GetLinkshellInfo(selectedlsindex) ->Id;
    //             lsname = InfoProxyLinkshell.Instance() ->GetLinkshellName(lsid).ToString();

    //             foreach (var characterData in InfoProxyLinkshellMember.Instance()->CharDataSpan)
    //             {
    //                 DataRow row = cachetable.NewRow();
    //                 //if (string.Compare(characterData.NameString, "")){row["member"] = characterData.NameString;}
    //                 row["member"] = characterData.NameString; // + " " + characterData.HomeWorld;
    //                 if (string.Equals(row["member"], "")){row["member"] = "(Unable to Retrieve)";}
    //                 row["state"] = "Offline";
    //                 if (characterData.State > 0)
    //                 {
    //                     row["state"] = "Online";
    //                 }
    //                 row["lastseen"] = dateToday;
    //                 if (characterData.State > 0)
    //                 {
    //                     row["seendays"] = 0;
    //                 }
    //                 else
    //                 {
    //                     row["seendays"] = 40000;
    //                 }
    //                 row["linkshell"] = lsname;
    //                 row["cachedate"] = dateToday.ToString();
    //                 row["ispresent"] = 0;
    //                 row["worldid"] = characterData.HomeWorld;
    //                 row["lslist"] = "";

    //                 cachetable.Rows.Add(row);
    //                 worldid = characterData.HomeWorld;
    //             }

    //             selectedlsname = lsname + " (" + WorldIdToName(worldid) + ")";
    //             lsloaded = 1;
    //         }
    //     }

    //     if (lsloaded == 1)
    //     {
    //         //Create Master Table
    //         DataTable mastertable = new DataTable();
    //         mastertable.Columns.Add("member", typeof(string));
    //         mastertable.Columns.Add("state", typeof(string));
    //         mastertable.Columns.Add("lastseen", typeof(DateTime));
    //         mastertable.Columns.Add("seendays", typeof(int));
    //         mastertable.Columns.Add("linkshell", typeof(string));
    //         mastertable.Columns.Add("cachedate", typeof(string));
    //         mastertable.Columns.Add("ispresent", typeof(int));
    //         mastertable.Columns.Add("worldid", typeof(int));
    //         mastertable.Columns.Add("lslist", typeof(string));

    //         string[] masterLines;
    //         masterLines = Plugin.Configuration.LSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
    //         string[] masterFields;
    //         for (int i = 1; i < masterLines.GetLength(0); i++)
    //         {
    //             masterFields = masterLines[i].Split(new char[] { ',' });
    //             DataRow Row = mastertable.NewRow();
    //             Row["member"] = masterFields[0];
    //             Row["state"] = masterFields[1];
    //             Row["lastseen"] = masterFields[2];
    //             Row["seendays"] = masterFields[3];
    //             Row["linkshell"] = masterFields[4];
    //             Row["cachedate"] = masterFields[5];
    //             Row["ispresent"] = masterFields[6];
    //             Row["worldid"] = masterFields[7];
    //             Row["lslist"] = masterFields[8];
    //             mastertable.Rows.Add(Row);
    //         }

    //         //Do Compare and Update
    //         foreach (DataRow cacheRow in cachetable.Rows)
    //         {
    //             string cacheMember = cacheRow.Field<string>("member");
    //             string cacheState = cacheRow.Field<string>("state");
    //             DateTime cacheLastseen = cacheRow.Field<DateTime>("lastseen");
    //             int cacheSeendays = cacheRow.Field<int>("seendays");
    //             string cacheLS = cacheRow.Field<string>("linkshell");
    //             string cacheCacheDate = cacheRow.Field<string>("cachedate");
    //             int cacheWorldId = cacheRow.Field<int>("worldid");
    //             int foundMember = 0;

    //             foreach (DataRow masterRow in mastertable.Rows)
    //             {
    //                 string masterMember = masterRow.Field<string>("member");
    //                 DateTime masterLastseen = masterRow.Field<DateTime>("lastseen");
    //                 int masterSeendays = masterRow.Field<int>("seendays");
    //                 string masterLS = masterRow.Field<string>("linkshell");
    //                 int masterWorldId = masterRow.Field<int>("worldid");

    //                 if (string.Equals(cacheLS, masterLS) && cacheWorldId == masterWorldId) //should prevent members leaving cwls orphaning csv entry cachedates
    //                 {
    //                     if (string.Equals(cacheMember, masterMember))
    //                     {
    //                         foundMember++;
    //                         if (string.Equals(cacheState, "Online"))
    //                         {
    //                             masterRow["lastseen"] = dateToday;
    //                             masterRow["seendays"] = 0;
    //                         }
    //                         else if (masterSeendays != 40000)
    //                         {
    //                             masterRow["seendays"] = (dateToday.Date - masterLastseen.Date).Days; //Difference between date last seen online and today
    //                         }
    //                         masterRow["state"] = cacheState;
    //                     }

    //                     masterRow["cachedate"] = cacheCacheDate;
    //                     masterRow["lslist"] = selectedlsname;
    //                 }

    //             }

    //             if (foundMember == 0) //if member has not been found, will = 0, then write that member to master
    //             {
    //                 DataRow Row = mastertable.NewRow();
    //                 Row["member"] = cacheMember;
    //                 Row["state"] = cacheState;
    //                 Row["lastseen"] = cacheLastseen;
    //                 Row["seendays"] = cacheSeendays;
    //                 Row["linkshell"] = cacheLS;
    //                 Row["cachedate"] = cacheCacheDate;
    //                 Row["ispresent"] = 0;
    //                 Row["worldid"] = cacheWorldId;
    //                 Row["lslist"] = selectedlsname;
    //                 mastertable.Rows.Add(Row);
    //             }
    //         }

    //         //compare master to cached to get presence
    //         foreach (DataRow mRow in mastertable.Rows)
    //         {
    //             if (string.Equals(mRow["linkshell"], lsname) && mRow["worldid"].Equals(worldid))
    //             {
    //                 mRow["ispresent"] = 0;
    //                 mRow["state"] = "Not Found";

    //                 foreach (DataRow cRow in cachetable.Rows)
    //                 {
    //                     if (string.Equals(mRow["member"], cRow["member"]))
    //                     {
    //                         mRow["ispresent"] = 1;
    //                         mRow["state"] = cRow["state"];
    //                     }
    //                 }
    //             }
    //         }

    //         //Write Back Updated Master Table CSV
    //         StringBuilder sb0 = new StringBuilder();
    //         string[] columnNames0 = mastertable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
    //         sb0.AppendLine(string.Join(",", columnNames0));

    //         foreach (DataRow row in mastertable.Rows)
    //         {
    //             string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
    //             sb0.AppendLine(string.Join(",", fields));
    //         }

    //         //make CSV list of known cwls names
    //         // List<string> selectedlslist = new List<string>();
    //         // foreach (DataRow mRow in mastertable.Rows)
    //         // {
    //         //     selectedlslist.Add(mRow["linkshell"].ToString() + " (" + WorldIdToName(Convert.ToInt32(mRow["worldid"])) + ")");
    //         // }

    //         //make CSV list of known cwls names
    //         StringBuilder sb2 = new StringBuilder();
    //         string[] lslist = mastertable.Rows.Cast<DataRow>().Select(r => r.Field<string>("lslist")).Distinct().ToArray();
    //         sb2.AppendLine(string.Join(",", lslist));

    //         // StringBuilder sb2 = new StringBuilder();
    //         // string[] lslist = selectedlslist.Distinct().ToArray();//mastertable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cwls")).Distinct().ToArray();
    //         // sb2.AppendLine(string.Join(",", lslist));

    //         //make CSV list of cwls cache dates
    //         StringBuilder sb4 = new StringBuilder();
    //         string[] lslistdates = mastertable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
    //         sb4.AppendLine(string.Join(",", lslistdates));

    //         Plugin.Configuration.LSCSVData = sb0.ToString();
    //         Plugin.Configuration.LSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
    //         Plugin.Configuration.LSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
    //         Configuration.Save();
    //     }
    // }

    // private void RemoveLSMember(string remlslist, string remlsmember)
    // {
    //     //this works by rebuilding the main csv while excluding the selected member
    //     DataTable remtable = new DataTable();
    //     remtable.Columns.Add("member", typeof(string));
    //     remtable.Columns.Add("state", typeof(string));
    //     remtable.Columns.Add("lastseen", typeof(DateTime));
    //     remtable.Columns.Add("seendays", typeof(int));
    //     remtable.Columns.Add("cwls", typeof(string));
    //     remtable.Columns.Add("cachedate", typeof(string));
    //     remtable.Columns.Add("ispresent", typeof(int));
    //     remtable.Columns.Add("worldid", typeof(int));
    //     remtable.Columns.Add("lslist", typeof(string));

    //     string[] remLines;
    //     remLines = Plugin.Configuration.LSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
    //     string[] remFields;

    //     for (int i = 1; i < remLines.GetLength(0); i++)
    //     {
    //         remFields = remLines[i].Split(new char[] { ',' });
    //         if (remFields[8].Equals(remlslist) && remFields[0].Equals(remlsmember))
    //         {
    //             //throw the name you want to remove into the void
    //         }
    //         else
    //         {
    //             DataRow Row = remtable.NewRow();
    //             Row["member"] = remFields[0];
    //             Row["state"] = remFields[1];
    //             Row["lastseen"] = remFields[2];
    //             Row["seendays"] = remFields[3];
    //             Row["cwls"] = remFields[4];
    //             Row["cachedate"] = remFields[5];
    //             Row["ispresent"] = remFields[6];
    //             Row["worldid"] = remFields[7];
    //             Row["lslist"] = remFields[8];
    //             remtable.Rows.Add(Row);
    //         }
    //     }

    //     StringBuilder sb1 = new StringBuilder();
    //     string[] columnNames0 = remtable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
    //     sb1.AppendLine(string.Join(",", columnNames0));

    //     foreach (DataRow row in remtable.Rows)
    //     {
    //         string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
    //         sb1.AppendLine(string.Join(",", fields));
    //     }

    //     //update CSV list of known cwls names
    //     StringBuilder sb2 = new StringBuilder();
    //     string[] lslist = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("lslist")).Distinct().ToArray(); //this removes the \r\n from the end of the string
    //     sb2.AppendLine(string.Join(",", lslist));

    //     //update CSV list of cwls cache dates
    //     StringBuilder sb4 = new StringBuilder();
    //     string[] lslistdates = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray(); //this removes the \r\n from the end of the string
    //     sb4.AppendLine(string.Join(",", lslistdates));

    //     Plugin.Configuration.LSCSVData = sb1.ToString();
    //     Plugin.Configuration.LSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
    //     Plugin.Configuration.LSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
    //     Configuration.Save();
    // }


    private void DrawCWLSTabContents()
    {
        //main ui
        // ImGui.Spacing();

        // if (ImGui.Button("Cache CWLS Members"))
        // {
        //     CacheCWLSMembers();
        // }

        // ImGui.SameLine();

        // if (ImGui.Button("Advanced Options"))
        // {
        //     Plugin.ToggleConfigUI();
        // }

        ImGui.Spacing();

        //-----------------------------------------------------------------------------
        //        start table display and sorting here
        //-----------------------------------------------------------------------------

        //check for empty strings that will cause things to break
        if (Configuration.CWLSCSVDataVersion == Configuration.CSVDataVersion && !Configuration.CWLSCSVList.Equals("") && !Configuration.CWLSCSVListDate.Equals("") && !Configuration.CWLSCSVData.Equals(""))
        {

            //make list of cwls names for drop down
            string[] CWLSList;
            CWLSList = Configuration.CWLSCSVList.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

            //check if list index is out of array, if it is return to 0 before drawing list
            // if (Configuration.CWLSListIndex != 0 && Configuration.CWLSListIndex >= CWLSList.Length)
            // {
            //     Configuration.CWLSListIndex = 0;
            //     Configuration.Save();
            // }

            //make list of cache dates to display next to cwls list drop down
            string[] CWLSListDate;
            CWLSListDate = Configuration.CWLSCSVListDate.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

            ImGui.SetNextItemWidth(300.0f);

            using (var cwlspicklist = ImRaii.Combo("##cwls_picklist", CWLSList[Configuration.CWLSListIndex]))
            {
                if (cwlspicklist)
                {
                    for (int i = 0; i < CWLSList.Length; i++)
                    {
                        bool isselected = Configuration.CWLSListIndex == i; //was previously bool isselected = (Configuration.CWLSListIndex == i);
                        if (ImGui.Selectable(CWLSList[i], isselected))
                        {
                            Configuration.CWLSListIndex = i;
                            Configuration.Save();
                        }
                        if (isselected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                }

            }

            ImGui.SameLine();
            if (ImGui.Button("Cache Members"))
            {
                ShellTools.CacheShell("CWLS");
            }

        


            //if (ImGui.BeginCombo("", CWLSList[Plugin.Configuration.CWLSListIndex]))

            //ImGui.EndCombo();


            //ImGui.SameLine();
            ImGui.Text($"Members: {memberCount}/64 ({onlineCount} Online)");

            ImGui.SameLine();
            ImGui.Spacing();

            ImGui.SameLine();
            ImGui.Text($"Last Cached: {CWLSListDate[Configuration.CWLSListIndex]}");

            ImGui.Spacing();

            //start making the display table
            string[] Lines;
            Lines = Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] Fields;

            Vector4 vectRed = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Vector4 vectGreen = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);

            ImGuiTableFlags flags = ImGuiTableFlags.ScrollY |
                                    ImGuiTableFlags.RowBg |
                                    ImGuiTableFlags.Sortable
                                    ;

            //ImGui.BeginTable("displaytable", 5, flags);
            using (var displaytable = ImRaii.Table("displaytable", 5, flags)) 
            {
                if (displaytable)
                {
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableSetupColumn("Member", ImGuiTableColumnFlags.None, 0.38f);
                    ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending, 0.12f);
                    ImGui.TableSetupColumn("Last Seen", ImGuiTableColumnFlags.None, 0.18f);
                    ImGui.TableSetupColumn("Days Since", ImGuiTableColumnFlags.None, 0.17f);
                    ImGui.TableSetupColumn("Hold CTRL", ImGuiTableColumnFlags.NoSort, 0.15f);
                    ImGui.TableHeadersRow();

                    ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();

                    Array.Sort(Lines, (line1, line2) =>
                    {
                        // Don't sort Lines[0] - it's the table headers
                        if (line1 == Lines[0]) return -1;
                        if (line2 == Lines[0]) return 1;

                        string[] fields1 = line1.Split(new char[] { '\t' });
                        string[] fields2 = line2.Split(new char[] { '\t' });

                        short index = sortSpecs.Specs.ColumnIndex; // this is the column that we're sorting by
                        int comparison = 0;

                        switch (index)
                        {
                            case 0: // Names
                                comparison = string.Compare(fields1[0], fields2[0]);
                                break;
                            case 1: // Online/Offline
                                comparison = string.Compare(fields1[2], fields2[2]);
                                break;
                            case 2: // Last Seen Date
                                var time1 = DateTime.Parse(fields1[3]);
                                var time2 = DateTime.Parse(fields2[3]);

                                comparison = time1.CompareTo(time2);
                                break;
                            case 3: // days since seen
                                comparison = int.Parse(fields1[4]) - int.Parse(fields2[4]);
                                break;
                        }

                        if (comparison == 0 && index != 0)
                        {
                            // If the lines have the same value in this column, sort by name as a second layer.
                            // Always sort ascending for the secondary name sort
                            return string.Compare(fields1[0], fields2[0]);
                        }

                        if (comparison != 0)
                        {
                            // Check sort direction here and return the inverse if descending
                            return sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending ? -comparison : comparison;
                        }
                        return 0;

                    });

                    memberCount = 0;
                    onlineCount = 0;

                    for (int i = 1; i < Lines.GetLength(0); i++)
                    {
                        Fields = Lines[i].Split(new char[] { '\t' }); //0 = member, 1 = homeworld, 2 = state, 3 = lastseen, 4 = seendays, 5 = listname, 6 = cachedate, 7 = ispresent

                        if (Fields[5].Equals(CWLSList[Configuration.CWLSListIndex]))
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            if (int.Parse(Fields[7]) == 1)
                            {
                                ImGui.Text($"{Fields[0]} ({Fields[1]})");
                                memberCount++;
                            } //member
                            else
                            {
                                ImGui.TextColored(vectRed, $"{Fields[0]} ({Fields[1]})");
                            }
                            // if (int.Parse(Fields[6]) == 1) { ImGui.Text($"{ShellTools.GetWorldName(Fields[0])}"); Plugin.Configuration.CWLSMemberCount++; } //member
                            // else { ImGui.TextColored(vectRed, $"{ShellTools.GetWorldName(Fields[0])}"); }
                            ImGui.TableNextColumn();
                            if (string.Equals(Fields[2], "Online"))
                            {
                                ImGui.TextColored(vectGreen, $"{Fields[2]}");
                                onlineCount++;
                            } //state
                            else
                            {
                                ImGui.TextColored(vectRed, $"{Fields[2]}");
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text($"{Fields[3].Substring(0, 10)}"); //lastseen
                            ImGui.TableNextColumn();
                            if (int.Parse(Fields[4]) == 40000)
                            {
                                ImGui.Text("Never Seen");
                            }
                            else
                            {
                                ImGui.Text($"{Fields[4]}");
                            } //seendays
                            ImGui.TableNextColumn();
                            if (ImGui.SmallButton($"Remove##{i}") && ImGui.GetIO().KeyCtrl)
                            {
                                ShellTools.RemoveShell("Member", "CWLS", Fields[5], Fields[0], Fields[1]);
                            }
                        }
                    }

                }
            }
            //ImGui.EndTable();
        }
        else if (Configuration.CWLSCSVDataVersion == 0 && !Configuration.CWLSCSVList.Equals("") && !Configuration.CWLSCSVListDate.Equals("") && !Configuration.CWLSCSVData.Equals(""))
        {
            ImGui.Spacing();

            ImGui.TextWrapped("CWLSLastSeenTool has been updated to Linkshell Tools. Large changes have been made to make this easier to maintain and expand on. This includes the structure of the CSV data stored.");
            ImGui.Spacing();
            ImGui.TextWrapped("Please click the below button to update the existing Cross-world Linkshell data from version 0 to version 1. This will not change the stored data under the internal backup.");
            ImGui.Spacing();
            ImGui.TextWrapped("Please note that CWLSLastSeenTool did not record Datacenter info for Cross-world Linkshells. Existing data will be assigned to the Datacenter you are currently on. Please use the Merge/Update CWLS List option to reassign these to the correct Datacenter once updated.");
            ImGui.Spacing();

            if (ImGui.Button("Update CWLS Data"))
            {
                ShellTools.TEMPUpdateCWLSData();
            }
        }
        else
        {
            ImGui.TextWrapped("No CWLS Data. Please open up a Cross-world Linkshell and click the Cache CWLS Members button in this window.");

            if (ImGui.Button("Cache Members"))
            {
                ShellTools.CacheShell("CWLS");
            }

        }
    }

    private void DrawLSTabContents()
    {
        ImGui.Spacing();

        //-----------------------------------------------------------------------------
        //        make picklist and count info
        //-----------------------------------------------------------------------------

        //check for empty strings that will cause things to break
        if (Configuration.LSCSVDataVersion == Configuration.CSVDataVersion && !Configuration.LSCSVList.Equals("") && !Configuration.LSCSVListDate.Equals("") && !Configuration.LSCSVData.Equals(""))
        {

            //make list of ls names for drop down
            string[] LSList = Configuration.LSCSVList.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

            //check if list index is out of array, if it is return to 0 before drawing list
            // if (Configuration.LSListIndex != 0 && Configuration.LSListIndex >= LSList.Length)
            // {
            //     Configuration.LSListIndex = 0;
            //     Configuration.Save();
            // }

            //make list of cache dates to display next to cwls list drop down
            string[] LSListDate = Configuration.LSCSVListDate.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

            ImGui.SetNextItemWidth(300.0f);

            using (var lspicklist = ImRaii.Combo("##ls_picklist", LSList[Configuration.LSListIndex]))
            {
                if (lspicklist)
                {
                    for (int i = 0; i < LSList.Length; i++)
                    {
                        bool isselected = Configuration.LSListIndex == i; // was bool isselected = (Configuration.LSListIndex == i);
                        if (ImGui.Selectable(LSList[i], isselected))
                        {
                            Configuration.LSListIndex = i;
                            Configuration.Save();
                        }
                        if (isselected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                }

            }

            ImGui.SameLine();
            if (ImGui.Button("Cache Members"))
            {
                ShellTools.CacheShell("LS");
            }

        


            //if (ImGui.BeginCombo("", CWLSList[Plugin.Configuration.CWLSListIndex]))

            //ImGui.EndCombo();


            //ImGui.SameLine();
            // ImGui.Text($"Members: {Plugin.Configuration.LSMemberCount}/128 ({Plugin.Configuration.LSOnlineCount} Online)");
            ImGui.Text($"Members: {memberCount}/128 ({onlineCount} Online)");

            ImGui.SameLine();
            ImGui.Spacing();

            ImGui.SameLine();
            ImGui.Text($"Last Cached: {LSListDate[Configuration.LSListIndex]}");

            ImGui.Spacing();

        //-----------------------------------------------------------------------------
        //        start table display and sorting here
        //-----------------------------------------------------------------------------



            //start making the display table
            string[] Lines;
            Lines = Configuration.LSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] Fields;

            Vector4 vectRed = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Vector4 vectGreen = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);

            ImGuiTableFlags flags = ImGuiTableFlags.ScrollY |
                                    ImGuiTableFlags.RowBg |
                                    ImGuiTableFlags.Sortable
                                    ;

            //ImGui.BeginTable("displaytable", 5, flags);
            using (var displaytable = ImRaii.Table("displaytable", 5, flags)) 
            {
                if (displaytable)
                {
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableSetupColumn("Member", ImGuiTableColumnFlags.None, 0.38f);
                    ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending, 0.12f);
                    ImGui.TableSetupColumn("Last Seen", ImGuiTableColumnFlags.None, 0.18f);
                    ImGui.TableSetupColumn("Days Since", ImGuiTableColumnFlags.None, 0.17f);
                    ImGui.TableSetupColumn("Hold CTRL", ImGuiTableColumnFlags.NoSort, 0.15f);
                    ImGui.TableHeadersRow();

                    ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();

                    Array.Sort(Lines, (line1, line2) =>
                    {
                        // Don't sort Lines[0] - it's the table headers
                        if (line1 == Lines[0]) return -1;
                        if (line2 == Lines[0]) return 1;

                        string[] fields1 = line1.Split(new char[] { '\t' });
                        string[] fields2 = line2.Split(new char[] { '\t' });

                        short index = sortSpecs.Specs.ColumnIndex; // this is the column that we're sorting by
                        int comparison = 0;

                        switch (index)
                        {
                            case 0: // Names
                                comparison = string.Compare(fields1[0], fields2[0]);
                                break;
                            case 1: // Online/Offline
                                comparison = string.Compare(fields1[2], fields2[2]);
                                break;
                            case 2: // Last Seen Date
                                var time1 = DateTime.Parse(fields1[3]);
                                var time2 = DateTime.Parse(fields2[3]);

                                comparison = time1.CompareTo(time2);
                                break;
                            case 3: // days since seen
                                comparison = int.Parse(fields1[4]) - int.Parse(fields2[4]);
                                break;
                        }

                        if (comparison == 0 && index != 0)
                        {
                            // If the lines have the same value in this column, sort by name as a second layer.
                            // Always sort ascending for the secondary name sort
                            return string.Compare(fields1[0], fields2[0]);
                        }

                        if (comparison != 0)
                        {
                            // Check sort direction here and return the inverse if descending
                            return sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending ? -comparison : comparison;
                        }
                        return 0;

                    });

                    memberCount = 0;
                    onlineCount = 0;

                    for (int i = 1; i < Lines.GetLength(0); i++)
                    {
                        Fields = Lines[i].Split(new char[] { '\t' }); //0 = member, 1 = homeworld, 2 = state, 3 = lastseen, 4 = seendays, 5 = listname, 6 = cachedate, 7 = ispresent

                        if (Fields[5].Equals(LSList[Configuration.LSListIndex]))
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            if (int.Parse(Fields[7]) == 1)
                            {
                                ImGui.Text($"{Fields[0]}");
                                memberCount++;
                            } //member
                            else
                            {
                                ImGui.TextColored(vectRed, $"{Fields[0]}");
                            }
                            // if (int.Parse(Fields[6]) == 1) { ImGui.Text($"{ShellTools.GetWorldName(Fields[0])}"); Plugin.Configuration.CWLSMemberCount++; } //member
                            // else { ImGui.TextColored(vectRed, $"{ShellTools.GetWorldName(Fields[0])}"); }
                            ImGui.TableNextColumn();
                            if (string.Equals(Fields[2], "Online"))
                            {
                                ImGui.TextColored(vectGreen, $"{Fields[2]}");
                                onlineCount++;
                            } //state
                            else
                            {
                                ImGui.TextColored(vectRed, $"{Fields[2]}");
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text($"{Fields[3].Substring(0, 10)}"); //lastseen
                            ImGui.TableNextColumn();
                            if (int.Parse(Fields[4]) == 40000)
                            {
                                ImGui.Text("Never Seen");
                            }
                            else
                            {
                                ImGui.Text($"{Fields[4]}");
                            } //seendays
                            ImGui.TableNextColumn();
                            if (ImGui.SmallButton($"Remove##{i}") && ImGui.GetIO().KeyCtrl)
                            {
                                ShellTools.RemoveShell("Member", "LS", Fields[5], Fields[0], Fields[1]);
                            }
                        }
                    }

                }
            }
            //ImGui.EndTable();
        }
        else if (Configuration.LSCSVDataVersion == 0 && !Configuration.LSCSVList.Equals("") && !Configuration.LSCSVListDate.Equals("") && !Configuration.LSCSVData.Equals(""))
        {
            ImGui.Spacing();

            ImGui.TextWrapped("CWLSLastSeenTool has been updated to Linkshell Tools. Large changes have been made to make this easier to maintain and expand on. This includes the structure of the CSV data stored.");
            ImGui.Spacing();
            ImGui.TextWrapped("Please click the below button to update the existing Home World Linkshell data from version 0 to version 1. This will not change the stored data under the internal backup.");
            ImGui.Spacing();

            if (ImGui.Button("Update Linkshell Data"))
            {
                ShellTools.TEMPUpdateLSData();
            }
        }
        else
        {
            ImGui.Spacing();

            ImGui.TextWrapped("No Linkshell Data. Please open up a Linkshell and click the Cache Linkshell Members button in this window.");

            if (Plugin.PlayerState.CurrentWorld.Value.DataCenter.Value.Name.ToString() != null)
            {
                ImGui.Text($"{Plugin.PlayerState.CurrentWorld.Value.DataCenter.Value.Name.ToString()}");
            }

            if (ImGui.Button("Cache Members"))
            {
                ShellTools.CacheShell("LS");
            }

        }
    }

    public override void Draw()
    {
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.SameLine();
        if (Plugin.PlayerState.CurrentWorld.IsValid)
        {
            ImGui.Text($"Current World (DC): {Plugin.PlayerState.CurrentWorld.Value.Name.ToString()} ({Plugin.PlayerState.CurrentWorld.Value.DataCenter.Value.Name.ToString()})");
        }
        else
        {
            ImGui.Text("Current World (DC): Unavailable");
        }
        
        if (Configuration.EnableDEBUGInfo)
        {
            ImGui.SameLine();
            ImGui.Text($" - {Configuration.DEBUGString}");
        }

        ImGui.SameLine(ImGui.GetWindowWidth()-140);

        if (ImGui.Button("Advanced Options"))
        {
            Plugin.ToggleConfigUI();
        }

        using (var MainTabBar = ImRaii.TabBar("main window tab bar"))
        {
            if(ImGui.BeginTabItem("Cross-world Linkshells"))
            {
                DrawCWLSTabContents();
                ImGui.EndTabItem();
            }
            if(ImGui.BeginTabItem("Home World Linkshells"))
            {
                DrawLSTabContents();
                ImGui.EndTabItem();
            }
        }
    }
}
