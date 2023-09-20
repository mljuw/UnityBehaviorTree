using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    public class BTBlackboardWindow : EditorWindow
    {
        [MenuItem("Window/AI/黑板")]
        public static BTBlackboardWindow ShowWindow()
        {
            return ShowWindow(null);
        }

        public static BTBlackboardWindow ShowWindow(BehaviorTreeWindow mainWin)
        {
            BTBlackboardWindow win = GetWindow<BTBlackboardWindow>("行为树黑板");
            win.Init(mainWin);
            win.Show();
            return win;
        }

        public FieldCreateProvider fieldCreator = new();
        public BTBlackboard editTarget = null;
        private SerializedObject serializedObject;
        public BTBlackboardInst debugInst;

        private VisualElement toolbarPanel;
        private BehaviorTreeWindow treeWin;
        private ScrollView container;
        private Button btnAddField;

        private Button btnOpenFile;
        private Button btnNewFile;
        private Button btnSave;

        private bool bDebugMode => treeWin != null && treeWin.IsDebug;

        public bool IsDebug => treeWin && treeWin.IsDebug;
 

        private void OnEnable()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        private void CreateGUI()
        {
            //创建工具栏
            if (!bDebugMode)
            {
                CreateToolbar();
            }
            
            rootVisualElement.AddToClassList("bt-blackboard");
            
            //加载界面样式
            var styleSheet = Resources.Load<StyleSheet>("Window/Blackboard");
            //加载界面布局
            var visualTreeAsset = Resources.Load<VisualTreeAsset>("Window/Blackboard");

            visualTreeAsset.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(styleSheet);
            //获取显示黑板字段的容器
            container = rootVisualElement.Q<ScrollView>("blackboard-container");

            //判断是否调试模式,如果是调试影响添加字段按钮
            btnAddField = rootVisualElement.Q<Button>("btnAdd");
            
            if (!bDebugMode)
            {
                btnAddField.RegisterCallback<ClickEvent>(OnClickAdd);
            }
            else
            {
                btnAddField.visible = false;
            }
            
            if (null != editTarget)
            {
                OpenAsset(editTarget);
            }
        }

        public void Init(BehaviorTreeWindow inTreeWin)
        {
            treeWin = inTreeWin;
            //如果是调试模式则加载行为树组件上的黑板实例
            if (bDebugMode)
            {
                var debugTarget = treeWin.DebugTarget;
                if (null != debugTarget)
                {
                    editTarget = debugTarget.Value.BlackboardAsset;
                    debugInst = debugTarget.Value.Blackboard;
                    LoadBlackboard();
                }
            }
            else
            {
                editTarget = CreateInstance<BTBlackboard>();
            }
        }

        private void OnGUI()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.control && e.keyCode == KeyCode.S)
                {
                    Save();
                }
            }
        }

        public void OpenAsset(BTBlackboard asset)
        {
            editTarget = asset;
            LoadBlackboard();
        }

        /// <summary>
        /// Undo/Redo
        /// </summary>
        private void UndoRedoPerformed()
        {
            if (bDebugMode || null == editTarget) return;
            LoadBlackboard();
        }


        private void LoadBlackboard()
        {
            container.Clear();

            serializedObject = new(editTarget);
            var fieldsProp = serializedObject.FindProperty("fields");

            for (int i = 0; i < editTarget.Count; ++i)
            {
                var fieldProp = fieldsProp.GetArrayElementAtIndex(i);
                var field = editTarget[i];
                var ele = new BlackboardFieldElement(this, field, bDebugMode, fieldProp);
                ele.onModificationEvent += OnModify;
                container.Add(ele);
            }
        }

        #region 工具栏

        /// <summary>
        /// 创建工具栏
        /// </summary>
        private void CreateToolbar()
        {
            toolbarPanel = new VisualElement
            {
                name = "tool-bar",
                style =
                {
                    width = new Length(100, LengthUnit.Percent),
                    height = new Length(28, LengthUnit.Pixel),
                    backgroundColor = new StyleColor(new Color32(50, 50, 50, 255)),
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 4,
                    paddingBottom = 4,
                    display = DisplayStyle.Flex,
                }
            };
            //加载层叠样式文件
            var styleSheet = Resources.Load<StyleSheet>("Window/Toolbar");

            //添加到工具栏的样式列表中
            toolbarPanel.styleSheets.Add(styleSheet);

            //将工具栏加入到窗口中
            rootVisualElement.Add(toolbarPanel);


            //创建一些按钮
            btnOpenFile = new Button(OpenFile);
            btnOpenFile.text = "打开文件";

            btnNewFile = new Button(NewFile);
            btnNewFile.text = "新建文件";

            btnSave = new Button(Save);
            btnSave.text = "保存";

            //将按钮等操作元素添加到工具栏
            toolbarPanel.Add(btnSave);
            toolbarPanel.Add(btnNewFile);
            toolbarPanel.Add(btnOpenFile);
        }

        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            //如果是在调试状态则不能保存文件
            if (IsDebug) return;
            //判断是否已经打开了一个编辑对象
            if (null == editTarget)
            {
                //如果没有则打开一个新建文件窗口
                editTarget = OpenNewFileWindow();
            }

            //从当前黑板编辑界面转换为黑板对象结构.
            SaveToAsset(editTarget);

            //保存到资源数据库
            EditorUtility.SetDirty(editTarget);
            AssetDatabase.SaveAssetIfDirty(editTarget);
        }

        /// <summary>
        /// 新建文件
        /// </summary>
        private void NewFile()
        {
            if (IsDebug) return;
            //打开窗口新建一个文件
            var newFile = OpenNewFileWindow();
            if (null != newFile)
            {
                //加载
                OpenAsset(newFile);
            }
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        private void OpenFile()
        {
            if (IsDebug) return;
            //打开窗口并且获得选择的文件路径
            string path = EditorUtility.OpenFilePanelWithFilters("选择创建黑板文件", "Assets",
                new string[] { "ScriptableObject", "asset" });
            //判断是否有选择文件
            if (!string.IsNullOrEmpty(path))
            {
                //绝对路径转换为项目相对路径
                path = FileUtil.GetProjectRelativePath(path);
                //传入路径从资源数据库中加载具体对象,并且转换为指定对象类型
                var asset = AssetDatabase.LoadAssetAtPath<BTBlackboard>(path);
                //判断选择的文件是不是需要的类型
                if (asset != null)
                {
                    //成功加载则加载行为树
                    OpenAsset(asset);
                }
                else
                {
                    //抛出选择错误文件的提示
                    EditorUtility.DisplayDialog("提示", "加载黑板文件错误:" + path, "确认");
                }
            }
        }

        /// <summary>
        /// 打开创建黑板窗口
        /// </summary>
        /// <returns></returns>
        private BTBlackboard OpenNewFileWindow()
        {
            var path = EditorUtility.SaveFilePanel("创建黑板", "Assets", "Blackboard", "asset");
            if (path.Length > 0)
            {
                //创建黑板对象
                var saveFile = ScriptableObject.CreateInstance<BTBlackboard>();
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

        private void OnModify()
        {
            Undo.RecordObject(editTarget, "bb");
            SaveToAsset(editTarget);
            LoadBlackboard();
        }

        private void OnClickAdd(ClickEvent evt)
        {
            IBTBlackboardField field = null;
            int counter = 0;
            do
            {
                field = fieldCreator.CreateByTypeName("int32", $"new Filed{counter}");
                counter++;
            } while (!editTarget.AddField(field));

            LoadBlackboard();
        }

        public void SaveToAsset(BTBlackboard asset)
        {
            asset.ClearAllField();
            foreach (var child in container.Children())
            {
                if (child is BlackboardFieldElement fieldElement)
                {
                    var field = fieldElement.Field;
                    asset.AddField(field);
                }
            }
        }

        #endregion

        #region 点击资源文件打开

        [OnOpenAsset(2)]
        public static bool Edit(int instanceID, int line)
        {
            UnityEngine.Object obj = EditorUtility.InstanceIDToObject(instanceID);
            BTBlackboard bt = obj as BTBlackboard;
            if (bt)
            {
                var win = ShowWindow(null);
                win.OpenAsset(bt);
                return true;
            }

            return false;
        }

        #endregion
    }
}