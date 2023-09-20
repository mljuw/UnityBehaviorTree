using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Pandora.BehaviorTree
{
    public class BehaviorTreeInstance
    {
        #region 事件
        
        /// <summary>
        /// 行为树执行事件
        /// </summary>
        public static event Action<BehaviorTreeInstance> startTreeEvent;
        
        /// <summary>
        /// 停止执行行为树事件
        /// </summary>
        public static event Action<BehaviorTreeInstance> stopTreeEvent;


        /// <summary>
        /// 搜索节点事件
        /// </summary>
        public event Action<BTNodeInstance> searchNodeEvent;

        /// <summary>
        /// 冒泡节点事件
        /// </summary>
        public event Action<BTNodeInstance> bubbleNodeEvent;

        /// <summary>
        /// 装饰器判断失败事件
        /// </summary>
        public event Action<BTNodeInstance> decoratorFailEvent;

        /// <summary>
        /// 搜索树结束事件
        /// (重新回到根节点时）
        /// </summary>
        public event Action<BehaviorTreeInstance> treeSearchFinish;

        /// <summary>
        /// 激活节点事件
        /// </summary>
        public event Action<BTNodeInstance> activatedTaskNodeEvent;

        /// <summary>
        /// 助手节点在被调用 OnBecomeRelevant时 
        /// </summary>
        public event Action<BTNodeInstance> auxBecomeRelevantEvent;

        /// <summary>
        /// 助手节点在被调用 OnCeaseRelevant时 
        /// </summary>
        public event Action<BTNodeInstance> auxCeaseRelevantEvent;

        #endregion
        
        #region 成员变量
        
        private Object owner;

        public Object Owner => owner;
        
        private BehaviorTreeAsset asset;
        public BTBlackboard BlackboardAsset => blackboardInst?.BlackboardAsset;

        private BTBlackboardInst blackboardInst;
        
        /// <summary>
        /// 获取黑板
        /// </summary>
        public BTBlackboardInst Blackboard => blackboardInst;


        private bool isRunning = false;

        /// <summary>
        /// 是否在运行中
        /// </summary>
        public bool IsRunning => isRunning;


        /// <summary>
        /// 树资源文件
        /// </summary>   
        public BehaviorTreeAsset Asset => asset;


        /// <summary>
        /// 根节点
        /// </summary>
        private BTNodeInstance rootNode;

        public BTNodeInstance RootNode => rootNode;

        /// <summary>
        /// 当前执行节点的执行路径
        /// </summary>
        private List<SearchPathData> executingPath = new();

        /// <summary>
        /// 当前执行节点的搜索路径
        /// </summary>
        private List<SearchPathData> searchPath = new();
        
        public List<SearchPathData> ExecutingNodePath => executingPath;
        
        
        /// <summary>
        /// 当前被激活的任务节点
        /// </summary>
        private ITaskNodeInstance activatedTaskNode;

        /// <summary>
        /// 是否执行一个空节点
        /// </summary>
        private bool activatedEmptyNode = false;
        
        #endregion

        public BehaviorTreeInstance(Object ownerObject)
        {
            owner = ownerObject;
        }
        
        /// <summary>
        /// 加载黑板
        /// </summary>
        /// <param name="loadAsset"></param>
        public void LoadBlackboard(BTBlackboard loadAsset)
        {
            blackboardInst = new BTBlackboardInst(loadAsset);
        }

        /// <summary>
        /// 设置共享黑板实例
        /// </summary>
        /// <param name="bbInst"></param>
        internal void SetShareBlackboardInst(BTBlackboardInst bbInst)
        {
            blackboardInst = bbInst;
        }

        /// <summary>
        /// 加载树(初始化)
        /// </summary>
        /// <param name="tree"></param>
        public bool StartTree(BehaviorTreeAsset tree)
        {
            if (null == tree.GetRootNode(out _) || isRunning)
            {
                return false;
            }
            
            if (rootNode == null || asset != tree)
            {
                asset = tree;
                int nodeIdx = 0;
                rootNode = LoadTree(null, tree.GetRootNode(out _), ref nodeIdx);
            }
            isRunning = true;
            startTreeEvent?.Invoke(this);
            StartSearch();
            return true;
        }

        public void StopTree()
        {
            if (!isRunning) return;
            isRunning = false;
            Dispose();
            startTreeEvent?.Invoke(this);
        }
        
        /// <summary>
        /// 递归树创建节点实例并且设置节点的编号与父节点等信息
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="node"></param>
        /// <param name="nodeIdx"></param>
        /// <returns></returns>
        private BTNodeInstance LoadTree(BTNodeInstance parent, BTNode node, ref int nodeIdx)
        {
            var nodeIns = BTNodeInstance.New(this, node);
            nodeIns.ParentNode = parent;
            nodeIns.NodeIndex = nodeIdx;

            //处理助手节点
            for (int i = 0; i < node.auxNodes.Count; ++i)
            {
                ++nodeIdx;
                var auxNode = (IAuxiliaryNodeInst)LoadTree(nodeIns, node.auxNodes[i], ref nodeIdx);
                nodeIns.auxNodes.Add(auxNode);
            }

            //处理子节点
            for (int i = 0; i < node.children.Count; ++i)
            {
                ++nodeIdx;
                nodeIns.children.Add(LoadTree(nodeIns, node.children[i], ref nodeIdx));
            }

            return nodeIns;
        }

        internal void NotifyAuxBecomeRelevant(BTNodeInstance node)
        {
            auxBecomeRelevantEvent?.Invoke(node);
        }

        internal void NotifyAuxCeaseRelevant(BTNodeInstance node)
        {
            auxCeaseRelevantEvent?.Invoke(node);
        }

        /// <summary>
        /// 搜索被激活的助手节点
        /// 一般给编辑器调试所用,遍历整棵树,性能较低。
        /// </summary>
        /// <returns></returns>
        public List<int> SearchRelevantAuxNode()
        {
            List<int> ret = new(64);
            List<BTNodeInstance> allNode = new List<BTNodeInstance>(rootNode.children);
            for (int i = 0; i < allNode.Count; ++i)
            {
                allNode.AddRange(allNode[i].children);
                foreach (var aux in allNode[i].auxNodes)
                {
                    if (aux.IsRelevant)
                    {
                        ret.Add(aux.NodeIndex);
                    }
                }
            }
            return ret;
        }
        
        #region 树执行逻辑

        public void Tick(float deltaTime)
        {
            if (!isRunning) return;

            if (executingPath.Count > 0)
            {
                if (null != activatedTaskNode || activatedEmptyNode)
                {
                    //要处理执行路径上的Tick(包括Services)
                    foreach (var pathData in executingPath)
                    {
                        pathData.node.TickNode(deltaTime);
                        foreach (var auxNode in pathData.node.auxNodes)
                        {
                            auxNode.TickNode(deltaTime);
                        }
                    }
                    
                    //处理打断
                    var abortNode = CheckAbort(out var onExecutingPath);
                    if (abortNode != null && !onExecutingPath)
                    {
                        //探测是否有可激活的路径
                        if (!ProbeCheckWithOutActivatedTask(abortNode))
                        {
                            abortNode = null;
                        }
                    }
                    if (abortNode != null)
                    {
                        if (null != activatedTaskNode)
                        {
                            FinishActivationNode(activatedTaskNode, ActiveNodeStateType.Abort);
                        }

                        for (int i = searchPath.Count - 1; i >= 0; --i)
                        {
                            if (searchPath[i].node.NodeIndex > abortNode.NodeIndex)
                            {
                                foreach (var aux in searchPath[i].node.auxNodes)
                                {
                                    if (aux.BaseDef.NodeIsDecorator())
                                    {
                                        aux.WrappedOnCeaseRelevant();
                                    }
                                }
                                searchPath.RemoveAt(i);
                            }
                            else
                            {
                                break;
                            }
                        }

                        while(true)
                        {
                            if (executingPath[^1].node == abortNode)
                            {
                                break;
                            }
                            Bubble(SearchResultType.ExecuteFail);
                        }
                        
                        BubbleAndSearchNext(SearchResultType.Normal);
                    }
                }
                else
                {
                    //任务已完成
                    BubbleAndSearchNext(executingPath[^1].result);
                }
            }
            else
            {
                StartSearch();
            }
        }

        /// <summary>
        /// 检测搜索路径上的装饰器打断
        /// </summary>
        /// <param name="onExecutingPath"></param>
        /// <returns></returns>
        private BTNodeInstance CheckAbort(out bool onExecutingPath)
        {
            onExecutingPath = false;
            //测试搜索路径上的装饰器
            foreach (var searchData in searchPath)
            {
                var node = searchData.node;
                if (searchData.result == SearchResultType.ExecuteFail)
                {
                    continue;
                }

                bool? isOnExecutingPath = null;

                ICompositeNodeInst crossNode = null;
                bool abortLowerPriority = false;

                foreach (var aux in node.auxNodes)
                {
                    if (!aux.BaseDef.NodeIsDecorator()) continue;

                    var decorator = aux as IDecoratorNodeInst;

                    //不打断
                    if (decorator.FlowAbortMode == BTFlowAbortMode.None) continue;

                    isOnExecutingPath ??= NodeIsOnExecutingPath(node);

                    var testResult = decorator.WrappedPerformConditionCheck();
                    //测试通过
                    if (testResult)
                    {
                        //在执行路径上
                        if (isOnExecutingPath.Value)
                        {
                            continue;
                        }
                        //测试通过并且不在执行路径上

                        //设置可以打断低优先级
                        if (decorator.FlowAbortMode is BTFlowAbortMode.LowerPriority or BTFlowAbortMode.Both)
                        {
                            if (crossNode == null)
                            {
                                var path = BubbleSearchCrossNode(node);
                                crossNode = path.Value.node as ICompositeNodeInst;
                            }
                            var allowAbort = crossNode.AllowLowerPriorityAbort();
                            if (allowAbort)
                            {
                                abortLowerPriority = true;
                            }
                            else
                            {
                                abortLowerPriority = false;
                                break;
                            }
                            
                        }
                    }
                    else //测试不通过
                    {
                        //在执行路径上, 并且可打断自身
                        if (isOnExecutingPath.Value)
                        {
                            if (decorator.FlowAbortMode is BTFlowAbortMode.Self or BTFlowAbortMode.Both)
                            {
                                onExecutingPath = true;
                                return node;
                            }
                        }
                        else
                        {
                            //设置可以打断低优先级
                            if (decorator.FlowAbortMode is BTFlowAbortMode.LowerPriority or BTFlowAbortMode.Both)
                            {
                                abortLowerPriority = false;
                                break;
                            }
                        }
                    }
                }

                if (abortLowerPriority)
                {
                    onExecutingPath = isOnExecutingPath.Value;
                    return crossNode as BTNodeInstance;
                }

            }

            return null;
        }

        /// <summary>
        /// 勘探是否有激活的路径
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool ProbeCheckWithOutActivatedTask(BTNodeInstance node)
        {
            foreach (var aux in node.auxNodes)
            {
                if (aux.BaseDef.NodeIsDecorator() && aux is IDecoratorNodeInst decorator)
                {
                    //检测不通过
                    if (!decorator.WrappedRawConditionCheck())
                    {
                        return false;
                    }
                }
            }

            if (node.BaseDef.NodeIsLeaf() || node.children.Count <= 0)
            {
                if (node == ExecutingNodePath[^1].node) return false;
                return true;
            }

            for (int i = 0; i < node.children.Count; ++i)
            {
                var compositeNodeInst = node as ICompositeNodeInst;
                var nextIndex = compositeNodeInst.GetNextChildHandler(i - 1,
                    node.children.Count,
                    SearchResultType.Normal, true);

                if (nextIndex == BTSpecialChild.ReturnToParent
                    || nextIndex >= node.children.Count)
                {
                    //返回上一层
                    return false;
                }

                if (ProbeCheckWithOutActivatedTask(node.children[nextIndex]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 重置行为树状态
        /// </summary>
        private void Reset()
        {
            activatedEmptyNode = false;
            activatedTaskNode = null;
            executingPath.Clear();
            searchPath.Clear();
        }

        
        /// <summary>
        /// 开始搜索树
        /// </summary>
        private void StartSearch()
        {
            Reset();
            SearchPathData rootPath = default;
            rootPath.node = rootNode;
            rootPath.indexWithParent = 0;
            rootPath.result = SearchResultType.Normal;

            executingPath.Add(rootPath);
            searchPath.Add(rootPath);

            Search();
        }

        
        
        /// <summary>
        /// 搜索树
        /// </summary>
        private void Search()
        {
            var curPathData = executingPath[^1];
            var curNode = curPathData.node;
            searchNodeEvent?.Invoke(curNode);
            curNode.OnSearchStart();

            //如果检测不通过,返回上一层执行下一个子节点
            if (!ConditionCheckAllDecorator(curNode))
            {
                //返回上一层
                BubbleAndSearchNext(SearchResultType.CheckFail);
                return;
            }

            
            //如果是叶节点, 终止遍历树等待任务结束
            if (curNode.BaseDef.NodeIsLeaf())
            {
                ActivateNode(curNode);
                return;
            }

            //循环自己的子节点
            NextChild(curNode, -1, SearchResultType.Normal, true);
        }

        /// <summary>
        /// 冒泡
        /// </summary>
        /// <param name="searchResult"></param>
        private void Bubble(SearchResultType searchResult)
        {
            var curPathData = executingPath[^1];
            bubbleNodeEvent?.Invoke(curPathData.node);
            curPathData.node.OnLeave(searchResult);
            
            executingPath.RemoveAt(executingPath.Count - 1);
            
            foreach (var aux in curPathData.node.auxNodes)
            {
                if (aux.BaseDef.NodeIsService())
                {
                    aux.WrappedOnCeaseRelevant();
                }
            }
        }
        
        /// <summary>
        /// 冒泡后执行下一个子节点
        /// </summary>
        /// <param name="searchResult"></param>
        private void BubbleAndSearchNext(SearchResultType searchResult)
        {
            var curPathData = executingPath[^1];
            
            if (searchResult == SearchResultType.ExecuteFail)
            {
                foreach (var aux in searchPath[^1].node.auxNodes)
                {
                    if (aux.BaseDef.NodeIsDecorator())
                    {
                        aux.WrappedOnCeaseRelevant();
                    }
                }
                    
                searchPath.RemoveAt(searchPath.Count - 1);
            }
            else
            {
                for (int i = searchPath.Count - 1; i >= 0; --i)
                {
                    if (searchPath[i].node == curPathData.node)
                    {
                        var searchData = searchPath[i];
                        searchData.result = searchResult;
                        searchPath[i] = searchData;
                        break;
                    }
                }
            }
            Bubble(searchResult);

            if (executingPath.Count > 0)
            {
                //下一个子节点
                NextChild(executingPath[^1].node, curPathData.indexWithParent, searchResult, false);
            }
            else
            {
                treeSearchFinish?.Invoke(this);
            }
        }

        /// <summary>
        /// 寻找下一个子节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="curChildIndex"></param>
        /// <param name="searchResult"></param>
        /// <param name="trickleDown"></param>
        private void NextChild(BTNodeInstance node, int curChildIndex, SearchResultType searchResult, bool trickleDown)
        {
            var compositeNodeInst = node as ICompositeNodeInst;
            if (null == compositeNodeInst) return;

            //执行空节点
            if (node.children.Count <= 0)
            {
                activatedEmptyNode = true;
                return;
            }

            var nextIndex = compositeNodeInst.GetNextChildHandler(curChildIndex,
                node.children.Count,
                searchResult, trickleDown);

            if (nextIndex == BTSpecialChild.ReturnToParent
                || nextIndex >= node.children.Count)
            {
                //返回上一层
                BubbleAndSearchNext(searchResult);
                return;
            }
            
            var nextNode = node.children[nextIndex];
            SearchPathData nextPathData = default;
            nextPathData.indexWithParent = nextIndex;
            nextPathData.node = nextNode;
            nextPathData.result = SearchResultType.Normal;
            executingPath.Add(nextPathData);
            searchPath.Add(nextPathData);
            Search();
        }


        /// <summary>
        /// 指定一个节点，冒泡搜索一个在执行路径上的节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private SearchPathData? BubbleSearchCrossNode(BTNodeInstance node)
        {
            if (executingPath.Count <= 0) return null;
            do
            {
                for (int i = executingPath.Count - 1; i >= 0; --i)
                {
                    if (executingPath[i].node == node)
                    {
                        return executingPath[i];
                    }
                }

                node = node.ParentNode;
            } while (node != null);


            return null;
        }

        /// <summary>
        /// 查找节点是否在执行路径上
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool NodeIsOnExecutingPath(BTNodeInstance node)
        {
            foreach (var item in executingPath)
            {
                if (item.node == node) return true;
            }

            return false;
        }

        /// <summary>
        /// 检测节点的装饰器是否通过
        /// </summary>
        /// <param name="node"></param>
        /// <param name="probe"></param>
        /// <returns></returns>
        private bool ConditionCheckAllDecorator(BTNodeInstance node)
        {
            foreach (var aux in node.auxNodes)
            {
                aux.WrappedOnBecomeRelevant();
                if (aux.BaseDef.NodeIsDecorator() && aux is IDecoratorNodeInst decorator)
                {
                    //检测不通过
                    if (!decorator.WrappedRawConditionCheck())
                    {
                        decoratorFailEvent?.Invoke(aux as BTNodeInstance);
                        return false;
                    }
                }
            }

            return true;
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ActivateNode(BTNodeInstance node)
        {
            var taskNode = node as ITaskNodeInstance;
            activatedTaskNode = taskNode;
            taskNode.OnActivation();
            activatedTaskNodeEvent?.Invoke(node);
        }

        /// <summary>
        /// 给node内部调用的结束当前节点的函数
        /// </summary>
        /// <param name="node"></param>
        /// <param name="stateType"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishActivationNode(ITaskNodeInstance node, ActiveNodeStateType stateType)
        {
            if (activatedTaskNode != node) return;
            activatedTaskNode = null;
            var data = executingPath[^1];
            if (stateType != ActiveNodeStateType.Success)
            {
                data.result = SearchResultType.ExecuteFail;
                executingPath[^1] = data;
            }

            node.OnDeactivation(stateType);
        }

        /// <summary>
        /// 给node 内部调用结束任务
        /// </summary>
        /// <param name="node"></param>
        /// <param name="bSuccess"></param>
        internal void FinishTask(ITaskNodeInstance node, bool bSuccess)
        {
            FinishActivationNode(node, bSuccess
                ? ActiveNodeStateType.Success
                : ActiveNodeStateType.Cancel);
        }

        /// <summary>
        /// 给node 内部调用结束任务
        /// </summary>
        internal void AbortTask(ITaskNodeInstance node)
        {
            FinishActivationNode(node, ActiveNodeStateType.Abort);
        }


        #endregion

        #region 发送消息
        
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息名称</param>
        /// <param name="payload">消息内容</param>
        /// <param name="range">消息范围</param>
        public void SendBTMessage<T>(string message, T payload, BTMessageRange range = BTMessageRange.TrickleDown)
        {
            if (range == BTMessageRange.Broadcast)
            {
                List<BTNodeInstance> children = RootNode.children;
                for (int i = 0; i < children.Count; ++i)
                {
                    var node = children[i];
                    children.AddRange(node.children);
                    SendMessageToNode(node, message, payload);
                }
            }
            else if (range == BTMessageRange.TrickleDown)
            {
                foreach (var path in executingPath)
                {
                    SendMessageToNode(path.node, message, payload);
                }
            }
            else if(range == BTMessageRange.ActivatedTaskNode)
            {
                if (0 >= executingPath.Count) return;
                SendMessageToNode(executingPath[^1].node, message, payload);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendMessageToNode<T>(BTNodeInstance node, string message, T payload)
        {
            node.ReceivedMessage(message, payload);
            //发送到助手节点上
            foreach (var auxNode in node.auxNodes)
            {
                auxNode.ReceivedMessage(message, payload);
            }
        }
        
        #endregion
        
        /// <summary>
        /// 释放资源
        /// </summary>
        private void Dispose()
        {
            List<BTNodeInstance> allNodes = new List<BTNodeInstance>(16) { RootNode };
            for (int i = 0; i < allNodes.Count; ++i)
            {
                var node = allNodes[i];
                foreach (var auxNode in node.auxNodes)
                {
                    auxNode.Dispose();
                }

                allNodes.AddRange(node.children);

                node.Dispose();
            }
        }
    }

    /// <summary>
    /// 行为树执行路径节点
    /// </summary>
    public struct SearchPathData
    {
        /// <summary>
        /// 所属父类的下标
        /// </summary>
        public int indexWithParent;

        public BTNodeInstance node;

        public SearchResultType result;
    }


    /// <summary>
    /// 行为树消息范围
    /// </summary>
    public enum BTMessageRange
    {
        /// <summary>
        /// 广播,不保证顺序并且全部节点收到
        /// </summary>
        Broadcast,

        /// <summary>
        /// 涓滴事件,从根节点涓滴到执行路径上的节点
        /// </summary>
        TrickleDown,

        /// <summary>
        /// 执行中的任务节点
        /// </summary>
        ActivatedTaskNode,
    }
}