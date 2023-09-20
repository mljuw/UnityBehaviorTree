
using System;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 叶子节点、没有输出端（没有它的子节点）
    /// </summary>
    [GraphBTNodeAttribute(typeof(BTTaskNode))]
    public class BTLeafGraphNode : BtCompositeGraphNode
    {
        protected override Type SpecifyInputType => typeof(BTLeafGraphNode);
        
        public BTLeafGraphNode()
        {
            AddToClassList("leaf-node");
        }
        
    }
}