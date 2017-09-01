// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FontManager.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Common
{
    using System;
    using System.Drawing;
    using System.Drawing.Text;
    using System.Reflection;
    using System.Runtime.InteropServices;

    public class FontManager
    {
        public static Font GetFont(string embeddedFontFilename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.GetName().Name}.Resources.{embeddedFontFilename}";

            byte[] fontBytes;
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new ApplicationException($"{embeddedFontFilename} was not found");
                }

                fontBytes = new byte[resourceStream.Length];
                resourceStream.Read(fontBytes, 0, (int)resourceStream.Length);
                resourceStream.Close();
            }

            var fontData = IntPtr.Zero;
            try
            {
                fontData = Marshal.AllocCoTaskMem(fontBytes.Length);
                Marshal.Copy(fontBytes, 0, fontData, fontBytes.Length);

                using (var privateFontCollection = new PrivateFontCollection())
                {
                    privateFontCollection.AddMemoryFont(fontData, fontBytes.Length);
                    var font = new Font(privateFontCollection.Families[0], 18, FontStyle.Regular, GraphicsUnit.Pixel);
                    return font;
                }
            }
            finally
            {
                if (fontData != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(fontData);
                }
            }
        }
    }
}