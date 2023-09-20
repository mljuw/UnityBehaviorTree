using System;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    [BTNode("并行节点", "组合")]
    public class BTParallelNode : BTCompositeNode
    {
        [Header("并行子树")]
        public BehaviorTreeAsset parallelTree;
        
        public BTParallelNode()
        {
            nodeType = BTNodeType.Parallel;
        }

        public override Type GetInstanceClass()
        {
            return typeof(BTParallelNodeInst);
        }
    }

    public class BTParallelNodeInst : BTCompositeNodeInst<BTParallelNode>
    {
        private BehaviorTreeInstance subSubTreeInst;
        
        public BehaviorTreeInstance SubTreeInst => subSubTreeInst;

        public BTParallelNodeInst()
        {
        }

        public override void OnSearchStart()
        {
            base.OnSearchStart();
            treeInst.activatedTaskNodeEvent += OnActivatedTaskNodeEvent;
        }

        public override void OnLeave(SearchResultType searchResult)
        {
            base.OnLeave(searchResult);
            Cleanup();
        }
        
        private void OnActivatedTaskNodeEvent(BTNodeInstance activationNode)
        {
            if (0 >= children.Count) return;
            if (activationNode == children[0])
            {
                StartSubTree();
            }
        }

        public override void TickNode(float deltaTime)
        {
            base.TickNode(deltaTime);
            if (subSubTreeInst != null)
            {
                subSubTreeInst.Tick(deltaTime);
            }
        }

        private void StartSubTree()
        {
            //开启并行子树
            if (null != Def.parallelTree)
            {
                subSubTreeInst ??= new BehaviorTreeInstance(treeInst.Owner);
                subSubTreeInst.SetShareBlackboardInst(treeInst.Blackboard);
                subSubTreeInst.StopTree();
                subSubTreeInst.StartTree(Def.parallelTree);
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
            Cleanup();
        }

        private void Cleanup()
        {
            if (subSubTreeInst != null)
            {
                subSubTreeInst.StopTree();
                treeInst.activatedTaskNodeEvent += OnActivatedTaskNodeEvent;
            }
        }
        
    }
}