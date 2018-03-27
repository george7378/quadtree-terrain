using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Terrain.QuadTree;

namespace Terrain.Environment
{
    public class World
    {
        #region Fields

        private QuadTreeNode _rootNode, _updatedRootNode;

        private bool _updatedRootNodeInConstruction;
        private readonly object _lockObject = new object();

        #endregion

        #region Properties

        public IHeightProvider HeightProvider { get; private set; }
        
        public int TerrainRootEdgeLength { get; private set; }

        public List<QuadTreeNode> CurrentRenderQueue { get; private set; }

        public DirectionLight Light { get; set; }

        public Vector3 TerrainCentrePosition { get; private set; }

        #endregion

        #region Constructors

        public World(IHeightProvider heightProvider, int edgeLength, DirectionLight light)
        {
            _rootNode = new QuadTreeNode(NodeType.Root, null, 0, 0, edgeLength, heightProvider);

            HeightProvider = heightProvider;
            TerrainRootEdgeLength = edgeLength;
            CurrentRenderQueue = new List<QuadTreeNode>();
            Light = light;
            TerrainCentrePosition = new Vector3();
        }

        #endregion

        #region Private methods

        private void CreateUpdatedRootNode(object parameters)
        {
            try
            {
                float[] updatedRootNodePosition = parameters as float[];
                if (updatedRootNodePosition == null || updatedRootNodePosition.Length != 3)
                    throw new ArgumentException("Invalid parameters during creation of updated root node");

                QuadTreeNode updatedRootNode = new QuadTreeNode(NodeType.Root, null, updatedRootNodePosition[0], updatedRootNodePosition[2], TerrainRootEdgeLength, HeightProvider);
                updatedRootNode.UpdateChildrenRecursively(new Vector3(updatedRootNodePosition[0], updatedRootNodePosition[1], updatedRootNodePosition[2]), null, HeightProvider);

                lock (_lockObject)
                {
                    _updatedRootNode = updatedRootNode;
                }
            }
            finally
            {
                lock (_lockObject)
                {
                    _updatedRootNodeInConstruction = false;
                }
            }
        }

        #endregion

        #region Methods

        public void Update(Vector3 cameraPosition)
        {
            lock (_lockObject)
            {
                // Create an updated root node as we are too far from the centre of the current one
                if (_updatedRootNode == null && !_updatedRootNodeInConstruction && Vector2.Distance(new Vector2(cameraPosition.X, cameraPosition.Z), new Vector2(_rootNode.CentrePoint.MainVertex.Position.X, _rootNode.CentrePoint.MainVertex.Position.Z)) > _rootNode.QuarterEdgeLength)
                {
                    _updatedRootNodeInConstruction = true;
                    TerrainCentrePosition = new Vector3(cameraPosition.X, 0, cameraPosition.Z);

                    Thread createNewRootNodeThread = new Thread(CreateUpdatedRootNode);
                    createNewRootNodeThread.Start(new float[3] { cameraPosition.X, cameraPosition.Y, cameraPosition.Z });
                }
            }
            
            CurrentRenderQueue.Clear();

            lock (_lockObject)
            {
                // An updated root node exists - replace the original with it
                if (_updatedRootNode != null)
                {
                    _rootNode = _updatedRootNode;
                    _updatedRootNode = null;
                }
            }

            _rootNode.UpdateChildrenRecursively(cameraPosition, CurrentRenderQueue, HeightProvider);
            _rootNode.UpdateNeighboursRecursively();
            
            CurrentRenderQueue.ForEach(n => n.UpdateCrackFixVertices());
        }

        #endregion
    }
}
