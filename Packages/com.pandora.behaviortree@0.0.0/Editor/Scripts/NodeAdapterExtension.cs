using UnityEditor.Experimental.GraphView;

namespace Pandora.BehaviorTree
{
    public static class NodeAdapterExtension
    {
        /// <summary>
        /// 定义支持链接节点的类型
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="inPort"></param>
        /// <param name="outPort"></param>
        /// <returns></returns>
        public static bool CheckConnectNodeNode(this NodeAdapter adapter, PortSource<BTGraphNode> inPort,
            PortSource<BTGraphNode> outPort)
        {
            return true;
        }
        
        public static bool CheckConnectNodeNode(this NodeAdapter adapter, PortSource<BTLeafGraphNode> inPort,
            PortSource<BTGraphNode> outPort)
        {
            return true;
        }

        public static bool CheckConnectNodeNode(this NodeAdapter adapter, PortSource<BTLeafGraphNode> inPort,
            PortSource<BTParallelGraphNode> outPort)
        {
            return true;
        }
    }
}