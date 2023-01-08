using System;
using UnityEngine;

namespace Testproject.Editor
{
    /// <remarks>
    /// Custom readme class based on the readme created for URP. For more context, see:
    /// https://github.com/Unity-Technologies/Graphics/tree/master/com.unity.template-universal
    /// </remarks>
    [CreateAssetMenu]
    public class Readme : ScriptableObject
    {
        public Texture2D Icon;
        public string Title;
        public Section[] Sections;
        public bool LoadedLayout;

        [Serializable]
        public class Section
        {
            public string Heading;
            public string Text;
            public string LinkText;
            public string Url;
        }
    }
}
