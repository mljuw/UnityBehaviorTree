using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using GNode = UnityEditor.Experimental.GraphView.Node;

namespace Pandora.BehaviorTree
{

    /// <summary>
    /// 行为树节点
    /// </summary>
    [GraphBTNode(typeof(BTNode))]
    public abstract class BTGraphNode : GNode, IDebugableBTElement, IBTEditableElement
    {
        /// <summary>
        /// 输入端
        /// </summary>
        public BTNodePort inPort;

        /// <summary>
        /// 输出端 
        /// </summary>
        public BTNodePort outPort;

        /// <summary>
        /// 节点元素存放容器
        /// </summary>
        public VisualElement nodeItemContainer;

        /// <summary>
        /// 对应行为树节点
        /// </summary>
        public BTNode nodeData;

        private string bindPropertyPath;
        public SerializedProperty serializedProp => GraphView.SerializedEditObj.FindProperty(bindPropertyPath);

        /// <summary>
        /// 保存节点元素的数组（装饰器、服务）
        /// </summary>
        protected List<GraphNodeElement> nodeElements = new ();

        private BehaviorTreeGraphView mGraphView;

        /// <summary>
        /// 下标显示元素
        /// </summary>
        protected VisualElementIndex elementIdx;

        /// <summary>
        /// 边框
        /// </summary>
        protected VisualElement nodeBorder;

        /// <summary>
        /// 调试的边框
        /// </summary>
        protected VisualDebugBorder debugBorder;

        public BTGraphNode()
        {
            //设置节点可选择、可移动、可删除
            capabilities = Capabilities.Selectable;
            capabilities |= Capabilities.Movable;
            capabilities |= Capabilities.Deletable;

            //创建调试边框
            debugBorder = new VisualDebugBorder();
            Add(debugBorder);
            // debugBorder.Blink();

            //隐藏下拉按钮
            titleButtonContainer.Clear();
            var titleLabel = titleContainer.Q<Label>(name: "title-label");
            titleLabel.style.marginRight = 6;

            nodeBorder = this.Q<VisualElement>("node-border");
            //复写超出区域的内容也显示出来(目前下标会显示在右上角并且超出了节点大小范围)
            nodeBorder.style.overflow = Overflow.Visible;

            //清除节点默认的下拉按钮
            nodeItemContainer = mainContainer.Q("contents", (string)null).Q("top");
            nodeItemContainer.Clear();

            var nodeContainer = this.Q<VisualElement>("node-border");

            //创建端口
            inPort = BTNodePort.CreateBTPort<BTEdge>(this, Orientation.Vertical, Direction.Input, InputCapacity, SpecifyInputType);
            nodeContainer.Insert(0, inPort);
            
            inPort.DisconnectedEvent += InPortDisconnected;
            inPort.ConnectedEvent += InPortConnected;

            outPort = BTNodePort.CreateBTPort<BTEdge>(this, Orientation.Vertical, Direction.Output, OutputCapacity, SpecifyOutputType);
            nodeContainer.Add(outPort);

            //设置节点在小地图的颜色
            elementTypeColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            //注册鼠标点击节点的事件
            RegisterCallback<MouseDownEvent>(OnMouseDownCallback, TrickleDown.NoTrickleDown);
            //注册被移除时的事件
            RegisterCallback<DetachFromPanelEvent>(OnDelete);

            //添加下标显示元素到节点中
            elementIdx = new VisualElementIndex();
            Add(elementIdx);
        }

        private void InPortConnected(BTNodePort parentPort)
        {
            GraphView.NodeConnectionChange(this);
        }

        /// <summary>
        /// 输入端断开链接
        /// </summary>
        private void InPortDisconnected()
        {
            GraphView.NodeConnectionChange(this);
        }

        /// <summary>
        /// 指定输入端口支持类型
        /// </summary>
        protected virtual Type SpecifyInputType => typeof(BTGraphNode);
        
        /// <summary>
        /// 指定输入端口是否多链接
        /// </summary>
        protected virtual Port.Capacity InputCapacity => Port.Capacity.Single;

        /// <summary>
        /// 指定输出端口支持类型
        /// </summary>
        protected virtual Type SpecifyOutputType => typeof(BTGraphNode);
        
        /// <summary>
        /// 指定输出端口是否多链接
        /// </summary>
        protected virtual Port.Capacity OutputCapacity => Port.Capacity.Multi;

        public virtual Type EditInspectorClass => typeof(ElementInspector);

        public BehaviorTreeGraphView GraphView => mGraphView ??= GetFirstAncestorOfType<BehaviorTreeGraphView>();

        /// <summary>
        /// 节点删除时的回调
        /// </summary>
        /// <param name="evt"></param>
        private void OnDelete(DetachFromPanelEvent evt)
        {
            var inPortConnections = inPort.connections.ToArray();
            foreach (var edge in inPortConnections)
            {
                edge.input.Disconnect(edge);
                edge.output.Disconnect(edge);
                edge.RemoveFromHierarchy();
            }

            inPort.DisconnectAll();

            var outPortConnections = outPort.connections.ToArray();
            foreach (var edge in outPortConnections)
            {
                edge.input.Disconnect(edge);
                edge.output.Disconnect(edge);
                edge.RemoveFromHierarchy();
            }

            outPort.DisconnectAll();
        }


        /// <summary>
        /// 传入行为树节点初始化编辑节点
        /// </summary>
        /// <param name="btNode"></param>
        /// <param name="nodeSerializedProp"></param>
        public void Init(BTNode btNode, SerializedProperty nodeSerializedProp)
        {
            bindPropertyPath = nodeSerializedProp.propertyPath;
            nodeData = btNode;
            title = GetDisplayName();
            SetCustomColor(nodeData.customColor);
        }

        /// <summary>
        /// 设置节点显示的下标
        /// </summary>
        /// <param name="idx"></param>
        public void SetNodeIndex(int idx)
        {
            elementIdx.SetIndex(idx);
        }

        /// <summary>
        /// 鼠标点击了节点
        /// </summary>
        /// <param name="evt"></param>
        protected virtual void OnMouseDownCallback(MouseDownEvent evt)
        {
            if (GetType().GetInterfaces().Contains(typeof(IBTEditableElement)))
            {
                SelectNode(this);
            }
        }

        public void SelectNode(IBTEditableElement node)
        {
            //在监视面板显示编辑信息
            GraphView.OnSelectedBTEditableElement(node);
        }


        /// <summary>
        /// 当更新节点位置时的回调
        /// </summary>
        public override void UpdatePresenterPosition()
        {
            Undo.RecordObject(GraphView.EditTarget, "Update node position.");
            
            //通知GraphView 节点更新
            nodeData.visitPos = GetPosition().position;
            GraphView.OnUpdateNodePos();
        }

        /// <summary>
        /// 获取显示的节点名称
        /// </summary>
        /// <returns></returns>
        public string GetDisplayName()
        {
            if (nodeData != null)
            {
                //如果有自定义标题则显示
                if (!string.IsNullOrEmpty(nodeData.customName))
                {
                    return nodeData.customName;
                }

                var attribute = nodeData.GetType().GetCustomAttribute<BTNodeAttribute>();
                return attribute.displayName;
            }

            return String.Empty;
        }
        

        /// <summary>
        /// 设置自定义颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetCustomColor(Color color)
        {
            inPort.style.backgroundColor = color;
        }


        /// <summary>
        /// 添加节点元素
        /// </summary>
        /// <param name="nodeEle"></param>
        public void AddNodeElement(GraphNodeElement nodeEle)
        {
            nodeElements.Add(nodeEle);
            nodeItemContainer.Add(nodeEle);
            GraphView.OnUpdateGraphBTNode(this);
        }

        /// <summary>
        /// 删除节点元素
        /// </summary>
        /// <param name="nodeEle"></param>
        public void RemoveElement(GraphNodeElement nodeEle)
        {
            nodeElements.Remove(nodeEle);
            nodeItemContainer.Remove(nodeEle);
            GraphView.OnUpdateGraphBTNode(this);
        }

        /// <summary>
        /// 获取节点元素 （装饰器、服务）
        /// </summary>
        /// <returns></returns>
        public List<GraphNodeElement> GetNodeElements()
        {
            return nodeElements;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (GraphView.IsDebug) return;

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("删除节点", GraphView.OnClickDeleteNode,
                !GraphView.IsDebug ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }
        
        
        #region 实现IBTEditableElement接口
        
        /// <summary>
        /// 获取节点下标 
        /// </summary>
        /// <returns></returns>
        public int GetNodeIndex()
        {
            return elementIdx.Index;
        }

        public void UnSelected()
        {
            
        }

        public void Selected()
        {
            
        }
        
        public BTNode GetNodeData()
        {
            return nodeData;
        }

        public SerializedProperty GetSerializedProp()
        {
            return serializedProp;
        }

        public void OnCustomColorChange(Color color)
        {
            SetCustomColor(color);
        }
        
        public void OnCustomNameChange(string newName)
        {
            title = GetDisplayName();
        }
        
        #endregion

        #region 调试

        public void SetEnableDebug(bool bDebug)
        {
            if (bDebug)
            {
                capabilities &= ~Capabilities.Selectable;
                capabilities &= ~Capabilities.Movable;
                capabilities &= ~Capabilities.Deletable;
            }
            else
            {
                capabilities |= Capabilities.Selectable;
                capabilities |= Capabilities.Movable;
                capabilities |= Capabilities.Deletable;
            }
        }

        public virtual void SetActivation(bool activated)
        {
            debugBorder.SetActivated(activated);
        }

        public virtual void DebugTick(float deltaTime)
        {
            debugBorder.Tick(deltaTime);
        }

        #endregion
    }
    
    /// <summary>
    /// 节点下标显示元素
    /// </summary>
    public class VisualElementIndex : VisualElement
    {
        private int index;

        public int Index => index;

        private Label lblIndex;

        public VisualElementIndex()
        {
            style.borderBottomLeftRadius = 15;
            style.borderBottomRightRadius = 15;
            style.borderTopLeftRadius = 15;
            style.borderTopRightRadius = 15;
            style.justifyContent = Justify.Center;

            lblIndex = new Label
            {
                text = "0"
            };
            Add(lblIndex);

            AddToClassList("node-idx");
        }

        /// <summary>
        /// 设置元素下标
        /// </summary>
        /// <param name="idx"></param>
        public void SetIndex(int idx)
        {
            lblIndex.text = idx.ToString();
            index = idx;
        }
    }
}