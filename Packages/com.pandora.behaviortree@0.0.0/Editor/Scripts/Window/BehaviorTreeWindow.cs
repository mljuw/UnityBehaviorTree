using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;
using Object = UnityEngine.Object;

namespace Pandora.BehaviorTree
{
 
    public class DelimiterElement : VisualElement
    {
        public DelimiterElement()
        {
            AddToClassList("delimiter");
        }
    }

    public partial class BehaviorTreeWindow : EditorWindow
    {
        private float totalTime;
        private float lastUpdateTime;
        private BehaviorTreeAsset editAsset;
        private BehaviorTreeGraphView graphView;

        public bool IsDebug => graphView is { IsDebug: true };

        public BTDebugTarget? DebugTarget => graphView.DebugTarget;

        private bool _needSave;

        private bool NeedSave
        {
            get => _needSave;
            set
            {
                _needSave = value;
                if (value)
                {
                    btnSave.AddToClassList("need-save");
                }
                else
                {
                    btnSave.RemoveFromClassList("need-save");
                }
            }
        }

        [MenuItem("Window/AI/行为树")]
        public static BehaviorTreeWindow ShowWindow()
        {
            BehaviorTreeWindow win = GetWindow<BehaviorTreeWindow>("行为树");
            win.Show();
            return win;
        }

        private void CreateGUI()
        {
            var styleSheet = Resources.Load<StyleSheet>("Window/Window");
            rootVisualElement.styleSheets.Add(styleSheet);

            graphView = new BehaviorTreeGraphView(this);
            graphView.selectedEditableEvent += GraphViewOnSelectedEditableEvent;
            graphView.debugStateChange += OnDebugStateChange;
            graphView.refreshViewEvent += OnRefreshGraphViewEvent;
            
            
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            BehaviorTreeInstance.startTreeEvent += OnStartTreeEvent;
            BehaviorTreeInstance.stopTreeEvent += OnStopTreeEvent; 
            
            rootVisualElement.Add(graphView);
            graphView.StretchToParentSize();

            CreateToolbar(rootVisualElement);
            CreateSidebar(rootVisualElement);
            graphView.RegisterCallback<KeyDownEvent>(OnGraphViewKeyDownCallback);

            if (null != editAsset)
            {
                OpenAsset(editAsset);
            }
        }

        private void OnEnable()
        {
            
            Undo.undoRedoPerformed += UndoRedoPerformed;
            Undo.postprocessModifications += PostprocessModifications;
        }

        private void OnDisable()
        {
            if (NeedSave)
            {
                var opt = EditorUtility.DisplayDialog("提示", "未保存内容，关闭编辑器将丢失更改过的内容。", "保存", "继续关闭");
                if (opt)
                {
                    Save();
                }
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            BehaviorTreeInstance.startTreeEvent -= OnStartTreeEvent;
            BehaviorTreeInstance.stopTreeEvent -= OnStopTreeEvent;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.postprocessModifications -= PostprocessModifications;
        }

        private void OnGUI()
        {
            //处理按键保存
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.S && e.control)
            {
                Save();
                e.Use();
            }
        }


        private void OnGraphViewKeyDownCallback(KeyDownEvent evt)
        {
            if (evt.ctrlKey && evt.keyCode == KeyCode.S)
            {
                Save();
            }
        }

        private void OnRefreshGraphViewEvent()
        {
            RefreshSidebar();
        }

        private UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            foreach (var modification in modifications)
            {
                if (modification.currentValue.target == graphView.EditTarget)
                {
                    NeedSave = true;
                    break;
                }
            }

            return modifications;
        }

        private void UndoRedoPerformed()
        {
            NeedSave = true;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RefreshDebugList();
            }

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (IsDebug)
                {
                    StopDebug();
                }
            }
        }

        private void Update()
        {
            float currentTime = (float)EditorApplication.timeSinceStartup;
            float deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;
            totalTime += deltaTime;
            graphView.Tick(deltaTime, totalTime);
            UpdateSidebar();
        }


        #region 打开资源文件

        [OnOpenAsset(2)]
        public static bool Edit(int instanceID, int line)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceID);
            BehaviorTreeAsset bt = obj as BehaviorTreeAsset;
            if (bt)
            {
                var win = ShowWindow();
                win.OpenAsset(bt);
                return true;
            }

            return false;
        }

        #endregion
    }
}