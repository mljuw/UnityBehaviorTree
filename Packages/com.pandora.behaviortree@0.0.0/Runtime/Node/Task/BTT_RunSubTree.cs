using System;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 运行子行为树
    /// </summary>
    [BTNode("运行子行为树", "任务")]
    public class BTT_RunSubTree : BTTaskNode
    {
        [Header("行为树资产")]
        public BehaviorTreeAsset subTreeAsset;

        public BehaviorTreeAsset GetSubTreeAsset()
        {
            return subTreeAsset;
        }

        public override Type GetInstanceClass()
        {
            return typeof(BTTSubTreeNodeInst);
        }
        
    }


    public class BTTSubTreeNodeInst : BTTaskNodeInst<BTT_RunSubTree>
    {
        private BehaviorTreeInstance subSubTreeInst;

        public BehaviorTreeInstance SubTreeInst => subSubTreeInst;

        public override void Dispose()
        {
            base.Dispose();
            Cleanup();
        }

        private void Cleanup()
        {
            if (subSubTreeInst != null)
            {
                subSubTreeInst.treeSearchFinish -= OnTreeSearchFinish;
                subSubTreeInst.StopTree();
            }
        }

        public override void OnActivation()
        {
            if (null == Define.subTreeAsset)
            {
                FinishTask(false);
                return;
            }

            subSubTreeInst ??= new BehaviorTreeInstance(treeInst.Owner);
            subSubTreeInst.SetShareBlackboardInst(treeInst.Blackboard);
            subSubTreeInst.StopTree();
            subSubTreeInst.StartTree(Define.subTreeAsset);

            subSubTreeInst.treeSearchFinish += OnTreeSearchFinish;
        }

        public override void TickNode(float deltaTime)
        {
            base.TickNode(deltaTime);
            if (subSubTreeInst != null)
            {
                subSubTreeInst.Tick(deltaTime);
            }
        }

        public override void OnDeactivation(ActiveNodeStateType stateType)
        {
            Cleanup();
            base.OnDeactivation(stateType);
        }

        private void OnTreeSearchFinish(BehaviorTreeInstance subTreeIns)
        {
            FinishTask(true);
        }
        
    }
}