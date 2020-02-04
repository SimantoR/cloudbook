using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CloudBook.API.Tools;
using CloudBook.API.Data.Database;

namespace CloudBook.API.Controllers
{
    [Route("search"), Authorize]
    public class SearchController : ControllerBase
    {
        private IdentityStore _database;

        public SearchController(IdentityStore database) => _database = database;

        [HttpGet("item")]
        public IActionResult SearchItem(string q)
        {
            string[] q_grams = GenerateNGrams(q.AsSpan(), 3);
            Dictionary<string, byte> dataSet = new Dictionary<string, byte>();

            for (int i = 0; i < q_grams.Length; i++)
            {
                string[] words = (
                    from arg in _database.NGrams
                    where arg.value == q_grams[i]
                    select arg.value
                ).Single().Split(':', StringSplitOptions.RemoveEmptyEntries);

                for (int j = 0; j < words.Length; j++)
                {
                    if (dataSet.ContainsKey(words[j]))
                        dataSet[words[j]] += 1;
                    else
                        dataSet.Add(words[j], 1);
                }
            }

            dataSet = (from data in dataSet orderby data.Value select data).ToDictionary(x => x.Key, x => x.Value);

            return Content(dataSet.Select(x => x.Key).Take(3).ToJson());
        }

        public IActionResult SearchUser(string q) => Ok();

        private string[] GenerateNGrams(ReadOnlySpan<char> source, byte n)
        {
            // A minor algorithm adjustment
            n = n >= 3 ? (byte)(n - 1) : n;

            // Length of the source kept as reference
            int len = source.Length;

            // A container to hold all the trigrams
            string[] trigrams = new string[source.Length - 1];

            // Using a string builder to reduce memory
            StringBuilder strBuilder = new StringBuilder(n);

            // Iterate through each letter
            for (int i = 0; i < len - 1; i++)
            {
                // Iterate through 3 letters from the i-th index
                for (int j = 0; j <= n; j++)
                {
                    if (i + j < len)
                        strBuilder.Append(source[i + j]);
                    else if (strBuilder[n - 2] != '_')
                        strBuilder.Append('_');
                }
                trigrams[i] = strBuilder.ToString();
            }

            return trigrams;
        }
    }
}