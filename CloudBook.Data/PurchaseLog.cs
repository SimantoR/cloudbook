using System;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CloudBook.Data
{
    public struct PurchaseLog
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("user_id")]
        public string Username { get; set; }

        [JsonProperty("sharers")]
        public string[] Sharers { get; set; }

        [JsonProperty("items")]
        public ulong[] Items { get; set; }

        [JsonProperty("creation")]
        public DateTime Time { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("receipt")]
        public string ReceiptId { get; set; }
    }
}