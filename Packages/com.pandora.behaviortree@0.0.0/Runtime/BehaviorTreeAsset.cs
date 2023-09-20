using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 行为树备注信息
    /// </summary>
    [Serializable]
    public class StickyAsset
    {
        public string title;
        public string content;
        public Rect position;
    }

    /// <summary>
    /// 行为树资产
    /// </summary>
    [CreateAssetMenu(menuName = "Developer/行为树/BehaviorTree", fileName = "behaviorTree")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        /// <summary>
        /// 备注信息
        /// </summary>
        [SerializeReference]
        public List<StickyAsset> stickies = new ();

        /// <summary>
        /// 节点数组
        /// </summary>
        [SerializeReference]
        public List<BTNode> nodes = new ();

        
        /// <summary>
        /// 获取根节点
        /// 如果行为树中有多个没有父节点的节点则会选择一个有子节点的作为根节点
        /// </summary>
        /// <returns></returns>
        public BTNode GetRootNode(out int index)
        {
            index = -1;
            if (!nodes.Any()) return null;
            
            for (int i = 0; i < nodes.Count; ++i)
            {
                var node = nodes[i];
                if (node.children.Any())
                {
                    index = i;
                    return node;
                }
            }
            index = 0;
            return nodes.First();
        }
    }
}