using System;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;

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

    private string WorldIdToName(int worldid)
    {
        string worldname = "??" + worldid.ToString() + "??";

        if (Plugin.DataManager.GetExcelSheet<World>().HasRow(Convert.ToUInt32(worldid)))
        {
            worldname = Plugin.DataManager.GetExcelSheet<World>().GetRow(Convert.ToUInt32(worldid)).Name.ToString();
        }

        return worldname;
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

    private void MergeLinkshell() //rename currently loaded linkshell list and merge it with new list being cached
    {
        //Fetch Linkshell Data and Create Cache Table

        //do a couple of sanity checks first to get active linkshell list and return out if they fail
        if (Configuration.LSCSVList.Equals(""))
        {
            return;
        }

        string[] LSList;
        LSList = Configuration.LSCSVList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        //check if list index is out of array, if it is return to 0 before drawing list
        if (Configuration.LSListIndex != 0 && Configuration.LSListIndex >= LSList.Length)
        {
            Configuration.LSListIndex = 0;
            Configuration.Save();
        }

        string activelslist = LSList[Configuration.LSListIndex];

        DateTime dateToday = DateTime.Now;
        string selectedlsname = "";
        string lsname = "";
        int worldid = 0;
        int lsloaded = 0;

        DataTable cachetable = new DataTable();
        cachetable.Columns.Add("member", typeof(string));
        cachetable.Columns.Add("state", typeof(string));
        cachetable.Columns.Add("lastseen", typeof(DateTime));
        cachetable.Columns.Add("seendays", typeof(int));
        cachetable.Columns.Add("linkshell", typeof(string));
        cachetable.Columns.Add("cachedate", typeof(string));
        cachetable.Columns.Add("ispresent", typeof(int));
        cachetable.Columns.Add("worldid", typeof(int));
        cachetable.Columns.Add("lslist", typeof(string));

        unsafe
        {
            if (InfoProxyLinkshellMember.Instance() != null && AgentLinkshell.Instance() != null && InfoProxyLinkshell.Instance() != null)
            {
                uint selectedlsindex = AgentLinkshell.Instance()->SelectedLSIndex;
                ulong lsid = InfoProxyLinkshell.Instance()->GetLinkshellInfo(selectedlsindex) ->Id;
                lsname = InfoProxyLinkshell.Instance() ->GetLinkshellName(lsid).ToString();

                foreach (var characterData in InfoProxyLinkshellMember.Instance()->CharDataSpan)
                {
                    DataRow row = cachetable.NewRow();
                    //if (string.Compare(characterData.NameString, "")){row["member"] = characterData.NameString;}
                    row["member"] = characterData.NameString; // + " " + characterData.HomeWorld;
                    if (string.Equals(row["member"], "")){row["member"] = "(Unable to Retrieve)";}
                    row["state"] = "Offline";
                    if (characterData.State > 0)
                    {
                        row["state"] = "Online";
                    }
                    row["lastseen"] = dateToday;
                    if (characterData.State > 0)
                    {
                        row["seendays"] = 0;
                    }
                    else
                    {
                        row["seendays"] = 40000;
                    }
                    row["linkshell"] = lsname;
                    row["cachedate"] = dateToday.ToString();
                    row["ispresent"] = 0;
                    row["worldid"] = characterData.HomeWorld;
                    row["lslist"] = "";

                    cachetable.Rows.Add(row);
                    worldid = characterData.HomeWorld;
                }

                selectedlsname = lsname + " (" + WorldIdToName(worldid) + ")";
                lsloaded = 1;
            }
        }

        //return out if failed to get linkshell info
        if (lsloaded == 0)
        {
            return;
        }

        //Create Master Table
        DataTable mastertable = new DataTable();
        mastertable.Columns.Add("member", typeof(string));
        mastertable.Columns.Add("state", typeof(string));
        mastertable.Columns.Add("lastseen", typeof(DateTime));
        mastertable.Columns.Add("seendays", typeof(int));
        mastertable.Columns.Add("linkshell", typeof(string));
        mastertable.Columns.Add("cachedate", typeof(string));
        mastertable.Columns.Add("ispresent", typeof(int));
        mastertable.Columns.Add("worldid", typeof(int));
        mastertable.Columns.Add("lslist", typeof(string));

        string[] masterLines;
        masterLines = Configuration.LSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        string[] masterFields;
        for (int i = 1; i < masterLines.GetLength(0); i++)
        {
            masterFields = masterLines[i].Split(new char[] { ',' });
            if (masterFields[8].Equals(selectedlsname)) //if linkshell being cached already exists in csv data then return out to prevent duplicates being created
            {
                return;
            }
            DataRow Row = mastertable.NewRow();
            Row["member"] = masterFields[0];
            Row["state"] = masterFields[1];
            Row["lastseen"] = masterFields[2];
            Row["seendays"] = masterFields[3];
            //Row["cwls"] = masterFields[4];
            if (masterFields[8].Equals(activelslist)) //if linkshell being loaded is the active linkshell rename it to the linkshell being cached
            {
                Row["linkshell"] = lsname;
            }
            else
            {
                Row["linkshell"] = masterFields[4];
            }
            Row["cachedate"] = masterFields[5];
            Row["ispresent"] = masterFields[6];
            Row["worldid"] = masterFields[7];
            //Row["lslist"] = masterFields[8];
            if (masterFields[8].Equals(activelslist)) //if linkshell being loaded is the active linkshell rename it to the linkshell being cached
            {
                Row["lslist"] = selectedlsname;
            }
            else
            {
                Row["lslist"] = masterFields[8];
            }
            mastertable.Rows.Add(Row);
        }

        //Do Compare and Update
        foreach (DataRow cacheRow in cachetable.Rows)
        {
            string cacheMember = cacheRow.Field<string>("member");
            string cacheState = cacheRow.Field<string>("state");
            DateTime cacheLastseen = cacheRow.Field<DateTime>("lastseen");
            int cacheSeendays = cacheRow.Field<int>("seendays");
            string cacheLS = cacheRow.Field<string>("linkshell");
            string cacheCacheDate = cacheRow.Field<string>("cachedate");
            int cacheWorldId = cacheRow.Field<int>("worldid");
            int foundMember = 0;

            foreach (DataRow masterRow in mastertable.Rows)
            {
                string masterMember = masterRow.Field<string>("member");
                DateTime masterLastseen = masterRow.Field<DateTime>("lastseen");
                int masterSeendays = masterRow.Field<int>("seendays");
                string masterLS = masterRow.Field<string>("linkshell");
                int masterWorldId = masterRow.Field<int>("worldid");

                if (string.Equals(cacheLS, masterLS) && cacheWorldId == masterWorldId) //should prevent members leaving cwls orphaning csv entry cachedates
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
                    masterRow["lslist"] = selectedlsname;
                }

            }

            if (foundMember == 0) //if member has not been found, will = 0, then write that member to master
            {
                DataRow Row = mastertable.NewRow();
                Row["member"] = cacheMember;
                Row["state"] = cacheState;
                Row["lastseen"] = cacheLastseen;
                Row["seendays"] = cacheSeendays;
                Row["linkshell"] = cacheLS;
                Row["cachedate"] = cacheCacheDate;
                Row["ispresent"] = 0;
                Row["worldid"] = cacheWorldId;
                Row["lslist"] = selectedlsname;
                mastertable.Rows.Add(Row);
            }
        }

        //compare master to cached to get presence
        foreach (DataRow mRow in mastertable.Rows)
        {
            if (string.Equals(mRow["linkshell"], lsname) && mRow["worldid"].Equals(worldid))
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
        string[] lslist = mastertable.Rows.Cast<DataRow>().Select(r => r.Field<string>("lslist")).Distinct().ToArray();
        sb2.AppendLine(string.Join(",", lslist));

        //make CSV list of cwls cache dates
        StringBuilder sb4 = new StringBuilder();
        string[] lslistdates = mastertable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
        sb4.AppendLine(string.Join(",", lslistdates));

        Configuration.LSCSVData = sb0.ToString();
        Configuration.LSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
        Configuration.LSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
        Configuration.Save();
    }

    private void RemoveLinkshell(string remlinkshell) //remove whole cwls list
    {
        //do a couple of sanity checks first to get active cwls list and return out if they fail
        if (Configuration.LSCSVList.Equals(""))
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
        remtable.Columns.Add("worldid", typeof(int));
        remtable.Columns.Add("lslist", typeof(string));

        string[] remLines;
        remLines = Configuration.LSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        string[] remFields;

        for (int i = 1; i < remLines.GetLength(0); i++)
        {
            remFields = remLines[i].Split(new char[] { ',' });
            if (remFields[8].Equals(remlinkshell)) //THIS IS THE ONLY DIFFERENCE TO REMOVE CWLS MEMBER, REMOVED THE MEMBER NAME CHECK
            {
                //throw the linkshell you want to remove into the void
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
                Row["worldid"] = remFields[7];
                Row["lslist"] = remFields[8];
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
        string[] lslist = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("lslist")).Distinct().ToArray();
        sb2.AppendLine(string.Join(",", lslist));

        //update CSV list of cwls cache dates
        StringBuilder sb4 = new StringBuilder();
        string[] lslistdates = remtable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
        sb4.AppendLine(string.Join(",", lslistdates));

        Configuration.LSCSVData = sb1.ToString();
        Configuration.LSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2); //this removes the \r\n from the end of the string
        Configuration.LSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2); //this removes the \r\n from the end of the string
        Configuration.Save();
    }

    private void DrawCWLSOptions()
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
        ImGui.Text($"Selected CWLS List: {activecwlslist}");
        ImGui.Spacing();

        if (ImGui.Button("Remove Selected CWLS List") && ImGui.GetIO().KeyCtrl)
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

    private void DrawLSOptions()
    {
        ImGui.Spacing();

        ImGui.Text("Advanced Actions (Hold CTRL)");
        ImGui.Separator();
        ImGui.Spacing();

        string activecwlslist = "No Linkshells Have Been Cached";
        if (!Configuration.LSCSVList.Equals(""))
        {
            string[] LSList;
            LSList = Configuration.LSCSVList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            //check if list index is out of array, if it is return to 0 before drawing list
            if (Configuration.LSListIndex != 0 && Configuration.LSListIndex >= LSList.Length)
            {
                Configuration.LSListIndex = 0;
                Configuration.Save();
            }

            activecwlslist = LSList[Configuration.LSListIndex];
        }
        ImGui.Text($"Selected Linkshell List: {activecwlslist}");
        ImGui.Spacing();

        if (ImGui.Button("Remove Selected Linkshell List") && ImGui.GetIO().KeyCtrl)
        {
            RemoveLinkshell(activecwlslist);
        }
        ImGui.TextWrapped("Removes all entries from the current list.");
        ImGui.Spacing();

        if (ImGui.Button("Merge/Update Linkshell List") && ImGui.GetIO().KeyCtrl)
        {
            MergeLinkshell();
        }
        ImGui.TextWrapped("Updates the name of the active Linkshell list and caches its members against the currently selected Linkshell. This will fail if the Linkshell has already been cached/exists in the Linkshell list.");
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

        if (ImGui.Button("Clear Active Linkshell Data") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.LSCSVData = "";
            Configuration.LSCSVList = "";
            Configuration.LSCSVListDate = "";
            Configuration.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button("Restore Linkshell Data Internal Backup") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.LSCSVData = Configuration.LSCSVDataBACKUP;
            Configuration.LSCSVList = Configuration.LSCSVListBACKUP;
            Configuration.LSCSVListDate = Configuration.LSCSVListDateBACKUP;
            Configuration.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button("Create Linkshell Data Internal Backup") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.LSCSVDataBACKUP = Configuration.LSCSVData;
            Configuration.LSCSVListBACKUP = Configuration.LSCSVList;
            Configuration.LSCSVListDateBACKUP = Configuration.LSCSVListDate;
            Configuration.Save();
        }

        ImGui.Spacing();

        if (!Configuration.LSCSVListBACKUP.Equals("") && !Configuration.LSCSVListDateBACKUP.Equals(""))
        {
            string[] backupnames;
            backupnames = Configuration.LSCSVListBACKUP.Split(new char[] { ',' });
            string[] backupdates;
            backupdates = Configuration.LSCSVListDateBACKUP.Split(new char[] { ',' });

            ImGui.Text("Backed Up Linkshells\t(Last Synced)");

            for (int i = 0; i < backupnames.GetLength(0); i++)
            {
                ImGui.Text($"{backupnames[i]}\t({backupdates[i]})");
            }
        }
        else
        {
            ImGui.Text("No Backups");
        }
    }

    public override void Draw()
    {
        ImGui.Spacing();

        using (var ConfigTabBar = ImRaii.TabBar("config window tab bar"))
        {
            if(ImGui.BeginTabItem("Cross-world Linkshell Options"))
            {
                DrawCWLSOptions();
                ImGui.EndTabItem();
            }
            if(ImGui.BeginTabItem("Home World Linkshell Options"))
            {
                DrawLSOptions();
                ImGui.EndTabItem();
            }
        }
    }
}
