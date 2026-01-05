using System.Collections.Generic;
using Godot;

namespace PrettyDunGen3D;

// TODO acutally we could use A* here?

[Tool]
[GlobalClass]
public partial class LoopPath3DRule : PrettyDunGen3DRule
{
    [ExportGroup("General")]
    [Export]
    public string Category { get; set; } = "path:loop";

    [Export(PropertyHint.Range, "1,20,1,or_greater")]
    public int LoopKillCounter { get; set; } = 20;

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

    [ExportGroup("Debug")]
    [Export]
    public Color PathColor { get; set; } = new Color(1f, 0f, 0f, 1f);

    RandomNumberGenerator numberGenerator;
    PrettyDunGen3DGenerator generator;

    public override void OnInitialize(PrettyDunGen3DGenerator generator)
    {
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

        Vector3I startCoordinates = startChunk.Coordinates;
        Vector3I endCoordinates = endChunk.Coordinates;

        PrettyDunGen3DChunk previousChunk = startChunk;
        startChunk.AddCategory(Category);

        Vector3I[] directions;
        Vector3I xDirection;
        Vector3I zDirection;
        int killCounter = Mathf.Max(1, LoopKillCounter);
        bool addedCurrentNodeSuccessfully = false;

        while (startCoordinates != endCoordinates)
        {
            if (killCounter < 1)
                return $"Could not connect the path after {killCounter} attempts";

            if (startCoordinates.X < endCoordinates.X)
                xDirection = Vector3I.Right;
            else if (startCoordinates.X > endCoordinates.X)
                xDirection = Vector3I.Left;
            else
                xDirection = numberGenerator.Randf() < 0.5f ? Vector3I.Right : Vector3I.Left;

            if (startCoordinates.Y < endCoordinates.Y)
                zDirection = Vector3I.Forward;
            else if (startCoordinates.Y > endCoordinates.Y)
                zDirection = Vector3I.Back;
            else
                zDirection = numberGenerator.Randf() < 0.5f ? Vector3I.Back : Vector3I.Forward;

            if (startCoordinates.X == endCoordinates.X)
                directions = [zDirection, xDirection, -zDirection, -xDirection];
            else
                directions = [xDirection, zDirection, -xDirection, -zDirection];

            foreach (var direction in directions)
            {
                Vector3I nextCoordinates = startCoordinates + direction;
                PrettyDunGen3DChunk nextChunk = graph.GetNodeAtCoordinate(nextCoordinates);

                if (nextChunk == endChunk)
                {
                    generator.Graph.AddEdge(previousChunk, endChunk);
                    nextChunk.AddCategory(Category);
                    nextChunk.Name = "Looped";
                    return null;
                }

                if (nextChunk == null)
                {
                    nextChunk = generator.GetOrCreateChunkAtCoordinates(nextCoordinates);
                    graph.AddEdge(previousChunk, nextChunk);
                    nextChunk.AddCategory(Category);
                    nextChunk.Name = "Looped";
                    startCoordinates = nextCoordinates;
                    addedCurrentNodeSuccessfully = true;
                    previousChunk = nextChunk;
                    break;
                }
            }

            if (!addedCurrentNodeSuccessfully)
                return "Could not add chunk";
        }

        return null;
    }

    public override void DrawDebug()
    {
        if (!Engine.IsEditorHint())
            return;

        if (generator == null || generator.Graph == null)
            return;

        var graph = generator.Graph;
        HashSet<PrettyDunGen3DChunk> marked = new();

        // Basically checks all chunks if they have a connection with this path rule and draws them.
        foreach (var chunk in graph.GetNodes())
        {
            if (!IsConnectedToPath(chunk, Category))
                continue;

            foreach (var neighbour in graph.GetNeighbours(chunk))
            {
                if (!IsConnectedToPath(neighbour, Category))
                    continue;

                if (marked.Contains(neighbour))
                    continue;

                DebugDraw3D.ScopedConfig().SetThickness(0.1f);
                DebugDraw3D.DrawBox(
                    chunk.GlobalPosition,
                    Quaternion.Identity,
                    Vector3.One,
                    PathColor,
                    true
                );
                DebugDraw3D.DrawBox(
                    neighbour.GlobalPosition,
                    Quaternion.Identity,
                    Vector3.One,
                    PathColor,
                    true
                );
                DebugDraw3D.ScopedConfig().SetThickness(0.2f);
                DebugDraw3D.DrawLine(
                    chunk.GlobalPosition,
                    neighbour.GlobalPosition,
                    PathColor,
                    0.2f
                );
            }

            marked.Add(chunk);
        }
    }

    private bool IsConnectedToPath(PrettyDunGen3DChunk chunk, string category)
    {
        return chunk.Categories.Contains(category);
    }
}
