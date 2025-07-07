using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShortURL.Model
{
    public class Group
    {
        [Required]
        public DateTime CreatedOn { get; set; }

        public string DefaultLink { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public ICollection<Domain> Domains { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        public bool IsDefault { get; set; }

        public DateTime? LatestVisit { get; set; }

        public ICollection<Record> Records { get; set; }

        [Required]
        public int Visits { get; set; }
    }
}