using System;
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
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.Button("Show Settings"))
        {
            Plugin.ToggleConfigUI();
        }

        ImGui.SameLine();

        if (ImGui.Button("CLEAR DEBUG"))
        {
            Plugin.Configuration.DEBUGString = "";
            Plugin.Configuration.DEBUGInt0 = 0;
            Plugin.Configuration.DEBUGInt1 = 0;
            Plugin.Configuration.DEBUGInt2 = 0;
            Plugin.Configuration.DEBUGInt3 = 0;
            Configuration.Save();
        }

        ImGui.Text($"DEBUG STEPS: {Plugin.Configuration.DEBUGString}");
        ImGui.Text($"DEBUG Int0: {Plugin.Configuration.DEBUGInt0}"); //member cached to table
        ImGui.Text($"DEBUG Int1: {Plugin.Configuration.DEBUGInt1}");
        ImGui.Text($"DEBUG Int2: {Plugin.Configuration.DEBUGInt2}");
        ImGui.Text($"DEBUG Int3: {Plugin.Configuration.DEBUGInt3}");

        if (ImGui.Button("Cache CWLS Members")) 
        {
            //Fetch CWLS Data and Create Cache Table

            DateTime dateToday = DateTime.Today;

            DataTable cachetable = new DataTable();
            cachetable.Columns.Add("member", typeof(string));
            cachetable.Columns.Add("state", typeof(string));
            cachetable.Columns.Add("lastseen", typeof(DateTime));
            cachetable.Columns.Add("seendays", typeof(int));

            unsafe
            {
                if (InfoProxyCrossWorldLinkshellMember.Instance() != null)
                {
                    Plugin.Configuration.DEBUGString = "";
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
                        Plugin.Configuration.DEBUGInt0++;
                    }
                }
                else
                {
                    Plugin.Configuration.DEBUGString = "CWLS not loaded. Open CWLS first.";
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
                Plugin.Configuration.DEBUGInt1++;

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
                    Plugin.Configuration.DEBUGInt2++;
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
                Plugin.Configuration.DEBUGInt3++;
            }

            //Plugin.Configuration.CWLSCSVMasterBackup = Plugin.Configuration.CWLSCSVMaster;
            Plugin.Configuration.CWLSCSVMaster = sb0.ToString();
            Configuration.Save();
        }

        ImGui.SameLine();
        ImGui.Text($"{Plugin.Configuration.DEBUGString}");

        ImGui.Spacing();

        DataTable displaytable = new DataTable();
        displaytable.Columns.Add("member", typeof(string));
        displaytable.Columns.Add("state", typeof(string));
        displaytable.Columns.Add("lastseen", typeof(string));
        displaytable.Columns.Add("seendays", typeof(string));

        string[] Lines;
        Lines = Plugin.Configuration.CWLSCSVMaster.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        string[] Fields;

        for (int i = 1; i < Lines.GetLength(0); i++)
        {
            Fields = Lines[i].Split(new char[] { ',' });
            DataRow Row = displaytable.NewRow();
            Row["member"] = Fields[0];//.PadRight(32);
            Row["state"] = Fields[1];//.PadRight(16);
            Row["lastseen"] = Fields[2].Substring(0, 10);//.PadRight(16);
            Row["seendays"] = Fields[3];
            displaytable.Rows.Add(Row);
        }

        foreach (DataRow row in displaytable.Rows)
        {
            //ImGui.Text($"{row.Field<string>("member")}\t{row.Field<string>("state")}\t{row.Field<string>("lastseen")}\t{row.Field<string>("seendays")}");
            ImGui.Text($"{row.Field<string>("member")}");
            ImGui.SameLine(200);
            ImGui.Text($"{row.Field<string>("state")}");
            ImGui.SameLine(300);
            ImGui.Text($"{row.Field<string>("lastseen")}");
            ImGui.SameLine(450);
            ImGui.Text($"{row.Field<string>("seendays")}");
        }
    }
}
