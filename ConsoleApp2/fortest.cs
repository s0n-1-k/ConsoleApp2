using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestProject1")]

namespace MathExpressionsLab
{
    public interface IExpr
    {
        IEnumerable<string> Variables { get; }
        bool IsConstant { get; }
        bool IsPolynomial { get; }
        int PolynomialDegree { get; }
        double Compute(IReadOnlyDictionary<string, double> variableValues);
    }

    public abstract class Expr : IExpr
    {
        public abstract IEnumerable<string> Variables { get; }
        public abstract bool IsConstant { get; }
        public abstract bool IsPolynomial { get; }
        public abstract int PolynomialDegree { get; }
        public abstract double Compute(IReadOnlyDictionary<string, double> variableValues);

        internal virtual int Precedence => 0;

        internal virtual string ToFormattedString(int parentPrecedence = 0, bool isRightOperand = false)
        {
            return ToString();
        }

        public override string ToString()
        {
            return ToFormattedString(0, false);
        }

        public static Expr operator +(Expr left, Expr right) => new Add(left, right);
        public static Expr operator -(Expr left, Expr right) => new Subtract(left, right);
        public static Expr operator *(Expr left, Expr right) => new Multiply(left, right);
        public static Expr operator /(Expr left, Expr right) => new Divide(left, right);
        public static Expr operator -(Expr operand) => new Negate(operand);

        public static implicit operator Expr(double value) => new Constant(value);

        public static Expr Sqrt(Expr arg) => new Sqrt(arg);
        public static Expr Sinh(Expr arg) => new Sinh(arg);
        public static Expr Cosh(Expr arg) => new Cosh(arg);
        public static Expr Tanh(Expr arg) => new Tanh(arg);
        public static Expr Coth(Expr arg) => new Coth(arg);
    }

    public sealed class Constant : Expr
    {
        public double Value { get; }

        public Constant(double value) => Value = value;

        public override IEnumerable<string> Variables => Enumerable.Empty<string>();
        public override bool IsConstant => true;
        public override bool IsPolynomial => true;
        public override int PolynomialDegree => 0;

        public override double Compute(IReadOnlyDictionary<string, double> variableValues) => Value;

        internal override string ToFormattedString(int parentPrecedence = 0, bool isRightOperand = false)
        {
            string valStr = Value.ToString(CultureInfo.InvariantCulture);
            if (Value < 0)
            {
                return $"({valStr})";
            }
            return valStr;
        }
    }

    public sealed class Variable : Expr
    {
        public string Name { get; }

        public Variable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Имя переменной не может быть пустым.", nameof(name));
            Name = name;
        }

        public override IEnumerable<string> Variables => new[] { Name };
        public override bool IsConstant => false;
        public override bool IsPolynomial => true;
        public override int PolynomialDegree => 1;

        public override double Compute(IReadOnlyDictionary<string, double> variableValues)
        {
            if (variableValues == null || !variableValues.TryGetValue(Name, out double val))
                throw new KeyNotFoundException($"Значение для переменной '{Name}' не найдено.");
            return val;
        }

        internal override string ToFormattedString(int parentPrecedence = 0, bool isRightOperand = false) => Name;
    }

    public abstract class UnaryOperation : Expr
    {
        public Expr Operand { get; }

        protected UnaryOperation(Expr operand)
            => Operand = operand ?? throw new ArgumentNullException(nameof(operand));

        public override IEnumerable<string> Variables => Operand.Variables.Distinct();
        public override bool IsConstant => Operand.IsConstant;

        public override bool IsPolynomial => Operand.IsPolynomial;
        public override int PolynomialDegree => Operand.PolynomialDegree;
    }

    public abstract class BinaryOperation : Expr
    {
        public Expr Left { get; }
        public Expr Right { get; }
        public abstract string OpSymbol { get; }

        protected BinaryOperation(Expr left, Expr right)
        {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
        }

        public override IEnumerable<string> Variables => Left.Variables.Union(Right.Variables).Distinct();
        public override bool IsConstant => Left.IsConstant && Right.IsConstant;

        internal override string ToFormattedString(int parentPrecedence = 0, bool isRightOperand = false)
        {
            bool needParens = false;

            if (this.Precedence < parentPrecedence)
            {
                needParens = true;
            }
            else if (this.Precedence == parentPrecedence)
            {
                if (isRightOperand && OpSymbol == "/")
                {
                    needParens = true;
                }
            }

            string leftStr = Left.ToFormattedString(this.Precedence, false);
            string rightStr = Right.ToFormattedString(this.Precedence, true);

            string formatted = $"{leftStr} {OpSymbol} {rightStr}";

            return needParens ? $"({formatted})" : formatted;
        }
    }

    public sealed class Negate : UnaryOperation
    {
        public Negate(Expr operand) : base(operand) { }

        //public override bool IsPolynomial => Operand.IsPolynomial;
        //public override int PolynomialDegree => Operand.PolynomialDegree;

        public override double Compute(IReadOnlyDictionary<string, double> variableValues)
            => -Operand.Compute(variableValues);

        internal override int Precedence => 3;

        internal override string ToFormattedString(int parentPrecedence = 0, bool isRightOperand = false)
        {
            string inner = Operand.ToFormattedString(this.Precedence, false);

            if (Operand is Negate)
            {
                inner = $"({inner})";
            }

            string formatted = $"-{inner}";

            bool needOuterParens = this.Precedence < parentPrecedence;

            if (isRightOperand && parentPrecedence >= 2)
            {
                needOuterParens = true;
            }

            return needOuterParens ? $"({formatted})" : formatted;
        }
    }

    public sealed class Add : BinaryOperation
    {
        public Add(Expr left, Expr right) : base(left, right) { }

        public override string OpSymbol => "+";
        internal override int Precedence => 1;

        public override bool IsPolynomial => Left.IsPolynomial && Right.IsPolynomial;
        public override int PolynomialDegree => IsPolynomial ? Math.Max(Left.PolynomialDegree, Right.PolynomialDegree) : -1;

        public override double Compute(IReadOnlyDictionary<string, double> variableValues)
            => Left.Compute(variableValues) + Right.Compute(variableValues);
    }

    public sealed class Subtract : BinaryOperation
    {
        public Subtract(Expr left, Expr right) : base(left, right) { }

        public override string OpSymbol => "-";
        internal override int Precedence => 1;

        public override bool IsPolynomial => Left.IsPolynomial && Right.IsPolynomial;
        public override int PolynomialDegree => IsPolynomial ? Math.Max(Left.PolynomialDegree, Right.PolynomialDegree) : -1;

        public override double Compute(IReadOnlyDictionary<string, double> variableValues)
            => Left.Compute(variableValues) - Right.Compute(variableValues);
        internal override string ToFormattedString(int parentPrecedence = 0, bool isRightOperand = false)
        {
            bool needParens = false;

            // 1. Если приоритет текущей операции ниже, чем у родителя, нужны скобки (как в базовом классе)
            if (this.Precedence < parentPrecedence)
            {
                needParens = true;
            }

            string leftStr = Left.ToFormattedString(this.Precedence, false);

            // 2. Вычисляем строку для правого операнда
            string rightStr = Right.ToFormattedString(this.Precedence, true);

            // 3. ГЛАВНОЕ ИСПРАВЛЕНИЕ: если справа стоит сложение или вычитание, 
            // принудительно оборачиваем его в скобки. (a - (b + c) или a - (b - c))
            if (Right is Add || Right is Subtract)
            {
                rightStr = $"({rightStr})";
            }

            string formatted = $"{leftStr} {OpSymbol} {rightStr}";

            return needParens ? $"({formatted})" : formatted;
        }
    }

    public sealed class Multiply : BinaryOperation
    {
        public Multiply(Expr left, Expr right) : base(left, right) { }

        public override string OpSymbol => "*";
        internal override int Precedence => 2;

        public override bool IsPolynomial => Left.IsPolynomial && Right.IsPolynomial;
        public override int PolynomialDegree => IsPolynomial ? Left.PolynomialDegree + Right.PolynomialDegree : -1;

        public override double Compute(IReadOnlyDictionary<string, double> variableValues)
            => Left.Compute(variableValues) * Right.Compute(variableValues);
    }

    public sealed class Divide : BinaryOperation
    {
        public Divide(Expr left, Expr right) : base(left, right) { }

        public override string OpSymbol => "/";
        internal override int Precedence => 2;

        public override bool IsPolynomial => Left.IsPolynomial && Right.IsConstant;
        public override int PolynomialDegree => IsPolynomial ? Left.PolynomialDegree : -1;

        public override double Compute(IReadOnlyDictionary<string, double> variableValues)
        {
            double rightVal = Right.Compute(variableValues);
            if (Math.Abs(rightVal) < 1e-15)
                throw new DivideByZeroException("Деление на ноль при вычислении выражения.");
            return Left.Compute(variableValues) / rightVal;
        }
    }

    public abstract class Function : Expr
    {
        public Expr Argument { get; }
        public abstract string Name { get; }

        protected Function(Expr argument)
            => Argument = argument ?? throw new ArgumentNullException(nameof(argument));

        public override IEnumerable<string> Variables => Argument.Variables.Distinct();
        public override bool IsConstant => Argument.IsConstant;

        public override bool IsPolynomial => Argument.IsConstant;
        public override int PolynomialDegree => Argument.IsConstant ? 0 : -1;

        protected abstract double Evaluate(double arg);

        public override double Compute(IReadOnlyDictionary<string, double> variableValues)
            => Evaluate(Argument.Compute(variableValues));

        //       internal override int Precedence => 4;

        internal override string ToFormattedString(int parentPrecedence = 0, bool isRightOperand = false)
        {
            string argStr = Argument.ToFormattedString(0, false);
            return $"{Name}({argStr})";
        }
    }

    public sealed class Sqrt : Function
    {
        public Sqrt(Expr argument) : base(argument) { }
        public override string Name => "Sqrt";

        protected override double Evaluate(double arg)
        {
            if (arg < 0)
                throw new ArgumentOutOfRangeException(nameof(arg), "Подкоренное выражение не может быть отрицательным.");
            return Math.Sqrt(arg);
        }
    }

    public sealed class Sinh : Function
    {
        public Sinh(Expr argument) : base(argument) { }
        public override string Name => "Sinh";

        protected override double Evaluate(double arg) => Math.Sinh(arg);
    }

    public sealed class Cosh : Function
    {
        public Cosh(Expr argument) : base(argument) { }
        public override string Name => "Cosh";

        protected override double Evaluate(double arg) => Math.Cosh(arg);
    }

    public sealed class Tanh : Function
    {
        public Tanh(Expr argument) : base(argument) { }
        public override string Name => "Tanh";

        protected override double Evaluate(double arg) => Math.Tanh(arg);
    }

    public sealed class Coth : Function
    {
        public Coth(Expr argument) : base(argument) { }
        public override string Name => "Coth";

        protected override double Evaluate(double arg)
        {
            double th = Math.Tanh(arg);
            if (Math.Abs(th) < 1e-15)
                throw new DivideByZeroException("Аргумент Coth равен 0 (деление на ноль).");
            return 1.0 / th;
        }
    }
}
