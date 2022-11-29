using CompileLib.Common;
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
    /// Production --- internal representation of rule
    /// </summary>
    internal struct Production
    {
        public Alternation<string, HelperTag> Tag;
        public bool HasErrorHandler;
        public int Greedy; // 0 if not greedy, delta index to the pairwise otherwise
        public int Divisor;
        public List<ProductionBodyElement> Body;
        public MethodInfo Handler;

        /// <summary>
        /// Expanding "Optional"s and "Many"s
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static List<Production> SplitIntoSimpleProductions(Production self)
        {
            List<Production> result = new();

            Production MakeCopy(Alternation<string, HelperTag> subTag, bool hasErrorHandler)
                => new()
                {
                    Tag = subTag,
                    HasErrorHandler = hasErrorHandler,
                    Body = new(),
                    Handler = self.Handler,
                    Greedy = 0,
                    Divisor = self.Divisor
                };

            ProductionBodyElement SplitBodyElement(ProductionBodyElement e)
            {
                if (e.TagType is KeywordsAttribute keywordsAttr)
                {
                    if (keywordsAttr.Keywords.Count == 1)
                        return e;

                    HelperTag subTag = new(keywordsAttr);
                    foreach (var kw in keywordsAttr.Keywords)
                    {
                        Production sub = MakeCopy(subTag.ToTag(), true);
                        result.Add(sub);
                        sub.Body.Add(e.ReplaceTags(new KeywordsAttribute(kw), SingleAttribute.Instance));
                    }
                    return e.ReplaceTags(subTag, SingleAttribute.Instance);
                }
                else if (e.TagType is RequireTagsAttribute requireTagsAttr)
                {
                    if (requireTagsAttr.Tags.Count == 1)
                        return e;

                    HelperTag subTag = new(requireTagsAttr);
                    foreach (var tag in requireTagsAttr.Tags)
                    {
                        Production sub = MakeCopy(subTag.ToTag(), true);
                        result.Add(sub);
                        sub.Body.Add(e.ReplaceTags(new RequireTagsAttribute(tag), SingleAttribute.Instance));
                    }
                    return e.ReplaceTags(subTag, SingleAttribute.Instance);
                }
                else
                {
                    Debug.Fail("Unexpected tag type attribute");
                    return e;
                }
            }

            Production newThis = MakeCopy(self.Tag, self.HasErrorHandler);
            result.Add(newThis);

            for (int i = 0; i < self.Body.Count;)
            {
                if (self.Body[i].RepetitionCount is SingleAttribute)
                {
                    newThis.Body.Add(SplitBodyElement(self.Body[i++]));
                }
                else if (self.Body[i].RepetitionCount is OptionalAttribute optionalAttribute)
                {
                    HelperTag subTag = new(optionalAttribute);
                    var subStart = new ProductionBodyElement() { TagType = subTag, RepetitionCount = SingleAttribute.Instance, Method = self.Body[i].Method, Parameter = self.Body[i].Parameter };
                    
                    Production emptySub = MakeCopy(subTag.ToTag(), true); // empty production
                    Production sub = MakeCopy(subTag.ToTag(), true);
                    sub.Greedy = optionalAttribute.Greedy ? -1 : 0;
                    sub.Body.Add(SplitBodyElement(self.Body[i]));
                    int divisor = 1;
                    for (i++; i < self.Body.Count && self.Body[i].RepetitionCount is TogetherWithAttribute; i++)
                    {
                        sub.Body.Add(SplitBodyElement(self.Body[i]));
                        divisor++;
                    }

                    newThis.Body.Add(subStart);

                    emptySub.Divisor = divisor;
                    result.Add(emptySub);
                    sub.Divisor = divisor;
                    result.Add(sub);
                }
                else if (self.Body[i].RepetitionCount is ManyAttribute manyAttribute)
                {
                    HelperTag subTag = new(manyAttribute);
                    var subStart = new ProductionBodyElement() { TagType = subTag, RepetitionCount = SingleAttribute.Instance, Method = self.Body[i].Method, Parameter = self.Body[i].Parameter };

                    // empty or non-rec production
                    Production sub1 = MakeCopy(subTag.ToTag(), true);
                    Production sub2 = MakeCopy(subTag.ToTag(), true);
                    var e = SplitBodyElement(self.Body[i]);
                    sub2.Body.Add(e);
                    if (!manyAttribute.CanBeEmpty) sub1.Body.Add(e);
                    int divisor = 1;
                    for (i++; i < self.Body.Count && self.Body[i].RepetitionCount is TogetherWithAttribute; i++)
                    {
                        e = SplitBodyElement(self.Body[i]);
                        sub2.Body.Add(e);
                        if (!manyAttribute.CanBeEmpty) sub1.Body.Add(e);
                        divisor++;
                    }

                    sub2.Body.Add(subStart);
                    newThis.Body.Add(subStart);

                    sub1.Divisor = divisor;
                    result.Add(sub1);
                    sub2.Divisor = divisor;
                    result.Add(sub2);
                }
                else
                {
                    Debug.Fail("Unexpected repetition count attribute");
                    return null;
                }
            }
            return result;
        }
    }
}
