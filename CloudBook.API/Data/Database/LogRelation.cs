using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBook.API.Data.Database
{
    /// A relationship between a purchase log and a user
    public class LogRelation
    {
        [Key]
        public Guid Id { get; set; }

        [Required, Column("purc_log_id")]
        private UInt64 purchaseLogId { get; set; }

        [Required, Column("user_id")]
        private string userId { get; set; }

        [Required, DefaultValue(0.0)]
        public double Amount { get; set; }

        [ForeignKey("purc_log_id")]
        public virtual PurchaseLog PurchaseLog { get; set; }

        [ForeignKey("user_id")]
        public virtual User User { get; set; }

        public LogRelation() { }

        public LogRelation(UInt64 purchaselog_id, string user_id, double amount) {
            purchaseLogId = purchaselog_id;
            userId = user_id;
            Amount = amount;
        }

        public LogRelation(ref PurchaseLog purchaseLog, User user, double amount) {
            PurchaseLog = purchaseLog;
            User = user;
            Amount = amount;
        }
    }
}