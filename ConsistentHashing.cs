using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ConsistentHashingLib
{
    public class ConsistentHashing
    {
        private List<object> _data = new List<object>();
        private List<int> NodeHashes = new List<int>();
        private List<string> _nodes = new List<string>();
        private int _nodeSpaceSize;
        private NodeSpacing _nodeSpacing;
        public Dictionary<int, List<int>> NodeSet { get; set; }
        public Dictionary<int, string> NodeMap { get; set; }
        public ConsistentHashing(IEnumerable<string> nodes, IEnumerable<object> data, int nodeSpaceSize, NodeSpacing spacingType)
        {
            _nodes = nodes.ToList();
            _data = data.ToList();
            _nodeSpaceSize = nodeSpaceSize;
            _nodeSpacing = spacingType;
            NodeSet = new Dictionary<int, List<int>>();
            NodeMap = new Dictionary<int, string>();
        }
        public void SetNodes()
        {
            if (_nodeSpacing == NodeSpacing.Random)
            {
                // 1. hash the nodes and data and add to the NodeSet 
                foreach (var node in _nodes) // add node hashes to set and list
                {
                    var modPos = Math.Abs(node.GetHashCode()) % _nodeSpaceSize;
                    while (NodeHashes.Any(x => x == modPos)) // To avoid collisions, a basic linear probing
                    {
                        modPos += 10;
                    }
                    NodeHashes.Add(modPos);
                    NodeSet.Add(modPos, new List<int>());
                    NodeMap.Add(modPos, node);
                }
                NodeHashes.Sort(); // Keeping it sorted for the binary search
            }
            else
            {
                //2. Equally spaced nodes
                var spacing = _nodeSpaceSize / _nodes.Count;
                for (int i = 0; i < _nodes.Count; i++)
                {
                    var pos = (spacing * i) % _nodeSpaceSize;
                    NodeHashes.Add(pos);
                    NodeSet.Add(pos, new List<int>());
                    NodeMap.Add(pos, _nodes[i]);
                }
            }
            foreach (var key in _data)
            {
                AddData(key);
            }
        }
        public void RemoveNode(string node)
        {
            var nodePos = 0;
            if (NodeMap.ContainsValue(node))
            {
                nodePos = NodeMap.FirstOrDefault(m => m.Value == node).Key;
                var nextNode = NodeHashes[(SearchNodes(nodePos) + 1) % NodeHashes.Count]; // get the next node
                var allPrevData = NodeSet[nodePos];//get all data keys for node to be removed
                NodeSet[nextNode].AddRange(allPrevData);
                NodeSet.Remove(nodePos);
                NodeMap.Remove(nodePos);
            }
        }
        public void AddNode(string node)
        {
            var nodePos = Math.Abs(node.GetHashCode()) % _nodeSpaceSize;
            var nextNode = NodeHashes[SearchNodes(nodePos)]; // get the next node
            var dataToMove = NodeSet[nextNode].Where(m => m <= nodePos);//get all data keys belonging to the next node but less than node to be added
            NodeSet.Add(nodePos, dataToMove.ToList());
            NodeSet[nextNode].RemoveAll(m => m <= nodePos);
            NodeMap.Add(nodePos, node);
        }
        public void AddData(object data)
        {
            var key = Math.Abs(data.GetHashCode()) % _nodeSpaceSize;
            var targetNode = NodeHashes[SearchNodes(key)]; // get the target node position in hashlist
            NodeSet[targetNode].Add(key);// update the hash map
        }

        private int SearchNodes(int nodeHash, bool prev = false)
        {
            if (nodeHash > NodeHashes[NodeHashes.Count - 1] && !prev) // if key is greater than the upperbound, circle back and assign it to the first node
            {
                return 0;
            }
            var start = 0;
            var end = NodeHashes.Count - 1;
            while (start <= end)
            {
                var mid = (end + start) / 2;
                if (nodeHash == NodeHashes[mid])
                {
                    return mid;
                }
                if (nodeHash > NodeHashes[mid])
                {
                    start = mid + 1;
                }
                else if (nodeHash < NodeHashes[mid])
                {
                    end = mid - 1;
                }
            }
            return prev ? end : start;
        }
    }

    public enum NodeSpacing
    {
        Random,
        Equidistant
    }
}
