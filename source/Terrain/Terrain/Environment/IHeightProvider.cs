using Microsoft.Xna.Framework;

namespace Terrain.Environment
{
    public interface IHeightProvider
    {
        float GetHeight(float x, float z);

        float GetHeight(Vector3 location);

        Vector3 GetVectorWithHeight(float x, float z);

        Vector3 GetVectorWithHeight(Vector3 location);

        Vector3 GetNormalFromFiniteOffset(float x, float z, float sampleOffset);

        Vector3 GetNormalFromFiniteOffset(Vector3 location, float sampleOffset);
    }
}
