using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VerifyDriversAPI.Models
{
    [Table("_vehicle")]
    public class Vehicle
    {
        [Key]
        public int vID { get; set; }
        
        [Required]
        [StringLength(10)]
        public string vregistration { get; set; }
        
        [StringLength(20)]
        public string vMake { get; set; }
        
        [StringLength(25)]
        public string vModel_name { get; set; }
        
        [StringLength(4)]
        public string vModel_year { get; set; }
        
        public int vPlatform_ID { get; set; }
        
        public int vPartner_ID { get; set; }

        // Navigation properties
        public Platform? Platform { get; set; } // Make nullable
        public Partner? Partner { get; set; } // Make nullable
    }
}
