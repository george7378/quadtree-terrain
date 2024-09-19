
namespace Terrain.Utility
{
    public class TerrainVertexContainer
    {
        #region Properties

        public VertexPositionNormalBlend MainVertex { get; private set; }

        public VertexPositionNormalBlend? CrackFixVertex { get; set; }

        #endregion

        #region Constructors

        public TerrainVertexContainer(VertexPositionNormalBlend mainVertex)
        {
            MainVertex = mainVertex;
        }

        #endregion
    }
}
