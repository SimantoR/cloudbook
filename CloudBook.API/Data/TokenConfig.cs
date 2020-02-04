using System.Collections.Generic;

namespace CloudBook.API.Data
{
    public class TokenConfig
    {
        public string Issuer { get; set; }

        public string Audience { get; set; }

        public int Validity { get; set; }

        public string Key { get; set; }

        public byte[] GetKey() => System.Text.Encoding.UTF8.GetBytes(Key);
    }
}