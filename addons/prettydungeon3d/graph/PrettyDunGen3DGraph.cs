using System.Linq;
using Godot;

namespace PrettyDunGen3D;

public class PrettyDunGen3DGraph : PrettyGraph<PrettyDunGen3DChunk>
{
    public PrettyDunGen3DGenerator Generator { get; private set; }

    public PrettyDunGen3DChunk GetNodeAtCoordinate(Vector3I coordinates)
    {
        return AdjList.Keys.FirstOrDefault(node => node.Coordinates == coordinates);
    }

    public PrettyDunGen3DGraph(PrettyDunGen3DGenerator generator)
    {
        Generator = generator;
    }

    public Vector3I GetGraphBoundingBoxSize()
    {
        if (OrderedNodeList.Count < 1)
            return Vector3I.Zero;

        // Calcuate Bounding Box
        Vector3I minCoordinates = OrderedNodeList[0].Coordinates;
        Vector3I maxCoordinates = OrderedNodeList[0].Coordinates;

        int count = OrderedNodeList.Count;
        for (int i = 1; i < count; i++)
        {
            minCoordinates = minCoordinates.Min(OrderedNodeList[i].Coordinates);
            maxCoordinates = maxCoordinates.Max(OrderedNodeList[i].Coordinates);
        }

        return maxCoordinates - minCoordinates;
    }

    public PrettyDunGen3DChunk[] GetAdjacentChunks(Vector3I coordinates)
    {
        Vector3I[] directions =
        {
            Vector3I.Right,
            Vector3I.Left,
            Vector3I.Up,
            Vector3I.Down,
            Vector3I.Forward,
            Vector3I.Back,
        };

        return AdjList
            .Keys.Where(k => directions.Any(d => k.Coordinates == coordinates + d))
            .ToArray();
    }

    public PrettyDunGen3DChunk[] GetChunksWithCategories(params string[] categories)
    {
        if (
            categories == null
            || categories.Length == 0
            || categories.Any(c => string.IsNullOrWhiteSpace(c))
        )
            return GetNodes();

        return GetNodes().Where(n => categories.Any(c => n.ContainsCategory(c))).ToArray();
    }

    public override void AddEdge(
        PrettyDunGen3DChunk from,
        PrettyDunGen3DChunk to,
        bool isDirected = false
    )
    {
        if (Generator == null || Generator.IsQueuedForDeletion())
        {
            GD.PushError("Can not add edge to graph. No Generator was specified");
            return;
        }

        base.AddEdge(from, to, isDirected);

        if (from.GetConnector(to) == null)
        {
            var connector = new PrettyDunGen3DChunkConnector(Generator, from, to);
            from.AddConnector(connector);
            to.AddConnector(connector);
            Generator.AddChild(connector);

            if (Generator.PersistGenerated)
                connector.Owner = Generator;
        }

        from.SyncWithGraph(this);
        to.SyncWithGraph(this);
    }

    public override void AddNode(PrettyDunGen3DChunk node)
    {
        if (Generator == null || Generator.IsQueuedForDeletion())
        {
            GD.PushError("Can not add Node to graph. No Generator was specified");
            return;
        }

        base.AddNode(node);
        Generator.AddChild(node);

        if (Generator.PersistGenerated)
            node.Owner = Generator;

        node.SyncWithGraph(this);
    }
}
