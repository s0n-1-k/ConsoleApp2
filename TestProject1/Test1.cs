using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MathExpressionsLab;
using System.Runtime.CompilerServices;

namespace MathExpressionsLab.Tests
{
 
    [TestClass]
    public class AddTests
    {
        // ---------- 1. Вычисление ----------

        [TestMethod]
        public void Compute_TwoConstants_ReturnsSum()
        {
            // Arrange
            var expr = new Add(new Constant(2), new Constant(3));

            // Act
            double actual = expr.Compute(new Dictionary<string, double>());

            // Assert
            Assert.AreEqual(5.0, actual, 1e-9);
        }

        [TestMethod]
        public void Compute_WithVariables_UsesProvidedValues()
        {
            // Arrange
            var x = new Variable("x");
            var y = new Variable("y");
            var expr = new Add(x, y); // x + y

            var values = new Dictionary<string, double> { ["x"] = 1.5, ["y"] = 2.5 };

            // Act
            double actual = expr.Compute(values);

            // Assert
            Assert.AreEqual(4.0, actual, 1e-9);
        }

        [TestMethod]
        public void Compute_NestedAdds_ReturnsCorrectResult()
        {
            // Arrange: (1 + 2) + (3 + 4)
            var expr = new Add(
                new Add(new Constant(1), new Constant(2)),
                new Add(new Constant(3), new Constant(4))
            );

            // Act
            double actual = expr.Compute(new Dictionary<string, double>());

            // Assert
            Assert.AreEqual(10.0, actual, 1e-9);
        }

        // ---------- 2. Метаданные ----------

        [TestMethod]
        public void Variables_ReturnsUnionOfLeftAndRight()
        {
            // Arrange
            var expr = new Add(new Variable("x"), new Variable("y"));

            // Act
            var vars = new List<string>(expr.Variables);

            // Assert
            CollectionAssert.AreEquivalent(new[] { "x", "y" }, vars);
        }

        [TestMethod]
        public void Variables_DuplicatesRemoved()
        {
            // Arrange: x + x
            var expr = new Add(new Variable("x"), new Variable("x"));

            // Act
            var vars = new List<string>(expr.Variables);

            // Assert
            Assert.AreEqual(1, vars.Count);
            Assert.AreEqual("x", vars[0]);
        }

        [TestMethod]
        public void IsConstant_TrueOnlyWhenBothOperandsConstant()
        {
            Assert.IsTrue(new Add(new Constant(1), new Constant(2)).IsConstant);
            Assert.IsFalse(new Add(new Constant(1), new Variable("x")).IsConstant);
            Assert.IsFalse(new Add(new Variable("x"), new Variable("y")).IsConstant);
        }

        [TestMethod]
        public void IsPolynomial_TrueWhenBothOperandsPolynomial()
        {
            Assert.IsTrue(new Add(new Constant(1), new Variable("x")).IsPolynomial);
            Assert.IsTrue(new Add(new Variable("x"), new Variable("y")).IsPolynomial);

            // Sqrt(x) не полином (аргумент — переменная)
            Assert.IsFalse(new Add(new Sqrt(new Variable("x")), new Constant(1)).IsPolynomial);
        }

        [TestMethod]
        public void PolynomialDegree_IsMaxOfOperands()
        {
            // x + x^2 (степени 1 и 2) -> 2
            var x = new Variable("x");
            var expr = new Add(x, new Multiply(x, x));

            Assert.AreEqual(2, expr.PolynomialDegree);
        }

        [TestMethod]
        public void PolynomialDegree_WithConstantOnly_IsZero()
        {
            Assert.AreEqual(0, new Add(new Constant(1), new Constant(2)).PolynomialDegree);
        }

        // ---------- 3. Форматирование ----------

        [TestMethod]
        public void ToString_SimpleConstants_FormatsWithSpaces()
        {
            var expr = new Add(new Constant(1), new Constant(2));

            Assert.AreEqual("1 + 2", expr.ToString());
        }

        [TestMethod]
        public void ToString_NestedAdd_NoParensAroundSamePrecedence()
        {
            // (a + b) + c  ->  "a + b + c"  (левая ассоциативность, скобки не нужны)
            var a = new Variable("a");
            var b = new Variable("b");
            var c = new Variable("c");
            var expr = new Add(new Add(a, b), c);

            Assert.AreEqual("a + b + c", expr.ToString());
        }

        [TestMethod]
        public void ToString_AddInsideSubtractRight_Parenthesized()
        {
            // a - (b + c)  ->  правая часть должна быть в скобках
            var a = new Variable("a");
            var b = new Variable("b");
            var c = new Variable("c");
            var expr = new Subtract(a, new Add(b, c));

            Assert.AreEqual("a - (b + c)", expr.ToString());
        }

        [TestMethod]
        public void ToString_NegativeConstant_Parenthesized()
        {
            // (-3) + x  ->  Constant(-3) выводится как "(-3)"
            var expr = new Add(new Constant(-3), new Variable("x"));

            Assert.AreEqual("(-3) + x", expr.ToString());
        }

        // ---------- 4. Перегрузка оператора и неявное преобразование ----------

        [TestMethod]
        public void OperatorPlus_CreatesAddExpression()
        {
            var x = new Variable("x");

            Expr expr = x + 5; // неявное преобразование 5 -> Constant(5)

            Assert.IsInstanceOfType(expr, typeof(Add));
            Assert.AreEqual("x + 5", expr.ToString());
            Assert.AreEqual(7.0, expr.Compute(new Dictionary<string, double> { ["x"] = 2 }), 1e-9);
        }

        // ---------- 5. Обработка ошибок ----------

        [TestMethod]
        public void Compute_MissingVariableValue_Throws()
        {
            // Arrange
            var expr = new Add(new Variable("x"), new Variable("y"));
            var values = new Dictionary<string, double> { ["x"] = 1 };

            // Act & Assert (проверяем, что вызов бросает нужное исключение)
            Assert.ThrowsExactly<KeyNotFoundException>(() => expr.Compute(values));
        }
        [TestMethod]
        public void Compute_ExistingVariable_ReturnsValue()
        {
            // Arrange
            var x = new Variable("x");
            var values = new Dictionary<string, double> { ["x"] = 42.5 };

            // Act
            double actual = x.Compute(values);

            // Assert
            Assert.AreEqual(42.5, actual, 1e-9);
        }
        [TestMethod]
        public void PolynomialDegree_WhenNotPolynomial_ReturnsMinusOne()
        {
            // Arrange
            var x = new Variable("x");
            // Sqrt(x) не является полиномом (так как x - переменная)
            var nonPolynomial = new Sqrt(x);

            // Складываем не-полином и константу
            var expr = new Add(nonPolynomial, new Constant(5));

            // Act
            int degree = expr.PolynomialDegree;

            // Assert
            Assert.AreEqual(-1, degree);
        }
        [TestMethod]
        public void Constructor_NullOperand_ThrowsArgumentNullException()
        {
            var x = new Variable("x");
            Assert.ThrowsExactly<ArgumentNullException>(() => new Add(null, x));
            Assert.ThrowsExactly<ArgumentNullException>(() => new Add(x, null));
        }
    }
    [TestClass]
    public class SubsractTests
    {
        [TestMethod]
        public void Compute_TwoConstants_ReturnsSub()
        {
            // Arrange
            var expr = new Subtract(new Constant(10), new Constant(3));

            // Act
            double actual = expr.Compute(new Dictionary<string, double>());

            // Assert
            Assert.AreEqual(7.0, actual, 1e-9);
        }
        [TestMethod]
        public void Compute_Variables_ReturnsSub()
        {
            var x = new Variable("x");
            var y = new Variable("y");
            var expr = new Subtract(x, y);
            var values = new Dictionary<string, double> { ["x"] = 10, ["y"] = 7 };
            double actual = expr.Compute(values);
            Assert.AreEqual(3.0, actual, 1e-9);
        }
        [TestMethod]
        public void IsVariables_forSub()
        {
            var x = new Variable("x");
            var y = new Variable("y");
            var expr = new Subtract(x, y);
            CollectionAssert.AreEquivalent(new[] { "x", "y" }, expr.Variables.ToList());
        }
        [TestMethod]
        public void Ispolinomial_Sub()
        {
            var x = new Variable("x");
            var polin = new Subtract(x, new Constant(5));
            Assert.IsTrue(polin.IsPolynomial);
            var nonpoli = new Subtract(new Sqrt(x), new Constant(5));
            Assert.IsFalse(nonpoli.IsPolynomial);
        }
        [TestMethod]
        public void PolynomialDegree_MaxOrMIinysOne()
        {
            var x = new Variable("x");
            var xx = new Multiply(x, x);
            var poly = new Subtract(xx, x);
            Assert.AreEqual(2, poly.PolynomialDegree);
            var nonpoly = new Subtract(new Sqrt(x), x);
            Assert.AreEqual(-1, nonpoly.PolynomialDegree);

        }
        [TestMethod]
        public void ToString_Subtract()
        {
            var x = new Variable("x");
            var y = new Variable("y");
            var z = new Variable("z");
            var expr = new Subtract(x, y);
            Assert.AreEqual("x - y", expr.ToString());
            var expr2 = new Subtract(x, new Add(y, z));
            Assert.AreEqual("x - (y + z)", expr2.ToString());
            var expr3 = new Multiply(new Subtract(x, y), z);
            Assert.AreEqual("(x - y) * z", expr3.ToString());
        }
        [TestMethod]
        public void ToString_SubtractInsideSubtractRight_Parenthesized()
        {
            var a = new Variable("a");
            var b = new Variable("b");
            var c = new Variable("c");
            var expr = new Subtract(a, new Subtract(b, c));

            Assert.AreEqual("a - (b - c)", expr.ToString());
        }
    }
    [TestClass]
    public class MultiplyTest
    {
        [TestMethod]
        public void Mult_Const()
        {
            var expr = new Multiply(new Constant(5), new Constant(4));
            double actual = expr.Compute(new Dictionary<string, double>());
            Assert.AreEqual(20.0, actual, 1e-9);
        }
        [TestMethod]
        public void Mult_Var()
        {
            var x = new Variable("x");
            var y = new Variable("y");
            var expr = new Multiply(x, y);
            var val = new Dictionary<string, double> { ["x"] = 5, ["y"] = 10 };
            double actual = expr.Compute(val);
            Assert.AreEqual(50.0, actual, 1e-9);
        }
        [TestMethod]
        public void Ispolinomial_Mult()
        {
            var x = new Variable("x");
            var polin = new Multiply(x, new Constant(5));
            Assert.IsTrue(polin.IsPolynomial);
            var nonpoli = new Multiply(new Sqrt(x), new Constant(5));
            Assert.IsFalse(nonpoli.IsPolynomial);
        }
        [TestMethod]
        public void MultPolynomialDegree_MaxOrMIinysOne()
        {
            var x = new Variable("x");
            var xx = new Multiply(x, x);
            var poly = new Multiply(xx, x);
            Assert.AreEqual(3, poly.PolynomialDegree);
            var nonpoly = new Multiply(new Sqrt(x), x);
            Assert.AreEqual(-1, nonpoly.PolynomialDegree);

        }
        [TestMethod]
        public void ToString_MultiplyInsideMultiply_NoParens()
        {
            var a = new Variable("a");
            var b = new Variable("b");
            var c = new Variable("c");

            var expr = new Multiply(a, new Multiply(b, c));

            Assert.AreEqual("a * b * c", expr.ToString());
        }
    }
    [TestClass]
    public class Div
    {
        [TestMethod]
        public void Div_Const()
        {
            var expr = new Divide(new Constant(20), new Constant(4));
            double actual = expr.Compute(new Dictionary<string, double>());
            Assert.AreEqual(5.0, actual, 1e-9);
        }
        [TestMethod]
        public void Div_Var()
        {
            var x = new Variable("x");
            var y = new Variable("y");
            var expr = new Divide(x, y);
            var val = new Dictionary<string, double> { ["x"] = 50, ["y"] = 10 };
            double actual = expr.Compute(val);
            Assert.AreEqual(5.0, actual, 1e-9);
        }
        [TestMethod]
        public void Div_OnZero()
        {
            var x = new Variable("x");
            var y = new Variable("y");
            var expr = new Divide(x, y);
            var val = new Dictionary<string, double> { ["x"] = 50, ["y"] = 0 };

            Assert.ThrowsExactly<DivideByZeroException>(() => expr.Compute(val));
        }
        [TestMethod]
        public void Ispolinomial_Div()
        {
            var x = new Variable("x");
            var polin = new Divide(x, new Constant(5));
            Assert.IsTrue(polin.IsPolynomial);
            var nonpoli = new Divide(new Sqrt(x), new Constant(5));
            Assert.IsFalse(nonpoli.IsPolynomial);
        }
        [TestMethod]
        public void MultPolynomialDegree_MaxOrMIinysOne()
        {
            var x = new Variable("x");
            var xx = new Multiply(x, x);
            var poly = new Divide(xx, new Constant(5));
            Assert.AreEqual(2, poly.PolynomialDegree);
            var nonpoly = new Divide(new Sqrt(x), x);
            Assert.AreEqual(-1, nonpoly.PolynomialDegree);

        }
        [TestMethod]
        public void ToString_Div()
        {
            var x = new Variable("x");
            var y = new Variable("y");
            var z = new Variable("z");
            var expr = new Divide(x, y);
            Assert.AreEqual("x / y", expr.ToString());
            var expr3 = new Divide(new Subtract(x, y), z);
            Assert.AreEqual("(x - y) / z", expr3.ToString());
        }
        [TestMethod]
        public void ToString_DivideInsideDivideRight_Parenthesized()
        {
            var a = new Variable("a");
            var b = new Variable("b");
            var c = new Variable("c");
            var expr = new Divide(a, new Divide(b, c));

            Assert.AreEqual("a / (b / c)", expr.ToString());
        }
    }
    [TestClass]
    public class Fignya
    {

        [TestMethod]
        public void Negate()
        {
            var expr = new Negate(new Constant(5));
            double act = expr.Compute(new Dictionary<string, double>());

            Assert.AreEqual(-5.0, act, 1e-9);
        }
        [TestMethod]
        public void Negate_IsConstant_ReturnsCorrectValue()
        {
            var constExpr = new Negate(new Constant(5));
            var varExpr = new Negate(new Variable("x"));

            Assert.IsTrue(constExpr.IsConstant);
            Assert.IsFalse(varExpr.IsConstant);
        }
        [TestMethod]
        public void Negate_PolynomialProperties_ReturnsOperandValues()
        {
            var x = new Variable("x");
            var expr = new Negate(x);
            Assert.IsTrue(expr.IsPolynomial);
            Assert.AreEqual(1, expr.PolynomialDegree);
        }
        [TestMethod]
        public void Negate_Constructor_NullOperand_ThrowsException()
        {
            Assert.ThrowsExactly<System.ArgumentNullException>(() => new Negate(null));
        }
        [TestMethod]
        public void Negate_Var()
        {
            var expr = new Negate(new Variable("x"));

            CollectionAssert.AreEquivalent(new[] { "x" }, expr.Variables.ToList());
        }
        [TestMethod]
        public void Neg_IsPolynomial_ReturnsOperandValue()
        {
            var expr = new Negate(new Variable("x"));

            Assert.IsTrue(expr.IsPolynomial);
            Assert.AreEqual(1, expr.PolynomialDegree);
        }
        [TestMethod]
        public void Neg_ToString_Simple()
        {
            var expr = new Negate(new Variable("x"));
            Assert.AreEqual("-x", expr.ToString());
            var expr1 = new Negate(new Add(new Variable("a"), new Variable("b")));
            Assert.AreEqual("-(a + b)", expr1.ToString());
            var inner = new Negate(new Variable("x"));
            var expr2 = new Negate(inner);

            Assert.AreEqual("-(-x)", expr2.ToString());

        }
        [TestMethod]
        public void Neg_BigViraghenie()
        {
            var expr = new Multiply(new Variable("a"), new Negate(new Variable("x")));
            Assert.AreEqual("a * (-x)", expr.ToString());
        }
    }
    [TestClass]
    public class Unarnii
    {
        [TestMethod]
        public void Sqrt_Compute_ValidNumber_CallsEvaluate()
        {
            var expr = new Sqrt(new Constant(16));
            double actual = expr.Compute(new Dictionary<string, double>());
            Assert.AreEqual(4.0, actual, 1e-9);
        }
        [TestMethod]
        public void Sqrt_ToString_ReturnsFormattedString()
        {
            var x = new Variable("x");
            var expr = new Sqrt(x);
            string actual = expr.ToString();
            Assert.AreEqual("Sqrt(x)", actual);
        }
        [TestMethod]
        public void Sqrt_Compute_NegativeNumber_ThrowsArgumentOutOfRangeException()
        {
            var expr = new Sqrt(new Constant(-1));

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                expr.Compute(new Dictionary<string, double>()));
        }
        [TestMethod]
        public void Sinh_Compute_Constant_ReturnsValue()
        {
            var expr = new Sinh(new Constant(0));
            double actual = expr.Compute(new Dictionary<string, double>());
            Assert.AreEqual(0.0, actual, 1e-9);
        }

        [TestMethod]
        public void Sinh_Compute_Variable_ReturnsValue()
        {
            var x = new Variable("x");
            var expr = new Sinh(x);
            var values = new Dictionary<string, double> { ["x"] = 1.0 };
            double actual = expr.Compute(values);
            Assert.AreEqual(1.175201, actual, 1e-5);
        }

        [TestMethod]
        public void Sinh_ToString_ReturnsFormattedString()
        {
            var expr = new Sinh(new Variable("x"));
            string actual = expr.ToString();
            Assert.AreEqual("Sinh(x)", actual);
        }

        [TestMethod]
        public void Cosh_Compute_Constant_ReturnsValue()
        {
            var expr = new Cosh(new Constant(0));
            double actual = expr.Compute(new Dictionary<string, double>());
            Assert.AreEqual(1.0, actual, 1e-9);
        }

        [TestMethod]
        public void Cos_Compute_Variable_ReturnsValue()
        {
            var x = new Variable("x");
            var expr = new Cosh(x);
            var values = new Dictionary<string, double> { ["x"] = 1.0 };
            double actual = expr.Compute(values);
            Assert.AreEqual(1.54308, actual, 1e-5);
        }

        [TestMethod]
        public void Cosh_ToString_ReturnsFormattedString()
        {
            var expr = new Cosh(new Variable("x"));
            string actual = expr.ToString();
            Assert.AreEqual("Cosh(x)", actual);
        }
        [TestMethod]
        public void Sinh_Properties_CheckMetadata()
        {
            var x = new Variable("x");
            var exprVar = new Sinh(x);
            var exprConst = new Sinh(new Constant(5));

            CollectionAssert.AreEquivalent(new[] { "x" }, exprVar.Variables.ToList());

            Assert.IsFalse(exprVar.IsConstant);
            Assert.IsTrue(exprConst.IsConstant);

            Assert.IsFalse(exprVar.IsPolynomial);
            Assert.AreEqual(-1, exprVar.PolynomialDegree);
            Assert.IsTrue(exprConst.IsPolynomial);
            Assert.AreEqual(0, exprConst.PolynomialDegree);
        }
        [TestMethod]
        public void Function_Constructor_NullArgument_ThrowsException()
        {

            Assert.ThrowsExactly<System.ArgumentNullException>(() => new Sinh(null));
        }
        [TestMethod]
        public void Sinh_ToString_CoversPrecedence()
        {
            var expr = new Sinh(new Variable("x"));

            string actual = expr.ToString();

            Assert.AreEqual("Sinh(x)", actual);
        }
    }
    [TestClass]
    public class TanhTests
    {
        [TestMethod]
        public void Tanh_Compute_Constant_ReturnsValue()
        {
            var expr = new Tanh(new Constant(0));
            double actual = expr.Compute(new Dictionary<string, double>());
            Assert.AreEqual(0.0, actual, 1e-9);
        }

        [TestMethod]
        public void Tanh_Compute_Variable_ReturnsValue()
        {
            var expr = new Tanh(new Variable("x"));
            var values = new Dictionary<string, double> { ["x"] = 1.0 };
            double actual = expr.Compute(values);
            Assert.AreEqual(0.76159, actual, 1e-5);
        }

        [TestMethod]
        public void Tanh_ToString_ReturnsFormattedString()
        {
            var expr = new Tanh(new Variable("x"));
            Assert.AreEqual("Tanh(x)", expr.ToString());
        }
    }

    [TestClass]
    public class CothTests
    {
        [TestMethod]
        public void Coth_Compute_Variable_ReturnsValue()
        {
            var expr = new Coth(new Variable("x"));
            var values = new Dictionary<string, double> { ["x"] = 1.0 };
            double actual = expr.Compute(values);
            Assert.AreEqual(1.313, actual, 1e-3);
        }

        [TestMethod]
        public void Coth_Compute_Zero_ThrowsDivideByZeroException()
        {
            var expr = new Coth(new Constant(0));
            Assert.ThrowsExactly<System.DivideByZeroException>(() =>
                expr.Compute(new Dictionary<string, double>()));
        }

        [TestMethod]
        public void Coth_ToString_ReturnsFormattedString()
        {
            var expr = new Coth(new Variable("x"));
            Assert.AreEqual("Coth(x)", expr.ToString());
        }
    }

    // Тесты для Variable и Constant можно выделить в отдельный класс (или оставить здесь)
    [TestClass]
    public class VariableAndConstantTests
    {
        [TestMethod]
        public void Variable_EmptyName_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new Variable(""));
            Assert.ThrowsExactly<ArgumentException>(() => new Variable("   "));
        }

        [TestMethod]
        public void Variable_Compute_NullDictionary_ThrowsKeyNotFoundException()
        {
            var x = new Variable("x");
            Assert.ThrowsExactly<KeyNotFoundException>(() => x.Compute(null));
        }

        [TestMethod]
        public void Constant_Variables_ReturnsEmpty()
        {
            var c = new Constant(5);
            var vars = c.Variables.ToList();
            Assert.AreEqual(0, vars.Count);
        }
    }

    // Тесты для базового класса Expr
    [TestClass]
    public class ExprTests
    {
        [TestMethod]
        public void Expr_Operators_CreateCorrectExpressions()
        {
            var x = new Variable("x");
            var y = new Variable("y");

            Expr exprSub = x - y;
            Assert.IsInstanceOfType(exprSub, typeof(Subtract));

            Expr exprMul = x * y;
            Assert.IsInstanceOfType(exprMul, typeof(Multiply));

            Expr exprDiv = x / y;
            Assert.IsInstanceOfType(exprDiv, typeof(Divide));

            Expr exprNeg = -x;
            Assert.IsInstanceOfType(exprNeg, typeof(Negate));
        }

        [TestMethod]
        public void Expr_StaticMethods_CreateFunctions()
        {
            var x = new Variable("x");
            Assert.IsInstanceOfType(Expr.Sqrt(x), typeof(Sqrt));
            Assert.IsInstanceOfType(Expr.Sinh(x), typeof(Sinh));
            Assert.IsInstanceOfType(Expr.Cosh(x), typeof(Cosh));
            Assert.IsInstanceOfType(Expr.Tanh(x), typeof(Tanh));
            Assert.IsInstanceOfType(Expr.Coth(x), typeof(Coth));
        }

    }

    public class TestExpr : Expr
    {
        public override IEnumerable<string> Variables => Enumerable.Empty<string>();
        public override bool IsConstant => true;
        public override bool IsPolynomial => true;
        public override int PolynomialDegree => 0;
        public override double Compute(IReadOnlyDictionary<string, double> variableValues) => 0;

        public override string ToString() => "TestExpr";
        public int TestGetPrecedence() => Precedence;
        public string TestBaseToFormattedString(int parentPrecedence, bool isRightOperand)
            => base.ToFormattedString(parentPrecedence, isRightOperand);
    }
    [TestClass]
    public class ExprInternalMethodsTests
    {
        [TestMethod]
        public void Precedence_BaseClassReturnsZero()
        {
            var testExpr = new TestExpr();
            Assert.AreEqual(0, testExpr.TestGetPrecedence());
        }

        [TestMethod]
        public void ToFormattedString_BaseImplementation_ReturnsToStringResult()
        {
            var testExpr = new TestExpr();

            // Базовая реализация ToFormattedString должна возвращать результат ToString()
            string result = testExpr.TestBaseToFormattedString(0, false);

            Assert.AreEqual("TestExpr", result);
        }

        [TestMethod]
        public void ToFormattedString_WithDifferentParameters_CallsBaseCorrectly()
        {
            var testExpr = new TestExpr();

            // Проверяем, что метод вызывается с разными параметрами
            string result1 = testExpr.TestBaseToFormattedString(0, false);
            string result2 = testExpr.TestBaseToFormattedString(5, true);
            string result3 = testExpr.TestBaseToFormattedString(10, false);

            // Все должны возвращать одно и то же (результат ToString())
            Assert.AreEqual("TestExpr", result1);
            Assert.AreEqual("TestExpr", result2);
            Assert.AreEqual("TestExpr", result3);
        }
    }
}
    
