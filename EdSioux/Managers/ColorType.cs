// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorType.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Managers
{
    public enum ColorType
    {
        [Color("#FF7000")]
        Default,

        [Color("#70B0F0")]
        Information,

        [Color("#FFCF00")]
        NotAvailable,

        [Color("#F6CBFF")]
        Name,

        [Color("#FF0000")]
        Warning,

        [Color("#FFFFFF")]
        Headline,

        [Color("#46CB30")]
        Friendly,
    }
}