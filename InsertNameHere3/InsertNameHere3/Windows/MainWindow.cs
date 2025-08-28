using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace InsertNameHere3.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;

    public MainWindow(Plugin plugin) : base(
        "你內心陰暗", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(100, 50),
            MaximumSize = new Vector2(100, 50),
            //MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        //this.GoatImage = goatImage;
        this.Plugin = plugin;
    }

    public void Dispose()
    {
        //this.GoatImage.Dispose();
    }

    public override void Draw()
    {
        //ImGui.Text($"The random config bool is {this.Plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        //if (ImGui.Button("Show Settings"))
        //{
        //    this.Plugin.DrawConfigUI();
        //}

        //ImGui.Spacing();

        //ImGui.Text("Have a goat:");
        //ImGui.Indent(55);
        //ImGui.Image(this.GoatImage.ImGuiHandle, new Vector2(this.GoatImage.Width, this.GoatImage.Height));
        //ImGui.Unindent(55);
        ImGui.Text("你是壞孩子");
    }
}
