// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiouxEventArgs.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Sioux
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class SiouxEventArgs : EventArgs
    {
        public SiouxEventArgs(string header, IEnumerable<SiouxMessagePart> messageParts, int displayDuration)
        {
            Header = header;
            MessageParts = messageParts.ToList();
            DisplayDuration = displayDuration;
        }

        public string Header { get; set; }

        public List<SiouxMessagePart> MessageParts { get; set; }

        public int DisplayDuration { get; set; }
    }
}