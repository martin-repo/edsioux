// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiouxEvent.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Models
{
    using EdNetApi.Journal;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class SiouxEvent
    {
        [JsonProperty("type", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public JournalEventType Type { get; set; }

        [JsonProperty("image", Required = Required.DisallowNull)]
        public string Image { get; set; }

        [JsonProperty("format", Required = Required.Always)]
        public string Format { get; set; }

        [JsonProperty("displayDuration", Required = Required.DisallowNull)]
        public int DisplayDuration { get; set; }
    }
}