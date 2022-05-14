using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.Parsing;
using CompileLib.Semantics;
using CompileLib.EmbeddedLanguage;

using TestCompiler.CodeObjects;

namespace TestCompiler
{
    internal class Syntax
    {
        [SetTag("global")]
        public static CodeObject ReadGlobal(
            [Many(true)][RequireTags("namespace", "class")] CodeObject[] components
            )
        {
            CodeObject global = new("global", "global", 1, 1);
            foreach (var component in components)
            {
                component.AddRelation("parent", global);
                global.AddRelation("child", component);
            }

            return global;
        }

        [SetTag("namespace")]
        public static CodeObject ReadNamespace(
            [Keywords("namespace")] Token kw,
            [RequireTags("id")] string id,
            [Keywords("{")] string brOpen,
            [Many(true)][RequireTags("namespace", "class")] CodeObject[] components,
            [Keywords("}")] string brClose
            )
        {
            CodeObject nmspc = new(id, "namespace", kw.Line, kw.Column);
            foreach (var component in components)
            {
                component.AddRelation("parent", nmspc);
                nmspc.AddRelation("child", component);
            }

            return nmspc;
        }

        [SetTag("class")]
        public static CodeObject ReadClass(
            [Optional(false)][RequireTags("visibility-mod")] Token? visMod,
            [Keywords("class")] Token kw,
            [RequireTags("id")] string id,
            [Optional(false)][Keywords(":")] string inherited,
            [TogetherWith][RequireTags("expr")] CodeObject baseClass,
            [Keywords("{")] string brOpen,
            [Many(true)][RequireTags("class", "method", "field", "constructor")] CodeObject[] components,
            [Keywords("}")] string brClose
            )
        {
            var firstToken = visMod ?? kw;
            CodeObject clss = new(id, "class", firstToken.Line, firstToken.Column);
            if(visMod is not null)
                clss.AddAttribute(visMod.Self);
            if (baseClass is not null)
                clss.AddRelation("base-class", baseClass);
            foreach(var component in components)
            {
                component.AddRelation("parent", clss);
                clss.AddRelation("child", component);
            }
            return clss;
        }

        [SetTag("visibility-mod")]
        public static Token ReadVisibilityModifier(
            [Keywords("public", "private", "protected")] Token self
            )
        {
            return self;
        }

        [SetTag("field")]
        public static CodeObject ReadField(
            [Optional(false)][RequireTags("visibility-mod")] Token? visMod,
            [Optional(false)][Keywords("static")] Token? kwStatic,
            [RequireTags("expr")] CodeObject type,
            [RequireTags("id")] string id,
            [Optional(false)][Keywords("=")] string eq,
            [TogetherWith][RequireTags("expr")] CodeObject expr,
            [Keywords(";")] string close
            )
        {
            var firstToken = visMod ?? kwStatic;
            CodeObject field = new(id, "field", firstToken?.Line ?? type.Line, firstToken?.Column ?? type.Column);
            if(visMod is not null)
                field.AddAttribute(visMod.Self);
            if (kwStatic is not null)
                field.AddAttribute(kwStatic.Self);
            field.AddRelation("type-expr", type);
            if(expr is not null)
                field.AddRelation("init-expr", expr);
            return field;
        }

        [SetTag("method")]
        public static CodeObject ReadMethod(
            [Optional(false)][RequireTags("visibility-mod")] Token? visMod,
            [Optional(false)][Keywords("static")] Token? kwStatic,
            [RequireTags("expr")] CodeObject type,
            [RequireTags("id")] string id,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("method.params")] List<CodeObject> parameters,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] CodeObject statement
            )
        {
            var firstToken = visMod ?? kwStatic;
            parameters.Reverse();
            CodeObject method = new Method(id, "method", firstToken?.Line ?? type.Line, firstToken?.Column ?? type.Column, parameters.ToArray());
            if (visMod is not null)
                method.AddAttribute(visMod.Self);
            if (kwStatic is not null)
                method.AddAttribute(kwStatic.Self);
            method.AddRelation("type-expr", type);
            method.AddRelation("statement", statement);
            return method;
        }

        [SetTag("method.params")]
        public static List<CodeObject> ReadMethodParameters(
            [RequireTags("expr")] CodeObject type,
            [RequireTags("id")] string id,
            [Optional(false)][Keywords(",")] string separator,
            [TogetherWith][RequireTags("method.params")] List<CodeObject> tail
            )
        {
            CodeObject parameter = new(id, "parameter", type.Line, type.Column);
            parameter.AddRelation("type-expr", type);
            tail.Add(parameter);
            return tail;
        }

        [SetTag("constructor")]
        public static CodeObject ReadConstructor(
            [Optional(false)][RequireTags("visibility-mod")] Token? visMod,
            [Optional(false)][Keywords("static")] Token? kwStatic,
            [RequireTags("expr")] CodeObject type,
            [Keywords("(")] string brOpen,
            [Optional(false)][RequireTags("method.params")] List<CodeObject> parameters,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] CodeObject statement
            )
        {
            var firstToken = visMod ?? kwStatic;
            parameters.Reverse();
            CodeObject method = new Method("", "constructor", firstToken?.Line ?? type.Line, firstToken?.Column ?? type.Column, parameters.ToArray());
            if (visMod is not null)
                method.AddAttribute(visMod.Self);
            if (kwStatic is not null)
                method.AddAttribute(kwStatic.Self);
            method.AddRelation("type-expr", type);
            method.AddRelation("statement", statement);
            return method;
        }

        [SetTag("statement")]
        public static CodeObject ReadEmptyStatement(
            [Keywords(";")] Token token
            )
        {
            return new("", "expr-statement", token.Line, token.Column);
        }

        [SetTag("statement")]
        public static CodeObject ReadExprStatement(
            [RequireTags("expr")] CodeObject expr,
            [Keywords(";")] string close
            )
        {
            CodeObject result = new("", "expr-statement", expr.Line, expr.Column);
            result.AddRelation("expr", expr);
            return result;
        }

        [SetTag("statement")]
        public static CodeObject ReadVarInit(
            [RequireTags("expr")] CodeObject type,
            [RequireTags("id")] string id,
            [Keywords("=")] string eq,
            [RequireTags("expr")] CodeObject expr,
            [Keywords(";")] string close
            )
        {
            CodeObject variable = new(id, "local-var", type.Line, type.Column);
            variable.AddRelation("type-expr", type);

            CodeObject result = new("", "init-statement", type.Line, type.Column);
            result.AddRelation("expr", expr);
            result.AddRelation("var", variable);
            return result;
        }

        [SetTag("statement")]
        public static CodeObject ReadStatementBlock(
            [Keywords("{")] Token brOpen,
            [Many(true)][RequireTags("statement")] CodeObject[] statements,
            [Keywords("}")] string brClose
            )
        {
            return new BlockStatement("", "block-statement", brOpen.Line, brOpen.Column, statements);
        }

        [SetTag("statement")]
        public static CodeObject ReadIfStatement(
            [Keywords("if")] Token kw,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] CodeObject condition,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] CodeObject ifBranch,
            [Optional(true)][Keywords("else")] string kwElse,
            [TogetherWith][RequireTags("statement")] CodeObject elseBranch
            )
        {
            CodeObject result = new("", "if-statement", kw.Line, kw.Column);
            result.AddRelation("condition", condition);
            result.AddRelation("if-branch", ifBranch);
            if (elseBranch is not null)
                result.AddRelation("else-branch", elseBranch);
            return result;
        }

        [SetTag("statement")]
        public static CodeObject ReadWhileStatement(
            [Keywords("while")] Token kw,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] CodeObject condition,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] CodeObject body
            )
        {
            CodeObject result = new("", "while-statement", kw.Line, kw.Column);
            result.AddRelation("condition", condition);
            result.AddRelation("body", body);
            return result;
        }

        [SetTag("statement")]
        public static CodeObject ReadDoWhileStatement(
            [Keywords("do")] Token kw,
            [RequireTags("statement")] CodeObject body,
            [Keywords("while")] string kwWhile,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] CodeObject condition,
            [Keywords(")")] string brClose,
            [Keywords(";")] string close
            )
        {
            CodeObject result = new("", "do-while-statement", kw.Line, kw.Column);
            result.AddRelation("condition", condition);
            result.AddRelation("body", body);
            return result;
        }

        [SetTag("statement")]
        public static CodeObject ReadForStatement(
            [Keywords("for")] Token kw,
            [Keywords("(")] string brOpen,
            [RequireTags("expr")] CodeObject init,
            [Keywords(";")] string sep1,
            [RequireTags("expr")] CodeObject cond,
            [Keywords(";")] string sep2,
            [RequireTags("expr")] CodeObject next,
            [Keywords(")")] string brClose,
            [RequireTags("statement")] CodeObject body
            )
        {
            CodeObject result = new("", "for-statement", kw.Line, kw.Column);
            result.AddRelation("init", init);
            result.AddRelation("cond", cond);
            result.AddRelation("next", next);
            result.AddRelation("body", body);
            return result;
        }

        // TODO: expr
    }
}
