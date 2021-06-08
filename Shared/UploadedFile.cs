using System;

namespace BlazorApp.Shared
{
    public class UploadedFile
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public byte[] FileContent { get; set; }
        public DateTimeOffset LastModified { get; set; }
    }
}
