using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;
using Wakecap.Data;
using Wakecap.DataModels;
using Wakecap.Models;
using Wakecap.Validators;


namespace Wakecap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssignmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly WorkerZoneAssignmentValidator _validator;
        private readonly ILogger<AssignmentController> _logger;

        public AssignmentController(
            ApplicationDbContext context,
            WorkerZoneAssignmentValidator validator,
            ILogger<AssignmentController> logger)
        {
            _context = context;
            _validator = validator;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file uploaded.");
                return BadRequest("No file uploaded.");
            }

            if (file.Length > 50_000 * 50) // ~50,000 rows
            {
                _logger.LogWarning("File exceeds maximum allowed size.");
                return BadRequest("File exceeds maximum allowed size.");
            }

            List<WorkerZoneAssignmentRecord> records;
            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    
                    records = csv.GetRecords<WorkerZoneAssignmentRecord>().ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV parsing failed");
                return BadRequest("Invalid file format");
            }

            if (records.Count > 50000)
            {
                _logger.LogWarning("File exceeds 50,000 row limit");
                return BadRequest("Maximum 50,000 rows allowed");
            }


            // Fetch mappings to use in validation
            var (workerCodes, zoneCodes) = await GetDatabaseMappings();

            // Validate records
            var errors = _validator.Validate(records,workerCodes,zoneCodes);
            if (errors.Any())
            {
                await SaveUploadStatus(file.FileName, "Rejected");
                return BadRequest(errors);
            }


            // Prepare valid assignments
            var assignments = records
                .Where(r => workerCodes.ContainsKey(r.worker_code) &&
                            zoneCodes.ContainsKey(r.zone_code))
                .Select(r => new WorkerZoneAssignment
                {
                    WorkerId = workerCodes[r.worker_code],
                    ZoneId = zoneCodes[r.zone_code],
                    EffectiveDate = DateOnly.ParseExact(
                        r.assignment_date,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture)
                })
                .ToList();

            try
            {
                await using var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync();
                await _context.Database.UseTransactionAsync(transaction);

                // Bulk insert
                // Official Npgsql bulk insert
                await using (var writer = await connection.BeginBinaryImportAsync(
                    "COPY worker_zone_assignment (worker_id, zone_id, effective_date) FROM STDIN (FORMAT BINARY)"))
                {
                    foreach (var assignment in assignments)
                    {
                        await writer.StartRowAsync();
                        await writer.WriteAsync(assignment.WorkerId);
                        await writer.WriteAsync(assignment.ZoneId);
                        await writer.WriteAsync(assignment.EffectiveDate);
                    }
                    await writer.CompleteAsync();
                }
                // Save upload status
                await SaveUploadStatus(file.FileName, "Saved");
                await transaction.CommitAsync();

                _logger.LogInformation("Processed {RecordCount} records", assignments.Count);
                return Ok("File processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk insert failed");
                return StatusCode(500, "Processing error");
            }
        }

        private async Task SaveUploadStatus(string fileName, string status)
        {
            //TODO: we can save the file in any file storage if required 

            await _context.UploadedFiles.AddAsync(new UploadedFile
            {
                FileName = fileName,
                Status = status,
                UploadedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private async Task<(
            Dictionary<string, int> workerCodes,
            Dictionary<string, int> zoneCodes
            )> GetDatabaseMappings()
        {
            var workerCodes = await _context.Workers
                .AsNoTracking()
                .Select(w => new { w.Code, w.Id })
                .ToDictionaryAsync(w => w.Code, w => w.Id);

            var zoneCodes = await _context.Zones
                .AsNoTracking()
                .Select(z => new { z.Code, z.Id })
                .ToDictionaryAsync(z => z.Code, z => z.Id);

            return (workerCodes, zoneCodes);
        }
    }
}