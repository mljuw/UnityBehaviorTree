using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 行为树窗口的工具栏
    /// </summary>
    public partial class BehaviorTreeWindow
    {
        private VisualElement toolbarPanel;
        private Button btnOpenFile;
        private Button btnNewFile;
        private Button btnSave;
        private Button btnBlackboard;
        private DropdownField dropdownDebugField;
        private Button btnAttachDebug;
        private Button btnReturnParentTree;
        private List<BTDebugTarget> debugTargets = new ();

        private WeakReference<BTBlackboardWindow> blackboardWinRef = new (null);

        private void CreateToolbar(VisualElement container)
        {
            //创建工具栏的容器（背景）
            toolbarPanel = new VisualElement
            {
                name = "tool-bar",
                style =
                {
                    width = new Length(100, LengthUnit.Percent),
                    backgroundColor = new StyleColor(new Color32(50, 50, 50, 255)),
                    display = DisplayStyle.Flex,
                }
            };
            //加载层叠样式文件
            var styleSheet = Resources.Load<StyleSheet>("Window/Toolbar");

            //添加到工具栏的样式列表中
            toolbarPanel.styleSheets.Add(styleSheet);
            //将工具栏加入到窗口中
            container.Add(toolbarPanel);

            //创建一些按钮
            btnOpenFile = new Button(OpenFile);
            btnOpenFile.text = "打开文件";

            btnNewFile = new Button(NewFile);
            btnNewFile.text = "新建文件";

            btnSave = new Button(Save);
            btnSave.text = "保存";

            btnBlackboard = new Button(OpenBlackboard);
            btnBlackboard.text = "黑板";
            
            dropdownDebugField = new DropdownField("调试对象", new List<string>(){"刷新列表"}, -1, OnFormatSelectedDebugTarget);
            dropdownDebugField.RegisterCallback<ChangeEvent<string>>( OnSelectedDebugTarget);
            dropdownDebugField.Q<Label>().RegisterCallback<MouseDownEvent>(ClickRefreshDebugTargets);
            btnAttachDebug = new Button(ClickToggleDebug);
            btnAttachDebug.text = "调试";

            btnReturnParentTree = new Button(ClickReturnParentTree);
            btnReturnParentTree.text = "返回父树";
            
            //将按钮等操作元素添加到工具栏
            toolbarPanel.Add(btnSave);
            toolbarPanel.Add(btnNewFile);
            toolbarPanel.Add(btnOpenFile);
            toolbarPanel.Add(btnBlackboard);
            AddDelimiter();
            toolbarPanel.Add(dropdownDebugField);
            toolbarPanel.Add(btnAttachDebug);
            toolbarPanel.Add(btnReturnParentTree);
            
            //刷新调试列表
            RefreshDebugList();
            
            UpdateDebugPanel();
        }

        
        private void OnDebugStateChange(bool obj)
        {
            UpdateDebugPanel();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateDebugPanel()
        {
            btnReturnParentTree.visible = graphView.HasDebugParentTree;
        }

        /// <summary>
        /// 行为树播放事件(不包括子树)
        /// </summary>
        private void OnStartTreeEvent(BehaviorTreeInstance treeInst)
        {
            RegisterDebugTarget(treeInst);
            RefreshDebugList();
        }

        /// <summary>
        /// 行为树停止播放事件(不包括子树)
        /// </summary>
        private void OnStopTreeEvent(BehaviorTreeInstance treeInst)
        {
            UnRegisterDebugTarget(treeInst);
            RefreshDebugList();
        }
        
        /// <summary>
        /// 打开黑板窗口
        /// </summary>
        private void OpenBlackboard()
        {
            CloseBlackboardWin();
            if (GetOpeningBlackboardWin() == null)
            {
                blackboardWinRef.SetTarget(BTBlackboardWindow.ShowWindow(this));
            }
        }

        private BTBlackboardWindow GetOpeningBlackboardWin()
        {
            BTBlackboardWindow blackboardWin = null;
            blackboardWinRef.TryGetTarget(out blackboardWin);
            return blackboardWin;
        }

        /// <summary>
        /// 关闭黑板窗口
        /// </summary>
        private void CloseBlackboardWin()
        {
            if (GetOpeningBlackboardWin() != null)
            {
                GetOpeningBlackboardWin().Close();
                blackboardWinRef.SetTarget(null);
            }
        }

        /// <summary>
        /// 点击了刷新可调试列表按钮
        /// </summary>
        /// <param name="evt"></param>
        private void ClickRefreshDebugTargets(MouseDownEvent evt)
        {
            RefreshDebugList();
        }
        
        /// <summary>
        /// 点击返回调试父树
        /// </summary>
        private void ClickReturnParentTree()
        {
            if (!IsDebug) return;
            if (graphView.HasDebugParentTree)
            {
                graphView.DebugParentTree();
            }
        }


        /// <summary>
        /// 点击调试按钮的处理
        /// </summary>
        private void ClickToggleDebug()
        {
            //黑板窗口
            BTBlackboardWindow blackboardWin = GetOpeningBlackboardWin();
            if (null != blackboardWin)
            {
                blackboardWin.Close();
            }
            
            //如果已经在调试状态则停止调试
            if (graphView.IsDebug)
            {
                StopDebug();
            }
            else
            {
                BeginDebug();
            }
            
        }

        /// <summary>
        /// 停止调试
        /// </summary>
        private void StopDebug()
        {
            if (!IsDebug) return;
            
            graphView.StopDebug();
            btnAttachDebug.text = "调试";
            btnAttachDebug.style.color = new StyleColor(Color.white);

            //重新刷新列表
            RefreshDebugList();
            dropdownDebugField.index = dropdownDebugField.choices.Count - 1;

            if (null != DebugTarget)
            {
                //打开行为树编辑它
                OpenAsset(DebugTarget.Value.TreeInst.Asset);

                //重新打开黑板,如果有配置黑板
                OpenBlackboard();
                GetOpeningBlackboardWin().OpenAsset(DebugTarget.Value.BlackboardAsset);
            }
        }

        /// <summary>
        /// 开始调试
        /// </summary>
        private void BeginDebug()
        {
            if (graphView.IsDebug) return;
            //判断如果调试目标没有被删除就开始调试
            if (graphView.DebugTargetIsAlive())
            {
                NeedSave = false;
                editAsset = null;
                graphView.Debug();
                //将按钮状态改为在调试状态
                btnAttachDebug.text = "取消调试";
                btnAttachDebug.style.color = new StyleColor(Color.red);
                //加载黑板
                OpenBlackboard();
            }
        }
        
        /// <summary>
        /// 注册调试目标
        /// </summary>
        /// <param name="target"></param>
        private void RegisterDebugTarget(BehaviorTreeInstance treeInst)
        {
            debugTargets.Add(new BTDebugTarget(treeInst));
        }

        /// <summary>
        /// 取消注册调试目标
        /// </summary>
        /// <param name="treeInst"></param>
        private void UnRegisterDebugTarget(BehaviorTreeInstance treeInst)
        {
            if (IsDebug && graphView.DebugTarget?.TreeInst == treeInst)
            {
                StopDebug();
            }
            for (int i = 0; i < debugTargets.Count; ++i)
            {
                if (debugTargets[i].TreeInst == treeInst)
                {
                    debugTargets.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 在工具栏上新增一个|(竖杠)的显示元素
        /// </summary>
        private void AddDelimiter()
        {
            var delimiter = new DelimiterElement();
            toolbarPanel.Add(delimiter);
        }
        
        /// <summary>
        /// 选择可调试目标的回调函数
        /// </summary>
        /// <returns></returns>
        private void OnSelectedDebugTarget(ChangeEvent<string> evt)
        {
            if (debugTargets.Count <= dropdownDebugField.index || dropdownDebugField.index < 0) return;
            var item = debugTargets[dropdownDebugField.index];
             
            if (!item.IsValid)
            {
                return;
            }
            
            //如果调试目标是正常状态的则设置为调试目标
            graphView.DebugTarget = item;
        }


        /// <summary>
        /// 选择可调试目标的回调函数
        /// </summary>
        /// <param name="arg">已选择项的内容</param>
        /// <returns></returns>
        private string OnFormatSelectedDebugTarget(string arg)
        {
            if (null == dropdownDebugField || dropdownDebugField.index < 0) return arg;
            if (debugTargets.Count > dropdownDebugField.index)
            {
                var item = debugTargets[dropdownDebugField.index];
              
                //判断一些非法情况, 如果是非法状态则刷新 列表
                if (!item.IsValid)
                {
                    RefreshDebugList();
                    return "对象已删除或没运行树";
                }
            }
            else
            {
                //如果选择了非可调试目标的项时则刷新列表
                RefreshDebugList();
            }
            return arg;
        }

        /// <summary>
        /// 刷新可调试下拉框列表
        /// </summary>
        private void RefreshDebugList()
        {
            //清理可调试对象信息
            dropdownDebugField.choices.Clear();

            var btTreeComps = FindObjectsByType<BehaviorTreeComponent>(FindObjectsSortMode.None);
            foreach (var comp in btTreeComps)
            {
                if (comp.IsRunningBehaviorTree)
                {
                    var newDebugTarget = new BTDebugTarget(comp.TreeInstance);
                    if (!debugTargets.Contains(newDebugTarget))
                    {
                        debugTargets.Add(newDebugTarget);
                    }
                }
            }

            var removeList = new List<int>(debugTargets.Count);
            //将他们保存起来并且添加到下拉列表中
            for (int i = 0 ; i < debugTargets.Count; ++i)
            {
                var item = debugTargets[i];
                if (item.IsValid)
                {
                    dropdownDebugField.choices.Add(item.ToString());
                }
                else
                {
                    removeList.Add(i);
                }
            }

            for (int i = removeList.Count - 1; i >= 0; --i)
            {
                debugTargets.RemoveAt(removeList[i]);
            }
            dropdownDebugField.choices.Add("刷新");
        }


        /// <summary>
        /// 保存行为树文件
        /// </summary>
        public void Save()
        {
            //如果是在调试状态则不能保存文件
            if (graphView.IsDebug) return;
            //判断是否已经打开了一个编辑对象
            if (null == editAsset)
            {
                //如果没有则打开一个新建文件窗口
                editAsset = OpenNewFileWindow();
            }

            if (editAsset)
            {
                graphView.SaveToAsset(editAsset);
                //保存到资源数据库
                EditorUtility.SetDirty(editAsset);
                AssetDatabase.SaveAssetIfDirty(editAsset);

                NeedSave = false;
            }
        }

        
        /// <summary>
        /// 新建行为树文件
        /// </summary>
        private void NewFile()
        {
            if (IsDebug) return;
            //打开窗口新建一个文件
            var newFile = OpenNewFileWindow();
            if (null != newFile)
            {
                OpenAsset(newFile);
            }
        }

        /// <summary>
        /// 打开创建行为树窗口
        /// </summary>
        /// <returns></returns>
        private BehaviorTreeAsset OpenNewFileWindow()
        {
            var path = EditorUtility.SaveFilePanel("创建行为树", "Assets", "NewMyScriptableObject", "asset");
            if (path.Length > 0)
            {
                //创建行为树对象
                var saveFile = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
                //绝对路径转换为项目相对路径
                path = FileUtil.GetProjectRelativePath(path);
                //将文件登记在资源数据库
                AssetDatabase.CreateAsset(saveFile, path);
                //保存资源数据库并且保存为文件
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                //将创建的文件显示在编辑器的“监视面板”
                Selection.activeObject = saveFile;
                return saveFile;
            }

            return null;
        }

        /// <summary>
        /// 打开行为树文件
        /// </summary>
        private void OpenFile()
        {
            if (IsDebug) return;
            //打开窗口并且获得选择的文件路径
            string path = EditorUtility.OpenFilePanelWithFilters("选择行为树文件", "Assets",
                new string[] { "BehaviorTreeAsset", "asset" });
            //判断是否有选择文件
            if (!string.IsNullOrEmpty(path))
            {
                //绝对路径转换为项目相对路径
                path = FileUtil.GetProjectRelativePath(path);
                //传入路径从资源数据库中加载具体对象,并且转换为指定对象类型
                editAsset = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(path);
                //判断选择的文件是不是需要的类型
                if (editAsset != null)
                {
                    //成功加载则加载行为树
                    OpenAsset(editAsset);
                }
                else
                {
                    //抛出选择错误文件的提示
                    // Debug.LogError("加载行为树文件错误:" + path);
                    EditorUtility.DisplayDialog("提示", "加载行为树文件错误:" + path, "确认");
                }
            }
        }

        /// <summary>
        /// 打开BT树资源
        /// </summary>
        /// <param name="asset"></param>
        private void OpenAsset(BehaviorTreeAsset asset)
        {
            NeedSave = false;
            editAsset = asset;
            if (asset)
            {
                graphView.LoadTree(asset);
                // RecordUnDo();
                GraphViewOnSelectedEditableEvent(null);
            }
        }
        
    }
}