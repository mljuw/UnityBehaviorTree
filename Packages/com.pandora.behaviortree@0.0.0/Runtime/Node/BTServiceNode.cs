using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pandora.BehaviorTree
{
    
    [BTNode("服务", description = "用于定时完成一些事情")]
    public abstract class BTServiceNode : BTAuxiliaryNode
    {
        public BTServiceNode()
        {
            nodeType = BTNodeType.Service;
        }
        
        [Header("执行间隔")]
        public float interval = 0.01f;

        [Header( "间隔浮动区间")]
        public float randomDeviation = 0f;

        public override Type GetInstanceClass()
        {
            return typeof(ServiceNodeInst<BTServiceNode>);
        }
    }

    
    public abstract class ServiceNodeInst<T> : BTAuxiliaryNodeInst<T> where T : BTServiceNode
    {
        protected float countdown = 0f;

        /// <summary>
        /// Service 要执行的逻辑在这个函数实现,而不是TickNode.
        /// </summary>
        /// <param name="deltaTime"></param>
        public virtual void CheckAndTick(float deltaTime)
        {
            
        }
        
        public override void TickNode(float deltaTime)
        {
            if (countdown <= 0)
            {
                CheckAndTick(deltaTime);
                countdown = Define.interval + Random.Range(- Define.randomDeviation, Define.randomDeviation);
                countdown = Mathf.Clamp(countdown, 0, countdown);
            }
            else
            {
                countdown -= deltaTime;
            }
        }

    }
}