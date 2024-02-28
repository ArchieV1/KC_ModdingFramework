using Newtonsoft.Json;
using Zat.InterModComm;

namespace KaC_Modding_Engine_API.Tools
{
    internal static class JsonTools
    {

        public static object DecodeObject(string str, JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                settings = IMCPort.serializerSettings;
            }

            return JsonConvert.DeserializeObject(str, settings);
        }

        public static string EncodeObject(object obj, JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                settings = IMCPort.serializerSettings;
            }

            return JsonConvert.SerializeObject(obj, settings);
        }

        public static bool IsDecodable(string str)
        {
            if (str == null) return false;

            try
            {
                DecodeObject(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsEncodable(object obj)
        {
            try
            {
                JsonConvert.SerializeObject(obj, IMCPort.serializerSettings);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsJSONable(object obj)
        {
            try
            {
                // If Encode/Decode/Encode == Encode then it can be sent and received without issue
                return EncodeObject(DecodeObject(EncodeObject(obj))) ==
                       EncodeObject(obj);
            }
            catch
            {
                return false;
            }
        }
    }
}