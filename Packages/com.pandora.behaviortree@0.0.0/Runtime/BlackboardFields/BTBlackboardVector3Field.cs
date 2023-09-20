
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace Pandora.BehaviorTree
{
    
    [Serializable]
    public partial class BTBlackboardVector3Field : BTBlackboardField<Vector3>
    {
        public override Type FieldRuntimeType => typeof(BTFloatVector3Inst);
    }

    public class BTFloatVector3Inst : BTBlackboardFieldInst<Vector3>
    {
        public BTFloatVector3Inst(BTBlackboardField<Vector3> define) : base(define)
        {
        }
    }
    
#if UNITY_EDITOR
    
    [BTBlackboardField("vector3", typeof(Vector3Field))]
    public partial class BTBlackboardVector3Field
    {
    }

#endif
    
}