using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terrain.Environment;
using Terrain.Utility;
using Microsoft.Xna.Framework.Graphics;

namespace Terrain.QuadTree
{
    public class QuadTreeNode
    {
        #region Constants

        /// <summary>
        /// Must be odd number and > 1
        /// </summary>
        public const int VerticesPerEdge = 5;

        private const float EdgeLengthLimit = 10;
        private const float SplitDistanceMultiplier = 4;
        private const float NormalSampleOffset = 1;
        
        #endregion

        #region Fields

        private readonly QuadTreeNode _parentNode;

        private readonly float _edgeLength, _halfEdgeLength;

        private readonly NodeType _nodeType;
        
        private bool _hasChildren;

        #endregion

        #region Properties

        #region Child nodes

        public QuadTreeNode ChildNorthWest { get; private set; }

        public QuadTreeNode ChildNorthEast { get; private set; }

        public QuadTreeNode ChildSouthEast { get; private set; }

        public QuadTreeNode ChildSouthWest { get; private set; }

        #endregion

        #region Neighbour nodes

        public QuadTreeNode NeighbourNorth { get; private set; }

        public QuadTreeNode NeighbourEast { get; private set; }

        public QuadTreeNode NeighbourSouth { get; private set; }

        public QuadTreeNode NeighbourWest { get; private set; }

        #endregion

        public Vector3 CentrePoint { get; private set; }
        
        public float QuarterEdgeLength { get; private set; }

        public VertexBuffer VertexBuffer { get; private set; }

        public BoundingBox BoundingBox { get; private set; }

        public IndexBufferSelection ActiveIndexBuffer { get; private set; }

        #endregion

        #region Constructors

        public QuadTreeNode(NodeType nodeType, QuadTreeNode parentNode, float centreX, float centreZ, float edgeLength, GraphicsDevice graphicsDevice, IHeightProvider heightProvider)
        {
            if (parentNode == null && nodeType != NodeType.Root)
            {
                throw new ArgumentException("A non-root node must have a parent");
            }
            
            _parentNode = parentNode;

            _edgeLength = edgeLength;
            _halfEdgeLength = _edgeLength/2;

            _nodeType = nodeType;

            CentrePoint = heightProvider.GetVectorWithHeight(centreX, centreZ);

            QuarterEdgeLength = _edgeLength/4;

            CalculateVertexBufferAndBoundingBox(graphicsDevice, heightProvider);

            ActiveIndexBuffer = IndexBufferSelection.Base;
        }

        #endregion

        #region Private methods       

        private void CalculateVertexBufferAndBoundingBox(GraphicsDevice graphicsDevice, IHeightProvider heightProvider)
        {
            float offsetNorth = CentrePoint.Z + _halfEdgeLength;
            float offsetWest = CentrePoint.X - _halfEdgeLength;
            float vertexSpacing = _edgeLength/(VerticesPerEdge - 1);

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            List<VertexPositionNormalBlend> resultVertices = new List<VertexPositionNormalBlend>();
            for (int y = 0; y < VerticesPerEdge; y++)
            {
                for (int x = 0; x < VerticesPerEdge; x++)
                {
                    float positionX = offsetWest + x*vertexSpacing;
                    float positionZ = offsetNorth - y*vertexSpacing;

                    VertexPositionNormalBlend vertex = new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(positionX, positionZ), heightProvider.GetNormalFromFiniteOffset(positionX, positionZ, NormalSampleOffset));
                    resultVertices.Add(vertex);

                    min = Vector3.Min(min, vertex.Position);
                    max = Vector3.Max(max, vertex.Position);
                }
            }

            VertexPositionNormalBlend[] resultVerticesData = resultVertices.ToArray();

            VertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalBlend.VertexDeclaration, resultVerticesData.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(resultVerticesData);

            BoundingBox = new BoundingBox(min, max);
        }

        #endregion

        #region Methods

        public void UpdateChildrenRecursively(Vector3 cameraPosition, List<QuadTreeNode> renderQueue, GraphicsDevice graphicsDevice, IHeightProvider heightProvider)
        {
            // Node should have children
            if (_halfEdgeLength > EdgeLengthLimit && (CentrePoint - cameraPosition).Length() < SplitDistanceMultiplier*_edgeLength)
            {
                // Add children if they aren't there
                if (!_hasChildren)
                {
                    ChildNorthWest = new QuadTreeNode(NodeType.NorthWest, this, CentrePoint.X - QuarterEdgeLength, CentrePoint.Z + QuarterEdgeLength, _halfEdgeLength, graphicsDevice, heightProvider);
                    ChildNorthEast = new QuadTreeNode(NodeType.NorthEast, this, CentrePoint.X + QuarterEdgeLength, CentrePoint.Z + QuarterEdgeLength, _halfEdgeLength, graphicsDevice, heightProvider);
                    ChildSouthEast = new QuadTreeNode(NodeType.SouthEast, this, CentrePoint.X + QuarterEdgeLength, CentrePoint.Z - QuarterEdgeLength, _halfEdgeLength, graphicsDevice, heightProvider);
                    ChildSouthWest = new QuadTreeNode(NodeType.SouthWest, this, CentrePoint.X - QuarterEdgeLength, CentrePoint.Z - QuarterEdgeLength, _halfEdgeLength, graphicsDevice, heightProvider);

                    _hasChildren = true;
                }

                ChildNorthWest.UpdateChildrenRecursively(cameraPosition, renderQueue, graphicsDevice, heightProvider);
                ChildNorthEast.UpdateChildrenRecursively(cameraPosition, renderQueue, graphicsDevice, heightProvider);
                ChildSouthEast.UpdateChildrenRecursively(cameraPosition, renderQueue, graphicsDevice, heightProvider);
                ChildSouthWest.UpdateChildrenRecursively(cameraPosition, renderQueue, graphicsDevice, heightProvider);
            }
            // Node shouldn't have children
            else
            {
                // Remove children if they are there
                if (_hasChildren)
                {
                    ChildNorthWest = null;
                    ChildNorthEast = null;
                    ChildSouthEast = null;
                    ChildSouthWest = null;

                    _hasChildren = false;
                }
                
                if (renderQueue != null)
                {
                    renderQueue.Add(this);
                }
            }
        }

        public void UpdateNeighboursRecursively()
        {
            switch (_nodeType)
            {
                case NodeType.NorthWest:
                    if (_parentNode.NeighbourNorth != null)
                    {
                        NeighbourNorth = _parentNode.NeighbourNorth.ChildSouthWest;
                    }
                    NeighbourEast = _parentNode.ChildNorthEast;
                    NeighbourSouth = _parentNode.ChildSouthWest;
                    if (_parentNode.NeighbourWest != null)
                    {
                        NeighbourWest = _parentNode.NeighbourWest.ChildNorthEast;
                    }
                    break;

                case NodeType.NorthEast:
                    if (_parentNode.NeighbourNorth != null)
                    {
                        NeighbourNorth = _parentNode.NeighbourNorth.ChildSouthEast;
                    }
                    if (_parentNode.NeighbourEast != null)
                    {
                        NeighbourEast = _parentNode.NeighbourEast.ChildNorthWest;
                    }
                    NeighbourSouth = _parentNode.ChildSouthEast;
                    NeighbourWest = _parentNode.ChildNorthWest;
                    break;

                case NodeType.SouthEast:
                    NeighbourNorth = _parentNode.ChildNorthEast;
                    if (_parentNode.NeighbourEast != null)
                    {
                        NeighbourEast = _parentNode.NeighbourEast.ChildSouthWest;
                    }
                    if (_parentNode.NeighbourSouth != null)
                    {
                        NeighbourSouth = _parentNode.NeighbourSouth.ChildNorthEast;
                    }
                    NeighbourWest = _parentNode.ChildSouthWest;
                    break;

                case NodeType.SouthWest:
                    NeighbourNorth = _parentNode.ChildNorthWest;
                    NeighbourEast = _parentNode.ChildSouthEast;
                    if (_parentNode.NeighbourSouth != null)
                    {
                        NeighbourSouth = _parentNode.NeighbourSouth.ChildNorthWest;
                    }
                    if (_parentNode.NeighbourWest != null)
                    {
                        NeighbourWest = _parentNode.NeighbourWest.ChildSouthEast;
                    }
                    break;
            }

            if (_hasChildren)
            {
                ChildNorthWest.UpdateNeighboursRecursively();
                ChildNorthEast.UpdateNeighboursRecursively();
                ChildSouthWest.UpdateNeighboursRecursively();
                ChildSouthEast.UpdateNeighboursRecursively();
            }
        }

        public void FixCracks()
        {
            ActiveIndexBuffer = IndexBufferSelection.Base;

            switch (_nodeType)
            {
                case NodeType.NorthWest:
                    if (NeighbourNorth == null && NeighbourWest == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.NwCrackFix;
                    }
                    else if (NeighbourNorth == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.NCrackFix;
                    }
                    else if (NeighbourWest == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.WCrackFix;
                    }
                    break;

                case NodeType.NorthEast:
                    if (NeighbourNorth == null && NeighbourEast == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.NeCrackFix;
                    }
                    else if (NeighbourNorth == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.NCrackFix;
                    }
                    else if (NeighbourEast == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.ECrackFix;
                    }
                    break;

                case NodeType.SouthEast:
                    if (NeighbourSouth == null && NeighbourEast == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.SeCrackFix;
                    }
                    else if (NeighbourSouth == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.SCrackFix;
                    }
                    else if (NeighbourEast == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.ECrackFix;
                    }
                    break;

                case NodeType.SouthWest:
                    if (NeighbourSouth == null && NeighbourWest == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.SwCrackFix;
                    }
                    else if (NeighbourSouth == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.SCrackFix;
                    }
                    else if (NeighbourWest == null)
                    {
                        ActiveIndexBuffer = IndexBufferSelection.WCrackFix;
                    }
                    break;
            }
        }
        
        #endregion
    }
}
