using System;
using Godot;
using Godot.Collections;

namespace PrettyDunGen3D;

[GlobalClass]
[Tool]
public partial class PrettyDunGen3DGenerator : Node3D
{
    // Mainly used by rules during generation to act on category changes.
    // TODO later we could only autogenerate when a rule has been changed or any property...
    // TODO Kick out Y-Coordinate Dimension or do we keep it?
    public event Action<PrettyDunGen3DChunk> OnChunkCategoriesChanged;
    public PrettyDunGen3DGraph Graph { get; private set; }
    public Array<PrettyDunGen3DRule> Rules { get; private set; }

    [ExportGroup("General")]
    [Export]
    public bool PersistGenerated { get; set; } = false;

    [Export]
    public bool AutoGenerate { get; set; } = false;

    [Export]
    public ulong Seed { get; set; } = 0;

    [Export]
    public bool RandomizeSeedOnGeneration { get; set; } = false;

    [ExportGroup("Generation")]
    [Export]
    public Vector3 DefaultChunkSize { get; set; } = new Vector3(5, 1, 5);

    [Export(PropertyHint.Range, "0,20,,or_greater")]
    public float DefaultChunkOffset { get; set; } = 2f;

    [ExportToolButton("Generate!")]
    Callable GenerateButton => Callable.From(Generate);

    [ExportToolButton("Clear")]
    Callable ClearGenerationButton => Callable.From(FreeGeneration);

    [ExportGroup("Debug")]
    [Export]
    public bool ShowDebug { get; set; } = true;

    [Export]
    public Color ChunkDebugColor { get; set; } = new Color(0f, 0, 1f, 0.5f);

    [Export]
    public float AutoGenerationEditorTimeout = 4f;

    RandomNumberGenerator numberGenerator;
    Node3D generationContainer;
    Timer debugAutoGenerationTimer;

    public override void _Ready()
    {
        numberGenerator = new();

        if (Engine.IsEditorHint())
        {
            debugAutoGenerationTimer = new Timer();
            debugAutoGenerationTimer.WaitTime = AutoGenerationEditorTimeout;
            debugAutoGenerationTimer.Timeout += DebugAutoGenerateTimeout;
            AddChild(debugAutoGenerationTimer);
            debugAutoGenerationTimer.Start();
        }

        if (AutoGenerate)
            Generate();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint() && ShowDebug && Rules != null)
        {
            foreach (var rule in Rules)
            {
                rule.DrawDebug();
            }
        }
    }

    public void FreeGeneration()
    {
        foreach (var node in GetChildren())
        {
            if (node is PrettyDunGen3DChunk)
            {
                RemoveChild(node);
                node.QueueFree();
            }
        }

        if (generationContainer != null)
        {
            RemoveChild(generationContainer);
            generationContainer.QueueFree();
            generationContainer = null;
        }

        if (Graph != null)
        {
            Graph.Clear();
        }
    }

    public void Generate()
    {
        if (Graph == null)
            Graph = new();
        if (Rules == null)
            Rules = new();

        if (numberGenerator == null)
            numberGenerator = new();

        // Clean Up Phase
        FreeGeneration();

        // Validation Phase
        if (DefaultChunkSize.X < 0f || DefaultChunkSize.Y < 0f || DefaultChunkSize.Z < 0f)
        {
            GD.PushWarning($"{nameof(DefaultChunkSize)} must use values greater 0.", this);
            return;
        }

        // Generation Phase
        if (RandomizeSeedOnGeneration)
        {
            Seed = numberGenerator.Randi();
        }

        numberGenerator.Seed = Seed;
        Rules.Clear();

        var ruleNodes = FindChildren("*", nameof(PrettyDunGen3DRule), true);
        foreach (var node in ruleNodes)
        {
            var rule = (PrettyDunGen3DRule)node;
            if (!rule.Mute)
                Rules.Add(rule);
        }
        foreach (var rule in Rules)
        {
            rule.OnInitialize(this);
        }

        foreach (var rule in Rules)
        {
            string msg = rule.OnGenerate(this);

            if (msg != null)
            {
                if (rule.StopDungeonGenerationOnError)
                {
                    GD.PushWarning($"[{rule.Name}]: {msg} - [Generation stopped]");
                    return;
                }
                else
                {
                    GD.PushWarning($"[{rule.Name}]: {msg}");
                }
            }
        }
    }

    public PrettyDunGen3DChunk GetOrCreateChunkAtCoordinates(Vector3I coordinates)
    {
        if (Graph == null)
        {
            GD.PrintErr(
                $"[{nameof(Name)}] Tried to create a Chunk outside of generation process",
                this
            );
            return null;
        }

        PrettyDunGen3DChunk chunk = Graph.GetNodeAtCoordinate(coordinates);

        if (chunk == null)
        {
            chunk = new PrettyDunGen3DChunk(this, coordinates);
            Graph.AddNode(chunk);
            AddChild(chunk);

            chunk.Size = DefaultChunkSize;
            chunk.Position = coordinates * chunk.Size + (Vector3)coordinates * DefaultChunkOffset;
            chunk.Rotation = Vector3.Zero;
            chunk.Scale = Vector3.One;

            if (PersistGenerated)
                chunk.Owner = this;
        }

        return chunk;
    }

    public void InformChunkCategoryChanged(PrettyDunGen3DChunk chunk)
    {
        OnChunkCategoriesChanged?.Invoke(chunk);
    }

    private void DebugAutoGenerateTimeout()
    {
        if (!Engine.IsEditorHint())
            return;

        if (AutoGenerate)
            Generate();

        debugAutoGenerationTimer.WaitTime = AutoGenerationEditorTimeout;
        debugAutoGenerationTimer.Start();
    }
}
