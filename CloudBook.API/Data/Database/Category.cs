using System;
using System.ComponentModel.DataAnnotations;

namespace CloudBook.API.Data.Database
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Left most limit
        /// </summary>
        [Required]
        public int Lft { get; set; }

        /// <summary>
        /// Right most limit
        /// </summary>
        [Required]
        public int Rgt { get; set; }

        /// <summary>
        /// Name of the category
        /// </summary>
        public string CategoryName { get; set; }

        public Category() => Id = Guid.NewGuid();
    }
}
