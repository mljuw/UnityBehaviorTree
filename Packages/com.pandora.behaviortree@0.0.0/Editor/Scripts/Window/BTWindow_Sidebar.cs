using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 行为树试图里的侧边栏
    /// </summary>
    public partial class BehaviorTreeWindow
    {
        private Label btnSidebar;
        private VisualElement sidebarPanel; 
        private ScrollView scrollView;
        private MiniMap miniMap;
        private int lastEditableNodeIdx = -1;
        private SidebarDragger dragger;

        private void CreateSidebar(VisualElement container)
        {
            //加载层叠样式
            var styleSheet = Resources.Load<StyleSheet>("Window/Sidebar");
            
            //隐藏/显示侧边栏的按钮
            btnSidebar = new Label()
            {
                name = "btn-sidebar",
                text = "||",
            };
            btnSidebar.styleSheets.Add(styleSheet);
            container.Add(btnSidebar);
            dragger = new SidebarDragger(btnSidebar, SidebarDragger.DragAxis.X);
            dragger.OnUpdateDragPos += OnUpdateDragPos;
            
            
            Color bgColor = new Color32(50, 50, 50, 255);
            //创建存放内容的容器（背景）
            sidebarPanel = new VisualElement
            {
                name = "side-bar",
                style =
                {
                    height = new Length(100, LengthUnit.Percent),
                    backgroundColor = bgColor,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 4,
                    paddingBottom = 4,
                    display = DisplayStyle.Flex,
                }
            };

           
            //添加到样式列表中
            sidebarPanel.styleSheets.Add(styleSheet);
            //将容器添加到窗口上
            container.Add(sidebarPanel);

            //创建一个可滚动视图
            scrollView = new ScrollView
            {
                name = "container",
                style =
                {
                    flexGrow = 1,
                },
                horizontalScroller =
                {
                    //隐藏水平滚动条
                    visible = false
                }
            };
            //将它添加到侧边栏中
            sidebarPanel.Add(scrollView);
            
            //创建一个小地图,用于方便观察行为树里面的结构与所在区域
            miniMap = new MiniMap
            {
                anchored = true,
                windowed = true,
                graphView = graphView,
                style =
                {
                    width = new Length(100, LengthUnit.Percent),
                    minHeight = 80,
                    maxHeight = 250f,
                }
            };
            //小地图添加到侧边栏
            sidebarPanel.Add(miniMap);
        }

        private void OnUpdateDragPos(Vector2 offset)
        {
            sidebarPanel.style.width = graphView.resolvedStyle.width - btnSidebar.resolvedStyle.left - btnSidebar.resolvedStyle.width ;
        }


        private void UpdateSidebar()
        {
            if (!dragger.OnTouch)
            {
                btnSidebar.style.left = sidebarPanel.resolvedStyle.left - btnSidebar.resolvedStyle.width;
                btnSidebar.style.top = sidebarPanel.resolvedStyle.height / 2;
            }
        }

        private void RefreshSidebar()
        {
            //新选择之前选择的编辑节点
            if (lastEditableNodeIdx > 0)
            {
                graphView.SelectNodeByIndex(lastEditableNodeIdx);
            }
            else
            {
                //如果没有则清空面板
                GraphViewOnSelectedEditableEvent(null);
            }
        }
       
        /// <summary>
        /// 如果 行为树视图中选了某个可编辑元素时会调用此函数
        /// </summary>
        /// <param name="ele"></param>
        private void GraphViewOnSelectedEditableEvent(IBTEditableElement ele)
        {
            //清理滚动视图
            scrollView.Clear();
            //判断选择的元素有没有设置 ‘监视面板’ 的类
            if (ele != null && ele.EditInspectorClass != null)
            {
                //创建编辑元素的‘监视面板’
                var editPanel = (ElementInspectorBase)Activator.CreateInstance(ele.EditInspectorClass);
                editPanel.Init(ele);
                //调用初始化函数
                editPanel.DrawUI();
                //'监视面板'添加到滚动视图中
                scrollView.Add(editPanel);
                
                //记录最后编辑的节点下标
                lastEditableNodeIdx = ele.GetNodeIndex();
            }
            else
            {
                lastEditableNodeIdx = -1;
            }
        }
    }

    /// <summary>
    /// 拖动侧边栏助手
    /// </summary>
    public class SidebarDragger
    {
        /// <summary>
        /// 参数位置偏移
        /// </summary>
        public Action<Vector2> OnUpdateDragPos;
        private VisualElement target;
        private Vector2 lastMousePos = default;
        private bool onTouch = false;
        public bool OnTouch => onTouch;
        private DragAxis axis;

        public enum DragAxis
        {
            XY,
            X,
            Y
        }
        
        public SidebarDragger(VisualElement dragTarget, DragAxis dragAxis)
        {
            axis = dragAxis;
            target = dragTarget;
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (onTouch)
            {
                var diff = evt.mousePosition - lastMousePos;
                if (axis == DragAxis.X)
                {
                    diff.y = 0;
                }
                else if (axis == DragAxis.Y)
                {
                    diff.x = 0;
                }
                if (diff.sqrMagnitude > float.Epsilon)
                {
                    OnUpdateDragPos?.Invoke(diff);
                    target.style.left = target.resolvedStyle.left + diff.x;
                    target.style.top = target.resolvedStyle.top + diff.y;
                }

                lastMousePos = evt.mousePosition;
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            onTouch = false;
            target.ReleaseMouse();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            lastMousePos = evt.mousePosition;
            target.CaptureMouse();
            onTouch = true;
        }
        
    }
}