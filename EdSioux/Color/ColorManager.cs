// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorManager.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Color
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Media;
    using System.Xml;

    using EdNetApi.Common;

    using Brush = System.Windows.Media.Brush;
    using Color = System.Windows.Media.Color;

    internal static class ColorManager
    {
        static ColorManager()
        {
            Colors = CreateColors();
            Brushes = Colors.ToDictionary(kvp => kvp.Key, kvp => (Brush)new SolidColorBrush(kvp.Value));
        }

        public static Dictionary<ColorType, Color> Colors { get; }

        public static Dictionary<ColorType, Brush> Brushes { get; }

        public static Color GetColorFromAttribute(ColorType colorType)
        {
            var color = EnumExtensions.GetAttributeValue<ColorAttribute, Color>(
                colorType,
                attribute => attribute.Color);
            return color;
        }

        /// <summary>
        /// Applies matrix to the input bitmap
        /// </summary>
        /// <param name="bitmap">Bitmap, in standard color space</param>
        /// <param name="matrix">Matrix to use</param>
        /// <returns>Bitmap with matrix applied, in linear color space</returns>
        private static Bitmap ApplyLinearMatrix(Bitmap bitmap, float[][] matrix)
        {
            float[][] rgbMatrix =
                {
                    new[] { matrix[0][0], matrix[1][0], matrix[2][0], matrix[3][0], 0 },
                    new[] { matrix[0][1], matrix[1][1], matrix[2][1], matrix[3][1], 0 },
                    new[] { matrix[0][2], matrix[1][2], matrix[2][2], matrix[3][2], 0 },
                    new[] { matrix[0][3], matrix[1][3], matrix[2][3], matrix[3][3], 0 },
                    new[] { 0, 0, 0, 0, 1f }
                };
            var colorMatrix = new ColorMatrix(rgbMatrix);

            var targetBitmap = new Bitmap(bitmap);
            using (var targetGraphics = Graphics.FromImage(targetBitmap))
            {
                var imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(colorMatrix);
                imageAttributes.SetGamma(1 / 2.2f);
                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                targetGraphics.DrawImage(
                    bitmap,
                    rect,
                    0,
                    0,
                    bitmap.Width,
                    bitmap.Height,
                    GraphicsUnit.Pixel,
                    imageAttributes);
            }

            return targetBitmap;
        }

        private static Color ApplyMatrix(Color color, float[][] matrix)
        {
            var rRaw = matrix[0][0] * color.R + matrix[1][0] * color.G + matrix[2][0] * color.B;
            var r = (byte)Math.Round(Math.Max(0, Math.Min(rRaw, 255)), MidpointRounding.AwayFromZero);

            var gRaw = matrix[0][1] * color.R + matrix[1][1] * color.G + matrix[2][1] * color.B;
            var g = (byte)Math.Round(Math.Max(0, Math.Min(gRaw, 255)), MidpointRounding.AwayFromZero);

            var bRaw = matrix[0][2] * color.R + matrix[1][2] * color.G + matrix[2][2] * color.B;
            var b = (byte)Math.Round(Math.Max(0, Math.Min(bRaw, 255)), MidpointRounding.AwayFromZero);

            var aRaw = matrix[0].Length > 3
                           ? matrix[0][3] * color.R + matrix[1][3] * color.G + matrix[2][3] * color.B
                           : 255;
            var a = (byte)Math.Round(Math.Max(0, Math.Min(aRaw, 255)), MidpointRounding.AwayFromZero);

            var matrixColor = Color.FromArgb(a, r, g, b);
            return matrixColor;
        }

        private static Dictionary<ColorType, Color> CreateColors()
        {
            var colorTypes = Enum.GetValues(typeof(ColorType)).Cast<ColorType>().ToList();

            var colorMatrix = GetGraphicsConfigurationFilePaths().Select(GetColorMatrix).FirstOrDefault();
            if (colorMatrix == null)
            {
                return colorTypes.ToDictionary(colorType => colorType, GetColorFromAttribute);
            }

            var colors = new Dictionary<ColorType, Color>();
            foreach (var colorType in colorTypes)
            {
                // ColorType colors were captured from screenshots
                // Game uses linear color space
                // Matrix algorithm is using standard color space
                var linearColor = GetColorFromAttribute(colorType);
                var standardColor = LinearToStandardRgb(linearColor);
                var matrixStandardColor = ApplyMatrix(standardColor, colorMatrix);
                var matrixLinearColor = StandardToLinearRgb(matrixStandardColor);
                colors.Add(colorType, matrixLinearColor);
            }

            return colors;
        }

        private static float[][] GetColorMatrix(string graphicsConfigurationPath)
        {
            if (!File.Exists(graphicsConfigurationPath))
            {
                return null;
            }

            try
            {
                var document = new XmlDocument();
                document.Load(graphicsConfigurationPath);
                var defaultNode = document.SelectSingleNode("/GraphicsConfig/GUIColour/Default");
                var matrixRed = defaultNode?.SelectSingleNode("MatrixRed");
                var matrixGreen = defaultNode?.SelectSingleNode("MatrixGreen");
                var matrixBlue = defaultNode?.SelectSingleNode("MatrixBlue");
                if (matrixRed == null || matrixGreen == null || matrixBlue == null)
                {
                    return null;
                }

                var redParts = GetMatrixParts(matrixRed.InnerText);
                var greenParts = GetMatrixParts(matrixGreen.InnerText);
                var blueParts = GetMatrixParts(matrixBlue.InnerText);
                if (redParts == null || greenParts == null || blueParts == null)
                {
                    return null;
                }

                return new List<float[]> { redParts, greenParts, blueParts }.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static List<string> GetGraphicsConfigurationFilePaths()
        {
            var filePaths = new List<string>();

            var localAppDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var graphicsOverrideFilePath = Path.Combine(
                localAppDataFolderPath,
                @"Frontier Developments\Elite Dangerous\Options\Graphics",
                "GraphicsConfigurationOverride.xml");
            filePaths.Add(graphicsOverrideFilePath);

            var edProcess = Process.GetProcessesByName("EliteDangerous64").FirstOrDefault();
            var edProcessFilePath = edProcess?.MainModule.FileName;
            var edProcessFolderPath = Path.GetDirectoryName(edProcessFilePath);
            if (edProcessFolderPath != null)
            {
                graphicsOverrideFilePath = Path.Combine(edProcessFolderPath, "GraphicsConfigurationOverride.xml");
                filePaths.Add(graphicsOverrideFilePath);

                var graphicsFilePath = Path.Combine(edProcessFolderPath, "GraphicsConfiguration.xml");
                filePaths.Add(graphicsFilePath);
            }

            return filePaths;
        }

        private static float[] GetMatrixParts(string text)
        {
            var stringParts = text.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var matrixParts = stringParts.Select(p => float.Parse(p, NumberStyles.Float, CultureInfo.InvariantCulture))
                .ToList();
            return matrixParts.Count == 3 ? matrixParts.ToArray() : null;
        }

        private static Color LinearToStandardRgb(Color linearColor)
        {
            var linearToStandardFunc = new Func<int, byte>(
                linear => (byte)Math.Round(Math.Pow(linear / 255f, 2.2) * 255, MidpointRounding.AwayFromZero));

            var r = linearToStandardFunc(linearColor.R);
            var g = linearToStandardFunc(linearColor.G);
            var b = linearToStandardFunc(linearColor.B);
            return Color.FromArgb(linearColor.A, r, g, b);
        }

        private static Color StandardToLinearRgb(Color standardColor)
        {
            var standardToLinearFunc = new Func<int, byte>(
                standard => (byte)Math.Round(
                    Math.Pow(standard / 255f, 1.0 / 2.2) * 255,
                    MidpointRounding.AwayFromZero));

            var r = standardToLinearFunc(standardColor.R);
            var g = standardToLinearFunc(standardColor.G);
            var b = standardToLinearFunc(standardColor.B);
            return Color.FromArgb(standardColor.A, r, g, b);
        }
    }
}