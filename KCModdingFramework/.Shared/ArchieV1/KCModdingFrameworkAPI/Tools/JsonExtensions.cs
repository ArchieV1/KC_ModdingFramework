using Newtonsoft.Json;
using Zat.InterModComm;

namespace KaC_Modding_Engine_API.Tools
{
    internal static class JsonExtensions
    {
        public static object Deserialise(this string str, JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                settings = IMCPort.serializerSettings;
            }

            return JsonConvert.DeserializeObject(str, settings);
        }

        public static string Serialise(this object obj, JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                settings = IMCPort.serializerSettings;
            }

            return JsonConvert.SerializeObject(obj, settings);
        }

        public static bool IsDeserialisable(this string str)
        {
            if (str == null) return false;

            try
            {
                Deserialise(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSerialiseable(this object obj)
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

        public static bool IsJSONable(this object obj)
        {
            try
            {
                // If Encode/Decode/Encode == Encode then it can be sent and received without issue
                return Serialise(Deserialise(Serialise(obj))) ==
                       Serialise(obj);
            }
            catch
            {
                return false;
            }
        }
    }
}