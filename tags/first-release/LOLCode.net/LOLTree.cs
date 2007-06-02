using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics.SymbolStore;
using notdot.LOLCode.stdlol;
using System.Reflection;
using System.IO;

namespace notdot.LOLCode
{
    internal class CodePragma
    {
        public ISymbolDocumentWriter doc;
        public int startLine;
        public int startColumn;
        public int endLine;
        public int endColumn;

        public CodePragma(ISymbolDocumentWriter doc, int line, int column)
        {
            this.doc = doc;
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
    }

    internal class Program
    {
        public List<Statement> statements = new List<Statement>();
        private List<Dictionary<string, LocalBuilder>> locals = new List<Dictionary<string, LocalBuilder>>();
        public List<Label> startLabels = new List<Label>();
        public List<Label> endLabels = new List<Label>();

        public MethodInfo Emit(ModuleBuilder mb)
        {
            TypeBuilder cls = mb.DefineType("Program");
            MethodBuilder main = cls.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { });
            ILGenerator gen = main.GetILGenerator();

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

        public static void CastObject(Type t, ILGenerator gen)
        {
            if (t == typeof(Dictionary<object, object>))
            {
                gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToDict"), null);
            }
            else if (t == typeof(int))
            {
                gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToInt"), null);
            }
            else if (t == typeof(string))
            {
                gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToString", BindingFlags.Public | BindingFlags.Static), null);
            }
            else if(t != typeof(object))
            {
                throw new ArgumentException("Type must be Dictionary<object,object>, int, or string");
            }
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
            gen.Emit(OpCodes.Ldloc, local);
            Program.CastObject(t, gen);
            if (t == typeof(Dictionary<object, object>))
            {
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Stloc, local);
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

                //Cast it to the appropriate type
                Program.CastObject(t, gen);
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

        public AssignmentStatement(CodePragma loc) : base(loc) { }
    }

    internal class VariableDeclarationStatement : Statement
    {
        public string name;

        public override void Emit(Program prog, ILGenerator gen)
        {
            prog.CreateLocal(name, gen);
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

        public PrimitiveExpression(CodePragma loc) : base(loc) { }
        public PrimitiveExpression(CodePragma loc, int val) : base(loc) { value = val; }
        public PrimitiveExpression(CodePragma loc, string val) : base(loc) { value = val; }
    }

    internal class ConditionalStatement : Statement
    {
        public Expression condition;
        public List<Statement> trueStatements = new List<Statement>();
        public List<Statement> falseStatements = new List<Statement>();

        public override void Emit(Program prog, ILGenerator gen)
        {
            Label ifFalse = gen.DefineLabel();
            Label end = gen.DefineLabel();

            //Condition
            location.MarkSequencePoint(gen);
            condition.Emit(prog, typeof(int), gen);
            gen.Emit(OpCodes.Brfalse, ifFalse);

            //True statements
            prog.BeginScope(gen);
            foreach (Statement stmt in trueStatements)
                stmt.Emit(prog, gen);
            gen.Emit(OpCodes.Br, end);
            prog.EndScope(gen);

            //False statements
            prog.BeginScope(gen);
            gen.MarkLabel(ifFalse);
            foreach (Statement stmt in falseStatements)
                stmt.Emit(prog, gen);
            prog.EndScope(gen);

            //End of conditional
            gen.MarkLabel(end);
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

        public InputStatement(CodePragma loc) : base(loc) { }
    }

    internal class BreakStatement : Statement
    {
        public override void Emit(Program prog, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            gen.Emit(OpCodes.Br, prog.endLabels[prog.endLabels.Count - 1]);
        }

        public BreakStatement(CodePragma loc) : base(loc) { }
    }
}