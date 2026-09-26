using ImGuiNET;
using vikistrike.cannon;

namespace vikistrike.window;

public class CannonWindow(CannonController cc, DesignatorWindow designator) {
    public enum State {
        Idle,
        Aiming,
        ReadyToFire,
    }
    
    private string _status = "idle";
    private int _pwdrC = 1;
    private State _state = State.Idle;
    private CannonController.FiringSolution _solution = CannonController.FiringSolution.FullSend;
    
    public void DrawImGui() {
        if (_state == State.Aiming && cc.Ready) _state = State.ReadyToFire;
        if (ImGui.Begin("Cannon controller", ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.Text($"available firepower: {cc.Cannons.Count} connected cannon(s)");
            ImGui.Text(_state != State.Aiming ? $"status: {_status}" : $"status: aiming cannons ({(cc.Ready ? "ready" : "not ready")})");
            ImGui.Separator();
            ImGui.SliderInt("Powder count", ref _pwdrC, 1, 12);
            if (ImGui.BeginCombo("Solution", _solution.ToString())) {
                if (ImGui.Selectable("Full send")) _solution = CannonController.FiringSolution.FullSend;
                ImGui.EndCombo();
            }
            
            if (ImGui.Button("Take aim")) {
                if (_state == State.Idle) {
                    if (designator.Targets is null) {
                        _status = "no targets";
                    }
                    else {
                        CannonController.AimResult res = cc.TakeAim(designator.Targets, _pwdrC);
                        _status = res switch {
                            CannonController.AimResult.CountMismatch => "mismatch",
                            CannonController.AimResult.DisconnectedCannon => "disconnected",
                            CannonController.AimResult.OutOfRange => "out of range",
                            CannonController.AimResult.SendOk => "ok",
                            _ => _status
                        };
                        
                        if (res == CannonController.AimResult.SendOk)
                            _state = State.Aiming;
                    }
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Fire")) {
                if (_state == State.ReadyToFire) {
                    cc.Fire(_solution);
                    _state = State.Idle;
                    _status = "idle";
                    Program.SetLatestMsg($"fired {cc.Cannons.Count} cannons with solution {_solution}");
                } else if (_state == State.Aiming) {
                    cc.Fire(_solution, 0, 2000, true);
                    _state = State.Idle;
                    _status = "idle";
                    Program.SetLatestMsg($"force fired {cc.Cannons.Count} cannons with solution {_solution}");
                }
            }
        }
        ImGui.End();
    }
}