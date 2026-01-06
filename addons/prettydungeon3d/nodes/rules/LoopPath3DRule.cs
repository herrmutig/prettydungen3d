using System.Linq;
using Godot;
using Godot.Collections;

namespace PrettyDunGen3D;

[Tool]
[GlobalClass]
public partial class LoopPath3DRule : PrettyDunGen3DRule
{
    public enum AllowPathFindingOptions
    {
        Vertical,
        Horizontal,
        Both,
    }

    [ExportGroup("General")]
    [Export]
    public string Category { get; set; } = "path:loop";

    [ExportGroup("Path Connection")]
    [Export]
    public Path3DRule StartPathRule { get; set; }

    [Export]
    public int MinStartChunkIndex { get; set; } = 1;

    [Export]
    public int MaxStartChunkIndex { get; set; } = 2;

    [Export]
    public Path3DRule EndPathRule { get; set; }

    [Export]
    public int MinEndChunkIndex { get; set; } = 1;

    [Export]
    public int MaxEndChunkIndex { get; set; } = 2;

    [ExportGroup("Advanced")]
    [Export(PropertyHint.Range, "0,3,,or_greater")]
    public int AStarGraphPadding { get; set; } = 1;

    [Export]
    public AllowPathFindingOptions AllowPathFindingOption { get; set; } =
        AllowPathFindingOptions.Horizontal;

    [ExportGroup("Debug")]
    [Export]
    public Color PathColor { get; set; } = new Color(1f, 0f, 0f, 1f);

    [Export] // For now to get an idea how big the AStar Grid becomes... Chould be optimized Im certain.
    public bool ShowDebugPrint { get; set; } = false;
    RandomNumberGenerator numberGenerator;
    PrettyDunGen3DGenerator generator;

    Vector3I graphSize;
    Vector3I GraphExtent => graphSize / 2;
    Vector3I GraphMinCoordinate => -GraphExtent;
    Vector3I GraphMaxCoordinate => GraphExtent;
    Dictionary<Vector3I, long> astarLookupMap;

    public override void OnInitialize(PrettyDunGen3DGenerator generator)
    {
        astarLookupMap = new();
        numberGenerator = new();
        numberGenerator.Seed = generator.Seed;
        this.generator = generator;
    }

    public override string OnGenerate(PrettyDunGen3DGenerator generator)
    {
        if (StartPathRule == null || EndPathRule == null)
            return $"Cannot create loop: {nameof(StartPathRule)} or {nameof(EndPathRule)} is not assigned.";

        int startIndex = Mathf.Min(
            StartPathRule.PathLength - 1,
            numberGenerator.RandiRange(MinStartChunkIndex, MaxStartChunkIndex)
        );
        int endIndex = Mathf.Min(
            EndPathRule.PathLength - 1,
            numberGenerator.RandiRange(MinEndChunkIndex, MaxEndChunkIndex)
        );

        if (startIndex < 0 || endIndex < 0)
            return $"Cannot create loop: path '{StartPathRule.Name}' or '{EndPathRule.Name}' contains no chunks.";

        PrettyDunGen3DGraph graph = generator.Graph;
        PrettyDunGen3DChunk startChunk = StartPathRule.GetChunk(startIndex);
        PrettyDunGen3DChunk endChunk = EndPathRule.GetChunk(endIndex);

        AStar3D astar = InitializeAStar(graph, startChunk, endChunk);

        if (
            !astarLookupMap.ContainsKey(startChunk.Coordinates)
            || !astarLookupMap.ContainsKey(endChunk.Coordinates)
        )
        {
            return $"Start or End Coordinates are likely not allowed to perform path calculation. Consider changing '{nameof(AllowPathFindingOption)}'.";
        }

        long startAStarId = astarLookupMap[startChunk.Coordinates];
        long endAStarId = astarLookupMap[endChunk.Coordinates];

        long[] path = astar.GetIdPath(startAStarId, endAStarId);

        if (path != null && path.Length > 1)
        {
            // Starting at 1 since first entry is always startChunk.
            PrettyDunGen3DChunk previousChunk = startChunk;
            for (int i = 1; i < path.Length; i++)
            {
                Vector3I newChunkCoordinates = (Vector3I)astar.GetPointPosition(path[i]);
                var newChunk = generator.GetOrCreateChunkAtCoordinates(newChunkCoordinates);

                graph.AddEdge(previousChunk, newChunk);
                newChunk.AddCategory(Category);
                newChunk.Name += $"|{Name}";
                newChunk.PathDebugColor = PathColor;
                previousChunk = newChunk;
            }
        }

        // Not needed but its always good to clean a bit of memory.
        astarLookupMap.Clear();
        astarLookupMap = null;
        return null;
    }

    private AStar3D InitializeAStar(
        PrettyDunGen3DGraph graph,
        PrettyDunGen3DChunk startChunk,
        PrettyDunGen3DChunk endChunk
    )
    {
        switch (AllowPathFindingOption)
        {
            case AllowPathFindingOptions.Vertical:
                return VerticalAStarInitialization(graph, startChunk, endChunk);
            case AllowPathFindingOptions.Horizontal:
                return HorizontalAStarInitialization(graph, startChunk, endChunk);
            default:
                return FullAStarInitialization(graph, startChunk, endChunk);
        }
    }

    private AStar3D VerticalAStarInitialization(
        PrettyDunGen3DGraph graph,
        PrettyDunGen3DChunk startChunk,
        PrettyDunGen3DChunk endChunk
    )
    {
        graph.GetGraphBoundingBoxSize();
        AStar3D astar = new AStar3D();
        Vector3I min = GraphMinCoordinate - Vector3I.One * AStarGraphPadding;
        Vector3I max = GraphMaxCoordinate + Vector3I.One * AStarGraphPadding;

        for (int y = min.Y; y < max.Y; y++)
        {
            long nextId = astar.GetAvailablePointId();
            Vector3I nextPoint = new Vector3I(0, y, 0);

            astarLookupMap.Add(nextPoint, nextId);
            astar.AddPoint(nextId, nextPoint);
        }

        DisableChunkPoints(ref astar, startChunk, endChunk);
        ConnectAStarPoints(ref astar, [Vector3I.Up, Vector3I.Down]);

        return astar;
    }

    private AStar3D HorizontalAStarInitialization(
        PrettyDunGen3DGraph graph,
        PrettyDunGen3DChunk startChunk,
        PrettyDunGen3DChunk endChunk
    )
    {
        graph.GetGraphBoundingBoxSize();
        AStar3D astar = new AStar3D();
        Vector3I min = GraphMinCoordinate - Vector3I.One * AStarGraphPadding;
        Vector3I max = GraphMaxCoordinate + Vector3I.One * AStarGraphPadding;

        for (int x = min.X; x < max.X; x++)
        for (int z = min.Z; z < max.Z; z++)
        {
            long nextId = astar.GetAvailablePointId();
            Vector3I nextPoint = new Vector3I(x, 0, z);

            astarLookupMap.Add(nextPoint, nextId);
            astar.AddPoint(nextId, nextPoint);
        }

        DisableChunkPoints(ref astar, startChunk, endChunk);
        ConnectAStarPoints(
            ref astar,
            [Vector3I.Right, Vector3I.Left, Vector3I.Forward, Vector3I.Back]
        );

        return astar;
    }

    private AStar3D FullAStarInitialization(
        PrettyDunGen3DGraph graph,
        PrettyDunGen3DChunk startChunk,
        PrettyDunGen3DChunk endChunk
    )
    {
        graph.GetGraphBoundingBoxSize();
        AStar3D astar = new AStar3D();
        Vector3I min = GraphMinCoordinate - Vector3I.One * AStarGraphPadding;
        Vector3I max = GraphMaxCoordinate + Vector3I.One * AStarGraphPadding;

        for (int x = min.X; x < max.X; x++)
        for (int y = min.Y; y < max.Y; y++)
        for (int z = min.Z; z < max.Z; z++)
        {
            long nextId = astar.GetAvailablePointId();
            Vector3I nextPoint = new Vector3I(x, y, z);

            astarLookupMap.Add(nextPoint, nextId);
            astar.AddPoint(nextId, nextPoint);
        }

        DisableChunkPoints(ref astar, startChunk, endChunk);
        ConnectAStarPoints(
            ref astar,
            [
                Vector3I.Right,
                Vector3I.Left,
                Vector3I.Up,
                Vector3I.Down,
                Vector3I.Forward,
                Vector3I.Back,
            ]
        );

        return astar;
    }

    private void DisableChunkPoints(ref AStar3D astar, params PrettyDunGen3DChunk[] skippedChunks)
    {
        foreach (var chunk in generator.Graph.GetNodes())
        {
            if (skippedChunks.Contains(chunk))
                continue;
            if (!astarLookupMap.ContainsKey(chunk.Coordinates))
                continue;

            long lookupId = astarLookupMap[chunk.Coordinates];
            astar.SetPointDisabled(lookupId);
        }
    }

    private void ConnectAStarPoints(ref AStar3D astar, params Vector3I[] directions)
    {
        foreach (long pointId in astar.GetPointIds())
        {
            Vector3I closestCoordinate = (Vector3I)astar.GetPointPosition(pointId);
            Vector3I[] lookupCoordinates = directions.Select(d => closestCoordinate + d).ToArray();

            foreach (Vector3I possibleConnection in lookupCoordinates)
            {
                if (astarLookupMap.ContainsKey(possibleConnection))
                    astar.ConnectPoints(pointId, astarLookupMap[possibleConnection]);
            }
        }
    }
}
