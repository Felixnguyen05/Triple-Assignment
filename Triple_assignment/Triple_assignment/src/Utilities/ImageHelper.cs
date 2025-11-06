using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Linq;

public static class ImageHelper
{
    private static readonly Font font;

    static ImageHelper()
    {
        var family = SystemFonts.Get("Arial");
        font = family.CreateFont(28);
    }

    public static Image DrawWeatherOnImage(Image baseImage, string stationName, string weatherText)
    {
        var clone = baseImage.Clone(ctx =>
        {
            ctx.Fill(new DrawingOptions
            {
                GraphicsOptions = new GraphicsOptions { BlendPercentage = 0.35f }
            }, Color.Black);
        });

        clone.Mutate(ctx =>
        {
            ctx.DrawText($"{stationName}\n{weatherText}", font, Color.White, new PointF(20, 20));
        });

        return clone;
    }
}
