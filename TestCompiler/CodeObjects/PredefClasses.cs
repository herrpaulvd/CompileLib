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
        public static Class[] Make()
        {
            var noMembers = Array.Empty<ClassMember>();

            Class clVoid = new("void", noMembers, ELType.Void);
            Class clInt8 = new("int8", noMembers, ELType.Int8);
            Class clUInt8 = new("uint8", noMembers, ELType.UInt8);
            Class clInt16 = new("int16", noMembers, ELType.Int16);
            Class clUInt16 = new("uint16", noMembers, ELType.UInt16);
            Class clInt32 = new("int32", noMembers, ELType.Int32);
            Class clUInt32 = new("uint32", noMembers, ELType.UInt32);
            Class clInt64 = new("int64", noMembers, ELType.Int64);
            Class clUInt64 = new("uint64", noMembers, ELType.UInt64);

            // TODO: add I/O methods
            Class system = new("System", noMembers, ELType.PVoid);

            return new Class[]
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
                system
            };
        }
    }
}
