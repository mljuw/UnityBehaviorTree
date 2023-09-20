using System;

namespace Pandora.BehaviorTree
{
    [GraphBTNode(typeof(BTServiceNode))]
    public class ServiceNodeElement : GraphNodeElement
    {
        public override Type EditInspectorClass => typeof(ElementInspector);
    }
    
}