using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class LutMerger
{
    public static Image<Rgba32>? Merge(Image<Rgba32>? existing, Image<Rgba32> incoming)
    {
        if (existing is not null && existing.Height != incoming.Height)
            return null;

        int height = incoming.Height;

        var columns = new List<Rgba32[]>();
        var seen    = new HashSet<string>();

        if (existing is not null)
        {
            AppendColumns(existing, height, columns, seen, dedupe: false);
            AppendColumns(incoming, height, columns, seen, dedupe: true);
        }
        else
        {
            AppendColumns(incoming, height, columns, seen, dedupe: false);
        }

        var result = new Image<Rgba32>(columns.Count, height);
        for (int x = 0; x < columns.Count; x++)
        {
            var col = columns[x];
            for (int y = 0; y < height; y++)
                result[x, y] = col[y];
        }

        return result;
    }

    private static void AppendColumns(
        Image<Rgba32> image, int height, List<Rgba32[]> columns, HashSet<string> seen, bool dedupe)
    {
        for (int x = 0; x < image.Width; x++)
        {
            var col = new Rgba32[height];
            for (int y = 0; y < height; y++)
                col[y] = image[x, y];

            var key = ColumnKey(col);
            if (dedupe && seen.Contains(key))
                continue;

            seen.Add(key);
            columns.Add(col);
        }
    }

    private static string ColumnKey(Rgba32[] column) =>
        Convert.ToBase64String(MemoryMarshal.AsBytes(column.AsSpan()));
}
