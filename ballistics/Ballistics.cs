using System.Numerics;

namespace vikistrike.ballistics;

public static class Ballistics {
   public static TrajectoryReport Simulate(Vector3 start, Vector3 initialVelocity, float targetY, int maxTicks = 1_000_000) {
      Vector3 pos = start;
      Vector3 prevPos = pos;
      Vector3 vel = initialVelocity;
      bool peaked = false;
      int tick = 0;

      for (tick = 0; tick < maxTicks; tick++) {
         prevPos = pos;
         vel -= vel * PhysConstants.Drag;
         vel.Y += PhysConstants.GravityBlocksPerTick;
         pos += vel;

         if (!peaked && vel.Y <= 0) peaked = true;
         if (peaked && pos.Y < targetY) break;
      }

      float tNum = prevPos.Y - targetY;
      float tDen = prevPos.Y - pos.Y;
      Vector3 landing = prevPos + (pos - prevPos) * (tNum / tDen);
      return new TrajectoryReport(landing, tick / 20f);
   }

   public static bool TryFindPitch(Vector3 cannonBase, Vector3 cannonDir, float muzzleVelocity, float targetY, Vector3 target, float pitchMin, float pitchMax, int barrelLength, out float pitch, int iterations = 64) {
      float errorMin = Eval(pitchMin);
      float errorMax = Eval(pitchMax);
      Console.WriteLine($"errorMin={errorMin} errorMax={errorMax} pitchMin={pitchMin} pitchMax={pitchMax}");
      Console.WriteLine($"cannonDir={cannonDir}");
      Console.WriteLine($"toTarget={target - cannonBase}");
      if (MathF.Sign(errorMin) == MathF.Sign(errorMax)) {
         pitch = 0;
         return false;
      }

      float lo = pitchMin;
      float hi = pitchMax;
      
      for (int i = 0; i < iterations; i++)
      {
         float mid = (lo + hi) / 2.0f;
         float err = Eval(mid);

         if (MathF.Abs(err) < 0.01f)
         {
            pitch = mid;
            return true;
         }

         if (Math.Sign(err) == Math.Sign(errorMin))
         {
            lo = mid;
            errorMin = err;
         }
         else
         {
            hi = mid;
            errorMax = err;
         }
      }

      pitch = (lo + hi) / 2.0f;
      return true;

      float Eval(float p) {
         Vector3 launchDir = BuildLaunchDir(cannonDir, p);
         Vector3 launchOrigin = BuildBarrelTip(cannonBase, launchDir, barrelLength);
         Vector3 vel = launchDir * muzzleVelocity;
         TrajectoryReport report = Simulate(launchOrigin, vel, targetY);
         Vector3 toTarget = target - cannonBase;
         Vector3 toLanding = report.LandingPosition - cannonBase;
         Vector3 horizontalDir = Vector3.Normalize(cannonDir);
         return horizontalDir.X * toLanding.X + horizontalDir.Z * toLanding.Z - (horizontalDir.X * toTarget.X + horizontalDir.Z * toTarget.Z);
      }
   }
   
   public static Vector3 BuildLaunchDir(Vector3 horizontalDir, float pitch) => (Vector3.Normalize(horizontalDir) * MathF.Cos(pitch)) + (new Vector3(0, 1, 0) * MathF.Sin(pitch));
   public static Vector3 BuildBarrelTip(Vector3 cannonBase, Vector3 launchDir, int barrelLength) => cannonBase + launchDir * barrelLength;
}