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
    public ConfigWindow(Plugin plugin) : base("CWLS Last Seen Tool Config/Debug") //("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(300, 400); //width x heght
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (Configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        //var configValue = Configuration.SomePropertyToBeSavedAndWithADefault;
        //if (ImGui.Checkbox("Random Config Bool", ref configValue))
        //{
        //    Configuration.SomePropertyToBeSavedAndWithADefault = configValue;
        //    // can save immediately on change, if you don't want to provide a "Save and Close" button
        //    Configuration.Save();
        //}

        if (ImGui.Button("RESTORE CWLSCSVMaster"))
        {
            Configuration.CWLSCSVMaster = Configuration.CWLSCSVMasterBackup;
            Configuration.Save();
        }

        //ImGui.SameLine();

        //if (ImGui.Button("CLEAR CWLSCSVCache"))
        //{
        //    Configuration.CWLSCSVCache = "";
        //    Configuration.Save();
        //}

        //ImGui.SameLine();

        if (ImGui.Button("CLEAR CWLSCSVMaster"))
        {
            Configuration.CWLSCSVMaster = "";
            Configuration.Save();
        }

        var movable = Configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            Configuration.IsConfigWindowMovable = movable;
            Configuration.Save();
        }

        if (ImGui.Button("WRITE CWLSCSVMasterBackup"))
        {
            Configuration.CWLSCSVMasterBackup = Configuration.CWLSCSVMaster;
            Configuration.Save();
        }

        ImGui.Text($"DEBUG STEPS: {Configuration.DEBUGString}");
        ImGui.Text($"DEBUG Int0: {Configuration.DEBUGInt0}"); //member cached to table
        ImGui.Text($"DEBUG Int1: {Configuration.DEBUGInt1}");
        ImGui.Text($"DEBUG Int2: {Configuration.DEBUGInt2}");
        ImGui.Text($"DEBUG Int3: {Configuration.DEBUGInt3}");

        if (ImGui.Button("CLEAR DEBUG"))
        {
            Configuration.DEBUGString = "";
            Configuration.DEBUGInt0 = 0;
            Configuration.DEBUGInt1 = 0;
            Configuration.DEBUGInt2 = 0;
            Configuration.DEBUGInt3 = 0;
            Configuration.Save();
        }
    }
}
