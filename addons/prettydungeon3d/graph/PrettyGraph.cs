using System;
using System.Collections.Generic;
using System.Linq;

namespace PrettyDunGen3D
{
    public class PrettyGraph<TNode>
    {
        // Lazy Initialization
        protected Dictionary<TNode, List<TNode>> AdjList => adjList ?? (adjList = new());
        protected List<TNode> OrderedNodeList => orderedNodeList ?? (orderedNodeList = new());
        private Dictionary<TNode, List<TNode>> adjList;
        private List<TNode> orderedNodeList;

        public TNode GetNode(int index) => OrderedNodeList[index];

        public TNode[] GetNodes() => OrderedNodeList.ToArray();

        public TNode[] GetNeighbours(TNode node) => AdjList[node].ToArray() ?? new TNode[0];

        public int GetNodeCount() => OrderedNodeList.Count;

        public bool HasNeighbours(TNode node) => node != null && GetNeighbours(node).Length > 0;

        public bool HasNode(TNode node) => node != null && (AdjList?.ContainsKey(node) ?? false);

        public int GetIndexOf(TNode node)
        {
            return OrderedNodeList.IndexOf(node);
        }

        public virtual void AddNode(TNode node)
        {
            if (!AdjList.ContainsKey(node))
            {
                AdjList[node] = new List<TNode>();
                OrderedNodeList.Add(node);
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
            AdjList.Clear();
            OrderedNodeList.Clear();
        }
    }
}
