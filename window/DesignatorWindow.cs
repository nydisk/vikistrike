using ImGuiNET;
using System.Numerics;
using fNbt;
using Raylib_cs;
using vikistrike.anvil;
using vikistrike.cannon;
using vikistrike.math;
using Rl = Raylib_cs.Raylib;

namespace vikistrike.window;

public class DesignatorWindow(CannonController cc, Anvil anvil) {
    public enum TargetPlacement {
        Scatter
    }
    
    private Vector2 _rectP1;
    private Vector2 _rectP2;
    private int _settingPoint = -1;
    private TargetPlacement _targetPlacementMode = TargetPlacement.Scatter;
    public Vec3i[]? Targets { get; private set; }

    public void DrawImGui() {
        if (ImGui.Begin("Target selector", ImGuiWindowFlags.AlwaysAutoResize)) {
            if (ImGui.Button("Select target area")) {
                Targets = null;
                _settingPoint = 0;
            }
            
            ImGui.Text($"X1={(int)_rectP1.X}, Z1={(int)_rectP1.Y}");
            ImGui.Text($"X2={(int)_rectP2.X}, Z2={(int)_rectP2.Y}");

            if (ImGui.BeginCombo("Target placement", _targetPlacementMode.ToString())) {
                if (ImGui.Selectable("Scatter")) _targetPlacementMode = TargetPlacement.Scatter;
                ImGui.EndCombo();
            }

            if (ImGui.Button($"Place targets ({cc.Cannons.Count})")) {
                Targets = Cascade(GenerateTargetsXZ(cc.Cannons.Count));
                Program.SetLatestMsg($"placed {cc.Cannons.Count} targets");
            }

            if (ImGui.TreeNode("Targets")) {
                if (Targets is not null) 
                    foreach (Vector3 target in Targets) 
                        ImGui.Text(target.ToString());
                ImGui.TreePop();
            }
        }
        ImGui.End();
    }
    
    public void Update(ref Camera2D cam) {
        Vector2 worldMouse = Rl.GetScreenToWorld2D(Rl.GetMousePosition(), cam);
        switch (_settingPoint) {
            case -1: return;
            case 0: {
                if (Rl.IsMouseButtonPressed(MouseButton.Left)) _settingPoint = 1;
                _rectP1 = Vector2.Round(worldMouse, MidpointRounding.ToNegativeInfinity);
                break;
            }
            default: {
                if (Rl.IsMouseButtonPressed(MouseButton.Left)) _settingPoint = -1;
                _rectP2 = Vector2.Round(worldMouse, MidpointRounding.ToNegativeInfinity);
                break;
            }
        }
    }

    public void Render() {
        if (_rectP1 != _rectP2 && _settingPoint != 0) {
            DrawDesignatorRectangle(_rectP1, _rectP2, Rl.Fade(Color.Maroon, 0.1f), true);
            DrawDesignatorRectangle(_rectP1, _rectP2, Color.Maroon, false);
        }

        if (Targets is not null) 
            foreach (Vector3 target in Targets) 
                Rl.DrawRectangle((int)target.X - 1, (int)target.Z - 1, 3, 3, Color.Red);
        
    }

    private static void DrawDesignatorRectangle(Vector2 a, Vector2 b, Color color, bool filled) {
        int x = (int)Math.Min(a.X, b.X);   
        int y = (int)Math.Min(a.Y, b.Y);
        int w = (int)Math.Abs(a.X - b.X);
        int h = (int)Math.Abs(a.Y - b.Y);
        
        if (!filled) Rl.DrawRectangleLines(x, y, w, h, color);
        else Rl.DrawRectangle(x, y, w, h, color);
    }
    
    // ReSharper disable once InconsistentNaming
    private Vector2[] GenerateTargetsXZ(int count) {
        float minX = Math.Min(_rectP1.X, _rectP2.X);
        float maxX = Math.Max(_rectP1.X, _rectP2.X);
        float minZ = Math.Min(_rectP1.Y, _rectP2.Y);
        float maxZ = Math.Max(_rectP1.Y, _rectP2.Y);

        int cols = (int)MathF.Ceiling(MathF.Sqrt(count));
        int rows = (int)MathF.Ceiling(count / (float)cols);

        float cellW = (maxX - minX) / cols;
        float cellH = (maxZ - minZ) / rows;

        var targets = new List<Vector2>();
        for (int r = 0; r < rows && targets.Count < count; r++) {
            for (int c = 0; c < cols && targets.Count < count; c++) {
                float x = minX + c * cellW + (float)Random.Shared.NextDouble() * cellW;
                float z = minZ + r * cellH + (float)Random.Shared.NextDouble() * cellH;
                targets.Add(new Vector2(x, z));
            }
        }

        return targets.ToArray();
    }

    private Vec3i[] Cascade(Vector2[] targets) {
        Dictionary<(int, int), block_id[]?> chunks = [];
        foreach (Vector2 t in targets) {
            (int cx, int cz) = ((int)t.X >> 4, (int)t.Y >> 4);
            if (!chunks.ContainsKey((cx, cz))) {
                NbtCompound? c = anvil.Chunk(cx, cz);
                chunks[(cx, cz)] = c is null ? null : new AnvilReader(c).GetBlocksInChunk();
            }
        }

        var result = new Vec3i[targets.Length];
        for (int i = 0; i < targets.Length; i++) {
            Vector2 t = targets[i];
            (int cx, int cz) = ((int)t.X >> 4, (int)t.Y >> 4);
            block_id[]? blocks = chunks[(cx, cz)];
            if (blocks is null) throw new Exception("no chunk");

            int lx = (int)t.X & 15;
            int lz = (int)t.Y & 15;

            int groundY = -64;
            for (int y = 319; y >= -64; y--) {
                int idx = (y + 64) * 256 + lz * 16 + lx;
                if (blocks[idx] == 0) continue;
                groundY = y;
                break;
            }

            result[i] = new Vec3i((int)t.X, groundY, (int)t.Y);
        }

        return result;
    }
}