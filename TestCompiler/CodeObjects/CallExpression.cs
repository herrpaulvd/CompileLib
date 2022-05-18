using CompileLib.EmbeddedLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class CallExpression : Expression, IObjectSearcher
    {
        public Expression Callee { get; private set; }
        public Expression[] Args { get; private set; }
        public string BracketType { get; private set; }

        private TypeExpression type;
        public override TypeExpression Type => type;

        public CallExpression(
            Expression callee,
            Expression[] args,
            string bracketType,
            int line,
            int column)
            : base(line, column)
        {
            Callee = callee;
            Args = args;
            BracketType = bracketType;
        }

        public object FindObject(CompilationParameters compilation)
        {
            if(BracketType != "[") return CompileRight(compilation);
            ELExpression expr;
            if(Callee is IObjectSearcher searcher)
            {
                var found = searcher.FindObject(compilation);
                if (found is TypeExpression type)
                {
                    if (Args.Length != 0)
                        throw new CompilationError("Cannot index type", Line, Column);
                    return new TypeExpression(type.Line, type.Column, type.ClassName, type.PointerDepth + 1);
                }
                else if (found is ThisClosure closure)
                {
                    throw new CompilationError("Cannot index method", Line, Column);
                }
                else if (found is ELExpression e)
                {
                    expr = e;
                }
                else throw new NotImplementedException();
            }
            else
            {
                expr = Callee.CompileRight(compilation);
            }

            // when expr
            {
                var ltype = Callee.Type;
                if (ltype.PointerDepth == 0)
                    throw new CompilationError("Cannot index non-pointer values", Line, Column);
                if (Args.Length != 1)
                    throw new CompilationError("Invalid number of parameters when indexing", Line, Column);
                type = new TypeExpression(-1, -1, ltype.ClassName, ltype.PointerDepth - 1);
                var index = Args[0].CompileRight(compilation);
                if (!Args[0].Type.IsIntegerType(compilation.Name2Class))
                    throw new CompilationError("Cannot index value with non-integer argument", Line, Column);
                return expr[index];
            }
        }

        public override ELExpression CompileRight(CompilationParameters compilation)
        {
            if(BracketType == "[")
            {
                if (FindObject(compilation) is ELMemoryCell expr)
                    return expr;
                throw new CompilationError("Invalid code object to be indexed", Line, Column);
            }

            if (Callee is IObjectSearcher searcher)
            {
                var found = searcher.FindObject(compilation);
                if(found is ThisClosure closure)
                {
                    if (Args.Length != closure.Method.Parameters.Length)
                        throw new CompilationError("Invalid number of arguments");

                    List<ELExpression> evaluatedArgs = new();
                    if (closure.This is not null)
                        evaluatedArgs.Add(closure.This);
                    foreach (var a in Args)
                        evaluatedArgs.Add(a.CompileRight(compilation));

                    for (int i = 0; i < Args.Length; i++)
                        if (!Args[i].Type.IsAssignableTo(closure.Method.Parameters[i].TypeExpression, compilation.Name2Class))
                            throw new CompilationError($"Argument #{i + 1} has invalid type", Line, Column);

                    type = closure.Method.TypeExpression;
                    return closure.Method.Compiled.Call(evaluatedArgs.ToArray());
                }
                if(found is TypeExpression typeexpr)
                {
                    if (typeexpr.IsVoid())
                        throw new CompilationError("Cannot cast to void type", Line, Column);

                    if (Args.Length != 1)
                        throw new CompilationError("Invalid number of args", Line, Column);
                    var expr = Args[0].CompileRight(compilation);
                    var exprt = Args[0].Type;
                    if (exprt.IsVoid())
                        throw new CompilationError("Cannot cast void-expression");

                    var target = typeexpr.GetResolvedType(compilation.Name2Class);
                    type = typeexpr;
                    return expr.Cast(target);
                }
            }
            throw new CompilationError("Cannot invoke the expression", Line, Column);
        }

        public override ELMemoryCell CompileLeft(CompilationParameters compilation)
        {
            if (BracketType == "[")
            {
                if (FindObject(compilation) is ELMemoryCell expr)
                    return expr;
                throw new CompilationError("Invalid code object to be indexed", Line, Column);
            }
            throw new CompilationError("Cannot assign right to left", Line, Column);
        }
    }
}
