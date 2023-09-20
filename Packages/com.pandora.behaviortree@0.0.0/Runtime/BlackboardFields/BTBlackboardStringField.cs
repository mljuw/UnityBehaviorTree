using System;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace Pandora.BehaviorTree
{
    [Serializable]
    public partial class BTBlackboardStringField : BTBlackboardField<string>
    {
        public override Type FieldRuntimeType => typeof(BTFloatStringInst);
    }

    public class BTFloatStringInst : BTBlackboardFieldInst<string>
    {
        public BTFloatStringInst(BTBlackboardField<string> define) : base(define)
        {
        }
    }
    
#if UNITY_EDITOR
    
    [BTBlackboardField("string", typeof(TextField))]
    public partial class BTBlackboardStringField
    {
    }


#endif
    
}