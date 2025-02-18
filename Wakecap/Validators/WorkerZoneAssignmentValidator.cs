using System.Collections.Concurrent;
using System.Globalization;
using Wakecap.Data;
using Wakecap.DataModels;

namespace Wakecap.Validators
{
    public class WorkerZoneAssignmentValidator
    {
        private readonly ApplicationDbContext _context;

        public WorkerZoneAssignmentValidator(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<WorkerZoneAssignmentError> Validate(List<WorkerZoneAssignmentRecord> records
            ,Dictionary<string, int> workerCodes,
            Dictionary<string, int> zoneCodes)
        {
            var errors = new ConcurrentBag<WorkerZoneAssignmentError>();

            //// Fetch worker and zone codes
            //var workerCodes = _context.Workers
            //    .Select(w => new { w.Id, w.Code })
            //    .ToDictionary(w => w.Code, w => w.Id);

            //var zoneCodes = _context.Zones
            //    .Select(z => new { z.Id, z.Code })
            //    .ToDictionary(z => z.Code, z => z.Id);

            // Fetch existing assignments as anonymous type
            var existingAssignments = _context.WorkerZoneAssignments
                .Select(a => new { a.WorkerId, a.EffectiveDate })
                .ToList();

            // Convert to ValueTuple AFTER materialization
            var existingAssignmentSet = existingAssignments
                .Select(a => (a.WorkerId, a.EffectiveDate))
                .ToHashSet();

            // Track duplicates in the file
            var fileAssignments = new ConcurrentDictionary<(string, DateOnly), bool>();

            Parallel.ForEach(records, (record, state, index) =>
            {
                var error = new WorkerZoneAssignmentError { RowNumber = (int)index + 1, Data = record };

                // Declare workerId and zoneId outside the validation blocks
                int workerId = 0;
                int zoneId = 0;

                // Validate Worker Code
                if (string.IsNullOrEmpty(record.worker_code))
                    error.Error["WorkerCode"] = "Worker Code is required.";
                else if (record.worker_code.Length > 10)
                    error.Error["WorkerCode"] = "Worker Code exceeds 10 characters.";
                else if (!workerCodes.TryGetValue(record.worker_code, out workerId))
                    error.Error["WorkerCode"] = "Worker Code does not exist.";

                // Validate Zone Code
                if (string.IsNullOrEmpty(record.zone_code))
                    error.Error["ZoneCode"] = "Zone Code is required.";
                else if (record.zone_code.Length > 10)
                    error.Error["ZoneCode"] = "Zone Code exceeds 10 characters.";
                else if (!zoneCodes.TryGetValue(record.zone_code, out zoneId))
                    error.Error["ZoneCode"] = "Zone Code does not exist.";

                // Validate Effective Date
                if (!DateOnly.TryParseExact(record.assignment_date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var effectiveDate))
                    error.Error["EffectiveDate"] = "Invalid date format.";
                else if (effectiveDate <= DateOnly.FromDateTime( DateTime.Today))
                    error.Error["EffectiveDate"] = "Effective Date must be in the future.";

                // Check for duplicates in the file (only if date is valid)
                if (effectiveDate > DateOnly.FromDateTime( DateTime.Today))
                {
                    var assignmentKey = (record.worker_code, effectiveDate);
                    if (!fileAssignments.TryAdd(assignmentKey, true))
                        error.Error["RowError"] = "Duplicate row in file.";
                }

                // Check conflicts with existing assignments (only if workerId is valid)
                if (workerId != 0 && existingAssignmentSet.Contains((workerId, effectiveDate)))
                    error.Error["RowError"] = "Assignment already exists in worker_zone_assignment table.";

                if (error.Error.Any())
                    errors.Add(error);
            });

            return errors.ToList();
        }
    }
}