using Microsoft.Azure.Cosmos.Table;

namespace BlazorApp.Shared
{
    public class IndexAction : TableEntity
    {
        public string BrowserUserAgent { get; set; }
        public string UserActionType { get; set; }
        public string IpAddress { get; set; }
    }
}
