using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBook.API.Data.Database
{
    public class NGram
    {
        [Key, MaxLength(3)]
        public string value { get; set; }

        [Required, Column("items")]
        public string items { get; set; }
    }
}