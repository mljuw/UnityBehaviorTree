using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    public interface IBTEditableElement
    {
        //用于RedoUndo 后找回此节点重新显示在编辑栏所用的索引信息
        public int GetNodeIndex();

        public void UnSelected();

        public void Selected();

        public BTNode GetNodeData();

        public SerializedProperty GetSerializedProp();

        public void OnCustomColorChange(Color color);

        public void OnCustomNameChange(string newName);

        public virtual Type EditInspectorClass => null;

    }

    public class ElementInspector : EditableElementInspector<BTNode>
    {
    }

    /// <summary>
    /// 支持泛型的编辑面板基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class EditableElementInspector<T> : ElementInspectorBase where T : BTNode
    {
        protected T editTarget;

        public override void Init(IBTEditableElement editableEle)
        {
            base.Init(editableEle);
            editTarget = (T)editableEle.GetNodeData();
        }

        public override void DrawUI()
        {
            DrawDefaultInspector();
        }

        #region 画默认编辑界面


        /// <summary>
        /// 画默认的编辑面板
        /// </summary>
        protected void DrawDefaultInspector()
        {
            var nodeData = ele.GetNodeData();
            var serializedProp = ele.GetSerializedProp();
            
            var fields = nodeData.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute(typeof(HideInInspector)) != null)
                {
                    continue;
                }
                var prop = serializedProp.FindPropertyRelative(field.Name);
                if (prop != null)
                {
                    var inputField = new PropertyField(prop);
                    inputField.BindProperty(prop);
                    container.Add(inputField);
                }
            }
            
        }

        #endregion
    }

    /// <summary>
    /// 编辑面板基类
    /// </summary>
    public abstract class ElementInspectorBase : VisualElement
    {
        public IBTEditableElement ele;

        protected TextField txtCustomName;
        protected ColorField customColorField;
        protected Label lblTitle;
        protected Label lblDesc;
        protected VisualElement container;
        protected VisualElement header;

        private Color DefaultDelimiterColor = new Color(25 / 255f, 25 / 255f, 25 / 255f, 25 / 255f);

        public ElementInspectorBase()
        {
            //加载编辑好的xml 并且显示在 监视面板上
            var tree = Resources.Load<VisualTreeAsset>("NodeElement/GraphNodeElementInspector");
            tree.CloneTree(this);

            //加载层叠样式添加样式表中
            var stylesheet = Resources.Load<StyleSheet>("NodeElement/GraphNodeElementInspector");
            styleSheets.Add(stylesheet);

            txtCustomName = this.Q<TextField>("txtCustomName");
            txtCustomName.RegisterCallback<ChangeEvent<string>>(OnCustomNameChange);
            
            customColorField = this.Q<ColorField>("customColorField");
            customColorField.RegisterCallback<ChangeEvent<Color>>(OnCustomColorChange);
            header = this.Q<VisualElement>("header-container");
            container = this.Q<VisualElement>("container");

            //显示节点名称
            lblTitle = this.Q<Label>("lblTitle");
            //显示描述信息
            lblDesc = this.Q<Label>("lblDesc");
            
            AddDelimiter(DefaultDelimiterColor);
        }

        public virtual void Init(IBTEditableElement editableEle)
        {
            ele = editableEle;
            
            var nodeAttr = ele.GetNodeData().GetType().GetCustomAttribute<BTNodeAttribute>();
            lblTitle.text = nodeAttr.displayName;
            lblDesc.text = nodeAttr.description;

            BTNode nodeData;
            var prop = ele.GetSerializedProp();
            txtCustomName.BindProperty(prop.FindPropertyRelative(nameof(nodeData.customName)));
            customColorField.BindProperty(prop.FindPropertyRelative(nameof(nodeData.customColor)));
        }

        protected void RecordModify()
        {
            var serializedObject = ele.GetSerializedProp().serializedObject;
            Undo.RecordObject(serializedObject.targetObject, "modify");
        }

        public virtual void DrawUI()
        {
        }
        
        protected void Repaint()
        {
            container.Clear();
            DrawUI();
        }

        private void OnCustomColorChange(ChangeEvent<Color> evt)
        {
            ele.GetNodeData().customColor = evt.newValue;
            ele.OnCustomColorChange(evt.newValue);
        }

        private void OnCustomNameChange(ChangeEvent<string> evt)
        {
            ele.GetNodeData().customName = evt.newValue;
            ele.OnCustomNameChange(evt.newValue);
        }

        /// <summary>
        /// 加一条虚线
        /// </summary>
        /// <param name="color"></param>
        /// <param name="strength"></param>
        /// <param name="widthPercent"></param>
        /// <param name="parentContainer"></param>
        protected void AddDelimiter(Color color, float strength = 2, float widthPercent = 100,
            VisualElement parentContainer = null)
        {
            var delimiter = new VisualElement
            {
                name = "delimiter",
                style =
                {
                    backgroundColor = new StyleColor(color),
                    width = new Length(widthPercent, LengthUnit.Percent),
                    height = new Length(strength, LengthUnit.Pixel)
                }
            };
            parentContainer ??= container;
            parentContainer.Add(delimiter);
        }
    }

    /// <summary>
    /// 列表字段容器
    /// </summary>
    public class ListFieldContainer : VisualElement
    {
        public ListFieldContainer()
        {
            name = "listField-container";
            style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// 指定Node的显示节点类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GraphBTNodeAttribute : Attribute
    {
        /// <summary>
        /// 对应运行时节点类型
        /// </summary>
        public Type nodeClass;

        public GraphBTNodeAttribute(Type nodeClass)
        {
            this.nodeClass = nodeClass;
        }
    }
}