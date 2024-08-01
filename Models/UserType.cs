using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VerifyDriversAPI.Models
{
    [Table("_user_types")]
    public class UserType
    {
        [Key]
        public int U_T_ID { get; set; }
        
        [Required]
        [StringLength(10)]
        public string U_T_description { get; set; }
    }
}
