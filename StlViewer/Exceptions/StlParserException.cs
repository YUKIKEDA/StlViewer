namespace StlViewer.Exceptions
{
    public class StlParserException : Exception
    {
        public StlParserException(string message) : base(message) { }
        public StlParserException(string message, Exception innerException) : base(message, innerException) { }
    }
}
