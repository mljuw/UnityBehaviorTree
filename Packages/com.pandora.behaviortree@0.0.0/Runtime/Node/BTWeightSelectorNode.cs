using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pandora.BehaviorTree
{
    [Serializable]
    public struct NodeWeightInfo
    {
        //权重范围0-100
        [Header("权重值"), Range(0, 100)]
        public int weight;
        
        [Header("随机数值，不应该手动设置"), HideInInspector]
        public int randomNum;
    }
    
    /// <summary>
    /// 按权重分配执行子节点
    /// 子节点执行成功、失败也会终止执行其他子节点。
    /// </summary>
    [BTNodeAttribute("权重选择器", "组合")]
    public class BTWeightSelectorNode : BTCompositeNode
    {
        /// <summary>
        /// 子节点的权重比例
        /// 如果数量少于子节点数量，则没有配置的子节点权重为0
        /// 权重范围0-1浮点数, 1为100%权重
        /// </summary>
        public List<NodeWeightInfo> childrenWeight = new();
        
        public BTWeightSelectorNode()
        {
            nodeType = BTNodeType.Selector;
        }

        public override Type GetInstanceClass()
        {
            return typeof(BtWeightSelectorNodeInst);
        }
    }

    public class BtWeightSelectorNodeInst : BTCompositeNodeInst<BTWeightSelectorNode>
    {
        public override int GetNextChildHandler(int preChild, int childNum, SearchResultType lastResult, bool trickleDown)
        {
            //子节点执行成功或者失败都返回父节点
            if(!trickleDown) return BTSpecialChild.ReturnToParent;
            
            int cfgNum = Def.childrenWeight.Count;
            int randNum = -1;
            for (int i = 0; i < childNum; ++i)
            {
                //如果没有找到合适权重则返回夫节点
                if (i >= cfgNum)
                {
                    return BTSpecialChild.ReturnToParent;
                }
                if (randNum < 0)
                {
                    randNum = Random.Range(0, 100);
                }
                if (randNum <= Def.childrenWeight[i].randomNum)
                {
                    return i;
                }
            }
            //如果没有配置任何节点权重则直接返回父节点。
            return BTSpecialChild.ReturnToParent;
        }
    }
}