using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompileLib.PEGen;

namespace CompileLib.QuasiAsm
{
    internal class AsmFunction : IAsmOperationType
    {
        private List<AsmOperand> parameters = new();
        private AsmOperand result;
        private List<AsmOperand> locals = new();
        private List<AsmOperation> operations = new();

        public AsmSenderRequired SenderRequired => AsmSenderRequired.Array;

        public AsmFunction(bool struc, bool signed, int size, object tag)
        {
            result = new(AsmOperandType.LocalVar, AsmOperandUse.Val, struc, signed, 0, size, tag);
        }

        public AsmOperand AddParameter(bool struc, bool signed, int size, object tag)
        {
            int id = parameters.Count;
            AsmOperand result = new(AsmOperandType.Param, AsmOperandUse.Val, struc, signed, id, size, tag);
            parameters.Add(result);
            return result;
        }

        public AsmOperand AddLocal(bool struc, bool signed, int size, object tag)
        {
            int id = locals.Count;
            AsmOperand result = new(AsmOperandType.LocalVar, AsmOperandUse.Val, struc, signed, id, size, tag);
            locals.Add(result);
            return result;
        }

        public int GetIP() => operations.Count;
        
        private void AddOperation(IAsmOperationType type, AsmOperand destination, object? source)
            => operations.Add(new AsmOperation(destination, source, type));
        public void AddOperation(IAsmOperationType type, AsmOperand destination)
            => AddOperation(type, destination, (object?)null);
        public void AddOperation(IAsmOperationType type, AsmOperand destination, AsmOperand source)
            => AddOperation(type, destination, source);
        public void AddOperation(IAsmOperationType type, AsmOperand destination, AsmOperand left, AsmOperand right)
            => AddOperation(type, destination, Tuple.Create(left, right));
        public void AddOperation(IAsmOperationType type, AsmOperand destination, AsmOperand[] source)
            => AddOperation(type, destination, source);

        public void Compile(
            List<byte> output, 
            List<ImportLableTableRecord> importTable,
            List<LableTableRecord> dataTable,
            List<LableTableRecord> globalVarTable,
            List<LableTableRecord> codeTable
            )
        {
            // TODO: запретим возврат Struc из функции
            // чтобы возвращать результат через регистры

            // 1: оптимизация ячеек - строим граф на локальных переменных, на которые не берётся ссылка
            // строим minmax
            // делаем scanline с переопределением ячеек
            // должен получиться массив index -> true AsmOperand

            // 2: выделяем на стеке память под всё локальное - команда ENTER

            // 3: emit => когда ret, не забываем вызвать LEAVE

            // 4: проверка на ret в конце и добавление если нету
        }
    }
}
