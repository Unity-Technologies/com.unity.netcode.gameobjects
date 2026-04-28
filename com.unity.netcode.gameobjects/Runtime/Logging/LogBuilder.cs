using System.Text;

namespace Unity.Netcode
{
    internal class LogBuilder
    {
        private readonly StringBuilder m_Builder = new();
        private const string k_OpenBracket = "[";
        private const string k_CloseBracket = "]";
        private const string k_Separator = ":";

        public void Reset()
        {
            m_Builder.Clear();
        }

        public void AppendTag(string context)
        {
            m_Builder.Append(k_OpenBracket);
            m_Builder.Append(context);
            m_Builder.Append(k_CloseBracket);
        }

        public void AppendInfo(object key, object value)
        {
            m_Builder.Append(k_OpenBracket);
            m_Builder.Append(key);
            m_Builder.Append(k_Separator);
            m_Builder.Append(value);
            m_Builder.Append(k_CloseBracket);
        }

        public void Append(string value) => m_Builder.Append(value);

        public string Build() => m_Builder.ToString();
    }
}
