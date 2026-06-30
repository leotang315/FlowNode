using System;
using System.Collections.Generic;
using System.Globalization;
using FlowNode.node.Attribute;
using System.Web.Script.Serialization;

namespace FlowNode.node
{
    /// <summary>
    /// 轻量 JSON 取值（根级 flat object，自动运行）。依赖 BCL System.Web.Extensions，无 NuGet。
    /// </summary>
    [Node("Json")]
    public class JsonOperator
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        [Function("getJsonString", true)]
        public static void GetJsonString(string json, string key, out string result)
        {
            result = string.Empty;
            if (!TryGetRootProperty(json, key, out object raw) || raw == null)
                return;

            result = Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        [Function("getJsonInt", true)]
        public static void GetJsonInt(string json, string key, out int result)
        {
            result = 0;
            if (!TryGetRootProperty(json, key, out object raw) || raw == null)
                return;

            if (raw is int i)
            {
                result = i;
                return;
            }

            if (raw is long l)
            {
                result = (int)l;
                return;
            }

            if (raw is double d)
            {
                result = (int)d;
                return;
            }

            int.TryParse(
                Convert.ToString(raw, CultureInfo.InvariantCulture),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out result);
        }

        private static bool TryGetRootProperty(string json, string key, out object value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                var dict = Serializer.Deserialize<Dictionary<string, object>>(json);
                return dict != null && dict.TryGetValue(key, out value);
            }
            catch
            {
                return false;
            }
        }
    }
}
