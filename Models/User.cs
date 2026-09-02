using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VerifyDriversAPI.Models
{
    [Table("_user")]
    public class User
    {
        [Key]
        public int uID { get; set; }
        
        [Required]
        [StringLength(35)]
        public string uNames { get; set; } = string.Empty;
        
        [StringLength(6)]
        public string uGender { get; set; } = string.Empty;
        
        public int uAge { get; set; }
        
        [Column(TypeName = "decimal(3,2)")]
        public decimal uRating { get; set; }
        
        public int uVID { get; set; }
        
        public int uPartner_ID { get; set; }
        
        public int uUsertype_ID { get; set; }

        //Needs to be aaded to DB
        //public int uPlatform_ID { get; set; }

        public Vehicle? Vehicle { get; set; }
        public Partner? Partner { get; set; }
        public UserType? UserType { get; set; }

        //Need to add the platform foreign key to DB 
        //public Platform? Platform { get; set; } // Nullable
    }
}
