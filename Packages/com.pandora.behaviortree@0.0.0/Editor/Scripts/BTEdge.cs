using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    public class BTEdge : Edge
    {
        BehaviorTreeGraphView mGraphView;
        private LinkedList<FlowEdge> flowEdgeList = new();

        private bool bIsDebug = false;
        private bool bActivated = false;
        
        public BTEdge()
        {
            showInMiniMap = true;
            elementTypeColor = Color.green;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
        }

        private void OnAttachToPanelEvent(AttachToPanelEvent evt)
        {
            //新建线条时通知 graphView
            if(graphView != null)
            {
                graphView.OnEdgePortChange(this);

                if (edgeControl.controlPoints != null)
                {
                    var diff = flowEdgeList.Count - edgeControl.controlPoints.Length;
                    while (diff > 0)
                    {
                        flowEdgeList.Last.Value.RemoveFromHierarchy();
                        flowEdgeList.RemoveLast();
                        diff--;
                    }

                    while (diff < 0)
                    {
                        var flowEdge = new FlowEdge();
                        graphView.Add(flowEdge);
                        flowEdgeList.AddLast(flowEdge);
                        diff++;
                    }
                }
            }
        }

        private void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            //删除线条时通知 graphView
            if(graphView != null)
            {
                graphView.OnEdgePortChange(this);
            }

            foreach (var flowEdge in flowEdgeList)
            {
                flowEdge.RemoveFromHierarchy();
            }
            flowEdgeList.Clear();
        }

        public void Tick(float deltaTime, float totalTime)
        {
            if (bActivated)
            {
                var firstFlowEdgeNode = flowEdgeList.First;
                for (int i = 0; i < edgeControl.controlPoints.Length - 1; ++i)
                {
                    var start = edgeControl.controlPoints[i];
                    var end = edgeControl.controlPoints[i + 1];

                    var flowEdge = firstFlowEdgeNode.Value;
                    start = this.LocalToWorld(start);
                    end = this.LocalToWorld(end);
                    start = graphView.WorldToLocal(start);
                    end = graphView.WorldToLocal(end);
                    flowEdge.start = start;
                    flowEdge.end = end;
                    flowEdge.Tick(totalTime);
                    firstFlowEdgeNode = firstFlowEdgeNode.Next;
                }
               
            }
        }

        /// <summary>
        /// 设置是否高亮
        /// </summary>
        /// <param name="activated"></param>
        public void HighLight(bool activated)
        {
            bActivated = activated;
            if (activated)
            {
                foreach (var flowEdge in flowEdgeList)
                {
                    flowEdge.SetEnable(true);
                }

                edgeControl.inputColor = Color.blue;
                //设置输入端颜色,因为在刷新界面时会覆盖掉inputColor. (output也一样)
                input.portColor = edgeControl.inputColor;
            }
            else
            {
                foreach (var flowEdge in flowEdgeList)
                {
                    flowEdge.SetEnable(false);
                }
                edgeControl.inputColor = new Color(0x76/255f, 0x67/255f, 0xB8/255f, 1);
                //设置输入端颜色,因为在刷新界面时会覆盖掉inputColor. (output也一样)
                input.portColor = edgeControl.inputColor;
            }
        }
        
        /// <summary>
        /// 设置调试模式
        /// </summary>
        /// <param name="bEnable"></param>
        public void SetEnableDebug(bool bEnable)
        {
            //屏蔽一些操作
            bIsDebug = bEnable;
            if (bEnable)
            {
                capabilities &= ~Capabilities.Selectable;
                capabilities &= ~Capabilities.Deletable;
                capabilities &= ~Capabilities.Droppable;
                pickingMode = PickingMode.Ignore;
            }
            else
            {
                capabilities |= Capabilities.Selectable;
                capabilities |= Capabilities.Deletable;
                capabilities |= Capabilities.Droppable;
                pickingMode = PickingMode.Position;
            }
        }
        
        protected override void ExecuteDefaultAction(EventBase evt)
        {
            //调试模式下屏蔽一些事件,比如获取焦点等。
            if (bIsDebug) return;
            base.ExecuteDefaultAction(evt);
        }

        public BehaviorTreeGraphView graphView
        {
            get
            {
                //往父级查找 graphView
                if (mGraphView == null)
                    mGraphView = GetFirstAncestorOfType<BehaviorTreeGraphView>();
                return mGraphView;
            }
        }

    }

    public class FlowEdge : ImmediateModeElement
    {
        public Vector2 start { get; set; }
        public Vector2 end { get; set; }

        private Material material;

        public FlowEdge()
        {
            material = Resources.Load<Material>("FlowEdge");
        }

        public void Tick(float totalTime)
        {
            material.SetFloat("_UITime", totalTime);
        }

        public void SetEnable(bool value)
        {
            visible = value;
            MarkDirtyRepaint();
        }
        
        protected override void ImmediateRepaint()
        {
            if (!visible || start == end)
                return;
            Vector2 vector2_1 = start + parent.layout.position;
            Vector2 vector2_2 = end + parent.layout.position;
            
            float segmentsLength = 5f;
            DrawLine(vector2_1, vector2_2, segmentsLength);

            style.position = Position.Absolute;
            style.top = 0.0f;
            style.left = 0.0f;
            style.right = 0.0f;
            style.bottom = 0.0f;
            pickingMode = PickingMode.Ignore;
            MarkDirtyRepaint();
        }

        private void DrawLine(Vector3 p1, Vector3 p2, float segmentsLen)
        {
            GL.PushMatrix();
            GL.LoadPixelMatrix();
            material.SetPass(0);
            GL.Begin(GL.LINES);
            float num1 = Vector3.Distance(p1, p2);
            int num2 = Mathf.CeilToInt(num1 / segmentsLen);
            for (int index = 0; index < num2; index += 2)
            {
                // GL.Vertex(Vector3.Lerp(p1, p2, (float) index * segmentsLen / num1));
                // GL.Vertex(Vector3.Lerp(p1, p2, (float) (index + 1) * segmentsLen / num1));

                var segP1 = Vector3.Lerp(p1, p2, (float)index * segmentsLen / num1);
                var segP2 = Vector3.Lerp(p1, p2, (float)(index + 1) * segmentsLen / num1);

                DrawSeg(segP1, segP2, 4);
            }
            GL.End();
            GL.PopMatrix();
        }

        private void DrawSeg(in Vector3 p1, in Vector3 p2, float lineThickness)
        {
            var dir = p2 - p1;
            
            Quaternion rotation = Quaternion.AngleAxis(90, Vector3.forward);
            dir = rotation * dir;
            dir = dir.normalized;
            
            // 绘制多条偏移的线段
            for (float offset = -lineThickness / 2f; offset <= lineThickness / 2f; offset++)
            {
                Vector3 offsetVector = dir * offset;
                
                GL.Vertex(p1 + offsetVector);
                GL.Vertex(p2 + offsetVector);
            }
        }
    }
}