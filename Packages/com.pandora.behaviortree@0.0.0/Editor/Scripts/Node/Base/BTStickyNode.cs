using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 注释节点
    /// </summary>
    public class BTStickyNode : StickyNote
    {
        
        protected BehaviorTreeGraphView mGraphView;
        public BehaviorTreeGraphView GraphView => mGraphView ??= GetFirstAncestorOfType<BehaviorTreeGraphView>();

        protected StickyAsset nodeData;

        public BTStickyNode(StickyAsset asset)
        {
            nodeData = asset;
            capabilities |= Capabilities.Deletable;
            fontSize = StickyNoteFontSize.Small;
            
            SetPosition(nodeData.position);
            title = nodeData.title;
            contents = nodeData.content;
            
            RegisterCallback<StickyNoteChangeEvent>(UpdateCallback);
        }

        private void UpdateCallback(StickyNoteChangeEvent evt)
        {
            Undo.RecordObject(GraphView.EditTarget, "Modify stick.");
            nodeData.title = title;
            nodeData.content = contents;
            nodeData.position = GetPosition();

            GraphView.SerializedEditObj.ApplyModifiedProperties();
            GraphView.SerializedEditObj.Update();
        }


        // /// <summary>
        // /// 修改位置时
        // /// </summary>
        // public override void UpdatePresenterPosition()
        // {
        //     base.UpdatePresenterPosition();
        // }
    }
}