using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Pandora.BehaviorTree
{

    public enum BTFlowAbortMode
    {
        None,
        LowerPriority,
        Self,
        Both
    }

    [BTNode("装饰器", description = "用于判断这个节点能否执行的条件")]
    public abstract class BTDecoratorNode : BTAuxiliaryNode
    {
        [Header("中断模式")]
        public BTFlowAbortMode flowAbortMode;

        [Header("条件反转")]
        public bool conditionReversal = false;

        public BTDecoratorNode()
        {
            nodeType = BTNodeType.Decorator;
        }

        public override Type GetInstanceClass()
        {
            return typeof(DecoratorNodeInst<BTDecoratorNode>);
        }
    }

    public interface IDecoratorNodeInst : IAuxiliaryNodeInst
    {
        public BTFlowAbortMode FlowAbortMode { get; }
        
        public bool WrappedPerformConditionCheck();
        
        public bool WrappedRawConditionCheck();

        public BTNodeInstance ParentNode { get; }
        
    }
    
    public abstract class DecoratorNodeInst<T> : BTAuxiliaryNodeInst<T>, IDecoratorNodeInst where T : BTDecoratorNode
    {
        public BTFlowAbortMode FlowAbortMode => Def.flowAbortMode;

        public bool WrappedPerformConditionCheck()
        { 
            return Def.conditionReversal ? !PerformConditionCheck() : PerformConditionCheck();
        }
        
        public bool WrappedRawConditionCheck()
        {
            return Def.conditionReversal ? !RawConditionCheck() : RawConditionCheck();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool PerformConditionCheck()
        {
            return RawConditionCheck();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool RawConditionCheck()
        {
            return true;
        }
        
        
    }
}