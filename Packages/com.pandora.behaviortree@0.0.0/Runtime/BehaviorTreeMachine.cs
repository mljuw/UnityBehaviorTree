using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pandora.BehaviorTree
{
    //
    // /// <summary>
    // /// 行为树执行器
    // /// </summary>
    // public class BehaviorTreeMachine
    // {
    //     
    //     /// <summary>
    //     /// 行为树执行事件
    //     /// </summary>
    //     public static event Action<BehaviorTreeInstance> startTreeEvent;
    //     
    //     /// <summary>
    //     /// 停止执行行为树事件
    //     /// </summary>
    //     public static event Action<BehaviorTreeInstance> stopTreeEvent;
    //     
    //     public BehaviorTreeMachine(object owner)
    //     {
    //         this.owner = owner;
    //     }
    //
    //     ~BehaviorTreeMachine()
    //     {
    //         Dispose();
    //     }
    //
    //     public void Dispose()
    //     {
    //         if (treeInstance != null)
    //         {
    //             treeInstance.Dispose();
    //             treeInstance = null;
    //         }
    //     }
    //     
    //     private object owner;
    //     public object Owner => owner;
    //     
    //
    //     private BTBlackboard blackboardAsset;
    //     public BTBlackboard BlackboardAsset => blackboardAsset;
    //
    //     /// <summary>
    //     /// 黑板实例
    //     /// </summary>
    //     protected BTBlackboardInst blackboardInst;
    //
    //     /// <summary>
    //     /// 获取黑板
    //     /// </summary>
    //     public BTBlackboardInst Blackboard
    //     {
    //         get => blackboardInst;
    //     }
    //     
    //     private bool isRunning = false;
    //     private bool isPause = false;
    //
    //     public bool IsRunningTree => isRunning;
    //
    //     public bool IsPause
    //     {
    //         get => isPause;
    //         set
    //         {
    //             isPause = value;
    //             treeInstance.IsRunning = value;
    //         }
    //     }
    //
    //     private BehaviorTreeInstance treeInstance;
    //
    //
    //     
    //     /// <summary>
    //     /// 运行行为树
    //     /// </summary>
    //     /// <param name="tree"></param>
    //     /// <param name="blackboard"></param>
    //     public void RunBehaviorTree(BehaviorTreeAsset tree, BTBlackboard blackboard)
    //     {
    //         blackboardAsset = blackboard;
    //         if (isRunning)
    //         {
    //             StopTree();
    //         }
    //         
    //         treeInstance = new BehaviorTreeInstance(this);
    //         if (treeInstance.StartTree(tree))
    //         {
    //             if (null != blackboard)
    //             {
    //                 blackboardInst = new BTBlackboardInst(blackboard);
    //             }
    //             isRunning = true;
    //             isPause = false;
    //         }
    //         
    //         startTreeEvent?.Invoke(treeInstance);
    //     }
    //
    //
    //     public void Tick(float deltaTime)
    //     {
    //         if (!isRunning || IsPause) return;
    //         
    //         treeInstance.Tick(deltaTime);
    //     }
    //
    //    
    //
    //     /// <summary>
    //     /// 停止树
    //     /// </summary>
    //     public void StopTree()
    //     {
    //         if (!isRunning) return;
    //         
    //         stopTreeEvent?.Invoke(treeInstance);
    //         isRunning = false;
    //         isPause = false;
    //         treeInstance.IsRunning = false;
    //         treeInstance.Dispose();
    //         treeInstance = null;
    //     }
    //
    //     /// <summary>
    //     /// 发送行为树事件
    //     /// </summary>
    //     /// <param name="message">消息名字</param>
    //     /// <param name="payload">消息负载</param>
    //     /// <param name="range">消息范围</param>
    //     public void SendBTMessage<T>(string message, T payload, BTMessageRange range)
    //     {
    //         if (!isRunning) return;
    //         treeInstance.SendBTMessage<T>(message, payload, range);
    //     }
    //
    // }
}