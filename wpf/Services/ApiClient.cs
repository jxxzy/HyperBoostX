namespace HyperBoostX.Services
{
    public sealed class ApiClient : HyperBoostBackendClient
    {
        public ApiClient(string baseUrl = "http://127.0.0.1:5000") : base(baseUrl)
        {
        }
    }
}
