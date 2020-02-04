using System;
using Newtonsoft.Json;

namespace CloudBook.Data
{
    public struct Registration
    {
        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("gender")]
        public char Gender { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("birthday")]
        public DateTime Birthday { get; set; }

        [JsonProperty("university")]
        public string University { get; set; }
    }
}