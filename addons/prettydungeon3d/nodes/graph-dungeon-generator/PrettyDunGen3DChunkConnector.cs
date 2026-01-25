using Godot;
using PrettyDunGen3D;

[Tool]
public partial class PrettyDunGen3DChunkConnector : Node3D
{
    // Note: Properties are only for debugging reasons visible in the Godot Editor.
    [Export]
    public Vector3 Size { get; private set; }

    [Export]
    public Vector3 ConnectionDirection { get; private set; }

    [Export]
    public PrettyDunGen3DChunk FromChunk { get; private set; }

    [Export]
    public PrettyDunGen3DChunk ToChunk { get; private set; }
    RandomNumberGenerator numberGenerator;
    PrettyDunGen3DGenerator generator;

    public PrettyDunGen3DChunkConnector(
        PrettyDunGen3DGenerator generator,
        PrettyDunGen3DChunk from,
        PrettyDunGen3DChunk to
    )
    {
        this.generator = generator;
        FromChunk = from;
        ToChunk = to;
        numberGenerator = new RandomNumberGenerator();
        numberGenerator.Seed = generator.Seed;

        ConnectionDirection = ToChunk.Coordinates - FromChunk.Coordinates;
        Vector3 defaultWidth = generator.DefaultChunkConnectorWidth;
        Name = "Connector_" + from.Name + "_" + to.Name;
        GenerateSize(defaultWidth, defaultWidth);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    public bool GenerateSize(RandomNumberGenerator numberGenerator = null)
    {
        return GenerateSize(
            generator.DefaultChunkConnectorWidth,
            generator.DefaultChunkConnectorWidth,
            numberGenerator
        );
    }

    public bool GenerateSize(
        Vector3 minWidth,
        Vector3 maxWidth,
        RandomNumberGenerator numberGenerator = null
    )
    {
        if (numberGenerator == null)
            numberGenerator = this.numberGenerator;

        bool isRight = Mathf.Abs(ConnectionDirection.X) > 0.01f;
        bool isUp = Mathf.Abs(ConnectionDirection.Y) > 0.01f;
        bool isForward = Mathf.Abs(ConnectionDirection.Z) > 0.01f;
        int directionCounter = (isRight ? 1 : 0) + (isUp ? 1 : 0) + (isForward ? 1 : 0);

        // TODO Consider throwing an Exception instead?
        if (directionCounter > 1)
        {
            GD.PushWarning("Could not determine size of Connector!", this);
            return false;
        }

        Vector3 connectorDistanceVector = FromChunk.GetDistanceVectorTo(ToChunk).Abs();
        Vector3 randomWidthVector = new Vector3(
            numberGenerator.RandfRange(minWidth.X, maxWidth.X),
            numberGenerator.RandfRange(minWidth.Y, maxWidth.Y),
            numberGenerator.RandfRange(minWidth.Z, maxWidth.Z)
        );

        randomWidthVector.Clamp(FromChunk.Size.Min(ToChunk.Size), FromChunk.Size.Max(ToChunk.Size));

        if (isRight)
            Size = new Vector3(connectorDistanceVector.X, randomWidthVector.Y, randomWidthVector.Z);
        else if (isUp)
            Size = new Vector3(randomWidthVector.X, connectorDistanceVector.Y, randomWidthVector.Z);
        else
            Size = new Vector3(randomWidthVector.X, randomWidthVector.Y, connectorDistanceVector.Z);

        return true;
    }

    public void UpdateConnectionCenter()
    {
        if (FromChunk == null || FromChunk.IsQueuedForDeletion())
            return;

        if (ToChunk == null || ToChunk.IsQueuedForDeletion())
            return;

        GlobalPosition = FromChunk.GetConnectionCenter(ToChunk);
    }

    public bool IsConnectedToChunk(PrettyDunGen3DChunk chunk)
    {
        return FromChunk == chunk || ToChunk == chunk;
    }
}
