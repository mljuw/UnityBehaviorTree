using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 组合节点基类, 组合节点下有辅助节点
    /// </summary>
    public abstract class BtCompositeGraphNode : BTGraphNode
    {
        
    }

    /// <summary>
    /// 选择器节点
    /// </summary>
    [GraphBTNode(typeof(BTSelectorNode))]
    public class SelectorGraphNode : BtCompositeGraphNode
    {
       
    }

    /// <summary>
    /// 队列(Sequence)节点
    /// </summary>
    [GraphBTNode(typeof(BTSequenceNode))]
    public class SequenceGraphNode : BtCompositeGraphNode
    {
        
    }
    
}