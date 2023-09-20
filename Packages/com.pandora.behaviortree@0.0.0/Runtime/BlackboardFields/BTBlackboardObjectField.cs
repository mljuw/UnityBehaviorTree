using System;
using UnityEngine.UIElements;


namespace Pandora.BehaviorTree
{

    [Serializable]
    public partial class BTBlackboardObjectField : BTBlackboardField<object>
    {
        public override Type FieldRuntimeType => typeof(BTFloatObjectInst);
    }

    public class BTFloatObjectInst : BTBlackboardFieldInst<object>
    {
        public BTFloatObjectInst(BTBlackboardField<object> define) : base(define)
        {
        }
    }
    
#if UNITY_EDITOR
    
    [BTBlackboardField("object")]
    public partial class BTBlackboardObjectField
    {
    }
    
#endif
    
}