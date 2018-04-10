using Microsoft.Xna.Framework;
using System;
using Terrain.Environment;

namespace Terrain.XNA
{
    public class Camera
    {
        #region Constants

        private const float MaxLookUpDotProduct = 0.95f;

        #endregion

        #region Fields

        private readonly Vector3 _vectorUp;
        private Vector3 _vectorLook, _vectorRight;

        #endregion

        #region Properties

        public Vector3 Position { get; private set; }

        public Matrix ViewMatrix { get; private set; }

        public Matrix ProjectionMatrix { get; set; }

        public BoundingFrustum Frustum { get; private set; }

        public bool WalkModeActive { get; set; }

        public IHeightProvider WalkModeHeightProvider { get; set; }

        public float WalkModeEyeHeight { get; set; }

        #endregion

        #region Constructors

        public Camera(Vector3 upDirection, Vector3 initialLookDirection, Vector3 initialPosition, Matrix projectionMatrix, IHeightProvider walkModeHeightProvider)
        {
            if (Math.Abs(Vector3.Dot(initialLookDirection, upDirection))/(initialLookDirection.Length()*upDirection.Length()) > MaxLookUpDotProduct)
            {
                throw new ArgumentException("upDirection and initialLookDirection do not have a sufficiently large angle between them");
            }

            _vectorUp = upDirection;
            _vectorUp.Normalize();

            _vectorLook = initialLookDirection;
            _vectorRight = Vector3.Cross(_vectorLook, _vectorUp);

            Position = initialPosition;
            ProjectionMatrix = projectionMatrix;
            WalkModeHeightProvider = walkModeHeightProvider;
            WalkModeEyeHeight = 10;

            // Ensure the view matrix reflects the starting conditions
            Update(0, 0, 0, 0);
        }

        #endregion

        #region Methods

        public void Update(float forwardDelta, float rightDelta, float yawDelta, float pitchDelta)
        {
            _vectorLook.Normalize();
            _vectorRight.Normalize();

            // Update vectors based on angle deltas
            Matrix yawMatrix = Matrix.CreateFromAxisAngle(_vectorUp, yawDelta);
            _vectorLook = Vector3.Transform(_vectorLook, yawMatrix);
            _vectorRight = Vector3.Transform(_vectorRight, yawMatrix);

            Matrix pitchMatrix = Matrix.CreateFromAxisAngle(_vectorRight, pitchDelta);
            Vector3 potentialVectorLook = Vector3.Transform(_vectorLook, pitchMatrix);
            if (Math.Abs(Vector3.Dot(potentialVectorLook, _vectorUp)) < MaxLookUpDotProduct)
            {
                _vectorLook = potentialVectorLook;
            }

            // Update position based on movement deltas
            Position += _vectorLook*forwardDelta + _vectorRight*rightDelta;
            if (WalkModeActive)
            {
                float cameraHeight = WalkModeHeightProvider.GetHeight(Position.X, Position.Z);
                if (cameraHeight < 0)
                {
                    cameraHeight = 0;
                }
                Position = new Vector3(Position.X, cameraHeight + WalkModeEyeHeight, Position.Z);
            }

            // Update view matrix and frustum
            ViewMatrix = Matrix.CreateLookAt(Position, Position + _vectorLook, _vectorUp);
            Frustum = new BoundingFrustum(ViewMatrix*ProjectionMatrix);
            //Frustum = new BoundingFrustum(Matrix.CreateLookAt(new Vector3(0, 30, 0), new Vector3(0, 30, -1), _vectorUp)*ProjectionMatrix);
        }

        #endregion
    }
}
