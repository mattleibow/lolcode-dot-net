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

        public abstract void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen);
    }

    internal abstract class Statement : CodeObject
    {
        public abstract void Emit(LOLMethod lm, ILGenerator gen);

        public Statement(CodePragma loc) : base(loc) { }
    }

    internal abstract class BreakableStatement : Statement
    {
        public abstract string Name { get; }
        public abstract Label? BreakLabel { get; }
        public abstract Label? ContinueLabel { get; }

        public BreakableStatement(CodePragma loc) : base(loc) { }
    }

    internal abstract class Expression : Statement {
        public abstract Type EvaluationType { get; }

        public abstract void Emit(LOLMethod lm, Type t, ILGenerator gen);

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            this.Emit(lm, typeof(object), gen);
        }

        public Expression(CodePragma loc) : base(loc) { }
    }

    internal abstract class LValue : CodeObject
    {
        public abstract void EmitGet(LOLMethod lm, Type t, ILGenerator gen);
        public abstract void StartSet(LOLMethod lm, Type t, ILGenerator gen);
        public abstract void EndSet(LOLMethod lm, Type t, ILGenerator gen);

        public LValue(CodePragma loc) : base(loc) { }
    }

    internal class BlockStatement : Statement
    {
        public List<Statement> statements = new List<Statement>();

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            foreach (Statement stat in statements)
                stat.Emit(lm, gen);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            foreach (Statement stat in statements)
                stat.Process(lm, errors, gen);
        }

        public BlockStatement(CodePragma loc) : base(loc) { }
    }

    internal class VariableLValue : LValue
    {
        public string name;

        public override void EmitGet(LOLMethod lm, Type t, ILGenerator gen)
        {
            LocalBuilder local = lm.GetLocal(name);
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

        public override void StartSet(LOLMethod lm, Type t, ILGenerator gen)
        {
            //Nothing to do
        }

        public override void  EndSet(LOLMethod lm, Type t, ILGenerator gen)
        {
            LOLProgram.WrapObject(t, gen);

            //Store it
            LocalBuilder local = lm.GetLocal(name);
            gen.Emit(OpCodes.Stloc, local);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
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

        public override void EmitGet(LOLMethod lm, Type t, ILGenerator gen)
        {
            //Get the object we're fetching from
            lval.EmitGet(lm, typeof(Dictionary<object,object>), gen);

            //Calculate the index
            index.Emit(lm, typeof(object), gen);

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

        public override void StartSet(LOLMethod lm, Type t, ILGenerator gen)
        {
            //Get the object we're setting to
            lval.EmitGet(lm, typeof(Dictionary<object,object>), gen);

            //Calculate the index
            index.Emit(lm, typeof(object), gen);
        }

        public override void  EndSet(LOLMethod lm, Type t, ILGenerator gen)
        {
            LOLProgram.WrapObject(t, gen);

            //Set
            gen.EmitCall(OpCodes.Callvirt, typeof(Dictionary<object, object>).GetProperty("Item", new Type[] { typeof(object) }).GetSetMethod(), null);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            lval.Process(lm, errors, gen);
            index.Process(lm, errors, gen);
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

        public override void Emit(LOLMethod lm, Type t, ILGenerator gen)
        {
            lval.EmitGet(lm, t, gen);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            lval.Process(lm, errors, gen);
        }

        public LValueExpression(CodePragma loc) : base(loc) { }
        public LValueExpression(CodePragma loc, LValue lv) : base(loc) { lval = lv; }
    }

    internal class AssignmentStatement : Statement
    {
        public LValue lval;
        public Expression rval;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            lval.StartSet(lm, rval.EvaluationType, gen);
            rval.Emit(lm, rval.EvaluationType, gen);
            lval.EndSet(lm, rval.EvaluationType, gen);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            lval.Process(lm, errors, gen);
            rval.Process(lm, errors, gen);
        }

        public AssignmentStatement(CodePragma loc) : base(loc) { }
    }

    internal class VariableDeclarationStatement : Statement
    {
        public string name;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            lm.CreateLocal(name, gen);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            return;
        }

        public VariableDeclarationStatement(CodePragma loc) : base(loc) { }
    }

    internal class LoopStatement : BreakableStatement
    {
        public string name = null;
        public Statement statements;
        private Label m_breakLabel;
        private Label m_continueLabel;

        public override Label? BreakLabel
        {
            get { return m_breakLabel; }
        }

        public override Label? ContinueLabel
        {
            get { return m_continueLabel; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            lm.breakables.Add(this);
            gen.MarkLabel(m_continueLabel);

            lm.BeginScope(gen);
            statements.Emit(lm, gen);
            lm.EndScope(gen);

            gen.Emit(OpCodes.Br, m_continueLabel);

            gen.MarkLabel(m_breakLabel);

            lm.breakables.RemoveAt(lm.breakables.Count - 1);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            m_breakLabel = gen.DefineLabel();
            m_continueLabel = gen.DefineLabel();

            lm.breakables.Add(this);
            statements.Process(lm, errors, gen);
            lm.breakables.RemoveAt(lm.breakables.Count - 1);
        }

        public LoopStatement(CodePragma loc) : base(loc) { }
    }

    internal class BinaryOpStatement : Statement
    {
        public LValue lval;
        public OpCode op;
        public Expression amount;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);
            lval.StartSet(lm, typeof(int), gen);
            lval.EmitGet(lm, typeof(int), gen);
            amount.Emit(lm, typeof(int), gen);
            gen.Emit(op);
            lval.EndSet(lm, typeof(int), gen);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            lval.Process(lm, errors, gen);
            amount.Process(lm, errors, gen);

            if (amount.EvaluationType != typeof(int) && amount.EvaluationType != typeof(object))
            {
                errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, string.Format("Expression is not valid on operand of type {0}", amount.EvaluationType.Name)));
            }
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

        public override void Emit(LOLMethod lm, Type t, ILGenerator gen)
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

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            if(value.GetType() != typeof(int) && value.GetType() != typeof(string))
                //We throw an exception here because this would indicate an issue with the compiler, not with the code being compiled.
                throw new InvalidOperationException("PrimitiveExpression values must be int or string.");

            return;
        }

        public PrimitiveExpression(CodePragma loc) : base(loc) { }
        public PrimitiveExpression(CodePragma loc, object val) : base(loc) { value = val; }
    }

    internal class ConditionalStatement : Statement
    {
        public Expression condition;
        public Statement trueStatements;
        public Statement falseStatements;
        public Label ifFalse;
        public Label statementEnd;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            //Condition
            location.MarkSequencePoint(gen);
            condition.Emit(lm, condition.EvaluationType, gen);

            if (!(condition is ComparisonExpression))
                //If the condition is a comparisonexpression, it emits the branch instruction
                gen.Emit(OpCodes.Brfalse, ifFalse);

            //True statements
            lm.BeginScope(gen);
            trueStatements.Emit(lm, gen);
            if(falseStatements is BlockStatement &&  ((BlockStatement)falseStatements).statements.Count > 0)
                gen.Emit(OpCodes.Br, statementEnd);
            lm.EndScope(gen);

            //False statements
            gen.MarkLabel(ifFalse);
            if (falseStatements is BlockStatement &&  ((BlockStatement)falseStatements).statements.Count > 0)
            {
                lm.BeginScope(gen);
                falseStatements.Emit(lm, gen);
                lm.EndScope(gen);
            }

            //End of conditional
            gen.MarkLabel(statementEnd);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            ifFalse = gen.DefineLabel();
            statementEnd = gen.DefineLabel();

            //If there are false statements but no true statements, invert the comparison and the branches
            if (trueStatements is BlockStatement && ((BlockStatement)trueStatements).statements.Count == 0)
            {
                Statement temp = trueStatements;
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

            condition.Process(lm, errors, gen);
            trueStatements.Process(lm, errors, gen);
            if(falseStatements != null)
                falseStatements.Process(lm, errors, gen);

            if (condition is ComparisonExpression)
                (condition as ComparisonExpression).ifFalse = ifFalse;
        }

        public ConditionalStatement(CodePragma loc) : base(loc) { }
    }

    /// <summary>
    /// A switch statement
    /// </summary>
    /// <remarks>
    /// Switch statements are fairly complex to emit in assembly. The general structure of the statement is like this:
    ///   Type check (jumps to either string or integer jump table)
    ///   String jump table
    ///   Jump to default case
    ///   Integer jump table
    ///   Jump to default case
    ///   Statement list
    ///   Default case
    ///   End of switch
    /// If the switch statement only has one type (string or int), the type check and the jump table of the type not
    /// present will be omitted.
    /// </remarks>
    internal class SwitchStatement : BreakableStatement
    {
        public class Case : IComparable<Case>
        {
            public object name;
            public Statement statement;
            public Label label;

            public Case(object name, Statement stat)
            {
                this.name = name;
                this.statement = stat;
            }

            public int CompareTo(Case other)
            {
                if (name is int)
                {
                    return ((int)name).CompareTo((int)other.name);
                }
                else
                {
                    return (name as string).CompareTo(other.name as string);
                }
            }
        }

        public Expression control;
        public List<Case> cases = new List<Case>();
        public Statement defaultCase = null;

        private Case[] sortedCases = null;
        private Label m_breakLabel;
        private Label defaultLabel;

        public override string Name
        {
            get { return null; }
        }

        public override Label? BreakLabel
        {
            get { return m_breakLabel; }
        }

        public override Label? ContinueLabel
        {
            get { return null; }
        }

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            lm.breakables.Add(this);
            lm.BeginScope(gen);
            location.MarkSequencePoint(gen);

            if (cases[0].name is int)
            {
                //Switch is integer
                control.Emit(lm, typeof(int), gen);
                EmitIntegerSwitch(lm, gen);
            }
            else if (cases[0].name is string)
            {
                //Switch is string
                control.Emit(lm, typeof(string), gen);
                EmitStringSwitch(lm, gen);
            }

            gen.Emit(OpCodes.Br, defaultLabel);

            //Output code for all the cases
            foreach (Case c in cases)
            {
                gen.MarkLabel(c.label);
                c.statement.Emit(lm, gen);
            }

            //Default case
            gen.MarkLabel(defaultLabel);
            defaultCase.Emit(lm, gen);

            //End of statement
            gen.MarkLabel(m_breakLabel);

            lm.EndScope(gen);
            lm.breakables.RemoveAt(lm.breakables.Count - 1);
        }

        private delegate void SwitchComparisonDelegate(ILGenerator gen, Case c);

        private void EmitIntegerSwitch(LOLMethod lm, ILGenerator gen)
        {
            if (sortedCases.Length * 2 >= (((int)sortedCases[sortedCases.Length - 1].name) - ((int)sortedCases[0].name)))
            {
                //Switch is compact, emit a jump table
                EmitIntegerJumpTable(lm, gen);
            }
            else
            {
                //Switch is not compact - emit a binary tree
                LocalBuilder loc = lm.GetTempLocal(gen, typeof(int));
                gen.Emit(OpCodes.Stloc, loc);
                EmitSwitchTree(lm, gen, 0, sortedCases.Length, loc, delegate(ILGenerator ig, Case c)
                {
                    ig.Emit(OpCodes.Ldc_I4, (int)c.name);
                    ig.Emit(OpCodes.Sub);
                });
                lm.ReleaseTempLocal(loc);
            }
        }

        private void EmitIntegerJumpTable(LOLMethod lm, ILGenerator gen)
        {
            int len = ((int)sortedCases[sortedCases.Length - 1].name) - ((int)sortedCases[0].name) + 1;
            int offset = (int)sortedCases[0].name;
            if(offset < len / 2) {
                len += offset;
                offset = 0;
            }

            Label[] jumpTable = new Label[len];
            int casePtr = 0;

            if (offset > 0)
            {
                gen.Emit(OpCodes.Ldc_I4, offset);
                gen.Emit(OpCodes.Sub);
            }

            for (int i = 0; i < len; i++)
            {
                if (((int)sortedCases[casePtr].name) == i + offset)
                {
                    jumpTable[i] = sortedCases[casePtr++].label = gen.DefineLabel();
                }
                else
                {
                    jumpTable[i] = defaultLabel;
                }
            }

            gen.Emit(OpCodes.Switch, jumpTable);
        }

        private void EmitStringSwitch(LOLMethod lm, ILGenerator gen)
        {
            LocalBuilder loc = lm.GetTempLocal(gen, typeof(string));
            gen.Emit(OpCodes.Stloc, loc);
            EmitSwitchTree(lm, gen, 0, sortedCases.Length, loc, delegate(ILGenerator ig, Case c)
            {
                ig.Emit(OpCodes.Ldstr, (string)c.name);
                ig.EmitCall(OpCodes.Call, typeof(string).GetMethod("Compare", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null), null);
            });
            lm.ReleaseTempLocal(loc);
        }

        private void EmitSwitchTree(LOLMethod lm, ILGenerator gen, int off, int len, LocalBuilder loc, SwitchComparisonDelegate compare)
        {
            Label branch;

            int idx = off + len / 2;
            Case c = sortedCases[idx];
            c.label = gen.DefineLabel();

            //Load the variable and compare it with the current case
            gen.Emit(OpCodes.Ldloc, loc);
            compare(gen, c);

            if (len == 1)
            {
                //If we're in a range of one, we can simplify things
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Beq, c.label);
                gen.Emit(OpCodes.Br, defaultLabel);
            }
            else if(idx == off)
            {
                //The less-than case is default, so test that last
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Ldc_I4_0);
                branch = gen.DefineLabel();
                gen.Emit(OpCodes.Bgt, branch);
                gen.Emit(OpCodes.Brfalse, c.label);
                //Not greater and not zero - must be less
                gen.Emit(OpCodes.Br, defaultLabel);

                gen.MarkLabel(branch);
                gen.Emit(OpCodes.Pop);
                EmitSwitchTree(lm, gen, off + 1, len - 1, loc, compare);
            }
            else if (idx == off + len - 1)
            {
                //The greater-than case is default,  so test that last
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Ldc_I4_0);
                branch = gen.DefineLabel();
                gen.Emit(OpCodes.Blt, branch);
                gen.Emit(OpCodes.Brfalse, c.label);
                //Not less and not zero - must be greater
                gen.Emit(OpCodes.Br, defaultLabel);

                gen.MarkLabel(branch);
                gen.Emit(OpCodes.Pop);
                EmitSwitchTree(lm, gen, off, len - 1, loc, compare);
            }
            else
            {
                //Both branches are non-empty
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Ldc_I4_0);
                branch = gen.DefineLabel();
                gen.Emit(OpCodes.Blt, branch);
                gen.Emit(OpCodes.Brfalse, c.label);
                //Not less and not zero - must be greater
                EmitSwitchTree(lm, gen, idx + 1, len - (idx - off) - 1, loc, compare);

                gen.MarkLabel(branch);
                gen.Emit(OpCodes.Pop);
                EmitSwitchTree(lm, gen, off, idx - off, loc, compare);
            }
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            m_breakLabel = gen.DefineLabel();
            if (defaultCase != null)
                defaultLabel = gen.DefineLabel();

            Type t = null;
            foreach (Case c in cases)
            {
                if (c.name.GetType() != t)
                {
                    if (t != null)
                    {
                        errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "A WTF statement cannot have OMGs with more than one type"));
                        break;
                    }
                    else
                    {
                        t = c.name.GetType();
                    }
                }
            }

            if (t != typeof(int) && t != typeof(string))
            {
                errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "OMG labels must be NUMBARs or YARNs"));
            }

            //Sort the cases
            sortedCases = new Case[cases.Count];
            cases.CopyTo(sortedCases);
            Array.Sort<Case>(sortedCases);

            //Check for duplicates
            for (int i = 1; i < sortedCases.Length; i++)
                if (sortedCases[i - 1].CompareTo(sortedCases[i]) == 0)
                    errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, string.Format("Duplicate OMG label: \"{0}\"", sortedCases[i].name)));

            //Process child statements
            lm.breakables.Add(this);
            control.Process(lm, errors, gen);
            foreach (Case c in cases)
                c.statement.Process(lm, errors, gen);
            defaultCase.Process(lm, errors, gen);
            lm.breakables.RemoveAt(lm.breakables.Count - 1);
        }

        public SwitchStatement(CodePragma loc) : base(loc) { }
    }

    internal class QuitStatement : Statement {
        public Expression code;
        public Expression message = null;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            if (message != null)
            {
                //Get the error stream
                gen.EmitCall(OpCodes.Call, typeof(Console).GetProperty("Error", BindingFlags.Public | BindingFlags.Static).GetGetMethod(), null);

                //Get the message
                message.Emit(lm, typeof(string), gen);

                //Write the message
                gen.EmitCall(OpCodes.Callvirt, typeof(TextWriter).GetMethod("WriteLine", new Type[] { typeof(string) }), null);
            }

            code.Emit(lm, gen);
            gen.Emit(OpCodes.Ret);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            code.Process(lm, errors, gen);
            if (message != null)
                message.Process(lm, errors, gen);

            if(code.EvaluationType != typeof(int) && code.EvaluationType != typeof(object))
                errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "First argument to BYES or DIAF must be an integer"));
            if(message != null && message.EvaluationType != typeof(string) && message.EvaluationType != typeof(object))
                errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "Second argument to BYES or DIAF must be a string"));
        }

        public QuitStatement(CodePragma loc) : base(loc) { }
    }

    internal class PrintStatement : Statement {
        public bool stderr = false;
        public Expression message;
        public bool newline = true;

        public override void Emit(LOLMethod lm, ILGenerator gen)
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
            message.Emit(lm, typeof(object), gen);

            //Indicate if it requires a newline or not
            if (newline)
            {
                gen.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                gen.Emit(OpCodes.Ldc_I4_0);
            }

            gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("PrintObject"), null);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            message.Process(lm, errors, gen);
        }

        public PrintStatement(CodePragma loc) : base(loc) { }
    }

    internal class IntegerBinaryExpression : Expression {
        public Expression left;
        public Expression right;
        public OpCode op;
        public bool negate = false;

        public override void Emit(LOLMethod lm, Type t, ILGenerator gen)
        {
            if(typeof(int) != t && typeof(object) != t)
                throw new ArgumentException("IntegerBinaryExpressions can only evaluate to type int");

            left.Emit(lm, typeof(int), gen);
            right.Emit(lm, typeof(int), gen);
            gen.Emit(op);

            if (typeof(object) == t)
                gen.Emit(OpCodes.Box, typeof(int));
        }

        public override Type EvaluationType
        {
	        get { return typeof(int); }
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            left.Process(lm, errors, gen);
            right.Process(lm, errors, gen);

            if ((left.EvaluationType != typeof(int) && left.EvaluationType != typeof(object)) || (right.EvaluationType != typeof(int) && right.EvaluationType != typeof(object)))
                errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, string.Format("Expression is not valid on operands of type {0} and {1}.", left.EvaluationType.Name, right.EvaluationType.Name)));
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

        public override void Emit(LOLMethod lm, Type t, ILGenerator gen)
        {
            left.Emit(lm, comparisonType, gen);
            right.Emit(lm, comparisonType, gen);

            if (comparisonType == typeof(object))
            {
                gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("CompareObjects"), null);
            }
            else if (comparisonType == typeof(string))
            {
                gen.EmitCall(OpCodes.Call, typeof(string).GetMethod("Compare", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null), null);
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
                            gen.Emit(OpCodes.Ldc_I4_M1);
                            gen.Emit(OpCodes.Cgt);
                            break;
                        case ComparisonOperator.Equal:
                            gen.Emit(OpCodes.Ldc_I4_0);
                            gen.Emit(OpCodes.Ceq);
                            break;
                        case ComparisonOperator.NotEqual:
                            gen.Emit(OpCodes.Ldc_I4_0);
                            gen.Emit(OpCodes.Ceq);
                            gen.Emit(OpCodes.Not);
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

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            left.Process(lm, errors, gen);
            right.Process(lm, errors, gen);

            if (left.EvaluationType == right.EvaluationType)
            {
                comparisonType = left.EvaluationType;
            }
            else if (left.EvaluationType == typeof(object))
            {
                comparisonType = right.EvaluationType;
            } 
            else if(right.EvaluationType == typeof(object)) 
            {
                comparisonType = right.EvaluationType;
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

        public override void Emit(LOLMethod lm, Type t, ILGenerator gen)
        {
            gen.Emit(OpCodes.Not);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
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

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            dest.StartSet(lm, typeof(string), gen);

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

            dest.EndSet(lm, typeof(string), gen);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            dest.Process(lm, errors, gen);
        }

        public InputStatement(CodePragma loc) : base(loc) { }
    }

    internal class BreakStatement : Statement
    {
        public string label = null;
        private int breakIdx = -1;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            gen.Emit(OpCodes.Br, lm.breakables[breakIdx].BreakLabel.Value);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            breakIdx = lm.breakables.Count - 1;
            if (label == null)
            {
                while (breakIdx >= 0 && !lm.breakables[breakIdx].BreakLabel.HasValue)
                    breakIdx--;
            }
            else
            {
                while (breakIdx >= 0 && (lm.breakables[breakIdx].Name != label || !lm.breakables[breakIdx].BreakLabel.HasValue))
                    breakIdx--;
            }

            if (breakIdx < 0)
            {
                if (label == null)
                {
                    errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "ENUF encountered, but nothing to break out of!"));
                }
                else
                {
                    errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, string.Format("Named ENUF \"{0}\" encountered, but nothing by that name exists to break out of!", label)));
                }
            }
        }

        public BreakStatement(CodePragma loc) : base(loc) { }
    }

    internal class ContinueStatement : Statement
    {
        public string label = null;
        private int breakIdx = -1;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            gen.Emit(OpCodes.Br, lm.breakables[breakIdx].ContinueLabel.Value);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            breakIdx = lm.breakables.Count - 1;
            if(label == null) {
                while(breakIdx >= 0 && !lm.breakables[breakIdx].ContinueLabel.HasValue)
                    breakIdx--;
            } else {
                while (breakIdx >= 0 && (lm.breakables[breakIdx].Name != label || !lm.breakables[breakIdx].ContinueLabel.HasValue))
                    breakIdx--;
            }

            if (breakIdx < 0)
            {
                if (label == null)
                {
                    errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "MOAR encountered, but nothing to continue!"));
                }
                else
                {
                    errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, string.Format("Named MOAR \"{0}\" encountered, but nothing by that name exists to continue!", label)));
                }
            }
        }

        public ContinueStatement(CodePragma loc) : base(loc) { }
    }
}