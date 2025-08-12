using System;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Dalamud.Bindings.ImGui;

namespace CWLSLastSeenTool.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("CWLS Last Seen Tool Advanced Options") //("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoCollapse; //| ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar |
                //ImGuiWindowFlags.NoScrollWithMouse;

        //Size = new Vector2(440, 320); //width x heght default 340 220
        //SizeCondition = ImGuiCond.Always;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 340),
            MaximumSize = new Vector2(480, float.MaxValue)
        };

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
    }
    private void MergeCWLS() //rename currently loaded cwls list and merge it with new list being cached
    {
        //Fetch CWLS Data and Create Cache Table

        //do a couple of sanity checks first to get active cwls list and return out if they fail
        if (Configuration.CWLSCSVList.Equals(""))
        {
            return;
        }

        string[] CWLSList;
        CWLSList = Configuration.CWLSCSVList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        //check if list index is out of array, if it is return to 0 before drawing list
        if (Configuration.CWLSListIndex != 0 && Configuration.CWLSListIndex >= CWLSList.Length)
        {
            Configuration.CWLSListIndex = 0;
            Configuration.Save();
        }

        string activecwlslist = CWLSList[Configuration.CWLSListIndex];

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
        masterLines = Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        string[] masterFields;
        for (int i = 1; i < masterLines.GetLength(0); i++)
        {
            masterFields = masterLines[i].Split(new char[] { ',' });
            if (masterFields[4].Equals(selectedcwlsname)) //if cwls being cached already exists in csv data then return out to prevent duplicates being created
            {
                return;
            }
            DataRow Row = mastertable.NewRow();
            Row["member"] = masterFields[0];
            Row["state"] = masterFields[1];
            Row["lastseen"] = masterFields[2];
            Row["seendays"] = masterFields[3];
            //Row["cwls"] = masterFields[4];
            if (masterFields[4].Equals(activecwlslist)) //if cwls being loaded is the active cwls rename it to the cwls being cached
            {
                Row["cwls"] = selectedcwlsname;
            }
            else
            {
                Row["cwls"] = masterFields[4];
            }
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

        Configuration.CWLSCSVData = sb0.ToString();
        Configuration.CWLSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
        Configuration.CWLSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
        Configuration.Save();
    }

    private void RemoveCWLS(string remcwls) //remove whole cwls list
    {
        //do a couple of sanity checks first to get active cwls list and return out if they fail
        if (Configuration.CWLSCSVList.Equals(""))
        {
            return;
        }

        //this works by rebuilding the main csv while excluding the selected cwls
        DataTable remtable = new DataTable();
        remtable.Columns.Add("member", typeof(string));
        remtable.Columns.Add("state", typeof(string));
        remtable.Columns.Add("lastseen", typeof(DateTime));
        remtable.Columns.Add("seendays", typeof(int));
        remtable.Columns.Add("cwls", typeof(string));
        remtable.Columns.Add("cachedate", typeof(string));
        remtable.Columns.Add("ispresent", typeof(int));

        string[] remLines;
        remLines = Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        string[] remFields;

        for (int i = 1; i < remLines.GetLength(0); i++)
        {
            remFields = remLines[i].Split(new char[] { ',' });
            if (remFields[4].Equals(remcwls)) //THIS IS THE ONLY DIFFERENCE TO REMOVE CWLS MEMBER, REMOVED THE MEMBER NAME CHECK
            {
                //throw the cwls you want to remove into the void
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
        string[] cwlslist = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cwls")).Distinct().ToArray();
        sb2.AppendLine(string.Join(",", cwlslist));

        //update CSV list of cwls cache dates
        StringBuilder sb4 = new StringBuilder();
        string[] cwlslistdates = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
        sb4.AppendLine(string.Join(",", cwlslistdates));

        Configuration.CWLSCSVData = sb1.ToString();
        Configuration.CWLSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2); //this removes the \r\n from the end of the string
        Configuration.CWLSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2); //this removes the \r\n from the end of the string
        Configuration.Save();
    }

    public override void Draw()
    {
        ImGui.Text("Display");
        ImGui.Separator();
        ImGui.Spacing();

        // can't ref a property, so use a local copy
        var useworldnamesValue = Configuration.UseWorldNames;
        if (ImGui.Checkbox("Show World Name Instead Of World ID", ref useworldnamesValue))
        {
            Configuration.UseWorldNames = useworldnamesValue;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            Configuration.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Text("Advanced Actions (Hold CTRL)");
        ImGui.Separator();
        ImGui.Spacing();

        string activecwlslist = "No CWLS Have Been Cached";
        if (!Configuration.CWLSCSVList.Equals(""))
        {
            string[] CWLSList;
            CWLSList = Configuration.CWLSCSVList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            //check if list index is out of array, if it is return to 0 before drawing list
            if (Configuration.CWLSListIndex != 0 && Configuration.CWLSListIndex >= CWLSList.Length)
            {
                Configuration.CWLSListIndex = 0;
                Configuration.Save();
            }

            activecwlslist = CWLSList[Configuration.CWLSListIndex];
        }
        ImGui.Text($"Active CWLS List: {activecwlslist}");
        ImGui.Spacing();

        if (ImGui.Button("Remove Active CWLS List") && ImGui.GetIO().KeyCtrl)
        {
            RemoveCWLS(activecwlslist);
        }
        ImGui.TextWrapped("Removes all entries from the current list.");
        ImGui.Spacing();

        if (ImGui.Button("Merge/Update CWLS List") && ImGui.GetIO().KeyCtrl)
        {
            MergeCWLS();
        }
        ImGui.TextWrapped("Updates the name of the active CWLS list and caches its members against the currently selected Cross-world Linkshell. This will fail if the Cross-world Linkshell has already been cached/exists in the CWLS list.");
        //ImGui.Spacing();

        

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Text("Backup and Restore (Hold CTRL)");
        ImGui.Separator();
        ImGui.Spacing();

        //ImGui.Text("Hold CTRL for CWLSCSVMaster Commands");

        //ImGui.Spacing();

        if (ImGui.Button("Clear Active CWLS Data") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.CWLSCSVData = "";
            Configuration.CWLSCSVList = "";
            Configuration.CWLSCSVListDate = "";
            Configuration.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button("Restore CWLS Data Internal Backup") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.CWLSCSVData = Configuration.CWLSCSVDataBACKUP;
            Configuration.CWLSCSVList = Configuration.CWLSCSVListBACKUP;
            Configuration.CWLSCSVListDate = Configuration.CWLSCSVListDateBACKUP;
            Configuration.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button("Create CWLS Data Internal Backup") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.CWLSCSVDataBACKUP = Configuration.CWLSCSVData;
            Configuration.CWLSCSVListBACKUP = Configuration.CWLSCSVList;
            Configuration.CWLSCSVListDateBACKUP = Configuration.CWLSCSVListDate;
            Configuration.Save();
        }

        ImGui.Spacing();

        //ImGui.Text($"LIST: {Configuration.CWLSCSVList}");
        //ImGui.Text($"DATE: {Configuration.CWLSCSVListDate}");
        //ImGui.Text($"LIST INDEX: {Configuration.CWLSListIndex}");
        //ImGui.TextWrapped($"Backed Up CWLS: {Configuration.CWLSCSVListBACKUP}");
        //ImGui.TextWrapped($"CWLS Dates: {Configuration.CWLSCSVListDateBACKUP}");

        if (!Configuration.CWLSCSVListBACKUP.Equals("") && !Configuration.CWLSCSVListDateBACKUP.Equals(""))
        {
            string[] backupnames;
            backupnames = Configuration.CWLSCSVListBACKUP.Split(new char[] { ',' });
            string[] backupdates;
            backupdates = Configuration.CWLSCSVListDateBACKUP.Split(new char[] { ',' });

            ImGui.Text("Backed Up CWLS\t(Last Synced)");

            for (int i = 0; i < backupnames.GetLength(0); i++)
            {
                ImGui.Text($"{backupnames[i]}\t({backupdates[i]})");
            }
        }
        else
        {
            ImGui.Text("No Backups");
        }

        ImGui.Spacing();

        //if (ImGui.Button("CLEAR CWLSCSVMaster") && ImGui.GetIO().KeyCtrl)
        //{
        //    Configuration.CWLSCSVMasterDate = "CWLS CSV Cleared";
        //    Configuration.CWLSCSVMaster = "";
        //    Configuration.Save();
        //}

        //ImGui.Spacing();

        //if (ImGui.Button("RESTORE CWLSCSVMaster") && ImGui.GetIO().KeyCtrl)
        //{
        //    Configuration.CWLSCSVMasterDate = "Backup Restored";//Configuration.CWLSCSVMasterBackupDate"";
        //    Configuration.CWLSCSVMaster = Configuration.CWLSCSVMasterBackup;
        //    Configuration.Save();
        //}

        //ImGui.Spacing();

        //if (ImGui.Button("WRITE CWLSCSVMasterBackup") && ImGui.GetIO().KeyCtrl)
        //{
        //    DateTime buDate = DateTime.Now;
        //    Configuration.CWLSCSVMasterBackupDate = buDate.ToString();
        //    Configuration.CWLSCSVMasterBackup = Configuration.CWLSCSVMaster;
        //    Configuration.Save();
        //}
        //ImGui.Text($"Last Backup: {Configuration.CWLSCSVMasterBackupDate}");

        //ImGui.Spacing();

        //ImGui.Text($"DEBUG STEPS: {Configuration.DEBUGString}");
        //ImGui.Text($"DEBUG Int0: {Configuration.DEBUGInt0}"); //member cached to table
        //ImGui.Text($"DEBUG Int1: {Configuration.DEBUGInt1}");
        //ImGui.Text($"DEBUG Int2: {Configuration.DEBUGInt2}");
        //ImGui.Text($"DEBUG Int3: {Configuration.DEBUGInt3}");

        //if (ImGui.Button("CLEAR DEBUG"))
        //{
        //    Configuration.DEBUGString = "";
        //    Configuration.DEBUGInt0 = 0;
        //    Configuration.DEBUGInt1 = 0;
        //    Configuration.DEBUGInt2 = 0;
        //    Configuration.DEBUGInt3 = 0;
        //    Configuration.Save();
        //}
    }
}
