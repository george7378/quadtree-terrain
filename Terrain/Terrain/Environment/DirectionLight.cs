using Microsoft.Xna.Framework;

namespace Terrain.Environment
{
    public class DirectionLight
    {
        #region Properties

        public Vector3 Direction { get; set; }

        public float Power { get; set; }

        public float AmbientPower { get; set; }

        #endregion

        #region Constructors

        public DirectionLight(Vector3 direction, float power, float ambientPower)
        {
            Direction = direction;
            Power = power;
            AmbientPower = ambientPower;
        }

        #endregion
    }
}
