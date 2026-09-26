using fNbt;

namespace vikistrike.anvil;

public class AnvilReader(NbtCompound chunk) {
    public block_id[] GetBlocksInChunk() {
        block_id[] blocks = new block_id[98304];
        foreach (NbtTag nbtTag in (NbtList)chunk["sections"]!) {
            var sect = (NbtCompound)nbtTag;
            sbyte sy = (sbyte)sect["Y"]!.ByteValue;
            AddBlocksFromSection(blocks, sect, sy);
        }

        return blocks;
    }

    private static void AddBlocksFromSection(Span<block_id> blocks, NbtCompound sect, sbyte sectY) {
        var states = (NbtCompound?)sect["block_states"];
        if (states is null) return;

        int begin = (sectY + 4) * 4096;
        Span<block_id> section = blocks.Slice(begin, 4096);

        var palette = (NbtList?)states["palette"];
        if (palette is null) return;
        switch (palette.Count) {
            case 0:
                return;
            case 1: {
                block_id id = AnvilBlockRegister.Get(palette[0]["Name"]?.StringValue!);
                section.Fill(id);
                return;
            }
        }

        var data = (NbtLongArray?)states["data"];
        if (data is null) return;

        for (int i = 0; i < 4096; i++) {
            int lx = i & 0xF;
            int lz = (i >> 4) & 0xF;
            int ly = i >> 8;
            int pIdx = GetPaletteIndex(lx, ly, lz, palette.Count, data);
            block_id id = AnvilBlockRegister.Get(palette[pIdx]["Name"]?.StringValue!);
            section[ly * 256 + lz * 16 + lx] = id;
        }
    }

    private static int GetPaletteIndex(int lx, int y, int lz, int paletteSize, NbtLongArray data) {
        int bits = Math.Max(4, (int)Math.Ceiling(Math.Log2(paletteSize)));
        int entries = 64 / bits;
        int mask = (1 << bits) - 1;
        int bIdx = (y & 15) * 256 + lz * 16 + lx;
        int lIdx = bIdx / entries;
        int bOff = bIdx % entries * bits;
        return (int)((data[lIdx] >> bOff) & mask);
    }
}