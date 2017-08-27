﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiouxData.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux
{
    using Newtonsoft.Json;

    public class SiouxData
    {
        [JsonProperty("filterOnCurrentCommander", Required = Required.Always)]
        public bool FilterOnCurrentCommander { get; set; }

        [JsonProperty("defaultDisplayDuration", Required = Required.Always)]
        public int DefaultDisplayDuration { get; set; }

        [JsonProperty("events", Required = Required.Always)]
        public SiouxEventCollection Events { get; set; }
    }
}