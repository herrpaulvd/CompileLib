using CompileLib.EmbeddedLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class UnaryExpression : Expression
    {
        public Expression Operand { get; private set; }
        public string Operation { get; private set; }

        private TypeExpression type;
        public override TypeExpression Type => type;

        public UnaryExpression(
            Expression operand,
            string operation,
            int line,
            int column)
            : base(line, column)
        {
            Operand = operand;
            Operation = operation;
        }

        public override ELExpression CompileRight(CompilationParameters compilation)
        {
            if(Operation == "&")
            {
                var cell = Operand.CompileLeft(compilation);
                var ctype = Operand.Type;
                type = new TypeExpression(-1, -1, ctype.ClassName, ctype.PointerDepth + 1);
                return cell.Address;
            }

            var expr = Operand.CompileRight(compilation);
            var otype = Operand.Type;
            var name2class = compilation.Name2Class;

            switch(Operation)
            {
                case "!":
                    if (!otype.IsIntegerType(name2class))
                        throw new CompilationError("Integer type expected", Operand.Line, Operand.Column);
                    type = otype;
                    return !expr;
                case "~":
                    if (!otype.IsIntegerType(name2class))
                        throw new CompilationError("Integer type expected", Operand.Line, Operand.Column);
                    type = otype;
                    return ~expr;
                case "-":
                    if (!otype.IsIntegerType(name2class))
                        throw new CompilationError("Integer type expected", Operand.Line, Operand.Column);
                    type = otype;
                    return -expr;
                case "+":
                    if (!otype.IsIntegerType(name2class))
                        throw new CompilationError("Integer type expected", Operand.Line, Operand.Column);
                    type = otype;
                    return expr;
                case "*":
                    if (otype.PointerDepth == 0)
                        throw new CompilationError("Cannot dereference non-pointer", Operand.Line, Operand.Column);
                    type = new TypeExpression(-1, -1, otype.ClassName, otype.PointerDepth - 1);
                    return expr.PtrToRef();
                default:
                    throw new NotImplementedException();
            }
        }

        public override ELMemoryCell CompileLeft(CompilationParameters compilation)
        {
            if (Operation == "*")
            {
                var expr = Operand.CompileRight(compilation);
                var otype = Operand.Type;
                if (otype.PointerDepth == 0)
                    throw new CompilationError("Cannot dereference non-pointer", Operand.Line, Operand.Column);
                type = new TypeExpression(-1, -1, otype.ClassName, otype.PointerDepth - 1);
                return expr.PtrToRef();
            }
            else throw new CompilationError("Cannot assign right to left", Line, Column);
        }
    }
}
