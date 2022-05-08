using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileLib.EmbeddedLanguage
{
    public abstract class ELExpression
    {
        internal int ID = -1;
        protected internal ELCompiler compiler;
        public ELExpression(ELCompiler compiler)
        {
            this.compiler = compiler;
        }

        public abstract ELType Type { get; }

        public ELExpression Cast(ELType targetType)
            => compiler.TestContext(this, "operand") ?? compiler.AddExpression(new ELCastExpression(this, targetType));

        public ELExpression Dereference
        {
            get => compiler.TestContext(this, "operand") ?? compiler.AddExpression(new ELPointerDereference(this));
            set
            {
                compiler.TestContext(this, "pointer");
                compiler.TestContext(value, "right");
                compiler.AddExpression(new ELBinaryOperation(
                    compiler.AddExpression(new ELPointerDereference(this)), 
                    value, 
                    BinaryOperationType.MOV));
            }
        }

        private static ELExpression CreateBinary(BinaryOperationType t, ELExpression left, ELExpression right)
            => left.compiler.TestContext(left, nameof(left)) 
            ?? left.compiler.TestContext(right, nameof(right)) 
            ?? left.compiler.AddExpression(new ELBinaryOperation(left, right, t));

        private static ELExpression CreateUnary(UnaryOperationType t, ELExpression operand)
            => operand.compiler.TestContext(operand, nameof(operand))
            ?? operand.compiler.AddExpression(new ELUnaryOperation(operand, t));

        public static ELExpression operator+(ELExpression left, ELExpression right)
            => CreateBinary(BinaryOperationType.ADD, left, right);
        public static ELExpression operator+(ELExpression left, long right)
            => CreateBinary(BinaryOperationType.ADD, left, left.compiler.MakeConst(right));
        public static ELExpression operator+(long left, ELExpression right)
            => CreateBinary(BinaryOperationType.ADD, right.compiler.MakeConst(left), right);
        public static ELExpression operator+(ELExpression left, ulong right)
            => CreateBinary(BinaryOperationType.ADD, left, left.compiler.MakeConst(right));
        public static ELExpression operator+(ulong left, ELExpression right)
            => CreateBinary(BinaryOperationType.ADD, right.compiler.MakeConst(left), right);
        public static ELExpression operator+(ELExpression left, int right)
            => CreateBinary(BinaryOperationType.ADD, left, left.compiler.MakeConst(right));
        public static ELExpression operator+(int left, ELExpression right)
            => CreateBinary(BinaryOperationType.ADD, right.compiler.MakeConst(left), right);
        public static ELExpression operator+(ELExpression left, uint right)
            => CreateBinary(BinaryOperationType.ADD, left, left.compiler.MakeConst(right));
        public static ELExpression operator+(uint left, ELExpression right)
            => CreateBinary(BinaryOperationType.ADD, right.compiler.MakeConst(left), right);

        public static ELExpression operator-(ELExpression left, ELExpression right)
            => CreateBinary(BinaryOperationType.SUB, left, right);
        public static ELExpression operator -(ELExpression left, long right)
            => CreateBinary(BinaryOperationType.SUB, left, left.compiler.MakeConst(right));
        public static ELExpression operator -(long left, ELExpression right)
            => CreateBinary(BinaryOperationType.SUB, right.compiler.MakeConst(left), right);
        public static ELExpression operator -(ELExpression left, ulong right)
            => CreateBinary(BinaryOperationType.SUB, left, left.compiler.MakeConst(right));
        public static ELExpression operator -(ulong left, ELExpression right)
            => CreateBinary(BinaryOperationType.SUB, right.compiler.MakeConst(left), right);
        public static ELExpression operator -(ELExpression left, int right)
            => CreateBinary(BinaryOperationType.SUB, left, left.compiler.MakeConst(right));
        public static ELExpression operator -(int left, ELExpression right)
            => CreateBinary(BinaryOperationType.SUB, right.compiler.MakeConst(left), right);
        public static ELExpression operator -(ELExpression left, uint right)
            => CreateBinary(BinaryOperationType.SUB, left, left.compiler.MakeConst(right));
        public static ELExpression operator -(uint left, ELExpression right)
            => CreateBinary(BinaryOperationType.SUB, right.compiler.MakeConst(left), right);

        public static ELExpression operator*(ELExpression left, ELExpression right)
            => CreateBinary(BinaryOperationType.MUL, left, right);
        public static ELExpression operator *(ELExpression left, long right)
            => CreateBinary(BinaryOperationType.MUL, left, left.compiler.MakeConst(right));
        public static ELExpression operator *(long left, ELExpression right)
            => CreateBinary(BinaryOperationType.MUL, right.compiler.MakeConst(left), right);
        public static ELExpression operator *(ELExpression left, ulong right)
            => CreateBinary(BinaryOperationType.MUL, left, left.compiler.MakeConst(right));
        public static ELExpression operator *(ulong left, ELExpression right)
            => CreateBinary(BinaryOperationType.MUL, right.compiler.MakeConst(left), right);
        public static ELExpression operator *(ELExpression left, int right)
            => CreateBinary(BinaryOperationType.MUL, left, left.compiler.MakeConst(right));
        public static ELExpression operator *(int left, ELExpression right)
            => CreateBinary(BinaryOperationType.MUL, right.compiler.MakeConst(left), right);
        public static ELExpression operator *(ELExpression left, uint right)
            => CreateBinary(BinaryOperationType.MUL, left, left.compiler.MakeConst(right));
        public static ELExpression operator *(uint left, ELExpression right)
            => CreateBinary(BinaryOperationType.MUL, right.compiler.MakeConst(left), right);

        public static ELExpression operator/(ELExpression left, ELExpression right)
            => CreateBinary(BinaryOperationType.DIV, left, right);
        public static ELExpression operator /(ELExpression left, long right)
            => CreateBinary(BinaryOperationType.DIV, left, left.compiler.MakeConst(right));
        public static ELExpression operator /(long left, ELExpression right)
            => CreateBinary(BinaryOperationType.DIV, right.compiler.MakeConst(left), right);
        public static ELExpression operator /(ELExpression left, ulong right)
            => CreateBinary(BinaryOperationType.DIV, left, left.compiler.MakeConst(right));
        public static ELExpression operator /(ulong left, ELExpression right)
            => CreateBinary(BinaryOperationType.DIV, right.compiler.MakeConst(left), right);
        public static ELExpression operator /(ELExpression left, int right)
            => CreateBinary(BinaryOperationType.DIV, left, left.compiler.MakeConst(right));
        public static ELExpression operator /(int left, ELExpression right)
            => CreateBinary(BinaryOperationType.DIV, right.compiler.MakeConst(left), right);
        public static ELExpression operator /(ELExpression left, uint right)
            => CreateBinary(BinaryOperationType.DIV, left, left.compiler.MakeConst(right));
        public static ELExpression operator /(uint left, ELExpression right)
            => CreateBinary(BinaryOperationType.DIV, right.compiler.MakeConst(left), right);

        public static ELExpression operator !(ELExpression operand)
            => CreateUnary(UnaryOperationType.BOOLEAN_NOT, operand);
        public static ELExpression operator -(ELExpression operand)
            => CreateUnary(UnaryOperationType.NEG, operand);
        public static ELExpression operator ~(ELExpression operand)
            => CreateUnary(UnaryOperationType.BITWISE_NOT, operand);

        public static ELExpression operator==(ELExpression left, ELExpression right)
        {
            return !(left != right);
        }
        public static ELExpression operator ==(ELExpression left, long right)
            => left == left.compiler.MakeConst(right);
        public static ELExpression operator ==(long left, ELExpression right)
            => right.compiler.MakeConst(left) == right;
        public static ELExpression operator ==(ELExpression left, ulong right)
            => left == left.compiler.MakeConst(right);
        public static ELExpression operator ==(ulong left, ELExpression right)
            => right.compiler.MakeConst(left) == right;
        public static ELExpression operator ==(ELExpression left, int right)
            => left == left.compiler.MakeConst(right);
        public static ELExpression operator ==(int left, ELExpression right)
            => right.compiler.MakeConst(left) == right;
        public static ELExpression operator ==(ELExpression left, uint right)
            => left == left.compiler.MakeConst(right);
        public static ELExpression operator ==(uint left, ELExpression right)
            => right.compiler.MakeConst(left) == right;


        public static ELExpression operator!=(ELExpression left, ELExpression right)
        {
            return left - right;
        }
        public static ELExpression operator !=(ELExpression left, long right)
            => left != left.compiler.MakeConst(right);
        public static ELExpression operator !=(long left, ELExpression right)
            => right.compiler.MakeConst(left) != right;
        public static ELExpression operator !=(ELExpression left, ulong right)
            => left != left.compiler.MakeConst(right);
        public static ELExpression operator !=(ulong left, ELExpression right)
            => right.compiler.MakeConst(left) != right;
        public static ELExpression operator !=(ELExpression left, int right)
            => left != left.compiler.MakeConst(right);
        public static ELExpression operator !=(int left, ELExpression right)
            => right.compiler.MakeConst(left) != right;
        public static ELExpression operator !=(ELExpression left, uint right)
            => left != left.compiler.MakeConst(right);
        public static ELExpression operator !=(uint left, ELExpression right)
            => right.compiler.MakeConst(left) != right;

        public ELExpression this[ELExpression index]
        {
            get => (this + index).Dereference;
            set => (this + index).Dereference = value;
        }
        public ELExpression this[long index]
        {
            get => (this + index).Dereference;
            set => (this + index).Dereference = value;
        }
        public ELExpression this[ulong index]
        {
            get => (this + index).Dereference;
            set => (this + index).Dereference = value;
        }
        public ELExpression this[int index]
        {
            get => (this + index).Dereference;
            set => (this + index).Dereference = value;
        }
        public ELExpression this[uint index]
        {
            get => (this + index).Dereference;
            set => (this + index).Dereference = value;
        }

        public ELExpression Reference => compiler.TestContext(this, "operand") ?? compiler.AddExpression(new ELReferenceExpression(this));

        public ELExpression GetFieldReference(int fieldIndex)
        {
            if(Type is ELPointerType pt && pt.BaseType is ELStructType st)
            {
                if (fieldIndex < 0 || fieldIndex >= st.FieldCount)
                    throw new ArgumentException("Invalid field index", nameof(fieldIndex));

                return compiler.TestContext(this, "operand") ?? compiler.AddExpression(new ELGetFieldReferenceExpression(fieldIndex, this));
            }
            throw new ArgumentException("The operand must be pointer to struct", "operand");
        }
    }
}
