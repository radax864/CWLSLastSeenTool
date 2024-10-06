using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace CWLSLastSeenTool.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("CWLS Last Seen Tool Config") //("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(340, 220); //width x heght default 320 200
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
    }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var useworldnamesValue = Configuration.UseWorldNames;
        if (ImGui.Checkbox("Use World Names Over World ID", ref useworldnamesValue))
        {
            Configuration.UseWorldNames = useworldnamesValue;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            Configuration.Save();
        }

        ImGui.Text("Hold CTRL for CWLSCSVMaster Commands");

        ImGui.Spacing();

        if (ImGui.Button("CLEAR CWLSCSVMaster") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.CWLSCSVMasterDate = "CWLS CSV Cleared";
            Configuration.CWLSCSVMaster = "";
            Configuration.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button("RESTORE CWLSCSVMaster") && ImGui.GetIO().KeyCtrl)
        {
            Configuration.CWLSCSVMasterDate = "Backup Restored";//Configuration.CWLSCSVMasterBackupDate"";
            Configuration.CWLSCSVMaster = Configuration.CWLSCSVMasterBackup;
            Configuration.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button("WRITE CWLSCSVMasterBackup") && ImGui.GetIO().KeyCtrl)
        {
            DateTime buDate = DateTime.Now;
            Configuration.CWLSCSVMasterBackupDate = buDate.ToString();
            Configuration.CWLSCSVMasterBackup = Configuration.CWLSCSVMaster;
            Configuration.Save();
        }
        ImGui.Text($"Last Backup: {Configuration.CWLSCSVMasterBackupDate}");

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
