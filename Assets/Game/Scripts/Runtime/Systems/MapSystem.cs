using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceRogue
{
    public class MapSystem
    {
        private static readonly Vector2Int StartGridPosition = new Vector2Int(0, 0);
        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down
        };

        private readonly List<MapNodeRuntimeState> nodes = new List<MapNodeRuntimeState>();
        private readonly Dictionary<Vector2Int, int> nodeIndicesByGridPosition = new Dictionary<Vector2Int, int>();

        public IReadOnlyList<MapNodeRuntimeState> Nodes => nodes;

        public void BuildMap(IReadOnlyList<MapNodeDefinition> definitions)
        {
            nodes.Clear();
            nodeIndicesByGridPosition.Clear();

            if (definitions == null)
            {
                return;
            }

            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                var isBossNode = definition != null && definition.NodeType == MapNodeType.Boss;
                var gridPosition = definition != null ? definition.GridPosition : default;

                nodes.Add(new MapNodeRuntimeState
                {
                    Index = index,
                    Definition = definition,
                    GridPosition = gridPosition,
                    IsUnlocked = false,
                    IsCompleted = false,
                    IsRevealed = isBossNode,
                    ShouldAnimateReveal = false
                });

                if (definition != null && !nodeIndicesByGridPosition.ContainsKey(gridPosition))
                {
                    nodeIndicesByGridPosition.Add(gridPosition, index);
                }
            }

            RefreshAvailableNodes(-1);
        }

        public IReadOnlyList<MapNodeRuntimeState> GetAvailableNodes()
        {
            return nodes.Where(node => node.IsUnlocked && !node.IsCompleted).ToList();
        }

        public MapNodeRuntimeState GetNode(int index)
        {
            return nodes.FirstOrDefault(node => node.Index == index);
        }

        public IReadOnlyList<MapNodeRuntimeState> ConsumeNodesPendingReveal()
        {
            var revealedNodes = nodes.Where(node => node.ShouldAnimateReveal).ToList();

            foreach (var node in revealedNodes)
            {
                node.ShouldAnimateReveal = false;
            }

            return revealedNodes;
        }

        public void CompleteNode(int index)
        {
            var node = GetNode(index);
            if (node == null)
            {
                return;
            }

            node.IsCompleted = true;
            node.IsUnlocked = false;
            node.IsRevealed = true;

            RefreshAvailableNodes(index);
        }

        public void MoveToNode(int index)
        {
            if (GetNode(index) == null)
            {
                return;
            }

            RefreshAvailableNodes(index);
        }

        public void MoveToStart()
        {
            RefreshAvailableNodes(-1);
        }

        private void RefreshAvailableNodes(int currentNodeIndex)
        {
            foreach (var node in nodes)
            {
                node.IsUnlocked = false;
            }

            var origin = currentNodeIndex >= 0
                ? GetNode(currentNodeIndex)?.GridPosition ?? StartGridPosition
                : StartGridPosition;

            foreach (var direction in CardinalDirections)
            {
                if (!nodeIndicesByGridPosition.TryGetValue(origin + direction, out var nextIndex))
                {
                    continue;
                }

                var nextNode = GetNode(nextIndex);
                if (nextNode == null)
                {
                    continue;
                }

                nextNode.IsUnlocked = true;
                RevealNode(nextNode);
            }
        }

        private static void RevealNode(MapNodeRuntimeState node)
        {
            if (node == null || node.IsRevealed)
            {
                return;
            }

            node.IsRevealed = true;
            node.ShouldAnimateReveal = true;
        }
    }
}
