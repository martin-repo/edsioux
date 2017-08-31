// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProgressEventArgs.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Sioux
{
    using System;

    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(int percent)
        {
            Percent = percent;
        }

        public int Percent { get; set; }
    }
}