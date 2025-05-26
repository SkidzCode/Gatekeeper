namespace GateKeeper.Server.Models.Common
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string? Details { get; set; }
        public string? TraceId { get; set; }
    }
}
