// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.cs" company="Martin Amareld">
//   Copyright(c) 2017 Martin Amareld. All rights reserved. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EdSioux.Common
{
    using System;
    using System.Collections.Generic;

    public static class IListExtensions
    {
        public static void RemoveWhere<T>(this IList<T> list, Func<T, bool> predicate)
        {
            for (var index = list.Count - 1; index >= 0; index--)
            {
                if (predicate(list[index]))
                {
                    list.RemoveAt(index);
                }
            }
        }
    }
}