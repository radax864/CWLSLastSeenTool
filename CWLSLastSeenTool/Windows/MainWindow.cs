using System;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using CWLSLastSeenTool.Utils;

namespace CWLSLastSeenTool.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private ShellTools ShellTools;
    private int memberCount = 0;
    private int onlineCount = 0;

    public MainWindow(Plugin plugin)
        : base("Linkshell Tools") //, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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

    private void DrawCWLSTabContents()
    {
        ImGui.Spacing();

        //check for empty strings that will cause things to break
        if (Configuration.CWLSCSVDataVersion == Configuration.CSVDataVersion && !Configuration.CWLSCSVList.Equals("") && !Configuration.CWLSCSVListDate.Equals("") && !Configuration.CWLSCSVData.Equals(""))
        {
            //make list of cwls names for drop down
            string[] CWLSList;
            CWLSList = Configuration.CWLSCSVList.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

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
                        bool isselected = Configuration.CWLSListIndex == i;
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

            ImGuiTableFlags flags = ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable;

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
                            if (int.Parse(Fields[7]) == 1) //member
                            {
                                ImGui.Text($"{Fields[0]} ({Fields[1]})");
                                memberCount++;
                            }
                            else
                            {
                                ImGui.TextColored(vectRed, $"{Fields[0]} ({Fields[1]})");
                            }
                            ImGui.TableNextColumn();
                            if (string.Equals(Fields[2], "Online")) //state
                            {
                                ImGui.TextColored(vectGreen, $"{Fields[2]}");
                                onlineCount++;
                            }
                            else
                            {
                                ImGui.TextColored(vectRed, $"{Fields[2]}");
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text($"{Fields[3].Substring(0, 10)}"); //lastseen
                            ImGui.TableNextColumn();
                            if (int.Parse(Fields[4]) == 40000) //seendays
                            {
                                ImGui.Text("Never Seen");
                            }
                            else
                            {
                                ImGui.Text($"{Fields[4]}");
                            }
                            ImGui.TableNextColumn();
                            if (ImGui.SmallButton($"Remove##{i}") && ImGui.GetIO().KeyCtrl) //remove member button
                            {
                                ShellTools.RemoveShell("Member", "CWLS", Fields[5], Fields[0], Fields[1]);
                            }
                        }
                    }
                }
            }
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

        //check for empty strings that will cause things to break
        if (Configuration.LSCSVDataVersion == Configuration.CSVDataVersion && !Configuration.LSCSVList.Equals("") && !Configuration.LSCSVListDate.Equals("") && !Configuration.LSCSVData.Equals(""))
        {
            //make list of ls names for drop down
            string[] LSList = Configuration.LSCSVList.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

            //make list of cache dates to display next to cwls list drop down
            string[] LSListDate = Configuration.LSCSVListDate.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

            ImGui.SetNextItemWidth(300.0f);

            using (var lspicklist = ImRaii.Combo("##ls_picklist", LSList[Configuration.LSListIndex]))
            {
                if (lspicklist)
                {
                    for (int i = 0; i < LSList.Length; i++)
                    {
                        bool isselected = Configuration.LSListIndex == i;
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

            ImGui.Text($"Members: {memberCount}/128 ({onlineCount} Online)");

            ImGui.SameLine();
            ImGui.Spacing();

            ImGui.SameLine();
            ImGui.Text($"Last Cached: {LSListDate[Configuration.LSListIndex]}");

            ImGui.Spacing();

            //start making the display table
            string[] Lines;
            Lines = Configuration.LSCSVData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] Fields;

            Vector4 vectRed = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Vector4 vectGreen = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);

            ImGuiTableFlags flags = ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable;

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
                            if (int.Parse(Fields[7]) == 1) //member
                            {
                                ImGui.Text($"{Fields[0]}");
                                memberCount++;
                            }
                            else
                            {
                                ImGui.TextColored(vectRed, $"{Fields[0]}");
                            }
                            ImGui.TableNextColumn();
                            if (string.Equals(Fields[2], "Online")) //state
                            {
                                ImGui.TextColored(vectGreen, $"{Fields[2]}");
                                onlineCount++;
                            }
                            else
                            {
                                ImGui.TextColored(vectRed, $"{Fields[2]}");
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text($"{Fields[3].Substring(0, 10)}"); //lastseen
                            ImGui.TableNextColumn();
                            if (int.Parse(Fields[4]) == 40000) //seendays
                            {
                                ImGui.Text("Never Seen");
                            }
                            else
                            {
                                ImGui.Text($"{Fields[4]}");
                            }
                            ImGui.TableNextColumn();
                            if (ImGui.SmallButton($"Remove##{i}") && ImGui.GetIO().KeyCtrl) //remove member button
                            {
                                ShellTools.RemoveShell("Member", "LS", Fields[5], Fields[0], Fields[1]);
                            }
                        }
                    }
                }
            }
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
