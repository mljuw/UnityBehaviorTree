
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

namespace Pandora.BehaviorTree
{

    [Serializable]
    public partial class BTBlackboardVector2Field : BTBlackboardField<Vector2>
    {
        public override Type FieldRuntimeType => typeof(BTFloatVector2Inst);
    }

    public class BTFloatVector2Inst : BTBlackboardFieldInst<Vector2>
    {
        public BTFloatVector2Inst(BTBlackboardField<Vector2> define) : base(define)
        {
        }
    }
    
#if UNITY_EDITOR
    
    [BTBlackboardField("vector2", typeof(Vector2Field))]
    public partial class BTBlackboardVector2Field
    {
    }

#endif
    
}