using System;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
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

    private static string GetWorldName(string rawname)
    {
        string[] splitname;
        splitname = rawname.Split(new char[] { ' ' });
        string newname = splitname[0] + " " + splitname[1];
        int worldid = int.Parse(splitname[2]);
        string worldname = "World ID " + splitname[2] + " Not Found";

        //aether dc
        if (worldid == 73) { worldname = "Adamantoise"; }
        if (worldid == 79) { worldname = "Cactuar"; }
        if (worldid == 54) { worldname = "Faerie"; }
        if (worldid == 63) { worldname = "Gilgamesh"; }
        if (worldid == 40) { worldname = "Jenova"; }
        if (worldid == 65) { worldname = "Midgardsormr"; }
        if (worldid == 99) { worldname = "Sargatanas"; }
        if (worldid == 57) { worldname = "Siren"; }

        //crystal dc
        if (worldid == 91) { worldname = "Balmung"; }
        if (worldid == 34) { worldname = "Brynhildr"; }
        if (worldid == 74) { worldname = "Coeurl"; }
        if (worldid == 62) { worldname = "Diabolos"; }
        if (worldid == 81) { worldname = "Goblin"; }
        if (worldid == 75) { worldname = "Malboro"; }
        if (worldid == 37) { worldname = "Mateus"; }
        if (worldid == 41) { worldname = "Zalera"; }

        //primal dc
        if (worldid == 78) { worldname = "Behemoth"; }
        if (worldid == 93) { worldname = "Excalibur"; }
        if (worldid == 53) { worldname = "Exodus"; }
        if (worldid == 35) { worldname = "Famfrit"; }
        if (worldid == 95) { worldname = "Hyperion"; }
        if (worldid == 55) { worldname = "Lamia"; }
        if (worldid == 64) { worldname = "Leviathan"; }
        if (worldid == 77) { worldname = "Ultros"; }

        //dynamis dc
        if (worldid == 406) { worldname = "Halicarnassus"; }
        if (worldid == 407) { worldname = "Maduin"; }
        if (worldid == 404) { worldname = "Marilith"; }
        if (worldid == 405) { worldname = "Seraph"; }
        if (worldid == 408) { worldname = "Cuchulainn"; }
        if (worldid == 411) { worldname = "Golem"; }
        if (worldid == 409) { worldname = "Kraken"; }
        if (worldid == 410) { worldname = "Rafflesia"; }

        //chaos dc
        if (worldid == 80) { worldname = "Cerberus"; }
        if (worldid == 83) { worldname = "Louisoix"; }
        if (worldid == 71) { worldname = "Moogle"; }
        if (worldid == 39) { worldname = "Omega"; }
        if (worldid == 401) { worldname = "Phantom"; }
        if (worldid == 97) { worldname = "Ragnarok"; }
        if (worldid == 400) { worldname = "Sagittarius"; }
        if (worldid == 85) { worldname = "Spriggan"; }

        //light dc
        if (worldid == 402) { worldname = "Alpha"; }
        if (worldid == 36) { worldname = "Lich"; }
        if (worldid == 66) { worldname = "Odin"; }
        if (worldid == 56) { worldname = "Phoenix"; }
        if (worldid == 403) { worldname = "Raiden"; }
        if (worldid == 67) { worldname = "Shiva"; }
        if (worldid == 33) { worldname = "Twintania"; }
        if (worldid == 42) { worldname = "Zodiark"; }

        //elemental dc
        if (worldid == 90) { worldname = "Aegis"; }
        if (worldid == 68) { worldname = "Atomos"; }
        if (worldid == 45) { worldname = "Carbuncle"; }
        if (worldid == 58) { worldname = "Garuda"; }
        if (worldid == 94) { worldname = "Gungnir"; }
        if (worldid == 49) { worldname = "Kujata"; }
        if (worldid == 72) { worldname = "Tonberry"; }
        if (worldid == 50) { worldname = "Typhon"; }

        //gaia dc
        if (worldid == 43) { worldname = "Alexander"; }
        if (worldid == 69) { worldname = "Bahamut"; }
        if (worldid == 92) { worldname = "Durandal"; }
        if (worldid == 46) { worldname = "Fenrir"; }
        if (worldid == 59) { worldname = "Ifrit"; }
        if (worldid == 98) { worldname = "Ridill"; }
        if (worldid == 76) { worldname = "Tiamat"; }
        if (worldid == 51) { worldname = "Ultima"; }

        //mana dc
        if (worldid == 44) { worldname = "Anima"; }
        if (worldid == 23) { worldname = "Asura"; }
        if (worldid == 70) { worldname = "Chocobo"; }
        if (worldid == 47) { worldname = "Hades"; }
        if (worldid == 48) { worldname = "Ixion"; }
        if (worldid == 96) { worldname = "Masamune"; }
        if (worldid == 28) { worldname = "Pandaemonium"; }
        if (worldid == 61) { worldname = "Titan"; }

        //meteor dc
        if (worldid == 24) { worldname = "Belias"; }
        if (worldid == 82) { worldname = "Mandragora"; }
        if (worldid == 60) { worldname = "Ramuh"; }
        if (worldid == 29) { worldname = "Shinryu"; }
        if (worldid == 30) { worldname = "Unicorn"; }
        if (worldid == 52) { worldname = "Valefor"; }
        if (worldid == 31) { worldname = "Yojimbo"; }
        if (worldid == 32) { worldname = "Zeromus"; }

        //materia dc
        if (worldid == 22) { worldname = "Bismarck"; }
        if (worldid == 21) { worldname = "Ravana"; }
        if (worldid == 86) { worldname = "Sephirot"; }
        if (worldid == 87) { worldname = "Sophia"; }
        if (worldid == 88) { worldname = "Zurvan"; }
        //if (worldid == 0) { worldname = "shard"; }
        //if (worldid == 0) { worldname = "shard"; }
        //if (worldid == 0) { worldname = "shard"; }

        newname += " (" + worldname + ")";

        return newname;

    }

    public override void Draw()
    {
        //main ui
        if (ImGui.Button("Cache CWLS Members")) 
        {
            //Fetch CWLS Data and Create Cache Table

            DateTime dateToday = DateTime.Now; //originally .Today

            DataTable cachetable = new DataTable();
            cachetable.Columns.Add("member", typeof(string));
            cachetable.Columns.Add("state", typeof(string));
            cachetable.Columns.Add("lastseen", typeof(DateTime));
            cachetable.Columns.Add("seendays", typeof(int));

            unsafe
            {
                if (InfoProxyCrossWorldLinkshellMember.Instance() != null)
                {
                    //Plugin.Configuration.DEBUGString = "";
                    foreach (var characterData in InfoProxyCrossWorldLinkshellMember.Instance()->CharDataSpan)
                    {
                        DataRow row = cachetable.NewRow();
                        row["member"] = characterData.NameString + " " + characterData.HomeWorld;
                        row["state"] = characterData.State;
                        row["lastseen"] = dateToday.Date;

                        if (characterData.State > 0)
                        {
                            row["seendays"] = 0;
                        }
                        else
                        {
                            row["seendays"] = 40000;
                        }

                        cachetable.Rows.Add(row);
                        //Plugin.Configuration.DEBUGInt0++;
                    }
                }
            }

            //Create Master Table
            DataTable mastertable = new DataTable();
            mastertable.Columns.Add("member", typeof(string));
            mastertable.Columns.Add("state", typeof(string));
            mastertable.Columns.Add("lastseen", typeof(DateTime));
            mastertable.Columns.Add("seendays", typeof(int));

            string[] masterLines;
            masterLines = Plugin.Configuration.CWLSCSVMaster.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] masterFields;
            for (int i = 1; i < masterLines.GetLength(0); i++)
            {
                masterFields = masterLines[i].Split(new char[] { ',' });
                DataRow Row = mastertable.NewRow();
                Row["member"] = masterFields[0];
                Row["state"] = masterFields[1];
                Row["lastseen"] = masterFields[2];
                Row["seendays"] = masterFields[3];
                mastertable.Rows.Add(Row);
            }

            //Do Compare and Update
            foreach (DataRow cacheRow in cachetable.Rows)
            {
                string cacheMember = cacheRow.Field<string>("member");
                string cacheState = cacheRow.Field<string>("state");
                DateTime cacheLastseen = cacheRow.Field<DateTime>("lastseen");
                int cacheSeendays = cacheRow.Field<int>("seendays");
                int foundMember = 0;
                //Plugin.Configuration.DEBUGInt1++;

                foreach (DataRow masterRow in mastertable.Rows)
                {
                    string masterMember = masterRow.Field<string>("member");
                    string masterState = masterRow.Field<string>("state");
                    DateTime masterLastseen = masterRow.Field<DateTime>("lastseen");
                    int masterSeendays = masterRow.Field<int>("seendays");

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
                    }

                }

                if (foundMember == 0) //if member has not been found, will = 0, then write that member to master
                {
                    DataRow Row = mastertable.NewRow();
                    Row["member"] = cacheMember;
                    Row["state"] = cacheState;
                    Row["lastseen"] = cacheLastseen;
                    Row["seendays"] = cacheSeendays;
                    mastertable.Rows.Add(Row);
                    //Plugin.Configuration.DEBUGInt2++;
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
                //Plugin.Configuration.DEBUGInt3++;
            }

            //Plugin.Configuration.CWLSCSVMasterBackup = Plugin.Configuration.CWLSCSVMaster;
            Plugin.Configuration.CWLSCSVMaster = sb0.ToString();
            Plugin.Configuration.CWLSCSVMasterDate = dateToday.ToString();
            Configuration.Save();
        }

        ImGui.SameLine();

        if (ImGui.Button("Show Settings"))
        {
            Plugin.ToggleConfigUI();
        }

        ImGui.SameLine();
        ImGui.Text($"Last Cache: {Configuration.CWLSCSVMasterDate}");

        ImGui.Spacing();

        string removeName = "";

        string[] Lines;
        Lines = Plugin.Configuration.CWLSCSVMaster.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        string[] Fields;

        Vector4 vectRed = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        Vector4 vectGreen = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);

        ImGuiTableFlags flags = ImGuiTableFlags.ScrollY | 
                                ImGuiTableFlags.RowBg | 
                                ImGuiTableFlags.Sortable
                                ;

        ImGui.BeginTable("displaytable", 5, flags);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Member", ImGuiTableColumnFlags.None, 0.38f);
        ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending, 0.11f);
        ImGui.TableSetupColumn("Last Seen", ImGuiTableColumnFlags.None, 0.18f);
        ImGui.TableSetupColumn("Days Since", ImGuiTableColumnFlags.None, 0.18f);
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
                return sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending ? - comparison : comparison;
            }
            return 0;

        });

        for (int i = 1; i < Lines.GetLength(0); i++)
        {
            Fields = Lines[i].Split(new char[] { ',' });
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (Configuration.UseWorldNames == true) { ImGui.Text($"{GetWorldName(Fields[0])}"); } //member
            else { ImGui.Text($"{Fields[0]}"); }
            ImGui.TableNextColumn();
            if (string.Equals(Fields[1], "Online")) { ImGui.TextColored(vectGreen, $"{Fields[1]}"); } //state
            else { ImGui.TextColored(vectRed, $"{Fields[1]}"); }
            ImGui.TableNextColumn();
            ImGui.Text($"{Fields[2].Substring(0, 10)}"); //lastseen
            ImGui.TableNextColumn();
            if (string.Equals(Fields[3], "40000")) { ImGui.Text("Never Seen"); }
            else { ImGui.Text($"{Fields[3]}"); } //seendays
            ImGui.TableNextColumn();
            if (ImGui.SmallButton($"Remove##{i}")) { removeName = Fields[0]; }
        }

        ImGui.EndTable();

        //remove member removeName from csv master
        if (!removeName.Equals(""))
        {
            if (ImGui.GetIO().KeyCtrl) //check for ctrl held
            {
                DataTable remtable = new DataTable();
                remtable.Columns.Add("member", typeof(string));
                remtable.Columns.Add("state", typeof(string));
                remtable.Columns.Add("lastseen", typeof(DateTime));
                remtable.Columns.Add("seendays", typeof(int));

                string[] remLines;
                remLines = Plugin.Configuration.CWLSCSVMaster.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string[] remFields;

                for (int i = 1; i < remLines.GetLength(0); i++)
                {
                    remFields = remLines[i].Split(new char[] { ',' });
                    if (!remFields[0].Equals(removeName))
                    {
                        DataRow Row = remtable.NewRow();
                        Row["member"] = remFields[0];
                        Row["state"] = remFields[1];
                        Row["lastseen"] = remFields[2];
                        Row["seendays"] = remFields[3];
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
                    //Plugin.Configuration.DEBUGInt3++;
                }

                Plugin.Configuration.CWLSCSVMaster = sb1.ToString();
                Configuration.Save();

            }
            removeName = "";
        }
    }
}
