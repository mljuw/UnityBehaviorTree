using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 权重选择器 
    /// </summary>
    [GraphBTNode( typeof(BTWeightSelectorNode))]
    public class WeightSelectorGraphNode : SelectorGraphNode, IBTEditableElement
    {
        public override Type EditInspectorClass
        {
            get => typeof(WeightSelectorInspector);
        }
    }

    /// <summary>
    /// 监视面板
    /// </summary>
    public class WeightSelectorInspector : EditableElementInspector<BTWeightSelectorNode>
    {
        private List<NodeWeightField> weightFields = new();
        private VisualElement weightContainer;
        
        public override void DrawUI()
        {
            weightContainer = new VisualElement();
            container.Add(weightContainer);
            FillWeightFields();
        }
        
        private void FillWeightFields()
        {
            weightContainer.Clear();
            weightFields.Clear();
            int totalPercent = 0;
            for (int i = 0; i < editTarget.childrenWeight.Count; ++i)
            {
                var item = editTarget.childrenWeight[i];
                totalPercent += item.weight;
                item.randomNum = totalPercent;
                var field = new NodeWeightField($"节点{i}权重", item.weight, item.randomNum, i);
                field.onDelete += FieldOnDelete;
                field.onModify += FieldOnModify;
                weightFields.Add(field);
                weightContainer.Add(field);
                editTarget.childrenWeight[i] = item;
            }

            var btnAdd = new Button()
            {
                text = "添加权重配置"
            };
            btnAdd.clicked += BtnAddOnClicked;
            weightContainer.Add(btnAdd);
        }

        private void BtnAddOnClicked()
        {
            RecordModify();
            editTarget.childrenWeight.Add(default);
            FillWeightFields();
        }

        private void FieldOnModify(object userData, int weight)
        {
            RecordModify();
            int index = (int)userData;
            //更新权重值
            int totalPercent = 0;
            for (int i = 0; i < editTarget.childrenWeight.Count; ++i)
            {
                var item = editTarget.childrenWeight[i];
                if (i == index)
                {
                    item.weight = weight;
                }
                
                totalPercent += item.weight;
                item.randomNum = totalPercent;
                editTarget.childrenWeight[i] = item;

                var field = weightFields[i];
                field.SetRandomNum(item.randomNum);
            }
        }

        private void FieldOnDelete(object obj)
        {
            RecordModify();
            int removeIdx = (int)obj;
            editTarget.childrenWeight.RemoveAt(removeIdx);
            weightFields.RemoveAt(removeIdx);
            FillWeightFields();
        }
        
        /// <summary>
        /// 节点权重编辑元素
        /// </summary>
        public class NodeWeightField : VisualElement
        {
            /// <summary>
            /// 点击删除按钮事件
            /// object:userData
            /// </summary>
            public event Action<object> onDelete;
            /// <summary>
            /// 修改权重事件
            /// object:userData
            /// float:权重
            /// </summary>
            public event Action<object, int> onModify;
            private object customData;
            private IntegerField weightField;
            private IntegerField randomNumField;
            private Button btnDel;

            public NodeWeightField(string title, int weight, int randomNum, object customData)
            {
                this.customData = customData;
                weightField = new IntegerField()
                {
                    label = title,
                    value = weight,
                };
                weightField.name = "WeightField";
                weightField.RegisterCallback<ChangeEvent<int>>(OnWeightChange);
                Add(weightField);

                randomNumField = new IntegerField()
                {
                    label = "随机值",
                    value = randomNum,
                };
                randomNumField.SetEnabled(false);
                Add(randomNumField);

                btnDel = new Button()
                {
                    text = "删除"
                };
                btnDel.clicked += OnDelete;
                Add(btnDel);
            }

            public void SetRandomNum(int randNum)
            {
                randomNumField.value = randNum;
            }

            private void OnWeightChange(ChangeEvent<int> evt)
            {
                int weight = Math.Clamp(evt.newValue, 0, 100);
                onModify?.Invoke(customData, weight);
            }

            private void OnDelete()
            {
                onDelete?.Invoke(customData);
            }
        }
    }
}