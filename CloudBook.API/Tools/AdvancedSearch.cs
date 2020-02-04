using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using CloudBook.API.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace CloudBook.API.Tools
{
    public static class AdvancedSearch
    {
        public static string[] GenerateTrigrams(ref string source) => GenerateNGrams(ref source, 3);

        public static string[] SearchItem(string query, DbSet<NGram> set, byte n = 3)
        {
            string[] query_tGrams = GenerateNGrams(ref query, n);
            Dictionary<string, byte> dataSet = new Dictionary<string, byte>();

            for (int j = 0; j < query_tGrams.Length; j++)
            {
                string[] matches = (from x in set where x.value == query_tGrams[j] select x.items).Single().Split(':', StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < matches.Length; i++)
                {
                    if (dataSet.ContainsKey(matches[i]))
                        dataSet[matches[i]] += 1;
                    else
                        dataSet.Add(matches[i], 1);
                }
            }

            return (from x in dataSet orderby x.Value select x.Key).Take(3).ToArray();
        }

        // Imcomplete
        public static string[] SearchUser(string query, DbSet<User> set, byte n = 3) => new string[] { };

        private static string[] GenerateNGrams(ref string source, byte n = 3)
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

        private static List<string[]> GenerateNGrams(ref string[] sources, byte n = 3)
        {
            List<string> trigrams = new List<string>();
            StringBuilder strBuilder = new StringBuilder(n);
            List<string[]> output = new List<string[]>();

            int len = 0;

            // Loop through each of the source strings
            for (int k = 0; k < sources.Length; k++)
            {
                // Get a read-only span of chars in the specific source string
                ReadOnlySpan<char> span = sources[k].AsSpan();

                // Get reference of the length, just saving processing
                len = span.Length;

                // Loop through each of the character of the selected source string
                for (int j = 0; j < len - 1; j++)
                {
                    // Loop through as many letters as n_gram requires to build the n_grams
                    for (int i = 0; i <= n; i++)
                    {
                        if (i + j < len)
                            strBuilder.Append(span[i + j]);
                        else if (strBuilder[n - 2] != '_')
                            strBuilder.Append('_');
                    }
                    // Add the trigram to the list of trigrams for the selected source
                    trigrams.Append(strBuilder.ToString());
                    // Clear the string builder for reuse
                    strBuilder.Clear();
                }
                // Once all the trigrams for a selected string are found, append it to the output list of string arrays
                output.Append(trigrams.ToArray());

                // Clear the trigram list to be used for next source
                trigrams.Clear();
            }

            // Mark things for Garbage Collector
            strBuilder = null;
            trigrams = null;

            return output;
        }
    }
}