using System;
using Newtonsoft.Json;

namespace CloudBook.Data
{
    public struct User
    {
        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("firstname")]
        public string FirstName { get; set; }

        [JsonProperty("lastname")]
        public string LastName { get; set; }

        [JsonProperty("gender")]
        public char Gender { get; set; }

        [JsonProperty("loc")]
        public string Location { get; set; }

        [JsonProperty("birthday")]
        public DateTime Birthday { get; set; }

        [JsonProperty("uni")]
        public string University { get; set; }

        [JsonProperty("isactive")]
        public bool IsActive { get; set; }
    }
}