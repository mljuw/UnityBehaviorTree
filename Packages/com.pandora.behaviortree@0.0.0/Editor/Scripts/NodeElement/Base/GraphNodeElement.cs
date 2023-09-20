using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 此类为装饰器基类
    /// </summary>
    public abstract class GraphNodeElement : VisualElement, IBTEditableElement, IDebugableBTElement
    {
        private Label title;
        public BTGraphNode owner;
        public BTNode nodeData;

        private Label live;

        /// <summary>
        /// 属性的绑定路径
        /// </summary>
        private string bindPropertyPath;
        
        /// <summary>
        /// 获取绑定的属性
        /// </summary>
        private SerializedProperty serializedProp => owner.GraphView.SerializedEditObj.FindProperty(bindPropertyPath);

        /// <summary>
        /// 节点下标的显示元素
        /// </summary>
        private VisualElementIndex indexVisualEle;

        public GraphNodeElement()
        {
            var tree = Resources.Load<VisualTreeAsset>("NodeElement/GraphNodeElement");
            tree.CloneTree(this);


            AddToClassList("graph-node-element");
            
            title = this.Q<Label>("title");
            title.text = "装饰器";

            live = this.Q<Label>("live");
            live.text = "*";
            live.visible = false;

            //注册鼠标点击事件
            RegisterCallback<MouseDownEvent>(OnMouseCallback, TrickleDown.NoTrickleDown);

            //创建元素下标
            indexVisualEle = new VisualElementIndex();
            Add(indexVisualEle);
        }


        /// <summary>
        /// 初始化，创建后必须要调用此函数
        /// </summary>
        /// <param name="ownerNode">视图节点</param>
        /// <param name="node">该元素对应的行为树节点</param>
        /// <param name="nodeSerializedProp">绑定数据的序列化属性</param>
        public virtual void Init(BTGraphNode ownerNode, BTNode node, SerializedProperty nodeSerializedProp)
        {
            //设置这个元素所在的视图节点
            bindPropertyPath = nodeSerializedProp.propertyPath;
            owner = ownerNode;
            nodeData = node;
            title.text = GetDisplayName();
        }

        /// <summary>
        /// 助手节点在被调用 OnBecomeRelevant时 
        /// </summary>
        public void OnDebugBecomeRelevant()
        {
            live.visible = true;
        }
        
        /// <summary>
        /// 助手节点在被调用 OnCeaseRelevant时 
        /// </summary>
        public void OnDebugCeaseRelevant()
        {
            live.visible = false;
        }

        /// <summary>
        /// 设置元素下标
        /// </summary>
        /// <param name="idx"></param>
        public void SetNodeIndex(int idx)
        {
            indexVisualEle.SetIndex(idx);
        }

        /// <summary>
        /// 获取节点下标 
        /// </summary>
        /// <returns></returns>
        public int GetNodeIndex()
        {
            return indexVisualEle.Index;
        }

        /// <summary>
        /// 当鼠标点击了这个元素时的回调
        /// </summary>
        /// <param name="evt"></param>
        private void OnMouseCallback(MouseDownEvent evt)
        {
            owner.GraphView.OnSelectedBTEditableElement(this);
            evt.StopImmediatePropagation();
        }


        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(nodeData.customName))
            {
                return nodeData.customName;
            }
            var nodeAttr = nodeData.GetType().GetCustomAttribute<BTNodeAttribute>();
            return nodeAttr.displayName;
        }


        public virtual Type EditInspectorClass
        {
            get => null;
        }
        
        
        #region 实现IBTEditableElement接口

        /// <summary>
        /// 当用户取消选择时的回调
        /// </summary>
        public void UnSelected()
        {
            title.RemoveFromClassList("selected");
        }

        /// <summary>
        /// 当用户选择时的回调
        /// </summary>
        public void Selected()
        {
            title.AddToClassList("selected");
        }

        public BTNode GetNodeData()
        {
            return nodeData;
        }

        public SerializedProperty GetSerializedProp()
        {
            return serializedProp;
        }


        /// <summary>
        /// 设置自定义颜色
        /// </summary>
        /// <param name="color"></param>
        public void OnCustomColorChange(Color color)
        {
            
        }

        public void OnCustomNameChange(string newName)
        {
            title.text = GetDisplayName();
        }

        #endregion

        #region 调试

        public virtual void SetActivation(bool activated)
        {
        }

        public virtual void DebugTick(float deltaTime)
        {
        }

        #endregion
    }
}