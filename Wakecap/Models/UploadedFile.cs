namespace Wakecap.Models
{
    public class UploadedFile
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; } // "Saved" or "Rejected"
        public DateTime UploadedAt { get; set; }
    }
}
