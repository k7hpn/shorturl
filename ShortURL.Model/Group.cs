using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShortURL.Model
{
    public class Group
    {
        [Required]
        public int GroupId { get; set; }

        [Required]
        public bool IsDefault { get; set; }

        [Required]
        public int Visits { get; set; }

        public DateTime? LatestVisit { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public ICollection<Domain> Domains { get; set; }
        public ICollection<Record> Records { get; set; }

        public string DefaultLink { get; set; }
    }
}
