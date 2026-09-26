using System.Numerics;
using Raylib_cs;
using vikistrike.anvil;
using vikistrike.cannon;
using Rl = Raylib_cs.Raylib;

namespace vikistrike.map;

public class MapRenderer(Anvil anvil, CannonController controller) {
    public static void RenderXaero(ref Camera2D cam) {
        foreach (((int fx, int fz), Texture2D tex) in XaeroLoader.Fragments) {
            Rl.DrawTexture(tex, fx, fz, Color.White);
        }
    }

    public void RenderCannons() {
        lock (controller.@Lock) {
            foreach (Cannon c in controller.Cannons) {

                Vector2 barrelStart = new(c.Position.X, c.Position.Z);
                Vector2 barrel = new(c.BaseDirection.X, c.BaseDirection.Z);

                for (int i = 1; i < c.Length; i++) {
                    Rl.DrawPixelV(barrelStart + barrel * i, Color.Yellow);
                }

                Rl.DrawPixel(c.Position.X, c.Position.Z, Color.Green);
            }
        }
    }
}