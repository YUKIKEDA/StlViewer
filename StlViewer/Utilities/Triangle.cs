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
            Normal = new Vector3(0, 0, 0);
            Vertex1 = new Vector3(0, 0, 0);
            Vertex2 = new Vector3(0, 0, 0);
            Vertex3 = new Vector3(0, 0, 0);
        }
    }
}
