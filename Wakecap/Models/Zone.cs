using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wakecap.Models
{
    [Table("zone")]
    public class Zone
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(200)]
        public required string Name { get; set; }

        [Required]
        [Column("code")]
        [MaxLength(200)]
        public required string Code { get; set; }
        public ICollection<WorkerZoneAssignment> WorkerZoneAssignments { get; set; }

    }
}
