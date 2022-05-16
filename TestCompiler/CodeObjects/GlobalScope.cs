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
            foreach(var component in PredefClasses.Make())
                name2class.Add(component.Name, component);

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
                bool instanceCtor = false;
                bool staticCtor = false;

                foreach (var member in component.Members)
                {
                    if (member.Type == "constructor")
                    {
                        if(member.HasAttribute("static"))
                        {
                            if (staticCtor) throw new CompilationError($"Type {component.Name}: only one static constructor is allowed");
                            staticCtor = true;
                        }
                        else
                        {
                            if (instanceCtor) throw new CompilationError($"Type {component.Name}: only one instance constructor is allowed");
                            instanceCtor = true;
                        }
                        continue;
                    }
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

            SortedSet<string> elResolved = new();
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
                    if(class2base[name] is string baseclass)
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
                            f.StrucFieldIndex = elFields.Count;
                            if(f.TypeExpression.IsVoid())
                                throw new CompilationError($"Field {component.Name}.{f.Name}: invalid type");
                            var fieldClassName = f.TypeExpression.ClassName;
                            if (!name2class.ContainsKey(fieldClassName))
                                throw new CompilationError($"Field {component.Name}.{f.Name}: invalid type");
                            var elFieldType = name2class[fieldClassName].TargetType;
                            for (int i = 0; i < f.TypeExpression.PointerDepth; i++)
                                elFieldType = elFieldType.MakePointer();
                            elFields.Add(elFieldType);
                        }
                        else if (member is Method m)
                        {
                            string fullName;
                            if(m.IsConstructor)
                            {
                                fullName = $"Constructor {component.Name}.constructor";
                                if (m.TypeExpression.ClassName != component.Name)
                                    throw new CompilationError($"Class {component.Name}: invalid constructor declaration");
                            }
                            else
                            {
                                fullName = $"Method {component.Name}.{m.Name}";
                                var resClassName = m.TypeExpression.ClassName;
                                if (!name2class.ContainsKey(resClassName))
                                    throw new CompilationError($"{fullName}: invalid type");
                            }

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
                    clss.TargetType = new ELStructType(1, elFields.ToArray());
                }
            }

            // TODO: раздать методам ELFunction
            // + начать их компилить
            // TODO: предусмотреть класс как parent для scope
            // засунуть инициализацию полей (в отдельных scope) в конструктор
            // скомпилить собственно методы
            // TODO: может быть раздать прям типы TypeExpression'ам???
            // с указанием куда кастовать ???

            //foreach(var component in Components)
            //    component.ResolveTypes(network);
        }
    }
}
