using System.Linq;
using Godot;

namespace PrettyDunGen3D;

public class PrettyDunGen3DGraph : PrettyGraph<PrettyDunGen3DChunk>
{
    public PrettyDunGen3DChunk GetNodeAtCoordinate(Vector3I coordinates)
    {
        return AdjList.Keys.FirstOrDefault(node => node.Coordinates == coordinates);
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
        base.AddEdge(from, to, isDirected);
        from.SyncWithGraph(this);
        to.SyncWithGraph(this);
    }

    public override void AddNode(PrettyDunGen3DChunk node)
    {
        base.AddNode(node);
        node.SyncWithGraph(this);
    }
}
