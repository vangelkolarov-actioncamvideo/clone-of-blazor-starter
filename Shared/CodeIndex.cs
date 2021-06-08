using Microsoft.Azure.Cosmos.Table;

namespace BlazorApp.Shared
{
    public class CodeIndex : TableEntity
    {
        public int DownloadCount { get; set; }
        public int PlaybackCount { get; set; }
        public string Filename { get; set; }
    }
}
