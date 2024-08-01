using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VerifyDriversAPI.Models
{
    [Table("_partner")]
    public class Partner
    {
        [Key]
        public int pID { get; set; }
        
        [Required]
        [StringLength(35)]
        public string pName { get; set; }

        // This model needs to change so that it only contais 
        // public int pID (partner ID)
        // public int pUID { get; set; }
        // public User? User { get; set; } // Make nullable

    }
}
