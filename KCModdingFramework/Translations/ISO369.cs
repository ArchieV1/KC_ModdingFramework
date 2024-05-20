using System.Collections.Generic;

namespace KaC_Modding_Engine_API.Translations
{
    public static class ISO639
    {
        // Private arrays for converting from any language code to language code `ISO 639-3`
        public static List<string> LanguagesISO639_1 => new List<string>()
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

        public static List<string> LanguagesISO639_2_T = new List<string>()
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

        public static List<string> LanguagesISO639_2_B = new List<string>()
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
        public static List<string> LanguagesISO639_3 = new List<string>()
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

        public static List<string> GetLangArrayFromISO639(ISO639Code ISOCode)
        {
            switch (ISOCode)
            {
                case ISO639Code.ISO639_1:
                    return ISO639.LanguagesISO639_1;
                case ISO639Code.ISO639_2_T:
                    return ISO639.LanguagesISO639_2_T;
                case ISO639Code.ISO639_2_B:
                    return ISO639.LanguagesISO639_2_B;
                case ISO639Code.ISO639_3:
                    return ISO639.LanguagesISO639_3;
                default:
                    return ISO639.LanguagesISO639_3;
            }
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
            List<string> fromArray = GetLangArrayFromISO639(codeFrom);

            for (int x = 0; x < fromArray.Count; x++)
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
