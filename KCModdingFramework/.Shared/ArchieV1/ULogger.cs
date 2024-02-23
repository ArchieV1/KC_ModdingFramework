using System;
using System.Text.RegularExpressions;
using static KaC_Modding_Engine_API.Tools.Tools;

namespace KaC_Modding_Engine_API.Shared.ArchieV1
{
    public static class ULogger
    {
        /// <summary>
        /// Logs to `~\steamapps\common\Kingdoms and Castles\KingdomsAndCastles_Data\mods\log.txt`.
        /// This is where compiler logging happens.
        /// </summary>
        /// <param name="category">The category/mod name. Max 25 chars.</param>
        /// <param name="message">The message to post. Newlines will be split over multiple logs</param>
        public static void Log(string category, string message)
        {
            foreach(string line in Regex.Split(message, @"\r\n?|\n"))
            {
                Console.WriteLine($"[ULOG|{DateTime.Now}] {category,25} | {line}");
            }
        }

        /// <summary>
        /// Logs a newline.
        /// </summary>
        /// <param name="emptyCat">If the category should be blank or not.</param>
        public static void Log(bool emptyCat = false)
        {
            Log("", emptyCat);
        }

        /// <summary>
        /// Logs the given message.
        /// </summary>
        /// <param name="message">The message to query.</param>
        /// <param name="emptyCat">
        /// If false will get the namespace of the calling method.
        /// WARNING: Namespaces are often longer than the 25 max character limit for characters.
        /// WARNING: Runs on main thread and so will block.
        /// </param>
        public static void Log(string message, bool emptyCat = false)
        {
            if (emptyCat)
            {
                Log("", message);
            }
            else
            {
                Log(GetCallingNamespace(2), message);
            }
        }

        public static void Log(object category, object message)
        {
            Log(category.ToString(), message.ToString());
        }

        public static void Log(object message)
        {
            Log(message, false);
        }
    }
}
