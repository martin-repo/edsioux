// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CurrentData.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Sioux
{
    using EdNetApi.Journal.Enums;

    public class CurrentData
    {
        public string Commander { get; set; }

        public ShipType Ship { get; set; }

        public string StarSystem { get; set; }

        public string StationName { get; set; }

        public string Body { get; set; }

        public string BodyType { get; set; }
    }
}