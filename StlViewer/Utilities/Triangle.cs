using System.Numerics;

namespace StlViewer.Utilities
{
    public class Triangle
    {
        public Vector3 Normal { get; set; }
        public Vector3 Vertex1 { get; set; }
        public Vector3 Vertex2 { get; set; }
        public Vector3 Vertex3 { get; set; }

        public Triangle()
        {
            Normal = Vector3.Zero;
            Vertex1 = Vector3.Zero;
            Vertex2 = Vector3.Zero;
            Vertex3 = Vector3.Zero;
        }
    }
}
