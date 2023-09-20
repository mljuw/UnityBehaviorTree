using System;

namespace Pandora.BehaviorTree
{
    [BTNode("选择器", "组合")]
    public class BTSelectorNode : BTCompositeNode
    {
        public BTSelectorNode()
        {
            nodeType = BTNodeType.Selector;
        }

        public override Type GetInstanceClass()
        {
            return typeof(BtSelectorNodeInst<BTSelectorNode>);
        }
    }

    public class BtSelectorNodeInst<T> : BTCompositeNodeInst<T> where T : BTSelectorNode
    {
        public override int GetNextChildHandler(int preChild, int childNum, SearchResultType lastResult, bool trickleDown)
        {
            //如果当前是冒泡并且子节点执行成功则返回上一个节点
            if (!trickleDown && lastResult == SearchResultType.Normal)
            {
                return BTSpecialChild.ReturnToParent;
            }

            return base.GetNextChildHandler(preChild, childNum, lastResult, trickleDown);
        }
    }
}