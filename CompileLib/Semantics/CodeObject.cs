using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.Semantics
{
    public class CodeObject
    {
        public int Line { get; private set; }
        public int Column { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }

        private SortedDictionary<string, List<SearchResult>> byName = new();
        private SortedDictionary<string, List<CodeObject>> byRelation = new();
        private SortedDictionary<string, string?> attributes = new();

        public CodeObject(string name, string type, int line, int column)
        {
            if(name is null) throw new ArgumentNullException(nameof(name));
            if(type is null) throw new ArgumentNullException(nameof(type));

            Name = name;
            Type = type;
            Line = line;
            Column = column;
        }

        public void AddRelation(string relationName, CodeObject obj)
        {
            if (byName.ContainsKey(obj.Name))
                byName[obj.Name].Add(new(obj, relationName));
            else
                byName.Add(obj.Name, new List<SearchResult>(1) { new(obj, relationName) });

            if (byRelation.ContainsKey(relationName))
                byRelation[relationName].Add(obj);
            else
                byRelation.Add(relationName, new List<CodeObject>(1) { obj });
        }

        public void AddAttribute(string name, string? value = null)
        {
            attributes.Add(name, value);
        }

        public bool HasAttribute(string name)
        {
            return attributes.ContainsKey(name);
        }

        public string? GetAttribute(string name)
        {
            return attributes[name];
        }

        private void GetByPredicate(Func<SearchResult, bool> predicate, List<SearchResult> result)
        {
            result.AddRange(byName.Values.SelectMany(v => v).Where(predicate));
        }

        internal void GetByName(string name, List<SearchResult> result)
        {
            if(byName.ContainsKey(name))
            {
                result.AddRange(byName[name]);
            }
        }

        internal void GetByRelation(string relation, List<SearchResult> result)
        {
            if(byRelation.ContainsKey(relation))
            {
                result.AddRange(byRelation[relation].Select(r => new SearchResult(r, relation)));
            }
        }

        internal void GetByType(string type, List<SearchResult> result)
        {
            GetByPredicate(obj => obj.Result.Type == type, result);
        }

        internal void GetByAttribute(List<SearchResult> result, string attribute, string? value = null)
        {
            if(value is null)
            {
                GetByPredicate(obj => obj.Result.HasAttribute(attribute), result);
            }
            else
            {
                GetByPredicate(obj => obj.Result.GetAttribute(attribute) == value, result);
            }
        }
    }
}
