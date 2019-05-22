using System;
using System.ComponentModel.DataAnnotations;

namespace ShortURL.Model
{
    public class Domain
    {
        [Key]
        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        public int GroupId { get; set; }

        public Group Group { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }
    }
}
