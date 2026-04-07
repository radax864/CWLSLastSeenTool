using System;
using System.Data;
using System.Linq;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.Sheets;

namespace CWLSLastSeenTool.Utils;

public class ShellTools : IDisposable
{
    private Configuration Configuration;

    public ShellTools(Plugin plugin)
    {
        Configuration = plugin.Configuration;
    }

    public void Dispose()
    {
        //nothing to dispose currently
    }

    private string WorldIdToName(ushort worldId)
    {
        string worldName = "??" + worldId.ToString() + "??";

        if (Plugin.DataManager.GetExcelSheet<World>().HasRow(Convert.ToUInt32(worldId)))
        {
            worldName = Plugin.DataManager.GetExcelSheet<World>().GetRow(Convert.ToUInt32(worldId)).Name.ToString();
        }

        return worldName;
    }

    public void ClampShellListIndex(string shellType)
    {
        string[] shellList;
        int shellListLength;
        if (string.Equals(shellType, "CWLS") && !string.Equals(Configuration.CWLSCSVList, ""))
        {
            shellList = Configuration.CWLSCSVList.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            shellListLength = shellList.Length;
        }
        else if (string.Equals(shellType, "LS") && !string.Equals(Configuration.LSCSVList, ""))
        {
            shellList = Configuration.LSCSVList.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            shellListLength = shellList.Length;
        }
        else
        {
            Configuration.DEBUGString = "ClampShellListIndex Failed to create shell list.";
            return;
        }

        //check if list index is out of array, if it is bring it back within before drawing list, neither of these run if index is within array
        if (string.Equals(shellType, "CWLS") && Configuration.CWLSListIndex != 0 && Configuration.CWLSListIndex >= shellListLength)
        {
            Configuration.CWLSListIndex = shellListLength - 1;
            Configuration.Save();
        }
        else if (string.Equals(shellType, "LS") && Configuration.LSListIndex != 0 && Configuration.LSListIndex >= shellListLength)
        {
            Configuration.LSListIndex = shellListLength - 1;
            Configuration.Save();
        }
    }

    public void CacheShell(string shellType, string cacheType = "Normal") //shellType CWLS and LS, cacheType Normal and Merge
    {
        //initial sanity check
        Configuration.DEBUGString = "";
        if (!Plugin.PlayerState.CurrentWorld.IsValid)
        {
            Configuration.DEBUGString = "Unable to get World/DC data.";
            return;
        }

        //Fetch LS Data and Create Cache Table
        DateTime cacheDate = DateTime.Now;
        string shellName = "";
        string shellDc = Plugin.PlayerState.CurrentWorld.Value.DataCenter.Value.Name.ToString();
        string shellHomeWorld = Plugin.PlayerState.HomeWorld.Value.Name.ToString();
        string listName = "";
        string mergeListName = "";

        //get shell list name to merge with
        if (string.Equals(cacheType, "Merge") && string.Equals(shellType, "CWLS") && !Configuration.CWLSCSVList.Equals(""))
        {
            string[] mergeList = Configuration.CWLSCSVList.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            mergeListName = mergeList[Configuration.CWLSListIndex];
        }
        else if (string.Equals(cacheType, "Merge") && string.Equals(shellType, "LS") && !Configuration.LSCSVList.Equals(""))
        {
            string[] mergeList = Configuration.LSCSVList.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            mergeListName = mergeList[Configuration.LSListIndex];
        }
        else if (string.Equals(cacheType, "Merge"))
        {
            Configuration.DEBUGString = "Failed to get merge list name.";
            return;
        }

        DataTable cacheTable = new DataTable();
        cacheTable.Columns.Add("member", typeof(string));
        cacheTable.Columns.Add("homeworld", typeof(string));
        cacheTable.Columns.Add("state", typeof(string));
        cacheTable.Columns.Add("listname", typeof(string));

        unsafe
        {
            if (string.Equals(shellType, "CWLS") && InfoProxyCrossWorldLinkshellMember.Instance() != null && AgentCrossWorldLinkshell.Instance() != null && InfoProxyCrossWorldLinkshell.Instance() != null)
            {
                uint selectedShellIndex = AgentCrossWorldLinkshell.Instance()->SelectedCWLSIndex;
                shellName = InfoProxyCrossWorldLinkshell.Instance()->GetCrossworldLinkshellName(selectedShellIndex)->ToString();
                listName = shellName + " (" + shellDc + ")";

                foreach (var characterData in InfoProxyCrossWorldLinkshellMember.Instance()->CharDataSpan)
                {
                    DataRow row = cacheTable.NewRow();
                    row["member"] = characterData.NameString;
                    if (string.Equals(row["member"], ""))
                    {
                        row["member"] = "(Unable to Retrieve)";
                    }
                    row["homeworld"] = WorldIdToName(characterData.HomeWorld);
                    row["state"] = "Offline";
                    if (characterData.State > 0)
                    {
                        row["state"] = "Online";
                    }
                    row["listname"] = listName;
                    cacheTable.Rows.Add(row);
                }
            }
            else if (string.Equals(shellType, "LS") && InfoProxyLinkshellMember.Instance() != null && AgentLinkshell.Instance() != null && InfoProxyLinkshell.Instance() != null)
            {
                uint selectedShellIndex = AgentLinkshell.Instance()->SelectedLSIndex;
                ulong shellId = InfoProxyLinkshell.Instance()->GetLinkshellInfo(selectedShellIndex) ->Id; //LS requires an extra step to get shellName compared to CWLS
                shellName = InfoProxyLinkshell.Instance() ->GetLinkshellName(shellId).ToString();
                listName = shellName + " (" + shellHomeWorld + ")";

                foreach (var characterData in InfoProxyLinkshellMember.Instance()->CharDataSpan)
                {
                    DataRow row = cacheTable.NewRow();
                    row["member"] = characterData.NameString;
                    if (string.Equals(row["member"], ""))
                    {
                        row["member"] = "(Unable to Retrieve)";
                    }
                    row["homeworld"] = WorldIdToName(characterData.HomeWorld);
                    row["state"] = "Offline";
                    if (characterData.State > 0)
                    {
                        row["state"] = "Online";
                    }
                    row["listname"] = listName;
                    cacheTable.Rows.Add(row);
                }
            }
            else
            {
                Configuration.DEBUGString = "Failed to get Linkshell info.";
                return;
            }
        }

        //Create Master Table
        DataTable masterTable = new DataTable();
        masterTable.Columns.Add("member", typeof(string));
        masterTable.Columns.Add("homeworld", typeof(string));
        masterTable.Columns.Add("state", typeof(string));
        masterTable.Columns.Add("lastseen", typeof(DateTime));
        masterTable.Columns.Add("seendays", typeof(int));
        masterTable.Columns.Add("listname", typeof(string));
        masterTable.Columns.Add("cachedate", typeof(string));
        masterTable.Columns.Add("ispresent", typeof(int));

        string[] masterLines;
        if ((string.Equals(shellType, "CWLS") && Configuration.CSVDataVersion == Configuration.CWLSCSVDataVersion) || (string.Equals(shellType, "CWLS") && string.Equals(Configuration.CWLSCSVData, "")))
        {
            masterLines = Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }
        else if ((string.Equals(shellType, "LS") && Configuration.CSVDataVersion == Configuration.LSCSVDataVersion) || (string.Equals(shellType, "LS") && string.Equals(Configuration.LSCSVData, "")))
        {
            masterLines = Configuration.LSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            Configuration.DEBUGString = "Failed to create masterTable.";
            return;
        }

        string[] masterFields;
        for (int i = 1; i < masterLines.GetLength(0); i++)
        {
            masterFields = masterLines[i].Split(new char[] { '\t' });
            if (string.Equals(cacheType, "Merge") && string.Equals(masterFields[5], listName))
            {
                Configuration.DEBUGString = "Target merge shell already exists.";
                return;
            }
            DataRow Row = masterTable.NewRow();
            Row["member"] = masterFields[0];
            Row["homeworld"] = masterFields[1];
            Row["state"] = masterFields[2];
            Row["lastseen"] = masterFields[3];
            Row["seendays"] = masterFields[4];
            Row["listname"] = masterFields[5];
            if (string.Equals(cacheType, "Merge") && string.Equals(masterFields[5], mergeListName))
            {
                Row["listname"] = listName;
            }
            Row["cachedate"] = masterFields[6];
            Row["ispresent"] = masterFields[7];
            masterTable.Rows.Add(Row);
        }

        //Do Compare and Update
        foreach (DataRow cacheRow in cacheTable.Rows)
        {
            int foundMember = 0;

            foreach (DataRow masterRow in masterTable.Rows)
            {
                if (string.Equals(cacheRow.Field<string>("listname"), masterRow.Field<string>("listname"))) //should prevent members leaving cwls orphaning csv entry cachedates
                {
                    if (string.Equals(cacheRow.Field<string>("member"), masterRow.Field<string>("member")) && string.Equals(cacheRow.Field<string>("homeworld"), masterRow.Field<string>("homeworld")))
                    {
                        foundMember++;
                        if (string.Equals(cacheRow.Field<string>("state"), "Online"))
                        {
                            masterRow["lastseen"] = cacheDate;
                            masterRow["seendays"] = 0;
                        }
                        else if (masterRow.Field<int>("seendays") != 40000)
                        {
                            masterRow["seendays"] = (cacheDate.Date - masterRow.Field<DateTime>("lastseen").Date).Days; //Difference between date last seen online and today
                        }
                        masterRow["state"] = cacheRow.Field<string>("state");
                    }
                    masterRow["cachedate"] = cacheDate;
                }
            }

            if (foundMember == 0) //if member has not been found, will = 0, then write that member to master
            {
                DataRow Row = masterTable.NewRow();
                Row["member"] = cacheRow.Field<string>("member");
                Row["homeworld"] = cacheRow.Field<string>("homeworld");
                Row["state"] = cacheRow.Field<string>("state");
                Row["lastseen"] = cacheDate;
                Row["seendays"] = 40000;
                if (string.Equals(cacheRow.Field<string>("state"), "Online"))
                {
                    Row["seendays"] = 0;
                }
                Row["listname"] = cacheRow.Field<string>("listname");
                Row["cachedate"] = cacheDate;
                Row["ispresent"] = 0;
                masterTable.Rows.Add(Row);
            }
        }

        //compare master to cached to get presence
        foreach (DataRow masterRow in masterTable.Rows)
        {
            if (string.Equals(masterRow["listname"], listName))
            {
                masterRow["ispresent"] = 0;
                masterRow["state"] = "Not Found";

                foreach (DataRow cacheRow in cacheTable.Rows)
                {
                    if (string.Equals(masterRow["member"], cacheRow["member"]) && string.Equals(masterRow["homeworld"], cacheRow["homeworld"]))
                    {
                        masterRow["ispresent"] = 1;
                        masterRow["state"] = cacheRow["state"];
                    }
                }
            }
        }

        //Build Updated Master Table CSV - make column headers
        StringBuilder sb0 = new StringBuilder();
        string[] columnNames0 = masterTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        sb0.AppendLine(string.Join("\t", columnNames0));

        foreach (DataRow row in masterTable.Rows) //Populate CSV fields
        {
            string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
            sb0.AppendLine(string.Join("\t", fields));
        }

        //make CSV list of known shell names - index is the same as dates
        StringBuilder sb2 = new StringBuilder();
        string[] shellList = masterTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("listname")).Distinct().ToArray();
        sb2.AppendLine(string.Join("\t", shellList));

        //make CSV list of shell cache dates - index is the same as names
        StringBuilder sb4 = new StringBuilder();
        string[] shellListDates = masterTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
        sb4.AppendLine(string.Join("\t", shellListDates));

        //write back updated list to appropriate type
        if (string.Equals(shellType, "CWLS"))
        {
            Configuration.CWLSCSVDataVersion = Configuration.CSVDataVersion;
            Configuration.CWLSCSVData = sb0.ToString();
            Configuration.CWLSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
            Configuration.CWLSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
            Configuration.DEBUGString = "Cross-world Linkshell info written successfully.";
            Configuration.Save();
            ClampShellListIndex("CWLS");
        }
        else if (string.Equals(shellType, "LS"))
        {
            Configuration.LSCSVDataVersion = Configuration.CSVDataVersion;
            Configuration.LSCSVData = sb0.ToString();
            Configuration.LSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
            Configuration.LSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
            Configuration.DEBUGString = "Linkshell info written successfully.";
            Configuration.Save();
            ClampShellListIndex("LS");
        }
        else
        {
            Configuration.DEBUGString = "Failed to write Linkshell info.";
            return;
        }
    }

    public void RemoveShell(string removeType, string shellType, string listName, string memberName = "", string memberHomeWorld = "") //removeType = "Member" remove individual member, "Shell" remove entire shell
    {
        //do a couple of sanity checks first to get active shell list and return out if they fail
        if (string.Equals(shellType, "CWLS") && Configuration.CWLSCSVList.Equals(""))
        {
            Configuration.DEBUGString = "Cross-world Linkshell List Empty.";
            return;
        }
        if (string.Equals(shellType, "LS") && Configuration.LSCSVList.Equals(""))
        {
            Configuration.DEBUGString = "Linkshell List Empty.";
            return;
        }

        //this works by rebuilding the main csv while excluding the selected member
        DataTable removeTable = new DataTable();
        removeTable.Columns.Add("member", typeof(string));
        removeTable.Columns.Add("homeworld", typeof(string));
        removeTable.Columns.Add("state", typeof(string));
        removeTable.Columns.Add("lastseen", typeof(DateTime));
        removeTable.Columns.Add("seendays", typeof(int));
        removeTable.Columns.Add("listname", typeof(string));
        removeTable.Columns.Add("cachedate", typeof(string));
        removeTable.Columns.Add("ispresent", typeof(int));

        string[] removeLines;
        if (string.Equals(shellType, "CWLS") && Configuration.CSVDataVersion == Configuration.CWLSCSVDataVersion)
        {
            removeLines = Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }
        else if (string.Equals(shellType, "LS") && Configuration.CSVDataVersion == Configuration.LSCSVDataVersion)
        {
            removeLines = Configuration.LSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            Configuration.DEBUGString = "Failed to create removeTable.";
            return;
        }

        string[] removeFields;

        for (int i = 1; i < removeLines.GetLength(0); i++)
        {
            removeFields = removeLines[i].Split(new char[] { '\t' });
            if (string.Equals(removeType, "Member") && removeFields[5].Equals(listName) && removeFields[0].Equals(memberName) && removeFields[1].Equals(memberHomeWorld))
            {
                //throw the name you want to remove into the void
            }
            else if (string.Equals(removeType, "Shell") && removeFields[5].Equals(listName))
            {
                //throw the shell you want to remove into the void
            }
            else
            {
                DataRow Row = removeTable.NewRow();
                Row["member"] = removeFields[0];
                Row["homeworld"] = removeFields[1];
                Row["state"] = removeFields[2];
                Row["lastseen"] = removeFields[3];
                Row["seendays"] = removeFields[4];
                Row["listname"] = removeFields[5];
                Row["cachedate"] = removeFields[6];
                Row["ispresent"] = removeFields[7];
                removeTable.Rows.Add(Row);
            }
        }

        //Build Updated Master Table CSV - make column headers
        StringBuilder sb0 = new StringBuilder();
        string[] columnNames0 = removeTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        sb0.AppendLine(string.Join("\t", columnNames0));

        foreach (DataRow row in removeTable.Rows) //Populate CSV fields
        {
            string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
            sb0.AppendLine(string.Join("\t", fields));
        }

        //make CSV list of known shell names - index is the same as dates
        StringBuilder sb2 = new StringBuilder();
        string[] shellList = removeTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("listname")).Distinct().ToArray();
        sb2.AppendLine(string.Join("\t", shellList));

        //make CSV list of shell cache dates - index is the same as names
        StringBuilder sb4 = new StringBuilder();
        string[] shellListDates = removeTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
        sb4.AppendLine(string.Join("\t", shellListDates));

        //write back updated list to appropriate type
        if (string.Equals(shellType, "CWLS"))
        {
            Configuration.CWLSCSVDataVersion = Configuration.CSVDataVersion;
            Configuration.CWLSCSVData = sb0.ToString();
            Configuration.CWLSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
            Configuration.CWLSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
            Configuration.DEBUGString = "Cross-world Linkshell info removed successfully.";
            Configuration.Save();
            ClampShellListIndex("CWLS");
        }
        else if (string.Equals(shellType, "LS"))
        {
            Configuration.LSCSVDataVersion = Configuration.CSVDataVersion;
            Configuration.LSCSVData = sb0.ToString();
            Configuration.LSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
            Configuration.LSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
            Configuration.DEBUGString = "Linkshell info removed successfully.";
            Configuration.Save();
            ClampShellListIndex("LS");
        }
        else
        {
            Configuration.DEBUGString = "Failed to remove Linkshell info.";
            return;
        }
    }

    public void TEMPUpdateCWLSData() //temp method to update data from csv version 0 to 1
    {
        string shellDc = Plugin.PlayerState.CurrentWorld.Value.DataCenter.Value.Name.ToString();

        DataTable updateTable = new DataTable();
        updateTable.Columns.Add("member", typeof(string));
        updateTable.Columns.Add("homeworld", typeof(string));
        updateTable.Columns.Add("state", typeof(string));
        updateTable.Columns.Add("lastseen", typeof(DateTime));
        updateTable.Columns.Add("seendays", typeof(int));
        updateTable.Columns.Add("listname", typeof(string));
        updateTable.Columns.Add("cachedate", typeof(string));
        updateTable.Columns.Add("ispresent", typeof(int));

        string[] updateLines = Configuration.CWLSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        string[] updateFields;

        for (int i = 1; i < updateLines.GetLength(0); i++) //0 member, 1 state, 2 lastseen, 3 seendays, 4 cwls, 5 cachedate, 6 ispresent
        {
            updateFields = updateLines[i].Split(new char[] { ',' });

            string[] nameFields = updateFields[0].Split(new char[] { ' ' });

            DataRow Row = updateTable.NewRow();
            Row["member"] = nameFields[0] + " " + nameFields[1];
            Row["homeworld"] = WorldIdToName(ushort.Parse(nameFields[2]));
            Row["state"] = updateFields[1];
            Row["lastseen"] = updateFields[2];
            Row["seendays"] = updateFields[3];
            Row["listname"] = updateFields[4] + " (" + shellDc + ")";
            Row["cachedate"] = updateFields[5];
            Row["ispresent"] = updateFields[6];
            updateTable.Rows.Add(Row);
        }

        //Build Updated Master Table CSV - make column headers
        StringBuilder sb0 = new StringBuilder();
        string[] columnNames0 = updateTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        sb0.AppendLine(string.Join("\t", columnNames0));

        foreach (DataRow row in updateTable.Rows) //Populate CSV fields
        {
            string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
            sb0.AppendLine(string.Join("\t", fields));
        }

        //make CSV list of known shell names - index is the same as dates
        StringBuilder sb2 = new StringBuilder();
        string[] shellList = updateTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("listname")).Distinct().ToArray();
        sb2.AppendLine(string.Join("\t", shellList));

        //make CSV list of shell cache dates - index is the same as names
        StringBuilder sb4 = new StringBuilder();
        string[] shellListDates = updateTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
        sb4.AppendLine(string.Join("\t", shellListDates));

        Configuration.CWLSCSVDataVersion = Configuration.CSVDataVersion;
        Configuration.CWLSCSVData = sb0.ToString();
        Configuration.CWLSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
        Configuration.CWLSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
        Configuration.Save();
        ClampShellListIndex("CWLS");
    }

    public void TEMPUpdateLSData() //temp method to update data from csv version 0 to 1
    {
        DataTable updateTable = new DataTable();
        updateTable.Columns.Add("member", typeof(string));
        updateTable.Columns.Add("homeworld", typeof(string));
        updateTable.Columns.Add("state", typeof(string));
        updateTable.Columns.Add("lastseen", typeof(DateTime));
        updateTable.Columns.Add("seendays", typeof(int));
        updateTable.Columns.Add("listname", typeof(string));
        updateTable.Columns.Add("cachedate", typeof(string));
        updateTable.Columns.Add("ispresent", typeof(int));

        string[] updateLines = Configuration.LSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        string[] updateFields;

        for (int i = 1; i < updateLines.GetLength(0); i++) //0 member, 1 state, 2 lastseen, 3 seendays, 4 linkshell, 5 cachedate, 6 ispresent, 7 worldid, 8 lslist
        {
            updateFields = updateLines[i].Split(new char[] { ',' });
            DataRow Row = updateTable.NewRow();
            Row["member"] = updateFields[0];
            Row["homeworld"] = WorldIdToName(ushort.Parse(updateFields[7]));
            Row["state"] = updateFields[1];
            Row["lastseen"] = updateFields[2];
            Row["seendays"] = updateFields[3];
            Row["listname"] = updateFields[8];
            Row["cachedate"] = updateFields[5];
            Row["ispresent"] = updateFields[6];
            updateTable.Rows.Add(Row);
        }

        //Build Updated Master Table CSV - make column headers
        StringBuilder sb0 = new StringBuilder();
        string[] columnNames0 = updateTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        sb0.AppendLine(string.Join("\t", columnNames0));

        foreach (DataRow row in updateTable.Rows) //Populate CSV fields
        {
            string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
            sb0.AppendLine(string.Join("\t", fields));
        }

        //make CSV list of known shell names - index is the same as dates
        StringBuilder sb2 = new StringBuilder();
        string[] shellList = updateTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("listname")).Distinct().ToArray();
        sb2.AppendLine(string.Join("\t", shellList));

        //make CSV list of shell cache dates - index is the same as names
        StringBuilder sb4 = new StringBuilder();
        string[] shellListDates = updateTable.Rows.Cast<DataRow>().Select(r => r.Field<string>("cachedate")).Distinct().ToArray();
        sb4.AppendLine(string.Join("\t", shellListDates));

        Configuration.LSCSVDataVersion = Configuration.CSVDataVersion;
        Configuration.LSCSVData = sb0.ToString();
        Configuration.LSCSVList = sb2.ToString().Remove(sb2.ToString().Length - 2);
        Configuration.LSCSVListDate = sb4.ToString().Remove(sb4.ToString().Length - 2);
        Configuration.Save();
        ClampShellListIndex("LS");
    }

}
