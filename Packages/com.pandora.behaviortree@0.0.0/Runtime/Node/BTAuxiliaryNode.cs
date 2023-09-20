using System;
using Codice.Client.BaseCommands;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 助手类基类
    /// </summary>
    [Serializable]
    public abstract class BTAuxiliaryNode : BTNode
    {
        public override Type GetInstanceClass()
        {
            return typeof(BTAuxiliaryNodeInst<BTAuxiliaryNode>);
        }
    }

    public interface IAuxiliaryNodeInst : IBTNodeInstance
    {
        bool IsRelevant => false;
        void WrappedOnBecomeRelevant();
        void WrappedOnCeaseRelevant();
    }
 
    public abstract class BTAuxiliaryNodeInst<T> : BTNodeInstance<T>, IAuxiliaryNodeInst where T : BTAuxiliaryNode
    {
        private bool isRelevant = false;

        public bool IsRelevant => isRelevant;

        public void WrappedOnBecomeRelevant()
        {
            if (isRelevant) return;
            OnBecomeRelevant();
            treeInst.NotifyAuxBecomeRelevant(this);
            isRelevant = true;
        }

        public void WrappedOnCeaseRelevant()
        {
            if (!isRelevant) return;
            OnCeaseRelevant();
            treeInst.NotifyAuxCeaseRelevant(this);
            isRelevant = false;
        }
        
        /// <summary>
        /// 在进入时调用
        /// 只有叶节点被激活(运行时)才会调用
        /// </summary>
        /// <param name="btComp"></param>
        protected virtual void OnBecomeRelevant()
        {
            
        }
        
        /// <summary>
        /// 在离开时调用
        /// 叶节点被取消激活时助手节点会调用这个函数
        /// </summary>
        /// <param name="btComp"></param>
        protected virtual void OnCeaseRelevant()
        {
            
        }

    }
}