using System.Numerics;
using vikistrike.ballistics;
using vikistrike.math;

namespace vikistrike.cannon;

public class Cannon {
    public Cannon(Vec3i basePos, int length, int baseYaw) {
        Position = basePos;
        Length = length;
        switch (baseYaw)
        {
            case 0: BaseDirection = new Vec3i(0, 0, 1); break;
            case 90: BaseDirection = new Vec3i(-1, 0, 0); break;
            case 180: BaseDirection = new Vec3i(0, 0, -1); break;
            case 270: BaseDirection = new Vec3i(1, 0, 0); break;
            default: Console.WriteLine("error: invalid base yaw"); return;
        }

        Pitch = 0f;
        Yaw = baseYaw;
    }
    
    public Vec3i Position { get; private set; }
    public int Length { get; private set; }
    public Vec3i BaseDirection { get; private set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    public float? ResolvedYawLow { get; private set; } = null;
    public float? ResolvedPitchLow { get; private set; } = null;
    public float? ResolvedYawHigh { get; private set; } = null;
    public float? ResolvedPitchHigh { get; private set; } = null;

    public void ResetResolve() {
        ResolvedYawLow = null;
        ResolvedPitchLow = null;
        ResolvedYawHigh = null;
        ResolvedPitchHigh = null;
    }
    
    public bool Resolve(Vector3 target, int powderCount) {
        Vec3i cannonBase = Position + new Vec3i(0, 2, 0);
        var cannonBaseF = new Vector3(cannonBase.X, cannonBase.Y, cannonBase.Z);
    
        float yawRad = MathF.Atan2(target.X - cannonBase.X, target.Z - cannonBase.Z);
        float yawRadMc = MathF.Atan2(-(target.X - cannonBase.X), target.Z - cannonBase.Z);
        float degrees = yawRadMc * (180f / MathF.PI);
        float absoluteYawDeg = (degrees % 360 + 360) % 360;
    
        Vector3 firingDir = new(MathF.Sin(yawRad), 0, MathF.Cos(yawRad));
        float muzzleVelocity = powderCount * PhysConstants.ChargeAdditiveVelocity / 20f;
        
        Console.WriteLine($"cannonBase={cannonBaseF}, target={target}, muzzleVelocity={muzzleVelocity}");
        bool fLow = Ballistics.TryFindPitch(cannonBaseF, firingDir, muzzleVelocity, target.Y, target, -MathF.PI / 3f, MathF.PI / 4f, Length, out float lowPitch);
        bool fHigh = Ballistics.TryFindPitch(cannonBaseF, firingDir, muzzleVelocity, target.Y, target, MathF.PI / 4f, MathF.PI * 5 / 12f, Length, out float highPitch);

        if (!fLow && !fHigh) {
            Console.WriteLine("unreachable");
            return false;
        }

        if (fLow) {
            ResolvedPitchLow = lowPitch * 180f / MathF.PI;
            ResolvedYawLow = absoluteYawDeg;
        }

        if (fHigh) {
            ResolvedPitchHigh = highPitch * 180f / MathF.PI;
            ResolvedYawHigh = absoluteYawDeg;
        }

        return true;
    }

    public float GetRangeApproximation(int powderCount, int baseGroundY) {
        Vec3i cannonBase = Position + new Vec3i(0, 2, 0);
        var cannonBaseF = new Vector3(cannonBase.X, cannonBase.Y, cannonBase.Z);
        var firingDir = new Vector3(BaseDirection.X, 0, BaseDirection.Z);

        float muzzleVelocity = powderCount * PhysConstants.ChargeAdditiveVelocity / 20f;

        const float lo = 0f;
        const float hi = MathF.PI / 2f - 0.001f;
        float bestRange = float.MinValue;

        float gr = (MathF.Sqrt(5f) - 1f) / 2f;
        float a = lo;
        float b = hi;
        float c = b - gr * (b - a);
        float d = a + gr * (b - a);

        for (int i = 0; i < 64; i++) {
            float rc = EvalRange(c);
            float rd = EvalRange(d);

            if (rc > bestRange) { bestRange = rc; }
            if (rd > bestRange) { bestRange = rd; }

            if (rc < rd) {
                a = c;
                c = d;
                d = a + gr * (b - a);
            } else {
                b = d;
                d = c;
                c = b - gr * (b - a);
            }

            if (MathF.Abs(b - a) < 1e-4f) break;
        }

        return bestRange;

        float EvalRange(float pitch) {
            Vector3 launchDir = Ballistics.BuildLaunchDir(firingDir, pitch);
            Vector3 launchOrigin = Ballistics.BuildBarrelTip(cannonBaseF, launchDir, Length); // pitched
            Vector3 vel = launchDir * muzzleVelocity;
            TrajectoryReport report = Ballistics.Simulate(launchOrigin, vel, baseGroundY);
            float dx = report.LandingPosition.X - cannonBaseF.X;
            float dz = report.LandingPosition.Z - cannonBaseF.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }
}