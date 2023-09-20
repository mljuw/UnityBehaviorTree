using System;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// ‘Wait’节点的GraphNode
    /// </summary>
    [GraphBTNode(typeof(BTT_Wait))]
    public class BTTWaitGraphNode : BTLeafGraphNode
    {
        public override Type EditInspectorClass
        {
            get => typeof(BTTWaitInspector);
        }
    }
    
    /// <summary>
    /// 监视面板
    /// </summary>
    public class BTTWaitInspector : EditableElementInspector<BTT_Wait>
    {
    }
    
}