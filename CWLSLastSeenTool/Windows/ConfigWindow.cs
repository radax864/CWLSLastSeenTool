using System;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using CWLSLastSeenTool.Utils;

namespace CWLSLastSeenTool.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private ShellTools ShellTools;
    private int listIndexCWLS = 0;
    private int listIndexLS = 0;

    public ConfigWindow(Plugin plugin) : base("Linkshell Tools Advanced Options")
    {
        Flags = ImGuiWindowFlags.NoCollapse; //| ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        //Size = new Vector2(440, 320); //width x heght default 340 220
        //SizeCondition = ImGuiCond.Always;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 340),
            MaximumSize = new Vector2(480, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
        ShellTools = new ShellTools(plugin);
    }

    public void Dispose()
    {
        ShellTools.Dispose();
    }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
    }

    private void DrawCWLSOptions()
    {
        ImGui.Spacing();

        ImGui.Text("Advanced Actions (Hold CTRL)");
        ImGui.Separator();
        ImGui.Spacing();

        if (Plugin.dataReadyCWLS == 1)
        {
            ImGui.SetNextItemWidth(200.0f);

            using (var cwlspicklist = ImRaii.Combo("##cwls_picklist", Plugin.listNamesCWLS[listIndexCWLS]))
            {
                if (cwlspicklist)
                {
                    for (int i = 0; i < Plugin.listNamesCWLS.Length; i++)
                    {
                        bool isselected = listIndexCWLS == i;
                        if (ImGui.Selectable(Plugin.listNamesCWLS[i], isselected))
                        {
                            listIndexCWLS = i;
                        }
                        if (isselected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Remove List") && ImGui.GetIO().KeyCtrl)
            {
                ShellTools.RemoveShell("Shell", "CWLS", Plugin.listNamesCWLS[listIndexCWLS]);
                listIndexCWLS = 0;
            }
            ImGui.SameLine();

            if (ImGui.Button("Merge/Update List") && ImGui.GetIO().KeyCtrl)
            {
                ShellTools.CacheShell("CWLS", "Merge", listIndexCWLS);
                listIndexCWLS = 0;
            }
            
            ImGui.Spacing();
            ImGui.TextWrapped("Remove List: Removes all entries from the selected list.");
            ImGui.Spacing();
            ImGui.TextWrapped("Merge/Update List: Updates the name of the selected CWLS list and caches its members against the currently opened Cross-world Linkshell. This will fail if the Cross-world Linkshell has already been cached/exists in the CWLS list.");
            ImGui.Spacing();
        }
        else
        {
            ImGui.TextWrapped("No CWLS Have Been Cached");
            listIndexCWLS = 0;
            listIndexLS = 0;
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Text("Backup and Restore (Hold CTRL)");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Clear Active CWLS Data") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.CWLSCSVDataVersion = 0;
            Configuration.CWLSCSVData = "";
            Configuration.CWLSCSVList = "";
            Configuration.CWLSCSVListDate = "";
            Configuration.Save();
            ShellTools.RefreshDisplayData("CWLS");
        }

        ImGui.Spacing();

        if (ImGui.Button("Restore CWLS Data Internal Backup") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.CWLSCSVDataVersion = Configuration.CWLSCSVDataVersionBACKUP;
            Configuration.CWLSCSVData = Configuration.CWLSCSVDataBACKUP;
            Configuration.CWLSCSVList = Configuration.CWLSCSVListBACKUP;
            Configuration.CWLSCSVListDate = Configuration.CWLSCSVListDateBACKUP;
            Configuration.Save();
            ShellTools.ClampShellListIndex("CWLS");
            ShellTools.RefreshDisplayData("CWLS");
        }

        ImGui.Spacing();

        if (ImGui.Button("Create CWLS Data Internal Backup") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.CWLSCSVDataVersionBACKUP = Configuration.CWLSCSVDataVersion;
            Configuration.CWLSCSVDataBACKUP = Configuration.CWLSCSVData;
            Configuration.CWLSCSVListBACKUP = Configuration.CWLSCSVList;
            Configuration.CWLSCSVListDateBACKUP = Configuration.CWLSCSVListDate;
            Configuration.Save();
        }

        ImGui.Spacing();

        if (!Configuration.CWLSCSVListBACKUP.Equals("") && !Configuration.CWLSCSVListDateBACKUP.Equals(""))
        {
            string[] backupnames;
            backupnames = Configuration.CWLSCSVListBACKUP.Split(new char[] { '\t' });
            string[] backupdates;
            backupdates = Configuration.CWLSCSVListDateBACKUP.Split(new char[] { '\t' });

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
    }

    private void DrawLSOptions()
    {
        ImGui.Spacing();

        ImGui.Text("Advanced Actions (Hold CTRL)");
        ImGui.Separator();
        ImGui.Spacing();

        if (Plugin.dataReadyLS == 1)
        {
            ImGui.SetNextItemWidth(200.0f);

            using (var lspicklist = ImRaii.Combo("##ls_picklist", Plugin.listNamesLS[listIndexLS]))
            {
                if (lspicklist)
                {
                    for (int i = 0; i < Plugin.listNamesLS.Length; i++)
                    {
                        bool isselected = listIndexLS == i;
                        if (ImGui.Selectable(Plugin.listNamesLS[i], isselected))
                        {
                            listIndexLS = i;
                        }
                        if (isselected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Remove List") && ImGui.GetIO().KeyCtrl)
            {
                ShellTools.RemoveShell("Shell", "LS", Plugin.listNamesLS[listIndexLS]);
                listIndexLS = 0;
            }
            ImGui.SameLine();

            if (ImGui.Button("Merge/Update List") && ImGui.GetIO().KeyCtrl)
            {
                ShellTools.CacheShell("LS", "Merge", listIndexLS);
                listIndexLS = 0;
            }
            
            ImGui.Spacing();
            ImGui.TextWrapped("Remove List: Removes all entries from the selected list.");
            ImGui.Spacing();
            ImGui.TextWrapped("Merge/Update List: Updates the name of the selected list and caches its members against the currently opened Linkshell. This will fail if the Linkshell has already been cached/exists in the Linkshell list.");
            ImGui.Spacing();
        }
        else
        {
            ImGui.TextWrapped("No Linkshells Have Been Cached");
            listIndexCWLS = 0;
            listIndexLS = 0;
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Text("Backup and Restore (Hold CTRL)");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Clear Active Linkshell Data") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.LSCSVDataVersion = 0;
            Configuration.LSCSVData = "";
            Configuration.LSCSVList = "";
            Configuration.LSCSVListDate = "";
            Configuration.Save();
            ShellTools.RefreshDisplayData("LS");
        }

        ImGui.Spacing();

        if (ImGui.Button("Restore Linkshell Data Internal Backup") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.LSCSVDataVersion = Configuration.LSCSVDataVersionBACKUP;
            Configuration.LSCSVData = Configuration.LSCSVDataBACKUP;
            Configuration.LSCSVList = Configuration.LSCSVListBACKUP;
            Configuration.LSCSVListDate = Configuration.LSCSVListDateBACKUP;
            Configuration.Save();
            ShellTools.ClampShellListIndex("LS");
            ShellTools.RefreshDisplayData("LS");
        }

        ImGui.Spacing();

        if (ImGui.Button("Create Linkshell Data Internal Backup") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.LSCSVDataVersionBACKUP = Configuration.LSCSVDataVersion;
            Configuration.LSCSVDataBACKUP = Configuration.LSCSVData;
            Configuration.LSCSVListBACKUP = Configuration.LSCSVList;
            Configuration.LSCSVListDateBACKUP = Configuration.LSCSVListDate;
            Configuration.Save();
        }

        ImGui.Spacing();

        if (!Configuration.LSCSVListBACKUP.Equals("") && !Configuration.LSCSVListDateBACKUP.Equals(""))
        {
            string[] backupnames;
            backupnames = Configuration.LSCSVListBACKUP.Split(new char[] { '\t' });
            string[] backupdates;
            backupdates = Configuration.LSCSVListDateBACKUP.Split(new char[] { '\t' });

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

    private void DrawDebugTabContents()
    {
        ImGui.Spacing();

        ImGui.Text("Debug");
        ImGui.Separator();
        ImGui.Spacing();

        // can't ref a property, so use a local copy
        // var useworldnamesValue = Configuration.UseWorldNames;
        // if (ImGui.Checkbox("Show World Name Instead Of World ID", ref useworldnamesValue))
        // {
        //     Configuration.UseWorldNames = useworldnamesValue;
        //     // can save immediately on change, if you don't want to provide a "Save and Close" button
        //     Configuration.Save();
        // }
        // ImGui.Spacing();

        // var ShowDEBUGString = Configuration.ShowDEBUGString;
        // if (ImGui.Checkbox("Show DEBUG String on Main Window", ref ShowDEBUGString))
        // {
        //     Configuration.ShowDEBUGString = ShowDEBUGString;
        //     Configuration.Save();
        // }

        // ImGui.Spacing();

        ImGui.Spacing();

        ImGui.Text($"DEBUG String: {Configuration.DEBUGString}");
        ImGui.Text($"DEBUG Int0: {Configuration.DEBUGInt0}");
        ImGui.Text($"DEBUG Int1: {Configuration.DEBUGInt1}");
        ImGui.Text($"DEBUG Int2: {Configuration.DEBUGInt2}");
        ImGui.Text($"DEBUG Int3: {Configuration.DEBUGInt3}");

        if (ImGui.Button("CLEAR DEBUG VALUES"))
        {
           Configuration.DEBUGString = "";
           Configuration.DEBUGInt0 = 0;
           Configuration.DEBUGInt1 = 0;
           Configuration.DEBUGInt2 = 0;
           Configuration.DEBUGInt3 = 0;
           Configuration.Save();
        }
    }

    public override void OnOpen()
    {
        listIndexCWLS = 0;
        listIndexLS = 0;
    }

    public override void Draw()
    {
        ImGui.Spacing();

        using (var ConfigTabBar = ImRaii.TabBar("config window tab bar"))
        {
            if (ImGui.BeginTabItem("Cross-world Options"))
            {
                DrawCWLSOptions();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Home World Options"))
            {
                DrawLSOptions();
                ImGui.EndTabItem();
            }
            if (Configuration.EnableDEBUGInfo)
            {
                if(ImGui.BeginTabItem("Debug Info"))
                {
                    DrawDebugTabContents();
                    ImGui.EndTabItem();
                }
            }
        }
    }

}
