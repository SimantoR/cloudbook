using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CloudBook.Data;
using Newtonsoft.Json;

namespace CloudBook.API.Data.Database
{
    public class ItemLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required, Column("location")]
        public string Location { get; set; }  // Location of the itemlog being made

        [Required, Column("price"), DefaultValue(0.0)]
        public double Price { get; set; }  // Price of the item

        [Required, Column("log_id")]
        public Guid PurchaseLogId { get; set; } // Private ref of the purchase log, only for db table

        [Required, Column("item_id")]
        public UInt32 ItemId { get; set; }    // Reference to item that is being added through the purchase log, only for db table

        [ForeignKey("log_id")]
        public virtual PurchaseLog PurchaseLog { get; set; }    // Virtual reference to purchase log, ultimately only _purchaseLogId will be stored

        [ForeignKey("item_id")]
        public virtual Item Item { get; set; }  // Virtual ref of Item, ultimate the ID of the item itself will be stored in _itemId
    }
}
