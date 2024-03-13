using KaC_Modding_Engine_API.Shared.ArchieV1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zat.InterModComm;
using System.Text.RegularExpressions;

namespace KaC_Modding_Engine_API.Tools
{
    public static class Tools
    {
        /// <summary>
        /// Joins two files paths with care for directory separators.
        /// </summary>
        /// <param name="string1">First half of path. Allows / or \ at end of string.</param>
        /// <param name="string2">Second half of path. Allows / or \ at start of string.</param>
        /// <returns></returns>
        public static string JoinFilePath(string string1, string string2)
        {
            // Using this does not require System assemblies that are likely blocked
            return Regex.Replace(string1, @"/|\\$", string.Empty) + "/" + Regex.Replace(string2, @"^/|\\", string.Empty);
        }

        /// <summary>
        /// Turns a 2d array into a 1d array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<T> FlattenTo1Dimension<T>(this IEnumerable<T> list) where T : IEnumerable<T>
        {
            List<T> values = new List<T>();

            foreach (T array in list)
            {
                foreach(T item in array)
                {
                    values.Add(item);
                }
            }

            return values;
        }

        /// <summary>
        /// Turns a 1d array into a 2d array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="width">The width of the 2d array.</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> ExpandTo2Dimension<T>(this IEnumerable<T> input, int width)
        {
            List<IEnumerable<T>> values = new List<IEnumerable<T>>();
            List<T> list = input.ToList();
            int counter = 0;

            while(counter <= list.Count())
            {
                int widthCounter = 0;
                List<T> row = new List<T>();

                while(widthCounter <= width)
                {
                    row.Add(list[counter]);
                    counter++;
                }

                counter += widthCounter;
                values.Add(list);
            }

            return values;
        }
    }
}
