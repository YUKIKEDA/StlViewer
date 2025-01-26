namespace StlViewer.Utilities
{
    public class StlFile
    {
        public string SolidName { get; set; } = "DefaultSolid";
        public List<Triangle> Triangles { get; set; } = [];
    }
}
