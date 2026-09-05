using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VerifyDriversAPI.Models
{
    [Table("_comments")]
    public class Comment
    {
        [Key]
        public int cID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string cText { get; set; } = string.Empty;
        
        public DateTime cDateTime { get; set; }
        
        public int c_Uid { get; set; }
        
        public int c_Pid { get; set; }

        public User? User { get; set; }
        public Partner? Partner { get; set; }
    }
}
