using System;
using System.Collections.Generic;
using Godot;

public sealed class RigidBody
{
    public readonly List<Vector2>  LocalCells; // unrotated offsets from CoM, fixed at construction
    public HashSet<(int x, int y)> GridCells;  // current world grid positions
    public Vector2  Position;                   // float CoM in sim-space
    public float    Angle;                      // radians
    public float    VelX, VelY;                // px / tick
    public float    SubX, SubY;                // sub-pixel accumulators
    public float    AngVel;                    // rad / tick
    public readonly float Mass;                // = cell count
    public readonly float Inertia;             // = Σ |r_i|², computed once
    public int      SleepTimer;
    public bool     Sleeping;

    public RigidBody(IEnumerable<(int x, int y)> cells)
    {
        var list = new List<(int x, int y)>(cells);
        float cx = 0f, cy = 0f;
        foreach (var (x, y) in list) { cx += x; cy += y; }
        cx /= list.Count;
        cy /= list.Count;

        Position   = new Vector2(cx, cy);
        LocalCells = new List<Vector2>(list.Count);
        foreach (var (x, y) in list)
            LocalCells.Add(new Vector2(x - cx, y - cy));

        GridCells = new HashSet<(int, int)>(list);
        Mass      = list.Count;
        Inertia   = 0f;
        foreach (var l in LocalCells)
            Inertia += l.LengthSquared();
        Inertia = MathF.Max(Inertia, 1f);
    }
}
