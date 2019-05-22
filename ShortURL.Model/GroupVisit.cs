using System;
using System.ComponentModel.DataAnnotations;

namespace ShortURL.Model
{
    public class GroupVisit
    {
        [Required]
        public int GroupVisitId { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        public DateTime VisitedAt { get; set; }
    }
}
