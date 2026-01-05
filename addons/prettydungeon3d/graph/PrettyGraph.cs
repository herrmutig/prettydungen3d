using System;
using System.Collections.Generic;
using System.Linq;

namespace PrettyDunGen3D
{
    public class PrettyGraph<TNode>
    {
        // Lazy Initialization
        protected Dictionary<TNode, List<TNode>> AdjList => adjList ?? (adjList = new());
        protected Dictionary<TNode, List<TNode>> adjList;

        public TNode[] GetNodes() => AdjList.Keys.ToArray() ?? new TNode[0];

        public TNode[] GetNeighbours(TNode node) => AdjList[node].ToArray() ?? new TNode[0];

        public int GetNodeCount() => AdjList.Count;

        public bool HasNeighbours(TNode node) => node != null && GetNeighbours(node).Length > 0;

        public bool HasNode(TNode node) => node != null && (AdjList?.ContainsKey(node) ?? false);

        // Note: Used for special cases when index or order of specfic nodes is needed.
        // Does not help accessing anything within the graph.
        public int GetIndexOf(TNode node)
        {
            int index = 0;
            foreach (var kvp in AdjList)
            {
                if (kvp.Key.Equals(node))
                    return index;
                index++;
            }

            return -1;
        }

        public virtual void AddNode(TNode node)
        {
            if (!AdjList.ContainsKey(node))
            {
                AdjList[node] = new List<TNode>();
                return;
            }

            throw new Exception("Node already exists, Skipping Insertion...");
        }

        public virtual void AddEdge(TNode from, TNode to, bool isDirected = false)
        {
            if (from == null || to == null)
                return;

            if (!AdjList.ContainsKey(from))
                AddNode(from);
            if (!AdjList.ContainsKey(to))
                AddNode(to);
            if (!AdjList[from].Contains(to))
                AdjList[from].Add(to);
            if (!isDirected && !AdjList[to].Contains(from))
                AdjList[to].Add(from);
        }

        public TNode[] BFS(int index) => BFS(GetNodes()[0]);

        public TNode[] BFS(TNode startNode)
        {
            Queue<TNode> queue = new();
            HashSet<TNode> visited = new();
            List<TNode> result = new();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                foreach (var neighbour in AdjList[current])
                {
                    if (!visited.Contains(neighbour))
                    {
                        queue.Enqueue(neighbour);
                        visited.Add(neighbour);
                    }
                }
            }

            return result.ToArray();
        }

        public void Clear()
        {
            if (AdjList == null)
                return;

            AdjList.Clear();
        }
    }
}
