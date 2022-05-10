using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.PEGen
{
    internal struct SectionLabel
    {
        public int Offset;

        public SectionLabel(int offset)
        {
            Offset = offset;
        }
    }

    internal struct LableTableRecord
    {
        public int Where;
        public SectionLabel What;
        public bool IsRelative;
        public int BytesToWrite; // 1/2/4/8

        public LableTableRecord(int where, SectionLabel what, bool isRelative, int bytesToWrite)
        {
            Where = where;
            What = what;
            IsRelative = isRelative;
            BytesToWrite = bytesToWrite;
        }

        public LableTableRecord(int where, int what, bool isRelative, int bytesToWrite)
            : this(where, new SectionLabel(what), isRelative, bytesToWrite) { }
    }

    internal struct ImportSectionLabel
    {
        public string DllName;
        public string FunctionName;

        public ImportSectionLabel(string dllName, string functionName)
        {
            DllName = dllName;
            FunctionName = functionName;
        }
    }

    internal struct ImportLableTableRecord
    {
        public int Where;
        public ImportSectionLabel What;
        public bool IsRelative;
        public int BytesToWrite; // 1/2/4/8

        public ImportLableTableRecord(int where, ImportSectionLabel what, bool isRelative, int bytesToWrite)
        {
            Where = where;
            What = what;
            IsRelative = isRelative;
            BytesToWrite = bytesToWrite;
        }
    }

    internal class PEBuilder
    {
        unsafe private static FileWriter CreateImage(
            IEnumerable<byte> data,
            IEnumerable<byte> code,
            ImportLableTableRecord[] importLabelTable,
            LableTableRecord[] dataLableTable,
            LableTableRecord[] codeLableTable
            )
        {
            const int fileAlign = 512;
            const int virtualAlign = 4096;
            const ulong imageBase = 0x00400000;

            // all headers
            FileWriter fw = new(fileAlign, virtualAlign);
            var headerSection = fw.AllocSection();
            headerSection.ReserveStruct(sizeof(IMAGE_DOS_HEADER));
            headerSection.ReserveStruct(sizeof(IMAGE_NT_HEADERS));
            headerSection.ReserveStruct(sizeof(IMAGE_SECTION_HEADER), 3); // .idata .data .text

            // constructing .idata
            SortedDictionary<string, SortedDictionary<string, ulong>> funcToAddress = new();
            foreach(var record in importLabelTable)
            {
                var dll = record.What.DllName;
                var func = record.What.FunctionName;

                if (!funcToAddress.ContainsKey(dll))
                    funcToAddress.Add(dll, new());
                var dllDict = funcToAddress[dll];
                if (!dllDict.ContainsKey(func))
                    dllDict.Add(func, 0);
            }

            var idataRAW = fw.FileSize;
            var idataRVA = fw.VirtualSize;
            var idataSection = fw.AllocSection();

            var dlls = funcToAddress.Keys.ToArray();
            var importDescriptors = new IMAGE_IMPORT_DESCRIPTOR[dlls.Length + 1];
            idataSection.ReserveStruct(sizeof(IMAGE_IMPORT_DESCRIPTOR), importDescriptors.Length);

            for(int i = 0; i < dlls.Length; i++)
            {
                int secptr = idataSection.GetPointer();
                importDescriptors[i].Name = (uint)(idataRVA + secptr);
                idataSection.WriteString(dlls[i]);

                var dllDict = funcToAddress[dlls[i]];
                var funcs = dllDict.Keys.ToArray();
                
                var thunks = new ulong[funcs.Length + 1];
                int originalThunkOffset = idataSection.GetPointer();
                idataSection.ReserveStruct(sizeof(ulong), thunks.Length);
                importDescriptors[i].OriginalFirstThunk = (uint)(idataRVA + originalThunkOffset);
                secptr = idataSection.GetPointer();
                idataSection.ReserveStruct(sizeof(ulong), thunks.Length);
                importDescriptors[i].FirstThunk = (uint)(idataRVA + secptr);

                for(int j = 0; j < funcs.Length; j++)
                {
                    secptr = idataSection.GetPointer();
                    thunks[j] = (ulong)idataRVA + (ulong)secptr;
                    idataSection.ReserveStruct(2);
                    idataSection.WriteString(funcs[j]);
                }

                secptr = idataSection.GetPointer();
                idataSection.SetPointer(originalThunkOffset);
                for(int q = 0; q < 2; q++)
                    foreach (var t in thunks)
                        idataSection.WriteStruct(&t, sizeof(ulong));
                idataSection.SetPointer(secptr);

                for (int j = 0; j < funcs.Length; j++)
                    dllDict[funcs[j]] = importDescriptors[i].FirstThunk + (ulong)j;
            }

            idataSection.SetPointer(0);
            foreach (var d in importDescriptors)
                idataSection.WriteStruct(&d, sizeof(IMAGE_IMPORT_DESCRIPTOR));

            var dataRAW = fw.FileSize;
            var dataRVA = fw.VirtualSize; // enough to solve data labels
            var dataSection = fw.AllocSection();
            dataSection.WriteByteSequence(data);

            var codeRAW = fw.FileSize;
            var codeRVA = fw.VirtualSize; // enough to solve code labels
            var codeSection = fw.AllocSection();

            var codearray = code.ToArray();
            foreach(var rec in importLabelTable)
            {
                ulong trueRVA = funcToAddress[rec.What.DllName][rec.What.FunctionName];
                ulong value;
                if (rec.IsRelative)
                    value = trueRVA - ((ulong)codeRVA + (ulong)rec.Where);
                else
                    value = trueRVA + imageBase;

                int end = rec.Where;
                int start = end - rec.BytesToWrite;
                byte* buffer = (byte*)(&value);
                for(int i = start; i < end; i++)
                    codearray[i] = buffer[i - start];
            }

            foreach(var rec in dataLableTable)
            {
                ulong trueRVA = (ulong)rec.What.Offset + (ulong)dataRVA;
                ulong value;
                if (rec.IsRelative)
                    value = trueRVA - ((ulong)codeRVA + (ulong)rec.Where);
                else
                    value = trueRVA + imageBase;

                int end = rec.Where;
                int start = end - rec.BytesToWrite;
                byte* buffer = (byte*)(&value);
                for (int i = start; i < end; i++)
                    codearray[i] = buffer[i - start];
            }

            foreach(var rec in codeLableTable)
            {
                ulong value;
                if (rec.IsRelative)
                    value = (ulong)rec.What.Offset - (ulong)rec.Where;
                else
                    value = (ulong)rec.What.Offset + (ulong)codeRVA + imageBase;

                int end = rec.Where;
                int start = end - rec.BytesToWrite;
                byte* buffer = (byte*)(&value);
                for (int i = start; i < end; i++)
                    codearray[i] = buffer[i - start];
            }

            codeSection.WriteByteSequence(codearray);

            const uint IMAGE_SCN_CNT_CODE = 0x00000020;
            const uint IMAGE_SCN_MEM_EXECUTE = 0x20000000;
            const uint IMAGE_SCN_MEM_READ = 0x40000000;
            const uint IMAGE_SCN_MEM_WRITE = 0x80000000;
            

            var imageSize = fw.VirtualSize;
            var dosHeader = IMAGE_DOS_HEADER.CreateUseful();
            var fileHeader = IMAGE_FILE_HEADER.CreateUseful(3);
            var optionalHeader = IMAGE_OPTIONAL_HEADER.CreateUseful(
                (uint)codeRVA,
                imageBase,
                (uint)virtualAlign,
                (uint)fileAlign,
                (uint)imageSize,
                (uint)headerSection.FileSize,
                0x02);
            optionalHeader.DataDirectory[1] = new IMAGE_DATA_DIRECTORY((uint)idataRVA, (uint)idataSection.VirtualSize);
            var ntHeaders = new IMAGE_NT_HEADERS(fileHeader, optionalHeader);

            var idataHeader = IMAGE_SECTION_HEADER.CreateUseful(
                ".idata",
                (uint)idataSection.VirtualSize,
                (uint)idataRVA,
                (uint)idataSection.FileSize,
                (uint)idataRAW,
                IMAGE_SCN_MEM_READ | IMAGE_SCN_MEM_WRITE);
            var dataHeader = IMAGE_SECTION_HEADER.CreateUseful(
                ".data",
                (uint)dataSection.VirtualSize,
                (uint)dataRVA,
                (uint)dataSection.FileSize,
                (uint)dataRAW,
                IMAGE_SCN_MEM_READ | IMAGE_SCN_MEM_WRITE);
            var codeHeader = IMAGE_SECTION_HEADER.CreateUseful(
                ".text",
                (uint)codeSection.VirtualSize,
                (uint)codeRVA,
                (uint)codeSection.FileSize,
                (uint)codeRAW,
                IMAGE_SCN_MEM_READ | IMAGE_SCN_MEM_EXECUTE | IMAGE_SCN_CNT_CODE);

            headerSection.SetPointer(0);
            headerSection.WriteStruct(&dosHeader, sizeof(IMAGE_DOS_HEADER));
            headerSection.WriteStruct(&ntHeaders, sizeof(IMAGE_NT_HEADERS));
            headerSection.WriteStruct(&idataHeader, sizeof(IMAGE_SECTION_HEADER));
            headerSection.WriteStruct(&dataHeader, sizeof(IMAGE_SECTION_HEADER));
            headerSection.WriteStruct(&codeHeader, sizeof(IMAGE_SECTION_HEADER));
            return fw;
        }

        public static void CreateAndSaveImage(
            string filename,
            IEnumerable<byte> data,
            IEnumerable<byte> code,
            ImportLableTableRecord[] importLabelTable,
            LableTableRecord[] dataLableTable,
            LableTableRecord[] codeLableTable
            )
        {
            unsafe
            {
                var fw = CreateImage(data, code, importLabelTable, dataLableTable, codeLableTable);
                fw.WriteFile(filename);
            }
        }
    }

    public class PETEST
    {
        public static void CreateExperimental(string filename)
        {
            const string user32 = "user32.dll";
            const string MessageBox = "MessageBoxA";
            const string Message = "Hello!";

            const string kernel32 = "kernel32.dll";
            const string ExitProcess = "ExitProcess";

            List<byte> data = new();
            foreach (var c in Message)
                data.Add((byte)c);
            data.Add(0);

            List<ImportLableTableRecord> importLabelTable = new();
            List<LableTableRecord> dataLabelTable = new();
            List<LableTableRecord> codeLabelTable = new();

            List<byte> code = new();
            void writecode(params byte[] bytes) => code.AddRange(bytes);

            writecode(0x48, 0x83, 0xEC, 0x28); // sub rsp, 40
            writecode(0x40, 0x80, 0xE4, 0xF0); // and spl, 0xF0
            writecode(0x48, 0x31, 0xC9); // xor rcx, rcx

            writecode(0x48, 0xC7, 0xC2, 0, 0, 0, 0); // mov rdx, ???
            dataLabelTable.Add(new()
            {
                BytesToWrite = 4,
                IsRelative = false,
                What = new(),
                Where = code.Count
            });

            writecode(0x49, 0xC7, 0xC0, 0, 0, 0, 0); // mov r8, ???
            dataLabelTable.Add(new()
            {
                BytesToWrite = 4,
                IsRelative = false,
                What = new(),
                Where = code.Count
            });

            writecode(0x4D, 0x31, 0xC9); // xor r9, r9

            writecode(0xFF, 0x14, 0x25, 0, 0, 0, 0); // call QWORD PTR ???
            importLabelTable.Add(new()
            {
                BytesToWrite = 4,
                IsRelative = false,
                Where = code.Count,
                What = new() { DllName = user32, FunctionName = MessageBox }
            });

            writecode(0x48, 0x31, 0xC9); // xor rcx, rcx
            writecode(0xFF, 0x14, 0x25, 0, 0, 0, 0); // call QWORD PTR ???
            importLabelTable.Add(new()
            {
                BytesToWrite = 4,
                IsRelative = false,
                Where = code.Count,
                What = new() { DllName = kernel32, FunctionName = ExitProcess }
            });

            PEBuilder.CreateAndSaveImage(filename, data, code, importLabelTable.ToArray(), dataLabelTable.ToArray(), codeLabelTable.ToArray());
        }
    }
}
