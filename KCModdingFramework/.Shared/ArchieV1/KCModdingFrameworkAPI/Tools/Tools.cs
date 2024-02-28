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
        /// Joins two files paths with care for directory seperators.
        /// </summary>
        /// <param name="string1">First half of path. Allows / or \ at end of string.</param>
        /// <param name="string2">Second half of path. Allows / or \ at start of string.</param>
        /// <returns></returns>
        public static string JoinFilePath(string string1, string string2)
        {
            // Using this does not require System assemblies that are likely blocked
            return Regex.Replace(string1, @"/|\\$", string.Empty) + "/" + Regex.Replace(string2, @"^/|\\", string.Empty);
        }
    }
}
