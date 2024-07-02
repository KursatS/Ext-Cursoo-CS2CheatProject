using System.Numerics;

namespace Cs2Cheats
{
    public class Entity
    {
        public Vector3 pos { get; set; }
        public Vector3 viewOffset { get; set; }
        public Vector2 pos2D { get; set; }
        public Vector2 viewPos2D { get; set; }
        public IntPtr pawnAdress { get; set; }
        public IntPtr controllerAdress { get; set; }
        public Vector3 origin { get; set; }
        public Vector3 view { get; set; }
        public Vector3 head { get; set; }
        public int health { get; set; }
        public int team { get; set; }
        public uint lifeState { get; set; }
        public float distance { get; set; }

    }
}
