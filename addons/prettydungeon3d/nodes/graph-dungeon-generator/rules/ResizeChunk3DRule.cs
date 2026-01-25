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
        marked = new();
        foreach (var chunk in chunks)
        {
            Vector3 size = Sizes[numberGenerator.RandiRange(0, Sizes.Length - 1)];
            chunk.Resize(size, generator.DefaultChunkOffset);
        }

        return RedistributeChunk(generator);
    }

    HashSet<PrettyDunGen3DChunk> marked = new();

    string RedistributeChunk(PrettyDunGen3DGenerator generator)
    {
        PrettyDunGen3DGraph graph = generator.Graph;

        if (graph.GetNodeCount() < 1)
            return null;

        PrettyDunGen3DChunk[] connectedChunks = generator.Graph.BFS(0);

        if (connectedChunks.Length < graph.GetNodeCount())
            return "Redistribution failed. Not all chunks are connected!";

        Vector3 sizeDistribution = connectedChunks
            .Select(c => c.Size)
            .Aggregate(
                (a, b) => new Vector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z))
            );
        ;

        foreach (var connectedChunk in connectedChunks)
        {
            Vector3 tempSize = connectedChunk.Size;
            connectedChunk.Resize(sizeDistribution, generator.DefaultChunkOffset);
            connectedChunk.Size = tempSize;
            connectedChunk.SyncChunk();
        }
        return null;
    }
}
