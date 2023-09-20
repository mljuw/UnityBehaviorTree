using UnityEngine;

namespace Pandora.BehaviorTree
{
    [GraphBTNode(typeof(BTRootNode))]
    public class BTRootGraphNode : BtCompositeGraphNode
    {
        public BTRootGraphNode():base()
        {
            inPort.visible = false;
            inPort.RemoveFromHierarchy();
            
            elementTypeColor = new Color(1,0,0,0.3f);
        }
    }
}