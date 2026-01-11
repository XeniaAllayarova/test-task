namespace TR.Connector.Models.Responses
{
    internal class ApiResponse<T>
    {
        public T? data { get; set; }
        public bool success { get; set; }
        public string? errorText { get; set; }
        public int? count { get; set; }
    }
}
