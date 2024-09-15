using System;
using System.ComponentModel.Design;
using System.Data;
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
        ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.None, 0.11f);
        ImGui.TableSetupColumn("Last Seen", ImGuiTableColumnFlags.None, 0.18f);
        ImGui.TableSetupColumn("Days Since", ImGuiTableColumnFlags.None, 0.18f);
        ImGui.TableSetupColumn("Hold CTRL", ImGuiTableColumnFlags.NoSort, 0.15f);
        ImGui.TableHeadersRow();

        for (int i = 1; i < Lines.GetLength(0); i++)
        {
            Fields = Lines[i].Split(new char[] { ',' });
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"{Fields[0]}"); //member
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
