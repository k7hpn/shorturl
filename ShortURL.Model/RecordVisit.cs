using System;
using System.ComponentModel.DataAnnotations;

namespace ShortURL.Model
{
    public class RecordVisit
    {
        [Required]
        public int RecordVisitId { get; set; }

        [Required]
        public int RecordId { get; set; }

        [Required]
        public DateTime VisitedAt { get; set; }
    }
}
