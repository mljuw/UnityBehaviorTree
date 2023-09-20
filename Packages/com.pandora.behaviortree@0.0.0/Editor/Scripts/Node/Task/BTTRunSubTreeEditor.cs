using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 运行子树节点的GraphNode
    /// </summary>
    [GraphBTNode(typeof(BTT_RunSubTree))]
    public class BTTRunSubTreeGraphNode: BTLeafGraphNode
    {
        protected override void OnMouseDownCallback(MouseDownEvent evt)
        {
            base.OnMouseDownCallback(evt);
            if (evt.clickCount >= 2)
            {
                var subTreeNode = nodeData as BTT_RunSubTree;
                if (subTreeNode == null || subTreeNode.GetSubTreeAsset() == null) return;
                GraphView.DebugSubTree(GetNodeIndex(), subTreeNode.GetSubTreeAsset());
            }
        }
    }
}