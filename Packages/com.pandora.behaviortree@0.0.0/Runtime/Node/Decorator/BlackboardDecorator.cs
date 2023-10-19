using System;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    public enum BCValueCompareOpt
    {
        //不开启
        IsSetValue,
        //等于
        Equal,
        //不等于
        NotEqual,
        //小于
        Less,
        //小等于
        LessEqual,
        //大于
        Greater,
        //大等于
        GreaterEqual,
    }

    [Serializable]
    public enum BCFieldComparableType
    {
        Int,
        Float,
        String,
        Vector3,
        Vector2,
    }
    
    [Serializable]
    public struct BlackboardCondition
    {
        public BCValueCompareOpt opt;
        public string compareStrValue;
        public int compareIntValue;
        public float compareFloatValue;
    }
    
    [BTNode("黑板装饰器", "装饰器", description = "用于判断这个节点能否执行的条件")]
    public class BlackboardDecorator : BTDecoratorNode
    {
        [Header("黑板字段名")]
        public string blackboardKey;

        [Header("字段类型"), HideInInspector]
        public BCFieldComparableType fieldComparableType;

        [Header("比较操作"), HideInInspector]
        public BlackboardCondition condition;

        public override Type GetInstanceClass()
        {
            return typeof(BlackboardDecoratorInst);
        }
    }

    public class BlackboardDecoratorInst : DecoratorNodeInst<BlackboardDecorator>
    {
        private IBTBlackboardFieldInst _fieldInst = null;
        private bool checkPass = false;

        public override void Dispose()
        {
            Cleanup();
            base.Dispose();
        }

        protected void Cleanup()
        {
            checkPass = false;
            fieldInst = null;
            treeInst.Blackboard.valueChangeEvent -= BlackboardOnValueChangeEvent;
        }

        private IBTBlackboardFieldInst GetField()
        {
            if (null != _fieldInst) return _fieldInst;
            _fieldInst = treeInst.Blackboard.GetField(Define.blackboardKey);
            return _fieldInst;
        }
        
        protected override void OnBecomeRelevant()
        {
            base.OnBecomeRelevant();
            _fieldInst = GetField();
            treeInst.Blackboard.valueChangeEvent += BlackboardOnValueChangeEvent;
        }

        protected override void OnCeaseRelevant()
        {
            base.OnCeaseRelevant();
            Cleanup();
        }

        private void BlackboardOnValueChangeEvent(IBTBlackboardFieldInst changedFieldInst)
        {
            if (_fieldInst != null)
            {
                if (_fieldInst == changedFieldInst)
                {
                    checkPass = Compare();
                }
            }
        }

        private bool Compare()
        {
            if (Define.condition.opt == BCValueCompareOpt.IsSetValue)
            {
                return _fieldInst.IsSetValue;
            }
            int compareVal = 0;
            if (_fieldInst.FieldType == typeof(string) )
            {
                compareVal = String.Compare(((BTBlackboardFieldInst<string>)_fieldInst).Value, Define.condition.compareStrValue, StringComparison.Ordinal);
            }
            else if (_fieldInst.FieldType == typeof(object))
            {
                //普通obj 不支持比较
                return false;
            }
            else if (_fieldInst.FieldType == typeof(Vector3))
            {
                compareVal = ((BTBlackboardFieldInst<Vector3>)_fieldInst).Value.magnitude.CompareTo(Define.condition.compareFloatValue);
            }
            else if (_fieldInst.FieldType == typeof(Vector2))
            {
                compareVal = ((BTBlackboardFieldInst<Vector2>)_fieldInst).Value.magnitude.CompareTo(Define.condition.compareFloatValue);
            }
            else if (_fieldInst.FieldType == typeof(int))
            {
                compareVal =  ((BTBlackboardFieldInst<int>)_fieldInst).Value.CompareTo(Define.condition.compareIntValue);
            }
            else if (_fieldInst.FieldType == typeof(float))
            {
                compareVal = ((BTBlackboardFieldInst<float>)_fieldInst).Value.CompareTo(Define.condition.compareFloatValue);
            }

            return Compare(compareVal);
        }

        private bool Compare(int compareVal)
        {
            
            switch (Define.condition.opt)
            {
                case BCValueCompareOpt.Equal:
                    return compareVal == 0;
                    
                case BCValueCompareOpt.NotEqual:
                    return compareVal != 0;
                case BCValueCompareOpt.Greater:
                    return compareVal > 0;
                case BCValueCompareOpt.GreaterEqual:
                    return compareVal >= 0;
                case BCValueCompareOpt.Less:
                    return compareVal < 0;
                case BCValueCompareOpt.LessEqual:
                    return compareVal <= 0;
            }

            return false;
        }

        protected override bool RawConditionCheck()
        {
            if (GetField() == null) return false;
            
            if (Define.condition.opt == BCValueCompareOpt.IsSetValue)
            {
                return _fieldInst.IsSetValue;
            }

            checkPass = Compare();
            return checkPass;
        }
        
        protected override bool PerformConditionCheck()
        {
            return checkPass;
        }
    }
}