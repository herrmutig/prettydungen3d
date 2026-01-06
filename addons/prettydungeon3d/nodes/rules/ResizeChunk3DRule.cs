using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrettyDunGen3D;

[Tool]
[GlobalClass]
public partial class ResizeChunk3DRule : PrettyDunGen3DRule
{
    [Export]
    public string[] CategoriesToResize { get; set; }

    [Export]
    public Vector3[] Sizes = [new Vector3(10f, 5f, 10f)];

    RandomNumberGenerator numberGenerator;

    public override void OnInitialize(PrettyDunGen3DGenerator generator)
    {
        numberGenerator = new();
        numberGenerator.Seed = generator.Seed;
    }

    public override string OnGenerate(PrettyDunGen3DGenerator generator)
    {
        if (Sizes == null || Sizes.Length < 1)
            return "Can not resize chunks. No Sizes specified.";

        var graph = generator.Graph;
        PrettyDunGen3DChunk[] chunks = graph.GetChunksWithCategories(CategoriesToResize);

        foreach (var chunk in chunks)
        {
            Vector3 size = Sizes[numberGenerator.RandiRange(0, Sizes.Length - 1)];
            chunk.Resize(size, generator.DefaultChunkOffset);

            // marked = new();

            //  RedistributeChunk(generator, chunk, size);
            continue;
            foreach (var neighbour in chunk.Neighbours)
            {
                if (chunks.Contains(neighbour))
                    continue;

                Vector3 tempSize = neighbour.Size;
                neighbour.Resize(size, generator.DefaultChunkOffset);
                neighbour.Size = tempSize;
            }
        }

        return null;
    }

    HashSet<PrettyDunGen3DChunk> marked = new();

    void RedistributeChunk(
        PrettyDunGen3DGenerator generator,
        PrettyDunGen3DChunk chunk,
        Vector3 size
    )
    {
        marked.Add(chunk);

        foreach (var neighbour in chunk.Neighbours)
        {
            if (marked.Contains(neighbour))
                continue;

            // Frage ist: Wann möchte ich redistributieren und wann nicht?

            Vector3 tempSize = neighbour.Size;
            neighbour.Resize(size, generator.DefaultChunkOffset);
            neighbour.Size = tempSize;
            RedistributeChunk(generator, neighbour, size);
        }
    }
}
