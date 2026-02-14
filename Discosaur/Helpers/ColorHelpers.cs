using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;

namespace Discosaur.Helpers;

public static class ColorHelpers
{
    public static async Task<Color> GetDominantColorAsync(string imagePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(imagePath);
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);

        var transform = new BitmapTransform
        {
            ScaledWidth = 300,
            ScaledHeight = 300,
            InterpolationMode = BitmapInterpolationMode.Linear
        };

        var pixelData = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform,
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);

        var pixels = pixelData.DetachPixelData();

        var colorCounts = new Dictionary<uint, (int Count, float SatSum)>();
        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte b = pixels[i];
            byte g = pixels[i + 1];
            byte r = pixels[i + 2];
            byte a = pixels[i + 3];

            // Skip transparent pixels
            if (a < 128) continue;

            // Skip near-black and near-white pixels
            float luminance = (0.2126f * r + 0.7152f * g + 0.0722f * b) / 255f;
            if (luminance < 0.05f || luminance > 0.95f) continue;

            // Quantize to reduce color space
            byte qr = (byte)(r & 0xF0);
            byte qg = (byte)(g & 0xF0);
            byte qb = (byte)(b & 0xF0);
            uint key = ((uint)qr << 16) | ((uint)qg << 8) | qb;

            // Calculate saturation for this pixel
            float max = Math.Max(r, Math.Max(g, b)) / 255f;
            float min = Math.Min(r, Math.Min(g, b)) / 255f;
            float sat = max == 0 ? 0 : (max - min) / max;

            var existing = colorCounts.GetValueOrDefault(key);
            colorCounts[key] = (existing.Count + 1, existing.SatSum + sat);
        }

        if (colorCounts.Count == 0)
        {
            // Fallback if everything was filtered out
            return Color.FromArgb(255, 128, 128, 128);
        }

        // Score = count * (1 + average_saturation), so vivid colors beat dull ones
        var dominant = colorCounts.MaxBy(kv =>
            kv.Value.Count * (1.0f + kv.Value.SatSum / kv.Value.Count)).Key;

        return Color.FromArgb(255,
            (byte)((dominant >> 16) & 0xFF),
            (byte)((dominant >> 8) & 0xFF),
            (byte)(dominant & 0xFF));
    }

    /// <summary>
    /// Calculates relative luminance per WCAG 2.x specification.
    /// </summary>
    public static double GetRelativeLuminance(Color c)
    {
        double R = c.R / 255.0;
        double G = c.G / 255.0;
        double B = c.B / 255.0;

        R = R <= 0.04045 ? R / 12.92 : Math.Pow((R + 0.055) / 1.055, 2.4);
        G = G <= 0.04045 ? G / 12.92 : Math.Pow((G + 0.055) / 1.055, 2.4);
        B = B <= 0.04045 ? B / 12.92 : Math.Pow((B + 0.055) / 1.055, 2.4);

        return 0.2126 * R + 0.7152 * G + 0.0722 * B;
    }

    /// <summary>
    /// WCAG contrast ratio between two colors (range 1:1 to 21:1).
    /// </summary>
    public static double GetContrastRatio(Color a, Color b)
    {
        double lumA = GetRelativeLuminance(a);
        double lumB = GetRelativeLuminance(b);
        double lighter = Math.Max(lumA, lumB);
        double darker = Math.Min(lumA, lumB);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Derives a text color from the background by preserving hue,
    /// desaturating slightly, and searching for a lightness that meets
    /// the target WCAG contrast ratio.
    /// </summary>
    public static Color GetTextColor(Color background, double targetContrast = 7.0)
    {
        var (h, s, l) = ToHsl(background);

        // Desaturate the text a bit so it doesn't fight the background
        float textSat = s * 0.25f;

        // Decide which direction to search: if background is dark, go lighter; otherwise darker
        bool goLight = l < 0.5f;
        float bestL = goLight ? 1.0f : 0.0f;
        float step = 0.01f;

        // Walk from the background lightness outward until we hit the contrast target
        float searchStart = goLight ? l + 0.1f : l - 0.1f;
        searchStart = Math.Clamp(searchStart, 0f, 1f);

        if (goLight)
        {
            for (float tl = searchStart; tl <= 1.0f; tl += step)
            {
                var candidate = FromHsl(h, textSat, tl);
                if (GetContrastRatio(background, candidate) >= targetContrast)
                {
                    bestL = tl;
                    break;
                }
            }
        }
        else
        {
            for (float tl = searchStart; tl >= 0.0f; tl -= step)
            {
                var candidate = FromHsl(h, textSat, tl);
                if (GetContrastRatio(background, candidate) >= targetContrast)
                {
                    bestL = tl;
                    break;
                }
            }
        }

        return FromHsl(h, textSat, bestL);
    }

    public static Color GetSecondaryTextColor(Color background)
    {
        // WCAG AA large-text minimum is 3:1, good for secondary/caption text
        return GetTextColor(background, targetContrast: 4.5);
    }

    public static double GetLuminance(Color c)
    {
        return (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
    }

    public static bool IsDark(Color c) => GetLuminance(c) <= 0.179;

    public static (float H, float S, float L) ToHsl(Color c)
    {
        float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float l = (max + min) / 2f;
        float h = 0, s = 0;

        if (max != min)
        {
            float d = max - min;
            s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

            if (max == r) h = (g - b) / d + (g < b ? 6f : 0f);
            else if (max == g) h = (b - r) / d + 2f;
            else h = (r - g) / d + 4f;

            h /= 6f;
        }

        return (h * 360f, s, l);
    }

    public static Color FromHsl(float h, float s, float l)
    {
        h = ((h % 360f) + 360f) % 360f;
        float c = (1f - Math.Abs(2f * l - 1f)) * s;
        float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
        float m = l - c / 2f;

        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromArgb(255,
            (byte)Math.Clamp((r + m) * 255, 0, 255),
            (byte)Math.Clamp((g + m) * 255, 0, 255),
            (byte)Math.Clamp((b + m) * 255, 0, 255));
    }

    public static Color ShiftLightness(Color c, float amount)
    {
        var (h, s, l) = ToHsl(c);
        l = Math.Clamp(l + amount, 0f, 1f);
        return FromHsl(h, s, l);
    }
}
