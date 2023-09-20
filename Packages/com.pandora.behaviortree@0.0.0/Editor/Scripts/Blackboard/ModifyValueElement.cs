using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 修改黑板值元素
    /// </summary>
    public class ModifyValueElement : VisualElement
    {
        private SerializedProperty serializedProp;
        private Label lblTitle;
        protected VisualElement editorField;

        public ModifyValueElement(string title, IBTBlackboardField bbField,
            SerializedProperty serializedProperty = null)
        {
            serializedProp = serializedProperty;

            AddToClassList("modifyValueField");
            var attribute = bbField.GetType().GetCustomAttribute<BTBlackboardFieldAttribute>();
            if (null == attribute.editorType) return;
            editorField = (VisualElement)Activator.CreateInstance(attribute.editorType);
            
            //检查是否继承了INotifyValueChanged, 需要继承它才能获取value值
            if (null == editorField || !IsImplNotifyValueInterface(editorField))
            {
                return;
            }

            //绑定值
            if (null != serializedProp && editorField is IBindable bindable)
            {
                bindable.BindProperty(serializedProp);
            }

            lblTitle = new Label
            {
                text = title,
                pickingMode = PickingMode.Ignore
            };
            Add(lblTitle);
            Add(editorField);

            editorField.style.minWidth = new StyleLength(180);
        }

        private bool IsImplNotifyValueInterface(VisualElement ele)
        {
            var notifyValueChangeType = typeof(INotifyValueChanged<>);
            var allInterface = from x in ele.GetType().GetInterfaces()
                where x.GetGenericTypeDefinition() == notifyValueChangeType
                select x;
            return allInterface.GetEnumerator().MoveNext();
        }
    }

    /// <summary>
    /// 调试时修改黑板值元素
    /// </summary>
    public class DebugModifyValueElement : ModifyValueElement
    {
        private Button btnModify;
        private IBTBlackboardFieldInst fieldInst;
        private BTBlackboardInst bb;

        public DebugModifyValueElement(BTBlackboardInst bb, IBTBlackboardFieldInst field)
            : base("修改为", field.DefineWithInterface)
        {
            this.bb = bb;
            fieldInst = field;

            if (editorField != null)
            {
                btnModify = new Button(Modify);
                btnModify.Add(new Label("设置"));
                Add(btnModify);
            }
        }


        private void Modify()
        {
            dynamic dynEditor = editorField;
            bb.SetValue(fieldInst.FieldName, dynEditor.value);
        }
    }
}