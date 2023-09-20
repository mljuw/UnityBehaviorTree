using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pandora.BehaviorTree
{
    public class BTNodePort : Port
    {
        public static BTNodePort CreateBTPort<TEdge>(
            BTGraphNode owner,
            Orientation portOrientation, 
            Direction portDirection,
            Capacity portCapacity,
            Type type) where TEdge : Edge, new()
        {
            EdgeConnectorListener listener = new EdgeConnectorListener();
            BTNodePort ele = new BTNodePort(portOrientation, portDirection, portCapacity, type ?? owner.GetType())
            {
                m_EdgeConnector = new EdgeConnector<TEdge>((IEdgeConnectorListener)listener),
                portColor = new Color32(120, 80, 255, 255)
            };
            ele.owner = owner;
            ele.AddManipulator(ele.m_EdgeConnector);
            return ele;
        }

        private BTGraphNode owner;

        public BTGraphNode Owner => owner;

        public event Action DisconnectedEvent;
        public event Action<BTNodePort> ConnectedEvent;

        private BTNodePort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type)
            : base(portOrientation, portDirection, portCapacity, type)
        {
            //隐藏节点类型
            var lblType = this.Q<Label>("type");
            lblType.text = "";
            lblType.visible = false;
        }
    
    
        private class EdgeConnectorListener : IEdgeConnectorListener
        {
            private readonly GraphViewChange _mGraphViewChange;
            private readonly List<Edge> _mEdgesToCreate;
            private readonly List<GraphElement> _mEdgesToDelete;
    
            public EdgeConnectorListener()
            {
                _mEdgesToCreate = new List<Edge>();
                _mEdgesToDelete = new List<GraphElement>();
                _mGraphViewChange.edgesToCreate = _mEdgesToCreate;
            }
    
            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                var inputConnected = edge.input?.connected ?? false;
                var outputConnected = edge.output?.connected ?? false;
                
                if (inputConnected && outputConnected)
                {
                    edge.input.Disconnect(edge);
                    edge.output.Disconnect(edge);
                    if (edge.input is BTNodePort input)
                    {
                        input.DisconnectedEvent?.Invoke();    
                    }

                    if (edge.output is BTNodePort output)
                    {
                        output.DisconnectedEvent?.Invoke();
                    }
                }
            }
    
            public void OnDrop(GraphView graphView, Edge edge)
            {
                _mEdgesToCreate.Clear();
                _mEdgesToCreate.Add(edge);
                _mEdgesToDelete.Clear();
                if (edge.input.capacity == Port.Capacity.Single)
                {
                    foreach (Edge connection in edge.input.connections)
                    {
                        if (connection != edge)
                            _mEdgesToDelete.Add(connection);
                    }
                }
    
                if (edge.output.capacity == Port.Capacity.Single)
                {
                    foreach (Edge connection in edge.output.connections)
                    {
                        if (connection != edge)
                            _mEdgesToDelete.Add((GraphElement)connection);
                    }
                }

                if (_mEdgesToDelete.Count > 0)
                {
                    graphView.DeleteElements(_mEdgesToDelete);
                }

                List<Edge> edgesToCreate = _mEdgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged(_mGraphViewChange).edgesToCreate;
                }

                foreach (Edge edge1 in edgesToCreate)
                {
                    graphView.AddElement(edge1);
                    edge.input.Connect(edge1);
                    edge.output.Connect(edge1);
                    
                    if (edge.input is BTNodePort input && edge.output is BTNodePort output)
                    {
                        input.ConnectedEvent?.Invoke(output);
                        output.ConnectedEvent?.Invoke(input);
                    }
 
                }
            }
        }
    }
}