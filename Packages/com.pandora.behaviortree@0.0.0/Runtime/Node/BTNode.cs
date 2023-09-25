using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    public enum BTNodeType
    {
        Root,
        Sequence,
        Selector,
        Service,
        Decorator,
        Task,
    }

    public enum ActiveNodeStateType
    {
        Abort,
        Cancel,
        Success,
    }

    public enum SearchResultType
    {
        Normal,
        CheckFail,
        ExecuteFail,
    }

    public static class BTSpecialChild
    {
        public static readonly int ReturnToParent = -2;
    }


    public class NotDefineNodeInstanceClassException : Exception
    {
        public NotDefineNodeInstanceClassException(string message) : base(message)
        {
        }
    }


    [BTNode("节点"), Serializable]
    public class BTNode
    {
        protected BTNodeType nodeType = BTNodeType.Root;

        public BTNodeType NodeType => nodeType;

        [SerializeReference, HideInInspector] public List<BTNode> children = new();
        
        [SerializeReference, HideInInspector] public List<BTAuxiliaryNode> auxNodes = new();

        /// <summary>
        /// 自定义名字
        /// </summary>
        [HideInInspector] public string customName;

        /// <summary>
        /// 自定义颜色
        /// </summary>
        [HideInInspector] public Color customColor = Color.black;

        /// <summary>
        /// 视图显示位置
        /// </summary>
        [HideInInspector] public Vector2 visitPos = default;

        public virtual Type GetInstanceClass()
        {
            return typeof(BTNodeInstance<BTNode>);
        }


        /// <summary>
        /// 是否合成节点
        /// </summary>
        /// <returns></returns>
        public bool NodeIsComposite()
        {
            return NodeType is BTNodeType.Selector or BTNodeType.Sequence or BTNodeType.Root;
        }


        /// <summary>
        /// 是否叶子节点
        /// </summary>
        /// <returns></returns>
        public bool NodeIsLeaf()
        {
            return NodeType == BTNodeType.Task;
        }


        /// <summary>
        /// 是否助手节点
        /// </summary>
        /// <returns></returns>
        public bool NodeIsAuxiliary()
        {
            return NodeIsDecorator() || NodeIsService();
        }

        /// <summary>
        /// 是否任务节点
        /// </summary>
        /// <returns></returns>
        public bool NodeIsService()
        {
            return NodeType == BTNodeType.Service;
        }

        /// <summary>
        /// 是否装饰器节点
        /// </summary>
        /// <returns></returns>
        public bool NodeIsDecorator()
        {
            return NodeType == BTNodeType.Decorator;
        }

        public bool NodeIsSelector()
        {
            return NodeType == BTNodeType.Selector;
        }
    }

    public interface IBTNodeInstance : IDisposable
    {
        BTNode BaseDef => null;

        int NodeIndex => 0;

        void TickNode(float deltaTime);

        void ReceivedMessage<T>(string messageName, T payload);
    }

    public class BTNodeInstance<T> : BTNodeInstance where T : BTNode
    {
        public T Define { get; private set; }

        public override BTNode BaseDef => Define;

        internal override void Init(BehaviorTreeInstance tree, BTNode template)
        {
            base.Init(tree, template);
            Define = template as T;
        }
    }

    public abstract class BTNodeInstance : IBTNodeInstance
    {
        protected BehaviorTreeInstance treeInst;

        public virtual BTNode BaseDef => null;

        public BTNodeInstance ParentNode { get; set; }

        internal List<BTNodeInstance> children = new();
        
        internal List<IAuxiliaryNodeInst> auxNodes = new();

        public int NodeIndex { get; set; }

        internal virtual void Init(BehaviorTreeInstance tree, BTNode template)
        {
            treeInst = tree;
        }

        public virtual void TickNode(float deltaTime)
        {
        }

        public virtual void ReceivedMessage<T>(string messageName, T payload)
        {
        }

        /// <summary>
        /// 在开始搜索子节点前触发
        /// </summary>
        public virtual void OnSearchStart()
        {
        }

        /// <summary>
        /// 搜索结束
        /// </summary>
        public virtual void OnLeave(SearchResultType searchResult)
        {
            
        }
        
        /// <summary>
        /// 实例化Node运行时
        /// </summary>
        /// <param name="treeInst"></param>
        /// <param name="nodeDef"></param>
        /// <returns></returns>
        public static BTNodeInstance New(BehaviorTreeInstance treeInst, BTNode nodeDef)
        {
            var insClass = nodeDef.GetInstanceClass();
            if (null == insClass)
            {
                throw new NotDefineNodeInstanceClassException("没有定义BTNode的运行时类型");
            }

            var obj = (BTNodeInstance)Activator.CreateInstance(insClass);
            obj.Init(treeInst, nodeDef);
            return obj;
        }

        public virtual void Dispose()
        {
        }
    }

    #region Attribute

    /// <summary>
    /// 1.用于描述节点在行为树视图的信息
    /// 2.编辑器下的节点与运行时的节点建立关系。
    /// 3.菜单栏上显示的信息
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class BTNodeAttribute : Attribute
    {
        /// <summary>
        /// 显示在视图上的名字
        /// </summary>
        public string displayName;

        /// <summary>
        /// 分组，用于创建节点的菜单上。
        /// </summary>
        public string group;

        /// <summary>
        /// 描述，用于编辑节点时显示在监视面板。
        /// </summary>
        public string description;

        public BTNodeAttribute(string displayName, string group = "默认", string description = "")
        {
            this.displayName = displayName;
            this.group = group;
            this.description = description;
        }
    }

    #endregion
}