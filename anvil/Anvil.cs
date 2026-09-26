using fNbt;

namespace vikistrike.anvil;

public class Anvil(string world) {
    public string WorldPath { get; } = world;
    private readonly Dictionary<(int, int), AnvilRegion> _regions = [];

    public NbtCompound? Chunk(int cx, int cz) {
        AnvilRegion? region = Region(cx >> 5, cz >> 5);
        return region?.Chunk(cx, cz);
    }

    public AnvilRegion? Region(int rx, int rz) {
        if (_regions.TryGetValue((rx, rz), out AnvilRegion? value)) return value;
        string path = Path.Combine(WorldPath, "region", $"r.{rx}.{rz}.mca");
        if (!File.Exists(path)) return null;

        AnvilRegion region = new(path);
        _regions.Add((rx, rz), region);
        return region;
    }
}