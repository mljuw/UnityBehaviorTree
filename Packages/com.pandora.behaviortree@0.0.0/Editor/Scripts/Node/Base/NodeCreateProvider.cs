using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 创建代理者
    /// 用于创建行为树编辑节点
    /// </summary>
    public class NodeCreateProvider : ScriptableObject, ISearchWindowProvider
    {
        private BehaviorTreeWindow window;
        private BehaviorTreeGraphView graphView;
        private NodeCreationTree creationTree = new();
        Dictionary<Type, Type> graphNodeMapping = new();

        public void Init(BehaviorTreeWindow window, BehaviorTreeGraphView graphView)
        {
            this.window = window;
            this.graphView = graphView;

            FetchAllBTNode();
            FetchAllGroupNode();
        }

        private void FetchAllBTNode()
        {
            //从程序集中查找实现了BTNode的所有类类型
            var types =
                from t in TypeCache.GetTypesDerivedFrom(typeof(BTNode))
                where !t.IsSubclassOf(typeof(BTAuxiliaryNode)) && !t.IsAbstract
                select t;
            
            foreach (var t in types)
            {
                var attr = t.GetCustomAttribute<BTNodeAttribute>();
                var group = creationTree.CreateGroupByPathIfNeeded(attr.group);
                group.datas.Add(t);
            }
        }

        private void FetchAllGroupNode()
        {
            var types =
                from t in TypeCache.GetTypesDerivedFrom(typeof(BTGraphNode))
                where !t.IsAbstract
                select t;

            //将他们与BTNode建立关联
            foreach (var nodeClass in types)
            {
                var attr = nodeClass.GetCustomAttribute<GraphBTNodeAttribute>();
                graphNodeMapping[attr.nodeClass] = nodeClass;
            }
        }
        
        /// <summary>
        /// 创建可创建节点的列表（树）
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                //设置一级内容
                new SearchTreeGroupEntry(new GUIContent("创建节点"), 0),
            };

            foreach (var g in creationTree.groups)
            {
                CreateMenuByGroup(g, ref tree);
            }

            return tree;
        }

        private void CreateMenuByGroup(NodeCreationGroup group, ref List<SearchTreeEntry> menuTree)
        {
            menuTree.Add(new SearchTreeGroupEntry(new GUIContent(group.groupName), group.deep));

            foreach (var child in group.children)
            {
                CreateMenuByGroup(child, ref menuTree);
            }

            foreach (var data in group.datas)
            {
                var attr = data.GetCustomAttribute<BTNodeAttribute>();
                var entry = new SearchTreeEntry(new GUIContent(attr.displayName))
                {
                    level = group.deep + 1, userData = data
                };
                menuTree.Add(entry);
            }
        }

        /// <summary>
        /// 当选择了某一项可创建节点时的回调
        /// </summary>
        /// <param name="searchTreeEntry"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            //获取在窗体坐标系的鼠标位置
            var mousePosition = window.rootVisualElement.ChangeCoordinatesTo(window.rootVisualElement.parent,
                context.screenMousePosition - window.position.position);
            
            //转换到 行为树视图下的鼠标位置
            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(mousePosition);

            //根据上下文创建视图节点
            var nodeType = (Type)searchTreeEntry.userData;

            return null != CreateGraphNodeAndData(nodeType, graphMousePosition);
        }

        /// <summary>
        /// 创建节点的显示Node和创建节点数据
        /// </summary>
        /// <param name="btNodeType"></param>
        /// <param name="graphMousePosition"></param>
        /// <returns></returns>
        public BTGraphNode CreateGraphNodeAndData(Type btNodeType, Vector2 graphMousePosition)
        {
            if (graphView.IsDebug) return null;
            
            var graphNode = CreateGraphNode(btNodeType);
            if (null == graphNode) return null;
            
            Undo.RecordObject(graphView.EditTarget, "Create node.");
            
            var serializedObj = graphView.SerializedEditObj;
            //创建节点数据
            var btNode = (BTNode)Activator.CreateInstance(btNodeType);
            btNode.visitPos = graphMousePosition;
            graphView.EditTarget.nodes.Add(btNode);
            serializedObj.ApplyModifiedProperties();
            serializedObj.Update();
            //获取新Node的 serialized属性
            var nodesProp = serializedObj.FindProperty(nameof(graphView.EditTarget.nodes));
            var nodeSerializedProp = nodesProp.GetArrayElementAtIndex(nodesProp.arraySize - 1);
            //初始化，并且设置位置
            graphNode.Init(btNode, nodeSerializedProp);
            graphNode.SetPosition(new Rect(graphMousePosition, graphNode.GetPosition().size));
            
            serializedObj.ApplyModifiedProperties();
            serializedObj.Update();
            
            // //添加到视图中
            graphView.AddBTElement(graphNode);
            return graphNode;
        }

        /// <summary>
        /// 创建节点的显示Node
        /// </summary>
        /// <param name="nodeType"></param>
        /// <returns></returns>
        public BTGraphNode CreateGraphNode(Type nodeType)
        {
            graphNodeMapping.TryGetValue(nodeType, out var graphNodeType);
            //如果没有自定义则给出默认的类型
            if (null == graphNodeType)
            {
                if (nodeType.IsSubclassOf(typeof(BTTaskNode)))
                {
                    graphNodeType = typeof(BTLeafGraphNode);
                }
                else if (nodeType.IsSubclassOf(typeof(BTSelectorNode)))
                {
                    graphNodeType = typeof(SelectorGraphNode);
                }
                else if (nodeType.IsSubclassOf(typeof(BTSequenceNode)))
                {
                    graphNodeType = typeof(SequenceGraphNode);
                }
                else if (nodeType == typeof(BTNode))
                {
                    graphNodeType = typeof(BTRootGraphNode);
                }
            }

            if (null != graphNodeType)
            {
                var newNode = Activator.CreateInstance(graphNodeType);
                return newNode as BTGraphNode;
            }

            return null;
        }
    }
}