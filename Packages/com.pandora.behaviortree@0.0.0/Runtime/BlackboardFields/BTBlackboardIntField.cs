using System;

#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace Pandora.BehaviorTree
{
    [Serializable]
    public partial class BTBlackboardIntField : BTBlackboardField<int>
    {
        public override Type FieldRuntimeType => typeof(BTFloatIntInst);
    }

    public class BTFloatIntInst : BTBlackboardFieldInst<int>
    {
        public BTFloatIntInst(BTBlackboardField<int> define) : base(define)
        {
        }
    }
    
    
#if UNITY_EDITOR
    
    [BTBlackboardField("int32", typeof(IntegerField))]
    public partial class BTBlackboardIntField
    {
    }
#endif
    
}