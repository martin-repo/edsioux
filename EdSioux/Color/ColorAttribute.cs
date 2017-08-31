// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorAttribute.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Color
{
    using System;
    using System.Windows.Media;

    public class ColorAttribute : Attribute
    {
        public ColorAttribute(Color color)
        {
            Color = color;
        }

        public ColorAttribute(string color)
        {
            Color = (Color)(ColorConverter.ConvertFromString(color) ?? Colors.White);
        }

        public Color Color { get; set; }
    }
}