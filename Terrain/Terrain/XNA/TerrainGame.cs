using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Terrain.Environment;
using Terrain.QuadTree;
using Terrain.Utility;

namespace Terrain.XNA
{
    public class TerrainGame : Game
    {
        #region Constants

        /// <summary>
        /// Size of the terrain vertex buffer in cells
        /// </summary>
        private const int TerrainVertexBufferSize = 1000;

        /// <summary>
        /// Size of the water refraction/reflection maps in pixels
        /// </summary>
        private const int WaterMapSize = 512;

        #endregion

        #region Fields

        private readonly GraphicsDeviceManager _graphics;

        private KeyboardState _oldKeyboardState;
        private MouseState _oldMouseState;
        private bool _mouseLookActive;

        private Plane _waterClipPlane;
        private Matrix _waterReflectionMatrix;
        private Vector2 _waterCoordsOffset1, _waterCoordsOffset2;

        private World _world;
        private Camera _camera;

        private Effect _terrainEffect, _waterEffect;

        private VertexBuffer _terrainVertexBuffer;
        private VertexPositionNormalBlend[] _terrainVertexBufferData;

        private Model _waterPlaneModel;

        private Texture2D _groundTexture, _groundSlopeTexture, _groundDetailTexture;
        private Texture2D _waterNormalMap;
        private RenderTarget2D _waterRefractionMapTarget, _waterReflectionMapTarget;

        #endregion

        #region Constructors

        public TerrainGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferMultiSampling = true;

            //_graphics.PreferredBackBufferWidth = 1920; 
            //_graphics.PreferredBackBufferHeight = 1080; 
            //_graphics.IsFullScreen = true;

            Content.RootDirectory = "Content";
        }

        #endregion

        #region Private methods

        #region Content initialisation/loading

        private void InitialiseTerrain()
        {
            _terrainVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalBlend.VertexDeclaration, TerrainVertexBufferSize*4, BufferUsage.WriteOnly);
            _terrainVertexBufferData = new VertexPositionNormalBlend[TerrainVertexBufferSize*4];
        }

        private void LoadWaterPlane()
        {
            _waterPlaneModel = Content.Load<Model>("Models/WaterPlane");

            foreach (ModelMesh mesh in _waterPlaneModel.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    part.Effect = _waterEffect;
                }
            }
        }

        #endregion

        #region Content drawing

        private void DrawTerrainRenderQueue()
        {
            List<QuadTreeNode> culledRenderQueue = _world.CurrentRenderQueue.Where(n => _camera.Frustum.Intersects(n.BoundingBox)).ToList();
            QuadTreeNode lastNodeInCulledRenderQueue = culledRenderQueue.LastOrDefault();
            int numNodesBuffered = 0;

            foreach (QuadTreeNode node in culledRenderQueue)
            {
                // Place the current node in the buffer data
                int curIndex = 4*numNodesBuffered;
                _terrainVertexBufferData[curIndex] = node.VertexNorthWest.CrackFixVertex ?? node.VertexNorthWest.MainVertex;
                _terrainVertexBufferData[curIndex + 1] = node.VertexSouthWest.CrackFixVertex ?? node.VertexSouthWest.MainVertex;
                _terrainVertexBufferData[curIndex + 2] = node.VertexNorthEast.CrackFixVertex ?? node.VertexNorthEast.MainVertex;
                _terrainVertexBufferData[curIndex + 3] = node.VertexSouthEast.CrackFixVertex ?? node.VertexSouthEast.MainVertex;

                numNodesBuffered += 1;

                // The buffer data is full - need to render the contents and refill from the beginning
                if (numNodesBuffered >= TerrainVertexBufferSize)
                {
                    _terrainVertexBuffer.SetData(_terrainVertexBufferData);
                    GraphicsDevice.SetVertexBuffer(_terrainVertexBuffer);
                    for (int i = 0; i < numNodesBuffered; i++)
                    {
                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, i*4, 2);
                    }
                    GraphicsDevice.SetVertexBuffer(null);

                    numNodesBuffered = 0;
                }
                // We have no more nodes to render after this one
                else if (node == lastNodeInCulledRenderQueue)
                {
                    _terrainVertexBuffer.SetData(_terrainVertexBufferData, 0, numNodesBuffered*4);
                    GraphicsDevice.SetVertexBuffer(_terrainVertexBuffer);
                    for (int i = 0; i < numNodesBuffered; i++)
                    {
                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, i*4, 2);
                    }
                    GraphicsDevice.SetVertexBuffer(null);
                }
            }
        }

        private void DrawTerrain(bool enableFog = true, Vector4 ? clipPlane = null, Matrix? reflectionMatrix = null)
        {
            _terrainEffect.CurrentTechnique = _terrainEffect.Techniques["TerrainTechnique"];

            _terrainEffect.Parameters["World"].SetValue(Matrix.Identity);
            _terrainEffect.Parameters["WorldViewProjection"].SetValue((reflectionMatrix ?? Matrix.Identity)*_camera.ViewMatrix*_camera.ProjectionMatrix);
            _terrainEffect.Parameters["EnableFog"].SetValue(enableFog);
            _terrainEffect.Parameters["LightPower"].SetValue(_world.Light.Power);
            _terrainEffect.Parameters["AmbientLightPower"].SetValue(_world.Light.AmbientPower);
            _terrainEffect.Parameters["TerrainTextureScale"].SetValue(0.03f);
            _terrainEffect.Parameters["FogStart"].SetValue(10);
            _terrainEffect.Parameters["FogEnd"].SetValue(_world.TerrainRootEdgeLength/4);
            _terrainEffect.Parameters["CameraPosition"].SetValue(_camera.Position);
            _terrainEffect.Parameters["LightDirection"].SetValue(_world.Light.Direction);
            _terrainEffect.Parameters["FogColour"].SetValue(Color.CornflowerBlue.ToVector3());
            _terrainEffect.Parameters["ClipPlane"].SetValue(clipPlane ?? Vector4.Zero);
            _terrainEffect.Parameters["TerrainTexture"].SetValue(_groundTexture);
            _terrainEffect.Parameters["TerrainSlopeTexture"].SetValue(_groundSlopeTexture);
            _terrainEffect.Parameters["TerrainDetailTexture"].SetValue(_groundDetailTexture);

            if (reflectionMatrix != null)
                GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            foreach (EffectPass pass in _terrainEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                DrawTerrainRenderQueue();
            }

            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        private void DrawWaterPlane()
        {
            Matrix waterWorldMatrix = Matrix.CreateScale(_world.TerrainRootEdgeLength)*Matrix.CreateTranslation(_world.TerrainCentrePosition);

            GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            foreach (ModelMesh mesh in _waterPlaneModel.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    effect.CurrentTechnique = effect.Techniques["WaterTechnique"];

                    effect.Parameters["World"].SetValue(waterWorldMatrix);
                    effect.Parameters["WorldViewProjection"].SetValue(waterWorldMatrix*_camera.ViewMatrix*_camera.ProjectionMatrix);
                    effect.Parameters["LightPower"].SetValue(_world.Light.Power);
                    effect.Parameters["SpecularPower"].SetValue(40);
                    effect.Parameters["WaterRefractiveIndexScale"].SetValue(0.04f);
                    effect.Parameters["FogStart"].SetValue(10);
                    effect.Parameters["FogEnd"].SetValue(_world.TerrainRootEdgeLength/4);
                    effect.Parameters["TexCoordDeltaFirst"].SetValue(_waterCoordsOffset1);
                    effect.Parameters["TexCoordDeltaSecond"].SetValue(_waterCoordsOffset2);
                    effect.Parameters["CameraPosition"].SetValue(_camera.Position);
                    effect.Parameters["LightDirection"].SetValue(_world.Light.Direction);
                    effect.Parameters["FogColour"].SetValue(Color.CornflowerBlue.ToVector3());
                    effect.Parameters["WaterIntrinsicColour"].SetValue(new Vector3(0, 0.1f, 0.2f));
                    effect.Parameters["WaterNormalMap"].SetValue(_waterNormalMap);
                    effect.Parameters["WaterRefractionMap"].SetValue(_waterRefractionMapTarget);
                    effect.Parameters["WaterReflectionMap"].SetValue(_waterReflectionMapTarget);
                }

                mesh.Draw();
            }

            GraphicsDevice.BlendState = BlendState.Opaque;
        }

        #endregion

        private void ProcessInput(GameTime gameTime)
        {
            KeyboardState newKeyboardState = Keyboard.GetState();
            MouseState newMouseState = Mouse.GetState();

            // Linear motion
            if (_oldKeyboardState.IsKeyDown(Keys.Space) && newKeyboardState.IsKeyUp(Keys.Space))
                _camera.WalkModeActive = !_camera.WalkModeActive;

            float forwardDelta = newKeyboardState.IsKeyDown(Keys.W) ? gameTime.ElapsedGameTime.Milliseconds : newKeyboardState.IsKeyDown(Keys.S) ? -gameTime.ElapsedGameTime.Milliseconds : 0;
            float rightDelta = newKeyboardState.IsKeyDown(Keys.D) ? gameTime.ElapsedGameTime.Milliseconds : newKeyboardState.IsKeyDown(Keys.A) ? -gameTime.ElapsedGameTime.Milliseconds : 0;

            // Angular motion
            if (_oldKeyboardState.IsKeyDown(Keys.C) && newKeyboardState.IsKeyUp(Keys.C))
                _mouseLookActive = !_mouseLookActive;

            float yawDelta = 0, pitchDelta = 0;
            if (_mouseLookActive)
            {
                yawDelta = _oldMouseState.X - newMouseState.X;
                pitchDelta = _oldMouseState.Y - newMouseState.Y;

                Mouse.SetPosition(GraphicsDevice.Viewport.Width/2, GraphicsDevice.Viewport.Height/2);
                newMouseState = Mouse.GetState();
            }

            // Apply motion to camera
            _camera.Update(forwardDelta*0.05f, rightDelta*0.05f, yawDelta*0.01f, pitchDelta*0.01f);

            _oldKeyboardState = newKeyboardState;
            _oldMouseState = newMouseState;
        }

        private void UpdateWater(GameTime gameTime)
        {
            _waterCoordsOffset1.X += gameTime.ElapsedGameTime.Milliseconds/100000.0f;
            if (_waterCoordsOffset1.X > 1)
                _waterCoordsOffset1.X -= 1;

            _waterCoordsOffset1.Y += gameTime.ElapsedGameTime.Milliseconds/100000.0f;
            if (_waterCoordsOffset1.Y > 1)
                _waterCoordsOffset1.Y -= 1;

            _waterCoordsOffset2.X -= gameTime.ElapsedGameTime.Milliseconds/150000.0f;
            if (_waterCoordsOffset2.X < 0)
                _waterCoordsOffset2.X += 1;
        }
        
        #endregion

        #region Game overrides

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            _oldKeyboardState = Keyboard.GetState();
            _oldMouseState = Mouse.GetState();

            _waterClipPlane = new Plane(0, 1, 0, 0);
            _waterReflectionMatrix = Matrix.CreateReflection(_waterClipPlane);
            _waterCoordsOffset1 = new Vector2();
            _waterCoordsOffset2 = new Vector2();

            DirectionLight light = new DirectionLight(new Vector3(0, -0.3f, -1), 1, 0.1f);
            _world = new World(new NoiseHeightProvider(124, 4, 0.2f, 20, 100, 0), 2500, light);

            Matrix projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60), GraphicsDevice.Viewport.AspectRatio, 5, _world.TerrainRootEdgeLength/4);
            _camera = new Camera(new Vector3(0, 1, 0), new Vector3(0, 0, -1), new Vector3(0, 30, 0), projectionMatrix, _world.HeightProvider);

            InitialiseTerrain();
            
            _waterRefractionMapTarget = new RenderTarget2D(GraphicsDevice, WaterMapSize, WaterMapSize, false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            _waterReflectionMapTarget = new RenderTarget2D(GraphicsDevice, WaterMapSize, WaterMapSize, false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            _terrainEffect = Content.Load<Effect>("Effects/TerrainEffect");
            _waterEffect = Content.Load<Effect>("Effects/WaterEffect");

            LoadWaterPlane();

            _groundTexture = Content.Load<Texture2D>("Textures/ground");
            _groundSlopeTexture = Content.Load<Texture2D>("Textures/groundSlope");
            _groundDetailTexture = Content.Load<Texture2D>("Textures/groundDetail");
            _waterNormalMap = Content.Load<Texture2D>("Textures/waterNormal");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            ProcessInput(gameTime);
            UpdateWater(gameTime);

            _world.Update(_camera.Position);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // PASS 1: Draw the water refraction map
            GraphicsDevice.SetRenderTarget(_waterRefractionMapTarget);
            GraphicsDevice.Clear(Color.Black);

                DrawTerrain(false, new Vector4(-_waterClipPlane.Normal, _waterClipPlane.D + 1));

            // PASS 2: Draw the water reflection map
            GraphicsDevice.SetRenderTarget(_waterReflectionMapTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);

                DrawTerrain(false, new Vector4(_waterClipPlane.Normal, _waterClipPlane.D), _waterReflectionMatrix);

            // PASS 3: Draw the scene
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.CornflowerBlue);

                DrawTerrain();
                DrawWaterPlane();

                base.Draw(gameTime);
        }

        #endregion
    }
}

