using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShortURL.Model
{
    public class Record
    {
        [Required]
        public int RecordId { get; set; }

        [MaxLength(255)]
        [Required]
        public string Stub { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public int Visits { get; set; }

        public DateTime? LatestVisit { get; set; }

        [Required]
        [MaxLength(255)]
        public string CreatedBy { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        [MaxLength(255)]
        [Required]
        public string Description { get; set; }

        public int? GroupId { get; set; }

        public ICollection<RecordVisit> Accesses { get; set; }

        public string Link { get; set; }
    }
}
