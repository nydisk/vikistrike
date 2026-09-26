using Raylib_cs;
using Rl = Raylib_cs.Raylib;
namespace vikistrike.map;

public static class XaeroLoader {
    public static readonly Dictionary<(int, int), Texture2D> Fragments = [];

    public static void LoadFragment(string file) {
        string[] parts = Path.GetFileNameWithoutExtension(file).Split('_');
        int fx = int.Parse(parts[2][1..]);
        int fz = int.Parse(parts[3][1..]);
        Fragments[(fx, fz)] = Rl.LoadTexture(file);
    }
}