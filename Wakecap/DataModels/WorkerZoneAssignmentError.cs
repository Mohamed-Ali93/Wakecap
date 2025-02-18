namespace Wakecap.DataModels
{
    public class WorkerZoneAssignmentError
    {
        public int RowNumber { get; set; }
        public WorkerZoneAssignmentRecord Data { get; set; }
        public Dictionary<string, string> Error { get; set; } = new Dictionary<string, string>();
    }
}
