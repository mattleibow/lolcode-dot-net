using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace notdot.LOLCode
{
    internal class CodePragma
    {
        public string file;
        public int line;
        public int column;

        public CodePragma(string file, int line, int column)
        {
            this.file = file;
            this.line = line;
            this.column = column;
        }
    }

    internal abstract class CodeObject
    {
        public CodePragma location;

        public CodeObject(CodePragma loc)
        {
            this.location = loc;
        }
    }

    internal class Program : CodeObject
    {
        public List<Statement> statements = new List<Statement>();

        public void Emit(ILGenerator gen)
        {
            foreach (Statement stat in statements)
                stat.Emit(this, gen);
        }

        public Program(CodePragma loc) : base(loc) { }
    }

    internal abstract class Statement : CodeObject
    {
        public abstract void Emit(Program prog, ILGenerator gen);

        public Statement(CodePragma loc) : base(loc) { }
    }

    internal abstract class Expression : Statement {
        public abstract Type EvaluationType { get; }

        public Expression(CodePragma loc) : base(loc) { }
    }

    internal abstract class LValue : CodeObject
    {
        public abstract void EmitGet(Program prog, ILGenerator gen);
        public abstract void EmitSet(Program prog, ILGenerator gen);

        public LValue(CodePragma loc) : base(loc) { }
    }

    internal class VariableLValue : LValue
    {
        public string name;

        public VariableLValue(CodePragma loc) : base(loc) { }
        public VariableLValue(CodePragma loc, string name) : base(loc) { this.name = name; }
    }

    internal class ArrayIndexLValue : LValue
    {
        public LValue lval;
        public Expression index;

        public ArrayIndexLValue(CodePragma loc) : base(loc) { }
    }

    internal class LValueExpression : Expression
    {
        public LValue lval;

        public override Type EvaluationType
        {
            get { return typeof(void); }
        }

        public LValueExpression(CodePragma loc) : base(loc) { }
        public LValueExpression(CodePragma loc, LValue lv) : base(loc) { lval = lv; }
    }

    internal class AssignmentStatement : Statement
    {
        public LValue lval;
        public Expression rval;

        public AssignmentStatement(CodePragma loc) : base(loc) { }
    }

    internal class VariableDeclarationStatement : Statement
    {
        public string name;

        public VariableDeclarationStatement(CodePragma loc) : base(loc) { }
    }

    internal class LoopStatement : Statement
    {
        public string name;
        public List<Statement> statements = new List<Statement>();

        public LoopStatement(CodePragma loc) : base(loc) { }
    }

    internal class BinaryOpStatement : Statement
    {
        public LValue lval;
        public OpCode op;
        public Expression amount;

        public BinaryOpStatement(CodePragma loc) : base(loc) { }
    }

    internal class PrimitiveExpression : Expression
    {
        public object value;

        public override Type  EvaluationType
        {
	        get { return value.GetType(); }
        }

        public PrimitiveExpression(CodePragma loc) : base(loc) { }
        public PrimitiveExpression(CodePragma loc, int val) : base(loc) { value = val; }
        public PrimitiveExpression(CodePragma loc, string val) : base(loc) { value = val; }
    }

    internal class ConditionalStatement : Statement
    {
        public Expression condition;
        public List<Statement> trueStatements = new List<Statement>();
        public List<Statement> falseStatements = new List<Statement>();

        public ConditionalStatement(CodePragma loc) : base(loc) { }
    }

    internal class FuncCallStatement : Statement
    {
        public string name;
        public List<Expression> arguments = new List<Expression>();

        public FuncCallStatement(CodePragma loc) : base(loc) { }    
    }

    internal class IntegerBinaryExpression : Expression {
        public Expression left;
        public Expression right;
        public OpCode op;
        public bool negate = false;

        public override Type EvaluationType
        {
	        get { return typeof(void); }
        }

        public IntegerBinaryExpression(CodePragma loc) : base(loc) { }    
    }

    internal enum IOAmount
    {
        Letter,
        Word,
        Line
    }

    internal class InputStatement : Statement
    {
        public Expression source = null;
        public IOAmount amount = IOAmount.Line;
        public LValue dest;

        public InputStatement(CodePragma loc) : base(loc) { }
    }

    internal class BreakStatement : Statement
    {
        public BreakStatement(CodePragma loc) : base(loc) { }
    }
}