using System;

namespace Pandora.BehaviorTree
{
    [BTNode("任务节点")]
    public abstract class BTTaskNode : BTCompositeNode
    {
        public BTTaskNode()
        {
            nodeType = BTNodeType.Task;
        }

        public override Type GetInstanceClass()
        {
            return typeof(BTTaskNodeInst<BTTaskNode>);
        }
    }

    public interface ITaskNodeInstance : IBTNodeInstance
    {
        public void OnActivation();
        public void OnDeactivation(ActiveNodeStateType stateType);
    }

    public abstract class BTTaskNodeInst<T> : BTNodeInstance<T>, ITaskNodeInstance where T : BTTaskNode
    {
        internal bool activated = false;

        
        protected void FinishTask( bool bSuccess)
        {
            treeInst.FinishTask(this, bSuccess);
        }

        protected void AbortTask()
        {
            treeInst.AbortTask(this);
        }
        
        /// <summary>
        /// 节点激活时
        /// </summary>
        /// <param name="btComp"></param>
        public virtual void OnActivation()
        {
        }

        
        /// <summary>
        /// 节点取消激活时
        /// </summary>
        /// <param name="btComp"></param>
        public virtual void OnDeactivation(ActiveNodeStateType stateType)
        {
        }

    }

}
