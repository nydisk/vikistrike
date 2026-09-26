using fNbt;

namespace vikistrike.anvil;

public class AnvilRegion(string rgPath) {
    private readonly FileStream _fs = new(rgPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    public NbtCompound? Chunk(int cx, int cz) {
        try {
            long loc = ((cx & 31) + (cz & 31) * 32) * 4L;
            _fs.Seek(loc, SeekOrigin.Begin);

            Span<byte> scratch = stackalloc byte[3];
            _fs.ReadExactly(scratch);

            int secOff = scratch[0] << 16 | scratch[1] << 8 | scratch[2];
            if (secOff == 0) return null;

            _fs.Seek(secOff * 4096L + 5L, SeekOrigin.Begin); // 5L = skip 4 size bytes and 1 compression byte

            NbtFile file = new();
            file.LoadFromStream(_fs, NbtCompression.AutoDetect);
            return file.RootTag;
        }
        catch (Exception e) {
            Console.WriteLine($"anvil region failed to load: \n  {e}");
            return null;
        }
    }
}