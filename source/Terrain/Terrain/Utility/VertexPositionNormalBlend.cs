using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terrain.Utility
{
    public struct VertexPositionNormalBlend
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Blend;

        public VertexPositionNormalBlend(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;

            Blend = new Vector3(Math.Max(Math.Abs(Normal.X), 0.001f), Math.Max(Math.Abs(Normal.Y), 0.001f), Math.Max(Math.Abs(Normal.Z), 0.001f));
            float b = Blend.X + Blend.Y + Blend.Z;
            Blend.X /= b;
            Blend.Y /= b;
            Blend.Z /= b;
        }

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float)*3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(sizeof(float)*6, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
        );
    }
}
