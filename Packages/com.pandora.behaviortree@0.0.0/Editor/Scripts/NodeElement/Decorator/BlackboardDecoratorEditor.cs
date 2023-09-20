using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    [GraphBTNode(typeof(BlackboardDecorator))]
    public class BlackboardDecoratorElement : DecoratorNodeElement
    {
        public override Type EditInspectorClass => typeof(BlackboardDecoratorInspector);
    }
    
    public class BlackboardDecoratorInspector : EditableElementInspector<BlackboardDecorator>
    {
        private TextField txtBlackboardKey;
        private EnumField dpFieldType;
        private EnumField dpCompareOpt;
        private IntegerField txtInt;
        private FloatField txtFloat;
        private TextField txtStr;
        
        public override void DrawUI()
        {
            base.DrawUI();
            var prop = ele.GetSerializedProp();
            
            dpFieldType = new EnumField("字段类型");
            dpFieldType.BindProperty(prop.FindPropertyRelative(nameof(editTarget.fieldComparableType)));
            dpFieldType.RegisterValueChangedCallback(FieldTypeChange);
            container.Add(dpFieldType);
            

            dpCompareOpt = new EnumField("值比较");
            dpCompareOpt.BindProperty(
                prop.FindPropertyRelative(nameof(editTarget.condition))
                .FindPropertyRelative(nameof(editTarget.condition.opt))
                );
            container.Add(dpCompareOpt);

            UpdateValueInput();
        }

        private void FieldTypeChange(ChangeEvent<Enum> changeEvent)
        {
            UpdateValueInput();
        }

        private void UpdateValueInput()
        {
            if (txtInt != null)
            {
                txtInt.RemoveFromHierarchy();
                txtInt = null;
            }
            if (txtFloat != null)
            {
                txtFloat.RemoveFromHierarchy();
                txtFloat = null;
            }
            if (txtStr != null)
            {
                txtStr.RemoveFromHierarchy();
                txtStr = null;
            }


            switch (editTarget.fieldComparableType)
            {
                case BCFieldComparableType.Vector2:
                case BCFieldComparableType.Vector3:
                case BCFieldComparableType.Float:
                    txtFloat = new FloatField("比较值");
                    txtFloat.RegisterCallback<ChangeEvent<float>>( TxtFloatChange);
                    txtFloat.value = editTarget.condition.compareFloatValue;
                    container.Add(txtFloat);
                    break;
                case BCFieldComparableType.Int:
                    txtInt = new IntegerField("比较值");
                    txtInt.RegisterCallback<ChangeEvent<int>>( TxtIntChange);
                    txtInt.value = editTarget.condition.compareIntValue;
                    container.Add(txtInt);
                    break;
                case BCFieldComparableType.String:
                    txtStr = new TextField("比较值");
                    txtStr.RegisterCallback<ChangeEvent<string>>( TxtStrChange);
                    txtStr.value = editTarget.condition.compareStrValue;
                    container.Add(txtStr);
                    break;
            }
        }

        private void TxtStrChange(ChangeEvent<string> evt)
        {
            editTarget.condition.compareStrValue = evt.newValue;
        }

        private void TxtFloatChange(ChangeEvent<float> evt)
        {
            editTarget.condition.compareFloatValue = evt.newValue;
        }

        private void TxtIntChange(ChangeEvent<int> evt)
        {
            editTarget.condition.compareIntValue = evt.newValue;
        }
 
    }
}