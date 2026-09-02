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
        public string vregistration { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string vMake { get; set; } = string.Empty;
        
        [StringLength(25)]
        public string vModel_name { get; set; } = string.Empty;
        
        [StringLength(4)]
        public string vModel_year { get; set; } = string.Empty;
        
        public int vPlatform_ID { get; set; }
        
        public int vPartner_ID { get; set; }

        public Platform? Platform { get; set; }
        public Partner? Partner { get; set; }
    }
}
