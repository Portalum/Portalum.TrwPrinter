using Portalum.TrwPrinter.EasyPrinterS3.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Buffers;

namespace Portalum.TrwPrinter.EasyPrinterS3.Helpers
{
    public static class ImageHelper
    {
        public static ImagePrintPackage GetImagePrintPackage(byte[] imageData, bool rotate90degree)
        {
            using var image = Image.Load<Rgba32>(imageData);

            //Test on Screen
            //Ordered3x3->Bad
            //Sierra3->Bad
            //Bayer8x8->Bad
            //StevensonArce->Bad
            //SierraLite->Neutral
            //Burks>Neutral
            //Bayer16x16->Neutral
            //JarvisJudiceNinke->Neutral
            //Burks->Good
            //Atkinson->Good
            //FloydSteinberg->Good
            //Stucki->Good

            if (rotate90degree)
            {
                image.Mutate(x => x.Rotate(90));
            }

            var averageLuminance = CalculateAverageLuminance(imageData);
            if (averageLuminance < 125)
            {
                image.Mutate(x => x.Brightness(1.5f));
            }

            image.Mutate(x => x.BinaryDither(KnownDitherings.Atkinson));

            var requiredBytesPerRow = (int)Math.Ceiling(image.Width / 8.0);

            var buffer = new byte[requiredBytesPerRow * image.Height];
            var bufferIndex = 0;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var j = 0;
                    var colorBits = 0;

                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        ref Rgba32 pixel = ref pixelRow[x];
                        var isBlackPixel = (byte)((pixel.R + pixel.G + pixel.B) / 3) < 128;

                        if (isBlackPixel)
                        {
                            colorBits |= 1 << j;
                        }

                        j++;

                        if (j == 8)
                        {
                            buffer[bufferIndex] = (byte)colorBits;
                            bufferIndex++;

                            colorBits = 0;
                            j = 0;
                        }
                    }

                    if (j != 0)
                    {
                        buffer[bufferIndex] = (byte)colorBits;
                        bufferIndex++;
                    }
                }
            });

            return new ImagePrintPackage
            {
                Rows = image.Height,
                BytesPerRow = requiredBytesPerRow,
                PrintData = buffer
            };
        }

        public static double CalculateAverageLuminance(byte[] imageData)
        {
            long luma = 0;

            using var image = Image.Load<Rgba32>(imageData);

            // Use memory pooling to allocate a buffer the length of one row 
            // to house our converted luma values.
            Configuration configuration = image.GetConfiguration();
            MemoryAllocator allocator = configuration.MemoryAllocator;
            using (IMemoryOwner<L8> lumaBuffer = allocator.Allocate<L8>(image.Width))
            {
                image.ProcessPixelRows(accessor =>
                {
                    Span<L8> lumaSpan = lumaBuffer.Memory.Span;
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                        PixelOperations<Rgba32>.Instance.ToL8(configuration, pixelRow, lumaSpan);

                        // This loop could be vectorized for maximum performance.
                        for (int x = 0; x < lumaSpan.Length; x++)
                        {
                            luma += lumaSpan[x].PackedValue;
                        }
                    }
                });

            }

            // Finally calculate the average luma.
            return luma / (double)(image.Width * image.Height);
        }
    }
}
