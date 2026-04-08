using System.Collections.Generic;
using System.Linq;

namespace DiceRogue
{
    public class MapSystem
    {
        private readonly List<MapNodeRuntimeState> nodes = new List<MapNodeRuntimeState>();

        public IReadOnlyList<MapNodeRuntimeState> Nodes => nodes;

        public void BuildMap(IReadOnlyList<MapNodeDefinition> definitions)
        {
            nodes.Clear();

            for (var index = 0; index < definitions.Count; index++)
            {
                nodes.Add(new MapNodeRuntimeState
                {
                    Index = index,
                    Definition = definitions[index],
                    IsUnlocked = index == 0,
                    IsCompleted = false
                });
            }
        }

        public IReadOnlyList<MapNodeRuntimeState> GetAvailableNodes()
        {
            return nodes.Where(node => node.IsUnlocked && !node.IsCompleted).ToList();
        }

        public MapNodeRuntimeState GetNode(int index)
        {
            return nodes.FirstOrDefault(node => node.Index == index);
        }

        public void CompleteNode(int index)
        {
            var node = GetNode(index);
            if (node == null)
            {
                return;
            }

            node.IsCompleted = true;

            foreach (var nextIndex in node.Definition.NextNodeIndices)
            {
                var nextNode = GetNode(nextIndex);
                if (nextNode != null)
                {
                    nextNode.IsUnlocked = true;
                }
            }
        }
    }
}
