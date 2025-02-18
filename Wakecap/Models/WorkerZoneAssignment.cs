using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wakecap.Models
{
    [Table("worker_zone_assignment")]
    public class WorkerZoneAssignment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("worker_id")]
        public int WorkerId { get; set; }
        public Worker Worker { get; set; }

        [Required]
        [Column("zone_id")]
        public int ZoneId { get; set; }
        public Zone Zone { get; set; }

        [Required]
        [Column("effective_date")]
        [DataType(DataType.Date)]
        public DateOnly EffectiveDate { get; set; }
    }
}
