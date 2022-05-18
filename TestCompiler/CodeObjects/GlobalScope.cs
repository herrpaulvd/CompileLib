using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Semantics;
using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal class GlobalScope : CodeObject
    {
        public Class[] Components { get; private set; }

        public GlobalScope(Class[] components) 
            : base("global", "global", 1, 1)
        {
            Components = components;
            foreach (var component in components)
            {
                component.AddRelation("parent", this);
                AddRelation("child", component);
            }
        }

        public void Compile(string filename)
        {
            SemanticNetwork network = new(Resources.searchrules);

            // check names
            SortedDictionary<string, Class> name2class = new();
            var (compiler, predefs) = PredefClasses.Make();
            foreach(var component in predefs)
            {
                name2class.Add(component.Name, component);
                component.AddRelation("parent", this);
                AddRelation("child", component);
            }

            foreach (var component in Components)
            {
                if (name2class.ContainsKey(component.Name))
                    throw new CompilationError($"Duplicate class name {component.Name}");
                name2class.Add(component.Name, component);
            }

            SortedDictionary<string, ClassMember> classMember2name = new();
            foreach (var component in Components)
            {
                classMember2name.Clear();

                foreach (var member in component.Members)
                {
                    if (classMember2name.ContainsKey(member.Name))
                        throw new CompilationError($"Duplicate member name {member.Name} in class {component.Name}");
                    classMember2name.Add(member.Name, member);
                }
            }

            SortedDictionary<string, string> class2base = new();
            // resolve base types
            foreach(var component in Components)
            {
                if(component.BaseClassName is not null)
                {
                    if (!name2class.ContainsKey(component.BaseClassName))
                        throw new CompilationError($"Class {component.BaseClassName} from which inherits {component.Name} is not found");
                    if (name2class[component.BaseClassName].IsPredefined)
                        throw new CompilationError($"Class {component.BaseClassName} inherits from a predefined class, it is forbidden");

                    List<string> baseChain = new() { component.BaseClassName };
                    while(class2base.ContainsKey(baseChain[^1]))
                    {
                        baseChain.Add(class2base[baseChain[^1]]);
                    }
                    if (baseChain.Contains(component.Name))
                        throw new CompilationError($"Class {component.Name} inherits itself recursively");
                    class2base.Add(component.Name, component.BaseClassName);
                }
            }

            foreach (var kv in class2base)
                name2class[kv.Key].AddRelation("base-class", name2class[kv.Value]);

            SortedSet<string> elResolved = new();
            List<Method> methods = new();
            foreach(var component in Components)
            {
                if (elResolved.Contains(component.Name)) continue;
                List<string> resolvingOrder = new() { component.Name };
                while (class2base.ContainsKey(resolvingOrder[^1]) && !elResolved.Contains(resolvingOrder[^1]))
                {
                    resolvingOrder.Add(class2base[resolvingOrder[^1]]);
                }
                resolvingOrder.Reverse();
                elResolved.UnionWith(resolvingOrder);
                foreach(var name in resolvingOrder)
                {
                    var clss = name2class[name];
                    List<ELType> elFields = new();
                    if(class2base.ContainsKey(name) && class2base[name] is string baseclass)
                    {
                        var baseStruc = name2class[baseclass].StrucType;
                        int n = baseStruc.FieldCount;
                        for(int i = 0; i < n; i++)
                            elFields.Add(baseStruc.GetFieldType(i));
                    }
                    // and type checking + method inspection
                    foreach(var member in clss.Members)
                    {
                        if (member is Field f)
                        {
                            if(f.HasAttribute("static"))
                            {
                                if (f.TypeExpression.IsVoid())
                                    throw new CompilationError($"Field {component.Name}.{f.Name}: invalid type");
                                var fieldClassName = f.TypeExpression.ClassName;
                                if (!name2class.ContainsKey(fieldClassName))
                                    throw new CompilationError($"Field {component.Name}.{f.Name}: invalid type");
                                var elFieldType = f.TypeExpression.GetResolvedType(name2class);
                                f.GlobalVar = compiler.AddGlobalVariable(elFieldType);
                            }
                            else
                            {
                                f.StrucFieldIndex = elFields.Count;
                                if (f.TypeExpression.IsVoid())
                                    throw new CompilationError($"Field {component.Name}.{f.Name}: invalid type");
                                var fieldClassName = f.TypeExpression.ClassName;
                                if (!name2class.ContainsKey(fieldClassName))
                                    throw new CompilationError($"Field {component.Name}.{f.Name}: invalid type");
                                var elFieldType = f.TypeExpression.GetResolvedType(name2class);
                                elFields.Add(elFieldType);
                            }
                        }
                        else if (member is Method m)
                        {
                            methods.Add(m);
                            string fullName = $"Method {component.Name}.{m.Name}";
                            var resClassName = m.TypeExpression.ClassName;
                            if (!name2class.ContainsKey(resClassName))
                                throw new CompilationError($"{fullName}: invalid type");

                            SortedSet<string> paramNames = new();
                            foreach(var p in m.Parameters)
                            {
                                if(paramNames.Contains(p.Name))
                                    throw new CompilationError($"Parameter {p.Name} in {fullName}: duplicate name");
                                paramNames.Add(p.Name);
                                if(p.TypeExpression.IsVoid())
                                    throw new CompilationError($"Parameter {p.Name} in {fullName}: invalid type");
                                var paramClassName = p.TypeExpression.ClassName;
                                if (!name2class.ContainsKey(paramClassName))
                                    throw new CompilationError($"Parameter {p.Name} in {fullName}: invalid type");
                            }
                        }
                    }
                    clss.StrucType = new ELStructType(1, elFields.ToArray());
                }
            }

            foreach(var m in methods)
            {
                var ret = m.TypeExpression.GetResolvedType(name2class);
                List<ELType> ptypes = new();
                List<Parameter> allparams = new();
                if (m.HasAttribute("instance"))
                {
                    var clss = (m.GetOneRelated("parent") as Class);
                    ptypes.Add(clss.TargetType);
                    Parameter pthis = new("this", -1, -1, new TypeExpression(-1, -1, clss.Name, 0));
                    allparams.Add(pthis);
                    m.AddRelation("parameter", pthis);
                }
                for (int i = 0; i < m.Parameters.Length; i++)
                {
                    ptypes.Add(m.Parameters[i].TypeExpression.GetResolvedType(name2class));
                    allparams.Add(m.Parameters[i]);
                }
                m.Compiled = compiler.CreateFunction(ret, ptypes.ToArray());
                for(int i = 0; i < allparams.Count; i++)
                    allparams[i].Variable = m.Compiled.GetParameter(i);
            }

            CompilationParameters compilation = new(network, compiler, this, name2class);

            foreach(var m in methods)
            {
                CodeObject scope = new("", "scope", m.Line, m.Column);
                scope.AddRelation("parent", m);
                m.Compiled.Open();
                m.MainStatement.Compile(compilation.WithScope(scope));
            }

            compiler.OpenEntryPoint();
            List<Method> mains = new();
            foreach(var m in methods)
            {
                if (m.Name == "Main" && m.Visibility == MemberVisibility.Public && m.IsStatic && m.Parameters.Length == 0)
                    mains.Add(m);
            }

            if (mains.Count == 0)
                throw new CompilationError("No entry point is found");
            if (mains.Count > 1)
                throw new CompilationError("More than one entry point are found");
            mains[0].Compiled.Call();
            compiler.BuildAndSave(filename);
        }
    }
}
