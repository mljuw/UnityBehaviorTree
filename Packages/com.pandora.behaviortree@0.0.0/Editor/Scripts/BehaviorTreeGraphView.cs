using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 图表界面
    /// 节点元素在此被表示为组合节点里面的‘装饰器与服务节点’。
    /// </summary>
    public class BehaviorTreeGraphView : GraphView
    {
        private WeakReference curSelectedNodeElement = new(null);

        public IBTEditableElement CurSelectedNodeElement 
            => curSelectedNodeElement.IsAlive ? (IBTEditableElement)curSelectedNodeElement.Target : null;
        
        
        /// <summary>
        /// 选择某个可编辑的节点或者节点元素
        /// </summary>
        public event Action<IBTEditableElement> selectedEditableEvent;
        
        /// <summary>
        /// 刷新视图时的事件
        /// </summary>
        public event Action refreshViewEvent;

        private static WeakReference curSelectedNode = new(null);

        protected BTGraphNode CurSelectedNode 
            => curSelectedNode.IsAlive ? (BTGraphNode)curSelectedNode.Target : null;

        private NodeCreateProvider nodeCreateProvider;
        internal NodeElementCreateProvider nodeElementCreateProvider;
        public BehaviorTreeWindow window;


        protected SelectionDragger selectionDragger = new();
        protected RectangleSelector rectangleSelector = new();
        
        private BehaviorTreeAsset editTarget;
        public BehaviorTreeAsset EditTarget => editTarget;
        
        private SerializedObject serializedEditObj;
        public SerializedObject SerializedEditObj => serializedEditObj;
        
        /// <summary>
        /// 重写是否能删除选中项目
        /// 当调试时不可以删除
        /// </summary>
        protected override bool canDeleteSelection => !IsDebug;

        private bool nodePosDirty = false;

        #region 调试
        
        /// <summary>
        /// 调试状态改变
        /// bool: 是否调试
        /// </summary>
        public event Action<bool> debugStateChange;

        public BTDebugTarget? DebugTarget;

        private bool bIsDebug = false;
        public bool IsDebug => bIsDebug;

        private Dictionary<int, IDebugableBTElement> debugNodeIdxMapping = new ();
        private LinkedList<BTEdge> debugEdgeList = new();
        private HashSet<SearchPathData> debugLastExecutingPath = new();
        private Stack<BTDebugTarget> debugStack = new();

        /// <summary>
        /// 是否可调试父树
        /// </summary>
        /// <returns></returns>
        public bool HasDebugParentTree => IsDebug && 0 < debugStack.Count;
        
        #endregion

        public BehaviorTreeGraphView(BehaviorTreeWindow window)
        {
            var styleSheet = Resources.Load<StyleSheet>("BTGraphView");
            styleSheets.Add(styleSheet);
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(selectionDragger);
            this.AddManipulator(rectangleSelector);
            // this.AddManipulator(new FreehandSelector());
            this.AddManipulator(new ContentZoomer());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            nodeCreateProvider = ScriptableObject.CreateInstance<NodeCreateProvider>();
            nodeCreateProvider.Init(window, this);
            nodeElementCreateProvider = ScriptableObject.CreateInstance<NodeElementCreateProvider>();
            nodeElementCreateProvider.Init(this);
            
            nodeCreationRequest += NodeCreationRequest;
            this.window = window;

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromParent);
            RegisterCallback<KeyDownEvent>(OnKeyDownCallback);

            editTarget = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            LoadTree(editTarget);
            
            
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private void OnDetachFromParent(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            UnBindDebugEvent();
        }

        private void UnBindDebugEvent()
        {
            if (DebugTarget is { TreeInst: not null })
            {
                var debugTreeIns = DebugTarget.Value.TreeInst;
                debugTreeIns.searchNodeEvent -= OnSearchNodeEvent;
                debugTreeIns.bubbleNodeEvent -= OnBubbleNodeEvent;
                debugTreeIns.decoratorFailEvent -= OnDecoratorFailEvent;
                debugTreeIns.auxCeaseRelevantEvent -= OnAuxCeaseRelevantEvent;
                debugTreeIns.auxBecomeRelevantEvent -= OnAuxBecomeRelevantEvent;
            }
        }


        private void BindDebugEvent()
        {
            if (DebugTarget is { TreeInst: not null })
            {
                var debugTreeIns = DebugTarget.Value.TreeInst;
                debugTreeIns.searchNodeEvent += OnSearchNodeEvent;
                debugTreeIns.bubbleNodeEvent += OnBubbleNodeEvent;
                debugTreeIns.decoratorFailEvent += OnDecoratorFailEvent;
                debugTreeIns.auxCeaseRelevantEvent += OnAuxCeaseRelevantEvent;
                debugTreeIns.auxBecomeRelevantEvent += OnAuxBecomeRelevantEvent;
            }
        }


        private void UndoRedoPerformed()
        {
            if (IsDebug) return;
            RefreshView();
        }

        /// <summary>
        /// 选择了可编辑的编辑元素(节点或者节点元素，比如装饰器、服务)
        /// </summary>
        /// <param name="element"></param>
        public void OnSelectedBTEditableElement(IBTEditableElement element)
        {
            // if (IsDebug()) return;

            //取消显示当前的节点
            if (CurSelectedNode != null)
            {
                CurSelectedNode.selected = false;
                if (CurSelectedNode is IBTEditableElement editableNode)
                {
                    editableNode.UnSelected();
                    curSelectedNode.Target = null;
                }
            }

            //取消显示当前的节点元素
            if (CurSelectedNodeElement != null)
            {
                CurSelectedNodeElement.UnSelected();
                curSelectedNodeElement.Target = null;
            }

            if (element is BTGraphNode btNode)
            {
                curSelectedNode.Target = btNode;
                btNode.selected = true;
            }
            else if (element is GraphNodeElement)
            {
                curSelectedNodeElement.Target = element;
            }

            if (null != element)
            {
                element.Selected();
            }

            selectedEditableEvent?.Invoke(element);
        }

        /// <summary>
        /// 根据 坐标x 比较
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int ComparisonByPosition(Edge x, Edge y)
        {
            var a = x.input as BTNodePort;
            var b = y.input as BTNodePort;
            return a.Owner.GetPosition().position.x.CompareTo(b.Owner.GetPosition().position.x);
        }

        /// <summary>
        /// 查找树根节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private BTGraphNode FindRootGraphNode(BTGraphNode node)
        {
            if (node.inPort.connected && node.inPort.connections.Any())
            {
                var edge = node.inPort.connections.First();
                if (edge.output != null && edge.output.connected)
                {
                    if (edge.output is BTNodePort { Owner: not null } parentPort)
                    {
                        return FindRootGraphNode(parentPort.Owner);
                    }
                }
            }

            return node;
        }
       

        #region 对元素的操作

        /// <summary>
        /// 添加行为树元素, 目前只用于添加节点
        /// </summary>
        /// <param name="graphElement"></param>
        public void AddBTElement(GraphElement graphElement)
        {
            if (IsDebug) return;

            AddElement(graphElement);
            UpdateNodeIndex();
        }

        /// <summary>
        /// 移除元素
        /// </summary>
        /// <param name="graphElement"></param>
        public void RemoveBTElement(GraphElement graphElement)
        {
            if (IsDebug) return;

            RemoveElement(graphElement);
            UpdateNodeIndex();
        }

        internal void NodeConnectionChange(BTGraphNode node)
        {
            SerializeAndRefreshView();
        }
        
        /// <summary>
        /// 根据下标选中节点
        /// </summary>
        /// <param name="nodeIndex"></param>
        public void SelectNodeByIndex(int nodeIndex)
        {
            var allElements = graphElements.ToList();
            foreach (var ele in allElements)
            {
                var graphNode = ele as BTGraphNode;
                if (null == graphNode) continue;
                
                if (graphNode.GetNodeIndex() == nodeIndex && graphNode is IBTEditableElement editable)
                {
                    OnSelectedBTEditableElement(editable);
                    return;
                }
                
                if (graphNode.nodeData.NodeIsComposite() && graphNode is BtCompositeGraphNode compositeNode)
                {
                    foreach (var nodeElement in compositeNode.GetNodeElements())
                    {
                        if (nodeElement.GetNodeIndex() == nodeIndex)
                        {
                            ele.selected = true;
                            OnSelectedBTEditableElement(nodeElement);
                            return;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 通知节点更新
        /// </summary>
        /// <param name="node"></param>
        public void OnUpdateGraphBTNode(BTGraphNode node)
        {
            UpdateNodeIndex();
        }

        /// <summary>
        /// 通知删除节点中的节点元素
        /// </summary>
        /// <param name="ownerNode"></param>
        /// <param name="nodeElement"></param>
        public void OnRemoveNodeElement(BTGraphNode ownerNode, GraphNodeElement nodeElement)
        {
            SerializeAndRefreshView();
        }

        /// <summary>
        /// 节点位置改变
        /// </summary>
        public void OnUpdateNodePos()
        {
            nodePosDirty = true;
        }

        /// <summary>
        /// 通知线条更新
        /// </summary>
        /// <param name="edge"></param>
        public void OnEdgePortChange(BTEdge edge)
        {
            UpdateNodeIndex();
        }

        /// <summary>
        /// 找到兼容的链接点
        /// </summary>
        /// <param name="startPort"></param>
        /// <param name="nodeAdapter"></param>
        /// <returns></returns>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> ret = new List<Port>(ports.Count());
        
            foreach (var port in ports)
            {
                if (port.direction != startPort.direction && port.node != startPort.node)
                {
                    var adapterCheck = nodeAdapter.GetAdapter(port.source, startPort.source);
                    if (adapterCheck != null)
                    {
                        ret.Add(port);
                    }
                }
            }
            
            return ret;
        }


        #endregion

        #region 调试
 
        /// <summary>
        /// 调试对象是否有效(没被销毁)
        /// </summary>
        /// <returns></returns>
        public bool DebugTargetIsAlive()
        {
            return DebugTarget?.IsAlive ?? false;
        }

        /// <summary>
        /// 停止调试
        /// </summary>
        public void StopDebug()
        {
            StopDebug(true);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StopDebug(bool clearDebugStack)
        {
            //清理调试所用的变量
            if (clearDebugStack)
            {
                debugStack.Clear();
            }
            debugNodeIdxMapping.Clear();
            debugLastExecutingPath.Clear();
            debugEdgeList.Clear();
            
            //删除所有元素
            DeleteElements(graphElements.ToList());
            //关闭元素(包括graphView自己)的调试状态, 先删除所有元素再调用。
            SetElementDebugState(false);
            
            UnBindDebugEvent();
            bIsDebug = false;
            debugStateChange?.Invoke(false);
        }

        /// <summary>
        /// 开始调试
        /// </summary>
        /// <returns></returns>
        public void Debug()
        {
            //如果已经在调试状态则不需要下面操作
            if (IsDebug) return;

            //获取并且检查调试对象
            if (!DebugTargetIsAlive()) return;
            
            bIsDebug = true;
            
            //加载行为树的节点
            editTarget = DebugTarget?.TreeInst.Asset;
            serializedEditObj = new SerializedObject(editTarget);
            LoadTreeInternal(editTarget);
 
            //监听查找节点与冒泡节点的事件
            BindDebugEvent();

            //设置元素的调试状态
            SetElementDebugState(true);
            
            OnDebugNodeUpdate(DebugTarget?.TreeInst.RootNode, false);

            debugStateChange?.Invoke(true);
        }

        /// <summary>
        /// 设置元素的调试状态
        /// </summary>
        /// <param name="bDebug"></param>
        private void SetElementDebugState(bool bDebug)
        {
            if (bDebug)
            {
                //在调试下删除元素选择操作器
                this.RemoveManipulator(selectionDragger);
                this.RemoveManipulator(rectangleSelector);
            }
            else
            {
                //在关闭调试时将元素选择器开启
                this.AddManipulator(selectionDragger);
                this.AddManipulator(rectangleSelector);
            }

            //遍历图标元素设置他们的调试状态
            foreach (var ele in graphElements)
            {
                var edge = ele as BTEdge;
                if (edge != null)
                {
                    edge.SetEnableDebug(bDebug);
                    continue;
                }

                var node = ele as BTGraphNode;
                if (null != node)
                {
                    node.SetEnableDebug(bDebug);
                }
            }
        }

        /// <summary>
        /// 调试子树
        /// </summary>
        /// <param name="hostNodeIndex"></param>
        /// <param name="treeAsset"></param>
        internal void DebugSubTree(int hostNodeIndex, BehaviorTreeAsset treeAsset)
        {
            if (!IsDebug || null == DebugTarget) return;
            var debugTreeInst = DebugTarget?.TreeInst;
            if (debugTreeInst.ExecutingNodePath.Count <= 0) return;
            var curPathData =  debugTreeInst.ExecutingNodePath[^1];
            
            BehaviorTreeInstance subTreeInst = null;
            if (curPathData.node is BTTSubTreeNodeInst subTreeNode)
            {
                if (curPathData.node.NodeIndex != hostNodeIndex) return;
                if (null == subTreeNode.SubTreeInst) return;
                subTreeInst = subTreeNode.SubTreeInst;
               
            }
            else if (curPathData.node.ParentNode is BTParallelNodeInst parallelNode)
            {
                if (parallelNode.NodeIndex != hostNodeIndex) return;
                if (null == parallelNode.SubTreeInst) return;
                subTreeInst = parallelNode.SubTreeInst;
            }

            if (null != subTreeInst)
            {
                debugStack.Push(DebugTarget.Value);
                StopDebug(false);
                DebugTarget = new BTDebugTarget(subTreeInst);
                Debug();
            }
        }

        internal void DebugParentTree()
        {
            if (!IsDebug) return;
            StopDebug(false);
            DebugTarget = debugStack.Pop();
            Debug();
        }
        
        /// <summary>
        /// 节点冒泡事件
        /// </summary>
        /// <param name="node"></param>
        private void OnBubbleNodeEvent(BTNodeInstance node)
        {
            OnDebugNodeUpdate(node, true);
        }

        /// <summary>
        /// 节点搜索(涓滴)事件
        /// </summary>
        /// <param name="node"></param>
        private void OnSearchNodeEvent(BTNodeInstance node)
        {
            OnDebugNodeUpdate(node, false);
        }
        
        
        /// <summary>
        /// 助手节点在被调用 OnBecomeRelevant时 
        /// </summary>
        private void OnAuxBecomeRelevantEvent(BTNodeInstance nodeInst)
        {
            if (debugNodeIdxMapping.TryGetValue(nodeInst.NodeIndex, out var ele))
            {
                if(ele is GraphNodeElement graphNodeElement)
                {
                    graphNodeElement.OnDebugBecomeRelevant();
                }
            }
        }
        
        /// <summary>
        /// 助手节点在被调用 OnCeaseRelevant时 
        /// </summary>
        /// <param name="obj"></param>
        private void OnAuxCeaseRelevantEvent(BTNodeInstance nodeInst)
        {
            if (debugNodeIdxMapping.TryGetValue(nodeInst.NodeIndex, out var ele))
            {
                if(ele is GraphNodeElement graphNodeElement)
                {
                    graphNodeElement.OnDebugCeaseRelevant();
                }
            }
        }

        /// <summary>
        /// 装饰器节点检测失败时的事件
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnDecoratorFailEvent(BTNodeInstance node)
        {
            IDebugableBTElement debugElement;
            if (debugNodeIdxMapping.TryGetValue(node.NodeIndex, out debugElement))
            {
                var nodeElement = debugElement as DecoratorNodeElement;
                nodeElement.SetActivation(true);
            }
        }

        /// <summary>
        /// 当节点被搜索时的事件处理
        /// </summary>
        /// <param name="node"></param>
        /// <param name="bIsBubble"></param>
        private void OnDebugNodeUpdate(BTNodeInstance node, bool bIsBubble)
        {
            //如果是助手节点
            if (node.BaseDef.NodeIsAuxiliary())
            {
                //如果搜索助手节点是否要显示？
            }
            else
            {
                var treeInst = DebugTarget?.TreeInst;
                
                //找出取消激活的节点
                debugLastExecutingPath.ExceptWith(treeInst.ExecutingNodePath);
                //取消激活节点
                foreach (var pathData in debugLastExecutingPath)
                {
                    IDebugableBTElement debugElement;
                    if (debugNodeIdxMapping.TryGetValue(pathData.node.NodeIndex, out debugElement))
                    {
                        var curNode = debugElement as BTGraphNode;
                        //关掉高亮线条
                        if (curNode.inPort != null && curNode.inPort.connected)
                        {
                            var edge = curNode.inPort.connections.First() as BTEdge;
                            edge.HighLight(false);
                        }

                        debugElement.SetActivation(false);
                    }
                }

                //冒泡当前激活路径，设置每个路径上的节点的激活状态(包括线条)
                Stack<SearchPathData> path = new Stack<SearchPathData>(treeInst.ExecutingNodePath);
                while (path.Count > 0)
                {
                    //获取节点的下标
                    var pathData = path.Pop();
                    IDebugableBTElement debugElement;
                    if (debugNodeIdxMapping.TryGetValue(pathData.node.NodeIndex, out debugElement))
                    {
                        //设置
                        debugElement.SetActivation(true);
                        var curNode = debugElement as BTGraphNode;
                        //点亮线条
                        if (curNode.inPort != null && curNode.inPort.connected)
                        {
                            var edge = curNode.inPort.connections.First() as BTEdge;
                            edge.HighLight(true);
                        }
                    }
                }

                debugLastExecutingPath = treeInst.ExecutingNodePath.ToHashSet();
            }
        }

        public void Tick(float deltaTime, float totalTime)
        {
            //检查是否有节点移动过, 同一帧可能对多个节点移动过，所以放在一帧统一处理
            if (nodePosDirty)
            {
                var dirtyRoot = UpdateNodeIndex(true);
                if (dirtyRoot != null)
                {
                    SerializeAndRefreshView();
                }                
                nodePosDirty = false;
            }
            
            if (IsDebug)
            {
                foreach (var item in debugNodeIdxMapping)
                {
                    item.Value.DebugTick(deltaTime);
                }

                foreach (var edge in debugEdgeList)
                {
                    edge.Tick(deltaTime, totalTime);
                }
            }
        }
 

        #endregion

        #region 保存

        public void SaveToAsset(BehaviorTreeAsset asset)
        {
            asset.hideFlags &= ~HideFlags.HideInHierarchy;
            asset.nodes.Clear();
            asset.stickies.Clear();
            var hashSet = graphElements.ToHashSet();
            //加载所有节点树
            while (hashSet.Count > 0)
            {
                var item = hashSet.First();
                var graphNode = item as BTGraphNode;
                if (null == graphNode)
                {
                    //保存注释
                    if (item is BTStickyNode stickyNode)
                    {
                        StickyAsset stickyData = new StickyAsset
                        {
                            position = stickyNode.GetPosition(),
                            content = stickyNode.contents,
                            title = stickyNode.title
                        };
                        asset.stickies.Add(stickyData);
                    }

                    hashSet.Remove(item);
                }
                else
                {
                    //找到树根节点
                    graphNode = FindRootGraphNode(graphNode);
                    //保存关联的全部子节点
                    var rootNode = SaveSubNode(ref asset, graphNode, null, ref hashSet);
                    asset.nodes.Add(rootNode);
                }
            }
        }

        private BTNode SaveSubNode(ref BehaviorTreeAsset asset, BTGraphNode curNode, BTGraphNode parentNode,
            ref HashSet<GraphElement> allNode)
        {
            var nodeData = curNode.nodeData;
            nodeData.children.Clear();

            //从全部节点集合中移除
            allNode.Remove(curNode);
            nodeData.visitPos = curNode.GetPosition().position;
            
            //如果是一个组合节点，则加载它的助手节点
            if (curNode.nodeData.NodeIsComposite() &&
                nodeData is BTCompositeNode compositeNode)
            {
                compositeNode.auxNodes.Clear();
                var graphCompositeNode = curNode as BtCompositeGraphNode;
                foreach (var ele in graphCompositeNode.GetNodeElements())
                {
                    var auxNodeData = ele.nodeData;
                    compositeNode.auxNodes.Add(auxNodeData as BTAuxiliaryNode);
                }
            }

           
            //处理链接的节点
            if (curNode.outPort.connected)
            {
                //按照x坐标排序, 最左边排在最前面
                var connections = curNode.outPort.connections.ToList();
                connections.Sort(ComparisonByPosition);
                //获取输出端线
                foreach (var edge in connections)
                {
                    //判断是否有连接到下一个节点的输入端
                    if (edge.input.connected && edge.input is BTNodePort childPort)
                    {
                        //作为子节点保存
                        nodeData.children.Add(SaveSubNode(ref asset, childPort.Owner, curNode,
                            ref allNode));
                    }
                }
                
            }
            return nodeData;
        }

        #endregion
        
        #region 加载

        /// <summary>
        /// 刷新界面
        /// </summary>
        private void SerializeAndRefreshView()
        {
            if (IsDebug) return;
            SaveToAsset(EditTarget);
            RefreshView();
        }

        private void RefreshView()
        {
            LoadTree(EditTarget);
            refreshViewEvent?.Invoke();
        }
        
        /// <summary>
        /// 用于外部调用的加载行为树
        /// </summary>
        /// <param name="tree"></param>
        public void LoadTree(BehaviorTreeAsset tree)
        {
            //如果在调试期间不能给外部加载树
            if (IsDebug) return;
            editTarget = tree;
            serializedEditObj = new SerializedObject(editTarget);
            LoadTreeInternal(tree);
        }
        
        private void LoadTreeInternal(BehaviorTreeAsset tree)
        {
            //删除所有元素
            DeleteElements(graphElements.ToList());

            //加载备注信息
            for (int i = 0 ; i < tree.stickies.Count; ++i)
            {
                var sticky = tree.stickies[i];
                var stickyNode = new BTStickyNode(sticky);
                AddBTElement(stickyNode);
            }

            var nodesProp = SerializedEditObj.FindProperty(nameof(tree.nodes));
            
            int index = 0;
            if (!IsDebug)
            {
                for (int i = 0; i < tree.nodes.Count; ++i)
                {
                    var nodeProp = nodesProp.GetArrayElementAtIndex(i);
                    //加载所有根节点树
                    LoadNode(tree.nodes[i], nodeProp, null, ref index);
                }
            }
            else
            {
                var rootNode = tree.GetRootNode(out var rootNodeIdx);
                var nodeProp = nodesProp.GetArrayElementAtIndex(rootNodeIdx);
                //只加载其中一个根节点
                LoadNode(rootNode, nodeProp, null, ref index);
                
                //找到已开启的助手节点标记为活跃状态
                if (DebugTarget != null)
                {
                    var relevantAuxNode = DebugTarget.Value.TreeInst.SearchRelevantAuxNode();
                    foreach (var nodeIdx in relevantAuxNode)
                    {
                        if (debugNodeIdxMapping.TryGetValue(nodeIdx, out var editable))
                        {
                            if (editable is GraphNodeElement ele)
                            {
                                ele.OnDebugBecomeRelevant();
                            }
                        }
                    }
                }
            }

            //更新节点下标
            UpdateNodeIndex();
        }

        /// <summary>
        /// 加载节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="nodeProp"></param>
        /// <param name="parentNode"></param>
        /// <param name="index"></param>
        protected void LoadNode(BTNode node, SerializedProperty nodeProp, BTGraphNode parentNode, ref int index)
        {
            //创建编辑器节点
            BTGraphNode btGraphNode = nodeCreateProvider.CreateGraphNode(node.GetType());
            btGraphNode.Init(node, nodeProp);

            //如果是在调试将建立 Node 与 显示节点 关联
            if (IsDebug)
            {
                debugNodeIdxMapping.Add(index, btGraphNode);
            }

            //添加到图表中
            AddElement(btGraphNode);
            btGraphNode.SetPosition(new Rect(node.visitPos, Vector2.zero));

            //如果有父级节点
            if (parentNode != null)
            {
                //创建链接线
                var edge = btGraphNode.inPort.ConnectTo<BTEdge>(parentNode.outPort);
                AddElement(edge);

                if (IsDebug)
                {
                    debugEdgeList.AddLast(edge);
                }
            }
            
            var auxNodesProp = nodeProp.FindPropertyRelative(nameof(node.auxNodes));
            for (int i = 0; i < node.auxNodes.Count; ++i)
            {
                ++index;
                var auxNode = node.auxNodes[i];
                var auxNodeProp = auxNodesProp.GetArrayElementAtIndex(i);
                //加载助手节点
                LoadAuxNode(auxNode, auxNodeProp, btGraphNode, ref index);
            }
            
            
            var childrenProp = nodeProp.FindPropertyRelative(nameof(node.children));
            for (int i = 0; i <node.children.Count; ++i)
            {
                ++index;
                var childNodeProp = childrenProp.GetArrayElementAtIndex(i);
                //加载子节点
                LoadNode(node.children[i], childNodeProp, btGraphNode, ref index);
            }
        }

        /// <summary>
        /// 加载助手节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="serializedProp"></param>
        /// <param name="parentNode"></param>
        /// <param name="index"></param>
        protected void LoadAuxNode(BTNode node, SerializedProperty serializedProp ,BTGraphNode parentNode, ref int index)
        {
            if (null == parentNode) return;

            var nodeElement = nodeElementCreateProvider.CreateGraphElement(node.GetType());
             
            if (null != nodeElement)
            {
                nodeElement.Init(parentNode, node, serializedProp);
                parentNode.AddNodeElement(nodeElement);

                //如果是在调试:将建立 Node下标 与 显示节点 的关联
                if (IsDebug)
                {
                    debugNodeIdxMapping.Add(index, nodeElement);
                }
            }
        }

        #endregion

        #region 更新下标

        /// <summary>
        /// 更新节点下标
        /// </summary>
        private BTGraphNode UpdateNodeIndex(bool findDirtyNode = false)
        {
            BTGraphNode dirtyNode = null;
            var hashSet = graphElements.ToHashSet();
            //加载所有节点树
            while (hashSet.Any())
            {
                var item = hashSet.First();
                var graphNode = item as BTGraphNode;
                if (null == graphNode)
                {
                    hashSet.Remove(item);
                }
                else
                {
                    //找到树根节点
                    graphNode = FindRootGraphNode(graphNode);
                    int index = 0;
                    dirtyNode = UpdateSubNodeIndex(graphNode, null, ref hashSet, ref index, findDirtyNode);
                    if (findDirtyNode && dirtyNode != null)
                    {
                        return dirtyNode;
                    }
                }
            }

            return dirtyNode;
        }

        /// <summary>
        /// 递归更新子节点下标
        /// </summary>
        /// <param name="curNode"></param>
        /// <param name="parentNode"></param>
        /// <param name="allNode"></param>
        /// <param name="index"></param>
        /// <param name="findDirtyNode"></param>
        private BTGraphNode UpdateSubNodeIndex(BTGraphNode curNode, BTGraphNode parentNode, ref HashSet<GraphElement> allNode,
            ref int index, bool findDirtyNode = false)
        {
            //从全部节点集合中移除
            allNode.Remove(curNode);

            if (findDirtyNode && curNode.GetNodeIndex() != index)
            {
                return parentNode;
            }
            
            curNode.SetNodeIndex(index);

            //如果是一个组合节点，则加载它的助手节点
            foreach (var ele in curNode.GetNodeElements())
            {
                index++;
                if (findDirtyNode && ele.GetNodeIndex() != index)
                {
                    return parentNode;
                }
                ele.SetNodeIndex(index);
            }

            //如果输出端有连接下一个节点则作为子节点处理
            if (curNode.outPort.connected)
            {
                var outPort = curNode.outPort;
                var connections = outPort.connections.Where((edge, i) => edge.input is { connected: true }).ToList();
                //安装x坐标排序, 最左边排在最前面
                connections.Sort(ComparisonByPosition);
                //获取输出端线
                for (int i = 0; i < connections.Count; ++i)
                {
                    var edge = connections[i];
                    BTNodePort childPort = edge.input as BTNodePort;

                    index++;
                    BTGraphNode dirtyNode = UpdateSubNodeIndex(childPort.Owner, curNode, ref allNode, ref index, findDirtyNode);
                    if (findDirtyNode && dirtyNode != null)
                    {
                        return dirtyNode;
                    }
                }
            }

            return null;
        }

        #endregion

        #region 按键操作

        private void OnKeyDownCallback(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.C)
            {
                NewAutoSizeStickyNode(evt.originalMousePosition);
            }
        }

        #endregion

        #region 右键菜单

        /// <summary>
        /// 右键显示菜单
        /// </summary>
        /// <param name="evt"></param>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // base.BuildContextualMenu(evt);
            if (evt.target is UnityEditor.Experimental.GraphView.GraphView)
            {
                evt.menu.AppendAction("创建节点", OnContextMenuNodeCreate, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("创建注释", OnClickNewSticky,
                IsDebug ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        /// <param name="obj"></param>
        public void OnClickDeleteNode(DropdownMenuAction action)
        {
            DeleteSelectionNodes();
        }

        private void OnClickNewSticky(DropdownMenuAction action)
        {
            NewAutoSizeStickyNode(action.eventInfo.localMousePosition);
        }

        
        /// <summary>
        /// 创建注释
        /// 在多选节点的情况下，自动适配大小。
        /// </summary>
        public void NewAutoSizeStickyNode(Vector2 position)
        {
            var allNodes = selection.OfType<BTGraphNode>();
            if (allNodes.Count() < 1)
            {
                NewStickyNode(new Rect(position, new Vector2(100, 100)));
            }
            else
            {
                
                Vector2 padding = new Vector2(0, -40);
                Vector2 max = Vector2.zero, min = Vector2.zero;
                bool firstEach = true;
                foreach (var node in allNodes)
                {
                    var pos = node.GetPosition();
                    var worldPos = pos.position;
                    var size = pos.size;
                    if (firstEach)
                    {
                        firstEach = false;
                        max = worldPos + node.GetPosition().size;
                        min = worldPos;
                    }
                    if (worldPos.x + size.x > max.x) max.x = worldPos.x + size.x;
                    if (worldPos.y + size.y > max.y) max.y = worldPos.y + size.y;
                    if (worldPos.x < min.x) min.x = worldPos.x;
                    if (worldPos.y < min.y) min.y = worldPos.y;
                }

                NewStickyNode(new Rect(contentViewContainer.LocalToWorld(min + padding), (max - min) - padding));
            }
        }
        
        /// <summary>
        /// 创建注释
        /// </summary>
        /// <param name="position"></param>
        public void NewStickyNode(Rect position)
        {
            Undo.RecordObject(EditTarget, "New stickyNode.");
            
            //转换到GraphView下的坐标空间
            var locPos = contentViewContainer.WorldToLocal(position.position);

            StickyAsset stickyData = new StickyAsset
            {
                position = new Rect(locPos, position.size),
                content = "...",
                title = "注释"
            };
            EditTarget.stickies.Add(stickyData);
            
            SerializedEditObj.ApplyModifiedProperties();
            SerializedEditObj.Update();

            var stickyNode = new BTStickyNode(stickyData);
            AddElement(stickyNode);
            
        }

        private void DeleteSelectionNodes()
        {
            HashSet<GraphElement> graphElementSet = new HashSet<GraphElement>();
            var allBTNodes = selection.OfType<GraphElement>();
            //收集选中元素
            CollectElements(allBTNodes, graphElementSet,
                e => (e.capabilities & Capabilities.Deletable) == Capabilities.Deletable);
            
            Undo.RecordObject(EditTarget, "Delete nodes.");
            //遍历删除选中元素
            foreach (var removal in graphElementSet)
            {
                if (removal is BTGraphNode graphBtNode)
                {
                    RemoveBTElement(graphBtNode);
                }
                else if (removal is BTStickyNode stickyNode)
                {
                    RemoveBTElement(stickyNode);
                }
            }
            
            SerializeAndRefreshView();
        }

        public override EventPropagation DeleteSelection()
        {
            DeleteSelectionNodes();
            return EventPropagation.Stop;
        }

        private void OnContextMenuNodeCreate(DropdownMenuAction a) =>
            RequestNodeCreation(null, -1, a.eventInfo.mousePosition);

        /// <summary>
        /// 请求创建菜单
        /// </summary>
        /// <param name="target"></param>
        /// <param name="index"></param>
        /// <param name="position"></param>
        private void RequestNodeCreation(VisualElement target, int index, Vector2 position)
        {
            if (nodeCreationRequest == null)
                return;
            var mousePosition = position + window.position.position;
            nodeCreationRequest(new NodeCreationContext()
            {
                screenMousePosition = mousePosition,
                target = target,
                index = index
            });
        }

        /// <summary>
        /// 右键创建菜单
        /// </summary>
        /// <param name="context"></param>
        private void NodeCreationRequest(NodeCreationContext context)
        {
            var ctx = new SearchWindowContext(context.screenMousePosition);
            SearchWindow.Open<NodeCreateProvider>(ctx, nodeCreateProvider);
        }

        #endregion
    }
    

    /// <summary>
    /// 调试目标
    /// </summary>
    public struct BTDebugTarget
    {
        public BTDebugTarget(BehaviorTreeInstance treeInst)
        {
            treeInstRef = new WeakReference<BehaviorTreeInstance>(treeInst);
        }
        

        private readonly WeakReference<BehaviorTreeInstance> treeInstRef;

        public BehaviorTreeInstance TreeInst
        {
            get
            {
                treeInstRef.TryGetTarget(out var inst);
                return inst;
            }
        }

        public BTBlackboardInst Blackboard
        {
            get
            {
                if (null == TreeInst) return null;
                return TreeInst.Blackboard;
            }
        } 
        
        public BTBlackboard BlackboardAsset
        {
            get
            {
                if (null == TreeInst) return null;
                return TreeInst.BlackboardAsset;
            }
        } 

        public bool IsAlive => null != TreeInst;

        public bool IsValid => TreeInst is { IsRunning : true };

        public override string ToString()
        {
            if (!IsValid) return "对象已删除或没运行树";
            if (TreeInst == null) return "";
            if(TreeInst.Owner == null) return TreeInst.ToString();
            return TreeInst.Owner.ToString();
        }

        public static bool operator ==(BTDebugTarget lhs, BTDebugTarget rhs)
        {
            return lhs.TreeInst == rhs.TreeInst;
        }

        public static bool operator !=(BTDebugTarget lhs, BTDebugTarget rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj is BTDebugTarget other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return treeInstRef.GetHashCode();
        }
    }
    
    #region 节点创建菜单的树结构
    
    /// <summary>
    /// 节点创建菜单的树结构
    /// </summary>
    public class NodeCreationTree
    {
        public List<NodeCreationGroup> groups = new();

        public NodeCreationGroup CreateGroupByPathIfNeeded(string path)
        {
            var pathArray = path.Split("|");
            
            var root = CreateRootNodeByPathIfNeeded(pathArray);
            
            if (root.groupName != pathArray[0]) return null;
            var curGroup = root;
            for (int i = 1; i < pathArray.Length; ++i)
            {
                var childGroup = curGroup.GetChildByName(pathArray[i]);
                if (null == childGroup)
                {
                    curGroup = curGroup.CreateChild(pathArray[i]);
                }
                else
                {
                    curGroup = childGroup;
                }
            }

            return curGroup;
        }

        private NodeCreationGroup CreateRootNodeByPathIfNeeded(in string[] pathArray)
        {
            var rootName = pathArray[0];
            foreach (var item in groups)
            {
                if (item.groupName == rootName)
                {
                    return item;
                }
            }
            var root = new NodeCreationGroup(rootName, 1);
            groups.Add(root);
            return root;
        }
    }
    
    
    /// <summary>
    /// 创建节点菜单的分组
    /// </summary>
    public class NodeCreationGroup
    {
        public NodeCreationGroup(string groupName, int deep)
        {
            this.groupName = groupName;
            this.deep = deep;
        }
        public string groupName;
        public int deep = 0;
        public List<Type> datas = new ();
        public List<NodeCreationGroup> children = new();

        public NodeCreationGroup CreateChild(string name)
        {
            var child = new NodeCreationGroup(name, deep + 1);
            children.Add(child);
            return child;
        }
        
        public NodeCreationGroup GetChildByName(string name)
        {
            foreach (var child in children)
            {
                if (child.groupName == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
    
    #endregion
    
    
}