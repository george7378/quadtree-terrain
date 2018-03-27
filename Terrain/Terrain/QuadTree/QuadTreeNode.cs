using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terrain.Environment;
using Terrain.Utility;

namespace Terrain.QuadTree
{
    public class QuadTreeNode
    {
        #region Constants

        private static readonly float EdgeLengthLimit = 2;
        private static readonly float SplitDistanceMultiplier = 10;
        private static readonly float NormalSampleOffset = 1;

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
        
        #region Vertices

        public TerrainVertexContainer CentrePoint { get; private set; }

        public TerrainVertexContainer VertexNorthWest { get; private set; }

        public TerrainVertexContainer VertexNorthEast { get; private set; }

        public TerrainVertexContainer VertexSouthEast { get; private set; }

        public TerrainVertexContainer VertexSouthWest { get; private set; }

        #endregion

        public float QuarterEdgeLength { get; private set; }

        public BoundingBox BoundingBox { get; private set; }

        #endregion

        #region Constructors

        public QuadTreeNode(NodeType nodeType, QuadTreeNode parentNode, float centreX, float centreZ, float edgeLength, IHeightProvider heightProvider)
        {
            if (parentNode == null && nodeType != NodeType.Root)
                throw new ArgumentException("A non-root node must have a parent");

            _parentNode = parentNode;

            _edgeLength = edgeLength;
            _halfEdgeLength = _edgeLength/2;

            _nodeType = nodeType;

            CentrePoint = new TerrainVertexContainer(new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(centreX, centreZ), heightProvider.GetNormalFromFiniteOffset(centreX, centreZ, NormalSampleOffset)));

            float offsetNorth = CentrePoint.MainVertex.Position.Z + _halfEdgeLength;
            float offsetEast = CentrePoint.MainVertex.Position.X + _halfEdgeLength;
            float offsetSouth = CentrePoint.MainVertex.Position.Z - _halfEdgeLength;
            float offsetWest = CentrePoint.MainVertex.Position.X - _halfEdgeLength;
            switch (_nodeType)
            {
                case NodeType.Root:
                    VertexNorthWest = new TerrainVertexContainer(new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(offsetWest, offsetNorth), heightProvider.GetNormalFromFiniteOffset(offsetWest, offsetNorth, NormalSampleOffset)));
                    VertexNorthEast = new TerrainVertexContainer(new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(offsetEast, offsetNorth), heightProvider.GetNormalFromFiniteOffset(offsetEast, offsetNorth, NormalSampleOffset)));
                    VertexSouthEast = new TerrainVertexContainer(new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(offsetEast, offsetSouth), heightProvider.GetNormalFromFiniteOffset(offsetEast, offsetSouth, NormalSampleOffset)));
                    VertexSouthWest = new TerrainVertexContainer(new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(offsetWest, offsetSouth), heightProvider.GetNormalFromFiniteOffset(offsetWest, offsetSouth, NormalSampleOffset)));
                    break;

                case NodeType.NorthWest:
                    VertexNorthWest = _parentNode.VertexNorthWest;
                    VertexNorthEast = new TerrainVertexContainer(new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(offsetEast, offsetNorth), heightProvider.GetNormalFromFiniteOffset(offsetEast, offsetNorth, NormalSampleOffset)));
                    VertexSouthEast = _parentNode.CentrePoint;
                    VertexSouthWest = new TerrainVertexContainer(new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(offsetWest, offsetSouth), heightProvider.GetNormalFromFiniteOffset(offsetWest, offsetSouth, NormalSampleOffset)));
                    break;

                case NodeType.NorthEast:
                    VertexNorthWest = _parentNode.ChildNorthWest.VertexNorthEast;
                    VertexNorthEast = _parentNode.VertexNorthEast;
                    VertexSouthEast = new TerrainVertexContainer(new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(offsetEast, offsetSouth), heightProvider.GetNormalFromFiniteOffset(offsetEast, offsetSouth, NormalSampleOffset)));
                    VertexSouthWest = _parentNode.ChildNorthWest.VertexSouthEast;
                    break;

                case NodeType.SouthEast:
                    VertexNorthWest = _parentNode.ChildNorthEast.VertexSouthWest;
                    VertexNorthEast = _parentNode.ChildNorthEast.VertexSouthEast;
                    VertexSouthEast = _parentNode.VertexSouthEast;
                    VertexSouthWest = new TerrainVertexContainer(new VertexPositionNormalBlend(heightProvider.GetVectorWithHeight(offsetWest, offsetSouth), heightProvider.GetNormalFromFiniteOffset(offsetWest, offsetSouth, NormalSampleOffset)));
                    break;

                case NodeType.SouthWest:
                    VertexNorthWest = _parentNode.ChildNorthWest.VertexSouthWest;
                    VertexNorthEast = _parentNode.ChildSouthEast.VertexNorthWest;
                    VertexSouthEast = _parentNode.ChildSouthEast.VertexSouthWest;
                    VertexSouthWest = _parentNode.VertexSouthWest;
                    break;
            }

            QuarterEdgeLength = _edgeLength/4;

            CalculateBoundingBox();
        }

        #endregion

        #region Private methods

        private void CalculateBoundingBox()
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            min = Vector3.Min(min, VertexNorthWest.CrackFixVertex == null ? VertexNorthWest.MainVertex.Position : VertexNorthWest.CrackFixVertex.Value.Position);
            max = Vector3.Max(max, VertexNorthWest.CrackFixVertex == null ? VertexNorthWest.MainVertex.Position : VertexNorthWest.CrackFixVertex.Value.Position);

            min = Vector3.Min(min, VertexNorthEast.CrackFixVertex == null ? VertexNorthEast.MainVertex.Position : VertexNorthEast.CrackFixVertex.Value.Position);
            max = Vector3.Max(max, VertexNorthEast.CrackFixVertex == null ? VertexNorthEast.MainVertex.Position : VertexNorthEast.CrackFixVertex.Value.Position);

            min = Vector3.Min(min, VertexSouthEast.CrackFixVertex == null ? VertexSouthEast.MainVertex.Position : VertexSouthEast.CrackFixVertex.Value.Position);
            max = Vector3.Max(max, VertexSouthEast.CrackFixVertex == null ? VertexSouthEast.MainVertex.Position : VertexSouthEast.CrackFixVertex.Value.Position);

            min = Vector3.Min(min, VertexSouthWest.CrackFixVertex == null ? VertexSouthWest.MainVertex.Position : VertexSouthWest.CrackFixVertex.Value.Position);
            max = Vector3.Max(max, VertexSouthWest.CrackFixVertex == null ? VertexSouthWest.MainVertex.Position : VertexSouthWest.CrackFixVertex.Value.Position);

            BoundingBox = new BoundingBox(min, max);
        }

        #endregion

        #region Methods

        public void UpdateChildrenRecursively(Vector3 cameraPosition, List<QuadTreeNode> renderQueue, IHeightProvider heightProvider)
        {
            // Node should have children
            if (_halfEdgeLength > EdgeLengthLimit && (CentrePoint.MainVertex.Position - cameraPosition).Length() < SplitDistanceMultiplier*_edgeLength)
            {
                // Add children if they aren't there
                if (!_hasChildren)
                {
                    ChildNorthWest = new QuadTreeNode(NodeType.NorthWest, this, CentrePoint.MainVertex.Position.X - QuarterEdgeLength, CentrePoint.MainVertex.Position.Z + QuarterEdgeLength, _halfEdgeLength, heightProvider);
                    ChildNorthEast = new QuadTreeNode(NodeType.NorthEast, this, CentrePoint.MainVertex.Position.X + QuarterEdgeLength, CentrePoint.MainVertex.Position.Z + QuarterEdgeLength, _halfEdgeLength, heightProvider);
                    ChildSouthEast = new QuadTreeNode(NodeType.SouthEast, this, CentrePoint.MainVertex.Position.X + QuarterEdgeLength, CentrePoint.MainVertex.Position.Z - QuarterEdgeLength, _halfEdgeLength, heightProvider);
                    ChildSouthWest = new QuadTreeNode(NodeType.SouthWest, this, CentrePoint.MainVertex.Position.X - QuarterEdgeLength, CentrePoint.MainVertex.Position.Z - QuarterEdgeLength, _halfEdgeLength, heightProvider);

                    _hasChildren = true;
                }

                ChildNorthWest.UpdateChildrenRecursively(cameraPosition, renderQueue, heightProvider);
                ChildNorthEast.UpdateChildrenRecursively(cameraPosition, renderQueue, heightProvider);
                ChildSouthEast.UpdateChildrenRecursively(cameraPosition, renderQueue, heightProvider);
                ChildSouthWest.UpdateChildrenRecursively(cameraPosition, renderQueue, heightProvider);
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
                    renderQueue.Add(this);
            }
        }

        public void UpdateNeighboursRecursively()
        {
            switch (_nodeType)
            {
                case NodeType.NorthWest:
                    if (_parentNode.NeighbourNorth != null)
                        NeighbourNorth = _parentNode.NeighbourNorth.ChildSouthWest;
                    NeighbourEast = _parentNode.ChildNorthEast;
                    NeighbourSouth = _parentNode.ChildSouthWest;
                    if (_parentNode.NeighbourWest != null)
                        NeighbourWest = _parentNode.NeighbourWest.ChildNorthEast;
                    break;

                case NodeType.NorthEast:
                    if (_parentNode.NeighbourNorth != null)
                        NeighbourNorth = _parentNode.NeighbourNorth.ChildSouthEast;
                    if (_parentNode.NeighbourEast != null)
                        NeighbourEast = _parentNode.NeighbourEast.ChildNorthWest;
                    NeighbourSouth = _parentNode.ChildSouthEast;
                    NeighbourWest = _parentNode.ChildNorthWest;
                    break;

                case NodeType.SouthEast:
                    NeighbourNorth = _parentNode.ChildNorthEast;
                    if (_parentNode.NeighbourEast != null)
                        NeighbourEast = _parentNode.NeighbourEast.ChildSouthWest;
                    if (_parentNode.NeighbourSouth != null)
                        NeighbourSouth = _parentNode.NeighbourSouth.ChildNorthEast;
                    NeighbourWest = _parentNode.ChildSouthWest;
                    break;

                case NodeType.SouthWest:
                    NeighbourNorth = _parentNode.ChildNorthWest;
                    NeighbourEast = _parentNode.ChildSouthEast;
                    if (_parentNode.NeighbourSouth != null)
                        NeighbourSouth = _parentNode.NeighbourSouth.ChildNorthWest;
                    if (_parentNode.NeighbourWest != null)
                        NeighbourWest = _parentNode.NeighbourWest.ChildSouthEast;
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

        public void UpdateCrackFixVertices()
        {
            bool boundingBoxInvalidated = false;

            switch (_nodeType)
            {
                case NodeType.NorthWest:
                    if (NeighbourNorth == null && VertexNorthEast.CrackFixVertex == null)
                    {
                        VertexNorthEast.CrackFixVertex = new VertexPositionNormalBlend(new Vector3(VertexNorthEast.MainVertex.Position.X, (_parentNode.VertexNorthWest.MainVertex.Position.Y + _parentNode.VertexNorthEast.MainVertex.Position.Y)/2, VertexNorthEast.MainVertex.Position.Z), Vector3.Lerp(_parentNode.VertexNorthWest.MainVertex.Normal, _parentNode.VertexNorthEast.MainVertex.Normal, 0.5f));
                        boundingBoxInvalidated = true;
                    }
                    else if (NeighbourNorth != null && VertexNorthEast.CrackFixVertex != null)
                    {
                        VertexNorthEast.CrackFixVertex = null;
                        boundingBoxInvalidated = true;
                    }

                    if (NeighbourWest == null && VertexSouthWest.CrackFixVertex == null)
                    {
                        VertexSouthWest.CrackFixVertex = new VertexPositionNormalBlend(new Vector3(VertexSouthWest.MainVertex.Position.X, (_parentNode.VertexSouthWest.MainVertex.Position.Y + _parentNode.VertexNorthWest.MainVertex.Position.Y)/2, VertexSouthWest.MainVertex.Position.Z), Vector3.Lerp(_parentNode.VertexSouthWest.MainVertex.Normal, _parentNode.VertexNorthWest.MainVertex.Normal, 0.5f));
                        boundingBoxInvalidated = true;
                    }
                    else if (NeighbourWest != null && VertexSouthWest.CrackFixVertex != null)
                    {
                        VertexSouthWest.CrackFixVertex = null;
                        boundingBoxInvalidated = true;
                    }
                    break;

                case NodeType.NorthEast:
                    if (NeighbourEast == null && VertexSouthEast.CrackFixVertex == null)
                    {
                        VertexSouthEast.CrackFixVertex = new VertexPositionNormalBlend(new Vector3(VertexSouthEast.MainVertex.Position.X, (_parentNode.VertexNorthEast.MainVertex.Position.Y + _parentNode.VertexSouthEast.MainVertex.Position.Y)/2, VertexSouthEast.MainVertex.Position.Z), Vector3.Lerp(_parentNode.VertexNorthEast.MainVertex.Normal, _parentNode.VertexSouthEast.MainVertex.Normal, 0.5f));
                        boundingBoxInvalidated = true;
                    }
                    else if (NeighbourEast != null && VertexSouthEast.CrackFixVertex != null)
                    {
                        VertexSouthEast.CrackFixVertex = null;
                        boundingBoxInvalidated = true;
                    }
                    break;

                case NodeType.SouthEast:
                    if (NeighbourSouth == null && VertexSouthWest.CrackFixVertex == null)
                    {
                        VertexSouthWest.CrackFixVertex = new VertexPositionNormalBlend(new Vector3(VertexSouthWest.MainVertex.Position.X, (_parentNode.VertexSouthEast.MainVertex.Position.Y + _parentNode.VertexSouthWest.MainVertex.Position.Y)/2, VertexSouthWest.MainVertex.Position.Z), Vector3.Lerp(_parentNode.VertexSouthEast.MainVertex.Normal, _parentNode.VertexSouthWest.MainVertex.Normal, 0.5f));
                        boundingBoxInvalidated = true;
                    }
                    else if (NeighbourSouth != null && VertexSouthWest.CrackFixVertex != null)
                    {
                        VertexSouthWest.CrackFixVertex = null;
                        boundingBoxInvalidated = true;
                    }
                    break;
            }

            if (boundingBoxInvalidated)
                CalculateBoundingBox();
        }
        
        #endregion
    }
}
