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
}
