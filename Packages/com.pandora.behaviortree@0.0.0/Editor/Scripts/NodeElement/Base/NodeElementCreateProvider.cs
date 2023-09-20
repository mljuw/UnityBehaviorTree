using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 创建对象的上下文
    /// </summary>
    struct ElementCreationContext
    {
        public Type nodeType;
    }
    
    /// <summary>
    /// 创建代理者
    /// 用于创建节点元素(装饰器、服务)
    /// </summary>
    public class NodeElementCreateProvider : ScriptableObject, ISearchWindowProvider
    {
        private BehaviorTreeGraphView graphView;
        private BTGraphNode parentGraphNode;
        private Dictionary<Type, Type> graphElementMapping = new();
        private NodeCreationTree creationTree = new ();

        public void Init(BehaviorTreeGraphView graphView)
        {
            this.graphView = graphView;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                FetchAllBTAuxNode(assembly);
                FetchAllGraphElement(assembly);
            }
        }

        public void SetParentGraphNode(BTGraphNode node)
        {
            parentGraphNode = node;
        }

        public void FetchAllBTAuxNode(Assembly assembly)
        {
            var list = assembly.GetTypes().Where(
                type => type.IsSubclassOf(typeof(BTAuxiliaryNode)) && !type.IsAbstract
            ).ToArray();

            foreach (var t in list)
            {
                var attr = t.GetCustomAttribute<BTNodeAttribute>();
                var group = creationTree.CreateGroupByPathIfNeeded(attr.group);
                group.datas.Add(t);
            }
        }
        
        public void FetchAllGraphElement(Assembly assembly)
        {
            //搜索程序中所有继承了INodeElementBase 的类类型
            var list = assembly.GetTypes().Where(
                type => type.IsSubclassOf(typeof(GraphNodeElement)) && !type.IsAbstract
            ).ToArray();
            
            //让BTNode 类型 与显示元素关联起来
            foreach (var t in list)
            {
                var attr = t.GetCustomAttribute<GraphBTNodeAttribute>();
                graphElementMapping[attr.nodeClass] = t;
            }
        }
        
        /// <summary>
        /// 创建可选择创建按元素的 列表(树)
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>();

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
                ElementCreationContext ud = default;
                ud.nodeType = data;
                var attr = data.GetCustomAttribute<BTNodeAttribute>();
                var entry = new SearchTreeEntry(new GUIContent(attr.displayName))
                {
                    level = group.deep + 1, userData = ud
                };
                menuTree.Add(entry);
            }
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            ElementCreationContext ud = (ElementCreationContext)searchTreeEntry.userData;
            return null != CreateGraphElementAndData(ud.nodeType);
        }

        /// <summary>
        /// 创建节点元素的显示Element,并且创建节点数据
        /// </summary>
        /// <param name="nodeType"></param>
        /// <returns></returns>
        public GraphNodeElement CreateGraphElementAndData(Type nodeType)
        {
            var ins = CreateGraphElement(nodeType);
            if (ins != null)
            {
                var btNode = (BTAuxiliaryNode)Activator.CreateInstance(nodeType);
                SerializedProperty nodeSerializedProp = null;
                if (parentGraphNode.nodeData is BTCompositeNode compositeNode)
                {
                    Undo.RecordObject(graphView.EditTarget, "newNode");
                    
                    var serializedObj = graphView.SerializedEditObj;
                    compositeNode.auxNodes.Add(btNode);
                    serializedObj.ApplyModifiedProperties();
                    serializedObj.Update();
                    
                    var auxNodesProp = parentGraphNode.serializedProp.FindPropertyRelative(nameof(compositeNode.auxNodes));
                    nodeSerializedProp = auxNodesProp.GetArrayElementAtIndex(auxNodesProp.arraySize - 1);
                    nodeSerializedProp.managedReferenceValue = btNode;
                }
                else
                {
                    return null;
                }
                ins.Init(parentGraphNode, btNode, nodeSerializedProp);
                parentGraphNode.AddNodeElement(ins);
            }

            return ins;
        }
        
        /// <summary>
        /// 创建节点元素的显示Element
        /// </summary>
        /// <param name="nodeType"></param>
        /// <returns></returns>
        public GraphNodeElement CreateGraphElement(Type nodeType)
        {
            graphElementMapping.TryGetValue(nodeType, out var graphElementType);
            if (null == graphElementType)
            {
                if (nodeType.IsSubclassOf(typeof(BTDecoratorNode)))
                {
                    graphElementType = typeof(DecoratorNodeElement);
                }
                else if (nodeType.IsSubclassOf(typeof(BTServiceNode)))
                {
                    graphElementType = typeof(ServiceNodeElement);
                }
            }

            if (null == graphElementType) return null;
            
            var ins = graphElementType.Assembly.CreateInstance(graphElementType.FullName);
            return ins as GraphNodeElement;
        }
    }
}