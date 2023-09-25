using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// '等待'任务节点
    /// </summary>
    [Serializable, BTNode("Wait", "任务", "等待一段时间")]
    public class BTT_Wait : BTTaskNode
    {
        [Header("等待时间")]
        public Vector2 durationRange = new (5f, 5f);
        
        public override Type GetInstanceClass()
        {
            return typeof(BttWaitInst);
        }
    }

    public class BttWaitInst : BTTaskNodeInst<BTT_Wait>
    {
        private float countdown = default;
        
        public override void OnActivation()
        {
            base.OnActivation();
            countdown = GetDuration();
        }

        public float GetDuration()
        {
            return Random.Range(Define.durationRange.x, Define.durationRange.y);
        }

        public override void TickNode(float deltaTime)
        {
            base.TickNode(deltaTime);
            if (0 >= countdown)
            {
                treeInst.FinishTask(this, true);
                return;
            }
            
            countdown -= deltaTime;
        }
    }
}