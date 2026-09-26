using System.Numerics;

namespace vikistrike.ballistics;

public record TrajectoryReport(
    Vector3 LandingPosition,
    float FlightTimeSeconds
);