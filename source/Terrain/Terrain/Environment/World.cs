using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Terrain.QuadTree;
using Microsoft.Xna.Framework.Graphics;

namespace Terrain.Environment
{
    public class World
    {
        #region Fields

        private readonly GraphicsDevice _graphicsDevice;

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

        public World(GraphicsDevice graphicsDevice, IHeightProvider heightProvider, int edgeLength, DirectionLight light)
        {
            _graphicsDevice = graphicsDevice;

            _rootNode = new QuadTreeNode(NodeType.Root, null, 0, 0, edgeLength, _graphicsDevice, heightProvider);

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
                Vector3? cameraPosition = parameters as Vector3?;
                if (!cameraPosition.HasValue)
                {
                    throw new ArgumentException("Invalid parameters during creation of updated root node");
                }

                QuadTreeNode updatedRootNode = new QuadTreeNode(NodeType.Root, null, cameraPosition.Value.X, cameraPosition.Value.Z, TerrainRootEdgeLength, _graphicsDevice, HeightProvider);
                updatedRootNode.UpdateChildrenRecursively(cameraPosition.Value, null, _graphicsDevice, HeightProvider);

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
                if (_updatedRootNode == null && !_updatedRootNodeInConstruction && Vector2.Distance(new Vector2(cameraPosition.X, cameraPosition.Z), new Vector2(_rootNode.CentrePoint.X, _rootNode.CentrePoint.Z)) > _rootNode.QuarterEdgeLength)
                {
                    _updatedRootNodeInConstruction = true;
                    TerrainCentrePosition = new Vector3(cameraPosition.X, 0, cameraPosition.Z);

                    Thread createNewRootNodeThread = new Thread(CreateUpdatedRootNode);
                    createNewRootNodeThread.Start(cameraPosition);
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

            _rootNode.UpdateChildrenRecursively(cameraPosition, CurrentRenderQueue, _graphicsDevice, HeightProvider);
            _rootNode.UpdateNeighboursRecursively();

            CurrentRenderQueue.ForEach(node => node.FixCracks());
        }
        
        #endregion
    }
}
