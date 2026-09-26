using System.Numerics;
using System.Text.Json;
using Fleck;
using vikistrike.math;

namespace vikistrike.cannon;

public class CannonController(string url = "ws://0.0.0.0:858") {
    public enum FiringSolution {
        FullSend
    }

    public enum AimResult {
        CountMismatch,
        OutOfRange,
        SendOk,
        DisconnectedCannon
    }
    
    public List<Cannon> Cannons { get; } = [];
    private readonly WebSocketServer _server = new(url);
    public bool Ready { get; private set; } = false;
    private IWebSocketConnection? _arrayMaster;
    public Lock @Lock { get; } = new();
    public void Start() {
        _server.Start(sock => {
            sock.OnOpen = () => { };
            sock.OnClose = () => {
                Ready = false;
                lock (@Lock) Cannons.Clear();
                if (sock == _arrayMaster) _arrayMaster = null;
            };
            sock.OnMessage = msg => HandleMessage(sock, msg);
        });
    }
    
    public AimResult TakeAim(Vec3i[] targets, int pwdrCount) {
        if (targets.Length != Cannons.Count) 
            return AimResult.CountMismatch;
        if (_arrayMaster is null)
            return AimResult.DisconnectedCannon;

        Ready = false;

        for (int i = 0; i < targets.Length; i++) {
            Cannon c = Cannons[i];
            Vec3i t = targets[i];

            if (!c.Resolve((Vector3)t, pwdrCount)) return AimResult.OutOfRange;
            
            Console.WriteLine($"hi: {c.ResolvedPitchHigh}");
            Console.WriteLine($"lo: {c.ResolvedPitchLow}");
            
            float yaw = (float)(c.ResolvedYawHigh ?? c.ResolvedYawLow)!;
            float pitch = (float)(c.ResolvedPitchHigh ?? c.ResolvedPitchLow)!;
            
            Console.WriteLine($"cannon {c.Position} ({i}) is aiming to {yaw}, {pitch}");
        }
        
        var aims = Cannons.Select((c, i) => {
            float yaw = (float)(c.ResolvedYawHigh ?? c.ResolvedYawLow)!;
            float pitch = (float)(c.ResolvedPitchHigh ?? c.ResolvedPitchLow)!;
            return $$$"""{"id":{{{i}}},"yaw":{{{yaw}}},"pitch":{{{pitch}}}}""";
        });

        _arrayMaster.Send($$"""{"type":"aim","aims":[{{string.Join(',', aims)}}]}""");
        
        Console.WriteLine($"instructions have been sent");
        return AimResult.SendOk;
    }
    
    public void Fire(FiringSolution solution, int delayMin = 0, int delayMax = 2000, bool force = false) {
        if ((!Ready && !force) || _arrayMaster is null) return;
        
        Console.WriteLine($"attempting to fire, mode={solution}");

        switch (solution) {
            case FiringSolution.FullSend: {
                _arrayMaster.Send($$"""{"type":"fire"}""");
                break;
            }
        }

        Ready = false;
        Console.WriteLine($"all cannons have been reset");
    }
    
    private void HandleMessage(IWebSocketConnection sock, string message) {
        using JsonDocument doc = JsonDocument.Parse(message);
        JsonElement root = doc.RootElement;
        string? type = root.GetProperty("type").GetString();
        Console.WriteLine(message);
        switch (type) {
            case "awake": {
                _arrayMaster = sock;
                Console.WriteLine($"connected to master");
                return;
            }
            case "register": {
                int x = root.GetProperty("x").GetInt32();
                int y = root.GetProperty("y").GetInt32();
                int z = root.GetProperty("z").GetInt32();
                int by = root.GetProperty("by").GetInt32();

                int index = Cannons.Count;
                lock (@Lock) {
                    Cannons.Add(new Cannon(new Vec3i(x, y, z), 17, by));
                }

                Console.WriteLine($"websocket pairing succeeded for {x}, {y}, {z}, index {index}, base yaw {by}");
                return;
            }
            case "ready": {
                Ready = true;
                Console.WriteLine($"yo champ i think they are ready :3");
                return;
            }
        }
    }
}