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

namespace CWLSLastSeenTool.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Cross-world Linkshell Last Seen Tool")//, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(580, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;

    }

    public void Dispose() { }

    private string GetWorldName(string rawname)
    {
        string[] splitname;
        splitname = rawname.Split(new char[] { ' ' });
        string newname = splitname[0] + " " + splitname[1];

        if (Configuration.UseWorldNames == true)
        {
            
            uint worldid = uint.Parse(splitname[2]);
            string worldname = "World ID " + splitname[2] + " Not Found";

            if (Plugin.DataManager.GetExcelSheet<World>().HasRow(worldid))
            {
                worldname = Plugin.DataManager.GetExcelSheet<World>().GetRow(worldid).Name.ToString();
            }

            newname += " (" + worldname + ")";

            return newname;
        }

        else
        {
            newname += " (" + splitname[2] + ")";

            return newname;
        }
    }

    private void CacheCWLSMembers()
    {
        //Fetch CWLS Data and Create Cache Table

        DateTime dateToday = DateTime.Now;
        string selectedcwlsname = "";

        DataTable cachetable = new DataTable();
        cachetable.Columns.Add("member", typeof(string));
        cachetable.Columns.Add("state", typeof(string));
        cachetable.Columns.Add("lastseen", typeof(DateTime));
        cachetable.Columns.Add("seendays", typeof(int));
        cachetable.Columns.Add("cwls", typeof(string));
        cachetable.Columns.Add("cachedate", typeof(string));
        cachetable.Columns.Add("ispresent", typeof(int));

        unsafe
        {
            if (InfoProxyCrossWorldLinkshellMember.Instance() != null && AgentCrossWorldLinkshell.Instance() != null && InfoProxyCrossWorldLinkshell.Instance() != null)
            {
                uint selectedcwlsindex = AgentCrossWorldLinkshell.Instance()->SelectedCWLSIndex;
                selectedcwlsname = InfoProxyCrossWorldLinkshell.Instance()->GetCrossworldLinkshellName(selectedcwlsindex)->ToString();
                //string selectedcwlsname = InfoProxyCrossWorldLinkshell.Instance()->GetCrossworldLinkshellName(selectedcwlsindex)->ToString();

                foreach (var characterData in InfoProxyCrossWorldLinkshellMember.Instance()->CharDataSpan)
                {
                    DataRow row = cachetable.NewRow();
                    row["member"] = characterData.NameString + " " + characterData.HomeWorld;
                    row["state"] = characterData.State;
                    row["lastseen"] = dateToday;

                    if (characterData.State > 0)
                    {
                        row["seendays"] = 0;
                    }
                    else
                    {
                        row["seendays"] = 40000;
                    }
                    
                    row["cwls"] = selectedcwlsname;
                    row["cachedate"] = dateToday.ToString();
                    row["ispresent"] = 0;

                    cachetable.Rows.Add(row);
                }
            }
        }

        //Create Master Table
        DataTable mastertable = new DataTable();
        mastertable.Columns.Add("member", typeof(string));
        mastertable.Columns.Add("state", typeof(string));
        mastertable.Columns.Add("lastseen", typeof(DateTime));
        mastertable.Columns.Add("seendays", typeof(int));
        mastertable.Columns.Add("cwls", typeof(string));
        mastertable.Columns.Add("cachedate", typeof(string));
        mastertable.Columns.Add("ispresent", typeof(int));

        string[] masterLines;
        masterLines = Plugin.Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        string[] masterFields;
        for (int i = 1; i < masterLines.GetLength(0); i++)
        {
            masterFields = masterLines[i].Split(new char[] { ',' });
            DataRow Row = mastertable.NewRow();
            Row["member"] = masterFields[0];
            Row["state"] = masterFields[1];
            Row["lastseen"] = masterFields[2];
            Row["seendays"] = masterFields[3];
            Row["cwls"] = masterFields[4];
            Row["cachedate"] = masterFields[5];
            Row["ispresent"] = masterFields[6];
            mastertable.Rows.Add(Row);
        }

        //Do Compare and Update
        foreach (DataRow cacheRow in cachetable.Rows)
        {
            string cacheMember = cacheRow.Field<string>("member");
            string cacheState = cacheRow.Field<string>("state");
            DateTime cacheLastseen = cacheRow.Field<DateTime>("lastseen");
            int cacheSeendays = cacheRow.Field<int>("seendays");
            string cacheCWLS = cacheRow.Field<string>("cwls");
            string cacheCacheDate = cacheRow.Field<string>("cachedate");
            int foundMember = 0;

            foreach (DataRow masterRow in mastertable.Rows)
            {
                string masterMember = masterRow.Field<string>("member");
                DateTime masterLastseen = masterRow.Field<DateTime>("lastseen");
                int masterSeendays = masterRow.Field<int>("seendays");
                string masterCWLS = masterRow.Field<string>("cwls");

                if (string.Equals(cacheCWLS, masterCWLS)) //should prevent members leaving cwls orphaning csv entry cachedates
                {
                    if (string.Equals(cacheMember, masterMember))
                    {
                        foundMember++;
                        if (string.Equals(cacheState, "Online"))
                        {
                            masterRow["lastseen"] = dateToday;
                            masterRow["seendays"] = 0;
                        }
                        else if (masterSeendays != 40000)
                        {
                            masterRow["seendays"] = (dateToday.Date - masterLastseen.Date).Days; //Difference between date last seen online and today
                        }
                        masterRow["state"] = cacheState;
                        //masterRow["cachedate"] = cacheCacheDate;
                    }

                    masterRow["cachedate"] = cacheCacheDate;
                }

            }

            if (foundMember == 0) //if member has not been found, will = 0, then write that member to master
            {
                DataRow Row = mastertable.NewRow();
                Row["member"] = cacheMember;
                Row["state"] = cacheState;
                Row["lastseen"] = cacheLastseen;
                Row["seendays"] = cacheSeendays;
                Row["cwls"] = cacheCWLS;
                Row["cachedate"] = cacheCacheDate;
                Row["ispresent"] = 0;
                mastertable.Rows.Add(Row);
            }
        }

        //compare master to cached to get presence
        foreach (DataRow mRow in mastertable.Rows)
        {
            if (string.Equals(mRow["cwls"], selectedcwlsname))
            {
                mRow["ispresent"] = 0;
                mRow["state"] = "Not Found";

                foreach (DataRow cRow in cachetable.Rows)
                {
                    if (string.Equals(mRow["member"], cRow["member"]))
                    {
                        mRow["ispresent"] = 1;
                        mRow["state"] = cRow["state"];
                    }
                }
            }
        }

        //Write Back Updated Master Table CSV
        StringBuilder sb0 = new StringBuilder();
        string[] columnNames0 = mastertable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        sb0.AppendLine(string.Join(",", columnNames0));

        foreach (DataRow row in mastertable.Rows)
        {
            string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
            sb0.AppendLine(string.Join(",", fields));
        }

        //make CSV list of known cwls names
        StringBuilder sb2 = new StringBuilder();
        string[] cwlslist = mastertable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cwls")).Distinct().ToArray();
        sb2.AppendLine(string.Join(",", cwlslist));

        //make CSV list of cwls cache dates
        StringBuilder sb4 = new StringBuilder();
        string[] cwlslistdates = mastertable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
        sb4.AppendLine(string.Join(",", cwlslistdates));

        Plugin.Configuration.CWLSCSVData = sb0.ToString();
        Plugin.Configuration.CWLSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
        Plugin.Configuration.CWLSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
        Configuration.Save();
    }

    private void RemoveCWLSMember(string remcwls, string remname)
    {
        //this works by rebuilding the main csv while excluding the selected member
        DataTable remtable = new DataTable();
        remtable.Columns.Add("member", typeof(string));
        remtable.Columns.Add("state", typeof(string));
        remtable.Columns.Add("lastseen", typeof(DateTime));
        remtable.Columns.Add("seendays", typeof(int));
        remtable.Columns.Add("cwls", typeof(string));
        remtable.Columns.Add("cachedate", typeof(string));
        remtable.Columns.Add("ispresent", typeof(int));

        string[] remLines;
        remLines = Plugin.Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        string[] remFields;

        for (int i = 1; i < remLines.GetLength(0); i++)
        {
            remFields = remLines[i].Split(new char[] { ',' });
            if (remFields[4].Equals(remcwls) && remFields[0].Equals(remname))
            {
                //throw the name you want to remove into the void
            }
            else
            {
                DataRow Row = remtable.NewRow();
                Row["member"] = remFields[0];
                Row["state"] = remFields[1];
                Row["lastseen"] = remFields[2];
                Row["seendays"] = remFields[3];
                Row["cwls"] = remFields[4];
                Row["cachedate"] = remFields[5];
                Row["ispresent"] = remFields[6];
                remtable.Rows.Add(Row);
            }
        }

        StringBuilder sb1 = new StringBuilder();
        string[] columnNames0 = remtable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        sb1.AppendLine(string.Join(",", columnNames0));

        foreach (DataRow row in remtable.Rows)
        {
            string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
            sb1.AppendLine(string.Join(",", fields));
        }

        //update CSV list of known cwls names
        StringBuilder sb2 = new StringBuilder();
        string[] cwlslist = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cwls")).Distinct().ToArray(); //this removes the \r\n from the end of the string
        sb2.AppendLine(string.Join(",", cwlslist));

        //update CSV list of cwls cache dates
        StringBuilder sb4 = new StringBuilder();
        string[] cwlslistdates = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray(); //this removes the \r\n from the end of the string
        sb4.AppendLine(string.Join(",", cwlslistdates));

        Plugin.Configuration.CWLSCSVData = sb1.ToString();
        Plugin.Configuration.CWLSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
        Plugin.Configuration.CWLSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
        Configuration.Save();
    }

    public override void Draw()
    {
        //main ui
        ImGui.Spacing();

        if (ImGui.Button("Cache CWLS Members"))
        {
            CacheCWLSMembers();
        }

        ImGui.SameLine();

        if (ImGui.Button("Advanced Options"))
        {
            Plugin.ToggleConfigUI();
        }

        ImGui.Spacing();

        //-----------------------------------------------------------------------------
        //        start table display and sorting here
        //-----------------------------------------------------------------------------

        //check for empty strings that will cause things to break
        if (!Plugin.Configuration.CWLSCSVList.Equals("") && !Plugin.Configuration.CWLSCSVListDate.Equals("") && !Plugin.Configuration.CWLSCSVData.Equals(""))
        {

            //make list of cwls names for drop down
            string[] CWLSList;
            CWLSList = Plugin.Configuration.CWLSCSVList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            //check if list index is out of array, if it is return to 0 before drawing list
            if (Plugin.Configuration.CWLSListIndex != 0 && Plugin.Configuration.CWLSListIndex >= CWLSList.Length)
            {
                Plugin.Configuration.CWLSListIndex = 0;
                Configuration.Save();
            }

            //make list of cache dates to display next to cwls list drop down
            string[] CWLSListDate;
            CWLSListDate = Plugin.Configuration.CWLSCSVListDate.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            ImGui.SetNextItemWidth(300.0f);
            //if (ImGui.BeginCombo("", CWLSList[Plugin.Configuration.CWLSListIndex]))
            using (var cwlspicklist = ImRaii.Combo("", CWLSList[Plugin.Configuration.CWLSListIndex]))
            {
                if (cwlspicklist)
                {
                    for (int i = 0; i < CWLSList.Length; i++)
                    {
                        bool isselected = (Plugin.Configuration.CWLSListIndex == i);
                        if (ImGui.Selectable(CWLSList[i], isselected))
                        {
                            Plugin.Configuration.CWLSListIndex = i;
                            Configuration.Save();
                        }
                        if (isselected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    //ImGui.EndCombo();
                }

            }

            //ImGui.SameLine();
            ImGui.Text($"Members: {Plugin.Configuration.CWLSMemberCount}/64 ({Plugin.Configuration.CWLSOnlineCount} Online)");

            ImGui.SameLine();
            ImGui.Spacing();

            ImGui.SameLine();
            ImGui.Text($"Last Cached: {CWLSListDate[Plugin.Configuration.CWLSListIndex]}");

            ImGui.Spacing();

            //start making the display table
            string[] Lines;
            Lines = Plugin.Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
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

                        string[] fields1 = line1.Split(new char[] { ',' });
                        string[] fields2 = line2.Split(new char[] { ',' });

                        short index = sortSpecs.Specs.ColumnIndex; // this is the column that we're sorting by
                        int comparison = 0;

                        switch (index)
                        {
                            case 0: // Names
                            case 1: // Online/Offline
                                comparison = string.Compare(fields1[index], fields2[index]);
                                break;
                            case 2: // Last Seen Date
                                var time1 = DateTime.Parse(fields1[index]);
                                var time2 = DateTime.Parse(fields2[index]);

                                comparison = time1.CompareTo(time2);
                                break;
                            case 3: // days since seen
                                comparison = int.Parse(fields1[index]) - int.Parse(fields2[index]);
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

                    Plugin.Configuration.CWLSMemberCount = 0;
                    Plugin.Configuration.CWLSOnlineCount = 0;

                    for (int i = 1; i < Lines.GetLength(0); i++)
                    {
                        Fields = Lines[i].Split(new char[] { ',' });

                        if (Fields[4].Equals(CWLSList[Plugin.Configuration.CWLSListIndex]))
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            if (int.Parse(Fields[6]) == 1) { ImGui.Text($"{GetWorldName(Fields[0])}"); Plugin.Configuration.CWLSMemberCount++; } //member
                            else { ImGui.TextColored(vectRed, $"{GetWorldName(Fields[0])}"); }
                            ImGui.TableNextColumn();
                            if (string.Equals(Fields[1], "Online")) { ImGui.TextColored(vectGreen, $"{Fields[1]}"); Plugin.Configuration.CWLSOnlineCount++; } //state
                            else { ImGui.TextColored(vectRed, $"{Fields[1]}"); }
                            ImGui.TableNextColumn();
                            ImGui.Text($"{Fields[2].Substring(0, 10)}"); //lastseen
                            ImGui.TableNextColumn();
                            if (int.Parse(Fields[3]) == 40000) { ImGui.Text("Never Seen"); }
                            else { ImGui.Text($"{Fields[3]}"); } //seendays
                            ImGui.TableNextColumn();
                            if (ImGui.SmallButton($"Remove##{i}") && ImGui.GetIO().KeyCtrl) { RemoveCWLSMember(Fields[4], Fields[0]); }
                        }
                    }

                }
            }
            //ImGui.EndTable();
        }

        else
        {
            ImGui.Text("No CWLS Data. Please open up a Cross-world Linkshell and click the Cache CWLS Members button in this window.");
        }
    }
}
