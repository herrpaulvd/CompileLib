using CompileLib.EmbeddedLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class BinaryExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }
        public string Operation { get; private set; }

        private TypeExpression type;
        public override TypeExpression Type => type;

        public BinaryExpression(
            Expression left, 
            Expression right, 
            string operation, 
            int line, 
            int column) 
            : base(line, column)
        {
            Left = left;
            Right = right;
            Operation = operation;
        }

        public override ELExpression CompileRight(CompilationParameters compilation)
        {
            var name2class = compilation.Name2Class;
            if (Operation == "=")
            {
                var lexpr = Left.CompileLeft(compilation);
                var ltype = Left.Type;
                var rexpr = Right.CompileRight(compilation);
                var rtype = Right.Type;

                if (!rtype.IsAssignableTo(ltype, name2class))
                    throw new CompilationError("Incompatible types when assignation", Line, Column);
                type = ltype;
                lexpr.Value = rexpr;
                return lexpr;
            }
            else if(Operation == "&&" || Operation == "||")
            {
                ELVariable local = compilation.Compiler.AddLocalVariable(ELType.UInt64);
                var label = compilation.Compiler.DefineLabel();
                var lexpr = Left.CompileRight(compilation);
                var ltype = Left.Type;
                type = new TypeExpression(-1, -1, "uint64", 0);
                if(!ltype.IsIntegerType(name2class))
                    throw new CompilationError("Integer type is required", Left.Line, Left.Column);
                local.Value = lexpr.Cast(ELType.UInt64);
                compilation.Compiler.GotoIf(Operation == "&&" ? !local : local, label);
                var rexpr = Right.CompileRight(compilation);
                var rtype = Right.Type;
                if (!rtype.IsIntegerType(name2class))
                    throw new CompilationError("Integer type is required", Right.Line, Right.Column);
                local.Value = rexpr.Cast(ELType.UInt64);
                compilation.Compiler.MarkLabel(label);
                return local;
            }
            else if(Operation == "==" || Operation == "!=")
            {
                var lexpr = Left.CompileRight(compilation);
                var ltype = Left.Type;
                var rexpr = Right.CompileRight(compilation);
                var rtype = Right.Type;
                var tl = name2class[ltype.ClassName];
                type = new TypeExpression(-1, -1, tl.IsSigned ? "int64" : "uint64", 0);

                if(ltype.IsAssignableTo(rtype, name2class) || rtype.IsAssignableTo(ltype, name2class))
                {
                    if (Operation == "==")
                        return (lexpr == rexpr).Cast(ELType.UInt64);
                    return (lexpr != rexpr).Cast(ELType.UInt64);
                }

                throw new CompilationError("Cannot compare two values", Line, Column);
            }
            else if(Operation == "&" || Operation == "|" || Operation == "^")
            {
                var lexpr = Left.CompileRight(compilation);
                var ltype = Left.Type;
                var rexpr = Right.CompileRight(compilation);
                var rtype = Right.Type;
                type = new TypeExpression(-1, -1, "uint64", 0);

                if(!ltype.IsIntegerType(name2class))
                    throw new CompilationError("Integer type is required", Left.Line, Left.Column);
                if(!rtype.IsIntegerType(name2class))
                    throw new CompilationError("Integer type is required", Right.Line, Right.Column);
                switch(Operation)
                {
                    case "&":
                        return lexpr & rexpr;
                    case "|":
                        return lexpr | rexpr;
                    case "^":
                        return lexpr ^ rexpr;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if(Operation == "<<" || Operation == ">>")
            {
                var lexpr = Left.CompileRight(compilation);
                var ltype = Left.Type;
                var rexpr = Right.CompileRight(compilation);
                var rtype = Right.Type;
                var tl = name2class[ltype.ClassName];
                type = new TypeExpression(-1, -1, tl.IsSigned ? "int64" : "uint64", 0);

                if (!ltype.IsIntegerType(name2class))
                    throw new CompilationError("Integer type is required", Left.Line, Left.Column);
                if (!rtype.IsIntegerType(name2class))
                    throw new CompilationError("Integer type is required", Right.Line, Right.Column);
                switch (Operation)
                {
                    case "<<":
                        return lexpr.ShiftLeft(rexpr);
                    case ">>":
                        return lexpr.ShiftRight(rexpr);
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                var lexpr = Left.CompileRight(compilation);
                var ltype = Left.Type;
                var rexpr = Right.CompileRight(compilation);
                var rtype = Right.Type;
                var tl = name2class[ltype.ClassName];
                type = new TypeExpression(-1, -1, tl.IsSigned ? "int64" : "uint64", 0);

                if (ltype.PointerDepth > 0)
                {
                    if (!rtype.IsIntegerType(name2class))
                        throw new CompilationError("Integer type is required", Right.Line, Right.Column);
                    type = ltype;

                    switch(Operation)
                    {
                        case "+":
                            return lexpr + rexpr;
                        case "-":
                            return lexpr - rexpr;
                        default:
                            throw new CompilationError("Invalid parameters or operation", Line, Column);
                    }
                }

                if(ltype.IsIntegerType(name2class))
                {
                    if (!rtype.IsIntegerType(name2class))
                        throw new CompilationError("Integer type is required", Right.Line, Right.Column);

                    var tr = name2class[rtype.ClassName];

                    if (tl.IsSigned != tr.IsSigned)
                        throw new CompilationError("Both operand must be either both signed or both unsigned", Line, Column);

                    switch(Operation)
                    {
                        case "+":
                            return lexpr + rexpr;
                        case "-":
                            return lexpr - rexpr;
                        case "/":
                            return lexpr / rexpr;
                        case "%":
                            return lexpr % rexpr;
                        case "*":
                            return lexpr * rexpr;
                        case "<":
                            return lexpr < rexpr;
                        case ">":
                            return lexpr > rexpr;
                        case "<=":
                            return lexpr <= rexpr;
                        case ">=":
                            return lexpr >= rexpr;
                    }
                }

                throw new CompilationError("Integer or pointer type is required", Left.Line, Left.Column);
            }
        }

        public override ELMemoryCell CompileLeft(CompilationParameters compilation)
        {
            throw new CompilationError("Cannot assign right to left", Line, Column);
        }
    }
}
