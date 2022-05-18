using CompileLib.EmbeddedLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCompiler.CodeObjects
{
    internal class IdExpression : Expression, IObjectSearcher
    {
        public string ID { get; private set; }

        private TypeExpression type;
        public override TypeExpression Type => type;

        public IdExpression(string id, int line, int column) : base(line, column)
        {
            ID = id;
        }

        public object FindObject(CompilationParameters compilation)
        {
            var found = compilation.SemanticNetwork.Search(compilation.Scope, "@local-search", ID);
            if (found.Count == 0)
                throw new CompilationError($"Unknown id {ID}", Line, Column);

            var result = found[0].Result;
            if (result is LocalVariable v)
            {
                type = v.TypeExpression;
                return v.Variable;
            }
            else if (result is Parameter p)
            {
                type = p.TypeExpression;
                return p.Variable;
            }
            else if (result is Field f)
            {
                type = f.TypeExpression;
                if (f.IsStatic)
                    return f.GlobalVar;

                found = compilation.SemanticNetwork.Search(compilation.Scope, "@local-search", "this");
                var pthis = found[0].Result as Parameter;
                var castType = pthis.TypeExpression.GetCastType(compilation.Name2Class);
                return pthis.Variable.Cast(castType).PtrToRef().FieldRef(f.StrucFieldIndex);
            }
            else if (result is Method m)
            {
                if (m.IsStatic)
                    return new ThisClosure(null, m);
                found = compilation.SemanticNetwork.Search(compilation.Scope, "@local-search", "this");
                var pthis = found[0].Result as Parameter;
                return new ThisClosure(pthis.Variable, m);
            }
            else if (result is Class c)
            {
                return new TypeExpression(-1, -1, c.Name, 0);
            }
            else throw new NotImplementedException();
        }

        private ELMemoryCell FindCell(CompilationParameters compilation)
        {
            if(FindObject(compilation) is ELMemoryCell cell)
                return cell;
            else
                throw new CompilationError("This code element cannot be used as independent expression", Line, Column);
        }

        public override ELExpression CompileRight(CompilationParameters compilation)
        {
            if(ID == "new")
            {
                var clss = compilation.SemanticNetwork.Search(compilation.Scope, "@find-parent-class")[0].Result as Class;
                type = new TypeExpression(-1, -1, clss.Name, 0);
                return PredefClasses.malloc.Call(compilation.Compiler.MakeConst((ulong)clss.StrucType.Size));
            }
            return FindCell(compilation);
        }

        public override ELMemoryCell CompileLeft(CompilationParameters compilation)
        {
            if (ID == "new") throw new CompilationError("Operator new cannot be used this way", Line, Column);
            return FindCell(compilation);
        }
    }
}
