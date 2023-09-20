using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 编辑黑板字段元素
    /// </summary>
    public class BlackboardFieldElement : VisualElement
    {
        public event Action onModificationEvent;

        private BTBlackboardWindow window;
        private BTBlackboard blackboard;
        private SerializedProperty serializedProp;
        private IBTBlackboardField field;
        
        private List<string> supportTypes;
        private VisualElement container;
        private VisualElement modifyContainer;
        private VisualElement defValContainer;
        private ModifyValueElement defaultValField;
        private Button btnDel;
        private TextField txtParamName;
        private Label lblValue;
        private Label lblValueTitle;
        private DropdownField ddlType;
        
        private bool IsDebugMode => window && window.IsDebug;

        public IBTBlackboardField Field => field;

        public BlackboardFieldElement(BTBlackboardWindow window, IBTBlackboardField inField, bool inDebugMode,
            SerializedProperty serializedProp = null)
        {
            this.window = window;
            this.serializedProp = serializedProp;

            supportTypes = window.fieldCreator.GetSupportTypes();

            //有可能为空，编辑模式下在没有打开行为树文件时会为空， 如果是debug模式则一定不会为空
            blackboard = window.editTarget;
            field = inField ?? window.fieldCreator.CreateByTypeName("int32", "");

            var visualTreeAsset = Resources.Load<VisualTreeAsset>("Window/BlackboardField");

            visualTreeAsset.CloneTree(this);
            container = this.Q<VisualElement>("container");
            modifyContainer = this.Q<VisualElement>("modify-container");
            defValContainer = this.Q<VisualElement>("defaultValue-container");

            //删除按钮
            btnDel = this.Q<Button>("btnDel");
            btnDel.RegisterCallback<ClickEvent>(OnClickDelete);

            //黑板值名称
            txtParamName = this.Q<TextField>("txtParamName");

            //黑板值
            lblValue = this.Q<Label>("lblValue");
            lblValueTitle = this.Q<Label>("lblValueTitle");

            //黑板值类型
            ddlType = this.Q<DropdownField>("ddlType");
            ddlType.choices = supportTypes;
            ddlType.RegisterValueChangedCallback(OnFieldTypeChange);


            //绑定属性
            if (null != serializedProp)
            {
                txtParamName.BindProperty(serializedProp.FindPropertyRelative("fieldName"));
            }


            var attr = field.GetType().GetCustomAttribute<BTBlackboardFieldAttribute>();

            var typeName = attr.typeName;
            ddlType.index = supportTypes.IndexOf(typeName);
            container.AddToClassList(typeName);

            //如果是Debug模式则需要绑定值改变的事件
            if (IsDebugMode)
            {
                modifyContainer.visible = true;
                window.debugInst.valueChangeEvent += BlackboardOnValueChangeEvent;

                var fieldIns = window.debugInst.GetField(field.FieldName);
                dynamic dyField = fieldIns;
                var value = dyField.Value;
                var valStr = value != null ? value.ToString() : string.Empty;

                //显示黑板值
                lblValue.text = $"当前值:{valStr}";
                lblValueTitle.visible = true;

                //调试模式可修改它的值
                var debugModifyElement = new DebugModifyValueElement(window.debugInst, fieldIns);
                modifyContainer.Add(debugModifyElement);

                //隐藏删除字段按钮
                btnDel.visible = false;
                btnDel.style.position = Position.Absolute;

                ddlType.SetEnabled(false);
                txtParamName.SetEnabled(false);
            }
            else
            {
                //显示默认值
                ShowDefaultValueElement();
                modifyContainer.visible = false;
            }

            //注册删除事件
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromParent);
        }

        /// <summary>
        /// 显示默认值
        /// </summary>
        private void ShowDefaultValueElement()
        {
            SerializedProperty defValProp = serializedProp?.FindPropertyRelative("defaultVal");

            defValContainer.Clear();
            defaultValField = new ModifyValueElement("默认值", field, defValProp);
            defValContainer.Add(defaultValField);
        }

        private void OnDetachFromParent(DetachFromPanelEvent evt)
        {
            //在被删除时需要取消绑定属性值改变事件
            if (IsDebugMode)
            {
                window.debugInst.valueChangeEvent -= BlackboardOnValueChangeEvent;
            }
        }

        /// <summary>
        /// 当值发生修改时更新界面显示
        /// </summary>
        /// <param name="instFieldInst">字段</param>
        private void BlackboardOnValueChangeEvent(IBTBlackboardFieldInst instFieldInst)
        {
            if (instFieldInst.FieldName == field.FieldName)
            {
                dynamic dyField = instFieldInst;
                lblValue.text = $"当前值:{dyField.Value}";
            }
        }

        /// <summary>
        /// 类型改变时的回调
        /// </summary>
        /// <param name="evt"></param>
        private void OnFieldTypeChange(ChangeEvent<string> evt)
        {
            container.RemoveFromClassList(evt.previousValue);
            container.AddToClassList(evt.newValue);
            
            field = window.fieldCreator.CreateByTypeName(evt.newValue, field.FieldName);

            ShowDefaultValueElement();
            
            onModificationEvent?.Invoke();
        }

        /// <summary>
        /// 点击删除按钮
        /// </summary>
        /// <param name="evt"></param>
        private void OnClickDelete(ClickEvent evt)
        {
            //弹出提示
            string msg = $"确定删除该字段吗？";
            if (EditorUtility.DisplayDialog("删除", msg, "是", "否"))
            {
                RemoveFromHierarchy();
                onModificationEvent?.Invoke();
            }
        }
    }
}