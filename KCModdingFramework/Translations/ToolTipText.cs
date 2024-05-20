using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaC_Modding_Engine_API.Translations
{
    public class ToolTipText
    {
        /// <summary>
        /// A dictionary of ISO639_3 to ToolTip
        /// </summary>
        private Dictionary<string, string> dict = new Dictionary<string, string>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="languageTextDictionary"></param>
        /// <param name="ISOCode">The ISO639 standard that the dictionary uses</param>
        public ToolTipText(Dictionary<string, string> languageTextDictionary, ISO639Code ISOCode = ISO639Code.ISO639_3)
        {
            // Set from whatever ISO639 code to ISO639-3
            List<string> langArray = ISO639.GetLangArrayFromISO639(ISOCode);
            Dictionary<string, string> newDict = new Dictionary<string, string>();

            foreach (string langCode in langArray)
            {
                languageTextDictionary.TryGetValue(langCode, out string toolTip);

                if (dict.ContainsKey(langCode))
                {
                    dict.Remove(langCode);
                }

                dict.Add(ISO639.ConvertStandard(langCode, ISOCode), toolTip);
            }
        }

        public ToolTipText(string[] textArray)
        {
            for (int x = 0; x < textArray.Length; x++)
            {
                dict[ISO639.LanguagesISO639_3[x]] = textArray[x];
            }
        }
    }
}
