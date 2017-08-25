// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiouxData.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class SiouxData
    {
        [JsonProperty("filterOnCurrentCommander")]
        public bool FilterOnCurrentCommander { get; set; }

        [JsonProperty("events")]
        public List<SiouxEvent> Events { get; set; }
    }
}