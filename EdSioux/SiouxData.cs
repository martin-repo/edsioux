// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiouxData.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class SiouxData
    {
        [JsonProperty("filterOnCurrentCommander", Required = Required.Always)]
        public bool FilterOnCurrentCommander { get; set; }

        [JsonProperty("defaultDisplayDuration", Required = Required.Always)]
        public int DefaultDisplayDuration { get; set; }

        [JsonProperty("defaultTextColorType", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ColorType DefaultTextColorType { get; set; }

        [JsonProperty("defaultTokenColorType", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ColorType DefaultTokenColorType { get; set; }

        [JsonProperty("events", Required = Required.Always)]
        public SiouxEventCollection Events { get; set; }
    }
}