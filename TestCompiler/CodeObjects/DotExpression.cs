using CompileLib.EmbeddedLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;

namespace TestCompiler.CodeObjects
{
    internal class DotExpression : Expression, IObjectSearcher
    {
        public Expression Left { get; private set; }
        public string Right { get; private set; }
        public string Operation { get; private set; }

        private TypeExpression type;
        public override TypeExpression Type => type;

        public DotExpression(
            Expression left,
            string right,
            string operation,
            int line,
            int column)
            : base(line, column)
        {
            Left = left;
            Right = right;
            Operation = operation;
        }

        private List<SearchResult> DotFind(CompilationParameters compilation, Class myclass, TypeExpression type, string isstatic)
        {
            if (type.PointerDepth > 0)
                throw new CompilationError("Cannot get member of pointer type or instance", Line, Column);
            var currclass = compilation.Name2Class[type.ClassName];

            List<SearchResult> searchRes;
            if (currclass == myclass)
            {
                searchRes = compilation.SemanticNetwork.Search(myclass, "@private-dot-search", Right, isstatic);
            }
            else
            {
                if (myclass.Inherits(currclass))
                {
                    searchRes = compilation.SemanticNetwork.Search(currclass, "@protected-dot-search", Right, isstatic);
                }
                else if (currclass.Inherits(myclass))
                {
                    searchRes = compilation.SemanticNetwork.Search(currclass, "@public-dot-search", Right, isstatic);
                    if (searchRes.Count == 0)
                        searchRes = compilation.SemanticNetwork.Search(myclass, "@private-dot-search", Right, isstatic);
                }
                else
                {
                    var myBasis = compilation.SemanticNetwork.Search(myclass, "@baseclasses-search");
                    var currBasis = compilation.SemanticNetwork.Search(currclass, "@baseclasses-search");

                    myBasis.Reverse();
                    currBasis.Reverse();

                    int index = 0;
                    while (index < myBasis.Count && index < currBasis.Count && myBasis[index].Result == currBasis[index].Result)
                        index++;

                    if (index == 0)
                    {
                        searchRes = compilation.SemanticNetwork.Search(currclass, "@public-dot-search", Right, isstatic);
                    }
                    else
                    {
                        searchRes = compilation.SemanticNetwork.Search(currclass, "@public-dot-search", Right, isstatic);
                        if (searchRes.Count == 0)
                            searchRes = compilation.SemanticNetwork.Search(myBasis[index - 1].Result, "@protected-dot-search", Right, isstatic);
                    }
                }
            }
            return searchRes;
        }

        public object FindObject(CompilationParameters compilation)
        {
            var myclass = compilation.SemanticNetwork.Search(compilation.Scope, "@find-parent-class")[0].Result as Class;

            ELExpression expr;
            if(Left is IObjectSearcher searcher)
            {
                var found = searcher.FindObject(compilation);
                if (found is TypeExpression type)
                {
                    var searchRes = DotFind(compilation, myclass, type, "static");
                    if (searchRes.Count == 0)
                        throw new CompilationError($"Cannot find member {Right}", Line, Column);
                    var res = searchRes[0].Result;
                    if (res is Field f)
                    {
                        type = f.TypeExpression;
                        return f.GlobalVar;
                    }
                    if (res is Method m)
                    {
                        return new ThisClosure(null, m);
                    }
                    throw new NotImplementedException();
                }
                else if (found is ThisClosure closure)
                {
                    throw new CompilationError("Cannot get method member", Line, Column);
                }
                else if (found is ELExpression e)
                {
                    expr = e;
                }
                else throw new NotImplementedException();
            }
            else
            {
                expr = Left.CompileRight(compilation);
            }

            // when expr
            {
                var ltype = Left.Type;
                var searchRes = DotFind(compilation, myclass, ltype, "anymember");
                if (searchRes.Count == 0)
                    throw new CompilationError($"Cannot find member {Right}", Line, Column);
                var res = searchRes[0].Result;
                if (res is Field f)
                {
                    type = f.TypeExpression;
                    if (f.IsStatic)
                        return f.GlobalVar;
                    else
                    {
                        var castType = ltype.GetCastType(compilation.Name2Class);
                        return expr.Cast(castType).PtrToRef().FieldRef(f.StrucFieldIndex);
                    }
                }
                if (res is Method m)
                {
                    if (m.IsStatic)
                        return new ThisClosure(null, m);
                    return new ThisClosure(expr, m);
                }
                throw new NotImplementedException();
            }
        }

        public override ELExpression CompileRight(CompilationParameters compilation)
        {
            var result = FindObject(compilation);
            if (result is ELMemoryCell cell)
                return cell;
            throw new CompilationError("Cannot use method like field", Line, Column);
        }

        public override ELMemoryCell CompileLeft(CompilationParameters compilation)
        {
            var result = FindObject(compilation);
            if (result is ELMemoryCell cell)
                return cell;
            throw new CompilationError("Cannot use method like field", Line, Column);
        }
    }
}
