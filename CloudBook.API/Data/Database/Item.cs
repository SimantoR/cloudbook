using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBook.API.Data.Database
{
    public class Item
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(15)]
        public string Name { get; set; }

        [Required]
        public float Quantity { get; set; }

        [MaxLength(9)]
        public UInt32 BarCode { get; set; }

        [Required, Column("brand_id")]
        private Guid _brandId { get; set; }

        [ForeignKey("brand_id")]
        public virtual Brand Brand { get; set; }
    }
}