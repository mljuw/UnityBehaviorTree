using System;

namespace Pandora.BehaviorTree
{
    [BTNode("队列", "组合")]
    public class BTSequenceNode : BTCompositeNode
    {
        public BTSequenceNode()
        {
            nodeType = BTNodeType.Sequence;
        }

        public override Type GetInstanceClass()
        {
            return typeof(BtSequenceNodeInst<BTSequenceNode>);
        }
    }
    
    public class BtSequenceNodeInst<T> : BTCompositeNodeInst<T> where T : BTSequenceNode
    {
        public override int GetNextChildHandler(int preChild, int childNum, SearchResultType lastResult, bool trickleDown)
        {
            //当前是冒泡时且子节点执行失败则返回上一个节点
            if (!trickleDown && lastResult != SearchResultType.Normal)
            {
                return BTSpecialChild.ReturnToParent;
            }

            return base.GetNextChildHandler(preChild, childNum, lastResult, trickleDown);
        }
        
        public override bool AllowLowerPriorityAbort()
        {
            return false;
        }
    }
}