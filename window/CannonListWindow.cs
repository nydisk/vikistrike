using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using vikistrike.cannon;
using Rl = Raylib_cs.Raylib;
namespace vikistrike.window;

public class CannonListWindow(CannonController controller) {
    private float? _activeRange = null;
    private Vector2 _cannonPos;
    private int _charges = 1;
    private int _y = 64;
    public void DrawImGui() {
        if (ImGui.Begin("Cannon list", ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.SliderInt("Powder count", ref _charges, 1, 100);
            ImGui.SliderInt("Target Y", ref _y, -64, 320);
            lock (controller.@Lock) {
                foreach (Cannon c in controller.Cannons) {
                    if (!ImGui.TreeNode(c.Position.ToString())) continue;
                    ImGui.Text($"Length: {c.Length}");

                    if (c.ResolvedYawHigh is not null) ImGui.Text($"Resolved Yaw (H): {(float)c.ResolvedYawHigh}");
                    if (c.ResolvedPitchHigh is not null)
                        ImGui.Text($"Resolved Pitch (H): {(float)c.ResolvedPitchHigh}");
                    if (c.ResolvedYawLow is not null) ImGui.Text($"Resolved Yaw (L): {(float)c.ResolvedYawLow}");
                    if (c.ResolvedPitchLow is not null) ImGui.Text($"Resolved Pitch (L): {(float)c.ResolvedPitchLow}");

                    if (ImGui.Button("Teleport")) {
                        Program.Teleport(c.Position.X, c.Position.Z);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Show range")) {
                        _activeRange = c.GetRangeApproximation(_charges, _y);
                        _cannonPos = new Vector2(c.Position.X, c.Position.Z);
                    }

                    ImGui.TreePop();
                }
            }
        }
        ImGui.End();
    }

    public void DrawCircle() {
        if (_activeRange is not null) {
            Rl.DrawCircle((int)_cannonPos.X, (int)_cannonPos.Y, (float)_activeRange!, Rl.Fade(Color.Yellow, 0.1f));
            Rl.DrawCircleLines((int)_cannonPos.X, (int)_cannonPos.Y, (float)_activeRange!, Color.Yellow);
        }
            
    }
}