using System.Numerics;

namespace cursooV1
{
    public class Entity
    {
        public IntPtr pawnAdress { get; set; }
        public IntPtr controllerAdress { get; set; }
        public List<Vector3> bones { get; set; }
        public List<Vector2> bones2D { get; set; }
        public Vector3 pos { get; set; }
        public Vector3 viewOffset { get; set; }
        public Vector3 origin { get; set; }
        public Vector3 view { get; set; }
        public Vector3 head { get; set; }
        public Vector3 crosshairCoordinates { get; set; }
        public Vector2 pos2D { get; set; }
        public Vector2 viewPos2D { get; set; }
        public Vector2 head2d { get; set; }
        public string name { get; set; }
        public bool spotted { get; set; }
        public uint lifeState { get; set; }
        public int entIndex { get; set; }
        public int health { get; set; }
        public int team { get; set; }
        public float distance { get; set; }
        public float pixelDistance { get; set; }

    }

    public enum BoneIds
    {
        Waist = 0,
        Neck = 5,
        Head = 6,
        ShoulderLeft = 8,
        ForeLeft = 9,
        HandLeft = 11,
        ShoulderRight = 13,
        ForeRight = 14,
        HandRight = 16,
        KneeLeft = 23,
        FeetLeft = 24,
        KneeRight = 26,
        FeetRight = 27
    }
}
