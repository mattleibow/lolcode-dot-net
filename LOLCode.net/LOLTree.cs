using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics.SymbolStore;
using notdot.LOLCode.stdlol;
using System.Reflection;
using System.IO;
using System.CodeDom.Compiler;

namespace notdot.LOLCode
{
    internal class CodePragma
    {
        public ISymbolDocumentWriter doc;
        public string filename;
        public int startLine;
        public int startColumn;
        public int endLine;
        public int endColumn;

        public CodePragma(ISymbolDocumentWriter doc, string filename, int line, int column)
        {
            this.doc = doc;
            this.filename = filename;
            this.startLine = this.endLine = line;
            this.startColumn = this.endColumn = column;
        }

        internal void MarkSequencePoint(ILGenerator gen)
        {
            gen.MarkSequencePoint(doc, startLine, startColumn, endLine, endColumn);
        }
    }

    internal abstract class CodeObject
    {
        public CodePragma location;

        public CodeObject(CodePragma loc)
        {
            this.location = loc;
        }

        public abstract void Process(CompilerErrorCollection errors, ILGenerator gen);
    }

    internal class Program
    {
        public List<Statement> statements = new List<Statement>();
        private List<Dictionary<string, LocalBuilder>> locals = new List<Dictionary<string, LocalBuilder>>();
        public List<Label> startLabels = new List<Label>();
        public List<Label> endLabels = new List<Label>();

        public MethodInfo Emit(CompilerErrorCollection errors, ModuleBuilder mb)
        {
            TypeBuilder cls = mb.DefineType("Program");
            MethodBuilder main = cls.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { });
            ILGenerator gen = main.GetILGenerator();

            foreach (Statement stat in statements)
                stat.Process(errors, gen);

            //locals.Add(new Dictionary<string, LocalBuilder>());
            BeginScope(gen);

            foreach (Statement stat in statements)
                stat.Emit(this, gen);

            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);

            EndScope(gen);

            Type t = cls.CreateType();
            return t.GetMethod("Main");
        }

        public void BeginScope(ILGenerator gen)
        {
            gen.BeginScope();
            locals.Add(new Dictionary<string, LocalBuilder>());
        }

        public void EndScope(ILGenerator gen)
        {
            locals.RemoveAt(locals.Count - 1);
            gen.EndScope();
        }

        public LocalBuilder CreateLocal(string name, ILGenerator gen)
        {
            LocalBuilder ret = gen.DeclareLocal(typeof(object));
            ret.SetLocalSymInfo(name);
            locals[locals.Count - 1].Add(name, ret);

            return ret;
        }

        public LocalBuilder GetLocal(string name)
        {
            LocalBuilder ret;

            for (int i = locals.Count - 1; i >= 0; i--)
                if (locals[i].TryGetValue(name, out ret))
                    return ret;

            throw new KeyNotFoundException();
        }

        public static void WrapObject(Type t, ILGenerator gen)
        {
            if (t == typeof(int))
            {
                //Box the int
                gen.Emit(OpCodes.Box, typeof(int));
            }
            else if (t == typeof(Dictionary<object, object>))
            {
                //Clone the array
                gen.Emit(OpCodes.Newobj, typeof(Dictionary<object, object>).GetConstructor(new Type[] { typeof(Dictionary<object, object>) }));
            }
        }
    }

    internal abstract class Statement : CodeObject
    {
        public abstract void Emit(Program prog, ILGenerator gen);

        public Statement(CodePragma loc) : base(loc) { }
    }

    internal abstract class Expression : Statement {
        public abstract Type EvaluationType { get; }

        public abstract void Emit(Program prog, Type t, ILGenerator gen);

        public override void Emit(Program prog, ILGenerator gen)
        {
            this.Emit(prog, typeof(object), gen);
        }

        public Expression(CodePragma loc) : base(loc) { }
    }

    internal abstract class LValue : CodeObject
    {
        public abstract void EmitGet(Program prog, Type t, ILGenerator gen);
        public abstract void StartSet(Program prog, Type t, ILGenerator gen);
        public abstract void EndSet(Program prog, Type t, ILGenerator gen);

        public LValue(CodePragma loc) : base(loc) { }
    }

    internal class VariableLValue : LValue
    {
        public string name;

        public override void EmitGet(Program prog, Type t, ILGenerator gen)
        {
            LocalBuilder local = prog.GetLocal(name);
            if (t == typeof(Dictionary<object, object>))
            {
                gen.Emit(OpCodes.Ldloca, local);
                gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToDict"), null);
            }
            else
            {
                gen.Emit(OpCodes.Ldloc, local);
                if (t == typeof(int))
                {
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToInt"), null);
                }
                else if (t == typeof(string))
                {
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToString", BindingFlags.Public | BindingFlags.Static), null);
                }
                else if (t != typeof(object))
                {
                    throw new ArgumentException("Type must be Dictionary<object,object>, int, or string");
                }
            }
        }

        public override void StartSet(Program prog, Type t, ILGenerator gen)
        {
            //Nothing to do
        }

        public override void  EndSet(Program prog, Type t, ILGenerator gen)
        {
            Program.WrapObject(t, gen);

            //Store it
            LocalBuilder local = prog.GetLocal(name);
            gen.Emit(OpCodes.Stloc, local);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            return;
        }

        public VariableLValue(CodePragma loc) : base(loc) { }
        public VariableLValue(CodePragma loc, string name) : base(loc) { this.name = name; }
    }

    internal class ArrayIndexLValue : LValue
    {
        public LValue lval;
        public Expression index;

        public override void EmitGet(Program prog, Type t, ILGenerator gen)
        {
            //Get the object we're fetching from
            lval.EmitGet(prog, typeof(Dictionary<object,object>), gen);

            //Calculate the index
            index.Emit(prog, typeof(object), gen);

            //Get the value
            if (t == typeof(Dictionary<object, object>))
            {
                gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("GetDict"), null);
            }
            else
            {
                gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("GetObject"), null);
                if (t == typeof(int))
                {
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToInt"), null);
                }
                else if (t == typeof(string))
                {
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToString", BindingFlags.Public | BindingFlags.Static), null);
                }
                else if (t != typeof(object))
                {
                    throw new ArgumentException("Type must be Dictionary<object,object>, int, or string");
                }
            }
        }

        public override void StartSet(Program prog, Type t, ILGenerator gen)
        {
            //Get the object we're setting to
            lval.EmitGet(prog, typeof(Dictionary<object,object>), gen);

            //Calculate the index
            index.Emit(prog, typeof(object), gen);
        }

        public override void  EndSet(Program prog, Type t, ILGenerator gen)
        {
            Program.WrapObject(t, gen);

            //Set
            gen.EmitCall(OpCodes.Callvirt, typeof(Dictionary<object, object>).GetProperty("Item", new Type[] { typeof(object) }).GetSetMethod(), null);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            lval.Process(errors, gen);
            index.Process(errors, gen);
        }

        public ArrayIndexLValue(CodePragma loc) : base(loc) { }
    }

    internal class LValueExpression : Expression
    {
        public LValue lval;

        public override Type EvaluationType
        {
            get { return typeof(object); }
        }

        public override void Emit(Program prog, Type t, ILGenerator gen)
        {
            lval.EmitGet(prog, t, gen);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            lval.Process(errors, gen);
        }

        public LValueExpression(CodePragma loc) : base(loc) { }
        public LValueExpression(CodePragma loc, LValue lv) : base(loc) { lval = lv; }
    }

    internal class AssignmentStatement : Statement
    {
        public LValue lval;
        public Expression rval;

        public override void Emit(Program prog, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            lval.StartSet(prog, rval.EvaluationType, gen);
            rval.Emit(prog, rval.EvaluationType, gen);
            lval.EndSet(prog, rval.EvaluationType, gen);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            lval.Process(errors, gen);
            rval.Process(errors, gen);
        }

        public AssignmentStatement(CodePragma loc) : base(loc) { }
    }

    internal class VariableDeclarationStatement : Statement
    {
        public string name;

        public override void Emit(Program prog, ILGenerator gen)
        {
            prog.CreateLocal(name, gen);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            return;
        }

        public VariableDeclarationStatement(CodePragma loc) : base(loc) { }
    }

    internal class LoopStatement : Statement
    {
        public string name;
        public List<Statement> statements = new List<Statement>();

        public override void Emit(Program prog, ILGenerator gen)
        {
            Label start = gen.DefineLabel();
            gen.MarkLabel(start);
            prog.startLabels.Add(start);

            Label end = gen.DefineLabel();
            prog.endLabels.Add(end);

            prog.BeginScope(gen);

            foreach (Statement stat in statements)
                stat.Emit(prog, gen);

            prog.EndScope(gen);

            gen.Emit(OpCodes.Br, start);

            gen.MarkLabel(end);            

            prog.startLabels.RemoveAt(prog.startLabels.Count - 1);
            prog.endLabels.RemoveAt(prog.endLabels.Count - 1);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            foreach (Statement stat in statements)
                stat.Process(errors, gen);
        }

        public LoopStatement(CodePragma loc) : base(loc) { }
    }

    internal class BinaryOpStatement : Statement
    {
        public LValue lval;
        public OpCode op;
        public Expression amount;

        public override void Emit(Program prog, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);
            lval.StartSet(prog, typeof(int), gen);
            lval.EmitGet(prog, typeof(int), gen);
            amount.Emit(prog, typeof(int), gen);
            gen.Emit(op);
            lval.EndSet(prog, typeof(int), gen);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            lval.Process(errors, gen);
            amount.Process(errors, gen);
        }

        public BinaryOpStatement(CodePragma loc) : base(loc) { }
    }

    internal class PrimitiveExpression : Expression
    {
        public object value;

        public override Type  EvaluationType
        {
	        get { return value.GetType(); }
        }

        public override void Emit(Program prog, Type t, ILGenerator gen)
        {
            if (value is int && t == typeof(string))
                value = ((int)value).ToString();

            if (value.GetType() != t && t != typeof(object))
                throw new ArgumentException(string.Format("{0} encountered, {1} expected.", value.GetType().Name, t.Name));

            if (value is int)
            {
                gen.Emit(OpCodes.Ldc_I4, (int)value);
                if (t == typeof(object))
                    gen.Emit(OpCodes.Box, typeof(int));
            }
            else if (value is string)
            {
                gen.Emit(OpCodes.Ldstr, (string)value);
            }
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            return;
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
        public Label ifFalse;
        public Label statementEnd;

        public override void Emit(Program prog, ILGenerator gen)
        {
            //Condition
            location.MarkSequencePoint(gen);
            condition.Emit(prog, typeof(int), gen);
            if (!(condition is ComparisonExpression))
                //If the condition is a comparisonexpression, it emits the branch instruction
                gen.Emit(OpCodes.Brfalse, ifFalse);

            //True statements
            prog.BeginScope(gen);
            foreach (Statement stmt in trueStatements)
                stmt.Emit(prog, gen);
            if(falseStatements.Count > 0)
                gen.Emit(OpCodes.Br, statementEnd);
            prog.EndScope(gen);

            //False statements
            gen.MarkLabel(ifFalse);
            if (falseStatements.Count > 0)
            {
                prog.BeginScope(gen);
                foreach (Statement stmt in falseStatements)
                    stmt.Emit(prog, gen);
                prog.EndScope(gen);
            }

            //End of conditional
            gen.MarkLabel(statementEnd);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            ifFalse = gen.DefineLabel();
            statementEnd = gen.DefineLabel();

            //If there are false statements but no true statements, invert the comparison and the branches
            if (trueStatements.Count == 0)
            {
                List<Statement> temp = trueStatements;
                trueStatements = falseStatements;
                falseStatements = temp;

                if (condition is ComparisonExpression)
                {
                    (condition as ComparisonExpression).op ^= ComparisonOperator.Not;
                }
                else
                {
                    condition = new NotExpression(condition.location, condition);
                }
            }

            condition.Process(errors, gen);
            foreach (Statement stat in trueStatements)
                stat.Process(errors, gen);
            foreach (Statement stat in falseStatements)
                stat.Process(errors, gen);

            if (condition is ComparisonExpression)
                (condition as ComparisonExpression).ifFalse = ifFalse;
        }

        public ConditionalStatement(CodePragma loc) : base(loc) { }
    }

    internal class QuitStatement : Statement {
        public Expression code;
        public Expression message = null;

        public override void Emit(Program prog, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            if (message != null)
            {
                //Get the error stream
                gen.EmitCall(OpCodes.Call, typeof(Console).GetProperty("Error", BindingFlags.Public | BindingFlags.Static).GetGetMethod(), null);

                //Get the message
                message.Emit(prog, typeof(string), gen);

                //Write the message
                gen.EmitCall(OpCodes.Callvirt, typeof(TextWriter).GetMethod("WriteLine", new Type[] { typeof(string) }), null);
            }

            code.Emit(prog, gen);
            gen.Emit(OpCodes.Ret);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            code.Process(errors, gen);
            if (message != null)
                message.Process(errors, gen);
        }

        public QuitStatement(CodePragma loc) : base(loc) { }
    }

    internal class PrintStatement : Statement {
        public bool stderr = false;
        public Expression message;
        public bool newline = true;

        public override void Emit(Program prog, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            //Get the appropriate stream
            if (stderr)
            {
                gen.EmitCall(OpCodes.Call, typeof(Console).GetProperty("Error", BindingFlags.Public | BindingFlags.Static).GetGetMethod(), null);
            }
            else
            {
                gen.EmitCall(OpCodes.Call, typeof(Console).GetProperty("Out", BindingFlags.Public | BindingFlags.Static).GetGetMethod(), null);
            }

            //Get the message
            message.Emit(prog, typeof(string), gen);

            //Write the message
            if (newline)
            {
                gen.EmitCall(OpCodes.Callvirt, typeof(TextWriter).GetMethod("WriteLine", new Type[] { typeof(string) }), null);
            }
            else
            {
                gen.EmitCall(OpCodes.Callvirt, typeof(TextWriter).GetMethod("Write", new Type[] { typeof(string) }), null);
            }
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            message.Process(errors, gen);
        }

        public PrintStatement(CodePragma loc) : base(loc) { }
    }

    internal class IntegerBinaryExpression : Expression {
        public Expression left;
        public Expression right;
        public OpCode op;
        public bool negate = false;

        public override void Emit(Program prog, Type t, ILGenerator gen)
        {
            if(typeof(int) != t)
                throw new ArgumentException("IntegerBinaryExpressions can only evaluate to type int");

            left.Emit(prog, t, gen);
            right.Emit(prog, t, gen);
            gen.Emit(op);
        }

        public override Type EvaluationType
        {
	        get { return typeof(int); }
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            left.Process(errors, gen);
            right.Process(errors, gen);
        }

        public IntegerBinaryExpression(CodePragma loc) : base(loc) { }    
    }

    [Flags]
    internal enum ComparisonOperator
    {
        None = 0,

        Equal = 1,
        LessThan = 2,
        GreaterThan = 4,
        Not = 8,

        NotEqual = Equal | Not,
        GreaterOrEqual = LessThan | Not,
        LessOrEqual = GreaterThan | Not
    }

    internal class ComparisonExpression : Expression
    {
        public Expression left;
        public Expression right;
        public ComparisonOperator op;
        public Label? ifFalse = null;
        Type comparisonType;

        public override Type EvaluationType
        {
            get { return typeof(int); }
        }

        public override void Emit(Program prog, Type t, ILGenerator gen)
        {
            left.Emit(prog, comparisonType, gen);
            right.Emit(prog, comparisonType, gen);

            if (comparisonType == typeof(object))
            {
                gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("CompareObjects"), null);
            }
            else if (comparisonType == typeof(string))
            {
                gen.EmitCall(OpCodes.Call, typeof(string).GetMethod("Compare", null), null);
            }

            if(comparisonType == typeof(int)) {
                if(ifFalse.HasValue) {
                    //Branching instruction
                    switch(op) {
                        case ComparisonOperator.LessThan:
                            gen.Emit(OpCodes.Bge, ifFalse.Value);
                            break;
                        case ComparisonOperator.LessOrEqual:
                            gen.Emit(OpCodes.Bgt, ifFalse.Value);
                            break;
                        case ComparisonOperator.Equal:
                            gen.Emit(OpCodes.Ceq);
                            gen.Emit(OpCodes.Brfalse, ifFalse.Value);
                            break;
                        case ComparisonOperator.NotEqual:
                            gen.Emit(OpCodes.Beq, ifFalse.Value);
                            break;
                        case ComparisonOperator.GreaterOrEqual:
                            gen.Emit(OpCodes.Blt, ifFalse.Value);
                            break;
                        case ComparisonOperator.GreaterThan:
                            gen.Emit(OpCodes.Ble, ifFalse.Value);
                            break;
                    }
                } else {
                    //Comparison instruction
                    switch(op) {
                        case ComparisonOperator.LessThan:
                        case ComparisonOperator.GreaterOrEqual:
                            gen.Emit(OpCodes.Clt);
                            break;
                        case ComparisonOperator.Equal:
                        case ComparisonOperator.NotEqual:
                            gen.Emit(OpCodes.Ceq);
                            break;
                        case ComparisonOperator.GreaterThan:
                        case ComparisonOperator.LessOrEqual:
                            gen.Emit(OpCodes.Cgt);
                            break;
                    }

                    if((op & ComparisonOperator.Not) != ComparisonOperator.None)
                        gen.Emit(OpCodes.Not);
                }
            } else {
                if(ifFalse.HasValue) {
                    //Branching instruction
                    switch(op) {
                        case ComparisonOperator.LessThan:
                            gen.Emit(OpCodes.Ldc_I4_0);
                            gen.Emit(OpCodes.Bge, ifFalse.Value);
                            break;
                        case ComparisonOperator.LessOrEqual:
                            gen.Emit(OpCodes.Ldc_I4_0);
                            gen.Emit(OpCodes.Bgt, ifFalse.Value);
                            break;
                        case ComparisonOperator.Equal:
                            gen.Emit(OpCodes.Brtrue, ifFalse.Value);
                            break;
                        case ComparisonOperator.NotEqual:
                            gen.Emit(OpCodes.Brfalse, ifFalse.Value);
                            break;
                        case ComparisonOperator.GreaterOrEqual:
                            gen.Emit(OpCodes.Ldc_I4_0);
                            gen.Emit(OpCodes.Blt, ifFalse.Value);
                            break;
                        case ComparisonOperator.GreaterThan:
                            gen.Emit(OpCodes.Ldc_I4_0);
                            gen.Emit(OpCodes.Ble, ifFalse.Value);
                            break;                        
                    }
                } else {
                    //Comparison instruction
                    switch(op) {
                        case ComparisonOperator.LessThan:
                            gen.Emit(OpCodes.Ldc_I4_0);
                            gen.Emit(OpCodes.Clt);
                            break;
                        case ComparisonOperator.GreaterOrEqual:
                            gen.Emit(OpCodes.Ldc_I4, -1);
                            gen.Emit(OpCodes.Cgt);
                            break;
                        case ComparisonOperator.Equal:
                            gen.Emit(OpCodes.Not);
                            break;
                        case ComparisonOperator.NotEqual:
                            break;
                        case ComparisonOperator.GreaterThan:
                            gen.Emit(OpCodes.Ldc_I4_0);
                            gen.Emit(OpCodes.Cgt);
                            break;
                        case ComparisonOperator.LessOrEqual:
                            gen.Emit(OpCodes.Ldc_I4_1);
                            gen.Emit(OpCodes.Clt);
                            break;
                    }
                }
            }
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            left.Process(errors, gen);
            right.Process(errors, gen);

            if (left.EvaluationType == right.EvaluationType)
            {
                comparisonType = left.EvaluationType;
            }
            else if (left.EvaluationType == typeof(object) || right.EvaluationType == typeof(object))
            {
                comparisonType = typeof(object);
            }
            else
            {
                errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "Cannot compare dissimilar types"));
            }
        }

        public ComparisonExpression(CodePragma loc) : base(loc) { }
    }

    internal class NotExpression : Expression
    {
        public Expression exp;

        public override Type EvaluationType
        {
            get { return typeof(int); }
        }

        public override void Emit(Program prog, Type t, ILGenerator gen)
        {
            gen.Emit(OpCodes.Not);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            if (exp.EvaluationType != typeof(int))
                errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "Cannot negate a non-integer expression"));
        }

        public NotExpression(CodePragma loc, Expression e) : base(loc) { exp = e; }
    }

    internal enum IOAmount
    {
        Letter,
        Word,
        Line
    }

    internal class InputStatement : Statement
    {
        public IOAmount amount = IOAmount.Line;
        public LValue dest;

        public override void Emit(Program prog, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            dest.StartSet(prog, typeof(string), gen);

            gen.EmitCall(OpCodes.Call, typeof(Console).GetProperty("In", BindingFlags.Public | BindingFlags.Static).GetGetMethod(), null);

            switch (amount)
            {
                case IOAmount.Letter:
                    gen.EmitCall(OpCodes.Callvirt, typeof(TextReader).GetMethod("Read", new Type[0]), null);
                    gen.EmitCall(OpCodes.Call, typeof(char).GetMethod("ToString", new Type[] { typeof(char) }), null);
                    break;
                case IOAmount.Word:
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ReadWord"), null);
                    break;
                case IOAmount.Line:
                    gen.EmitCall(OpCodes.Callvirt, typeof(TextReader).GetMethod("ReadLine", new Type[0]), null);
                    break;
            }

            dest.EndSet(prog, typeof(string), gen);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            dest.Process(errors, gen);
        }

        public InputStatement(CodePragma loc) : base(loc) { }
    }

    internal class BreakStatement : Statement
    {
        public override void Emit(Program prog, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            gen.Emit(OpCodes.Br, prog.endLabels[prog.endLabels.Count - 1]);
        }

        public override void Process(CompilerErrorCollection errors, ILGenerator gen)
        {
            return;
        }

        public BreakStatement(CodePragma loc) : base(loc) { }
    }
}