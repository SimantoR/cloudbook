using System.Linq;
using System.Text;

namespace System.Security.Cryptography
{
    public static class TinyCrypt
    {
        private static byte[] _cryptoKey;
        private const byte CRYPTO_KEY_LENGTH = 16;
        private const byte MIN_ENCRYPTION_LENGTH = 6;
        
        // Force the key to be exactly 16 bit long
        private static void VerifyKey(ref string key)
        {
            if (key.Length != CRYPTO_KEY_LENGTH)
                throw new ArgumentException("Invalid length of key", "cryptoKey");
            else
                _cryptoKey = Encoding.UTF8.GetBytes(key);
        }

        /// <summary>
        /// Encryption using corrected Block TEA (xxtea) algorithm
        /// </summary>
        /// <param name="source">String to be encrypted (multi-byte safe)</param>
        /// <returns>Encrypted string with minimum of 48bit output</returns>
        public static string Encrypt(string source, string key)
        {
            // Prepare key to be exactly what TEA requres
            VerifyKey(ref key);

            // Ensure text size matches MIN_ENCRYPTION_LENGTH and convert to UTF8 encoded data
            byte[] textBytes = GetBytesForEncryption(source);

            // Convert the source to unsigned 32bit integar array
            UInt32[] v = ToLongs(textBytes);

            // Convert first 16 chars of password as key
            UInt32[] k = ToLongs(_cryptoKey);

            // Use UInt32 as the original is based on 'unsigned long' in C
            UInt32 n = (UInt32)v.Length,
                   z = v[n - 1],
                   y = v[0],
                   delta = 0x9e3779b9,
                   e,
                   q = 6 + (52 / n),
                   sum = 0,
                   p = 0;

            while (q-- > 0)
            {
                sum += delta;
                e = sum >> 2 & 3;

                for (p = 0; p < (n - 1); p++)
                {
                    y = v[(p + 1)];
                    z = v[p] += (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z);
                }

                y = v[0];
                z = v[n - 1] += (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z);
            }

            // Convert to Base64 so that Control characters don't break it
            return Convert.ToBase64String(ToBytes(v));
        }

        private static byte[] GetBytesForEncryption(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);

            if (bytes.Length < MIN_ENCRYPTION_LENGTH)
            {
                var incBytes = Enumerable.Repeat((byte)0, MIN_ENCRYPTION_LENGTH).ToArray();
                Buffer.BlockCopy(bytes, 0, incBytes, 0, bytes.Length);
                bytes = incBytes;
            }
            return bytes;
        }

        /// <summary>
        /// Decryption using Corrected Block TEA (xxtea) algorithm
        /// </summary>
        /// <param name="encrypted_source">String to be decrypted</param>
        /// <reutrns>Decrypted String</returns>
        public static string Decrypt(string encrypted_source, string key)
        {
            if (encrypted_source == string.Empty || encrypted_source.Equals(null))       // If the provided source is empty/null
                throw new ArgumentNullException("encrypted_source");
            else if (key == string.Empty || key.Equals(null))   // If the provided key is empty/null
                throw new ArgumentNullException("key");

            VerifyKey(ref key);
            UInt32[] v = ToLongs(Convert.FromBase64String(encrypted_source));
            UInt32[] k = ToLongs(_cryptoKey);

            UInt32 n = (UInt32)v.Length,
                   y = v[0],
                   z = v[n - 1],
                   delta = 0b1001_1110_0011_0111_0111_1001_1011_1001,
                   e = 0,
                   q = (UInt32)(6 + (52 / n)),
                   sum = q * delta,
                   p = 0;

            while (sum != 0)
            {
                e = sum >> 2 & 3;

                for (p = (n - 1); p > 0; p--)
                {
                    z = v[p - 1];
                    y = v[p] -= (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z);
                }

                z = v[n - 1];
                y = v[0] -= (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z);

                sum -= delta;
            }

            var plain_text = Encoding.UTF8.GetString(ToBytes(v)).TrimEnd('\0');
            return plain_text;
        }

        /// <summary>
        /// Convert array of unsigned bytes to array of unsigned 32bit integars
        /// </summary>
        /// <param name="byte_data">Array of unsigned bytes to be converted to array of unsigned 32bit integars</param>
        /// <returns>An array of unsigned integars</returns>
        private static UInt32[] ToLongs(byte[] byte_data)
        {
            // note chars must be within ISO-8859-1 (with Unicode code-point < 256) to fit 4/long
            UInt32[] bytes = new uint[(ushort)Math.Ceiling(((decimal)byte_data.Length / 4))];

            // Create an array of long, each long holding the data of 4 characters, if the last block is less than 4
            // characters in length, fill with ascii null values
            for (UInt16 i = 0; i < bytes.Length; i++)
            {
                // Note: little-endian encoding - endianness is irrelevant as long as it is the same in ToBytes()
                bytes[i] = ((byte_data[i * 4])) +
                       ((i * 4 + 1) >= byte_data.Length ? (UInt32)0 << 8 : ((UInt32)byte_data[i * 4 + 1] << 8)) +
                       ((i * 4 + 2) >= byte_data.Length ? (UInt32)0 << 16 : ((UInt32)byte_data[i * 4 + 2] << 16)) +
                       ((i * 4 + 3) >= byte_data.Length ? (UInt32)0 << 24 : ((UInt32)byte_data[i * 4 + 3] << 24));
            }

            return bytes;
        }

        /// <summary>
        /// Convert array of longs back to utf-8 byte array
        /// </summary>
        /// <returns></returns>
        private static byte[] ToBytes(UInt32[] l)
        {
            byte[] b = new byte[l.Length * 4];

            // Split each long value into 4 separate characters (bytes) using the same format as ToLongs()
            for (int i = 0; i < l.Length; i++)
            {
                b[(i * 4)] = (byte)(l[i] & 0xFF);
                b[(i * 4) + 1] = (byte)(l[i] >> (8 & 0xFF));
                b[(i * 4) + 2] = (byte)(l[i] >> (16 & 0xFF));
                b[(i * 4) + 3] = (byte)(l[i] >> (24 & 0xFF));
            }
            return b;
        }

    }
}