using Godot;

// Instantiates a Scene on Chunks
// TODO Add Constants (pd3d_chunk_size) for the meta strings
// TODO Add Option to spawn Scenes for Connection Points

namespace PrettyDunGen3D;

[GlobalClass]
[Tool]
public partial class PackedSceneInstantiateRule : PrettyDunGen3DRule
{
    [Export]
    public PackedScene Node3DSceneToInstantiate { get; set; }

    [Export]
    public bool InstantiateForConnectors { get; set; } = true;

    public override string OnGenerate(PrettyDunGen3DGenerator generator)
    {
        if (Node3DSceneToInstantiate == null)
            return "Can not instantiate scenes since Node3DSceneToInstantiate is not set!";

        foreach (var chunk in generator.Graph.GetNodes())
        {
            Node3D instance = (Node3D)Node3DSceneToInstantiate.Instantiate();
            instance.SetMeta("pd3d_size", chunk.Size);
            chunk.AddChild(instance);
            instance.Owner = chunk.Owner;

            if (InstantiateForConnectors)
            {
                foreach (var chunkConnector in chunk.Connectors)
                {
                    // Avoids duplicate scene spawning when another chunk has the same connector attached.
                    if (chunkConnector.HasMeta("pd3d_chunk_connected"))
                        continue;

                    chunkConnector.SetMeta("pd3d_chunk_connected", true);

                    Node3D connInstance = (Node3D)Node3DSceneToInstantiate.Instantiate();
                    connInstance.SetMeta("pd3d_size", chunkConnector.Size);

                    chunkConnector.AddChild(connInstance);
                    connInstance.Owner = chunkConnector.Owner;
                }
            }
        }

        return null;
    }
}
