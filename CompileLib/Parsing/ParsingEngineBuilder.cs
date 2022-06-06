using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

using CompileLib.LexerTools;
using CompileLib.ParserTools;
using static CompileLib.Parsing.SpecialTags;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Builder of parsing engine TODO Regex
    /// </summary>
    public class ParsingEngineBuilder
    {
        private static readonly SingleAttribute defaultSingleAttribute = new();
        private static readonly ParsingEngineBuildingException nullTagException = new("Null-tags are not allowed");
        private static readonly ParsingEngineBuildingException specialTagException = new("Special tag is not allowed here");
        private static readonly ParsingEngineBuildingException usedTokenTagException = new("The tag is already used as a token tag");
        private static readonly ParsingEngineBuildingException usedNonTokenTagException = new("The tag is already used as a non-token tag");

        private static Exception GetBuildingException(MethodInfo method, string message)
        {
            return new ParsingEngineBuildingException($"[Method {method.Name}] {message}");
        }

        private static Exception GetBuildingException(string methodName, string parameterName, string message)
        {
            return new ParsingEngineBuildingException($"[Method {methodName}, parameter {parameterName}] {message}");
        }

        private static Exception GetParsingException(MethodInfo method, ParameterInfo parameter, string message)
        {
            Debug.Assert(method.DeclaringType is not null);
            return new ParsingException($"[Method {method.DeclaringType.Name}::{method.Name}, parameter {parameter.Name}] {message}");
        }

        private class HelperTag
        {
            private static long Counter = 0;

            public string Name { get; private set; }
            public object ParentAttribute { get; private set; }

            public HelperTag(object parentAttribute)
            {
                Name = "#" + (Counter++);
                ParentAttribute = parentAttribute;
            }

            public override string? ToString()
            {
                return ProductionBodyElement.ShowHelperTag(this);
            }

            public override bool Equals(object? obj)
            {
                return obj is HelperTag ht && ht.Name == Name;
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }

        private struct ProductionBodyElement
        {
            public object TagType;
            public object RepetitionCount;
            public string MethodName;
            public string ParameterName;

            public ProductionBodyElement ReplaceTags(object tagType, object repetitionCount)
                => new() { TagType = tagType, RepetitionCount = repetitionCount, MethodName = MethodName, ParameterName = ParameterName };

            public void SetTagType(Attribute tagType)
            {
                if (TagType is not null)
                    throw GetBuildingException(MethodName, ParameterName, "Only one tag requiring attribute is allowed");
                TagType = tagType;
            }

            public void SetRepetitionCount(Attribute repetitionCount)
            {
                if (RepetitionCount is not null)
                    throw GetBuildingException(MethodName, ParameterName, "At most one repetition count attribute is allowed");
                RepetitionCount = repetitionCount;
            }

            public static string ShowKeyword(string keyword)
                => $"[keyword]\"{keyword}\"";

            // for information only
            [Obsolete]
            public static string ShowTag(string tag)
                => tag;

            public static string ShowHelperTag(HelperTag ht)
                => "[helper tag]" + ht.Name;

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
                    else if(TagType is HelperTag ht)
                    {
                        result = ShowHelperTag(ht);
                    }
                    else
                    {
                        Debug.Fail("Unexpected tag type attribute");
                        result = "";
                    }

                    //Debug.Assert(RepetitionCount is SingleAttribute);

                    return result;
                }
            }
        }

        private struct Production
        {
            public object Tag; // string or HelperTag
            public bool HasErrorHandler;
            public bool Greedy;
            public int Divisor;
            public List<ProductionBodyElement> Body;
            public MethodInfo Handler;

            public static List<Production> SplitIntoSimpleProductions(Production self)
            {
                List<Production> result = new();

                Production MakeCopy(object subTag, bool hasErrorHandler)
                    => new()
                    {
                        Tag = subTag,
                        HasErrorHandler = hasErrorHandler, // ???
                        Body = new(),
                        Handler = self.Handler,
                        Greedy = false,
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
                            Production sub = MakeCopy(subTag, true);
                            result.Add(sub);
                            //sub.Body.Add(new ProductionBodyElement() { TagType = new KeywordsAttribute(kw), RepetitionCount = defaultSingleAttribute });
                            sub.Body.Add(e.ReplaceTags(new KeywordsAttribute(kw), defaultSingleAttribute));
                        }
                        //return new() { TagType = subTag, RepetitionCount = defaultSingleAttribute };
                        return e.ReplaceTags(subTag, defaultSingleAttribute);
                    }
                    else if (e.TagType is RequireTagsAttribute requireTagsAttr)
                    {
                        if (requireTagsAttr.Tags.Count == 1)
                            return e;

                        HelperTag subTag = new(requireTagsAttr);
                        foreach (var tag in requireTagsAttr.Tags)
                        {
                            Production sub = MakeCopy(subTag, true);
                            result.Add(sub);
                            //sub.Body.Add(new ProductionBodyElement() { TagType = new RequireTagsAttribute(tag), RepetitionCount = defaultSingleAttribute });
                            sub.Body.Add(e.ReplaceTags(new RequireTagsAttribute(tag), defaultSingleAttribute));
                        }
                        //return new() { TagType = subTag, RepetitionCount = defaultSingleAttribute };
                        return e.ReplaceTags(subTag, defaultSingleAttribute);
                    }
                    else if (e.TagType is AnyTokenAttribute || e.TagType is AnyTagAttribute)
                    {
                        return e;
                    }
                    else
                    {
                        Debug.Fail("Unexpected tag type attribute");
                        return e;
                    }
                }

                Production newThis = MakeCopy(self.Tag, self.HasErrorHandler);
                result.Add(newThis);

                for(int i = 0; i < self.Body.Count; )
                {
                    if(self.Body[i].RepetitionCount is SingleAttribute)
                    {
                        newThis.Body.Add(SplitBodyElement(self.Body[i++]));
                    }
                    else if(self.Body[i].RepetitionCount is OptionalAttribute optionalAttribute)
                    {
                        HelperTag subTag = new(optionalAttribute);
                        var subStart = new ProductionBodyElement() { TagType = subTag, RepetitionCount = defaultSingleAttribute, MethodName = self.Body[i].MethodName };
                        List<string> subParamNames = new();

                        // empty production
                        Production emptySub = MakeCopy(subTag, true);
                        Production sub = MakeCopy(subTag, true);
                        sub.Greedy = optionalAttribute.Greedy;
                        sub.Divisor = 1;
                        sub.Body.Add(SplitBodyElement(self.Body[i]));
                        subParamNames.Add(self.Body[i].ParameterName);
                        int divisor = 1;
                        for (i++; i < self.Body.Count && self.Body[i].RepetitionCount is TogetherWithAttribute; i++)
                        {
                            sub.Body.Add(SplitBodyElement(self.Body[i]));
                            subParamNames.Add(self.Body[i].ParameterName);
                            divisor++;
                        }

                        subStart.ParameterName = string.Join(",", subParamNames);
                        newThis.Body.Add(subStart);

                        emptySub.Divisor = divisor;
                        result.Add(emptySub);
                        sub.Divisor = divisor;
                        result.Add(sub);
                    }
                    else if(self.Body[i].RepetitionCount is ManyAttribute manyAttribute)
                    {
                        HelperTag subTag = new(manyAttribute);
                        var subStart = new ProductionBodyElement() { TagType = subTag, RepetitionCount = defaultSingleAttribute, MethodName = self.Body[i].MethodName };
                        List<string> subParamNames = new();

                        // empty or non-rec production
                        Production sub1 = MakeCopy(subTag, true);
                        Production sub2 = MakeCopy(subTag, true);
                        var e = SplitBodyElement(self.Body[i]);
                        sub2.Body.Add(e);
                        if (!manyAttribute.CanBeEmpty) sub1.Body.Add(e);
                        int divisor = 1;
                        for (i++; i < self.Body.Count && self.Body[i].RepetitionCount is TogetherWithAttribute; i++)
                        {
                            subParamNames.Add(self.Body[i].ParameterName);
                            e = SplitBodyElement(self.Body[i]);
                            sub2.Body.Add(e);
                            if (!manyAttribute.CanBeEmpty) sub1.Body.Add(e);
                            divisor++;
                        }

                        subStart.ParameterName = string.Join(",", subParamNames);
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

        private readonly SortedSet<string> tokenTags = new();
        private readonly List<(string, IMachine)> tokens = new();
        private readonly SortedSet<string> keywords = new();
        private readonly List<string> fictiveTokens = new(); // part of tokenTags

        private readonly SortedSet<string> nonTokenTags = new();
        private readonly List<Production> productions = new();
        private readonly SortedSet<string> fictiveNonTokens = new(); // separated with nonTokenTags

        private void AddTokenTag(string tag)
        {
            if (tag is null)
                throw nullTagException;
            if (IsSpecial(tag))
                throw specialTagException;
            if (tokenTags.Contains(tag))
                throw usedTokenTagException;
            if (nonTokenTags.Contains(tag))
                throw usedNonTokenTagException;
            tokenTags.Add(tag);
        }

        internal ParsingEngineBuilder AddTokenViaMachine(string tag, IMachine machine)
        {
            if(tag != TAG_SKIP)
                AddTokenTag(tag);
            tokens.Add((tag, machine));
            return this;
        }

        /// <summary>
        /// Adds fictive token. It may be useful in error handling
        /// </summary>
        /// <param name="tag">Tag of the token</param>
        /// <returns></returns>
        /// <exception cref="ParsingEngineBuildingException"></exception>
        public ParsingEngineBuilder AddFictiveToken(string tag)
        {
            AddTokenTag(tag);
            fictiveTokens.Add(tag);
            return this;
        }

        /// <summary>
        /// Adds token representing only the given string. Has a higher priority than non-keyword tokens.
        /// </summary>
        /// <param name="keyword">The token itself. It will be interpreted "as is", not as regular expression</param>
        /// <returns></returns>
        public ParsingEngineBuilder AddKeyword(string keyword)
        {
            keywords.Add(keyword);
            return this;
        }

        /// <summary>
        /// Adds token represented by regular expression
        /// </summary>
        /// <param name="tag">Tag of the token</param>
        /// <param name="regex">Regular expression representing the token</param>
        /// <returns></returns>
        public ParsingEngineBuilder AddToken(string tag, string regex, params Predicate<char>[] customClasses)
        {
            if (tag != TAG_SKIP)
                AddTokenTag(tag);
            SortedDictionary<string, Predicate<char>> classes = new()
            {
                { "alnum", char.IsLetterOrDigit },
                { "alpha", char.IsLetter },
                { "blank", c => c == ' ' || c == '\t'},
                { "cntrl", char.IsControl},
                { "digit", char.IsDigit},
                { "graph", c => (c >= 0x21 && c <= 0x7E) || (c >= 0x100) },
                { "lower", c => char.IsLetter(c) && char.IsLower(c)},
                { "print", c => (c >= 0x20 && c <= 0x7E) || (c >= 0x100) },
                { "punct", char.IsPunctuation},
                { "space", char.IsWhiteSpace},
                { "upper", c => char.IsLetter(c) && char.IsUpper(c)},
                { "xdigit", c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')}
            };
            for (int i = 0; i < customClasses.Length; i++)
                classes.Add(i.ToString(), customClasses[i]);
            var machine = RegexParser.Instance.Parse(regex, classes);
            tokens.Add((tag, machine));
            return this;
        }

        /// <summary>
        /// Adds fictive non-token. It may be useful in error handling
        /// </summary>
        /// <param name="tag">Tag of the non-token</param>
        /// <returns></returns>
        public ParsingEngineBuilder AddFictiveNonToken(string tag)
        {
            if (tag is null)
                throw nullTagException;
            if (IsSpecial(tag))
                throw specialTagException;
            if (tokenTags.Contains(tag))
                throw usedTokenTagException;
            if (nonTokenTags.Contains(tag))
                throw usedNonTokenTagException;
            fictiveNonTokens.Add(tag);
            return this;
        }

        /// <summary>
        /// Adds production methods to the Parsing Engine
        /// </summary>
        /// <param name="t">Type containing production methods</param>
        /// <returns></returns>
        public ParsingEngineBuilder AddProductions(Type t)
        {
            // method is considered as production method <=>:
            // 1) it is public static
            // 2) returns no void
            // 3) is marked with SetTag attribute
            // This method (AddProductions) checks all production methods of t,
            // then either throws an Exception if any one is incorrect,
            // or adds all the methods to productions list

            foreach(var method in t.GetMethods().Where(m => m.IsPublic && m.IsStatic && m.ReturnType != typeof(void)))
            {
                var tagAttr = method.GetCustomAttribute<SetTagAttribute>();
                if(tagAttr is not null)
                {
                    // now it's correct or not correct production method
                    if (tagAttr.Name is null)
                        throw GetBuildingException(method, nullTagException.Message);
                    if (IsSpecial(tagAttr.Name))
                        throw GetBuildingException(method, specialTagException.Message);
                    if(tokenTags.Contains(tagAttr.Name))
                        throw GetBuildingException(method, usedTokenTagException.Message);
                    if (fictiveNonTokens.Contains(tagAttr.Name))
                        throw GetBuildingException(method, "The token is already defined as a fictive non-token");
                    var p = new Production
                    {
                        Tag = tagAttr.Name,
                        Handler = method,
                        Body = new()
                    };
                    var parameters = method.GetParameters();
                    for(int i = 0; i < parameters.Length; i++)
                    {
                        var e = new ProductionBodyElement();
                        var param = parameters[i];
                        e.MethodName = method.Name;
                        e.ParameterName = param.Name;

                        if (param.ParameterType.IsValueType)
                            throw GetBuildingException(method.Name, param.Name, "No Value Type is allowed to be production method parameter");

                        // handler attrs
                        var errorHandlerAttr = param.GetCustomAttribute<ErrorHandlerAttribute>();
                        if (errorHandlerAttr is not null)
                        {
                            if (i < parameters.Length - 1)
                                throw GetBuildingException(method.Name, param.Name, "Error Handler attribute must mark the last parameter of the method");
                            if (param.ParameterType != typeof(ErrorHandlingDecider))
                                throw GetBuildingException(method.Name, param.Name, "Error Handler attribute must mark the last parameter with the type " + typeof(ErrorHandlingDecider).FullName);
                            p.HasErrorHandler = true;
                            break;
                        }

                        // requiring attrs
                        var requireTagsAttr = param.GetCustomAttribute<RequireTagsAttribute>();
                        if (requireTagsAttr is not null)
                        {
                            if (requireTagsAttr.Tags.Count == 0)
                                throw GetBuildingException(method.Name, param.Name, "At least one tag must be required");
                            foreach(var tag in requireTagsAttr.Tags)
                            {
                                if (tag is null)
                                    throw GetBuildingException(method.Name, param.Name, "Null-tag cannot be required");
                                else if (IsSpecial(tag))
                                    throw GetBuildingException(method.Name, param.Name, "Special tag cannot be required");
                            }
                            e.SetTagType(requireTagsAttr);
                        }
                        var keywordAttr = param.GetCustomAttribute<KeywordsAttribute>();
                        if (keywordAttr is not null)
                        {
                            if(keywordAttr.Keywords is null)
                                throw GetBuildingException(method.Name, param.Name, "Null-token cannot be required");
                            e.SetTagType(keywordAttr);
                        }
                        var anyTagAttr = param.GetCustomAttribute<AnyTagAttribute>();
                        if (anyTagAttr is not null)
                        {
                            e.SetTagType(anyTagAttr);
                        }
                        var anyTokenAttr = param.GetCustomAttribute<AnyTokenAttribute>();
                        if(anyTokenAttr is not null)
                        {
                            e.SetTagType(anyTokenAttr);
                        }
                        // exactly one such attr must exist
                        if (e.TagType is null)
                            throw GetBuildingException(method.Name, param.Name, "Exactly one tag requiring attribute must mark the parameter");

                        // count attrs
                        var singleAttr = param.GetCustomAttribute<SingleAttribute>();
                        if (singleAttr is not null)
                        {
                            e.SetRepetitionCount(singleAttr);
                        }
                        var optionalAttr = param.GetCustomAttribute<OptionalAttribute>();
                        if (optionalAttr is not null)
                        {
                            e.SetRepetitionCount(optionalAttr);
                        }
                        var manyAttr = param.GetCustomAttribute<ManyAttribute>();
                        if (manyAttr is not null)
                        {
                            e.SetRepetitionCount(manyAttr);
                        }
                        var togetherWithAttr = param.GetCustomAttribute<TogetherWithAttribute>();
                        if (togetherWithAttr is not null)
                        {
                            if (i == 0)
                                throw GetBuildingException(method.Name, param.Name, "The first parameter cannot be marked with Together With attribute");
                            if (p.Body[i - 1].RepetitionCount is SingleAttribute)
                                e.SetRepetitionCount(defaultSingleAttribute);
                            else
                                e.SetRepetitionCount(togetherWithAttr);
                        }
                        // set single by default
                        if (e.RepetitionCount is null)
                            e.RepetitionCount = defaultSingleAttribute;

                        p.Body.Add(e);
                    }

                    //successful, we must add the production, some tags and keywords
                    productions.Add(p);
                    nonTokenTags.Add(tagAttr.Name);
                    foreach(var e in p.Body)
                        if(e.TagType is KeywordsAttribute keywordAttr)
                            keywords.UnionWith(keywordAttr.Keywords);
                }
            }

            return this;
        }

        /// <summary>
        /// Adds production methods to the Parsing Engine
        /// </summary>
        /// <typeparam name="T">Type containing production methods</typeparam>
        /// <returns></returns>
        public ParsingEngineBuilder AddProductions<T>()
        {
            return AddProductions(typeof(T));
        }

        private static readonly ParsingEngineBuildingException startIsInvalidException = new("start must be non-fictive non-token tag");

        private interface IGroup
        {
            IEnumerable<object?> Expand();
        }

        private class OptionalGroup : IGroup
        {
            private readonly object?[] children;
            private readonly int divisor;

            public OptionalGroup(object?[] children, int divisor)
            {
                this.children = children;
                this.divisor = divisor;
            }

            public IEnumerable<object?> Expand()
            {
                if (children.Length == divisor)
                    return children;
                return children.Concat(Enumerable.Repeat((object?)null, divisor - children.Length));
            }
        }

        private class UnknownArray
        {
            private readonly object?[] children;

            public UnknownArray(int length)
            {
                children = new object?[length];
            }

            public object? this[int index]
            {
                get => children[index];
                set => children[index] = value;
            }

            private static bool IsAssignable(Type src, Type dest)
            {
                if (src == typeof(Common.Token))
                    return typeof(Parsing.Token).IsAssignableTo(dest) || typeof(string).IsAssignableTo(dest);

                return src.IsAssignableTo(dest);
            }

            private static object? Convert(object? o, Type dest, Func<int, string> typeToStr)
            {
                if(o is Common.Token tk)
                {
                    if (typeof(Parsing.Token).IsAssignableTo(dest))
                        return new Parsing.Token(typeToStr(tk.Type), tk.Self, tk.Line, tk.Column);
                    if (typeof(string).IsAssignableTo(dest))
                        return tk.Self;
                    Debug.Fail("Check before was wrong");
                }
                return o;
            }

            public bool Check(Type t)
            {
                return children.All(x => x is null || IsAssignable(x.GetType(), t));
            }

            public object? Cast(Type t, Func<int, string> typeToStr)
            {
                var result = Array.CreateInstance(t, children.Length);
                for(int i = 0; i < children.Length; i++)
                    result.SetValue(Convert(children[i], t, typeToStr), i);
                return result;
            }
        }

        private class ManyGroup : IGroup
        {
            private readonly object?[] children;
            private readonly int divisor;

            public ManyGroup(object?[] children, int divisor)
            {
                this.children = children;
                this.divisor = divisor;
            }

            private void GetLeaves(List<object?> result)
            {
                foreach (var e in children)
                    if (e is ManyGroup g)
                        g.GetLeaves(result);
                    else
                        result.Add(e);
            }

            public IEnumerable<object?> Expand()
            {
                List<object?> leaves = new();
                GetLeaves(leaves);

                var result = new object?[divisor];
                for(int i = 0; i < divisor; i++)
                {
                    int itemLength = leaves.Count / divisor;
                    if (i < leaves.Count % divisor) itemLength++;
                    result[i] = new UnknownArray(itemLength);
                }
                for(int i = 0; i < leaves.Count; i++)
                    (result[i % divisor] as UnknownArray)[i / divisor] = leaves[i];
                return result;
            }
        }

        private class ProductionHandler : IProductionHandler, IErrorHandler
        {
            private readonly MethodInfo method;
            private readonly bool hasErrorHandler;
            private readonly Func<int, string> typeToStr;
            private readonly Func<Token, int> strToType;

            public ProductionHandler(MethodInfo method, bool hasErrorHandler, Func<int, string> typeToStr, Func<Token, int> strToType)
            {
                this.method = method;
                this.hasErrorHandler = hasErrorHandler;
                this.typeToStr = typeToStr;
                this.strToType = strToType;
            }

            private void AddRange(ParameterInfo[] parameters, ref int ptr, List<object?> args, IEnumerable<object?> objs)
            {
                Debug.Assert(method.DeclaringType is not null);
                foreach (var c in objs)
                {
                    if (c is null)
                        args.Add(null);
                    else if (c is Common.Token t)
                    {
                        if (typeof(Token).IsAssignableTo(parameters[ptr].ParameterType))
                            args.Add(new Token(typeToStr(t.Type), t.Self, t.Line, t.Column));
                        else if (typeof(string).IsAssignableTo(parameters[ptr].ParameterType))
                            args.Add(t.Self);
                        else
                            throw GetParsingException(method, parameters[ptr], "Cannot represent token via the parameter type");
                    }
                    else if (c is IGroup g)
                    {
                        AddRange(parameters, ref ptr, args, g.Expand());
                        continue;
                    }
                    else if (c is UnknownArray a)
                    {
                        var ptype = parameters[ptr].ParameterType;
                        if (!ptype.IsArray)
                            throw GetParsingException(method, parameters[ptr], "Expected array type");
                        ptype = ptype.GetElementType();
                        Debug.Assert(ptype is not null);
                        if (ptype.IsValueType)
                            throw GetParsingException(method, parameters[ptr], "Expected no value type of array elements");
                        if (!a.Check(ptype))
                            throw GetParsingException(method, parameters[ptr], "Cannot represent one or more elements via the array element type");
                        args.Add(a.Cast(ptype, typeToStr));
                    }
                    else
                    {
                        if (c.GetType().IsAssignableTo(parameters[ptr].ParameterType))
                            args.Add(c);
                        else
                            throw GetParsingException(method, parameters[ptr], "Cannot represent non-token via the parameter type");
                    }
                    ptr++;
                }
            }

            public object? Handle(object?[] children)
            {
                var parameters = method.GetParameters();
                List<object?> args = new();
                int ptr = 0;
                AddRange(parameters, ref ptr, args, children);
                if (hasErrorHandler)
                    args.Add(null);
                return method.Invoke(null, args.ToArray());
            }

            public void Handle(object?[] prefix, ParserTools.ErrorHandlingDecider decider)
            {
                var parameters = method.GetParameters();
                List<object?> args = new();
                int ptr = 0;
                AddRange(parameters, ref ptr, args, prefix);
                for (int i = ptr; i < parameters.Length - 1; i++)
                    args.Add(null);

                var userDecider = new ErrorHandlingDecider(decider, typeToStr(decider.NextToken.Type), strToType);
                args.Add(userDecider);
                method.Invoke(null, args.ToArray());
            }
        }

        private class OptionalProductionHandler : IProductionHandler, IErrorHandler
        {
            private readonly int divisor;

            public OptionalProductionHandler(int divisor)
            {
                this.divisor = divisor;
            }

            public object? Handle(object?[] children)
            {
                return new OptionalGroup(children, divisor);
            }

            public void Handle(object?[] prefix, ParserTools.ErrorHandlingDecider decider)
            {
                decider.FoldAndReraise(new OptionalGroup(prefix, divisor));
            }
        }

        private class ManyProductionHandler : IProductionHandler, IErrorHandler
        {
            private readonly int divisor;

            public ManyProductionHandler(int divisor)
            {
                this.divisor = divisor;
            }

            public object? Handle(object?[] children)
            {
                return new ManyGroup(children, divisor);
            }

            public void Handle(object?[] prefix, ParserTools.ErrorHandlingDecider decider)
            {
                decider.FoldAndReraise(new ManyGroup(prefix, divisor));
            }
        }

        private class IDFuncHandler : IProductionHandler, IErrorHandler
        {
            public object? Handle(object?[] children)
            {
                return children[0];
            }

            public void Handle(object?[] prefix, ParserTools.ErrorHandlingDecider decider)
            {
                decider.FoldAndReraise(null);
            }

            public static readonly IDFuncHandler Instance = new();
        }

        private static Func<Token, int> TypeToString(SortedDictionary<string, int> keywordToIndex, SortedDictionary<string, int> tagToIndex, Func<string, int> getTokenType)
        {
            return t => t.Tag switch
                {
                    TAG_KEYWORD => keywordToIndex.ContainsKey(t.Self) ? keywordToIndex[t.Self] : throw new ParsingException($"The keyword {t.Self} does not exist"),
                    TAG_UNKNOWN => getTokenType(t.Self),
                    string s => tagToIndex.ContainsKey(s) ? tagToIndex[s] : throw new ParsingException("Illegal token tag ${s}")
                };
        }

        public ParsingEngine Create(string start)
        {
            // check start
            if (!nonTokenTags.Contains(start))
                throw startIsInvalidException;
            // check that all tokens in productions are defined
            foreach (var p in productions)
                for (int i = 0; i < p.Body.Count; i++)
                    if (p.Body[i].TagType is RequireTagsAttribute requireTagAttr)
                        foreach (var tag in requireTagAttr.Tags)
                            if (!tokenTags.Contains(tag) && !nonTokenTags.Contains(tag) && !fictiveNonTokens.Contains(tag))
                                throw GetBuildingException(p.Handler.Name, p.Handler.GetParameters()[i].Name, $"Tag {tag} is not defined");

            // build lexer
            (int, IMachine)[] tokensForLexer = new (int, IMachine)[keywords.Count + tokens.Count];
            int ptr = 0;
            foreach (var kw in keywords)
                tokensForLexer[ptr] = (ptr++, new TokenMachine(kw));
            foreach (var (_, machine) in tokens)
                tokensForLexer[ptr] = (ptr++, machine);
            var lexer = new Lexer(tokensForLexer);

            // build parser

            // first go keywords, then ordinary tokens, then fictive tokens
            SortedDictionary<string, int> keywordToIndex = new(); // [no ~]
            SortedDictionary<string, int> tokenTagToIndex = new(); // [no ~] part of tagToIndex

            SortedDictionary<string, int> tagToIndex = new(); // all tags except helpers
            SortedDictionary<string, int> helperTagToIndex = new(); // helper tags except AnyToken/AnyTag
            SortedDictionary<string, int> anyTokenToIndex = new(); // AnyToken helper tags
            SortedDictionary<string, int> anyTagToIndex = new(); // AnyTag helper tags
            
            SortedSet<int> whitespaces = new();

            List<string> tokenName = new();
            List<string> showedTokensNames = new();

            int totalTokensCount = 0;
            foreach (var kw in keywords)
            {
                tokenName.Add(TAG_KEYWORD);
                showedTokensNames.Add(ProductionBodyElement.ShowKeyword(kw));
                keywordToIndex.Add(kw, totalTokensCount++);
            }
            foreach (var (tag, _) in tokens)
            {
                if (tag == TAG_SKIP)
                {
                    showedTokensNames.Add("[whitespace]");
                    whitespaces.Add(totalTokensCount++);
                }
                else
                {
                    showedTokensNames.Add(tag);
                    tokenTagToIndex.Add(tag, totalTokensCount);
                    tagToIndex.Add(tag, ~(totalTokensCount++));
                }

                tokenName.Add(tag);
            }
            foreach (var tag in fictiveTokens)
            {
                tagToIndex.Add(tag, ~(totalTokensCount++));
                tokenName.Add(tag);
                showedTokensNames.Add(tag);
            }

            List<string> showedNonTokensNames = new();
            int totalNonTokensCount = 0;
            foreach(var tag in nonTokenTags.Concat(fictiveNonTokens))
            {
                tagToIndex.Add(tag, totalNonTokensCount++);
                showedNonTokensNames.Add(tag);
            }

            int minNonHelperTag = totalNonTokensCount;
            List<HelperTag> allht = new();

            List<Production> splittedProductions = productions.SelectMany(Production.SplitIntoSimpleProductions).ToList();
            foreach (var p in splittedProductions)
            {
                if (p.Tag is HelperTag ht && !helperTagToIndex.ContainsKey(ht.Name))
                {
                    helperTagToIndex.Add(ht.Name, totalNonTokensCount++);
                    showedNonTokensNames.Add(ProductionBodyElement.ShowHelperTag(ht));
                    allht.Add(ht);
                }

                for(int i = 0; i < p.Body.Count; i++)
                {
                    if(p.Body[i].TagType is AnyTokenAttribute anyToken)
                    {
                        HelperTag subTag = new(anyToken);
                        anyTokenToIndex.Add(subTag.Name, totalNonTokensCount++);
                        showedNonTokensNames.Add(ProductionBodyElement.ShowHelperTag(subTag));
                        allht.Add(subTag);
                        p.Body[i] = new() { TagType = subTag, RepetitionCount = p.Body[i].RepetitionCount };
                    }
                    else if(p.Body[i].TagType is AnyTagAttribute anyTag)
                    {
                        HelperTag subTag = new(anyTag);
                        anyTagToIndex.Add(subTag.Name, totalNonTokensCount++);
                        showedNonTokensNames.Add(ProductionBodyElement.ShowHelperTag(subTag));
                        allht.Add(subTag);
                        p.Body[i] = new() { TagType = subTag, RepetitionCount = p.Body[i].RepetitionCount };
                    }
                }
            }

            var grammarBuilder = new GrammarBuilder(totalTokensCount, totalNonTokensCount, tagToIndex[start]);
            
            string TokenTypeToStr(int id)
                => (id < 0 ? TAG_UNDEFINED : (id == tokenName.Count ? TAG_EOF : tokenName[id]));
            
            int GetIndexOfTag(object tag)
            {
                if(tag is string s) return tagToIndex[s];
                if(tag is HelperTag ht)
                {
                    if(helperTagToIndex.ContainsKey(ht.Name))
                        return helperTagToIndex[ht.Name];
                    if(anyTokenToIndex.ContainsKey(ht.Name))
                        return anyTokenToIndex[ht.Name];
                    if(anyTagToIndex.ContainsKey(ht.Name))
                        return anyTagToIndex[ht.Name];
                }
                Debug.Fail("Invalid tag");
                return -1;
            }

            List<object> addedProductionInfo = new(); // either index i of splittedProductions or AnyToken/AnyTag helper tag
            for(int j = 0; j < splittedProductions.Count; j++)
            {
                var p = splittedProductions[j];
                int pStart = GetIndexOfTag(p.Tag);
                List<int> body = new();
                for(int i = 0; i < p.Body.Count; i++)
                {
                    var e = p.Body[i];
                    var tt = e.TagType;
                    int singleID;
                    if (tt is KeywordsAttribute ka)
                    {
                        singleID = ~keywordToIndex[ka.Keywords[0]]; // because no ~
                    }
                    else if (tt is RequireTagsAttribute rta)
                    {
                        singleID = tagToIndex[rta.Tags[0]];
                    }
                    else if(tt is HelperTag htt)
                    {
                        if(helperTagToIndex.ContainsKey(htt.Name))
                        {
                            singleID = helperTagToIndex[htt.Name];
                        }
                        else if(anyTokenToIndex.ContainsKey(htt.Name))
                        {
                            singleID = anyTokenToIndex[htt.Name];
                            foreach (var tag in tokenTags)
                            {
                                grammarBuilder.AddProduction(
                                    singleID,
                                    new int[] { tagToIndex[tag] },
                                    IDFuncHandler.Instance,
                                    IDFuncHandler.Instance);
                                addedProductionInfo.Add(htt);
                            }
                        }
                        else if(anyTagToIndex.ContainsKey(htt.Name))
                        {
                            singleID = anyTagToIndex[htt.Name];
                            foreach (var tag in tagToIndex.Keys)
                            {
                                grammarBuilder.AddProduction(
                                    singleID,
                                    new int[] { GetIndexOfTag(tag) },
                                    IDFuncHandler.Instance,
                                    IDFuncHandler.Instance);
                                addedProductionInfo.Add(htt);
                            }
                        }
                        else
                        {
                            Debug.Fail("Unexpected token type");
                            singleID = -1;
                        }
                    }
                    else
                    {
                        Debug.Fail("Unexpected token type");
                        singleID = -1;
                    }
                    body.Add(singleID);
                }

                IProductionHandler productionHandler;
                IErrorHandler errorHandler;

                if(p.Tag is HelperTag ht)
                {
                    if (ht.ParentAttribute is OptionalAttribute)
                    {
                        var h = new OptionalProductionHandler(p.Divisor);
                        productionHandler = h;
                        errorHandler = h;
                    }
                    else if (ht.ParentAttribute is ManyAttribute)
                    {
                        var h = new ManyProductionHandler(p.Divisor);
                        productionHandler = h;
                        errorHandler = h;
                    }
                    else
                    {
                        var h = IDFuncHandler.Instance;
                        productionHandler = h;
                        errorHandler = h;
                    }
                }
                else
                {
                    var h = new ProductionHandler(p.Handler, p.HasErrorHandler, TokenTypeToStr, TypeToString(keywordToIndex, tokenTagToIndex, lexer.SingleAnalyze));
                    productionHandler = h;
                    errorHandler = h;
                }

                grammarBuilder.AddProduction(
                    pStart,
                    body.ToArray(),
                    productionHandler,
                    p.HasErrorHandler ? errorHandler : null);
                addedProductionInfo.Add(j);
            }

            // searching for greedies
            for(int i = 0; i < addedProductionInfo.Count; i++)
            {
                if(addedProductionInfo[i] is int index && splittedProductions[index].Greedy)
                {
                    var pStart = splittedProductions[index].Tag;
                    for (int j = 0; j < addedProductionInfo.Count; j++)
                        if (i != j && addedProductionInfo[j] is int other && splittedProductions[other].Tag.Equals(pStart))
                            grammarBuilder.AddBanRule(j, i);
                }
            }

            try
            {
                return new ParsingEngine(lexer, grammarBuilder.CreateMachine(), tk => !whitespaces.Contains(tk.Type), TokenTypeToStr);
            }
            catch (LRConflictException e)
            {
                SortedDictionary<string, int> helperTagOwner = new(); // ht to production index
                SortedDictionary<string, int> secondOwner = new(); // may be the second owner
                SortedDictionary<string, List<int>> helperTagProductions = new(); // ht to productions starting with it

                for(int i = 0; i < addedProductionInfo.Count; i++)
                {
                    var info = addedProductionInfo[i];
                    if(info is int index)
                    {
                        var p = splittedProductions[index];
                        if(p.Tag is HelperTag ht)
                        {
                            if(!helperTagProductions.ContainsKey(ht.Name))
                                helperTagProductions.Add(ht.Name, new());
                            helperTagProductions[ht.Name].Add(i);
                        }

                        foreach (var element in p.Body)
                            if (element.TagType is HelperTag subht)
                            {
                                if (p.Tag is HelperTag parentTag && parentTag.Name == subht.Name) continue;
                                if (helperTagOwner.ContainsKey(subht.Name))
                                    secondOwner.Add(subht.Name, i);
                                else
                                    helperTagOwner.Add(subht.Name, i);
                            }
                    }
                }

                SortedSet<int> productionsUsed = new();

                void PrintProduction(int production, StringBuilder output)
                {
                    var info = addedProductionInfo[production];
                    if(info is HelperTag ht)
                    {
                        if (ht.ParentAttribute is AnyTokenAttribute)
                            output.Append($"{ht.Name} -> [any token]\n");
                        else if(ht.ParentAttribute is AnyTagAttribute)
                            output.Append($"{ht.Name} -> [any tag]\n");
                    }
                    else if(info is int splittedProductionIndex)
                    {
                        var p = splittedProductions[splittedProductionIndex];
                        output.Append($"{p.Tag} -> \n{string.Join('\n', p.Body.Select(e => $"\t[{e.MethodName}(...,{e.ParameterName},...)]{e.FullTagName}"))}\n");
                    }
                }

                void DependenciesDFS(int production, StringBuilder output, bool spawnWhere)
                {
                    if(productionsUsed.Add(production))
                    {
                        PrintProduction(production, output);
                        
                        if(addedProductionInfo[production] is int splittedProductionIndex)
                        {
                            foreach (var e in splittedProductions[splittedProductionIndex].Body)
                                if (e.TagType is HelperTag ht)
                                    foreach (var p in helperTagProductions[ht.Name])
                                    {
                                        if (spawnWhere) output.Append("where\n");
                                        spawnWhere = false;
                                        DependenciesDFS(p, output, false);
                                    }
                        }
                    }
                }

                void GoToRoot(int p1, int p2, StringBuilder output)
                {
                    //output.Append("where\n");
                    DependenciesDFS(p1, output, true);
                    if(p2 >= 0) DependenciesDFS(p2, output, true);

                    object info = addedProductionInfo[p1];
                    if(info is HelperTag ht)
                    {
                        Debug.Assert(
                            p2 < 0 
                            || addedProductionInfo[p2] is HelperTag ht2 
                            && helperTagOwner[ht2.Name] == helperTagOwner[ht.Name]);

                        output.Append("being a part of\n");
                        p1 = helperTagOwner[ht.Name];
                        p2 = secondOwner.ContainsKey(ht.Name) ? secondOwner[ht.Name] : -1;
                        GoToRoot(p1, p2, output);
                    }
                    else if(info is int splittedProductionIndex)
                    {
                        var start = splittedProductions[splittedProductionIndex].Tag;
                        if(start is HelperTag hts)
                        {
                            Debug.Assert(
                                p2 < 0
                                || addedProductionInfo[p2] is int secondIndex
                                && splittedProductions[secondIndex].Tag is HelperTag ht2
                                && helperTagOwner[ht2.Name] == helperTagOwner[hts.Name]);

                            p1 = helperTagOwner[hts.Name];
                            p2 = secondOwner.ContainsKey(hts.Name) ? secondOwner[hts.Name] : -1;
                            output.Append("being a part of\n");
                            GoToRoot(p1, p2, output);
                        }
                    }
                }

                string GetFullView(int production)
                {
                    StringBuilder sb = new();
                    productionsUsed.Clear();
                    GoToRoot(production, -1, sb);
                    return sb.ToString();
                }

                ParsingConflictOpponent CreateOpponent(LROpponent opponent)
                {
                    if (opponent.Production is null)
                        return new(null, null, opponent.IsCarry);

                    int p = opponent.Production.Value;
                    var info = addedProductionInfo[p];

                    if(info is HelperTag ht)
                    {
                        string type = "";
                        if (ht.ParentAttribute is AnyTokenAttribute)
                            type = "[any token]";
                        else if (ht.ParentAttribute is AnyTagAttribute)
                            type = "[any tag]";
                        else
                            Debug.Fail("Unexpected helper tag");
                        return new(ht.Name, $"{ht.Name} -> {type}", opponent.IsCarry);
                    }

                    var prod = splittedProductions[(int)info];

                    return new(
                        prod.Tag.ToString(),
                        GetFullView(p),
                        opponent.IsCarry);
                }

                // ???
                // лучше не использовать
                // вместо этого передавать в way собственно токенный путь
                // плюс надо как-то восстанавливать всю иерархию правил
                IEnumerable<int> ExpandWay(IEnumerable<int> way)
                {
                    List<int> result = new();
                    void rec(IEnumerable<int> w)
                    {
                        foreach(var e in w)
                        {
                            if (e < minNonHelperTag)
                                result.Add(e);
                            else
                            {
                                foreach(var p in helperTagProductions[allht[e - minNonHelperTag].Name])
                                {
                                    var body = grammarBuilder.GetBodyByProductionIndex(p);
                                    if (body.Contains(e)) continue;
                                    rec(body);
                                    break;
                                }
                            }
                        }    
                    }
                    rec(way);
                    return result;
                }

                //Console.WriteLine("Diagnostics: all productions:\n");
                //
                //StringBuilder sbb = new();
                //for(int i = 0; i < addedProductionInfo.Count; i++)
                //{
                //    sbb.Clear();
                //    PrintProduction(i, sbb);
                //    Console.WriteLine(sbb);
                //}
                //Console.WriteLine();

                throw new ParsingConflictException(
                    ExpandWay(e.Way).Select(id => (id < 0 ? showedTokensNames[~id] : showedNonTokensNames[id])).ToArray(),
                    e.TokenType.HasValue ? showedTokensNames[e.TokenType.Value] : null,
                    CreateOpponent(e.First),
                    CreateOpponent(e.Second));
            }
        }
    }
}
