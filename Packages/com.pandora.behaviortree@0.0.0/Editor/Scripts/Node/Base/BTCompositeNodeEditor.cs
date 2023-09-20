using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 组合节点基类, 组合节点下有辅助节点
    /// </summary>
    public abstract class BtCompositeGraphNode : BTGraphNode
    {
        
        #region 右键菜单
        
        /// <summary>
        /// 创建右键菜单
        /// </summary>
        /// <param name="evt"></param>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (GraphView.IsDebug || nodeData.NodeType == BTNodeType.Root) return;
            
            if (evt.target is BTGraphNode)
            {
                evt.menu.AppendAction("添加装饰器", OnContextMenuCreateItem, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }

            if (GraphView.CurSelectedNodeElement is GraphNodeElement graphNodeElement && graphNodeElement.owner == this)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("删除装饰器", OnClickDeleteNodeElement);
            }
        }
        
        /// <summary>
        /// 删除节点元素
        /// </summary>
        /// <param name="obj"></param>
        private void OnClickDeleteNodeElement(DropdownMenuAction obj)
        {
            if (GraphView.CurSelectedNodeElement is GraphNodeElement graphNodeElement && graphNodeElement.owner == this)
            {
                //删除节点数据
                var compositeNode = nodeData as BTCompositeNode;
                compositeNode.auxNodes.Remove(graphNodeElement.nodeData as BTAuxiliaryNode);
                RemoveElement(graphNodeElement);
                GraphView.OnRemoveNodeElement(this, graphNodeElement);
            }
        }

        /// <summary>
        /// 点击创建节点菜单后显示 具体的 节点创建类型选择窗口
        /// </summary>
        /// <param name="action"></param>
        private void OnContextMenuCreateItem(DropdownMenuAction action)
        {
            var screenMousePosition = GraphView.window.position.position + action.eventInfo.mousePosition;
            var ctx = new SearchWindowContext(screenMousePosition);
            var provider = GraphView.nodeElementCreateProvider;
            provider.SetParentGraphNode(this);
            SearchWindow.Open(ctx, provider);
        }
        
        #endregion
        
    }

    /// <summary>
    /// 选择器节点
    /// </summary>
    [GraphBTNode(typeof(BTSelectorNode))]
    public class SelectorGraphNode : BtCompositeGraphNode
    {
       
    }

    /// <summary>
    /// 队列(Sequence)节点
    /// </summary>
    [GraphBTNode(typeof(BTSequenceNode))]
    public class SequenceGraphNode : BtCompositeGraphNode
    {
        
    }
    
}