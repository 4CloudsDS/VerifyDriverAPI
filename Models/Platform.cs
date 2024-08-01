using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VerifyDriversAPI.Models
{
    [Table("_platform")]
    public class Platform
    {
        [Key]
        public int pID { get; set; }
        
        [Required]
        [StringLength(10)]
        public string pName { get; set; }
        
        [StringLength(20)]
        public string pType { get; set; }
    }
}
