using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wakecap.Models
{
    [Table("worker")]
    public class Worker
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(200)]
        public required string Name { get; set; }

        [Column("code")]
        [MaxLength(200)]
        public required string Code { get; set; }

        public ICollection<WorkerZoneAssignment> WorkerZoneAssignments { get; set; }

    }
}
