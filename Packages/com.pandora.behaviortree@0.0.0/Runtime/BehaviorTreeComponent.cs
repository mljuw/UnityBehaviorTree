
using UnityEngine;

namespace Pandora.BehaviorTree
{

    public class BehaviorTreeComponent : MonoBehaviour
    {
        
        [Header("行为树")]
        public BehaviorTreeAsset treeAsset;

        [Header("自动播放行为树")]
        public bool autoplayTree;

        [Header("黑板")] 
        public BTBlackboard blackboardAsset;

        private BehaviorTreeInstance treeInstance;

        public BehaviorTreeInstance TreeInstance => treeInstance;

        public bool IsPause;

        private bool enableTick;

        public bool IsRunningBehaviorTree => treeInstance.IsRunning;


        public BehaviorTreeComponent()
        {
            treeInstance = new (this);
        }

        public BTBlackboardInst Blackboard => treeInstance.Blackboard;

        public void Start()
        {
            if (autoplayTree && treeAsset != null)
            {
                treeInstance.LoadBlackboard(blackboardAsset);
                treeInstance.StartTree(treeAsset);
                ShouldTick();
            }
        }

        public void Update()
        {
            if (enableTick)
            {
               treeInstance.Tick(Time.deltaTime);
            }
        }

        private void ShouldTick()
        {
            enableTick = treeInstance.IsRunning & !IsPause;
        }

        
        /// <summary>
        /// 暂停行为树
        /// </summary>
        public void Pause()
        {
            IsPause = true;
            ShouldTick();
        }

        /// <summary>
        /// 恢复运行行为树
        /// </summary>
        public void Resume()
        {
            IsPause = false;
            ShouldTick();
        }

        public void RunBehaviorTree()
        {
            treeInstance.LoadBlackboard(blackboardAsset);
            treeInstance.StartTree(treeAsset);
            ShouldTick();
        }

        public void RunBehaviorTree(BehaviorTreeAsset tree, BTBlackboard blackboard)
        {
            treeInstance.LoadBlackboard(blackboard);
            treeInstance.StartTree(tree);
            ShouldTick();
        }

    }
}