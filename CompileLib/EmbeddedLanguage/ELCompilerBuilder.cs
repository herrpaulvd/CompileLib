using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public class ELCompilerBuilder
    {
        private ELCompiler compiler = new();

        // some types
        private static readonly ELType LPVOID = ELType.PVoid;
        private static readonly ELType HANDLE = LPVOID;
        private static readonly ELType DWORD = ELType.UInt32;
        private static readonly ELType SIZE = ELType.UInt64;
        private static readonly ELType BOOL = ELType.Int32;

        // memory functions
        private ELVariable hHeap;
        private ELFunction GetProcessHeap, HeapAlloc, HeapReAlloc, HeapFree;
        private ELFunction malloc, realloc, free;

        public ELCompilerBuilder AddMemoryFunctions(
            out ELFunction malloc,
            out ELFunction realloc,
            out ELFunction free,
            bool mallocAuto0Fill,
            bool reallocAuto0Fill)
        {
            if(this.malloc is not null)
            {
                malloc = this.malloc;
                realloc = this.realloc;
                free = this.free;
                return this;
            }

            compiler.OpenEntryPoint();
            hHeap = compiler.AddGlobalVariable(HANDLE);
            GetProcessHeap = compiler.ImportFunction("kernel32.dll", "GetProcessHeap", HANDLE);
            HeapAlloc = compiler.ImportFunction("kernel32.dll", "HeapAlloc", LPVOID, HANDLE, DWORD, SIZE);
            HeapReAlloc = compiler.ImportFunction("kernel32.dll", "HeapReAlloc", LPVOID, HANDLE, DWORD, LPVOID, SIZE);
            HeapFree = compiler.ImportFunction("kernel32.dll", "HeapFree", BOOL, HANDLE, DWORD, LPVOID);
            hHeap.Value = GetProcessHeap.Call();

            this.malloc = malloc = compiler.CreateFunction(LPVOID, SIZE);
            var pSize = malloc.GetParameter(0);
            malloc.Open();
            compiler.Return(HeapAlloc.Call(hHeap, (compiler.MakeConst(mallocAuto0Fill ? 0x08U : 0U)), pSize));

            this.realloc = realloc = compiler.CreateFunction(LPVOID, LPVOID, SIZE);
            var pOld = realloc.GetParameter(0);
            pSize = realloc.GetParameter(1);
            realloc.Open();
            compiler.Return(HeapReAlloc.Call(hHeap, (compiler.MakeConst(reallocAuto0Fill ? 0x08U : 0U)), pOld, pSize));

            this.free = free = compiler.CreateFunction(ELType.Void, LPVOID);
            pOld = free.GetParameter(0);
            free.Open();
            HeapFree.Call(hHeap, compiler.MakeConst(0U), pOld);

            return this;
        }

        // memcpy
        private ELFunction memcpy;
        public ELCompilerBuilder AddMemcpy(
            out ELFunction memcpy
            )
        {
            if(this.memcpy is not null)
            {
                memcpy = this.memcpy;
                return this;
            }

            this.memcpy = memcpy = compiler.ImportFunction("kernel32.dll", "CopyMemory", ELType.Void, LPVOID, LPVOID, SIZE);
            return this;
        }

        // memmove
        private ELFunction memmove;
        public ELCompilerBuilder AddMemmove(
            out ELFunction memmove
            )
        {
            if(this.memmove is not null)
            {
                memmove = this.memmove;
                return this;
            }

            this.memmove = memmove = compiler.ImportFunction("kernel32.dll", "MoveMemory", ELType.Void, LPVOID, LPVOID, SIZE);
            return this;
        }

        // console functions
        private static readonly ELType WCHAR = ELType.UInt16;
        private static readonly ELType PWCHAR = WCHAR.MakePointer();
        private ELVariable conin, conout;
        private const uint STD_INPUT_HANDLE = unchecked((uint)(-10));
        private const uint STD_OUTPUT_HANDLE = unchecked((uint)(-11));
        private const uint CP_UTF8 = 65001;
        private ELFunction GetStdHandle, SetConsoleCP, SetConsoleOutputCP, ReadConsoleW, WriteConsoleW;
        private ELFunction ConsoleReadW, ConsoleWriteW;

        public ELCompilerBuilder AddConsoleFunctionsW(
                out ELFunction ConsoleReadW,
                out ELFunction ConsoleWriteW
            )
        {
            if(this.ConsoleReadW is not null)
            {
                ConsoleReadW = this.ConsoleReadW;
                ConsoleWriteW = this.ConsoleWriteW;
                return this;
            }

            compiler.OpenEntryPoint();
            conin = compiler.AddGlobalVariable(HANDLE);
            conout = compiler.AddGlobalVariable(HANDLE);

            GetStdHandle = compiler.ImportFunction("kernel32.dll", "GetStdHandle", HANDLE, DWORD);
            SetConsoleCP = compiler.ImportFunction("kernel32.dll", "SetConsoleCP", BOOL, DWORD);
            SetConsoleOutputCP = compiler.ImportFunction("kernel32.dll", "SetConsoleOutputCP", BOOL, DWORD);
            ReadConsoleW = compiler.ImportFunction("kernel32.dll", "ReadConsoleW", BOOL, HANDLE, PWCHAR, DWORD, LPVOID, LPVOID);
            WriteConsoleW = compiler.ImportFunction("kernel32.dll", "WriteConsoleW", BOOL, HANDLE, PWCHAR, DWORD, LPVOID, LPVOID);

            conin.Value = GetStdHandle.Call(compiler.MakeConst(STD_INPUT_HANDLE));
            conout.Value = GetStdHandle.Call(compiler.MakeConst(STD_OUTPUT_HANDLE));
            SetConsoleCP.Call(compiler.MakeConst(CP_UTF8));
            SetConsoleOutputCP.Call(compiler.MakeConst(CP_UTF8));

            this.ConsoleReadW = ConsoleReadW = compiler.CreateFunction(SIZE, PWCHAR, SIZE);
            ConsoleReadW.Open();
            var buffer = ConsoleReadW.GetParameter(0);
            var count = ConsoleReadW.GetParameter(1);
            var nCharsWritten = compiler.AddLocalVariable(SIZE);
            nCharsWritten.Value = compiler.MakeConst(0U);
            ReadConsoleW.Call(conin, buffer, count.Cast(DWORD), nCharsWritten.Address, compiler.NULLPTR);
            compiler.Return(nCharsWritten);
            
            this.ConsoleWriteW = ConsoleWriteW = compiler.CreateFunction(ELType.Void, PWCHAR, SIZE);
            ConsoleWriteW.Open();
            buffer = ConsoleWriteW.GetParameter(0);
            count = ConsoleWriteW.GetParameter(1);
            WriteConsoleW.Call(conout, buffer, count.Cast(DWORD), compiler.NULLPTR, compiler.NULLPTR);

            return this;
        }

        // readline function
        private ELFunction ConsoleReadLineW;

        public ELCompilerBuilder AddConsoleReadLineW(
            out ELFunction ConsoleReadLineW
            )
        {
            if(this.ConsoleReadLineW is not null)
            {
                ConsoleReadLineW = this.ConsoleReadLineW;
                return this;
            }

            if (malloc is null)
                throw new Exception($"Before the call, {nameof(AddMemoryFunctions)} call is required");

            if(memcpy is null)
                throw new Exception($"Before the call, {nameof(AddMemcpy)} call is required");

            if(ConsoleReadW is null)
                throw new Exception($"Before the call, {nameof(AddConsoleFunctionsW)} call is required");

            this.ConsoleReadLineW = ConsoleReadLineW = compiler.CreateFunction(PWCHAR, SIZE.MakePointer());
            ConsoleReadLineW.Open();
            var pTotalCount = ConsoleReadLineW.GetParameter(0);
            var totalCount = pTotalCount.PtrToRef();

            var capacity = compiler.AddLocalVariable(SIZE);
            capacity.Value = compiler.MakeConst(16U);
            var bytes = compiler.AddLocalVariable(SIZE);
            bytes.Value = capacity * (uint)WCHAR.Size;
            var result = compiler.AddLocalVariable(PWCHAR);
            result.Value = malloc.Call(bytes).Cast(PWCHAR);
            totalCount.Value = ConsoleReadW.Call(result, capacity);

            var repeatStart = compiler.DefineLabel();
            var repeatEnd = compiler.DefineLabel();

            compiler.MarkLabel(repeatStart);
            compiler.GotoIf(capacity != totalCount, repeatEnd);
            compiler.GotoIf(result[capacity - 1U] == (uint)'\n', repeatEnd);
            bytes.Value = bytes * 2U;
            result.Value = realloc.Call(result, bytes).Cast(PWCHAR);
            totalCount.Value += ConsoleReadW.Call(result + capacity, capacity);
            capacity.Value = capacity * 2U;
            compiler.Goto(repeatStart);
            compiler.MarkLabel(repeatEnd);

            var nullchar = compiler.MakeConst(0U).Cast(WCHAR);
            totalCount.Value -= 1U;
            result[totalCount].Value = nullchar;
            var next = compiler.DefineLabel();
            compiler.GotoIf(!totalCount, next);
            compiler.GotoIf(result[totalCount - 1U] != (uint)'\r', next);
            totalCount.Value -= 1U;
            result[totalCount].Value = nullchar;
            compiler.MarkLabel(next);

            compiler.Return(result);
            return this;
        }

        public ELCompiler Create()
        {
            return compiler;
        }
    }
}
