using System;
using System.Diagnostics;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 标识字段的自定义名称、编辑器类型
    /// </summary>
    /// <remarks>只用于编辑器</remarks>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class)]
    public class BTBlackboardFieldAttribute : Attribute
    {
        /// <summary>
        /// 显示字段类型的自定义名字
        /// </summary>
        public string typeName;

        /// <summary>
        /// 编辑字段的编辑器类型
        /// </summary>
        /// <remarks>类型需要继承 INotifyValueChange, IBindable</remarks>
        public Type editorType;

        public BTBlackboardFieldAttribute(string typeName, Type editorType = null)
        {
            this.typeName = typeName;
            this.editorType = editorType;
        }
    }

    public interface IBTBlackboardFieldInst
    {
        string FieldName => String.Empty;

        IBTBlackboardField DefineWithInterface => null;

        Type FieldType => null;

        bool IsSetValue => false;
    }

    public class BTBlackboardFieldInst<TValue> : IBTBlackboardFieldInst
    {
        private BTBlackboardField<TValue> def;
        public BTBlackboardField<TValue> Define => def;
        
        public IBTBlackboardField DefineWithInterface => def;
        
        
        private TValue data;

        public string FieldName => def.FieldName;

        public TValue Value
        {
            get => IsSetValue? data : Define.defaultVal;
            set
            {
                data = value;
                isSetValue = true;
            } 
        }

        private bool isSetValue;

        public bool IsSetValue => isSetValue;
        
        public Type FieldType => typeof(TValue);

        public BTBlackboardFieldInst(BTBlackboardField<TValue> define)
        {
            def = define;
        }
        
        public bool SetValue(TValue newValue)
        {
            if (IsSetValue && data.Equals(newValue)) return false;
            data = newValue;
            isSetValue = true;
            return true;
        }

    }


    public interface IBTBlackboardField
    {
        string FieldName 
        { 
            get;
        }


        /// <summary>
        /// 设置名字
        /// </summary>
        /// <param name="newName"></param>
        /// <remarks>只用于编辑器下有效</remarks>
        void SetFieldName(string newName);

        /// <summary>
        /// 定义运行时类型
        /// </summary>
        Type FieldRuntimeType => null;
        
    }

    /// <summary>
    /// 黑板字段基类
    /// </summary>
    [Serializable]
    public abstract class BTBlackboardField<TValue> : IBTBlackboardField
    {
        /// <summary>
        /// 黑板字段名字
        /// </summary>
        [SerializeField] 
        private string fieldName;

        public string FieldName => fieldName;

        public TValue defaultVal;

        
        /// <summary>
        /// 设置名字
        /// </summary>
        /// <param name="newName"></param>
        /// <remarks>只用于编辑器下有效</remarks>
        public void SetFieldName(string newName)
        {
            fieldName = newName;
        }

        public virtual Type FieldRuntimeType => null;
        
    }
}