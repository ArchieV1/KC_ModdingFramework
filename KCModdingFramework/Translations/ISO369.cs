using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaC_Modding_Engine_API.Translations
{
    public static class ISO639
    {
        // Private arrays for converting from any language code to language code `ISO 639-3`
        public static readonly string[] languagesISO639_1 = new[]
        {
        "en",
        "de",
        "fr",
        "zh1",
        "zh2",
        "es",
        "nl",
        "pt",
        "it",
        "ja",
        "ko",
        "no",
        "pl",
        "ro",
        "ru",
        "uk",
        "sv",
        "tr"
    };

        public static readonly string[] languagesISO639_2_T = new[]
        {
        "eng",
        "deu",
        "fra",
        "zho1",
        "zho2",
        "spa",
        "nld",
        "por",
        "ita",
        "jpn",
        "kor",
        "nor",
        "pol",
        "ron",
        "rus",
        "ukr",
        "swe",
        "tur"
    };

        public static readonly string[] languagesISO639_2_B = new[]
        {
        "eng",
        "ger",
        "fre",
        "chi1",
        "chi2",
        "spa",
        "dut",
        "por",
        "ita",
        "jpn",
        "kor",
        "nor",
        "pol",
        "rum",
        "rus",
        "ukr",
        "swe",
        "tur"
    };

        /// <summary>
        /// This is the preferred ISO language code of this mod (See wiki on Chinese variation)
        /// </summary>
        public static readonly string[] languagesISO639_3 = new[]
        {
        "eng",
        "deu",
        "fra",
        "zho1",
        "zho2",
        "spa",
        "nld",
        "por",
        "ita",
        "jpn",
        "kor",
        "nor",
        "pol",
        "ron",
        "rus",
        "ukr",
        "swe",
        "tur"
    };

        public static string[] GetLangArrayFromISO639(ISO639Code ISOCode)
        {
            string[] langArray;
            switch (ISOCode)
            {
                case ISO639Code.ISO639_1:
                    langArray = ISO639.languagesISO639_1;
                    break;
                case ISO639Code.ISO639_2_T:
                    langArray = ISO639.languagesISO639_2_T;
                    break;
                case ISO639Code.ISO639_2_B:
                    langArray = ISO639.languagesISO639_2_B;
                    break;
                case ISO639Code.ISO639_3:
                    langArray = ISO639.languagesISO639_3;
                    break;
                default:
                    langArray = ISO639.languagesISO639_3;
                    break;
            }

            return langArray;
        }

        /// <summary>
        /// Converts between standards
        /// </summary>
        /// <param name="str"></param>
        /// <param name="codeFrom"></param>
        /// <param name="codeTo"></param>
        /// <returns></returns>
        public static string ConvertStandard(string str, ISO639Code codeFrom, ISO639Code codeTo = ISO639Code.ISO639_3)
        {
            string[] fromArray = GetLangArrayFromISO639(codeFrom);

            for (int x = 0; x < fromArray.Length; x++)
            {
                if (fromArray[x] == str)
                {
                    return GetLangArrayFromISO639(codeTo)[x];
                }
            }

            return null;
        }
    }
}
