using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    [Serializable]
    public abstract class BTCompositeNode : BTNode
    {
        public override Type GetInstanceClass()
        {
            return typeof(BTCompositeNodeInst<BTCompositeNode>);
        }
    }

    public interface ICompositeNodeInst : IBTNodeInstance
    {
        /// <summary>
        /// 返回是否能被低优先级的节点打断
        /// </summary>
        /// <returns></returns>
        bool AllowLowerPriorityAbort();

        /// <summary>
        /// 选择下一个子节点的处理函数
        /// </summary>
        /// <param name="preChild">上一个子节点下标,没有上一个则是-1</param>
        /// <param name="childNum">子节点数量</param>
        /// <param name="lastResult">上一个节点结果</param>
        /// <param name="trickleDown">是否在涓滴</param>
        /// <returns>返回下一个子节点下标</returns>
        int GetNextChildHandler(int preChild, int childNum, SearchResultType lastResult, bool trickleDown);
    }
    
    public abstract class BTCompositeNodeInst<T> : BTNodeInstance<T>, ICompositeNodeInst where T : BTCompositeNode
    {
        public virtual int GetNextChildHandler(int preChild, int childNum, SearchResultType lastResult, bool trickleDown)
        {
            return preChild + 1;
        }
        
        public virtual bool AllowLowerPriorityAbort()
        {
            return true;
        }
    }
}