// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiouxEvent.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux
{
    using EdNetApi.Journal;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class SiouxEvent
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public JournalEventType Type { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }
    }
}