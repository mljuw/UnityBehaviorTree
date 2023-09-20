using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Pandora.BehaviorTree
{

    public class BTBlackboardInst : IEnumerable<IBTBlackboardFieldInst>
    {
        private Dictionary<int, IBTBlackboardFieldInst> fieldsWithName = new();

        private BTBlackboard blackboardAsset;

        public BTBlackboard BlackboardAsset => blackboardAsset;

        /// <summary>
        /// 参数： 字段、旧值、新值
        /// </summary>
        public event Action<IBTBlackboardFieldInst> valueChangeEvent;

        public BTBlackboardInst(BTBlackboard asset)
        {
            blackboardAsset = asset;
            foreach (var fieldDef in asset)
            {
                var field = Activator.CreateInstance(fieldDef.FieldRuntimeType, new object[]{fieldDef}) as IBTBlackboardFieldInst;
                var hashCode = GetFieldNameHashCode(field.FieldName);
                fieldsWithName.Add(hashCode, field);
            }
        }
        
        
        /// <summary>
        /// TODO: 有可能出现Hash code 相同情况
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetFieldNameHashCode(string fieldName)
        {
            return fieldName.GetHashCode(StringComparison.CurrentCulture);
        }

        private IBTBlackboardFieldInst UpdateFieldDict(int hashCode)
        {
            if (fieldsWithName.TryGetValue(hashCode, out var ret))
            {
                return ret;
            }
            foreach (var field in fieldsWithName.Values)
            {
                if (GetFieldNameHashCode(field.FieldName) == hashCode)
                {
                    fieldsWithName.Add(hashCode, field);
                    return field;
                }
            }
            return null;
        }

        public BTBlackboardFieldInst<TValue> GetField<TValue>(string fieldName)
        {
            return GetFieldByHasCode<TValue>(GetFieldNameHashCode(fieldName));
        }

        /// <summary>
        /// 获取黑板字段
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public IBTBlackboardFieldInst GetField(string fieldName)
        {
            return UpdateFieldDict(GetFieldNameHashCode(fieldName));
        }

        /// <summary>
        /// 获取黑板值
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="defaultVal"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>(string fieldName, T defaultVal)
        {
            var field = GetField<T>(fieldName);
            if (null == field) return defaultVal;
            return field.Value;
        }


        /// <summary>
        /// 为黑板设置值
        /// </summary>
        public void SetValue<TValue>(string fieldName, TValue value)
        {
            var field = GetField<TValue>(fieldName);
            if (null == field) return;
            field.SetValue(value);
            valueChangeEvent?.Invoke(field);
        }
        
        public BTBlackboardFieldInst<TValue> GetFieldByHasCode<TValue>(int hashCode)
        { 
            IBTBlackboardFieldInst field = UpdateFieldDict(hashCode);
            return (BTBlackboardFieldInst<TValue>)(field);
        }


        public IEnumerator<IBTBlackboardFieldInst> GetEnumerator()
        {
            return fieldsWithName.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    
    [CreateAssetMenu(menuName = "Developer/行为树/Blackboard", fileName = "blackboard")]
    public class BTBlackboard : ScriptableObject, IEnumerable<IBTBlackboardField>
    {
        [SerializeReference]
        private List<IBTBlackboardField> fields = new();

        public int Count => fields.Count;

        public void ClearAllField()
        {
            fields.Clear();
        }

        public bool AddField(IBTBlackboardField field)
        {
            if (CheckFieldNameExists(field.FieldName)) return false;
            fields.Add(field);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckFieldNameExists(string fieldName)
        {
            foreach (var field in fields)
            {
                if (field.FieldName == fieldName)
                {
                    return true;
                }
            }

            return false;
        }
        
        public IBTBlackboardField this[int index] => fields[index];

        public IEnumerator<IBTBlackboardField> GetEnumerator()
        {
            return fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            bool bFirst = true;

            foreach (var field in fields)
            {
                if (!bFirst)
                {
                    sb.Append("\n");
                }
                sb.Append(field);
                bFirst = false;
            }

            return sb.ToString();
        }

    }
    
    
}