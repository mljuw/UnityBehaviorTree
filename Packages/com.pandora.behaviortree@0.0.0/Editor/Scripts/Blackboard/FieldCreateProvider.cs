using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 创建代理者
    /// 用于创建黑板字段
    /// </summary>
    public class FieldCreateProvider
    {
        private Dictionary<string, Type> dictNameToType = new();
        
        public FieldCreateProvider()
        {
            FetchAllFields();
        }

        private void FetchAllFields()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                FetchFieldsFromAssembly(assembly);
            }
        }

        private void FetchFieldsFromAssembly(Assembly assembly)
        {
            //从程序集中查找实现了BTBlackboardField的所有类类型
            var types = 
                from type in TypeCache.GetTypesDerivedFrom(typeof(IBTBlackboardField))
                where !type.IsAbstract
                select type;
            
            foreach (var t in types)
            {
                dictNameToType[GetTypeDisplayName(t)] = t;
            }
        }

        /// <summary>
        /// 获取字段类型的显示名字
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private string GetTypeDisplayName(Type t)
        {
            var attr = t.GetCustomAttribute<BTBlackboardFieldAttribute>();
            if (null != attr)
            {
                return attr.typeName;
            }

            if (t.BaseType == null)
            {
                return t.Name;
            }
            if (t.BaseType.IsGenericType)
            {
                var realTypeName = t.BaseType.GetGenericArguments()[0];
                return realTypeName.Name;
            }
            return t.Name;
        }

        /// <summary>
        /// 获取支持的类型列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetSupportTypes()
        {
            return dictNameToType.Keys.ToList();
        }

        /// <summary>
        /// 根据类型名字创建黑板字段
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public IBTBlackboardField CreateByTypeName(string typeName, string fieldName)
        {
            dictNameToType.TryGetValue(typeName, out var fieldType);
            if (fieldType == null) return null;
            var field = (IBTBlackboardField)Activator.CreateInstance(fieldType);
            field.SetFieldName(fieldName);
            return field;
        }
    }
} 