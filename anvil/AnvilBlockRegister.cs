using Raylib_cs;
using Rl = Raylib_cs.Raylib;
namespace vikistrike.anvil;

public static class AnvilBlockRegister {
    private static readonly Dictionary<string, block_id> Register = new() {
        {"minecraft:air", 0}
    };

    private static readonly Dictionary<block_id, string> Reverse = new() {
        {0, "minecraft:air"}
    };

    private static block_id _idc = 1;
    public static block_id Get(string id) {
        if (Register.TryGetValue(id, out block_id bid)) return bid;
        Register.Add(id, _idc);
        Reverse.Add(_idc, id);
        return _idc++;
    }

    public static string? GetName(block_id id) => Reverse.GetValueOrDefault(id);
}