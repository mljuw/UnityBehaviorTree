using System;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace Pandora.BehaviorTree
{
    
    [Serializable]
    public partial class BTBlackboardFloatField : BTBlackboardField<float>
    {
        public override Type FieldRuntimeType => typeof(BTFloatFieldInst);
    }

    public class BTFloatFieldInst : BTBlackboardFieldInst<float>
    {
        public BTFloatFieldInst(BTBlackboardField<float> define) : base(define)
        {
        }
    }

#if UNITY_EDITOR
    
    [BTBlackboardField("float", typeof(FloatField))]
    public partial class BTBlackboardFloatField
    {
    }
    
#endif
    
}