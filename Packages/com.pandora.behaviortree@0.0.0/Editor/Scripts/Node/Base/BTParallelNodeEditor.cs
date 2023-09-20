using System;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 并行节点
    /// </summary>
    [GraphBTNode(typeof(BTParallelNode))]
    public class BTParallelGraphNode : BtCompositeGraphNode
    {
        private bool isRunningSubTree = false;
        private float blinkInterval = 0.3f;
        private float blinkCounter = 0;
        private StringBuilder tips = new();
        private string displayName = String.Empty;

        protected override Port.Capacity OutputCapacity => Port.Capacity.Single;

        protected override Type SpecifyOutputType => typeof(BTParallelGraphNode);

        public BTParallelGraphNode()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToParentCallback);
            RegisterCallback<DetachFromPanelEvent>(OnDetachParentCallback);
        }

        private void OnDetachParentCallback(DetachFromPanelEvent evt)
        { 
            if (GraphView.DebugTarget is { TreeInst: not null } debugTarget)
            {
                // debugTarget.TreeInst.subTreeStateChangeEvent -= TreeInstOnSubTreeStateChangeEvent;
            }
        }

        private void OnAttachToParentCallback(AttachToPanelEvent evt)
        {
            if (GraphView.IsDebug && GraphView.DebugTarget is { TreeInst: not null } debugTarget)
            {
                displayName = GetDisplayName();
                tips.Clear();
                tips.Append(displayName);
                // debugTarget.TreeInst.subTreeStateChangeEvent += TreeInstOnSubTreeStateChangeEvent;
            }
        }

        private void TreeInstOnSubTreeStateChangeEvent(int nodeIndex, bool running)
        {
            if (nodeIndex == GetNodeIndex())
            {
                isRunningSubTree = running;
                if (isRunningSubTree)
                {
                    blinkCounter = blinkInterval;
                }
                else
                {
                    title = displayName;
                }
            }
        }

        protected override void OnMouseDownCallback(MouseDownEvent evt)
        {
            base.OnMouseDownCallback(evt);
            if (evt.clickCount >= 2)
            {
                var parallelNode = nodeData as BTParallelNode;
                if (parallelNode == null || parallelNode.parallelTree == null) return;
                GraphView.DebugSubTree(GetNodeIndex(), parallelNode.parallelTree);
            }
        }

        public override void DebugTick(float deltaTime)
        {
            base.DebugTick(deltaTime);

            if (isRunningSubTree)
            {
                blinkCounter -= deltaTime;
                if (blinkCounter <= 0)
                {
                    blinkCounter = blinkInterval;

                    if (tips.Length < 3 + displayName.Length)
                    {
                        tips.Append(".");
                    }
                    else
                    {
                        tips.Clear();
                        tips.Append(displayName);
                    }
                }

                title = tips.ToString();
            }
        }
    }
}