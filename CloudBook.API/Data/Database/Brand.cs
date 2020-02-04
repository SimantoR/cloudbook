using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBook.API.Data.Database
{
    public class Brand
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(30)]
        public string Name { get; set; }

        [MaxLength(30)]
        public string URL { get; set; }
    }
}
