// ReSharper disable once BuiltInTypeReferenceStyle
global using block_id = System.UInt16;
using System.Numerics;
using ImGuiNET;
using NativeFileDialogSharp;
using Raylib_cs;
using rlImGui_cs;
using vikistrike.anvil;
using vikistrike.cannon;
using vikistrike.map;
using vikistrike.window;
using Rl = Raylib_cs.Raylib;

namespace vikistrike;

public static class Program
{
    private static string _latestMsg = "Idle";
    private static Camera2D _camera;

    [STAThread]
    public static void Main(string[] args) {
        if (args.Length < 1) {
            Console.WriteLine($"Usage: vikistrike <path to mc world folder>"); //TODO: add GUI prompt instead of requiring command-line input
            return;
        }

        Rl.InitWindow(1920, 1080, "vikistrike");
        Rl.SetTraceLogLevel(TraceLogLevel.Warning);
        Rlgl.SetClipPlanes(0.01, 10000);
        rlImGui.Setup(true, true);
        Rl.SetExitKey(0);
        unsafe { ImGui.GetIO().NativePtr->IniFilename = null; }

        _camera = new Camera2D {
            Offset = new Vector2(1920 / 2f, 1080 / 2f),
            Target = Vector2.Zero,
            Rotation = 0f,
            Zoom = 3f
        };

        Anvil anvil = new(args[0]);

        CannonController controller = new();

        MapRenderer map = new(anvil, controller);

        DesignatorWindow wDesignator = new(controller, anvil);
        CannonWindow wCannon = new(controller, wDesignator);
        CannonListWindow wCannonList = new(controller);

        controller.Start();

        bool dragging = false;
        while (!Rl.WindowShouldClose()) {
            dragging = dragging switch {
                false when Rl.IsMouseButtonDown(MouseButton.Right) => true,
                true when Rl.IsMouseButtonReleased(MouseButton.Right) => false,
                _ => dragging
            };

            if (dragging) {
                _camera.Target -= Rl.GetMouseDelta() / _camera.Zoom;
            }

            float scroll = Rl.GetMouseWheelMove();
            if (scroll != 0) {
                Vector2 mouseWorld = Rl.GetScreenToWorld2D(Rl.GetMousePosition(), _camera);
                _camera.Zoom = Math.Clamp(_camera.Zoom + scroll * 0.25f, 0.1f, 20f);
                Vector2 mouseWorldAfter = Rl.GetScreenToWorld2D(Rl.GetMousePosition(), _camera);
                _camera.Target += mouseWorld - mouseWorldAfter;
            }

            wDesignator.Update(ref _camera);

            Rl.BeginDrawing();
            Rl.ClearBackground(Color.Black);
            Rl.BeginMode2D(_camera);
            MapRenderer.RenderXaero(ref _camera);
            map.RenderCannons();
            wDesignator.Render();
            wCannonList.DrawCircle();
            Rl.EndMode2D();

            Rl.DrawFPS(8, 24);
            Rl.DrawText(_latestMsg, 8, Rl.GetRenderHeight() - 20 - 8, 20, Color.White);

            rlImGui.Begin();
            ImGuiMenuBar();
            wDesignator.DrawImGui();
            wCannonList.DrawImGui();
            wCannon.DrawImGui();
            ImTeleportMenu();
            rlImGui.End();
            Rl.EndDrawing();
        }
    }

    public static void SetLatestMsg(string msg) {
        _latestMsg = msg;
    }

    private static void ImGuiMenuBar() {
        if (ImGui.BeginMainMenuBar()) {
            ImXaeroMenu();
            ImGui.EndMainMenuBar();
        }
    }

    private static void ImXaeroMenu() {
        if (ImGui.BeginMenu("Xaero")) {
            if (ImGui.MenuItem("Import map fragment(s)...")) {
                DialogResult? res = Dialog.FileOpenMultiple("*.png", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                if (res is not null && res.IsOk) {
                    foreach (string path in res.Paths) XaeroLoader.LoadFragment(path);
                    _latestMsg = $"loaded {res.Paths.Count} fragment(s) successfully";
                }
            }
            ImGui.EndMenu();
        }
    }

    private static readonly int[] TpPos = new int[2];
    private static void ImTeleportMenu() {
        if (ImGui.Begin("Teleport", ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.InputInt2("Coords (X, Z)", ref TpPos[0]);
            if (ImGui.Button("Teleport")) {
                Teleport(TpPos[0], TpPos[1]);
            }
        }
        ImGui.End();
    }

    public static void Teleport(int x, int z) {
        _camera.Target = new Vector2(x, z);
    }
}
