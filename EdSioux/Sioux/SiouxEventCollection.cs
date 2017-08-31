// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiouxEventCollection.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Sioux
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [JsonArray(false)]
    public class SiouxEventCollection : List<SiouxEvent>
    {
    }
}