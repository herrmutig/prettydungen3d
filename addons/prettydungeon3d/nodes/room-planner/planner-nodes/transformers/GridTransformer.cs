using System;
using System.Collections.Generic;
using Godot;

namespace PrettyDunGen3D;

[Tool]
[GlobalClass]
public partial class GridTransformer : PrettyPlannerTransformer
{
    [ExportGroup("Grid Settings")]
    [Export(PropertyHint.Range, "0.1,1,")]
    public float RelativeSizeX { get; set; } = 1f;

    [Export(PropertyHint.Range, "0.1,1,")]
    public float RelativeSizeZ { get; set; } = 1f;

    [Export(PropertyHint.Range, "-1, 10,1,or_greater")]
    public float MaxColumns { get; set; } = -1;

    [Export]
    public bool InvertColumnOrder { get; set; } = false;

    [Export(PropertyHint.Range, "-1, 10,1,or_greater")]
    public float MaxRows { get; set; } = -1;

    [Export]
    public bool InvertRowOrder { get; set; } = false;

    [Export]
    public Vector3 Offset { get; set; } = Vector3.Zero;

    [ExportGroup("Cell Settings")]
    [Export]
    public bool AlwaysRoundToNextCell { get; set; } = true;

    [Export]
    public float CellSizeX = 1f;

    [Export]
    public float CellSizeZ = 1f;

    [Export]
    public Vector3 CellRotation { get; set; } = Vector3.Zero;

    [ExportGroup("Debugging")]
    [Export]
    public bool ShowDebugDrawingUnfocused { get; set; } = false;

    public override Transform3D[] GetTransformations()
    {
        int maxColumns = (int)MaxColumns;
        int maxRows = (int)MaxRows;

        if (maxColumns == 0 || maxRows == 0)
            return [];

        Vector2I iterations = GetIterations();
        Vector3 origin = GetOriginPosition();

        List<Transform3D> result = new();

        Vector3 radAngles = new Vector3(
            Mathf.DegToRad(CellRotation.X),
            Mathf.DegToRad(CellRotation.Y),
            Mathf.DegToRad(CellRotation.Z)
        );

        int xLimit = CalculateIterationLimit(iterations.X, maxColumns);
        int yLimit = CalculateIterationLimit(iterations.Y, maxRows);

        int x = InvertColumnOrder ? iterations.X - 1 : 0;
        int xStep = InvertColumnOrder ? -1 : 1;
        int zStep = InvertRowOrder ? -1 : 1;

        Func<int, int, bool, bool> columnCheck = (index, limit, invert) =>
        {
            if (invert)
                return index >= 0 && index >= iterations.X - limit;
            return index >= 0 && index < limit;
        };

        Func<int, int, bool, bool> rowCheck = (index, limit, invert) =>
        {
            if (invert)
                return index >= 0 && index >= iterations.Y - limit;
            return index >= 0 && index < limit;
        };

        while (columnCheck(x, xLimit, InvertColumnOrder))
        {
            int z = InvertRowOrder ? iterations.Y - 1 : 0;

            while (rowCheck(z, yLimit, InvertRowOrder))
            {
                Transform3D transform = new Transform3D(
                    Basis.FromEuler(radAngles),
                    origin + new Vector3(x * CellSizeX, 0f, z * CellSizeZ)
                );

                result.Add(transform);
                z += zStep;
            }
            x += xStep;
        }

        return result.ToArray();
    }

    public Vector2I GetIterations()
    {
        Vector3 size = GetGridSize();
        Vector2I iterations = new Vector2I(
            AlwaysRoundToNextCell
                ? Mathf.CeilToInt(size.X / CellSizeX)
                : Mathf.RoundToInt(size.X / CellSizeX),
            AlwaysRoundToNextCell
                ? Mathf.CeilToInt(size.Z / CellSizeZ)
                : Mathf.RoundToInt(size.Z / CellSizeZ)
        );

        return iterations;
    }

    public Vector3 GetGridSize()
    {
        var size = RoomPlanner.Size;
        size.X *= RelativeSizeX;
        size.Z *= RelativeSizeZ;
        size.Y = 0.01f;
        return size;
    }

    public Vector3 GetOriginPosition()
    {
        Vector3 size = GetGridSize();
        Vector3 origin = Vector3.Zero;
        origin.X = -size.X * 0.5f + 0.5f * CellSizeX;
        origin.Z = -size.Z * 0.5f + 0.5f * CellSizeZ;
        return origin + Offset;
    }

    private int CalculateIterationLimit(int iterationDimension, int limit)
    {
        if (limit < 0)
            return iterationDimension;

        return Mathf.Clamp(Mathf.Clamp(iterationDimension, 0, limit), 0, iterationDimension);
    }

    // DEBUGGING
    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint() || RoomPlanner == null)
            return;

        if (!ShowDebugDrawingUnfocused)
        {
            var selection = EditorInterface.Singleton?.GetSelection();
            if (selection == null || !selection.GetSelectedNodes().Contains(this))
                return;
        }

        DebugDraw3D.DrawBox(
            RoomPlanner.GlobalPosition,
            Quaternion.Identity,
            GetGridSize(),
            null,
            true
        );

        foreach (var transform in GetTransformations())
        {
            Vector3 origin = RoomPlanner.GlobalPosition + transform.Origin;
            DebugDraw3D.DrawBox(
                origin,
                Quaternion.Identity,
                new Vector3(CellSizeX, 0.01f, CellSizeZ),
                new Color(0, 0.4f, 0.7f),
                true
            );

            float thickness = 0.005f;

            // Draws a Gizmo to visualize rotation
            Vector3 xDir = transform.Basis.X * 0.3f;
            Vector3 yDir = transform.Basis.Y * 0.3f;
            Vector3 zDir = transform.Basis.Z * 0.3f;

            DebugDraw3D.DrawArrow(origin, origin + xDir, new Color(1, 0.25f, 0.25f), thickness);
            DebugDraw3D.DrawArrow(origin, origin + yDir, new Color(0.25f, 1, 0.25f), thickness);
            DebugDraw3D.DrawArrow(origin, origin + zDir, new Color(0.25f, 0.25f, 1), thickness);
        }
    }
}
