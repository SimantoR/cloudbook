using System;
using System.Linq;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;

public static class Extension
{

    public static IQueryable<T> Page<T>(this IQueryable<T> @this, int page, int page_size)
        => @this.Skip((page <= 1) ? 0 : (page - 1) * page_size).Take(page_size);

    /// <summary>
    /// Converts the given object
    /// </summary>
    public static string ToJson(this object @this) => JsonConvert.SerializeObject(@this);

    /// <summary>
    /// Converts the string into object of type T
    /// </summary>
    /// <returns>Returns an object instance of type T</returns>
    public static T FromJson<T>(this string @this) => JsonConvert.DeserializeObject<T>(@this);

    #region ImageSharp extensions

    public static void RemoveGreenScreen(this Image<Rgba32> img, System.IO.Stream output)
    {
        // Manipulate pixel data here

        for (int i = 0; i < img.Height; i++)
        {
            var span = img.GetPixelRowSpan(i);
            for (int j = 0; j < span.Length; j++)
            {
                ref Rgba32 pixel = ref span[j];

                float x = (pixel.R + pixel.B) - pixel.G;

                pixel.A = (x <= 0) ? (byte)0xFF : (byte)0x0;
            }
        }

        img.Save(output, ImageFormats.Png);
    }

    public static void RemoveGreenScreen(this Image<Rgba32> img, string output)
    {
        for (int i = 0; i < img.Height; i++)
        {
            var span = img.GetPixelRowSpan(i);
            for (int j = 0; j < span.Length; j++)
            {
                var pixel = span[j];

                float x = (pixel.R + pixel.B) - pixel.G;

                pixel.A = (x <= 0) ? (byte)0xFF : (byte)0x0;
            }
        }

        img.Save(output);
    }

    #endregion

    #region BitWise

    public static ulong RotateLeft(this ulong original, int bits) => (original << bits) | (original >> (64 - bits));

    public static ulong RotateRight(this ulong original, int bits) => (original >> bits) | (original << (64 - bits));

    unsafe public static ulong GetUInt64(this byte[] bb, int pos)
    {
        // We only read aligned longs, so a simple casting is enough
        fixed (byte* p_byte = &bb[pos])
        {
            return *((ulong*)p_byte);
        }
    }

    #endregion
}