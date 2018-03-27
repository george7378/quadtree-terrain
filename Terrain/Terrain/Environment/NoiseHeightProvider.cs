using System;
using Microsoft.Xna.Framework;

namespace Terrain.Environment
{
    public class NoiseHeightProvider : IHeightProvider
    {
        #region Properties

        public int Seed { get; set; }

        public int NumberOfOctaves { get; set; }

        public float Persistence { get; set; }

        public float AmplitudeMultiplier { get; set; }

        public float Zoom { get; set; }

        public float VerticalOffset { get; set; }

        #endregion

        #region Constructors

        public NoiseHeightProvider(int seed, int numberOfOctaves, float persistence, float amplitudeMultiplier, float zoom, float verticalOffset)
        {
            Seed = seed;
            NumberOfOctaves = numberOfOctaves;
            Persistence = persistence;
            AmplitudeMultiplier = amplitudeMultiplier;
            Zoom = zoom;
            VerticalOffset = verticalOffset;
        }

        #endregion

        #region Private methods

        private float CosineInterpolate(float v1, float v2, float w)
        {
            return v1 + 0.5f*(1 - (float)Math.Cos(w*Math.PI))*(v2 - v1);
        }

        private float GridNoise(int x, int z)
        {
            int n = (1619*x + 31337*z + 1013*Seed) & 0x7fffffff;
            n = (n >> 13) ^ n;

            return 1 - ((n*(n*n*60493 + 19990303) + 1376312589) & 0x7fffffff)/(float)1073741824;
        }

        private float InterpolatedGridNoise(float x, float z)
        {
            int integerX = (int)x;
            float fractionalX = x - integerX;

            int integerY = (int)z;
            float fractionalY = z - integerY;

            float v1 = GridNoise(integerX, integerY);
            float v2 = GridNoise(integerX + 1, integerY);
            float v3 = GridNoise(integerX, integerY + 1);
            float v4 = GridNoise(integerX + 1, integerY + 1);

            float i1 = CosineInterpolate(v1, v2, fractionalX);
            float i2 = CosineInterpolate(v3, v4, fractionalX);

            return CosineInterpolate(i1, i2, fractionalY);
        }

        #endregion
        
        #region IHeightProvider implementations

        public float GetHeight(float x, float z)
        {
            float total = 0;
            for (int o = 0; o < NumberOfOctaves; o++)
            {
                int frequency = (int)Math.Pow(2, o);
                float amplitude = (float)Math.Pow(Persistence, o)*AmplitudeMultiplier;

                total += InterpolatedGridNoise(Math.Abs(x)*frequency/Zoom, Math.Abs(z)*frequency/Zoom)*amplitude;
            }

            return total + VerticalOffset;
        }

        public float GetHeight(Vector3 location)
        {
            return GetHeight(location.X, location.Z);
        }

        public Vector3 GetVectorWithHeight(float x, float z)
        {
            return new Vector3(x, GetHeight(x, z), z);
        }

        public Vector3 GetVectorWithHeight(Vector3 location)
        {
            return GetVectorWithHeight(location.X, location.Z);
        }

        public Vector3 GetNormalFromFiniteOffset(float x, float z, float sampleOffset)
        {
            float hL = GetHeight(x - sampleOffset, z);
            float hR = GetHeight(x + sampleOffset, z);
            float hD = GetHeight(x, z - sampleOffset);
            float hU = GetHeight(x, z + sampleOffset);

            Vector3 normal = new Vector3(hL - hR, 2, hD - hU);
            normal.Normalize();

            return normal;
        }

        public Vector3 GetNormalFromFiniteOffset(Vector3 location, float sampleOffset)
        {
            return GetNormalFromFiniteOffset(location.X, location.Z, sampleOffset);
        }

        #endregion
    }
}
