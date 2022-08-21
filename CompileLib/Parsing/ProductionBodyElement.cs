using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Part of production
    /// </summary>
    internal struct ProductionBodyElement
    {
        public object TagType;
        public object RepetitionCount;
        public MethodInfo Method;
        public ParameterInfo Parameter;

        public ProductionBodyElement ReplaceTags(object tagType, object repetitionCount)
            => new() { TagType = tagType, RepetitionCount = repetitionCount, Method = Method, Parameter = Parameter };

        public void SetTagType(Attribute tagType)
        {
            if (TagType is not null)
                throw new ParsingEngineBuildingException(Method, Parameter, "Only one tag requiring attribute is allowed");
            TagType = tagType;
        }

        public void SetRepetitionCount(Attribute repetitionCount)
        {
            if (RepetitionCount is not null)
                throw new ParsingEngineBuildingException(Method, Parameter, "At most one repetition count attribute is allowed");
            RepetitionCount = repetitionCount;
        }

        public static string ShowKeyword(string keyword)
            => $"[keyword]\"{keyword}\"";

        public static string ShowHelperTag(HelperTag ht)
            => "[helper tag]" + ht.ID;

        public string FullTagName
        {
            get
            {
                string result;
                if (TagType is KeywordsAttribute keywordsAttr && keywordsAttr.Keywords.Count == 1)
                {
                    result = ShowKeyword(keywordsAttr.Keywords[0]);
                }
                else if (TagType is RequireTagsAttribute requireTagsAttr && requireTagsAttr.Tags.Count == 1)
                {
                    result = requireTagsAttr.Tags[0];
                }
                else if (TagType is HelperTag ht)
                {
                    result = ShowHelperTag(ht);
                }
                else
                {
                    Debug.Fail("Unexpected tag type attribute");
                    result = "";
                }

                Debug.Assert(RepetitionCount is SingleAttribute);
                return result;
            }
        }
    }
}
