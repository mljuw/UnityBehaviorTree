using System;

namespace Pandora.BehaviorTree
{
    [BTNode("根节点")]
    public class BTRootNode : BTCompositeNode
    {
        public override Type GetInstanceClass()
        {
            return typeof(BtRootNodeInst);
        }
    }
    
    public class BtRootNodeInst : BTCompositeNodeInst<BTRootNode>
    {

    }
}