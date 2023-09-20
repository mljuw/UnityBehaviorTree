using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    [GraphBTNode(typeof(BTDecoratorNode))]
    public class DecoratorNodeElement : GraphNodeElement
    {
        public override Type EditInspectorClass
        {
            get => typeof(ElementInspector);
        }
        
        /// <summary>
        /// 调试的边框
        /// </summary>
        protected VisualDebugBorder debugBorder;

        public override void Init(BTGraphNode ownerNode, BTNode node, SerializedProperty nodeSerializedProp)
        {
            base.Init(ownerNode, node, nodeSerializedProp);
            
            debugBorder = new VisualDebugBorder();
            var color = new StyleColor(Color.red);
            debugBorder.blinkBorder.sColor = color;
            debugBorder.blinkBorder.fadeOutDuration = 1f;
            Add(debugBorder);
        }

        #region 调试
        
        
        public override void SetActivation(bool activated)
        {
            debugBorder.Blink();
        }

        public override void DebugTick(float deltaTime)
        {
            debugBorder.Tick(deltaTime);
        }
        
        #endregion
    }
    
}