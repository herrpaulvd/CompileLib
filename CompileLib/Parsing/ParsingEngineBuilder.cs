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
using CompileLib.Common;
using System.Security;

namespace CompileLib.Parsing
{
    /// <summary>
    /// Builder of parsers
    /// </summary>
    public class ParsingEngineBuilder
    {
        private const string errorUnexpectedNullTag = "Unexpected null tag";
        private const string errorUnexpectedSpecialTag = "Unexpected special tag";
        private const string errorUsedTokenTag = "The tag is already used as a token tag";
        private const string errorUsedNonTokenTag = "The tag is already used as a non-token tag";
        private const string errorUsedFictiveNonTokenTag = "The token is already defined as a fictive non-token";
        private const string errorStartIsInvalid = "Start must be non-fictive non-token tag";
        private const string errorEHDLastRequired = "Error Handler attribute must mark the last parameter of the method";
        private static readonly string errorEHDInvalidType = "Error Handler attribute must mark the last parameter with the type " + typeof(ErrorHandlingDecider).FullName;
        private const string errorValueType = "No Value Type is allowed to be production method parameter";

        /// <summary>
        /// Set of all added tokens' tags INCLUDING fictives
        /// </summary>
        private readonly SortedSet<string> tokenTags = new();
        /// <summary>
        /// Array of all added pairs (tag, machine)
        /// </summary>
        private readonly List<(string, IMachine)> tokens = new();
        /// <summary>
        /// Set of all added keywords
        /// </summary>
        private readonly SortedSet<string> keywords = new();
        /// <summary>
        /// Array of all added fictive tokens
        /// </summary>
        private readonly List<string> fictiveTokens = new();
        /// <summary>
        /// Set of all added non-tokens' tags EXCLUDING fictives
        /// </summary>
        private readonly SortedSet<string> nonTokenTags = new();
        /// <summary>
        /// Array of all generated productions
        /// </summary>
        private readonly List<Production> productions = new();
        /// <summary>
        /// Array of all added fictive non-tokens
        /// </summary>
        private readonly SortedSet<string> fictiveNonTokens = new();
        /// <summary>
        /// Set of all expression tags with the corresponding operations
        /// </summary>
        private readonly SortedDictionary<string, ExpressionTagOperationsSet> operations = new();

        /// <summary>
        /// Performing some actions to register an operation
        /// </summary>
        /// <param name="tag">Expression tag</param>
        /// <param name="handler">Production handler method</param>
        /// <param name="sign">Sign of the operation</param>
        /// <param name="priority">Priority of the operation</param>
        /// <param name="binary">If true, the op is binary, unary otherwise</param>
        /// <param name="right">If true, the op is right-associative, left- otherwise</param>
        /// <exception cref="ParsingEngineBuildingException"></exception>
        private void AddOperation(string tag, MethodInfo handler, string sign, int priority, bool binary, bool right)
        {
            if (!operations.ContainsKey(tag))
                operations.Add(tag, new(tag));
            operations[tag].AddOperation(handler, sign, priority, binary, right);
            AddKeyword(sign);
        }

        /// <summary>
        /// Checks if tag is used and adds it into the set otherwise
        /// </summary>
        /// <param name="tag">The tag to be checked</param>
        /// <exception cref="ParsingEngineBuildingException"></exception>
        private void AddTokenTag(string tag)
        {
            if (tag is null)
                throw new ParsingEngineBuildingException(errorUnexpectedNullTag);
            if (IsSpecial(tag))
                throw new ParsingEngineBuildingException(errorUnexpectedSpecialTag);
            if (tokenTags.Contains(tag))
                throw new ParsingEngineBuildingException(errorUsedTokenTag);
            if (nonTokenTags.Contains(tag))
                throw new ParsingEngineBuildingException(errorUsedNonTokenTag);
            tokenTags.Add(tag);
        }

        /// <summary>
        /// Adds token directly as a pair (tag, machine)
        /// </summary>
        /// <param name="tag">Token's tag</param>
        /// <param name="machine">Token's machine</param>
        /// <returns></returns>
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
                throw new ParsingEngineBuildingException(errorUnexpectedNullTag);
            if (IsSpecial(tag))
                throw new ParsingEngineBuildingException(errorUnexpectedSpecialTag);
            if (tokenTags.Contains(tag))
                throw new ParsingEngineBuildingException(errorUsedTokenTag);
            if (nonTokenTags.Contains(tag))
                throw new ParsingEngineBuildingException(errorUsedNonTokenTag);
            fictiveNonTokens.Add(tag);
            return this;
        }

        private void AddSetTagProduction(MethodInfo method, SetTagAttribute tagAttr)
        {
            // now it's correct or not correct SetTag-production method
            if (tagAttr.Name is null)
                throw new ParsingEngineBuildingException(method, errorUnexpectedNullTag);
            if (IsSpecial(tagAttr.Name))
                throw new ParsingEngineBuildingException(method, errorUnexpectedSpecialTag);
            if (tokenTags.Contains(tagAttr.Name))
                throw new ParsingEngineBuildingException(method, errorUsedTokenTag);
            if (fictiveNonTokens.Contains(tagAttr.Name))
                throw new ParsingEngineBuildingException(method, errorUsedFictiveNonTokenTag);
            var p = new Production
            {
                Tag = tagAttr.Name.ToTag<HelperTag>(),
                Handler = method,
                Body = new()
            };
            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                var e = new ProductionBodyElement();
                var param = parameters[i];
                e.Method = method;
                e.Parameter = param;

                if (param.ParameterType.IsValueType)
                    throw new ParsingEngineBuildingException(method, param, errorValueType);

                // handler attrs
                var errorHandlerAttr = param.GetCustomAttribute<ErrorHandlerAttribute>();
                if (errorHandlerAttr is not null)
                {
                    if (i != parameters.Length - 1)
                        throw new ParsingEngineBuildingException(method, param, errorEHDLastRequired);
                    if (param.ParameterType != typeof(ErrorHandlingDecider))
                        throw new ParsingEngineBuildingException(method, param, errorEHDInvalidType);
                    p.HasErrorHandler = true;
                    break;
                }

                // requiring attrs
                var requireTagsAttr = param.GetCustomAttribute<RequireTagsAttribute>();
                if (requireTagsAttr is not null)
                {
                    if (requireTagsAttr.Tags.Count == 0)
                        throw new ParsingEngineBuildingException(method, param, "At least one tag must be required");
                    foreach (var tag in requireTagsAttr.Tags)
                    {
                        if (tag is null)
                            throw new ParsingEngineBuildingException(method, param, "Null-tag cannot be required");
                        else if (IsSpecial(tag))
                            throw new ParsingEngineBuildingException(method, param, "Special tag cannot be required");
                    }
                    e.SetTagType(requireTagsAttr);
                }
                var keywordAttr = param.GetCustomAttribute<KeywordsAttribute>();
                if (keywordAttr is not null)
                {
                    if (keywordAttr.Keywords is null)
                        throw new ParsingEngineBuildingException(method, param, "Null-token cannot be required");
                    e.SetTagType(keywordAttr);
                }
                // exactly one such attr must exist
                if (e.TagType is null)
                    throw new ParsingEngineBuildingException(method, param, "Exactly one tag requiring attribute must mark the parameter");

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
                        throw new ParsingEngineBuildingException(method, param, "The first parameter cannot be marked with TogetherWith attribute");
                    if (p.Body[i - 1].RepetitionCount is SingleAttribute)
                        e.SetRepetitionCount(SingleAttribute.Instance);
                    else
                        e.SetRepetitionCount(togetherWithAttr);
                }
                // set single by default
                e.RepetitionCount ??= SingleAttribute.Instance;
                p.Body.Add(e);
            }

            //successful, we must add the production, some tags and keywords
            productions.Add(p);
            nonTokenTags.Add(tagAttr.Name);
            foreach (var e in p.Body)
                if (e.TagType is KeywordsAttribute keywordAttr)
                    keywords.UnionWith(keywordAttr.Keywords);
        }

        private void AddBinaryOperations(MethodInfo method, string tag, BinaryOperationAttribute[] operations)
        {
            var parameters = method.GetParameters();
            int n = parameters.Length;
            int argsNumber = ExpressionTagOperationsSet.BinaryMethodParamsCount;
            for(int i = 0; i < n; i++)
            {
                var param = parameters[i];
                if (param.ParameterType.IsValueType)
                    throw new ParsingEngineBuildingException(method, param, errorValueType);

                // handler attrs
                var errorHandlerAttr = param.GetCustomAttribute<ErrorHandlerAttribute>();
                if (errorHandlerAttr is not null)
                {
                    if (i != n - 1)
                        throw new ParsingEngineBuildingException(method, param, errorEHDLastRequired);
                    if (param.ParameterType != typeof(ErrorHandlingDecider))
                        throw new ParsingEngineBuildingException(method, param, errorEHDInvalidType);
                    argsNumber++;
                    break;
                }
            }

            if (n != argsNumber)
                throw new ParsingEngineBuildingException(method, $"Expected {argsNumber} parameters but {n} found");

            foreach (var attr in operations)
                AddOperation(tag, method, attr.Sign, attr.Priority, true, attr.IsRightAssociative);
        }

        private void AddUnaryOperations(MethodInfo method, string tag, UnaryOperationAttribute[] operations)
        {
            var parameters = method.GetParameters();
            int n = parameters.Length;
            int argsNumber = ExpressionTagOperationsSet.UnaryMethodParamsCount;
            for (int i = 0; i < n; i++)
            {
                var param = parameters[i];
                if (param.ParameterType.IsValueType)
                    throw new ParsingEngineBuildingException(method, param, errorValueType);

                // handler attrs
                var errorHandlerAttr = param.GetCustomAttribute<ErrorHandlerAttribute>();
                if (errorHandlerAttr is not null)
                {
                    if (i != n - 1)
                        throw new ParsingEngineBuildingException(method, param, errorEHDLastRequired);
                    if (param.ParameterType != typeof(ErrorHandlingDecider))
                        throw new ParsingEngineBuildingException(method, param, errorEHDInvalidType);
                    argsNumber++;
                    break;
                }
            }

            if (n != argsNumber)
                throw new ParsingEngineBuildingException(method, $"Expected {argsNumber} parameters but {n} found");

            foreach (var attr in operations)
                AddOperation(tag, method, attr.Sign, attr.Priority, false, attr.IsSuffix);
        }

        private void AddSetExpressionTagProduction(MethodInfo method, SetExpressionTagAttribute tagAttr)
        {
            // now it's correct or not correct SetExpressionTag-production method
            if (tagAttr.Name is null)
                throw new ParsingEngineBuildingException(method, errorUnexpectedNullTag);
            if (IsSpecial(tagAttr.Name))
                throw new ParsingEngineBuildingException(method, errorUnexpectedSpecialTag);
            if (tokenTags.Contains(tagAttr.Name))
                throw new ParsingEngineBuildingException(method, errorUsedTokenTag);
            if (fictiveNonTokens.Contains(tagAttr.Name))
                throw new ParsingEngineBuildingException(method, errorUsedFictiveNonTokenTag);

            BinaryOperationAttribute[] binaries = method.GetCustomAttributes<BinaryOperationAttribute>().ToArray();
            UnaryOperationAttribute[] unaries = method.GetCustomAttributes<UnaryOperationAttribute>().ToArray();

            if(binaries.Length == 0)
            {
                if (unaries.Length == 0)
                    throw new ParsingEngineBuildingException(method, "The method must be marked with non-zero number of either binary operations or unary operations attributes");
                else
                    AddUnaryOperations(method, tagAttr.Name, unaries);
            }
            else
            {
                if (unaries.Length == 0)
                    AddBinaryOperations(method, tagAttr.Name, binaries);
                else
                    throw new ParsingEngineBuildingException(method, "The method cannot be marked with binary operations and unary operations attributes at the same time");
            }
            nonTokenTags.Add(tagAttr.Name);
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
            // 2) returns something but void
            // 3) is marked with SetTag OR SetExpressionTag attribute
            // This method (AddProductions) checks all production methods of t,
            // then either throws an Exception if any one is incorrect,
            // or adds all the methods to productions list

            foreach(var method in t.GetMethods().Where(m => m.IsPublic && m.IsStatic && m.ReturnType != typeof(void)))
            {
                var setTagAttr = method.GetCustomAttribute<SetTagAttribute>();
                var setExprTagAttr = method.GetCustomAttribute<SetExpressionTagAttribute>();
                if (setTagAttr is not null)
                {
                    if (setExprTagAttr is not null)
                        throw new ParsingEngineBuildingException(method, "Production method cannot be marked with SetTag and SetExpressionTag attributes at the same time");
                    AddSetTagProduction(method, setTagAttr);
                }
                else if (setExprTagAttr is not null)
                    AddSetExpressionTagProduction(method, setExprTagAttr);
                // otherwise the method is not a production method
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

        private static Func<Parsed<string>, int?> TokenToType(SortedDictionary<string, int> keywordToIndex, SortedDictionary<string, int> tagToIndex, Func<string, int?> getTokenType)
        {
            return t => t.Tag switch
                {
                    TAG_KEYWORD => keywordToIndex.ContainsKey(t.Self) ? keywordToIndex[t.Self] : throw new ParsingException($"The keyword {t.Self} does not exist"),
                    TAG_UNKNOWN => getTokenType(t.Self),
                    string s => tagToIndex.ContainsKey(s) ? tagToIndex[s] : throw new ParsingException($"Illegal token tag ${s}")
                };
        }

        public ParsingEngine Create(string start)
        {
            // check start
            if (!nonTokenTags.Contains(start))
                throw new ParsingEngineBuildingException(errorStartIsInvalid);
            // check that all tags in productions are defined
            foreach (var p in productions)
                for (int i = 0; i < p.Body.Count; i++)
                    if (p.Body[i].TagType is RequireTagsAttribute requireTagAttr)
                        foreach (var tag in requireTagAttr.Tags)
                            if (!tokenTags.Contains(tag) && !nonTokenTags.Contains(tag) && !fictiveNonTokens.Contains(tag))
                                throw new ParsingEngineBuildingException(p.Handler, p.Handler.GetParameters()[i], $"Tag {tag} is not defined");

            // build lexer
            (int, IMachine)[] tokensForLexer = new (int, IMachine)[keywords.Count + tokens.Count];
            int ptr = 0;
            foreach (var kw in keywords)
                tokensForLexer[ptr] = (ptr++, new TokenMachine(kw));
            foreach (var (_, machine) in tokens)
                tokensForLexer[ptr] = (ptr++, machine);
            var lexer = new Lexer(tokensForLexer);

            // build parser

            //TODO!!!!!!!!!!!!!!!!! Add expression sets performing

            // first go keywords, then ordinary tokens, then fictive tokens
            SortedDictionary<string, int> keywordToIndex = new(); // [no ~]
            SortedDictionary<string, int> tokenTagToIndex = new(); // [no ~] part of tagToIndex

            SortedDictionary<string, int> tagToIndex = new(); // all tags except helpers
            SortedDictionary<long, int> helperTagToIndex = new(); // helper tags
            
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
            List<string> nonTokenName = showedNonTokensNames; // for now, it's alias but in the future it can be changed
            int totalNonTokensCount = 0;
            foreach(var tag in nonTokenTags.Concat(fictiveNonTokens))
            {
                tagToIndex.Add(tag, totalNonTokensCount++);
                showedNonTokensNames.Add(tag);
            }

            //DEBUG ONLY
            //string logfilename = "output" + new string(DateTime.Now.ToString().Where(char.IsDigit).ToArray()) + ".txt";
            //StringBuilder log = new();
            //for (int i = 0; i < showedTokensNames.Count; i++)
            //    log.Append($"{~i} = {showedTokensNames[i]}\n");
            //for (int i = 0; i < showedNonTokensNames.Count; i++)
            //    log.Append($"{i} = {showedNonTokensNames[i]}\n");
            //System.IO.File.WriteAllText(logfilename, log.ToString());
            //Console.WriteLine("Successful writing log to " + logfilename);
            //END

            int minNonHelperTag = totalNonTokensCount;
            List<HelperTag> allHelperTags = new();

            var splittedProductions = productions.SelectMany(Production.SplitIntoSimpleProductions).ToList();
            foreach (var ht in splittedProductions.Where(p => p.Tag.SecondType()).Select(p => p.Tag.Second))
            {
                if (!helperTagToIndex.ContainsKey(ht.ID))
                {
                    helperTagToIndex.Add(ht.ID, totalNonTokensCount++);
                    showedNonTokensNames.Add(ProductionBodyElement.ShowHelperTag(ht));
                    allHelperTags.Add(ht);
                }
            }

            List<(int, string)> carryBans = new();
            List<(int, string)> foldingBans = new();
            foreach (var set in operations.Values)
                set.GetProductions(splittedProductions, foldingBans, carryBans);

            var grammarBuilder = new GrammarBuilder(totalTokensCount, totalNonTokensCount, tagToIndex[start]);

            string TokenTypeToString(int? id)
                => id.HasValue ? (id.Value == totalTokensCount ? TAG_EOF : tokenName[id.Value]) : TAG_UNDEFINED;
            string NonTokenTypeToString(int? id)
                => id.HasValue ? nonTokenName[id.Value] : TAG_UNDEFINED;

            int GetIndexOfTag(Alternation<string, HelperTag> tag)
                => tag.FirstType() ? tagToIndex[tag.First] : helperTagToIndex[tag.Second.ID];

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
                        singleID = ~keywordToIndex[ka.Keywords[0]]; // because wk2index without ~
                    }
                    else if (tt is RequireTagsAttribute rta)
                    {
                        singleID = tagToIndex[rta.Tags[0]];
                    }
                    else if(tt is HelperTag htt)
                    {
                        singleID = helperTagToIndex[htt.ID];
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

                if(p.Tag.SecondType())
                {
                    var ht = p.Tag.Second;
                    if (ht.ParentAttribute is OptionalAttribute)
                    {
                        var h = new GroupHandler<OptionalGroup>(p.Divisor);
                        productionHandler = h;
                        errorHandler = h;
                    }
                    else if (ht.ParentAttribute is ManyAttribute)
                    {
                        var h = new GroupHandler<ManyGroup>(p.Divisor);
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
                    var h = new UserHandler(p.Handler, p.HasErrorHandler, TokenToType(keywordToIndex, tokenTagToIndex, lexer.SingleAnalyze));
                    productionHandler = h;
                    errorHandler = h;
                }

                grammarBuilder.AddProduction(
                    pStart,
                    body.ToArray(),
                    productionHandler,
                    p.HasErrorHandler ? errorHandler : null);
            }

            for(int i = 0; i < splittedProductions.Count; i++)
            {
                if (splittedProductions[i].Greedy != 0)
                {
                    int j = i + splittedProductions[i].Greedy;
                    grammarBuilder.AddBanFoldingWhenFirstRule(j, i);
                }
            }

            foreach (var (prod, kw) in foldingBans)
                grammarBuilder.AddBanFoldingWhenCharacterRule(prod, ~keywordToIndex[kw]);
            foreach (var (prod, kw) in carryBans)
                grammarBuilder.AddBanCarryWhenCharacterRule(prod, ~keywordToIndex[kw]);

            try
            {
                return new ParsingEngine(lexer, grammarBuilder.CreateMachine(TokenTypeToString, NonTokenTypeToString), tk => tk.Type.HasValue && !whitespaces.Contains(tk.Type.Value), TokenTypeToString);
            }
            catch (LRConflictException e)
            {
                SortedDictionary<long, int> helperTagOwner = new(); // ht to production index
                SortedDictionary<long, int> secondOwner = new(); // may be the second owner
                SortedDictionary<long, List<int>> helperTagProductions = new(); // ht to productions starting with it

                for (int i = 0; i < splittedProductions.Count; i++)
                {
                    var p = splittedProductions[i];
                    if (p.Tag.SecondType())
                    {
                        var ht = p.Tag.Second;
                        if (!helperTagProductions.ContainsKey(ht.ID))
                            helperTagProductions.Add(ht.ID, new());
                        helperTagProductions[ht.ID].Add(i);
                    }
                    foreach (var element in p.Body)
                        if (element.TagType is HelperTag subht)
                        {
                            if (p.Tag.SecondType() && p.Tag.Second.ID == subht.ID) 
                                continue;
                            if (helperTagOwner.ContainsKey(subht.ID))
                                secondOwner.Add(subht.ID, i);
                            else
                                helperTagOwner.Add(subht.ID, i);

                        }
                }

                SortedSet<int> productionsUsed = new();

                void PrintProduction(int production, StringBuilder output)
                {
                    var p = splittedProductions[production];
                    output.Append($"{p.Tag} -> \n{string.Join('\n', p.Body.Select(e => $"\t[{e.Method.Name}(...,{e.Parameter.Name},...)]{e.FullTagName}"))}\n");
                }

                void DependenciesDFS(int production, StringBuilder output, bool spawnWhere)
                {
                    if (productionsUsed.Add(production))
                    {
                        PrintProduction(production, output);

                        foreach (var e in splittedProductions[production].Body)
                            if (e.TagType is HelperTag ht)
                                foreach (var p in helperTagProductions[ht.ID])
                                {
                                    if (spawnWhere) output.Append("where\n");
                                    spawnWhere = false;
                                    DependenciesDFS(p, output, false);
                                }
                    }
                }

                void GoToRoot(int p1, int p2, StringBuilder output)
                {
                    DependenciesDFS(p1, output, true);
                    if (p2 >= 0) DependenciesDFS(p2, output, true);

                    var start = splittedProductions[p1].Tag;
                    if (start.SecondType())
                    {
                        var hts = start.Second;
                        p1 = helperTagOwner[hts.ID];
                        p2 = secondOwner.ContainsKey(hts.ID) ? secondOwner[hts.ID] : -1;
                        output.Append("being a part of\n");
                        GoToRoot(p1, p2, output);
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
                    var prod = splittedProductions[p];
                    return new(
                        prod.Tag.ToString(),
                        GetFullView(p),
                        opponent.IsCarry);
                }

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
                                foreach(var p in helperTagProductions[allHelperTags[e - minNonHelperTag].ID])
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

                throw new ParsingConflictException(
                    ExpandWay(e.Way).Select(id => (id < 0 ? showedTokensNames[~id] : showedNonTokensNames[id])).ToArray(),
                    e.TokenType.HasValue ? showedTokensNames[e.TokenType.Value] : null,
                    CreateOpponent(e.First),
                    CreateOpponent(e.Second));
            }
        }
    }
}
