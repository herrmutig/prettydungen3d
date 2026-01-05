using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PrettyDunGen3D;

// TODO markedChunks are not persisted for some reason...
// TODO Add support for description when added to Godot .NET

[Tool]
[GlobalClass]
public partial class Path3DRule : PrettyDunGen3DRule
{
    public int PathLength => markedChunks?.Count ?? 0;

    public enum Path3DDirection
    {
        Forward = 0,
        Right = 1,
        Backward = 2,
        Left = 3,
        Up = 4,
        Down = 5,
        Random,
    }

    public enum PathStartOptions
    {
        StartCoordinates,
        StartAtPath,
    }

    public enum PathConflictStrategy
    {
        ConnectToExistingPath,
        StopRule,
    }

    /** Inspector  - Note: some properties are exposed via _GetPropertyList **/
    [ExportGroup("General")]
    [Export]
    public string Category { get; set; } = "path:main";

    [Export]
    public PathConflictStrategy ConflictStrategy { get; set; } =
        PathConflictStrategy.ConnectToExistingPath;

    [Export]
    public Path3DDirection PathDirection { get; set; } = Path3DDirection.Forward;

    [Export(PropertyHint.Range, "0,20,1,or_greater")]
    public int MinPathLength { get; set; } = 3;

    [Export(PropertyHint.Range, "0,20,1,or_greater")]
    public int MaxPathLength { get; set; } = 5;

    [ExportGroup("Path Start")]
    [Export]
    public PathStartOptions PathStartOption
    {
        get => pathStartOption;
        set
        {
            if (value != pathStartOption)
            {
                pathStartOption = value;
                NotifyPropertyListChanged();
            }
        }
    }
    public Vector3I StartCoordinates { get; set; } = new Vector3I(0, 0, 0);
    public Path3DRule StartPathRule { get; set; }
    public Vector2I StartPathRange { get; set; } = new Vector2I(0, 2);
    PathStartOptions pathStartOption;

    Color PathColor { get; set; } = new Color(1f, 0, 0f, 1f);
    Array<PrettyDunGen3DChunk> markedChunks = new();
    PrettyDunGen3DGenerator generator;
    RandomNumberGenerator numberGenerator;

    public override void OnInitialize(PrettyDunGen3DGenerator generator)
    {
        generator.OnChunkCategoriesChanged -= UpdatePathOnAnyChunkGroupChanged;
        this.generator = generator;
        numberGenerator = new();
        numberGenerator.Seed = generator.Seed;
    }

    public override string OnGenerate(PrettyDunGen3DGenerator generator)
    {
        if (MinPathLength < 0 || MaxPathLength < 0)
            return $"{nameof(MinPathLength)} and {nameof(MaxPathLength)} must have a value >= 0";

        if (markedChunks == null)
            markedChunks = new();

        markedChunks.Clear();
        PrettyDunGen3DGraph graph = generator.Graph;
        PrettyDunGen3DChunk lastChunk;
        PrettyDunGen3DChunk nextChunk;
        int pathLength = numberGenerator.RandiRange(MinPathLength, MaxPathLength);

        string errorMessage = FindOrCreateStartChunk(out lastChunk);

        if (errorMessage != null)
            return errorMessage;

        MarkPathConnected(lastChunk, Category);

        // Building the Path
        for (int i = 0; i < pathLength; i++)
        {
            errorMessage = FindOrCreateNextChunk(lastChunk, out nextChunk);

            if (errorMessage != null)
            {
                return errorMessage;
            }

            graph.AddEdge(lastChunk, nextChunk);
            MarkPathConnected(nextChunk, Category);
            lastChunk = nextChunk;
        }

        // Path updates from other rules are only relevant after initial path generation finishes
        generator.OnChunkCategoriesChanged += UpdatePathOnAnyChunkGroupChanged;
        return null;
    }

    public PrettyDunGen3DChunk GetChunk(int index)
    {
        if (index < markedChunks.Count && index > -1)
            return markedChunks[index];

        return null;
    }

    public override Array<Dictionary> _GetPropertyList()
    {
        Array<Dictionary> properties = [];

        properties.Add(
            new()
            {
                { "name", nameof(StartCoordinates) },
                { "type", (int)Variant.Type.Vector3I },
                { "hint", (int)PropertyHint.None },
                {
                    "usage",
                    (int)(
                        PathStartOption == PathStartOptions.StartCoordinates
                            ? PropertyUsageFlags.Default
                            : PropertyUsageFlags.None
                    )
                },
            }
        );

        properties.Add(
            new()
            {
                { "name", nameof(StartPathRule) },
                { "type", (int)Variant.Type.Object },
                { "hint", (int)PropertyHint.NodeType },
                { "hint_string", nameof(Path3DRule) },
                {
                    "usage",
                    (int)(
                        PathStartOption == PathStartOptions.StartAtPath
                            ? PropertyUsageFlags.Default
                            : PropertyUsageFlags.None
                    )
                },
            }
        );

        properties.Add(
            new()
            {
                { "name", nameof(StartPathRange) },
                { "type", (int)Variant.Type.Vector2I },
                { "hint", (int)PropertyHint.None },
                {
                    "usage",
                    (int)(
                        PathStartOption == PathStartOptions.StartAtPath
                            ? PropertyUsageFlags.Default
                            : PropertyUsageFlags.None
                    )
                },
            }
        );
        properties.Add(
            new()
            {
                { "name", "Debug" },
                { "type", (int)Variant.Type.Nil },
                { "usage", (int)PropertyUsageFlags.Group },
            }
        );
        properties.Add(
            new()
            {
                { "name", nameof(PathColor) },
                { "type", (int)Variant.Type.Color },
                { "hint", (int)PropertyHint.None },
                { "usage", (int)PropertyUsageFlags.Default },
            }
        );

        return properties;
    }

    private void UpdatePathOnAnyChunkGroupChanged(PrettyDunGen3DChunk chunk)
    {
        if (markedChunks.Contains(chunk))
            return;

        var graph = generator.Graph;

        if (!chunk.ContainsCategory(Category))
            return;

        foreach (var adjChunk in graph.GetAdjacentChunks(chunk.Coordinates))
        {
            if (adjChunk.ContainsCategory(Category))
            {
                graph.AddEdge(adjChunk, chunk);
                MarkPathConnected(chunk, Category);
            }
        }
    }

    private string FindOrCreateNextChunk(
        PrettyDunGen3DChunk lastChunk,
        out PrettyDunGen3DChunk nextChunk
    )
    {
        Vector3I[] directions =
            PathDirection == Path3DDirection.Random
                ? ShufflePathDirections()
                : [PathDirectionToVector(PathDirection)];

        nextChunk = null;

        foreach (var direction in directions)
        {
            nextChunk = generator.GetOrCreateChunkAtCoordinates(lastChunk.Coordinates + direction);

            if (HasAnyConnectedPath(nextChunk))
            {
                if (ConflictStrategy == PathConflictStrategy.StopRule)
                {
                    nextChunk = null;
                    continue;
                }
            }

            if (ConflictStrategy == PathConflictStrategy.ConnectToExistingPath)
                return null;
        }

        return "Could not find a suitable next chunk for the path.";
    }

    private Vector3I[] ShufflePathDirections()
    {
        Path3DDirection[] directions =
        [
            Path3DDirection.Forward,
            Path3DDirection.Right,
            Path3DDirection.Backward,
            Path3DDirection.Left,
            Path3DDirection.Up,
            Path3DDirection.Down,
        ];

        // Fisher–Yates Shuffle
        for (int i = directions.Length - 1; i > 0; i--)
        {
            int j = numberGenerator.RandiRange(0, i);
            (directions[i], directions[j]) = (directions[j], directions[i]);
        }

        Vector3I[] result = new Vector3I[directions.Length];
        for (int i = 0; i < directions.Length; i++)
            result[i] = PathDirectionToVector(directions[i]);

        return result;
    }

    private Vector3I PathDirectionToVector(Path3DDirection direction)
    {
        switch (direction)
        {
            case Path3DDirection.Forward:
                return Vector3I.Forward;
            case Path3DDirection.Right:
                return Vector3I.Right;
            case Path3DDirection.Backward:
                return Vector3I.Back;
            case Path3DDirection.Left:
                return Vector3I.Left;
            case Path3DDirection.Up:
                return Vector3I.Up;
            case Path3DDirection.Down:
                return Vector3I.Down;
            default:
            {
                int axis = numberGenerator.RandiRange(0, 3 - 1);
                int sign = numberGenerator.Randf() < 0.5f ? -1 : 1;

                return axis switch
                {
                    0 => new Vector3I(sign, 0, 0),
                    1 => new Vector3I(0, sign, 0),
                    _ => new Vector3I(0, 0, sign),
                };
            }
        }
    }

    private string FindOrCreateStartChunk(out PrettyDunGen3DChunk chunk)
    {
        chunk = null;
        if (pathStartOption == PathStartOptions.StartCoordinates)
        {
            chunk = generator.GetOrCreateChunkAtCoordinates(StartCoordinates);
            if (chunk == null)
                return "Unable to find or create a chunk. The generator is misconfigured!";
            if (HasAnyConnectedPath(chunk))
            {
                chunk = null;
                return $"StartChunk at Coordinates {StartCoordinates} is already connected to a path.";
            }

            return null;
        }

        if (pathStartOption == PathStartOptions.StartAtPath)
        {
            if (StartPathRule == this)
                return $"Can not use the same Path to connect to. Please use another Path3DRule in {nameof(StartPathRule)}";

            chunk = StartPathRule.GetChunk(
                numberGenerator.RandiRange(StartPathRange.X, StartPathRange.Y - 1)
            );

            return null;
        }

        return $"StartOption '{pathStartOption}' is not supported.";
    }

    private void MarkPathConnected(PrettyDunGen3DChunk chunk, string category)
    {
        if (!markedChunks.Contains(chunk))
            markedChunks.Add(chunk);

        chunk.AddCategory(category);
    }

    private bool IsConnectedToPath(PrettyDunGen3DChunk chunk, string category)
    {
        return chunk.Categories.Contains(category);
    }

    private bool HasAnyConnectedPath(PrettyDunGen3DChunk chunk)
    {
        return chunk.Generator.Graph.HasNeighbours(chunk);
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
}
