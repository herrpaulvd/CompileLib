using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.EmbeddedLanguage;

namespace TestCompiler.CodeObjects
{
    internal static class PredefClasses
    {
        public static ELFunction malloc;

        private static Method CreateSystemMethod(string name, TypeExpression typeExpression, params Parameter[] parameters)
            => new(name, -1, -1, MemberVisibility.Public, true, typeExpression, null, parameters);

        public static (ELCompiler, Class[]) Make()
        {
            var noMembers = Array.Empty<ClassMember>();

            Class clVoid = new("void", noMembers, ELType.Void, false);
            Class clInt8 = new("int8", noMembers, ELType.Int8, true);
            Class clUInt8 = new("uint8", noMembers, ELType.UInt8, false);
            Class clInt16 = new("int16", noMembers, ELType.Int16, true);
            Class clUInt16 = new("uint16", noMembers, ELType.UInt16, false);
            Class clChar = new("char", noMembers, ELType.UInt16, false);
            Class clInt32 = new("int32", noMembers, ELType.Int32, true);
            Class clUInt32 = new("uint32", noMembers, ELType.UInt32, false);
            Class clInt64 = new("int64", noMembers, ELType.Int64, true);
            Class clUInt64 = new("uint64", noMembers, ELType.UInt64, false);

            // TODO: add I/O methods
            ELCompiler compiler = new ELCompilerBuilder()
                .AddMemoryFunctions(out malloc, out ELFunction realloc, out ELFunction free, true, true)
                .AddMemcpy(out ELFunction memcpy)
                .AddMemmove(out ELFunction memmove)
                .AddConsoleFunctionsW(out ELFunction ConsoleReadW, out ELFunction ConsoleWriteW)
                .AddConsoleReadLineW(out ELFunction ConsoleReadLineW)
                .Create();

            TypeExpression PVOID = new(-1, -1, "void", 1);
            TypeExpression VOID = new(-1, -1, "void", 0);
            TypeExpression UINT64 = new(-1, -1, "uint64", 0);
            TypeExpression PUINT64 = new(-1, -1, "uint64", 1);
            TypeExpression PCHAR = new(-1, -1, "char", 1);

            Method methodMalloc = CreateSystemMethod(
                "malloc",
                PVOID,
                new Parameter("size", -1, -1, UINT64));
            Method methodRealloc = CreateSystemMethod(
                "realloc",
                PVOID,
                new Parameter("oldptr", -1, -1, PVOID),
                new Parameter("size", -1, -1, UINT64));
            Method methodFree = CreateSystemMethod(
                "free",
                VOID,
                new Parameter("ptr", -1, -1, PVOID));
            Method methodMemcpy = CreateSystemMethod(
                "memcpy",
                VOID,
                new Parameter("dst", -1, -1, PVOID),
                new Parameter("src", -1, -1, PVOID),
                new Parameter("size", -1, -1, UINT64));
            Method methodMemmove = CreateSystemMethod(
                "memmove",
                VOID,
                new Parameter("dst", -1, -1, PVOID),
                new Parameter("src", -1, -1, PVOID),
                new Parameter("size", -1, -1, UINT64));
            Method methodReadLine = CreateSystemMethod(
                "ReadLine",
                PCHAR,
                new Parameter("pCharsRead", -1, -1, PUINT64));
            Method methodWrite = CreateSystemMethod(
                "Write",
                VOID,
                new Parameter("output", -1, -1, PCHAR),
                new Parameter("count", -1, -1, UINT64));

            methodMalloc.Compiled = malloc;
            methodRealloc.Compiled = realloc;
            methodFree.Compiled = free;
            methodMemcpy.Compiled = memcpy;
            methodMemmove.Compiled = memmove;
            methodWrite.Compiled = ConsoleWriteW;
            methodReadLine.Compiled = ConsoleReadLineW;
            Method[] sysmembers =
            {
                methodMalloc,
                methodRealloc,
                methodFree,
                methodMemcpy,
                methodMemmove,
                methodWrite,
                methodReadLine
            };

            Class system = new("System", sysmembers, ELType.PVoid, false);


            return (compiler, new Class[]
            {
                clVoid,
                clInt8,
                clUInt8,
                clInt16,
                clUInt16,
                clInt32,
                clUInt32,
                clInt64,
                clUInt64,
                clChar,
                system
            });
        }
    }
}
