using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics.SymbolStore;
using stdlol;
using System.Reflection;
using System.IO;
using System.CodeDom.Compiler;
using System.Text;
using notdot.LOLCode.Parser.v1_2;

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

        public static void EmitCast(ILGenerator gen, Type from, Type  to) {
            if (from == to) {
                return;
            }
            else if (to == typeof(object))
            {
                if (from.IsValueType)
                {
                    gen.Emit(OpCodes.Box, from);
                }
            }
            else if (from == typeof(object))
            {
                if (to == typeof(int))
                {
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToInt", BindingFlags.Public | BindingFlags.Static), null);
                }
                else if (to == typeof(float))
                {
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToFloat", BindingFlags.Public | BindingFlags.Static), null);
                }
                else if (to == typeof(string))
                {
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToString", BindingFlags.Public | BindingFlags.Static), null);
                }
                else if (to == typeof(bool))
                {
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToBool", BindingFlags.Public | BindingFlags.Static), null);
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Unknown cast: From {0} to {1}", from.Name, to.Name));
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("Unknown cast: From {0} to {1}", from.Name, to.Name));
            }
        }
    }

    internal abstract class LValue : Expression
    {
        public abstract void EmitGet(LOLMethod lm, Type t, ILGenerator gen);
        public abstract void StartSet(LOLMethod lm, Type t, ILGenerator gen);
        public abstract void EndSet(LOLMethod lm, Type t, ILGenerator gen);

        public LValue(CodePragma loc) : base(loc) { }

        public override void Emit(LOLMethod lm, Type t, ILGenerator gen)
        {
            EmitGet(lm, t, gen);
        }
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
        public VariableRef var;

        public override Type EvaluationType
        {
            get { return var.Type; }
        }

        public override void EmitGet(LOLMethod lm, Type t, ILGenerator gen)
        {
            if (var is LocalRef)
            {
                if (t == typeof(Dictionary<object,object>))
                {
                    gen.Emit(OpCodes.Ldloca, (var as LocalRef).Local);
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToDict"), null);
                }
                else
                {
                    gen.Emit(OpCodes.Ldloc, (var as LocalRef).Local);
                }
            }
            else if(var is GlobalRef) 
            {
                if (t == typeof(Dictionary<object,object>))
                {
                    gen.Emit(OpCodes.Ldnull);
                    gen.Emit(OpCodes.Ldflda, (var as GlobalRef).Field);
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToDict"), null);
                }
                else
                {
                    gen.Emit(OpCodes.Ldnull);
                    gen.Emit(OpCodes.Ldfld, (var as GlobalRef).Field);
                }
            }
            else if (var is ArgumentRef)
            {
                if (t == typeof(Dictionary<object, object>))
                {
                    gen.Emit(OpCodes.Ldarga, (var as ArgumentRef).Number);
                    gen.EmitCall(OpCodes.Call, typeof(stdlol.Utils).GetMethod("ToDict"), null);
                }
                else
                {
                    gen.Emit(OpCodes.Ldarg, (var as ArgumentRef).Number);
                }
            }
            else
            {
                throw new InvalidOperationException("Unknown variable type");
            }

            Expression.EmitCast(gen, var.Type, t);
        }

        public override void StartSet(LOLMethod lm, Type t, ILGenerator gen)
        {
            //Nothing to do
        }

        public override void  EndSet(LOLMethod lm, Type t, ILGenerator gen)
        {
            //LOLProgram.WrapObject(t, gen);
            Expression.EmitCast(gen, t, var.Type);

            //Store it
            if (var is LocalRef)
            {
                gen.Emit(OpCodes.Stloc, (var as LocalRef).Local);
            }
            else if(var is GlobalRef)
            {
                gen.Emit(OpCodes.Stfld, (var as GlobalRef).Field);
            }
            else if (var is ArgumentRef)
            {
                gen.Emit(OpCodes.Starg, (var as ArgumentRef).Number);
            }
            else
            {
                throw new InvalidOperationException("Unknown variable type");
            }
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            return;
        }

        public VariableLValue(CodePragma loc) : base(loc) { }
        public VariableLValue(CodePragma loc, VariableRef vr) : base(loc) { this.var = vr; }
    }

    /*
    internal class ArrayIndexLValue : LValue
    {
        public LValue lval;
        public Expression index;

        public override Type EvaluationType
        {
            get { return typeof(object); }
        }

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
            }

            Expression.EmitCast(gen, typeof(object), t);
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
    */

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
        public VariableRef var;
        public Expression expression = null;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            if(var is LocalRef)
                lm.DefineLocal(gen, var as LocalRef);

            if (expression != null)
            {
                expression.Emit(lm, gen);

                if (var is LocalRef)
                {
                    gen.Emit(OpCodes.Stloc, (var as LocalRef).Local);
                }
                else
                {
                    gen.Emit(OpCodes.Ldnull);
                    gen.Emit(OpCodes.Stfld, (var as GlobalRef).Field);
                }
            }
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            return;
        }

        public VariableDeclarationStatement(CodePragma loc) : base(loc) { }
    }

    internal enum LoopType
    {
        Infinite,
        While,
        Until
    }

    internal class LoopStatement : BreakableStatement
    {
        public string name = null;
        public Statement statements;
        public Statement operation;
        public LoopType type = LoopType.Infinite;
        public Expression condition;

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
            //Create the loop variable if it's defined
            /*if (operation != null)
                lm.DefineLocal(gen, ((operation as AssignmentStatement).lval as VariableLValue).var as LocalRef);*/

            lm.breakables.Add(this);
            gen.MarkLabel(m_continueLabel);

            //Evaluate the condition (if one exists)
            if (condition != null)
            {
                condition.Emit(lm, typeof(bool), gen);
                if (type == LoopType.While)
                {
                    gen.Emit(OpCodes.Brfalse, m_breakLabel);
                }
                else if (type == LoopType.Until)
                {
                    gen.Emit(OpCodes.Brtrue, m_breakLabel);
                }
                else
                {
                    throw new InvalidOperationException("Unknown loop type");
                }
            }

            //lm.BeginScope(gen);
            //Emit the loop body
            statements.Emit(lm, gen);
            //lm.EndScope(gen);

            //Emit the loop op (if one exists)
            if (operation != null)
                operation.Emit(lm, gen);

            gen.Emit(OpCodes.Br, m_continueLabel);

            gen.MarkLabel(m_breakLabel);

            lm.breakables.RemoveAt(lm.breakables.Count - 1);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            if (operation != null)
            {
                FunctionRef fr = ((operation as AssignmentStatement).rval as FunctionExpression).func;
                if (fr.Arity > 1 || (fr.IsVariadic && fr.Arity != 0))
                    errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "Function used in loop must take 1 argument"));
            }

            m_breakLabel = gen.DefineLabel();
            m_continueLabel = gen.DefineLabel();

            lm.breakables.Add(this);
            statements.Process(lm, errors, gen);
            lm.breakables.RemoveAt(lm.breakables.Count - 1);
        }

        public void StartOperation(CodePragma loc)
        {
            operation = new AssignmentStatement(loc);
        }

        public void SetOperationFunction(FunctionRef fr)
        {
            (operation as AssignmentStatement).rval = new FunctionExpression(operation.location, fr);
        }

        public void SetLoopVariable(CodePragma loc, VariableRef vr)
        {
            (operation as AssignmentStatement).lval = new VariableLValue(loc, vr);
            ((operation as AssignmentStatement).rval as FunctionExpression).arguments.Add(new VariableLValue(loc, vr));
        }

        public VariableRef GetLoopVariable()
        {
            return ((operation as AssignmentStatement).lval as VariableLValue).var;
        }

        public LoopStatement(CodePragma loc) : base(loc) { }
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
            if (value is float && t == typeof(string))
                value = ((float)value).ToString();
            if (value is string && t == typeof(int))
                value = int.Parse((string)value);
            if (value is string && t == typeof(float))
                value = float.Parse((string)value);
            if (value.GetType() != t && t != typeof(object))
                throw new ArgumentException(string.Format("{0} encountered, {1} expected.", value.GetType().Name, t.Name));

            if (value is int)
            {
                gen.Emit(OpCodes.Ldc_I4, (int)value);
                if (t == typeof(object))
                    gen.Emit(OpCodes.Box, typeof(int));
            }
            else if (value is float)
            {
                gen.Emit(OpCodes.Ldc_R4, (float)value);
                if (t == typeof(object))
                    gen.Emit(OpCodes.Box, typeof(float));
            }
            else if (value is string)
            {
                gen.Emit(OpCodes.Ldstr, (string)value);
                if (t == typeof(object))
                    gen.Emit(OpCodes.Castclass, typeof(object));
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

        private bool invert = false;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            location.MarkSequencePoint(gen);

            condition.Emit(lm, typeof(bool), gen);

            if (invert)
            {
                gen.Emit(OpCodes.Brtrue, ifFalse);
            }
            else
            {
                gen.Emit(OpCodes.Brfalse, ifFalse);
            }

            //True statements
            //lm.BeginScope(gen);
            trueStatements.Emit(lm, gen);
            if(!(falseStatements is BlockStatement) ||  ((BlockStatement)falseStatements).statements.Count > 0)
                gen.Emit(OpCodes.Br, statementEnd);
            //lm.EndScope(gen);

            //False statements
            gen.MarkLabel(ifFalse);
            if (!(falseStatements is BlockStatement) ||  ((BlockStatement)falseStatements).statements.Count > 0)
            {
                //lm.BeginScope(gen);
                falseStatements.Emit(lm, gen);
                //lm.EndScope(gen);
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
                invert = true;
            }

            condition.Process(lm, errors, gen);
            trueStatements.Process(lm, errors, gen);
            if(falseStatements != null)
                falseStatements.Process(lm, errors, gen);
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

        public List<Case> cases = new List<Case>();
        public Statement defaultCase = null;
        public Expression control;

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
            //lm.BeginScope(gen);
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

            //lm.EndScope(gen);
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

            if (breakIdx < 0)
            {
                if (lm.info.ReturnType != typeof(void))
                    //TODO: When we optimise functions to possibly return other values, worry about this
                    gen.Emit(OpCodes.Ldnull);
                gen.Emit(OpCodes.Ret);
            }
            else
            {
                gen.Emit(OpCodes.Br, lm.breakables[breakIdx].BreakLabel.Value);
            }
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
                    //Return from function
                    //errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, "ENUF encountered, but nothing to break out of!"));
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

    internal class FunctionExpression : Expression
    {
        public FunctionRef func;
        public List<Expression> arguments = new List<Expression>();

        public override Type EvaluationType
        {
            get { return func.ReturnType; }
        }

        public override void Emit(LOLMethod lm, Type t, ILGenerator gen)
        {
            if (func.IsVariadic)
            {
                //First do standard (non variadic) arguments)
                for (int i = 0; i < func.Arity; i++)
                    arguments[i].Emit(lm, func.ArgumentTypes[i], gen);

                //Now any variadic arguments go into an array
                Type argType = func.ArgumentTypes[func.Arity].GetElementType();
                gen.Emit(OpCodes.Ldc_I4, arguments.Count - func.Arity);
                gen.Emit(OpCodes.Newarr, argType);

                for (int i = func.Arity; i < arguments.Count; i++)
                {
                    gen.Emit(OpCodes.Dup);
                    gen.Emit(OpCodes.Ldc_I4, i - func.Arity);
                    arguments[i].Emit(lm, argType, gen);
                    gen.Emit(OpCodes.Stelem, argType);
                }

                gen.EmitCall(OpCodes.Call, func.Method, null);
            }
            else
            {
                for (int i = 0; i < arguments.Count; i++)
                    arguments[i].Emit(lm, func.ArgumentTypes[i], gen);

                gen.EmitCall(OpCodes.Call, func.Method, null);
            }
            
            //Finally, make sure the return type is correct
            Expression.EmitCast(gen, func.ReturnType, t);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            if (arguments.Count != func.Arity && !func.IsVariadic)
            {
                errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, string.Format("Function \"{0}\" requires {1} arguments, passed {2}.", func.Name, func.Arity, arguments.Count)));
            }
            else if (arguments.Count < func.Arity && func.IsVariadic)
            {
                errors.Add(new CompilerError(location.filename, location.startLine, location.startColumn, null, string.Format("Function \"{0}\" requires at least {1} arguments, passed {2}.", func.Name, func.Arity, arguments.Count)));
            }

            foreach (Expression arg in arguments)
                arg.Process(lm, errors, gen);
        }

        public FunctionExpression(CodePragma loc) : base(loc) { }

        public FunctionExpression(CodePragma loc, FunctionRef fr) : base(loc) { func = fr; }
    }

    internal class StringExpression : Expression
    {
        private LValue[] vars;
        private string str;

        public override Type EvaluationType
        {
            get { return typeof(string); }
        }

        public override void Emit(LOLMethod lm, Type t, ILGenerator gen)
        {
            if (vars.Length == 0)
            {
                //Just output the string
                gen.Emit(OpCodes.Ldstr, str);
            }
            else
            {
                //Output a call to string.Format
                gen.Emit(OpCodes.Ldstr, str);
                gen.Emit(OpCodes.Ldc_I4, vars.Length);
                gen.Emit(OpCodes.Newarr, typeof(object));

                for (int i = 0; i < vars.Length; i++)
                {
                    gen.Emit(OpCodes.Dup);
                    gen.Emit(OpCodes.Ldc_I4, i);
                    vars[i].EmitGet(lm, vars[i].EvaluationType, gen);
                    gen.Emit(OpCodes.Stelem, vars[i].EvaluationType);
                }

                gen.EmitCall(OpCodes.Call, typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object[]) }), null);
            }
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            return;
        }

        public static string UnescapeString(string str, Scope s, Errors e, CodePragma location, List<VariableRef> refs)
        {
            int lastIdx = 1;
            int idx;
            StringBuilder ret = new StringBuilder();

            try
            {
                while ((idx = str.IndexOf(':', lastIdx)) != -1)
                {
                    //Append the string between the last escape and this one
                    ret.Append(str, lastIdx, idx - lastIdx);

                    //Decipher the escape
                    int endIdx, refnum;
                    VariableRef vr;
                    switch (str[idx + 1])
                    {
                        case ')':
                            ret.Append('\n');
                            lastIdx = idx + 2;
                            break;
                        case '>':
                            ret.Append('\t');
                            lastIdx = idx + 2;
                            break;
                        case 'o':
                            ret.Append('\a');
                            lastIdx = idx + 2;
                            break;
                        case '"':
                            ret.Append('"');
                            lastIdx = idx + 2;
                            break;
                        case ':':
                            ret.Append(':');
                            lastIdx = idx + 2;
                            break;
                        case '(':
                            endIdx = str.IndexOf(')', idx + 2);
                            ret.Append(char.ConvertFromUtf32(int.Parse(str.Substring(idx + 2, endIdx - idx - 2), System.Globalization.NumberStyles.AllowHexSpecifier)));
                            lastIdx = endIdx + 1;
                            break;
                        case '{':
                            endIdx = str.IndexOf('}', idx + 2);
                            vr = s[str.Substring(idx + 2, endIdx - idx - 2)] as VariableRef;
                            if (vr == null)
                                e.SemErr(location.filename, location.startLine, location.startColumn, string.Format("Undefined variable: \"{0}\"", str.Substring(idx + 2, endIdx - idx - 2)));
                            refnum = refs.IndexOf(vr);
                            if (refnum == -1)
                            {
                                refnum = refs.Count;
                                refs.Add(vr);
                            }
                            ret.Append("{" + refnum.ToString() + "}");
                            lastIdx = endIdx + 1;
                            break;
                        case '[':
                            endIdx = str.IndexOf(']', idx + 2);
                            string uc = UnicodeNameLookup.GetUnicodeCharacter(str.Substring(idx + 2, endIdx - idx - 2));
                            if (uc == null)
                            {
                                e.SemErr(location.filename, location.startLine, location.startColumn, string.Format("Unknown unicode normative name: \"{0}\".", str.Substring(idx + 2, endIdx - idx - 2)));
                            }
                            else
                            {
                                ret.Append(uc);
                            }
                            lastIdx = endIdx + 1;
                            break;
                    }
                }

                //Append the end of the string
                ret.Append(str, lastIdx, str.Length - lastIdx - 1);
            }
            catch (Exception ex)
            {
                e.SemErr(string.Format("Invalid escape sequence in string constant: {0}", ex.Message));
            }

            return ret.ToString();
        }

        public StringExpression(CodePragma loc, string str, Scope s, Errors e) : base(loc)
        {
            List<VariableRef> refs = new List<VariableRef>();
            this.str = UnescapeString(str, s, e, location, refs);

            vars = new LValue[refs.Count];
            for (int i = 0; i < vars.Length; i++)
                vars[i] = new VariableLValue(location, refs[i]);
        }
    }

    internal class TypecastExpression : Expression
    {
        public Type destType;
        public Expression exp;

        public override Type EvaluationType
        {
            get { return destType; }
        }

        public override void Emit(LOLMethod lm, Type t, ILGenerator gen)
        {
            exp.Emit(lm, destType, gen);
            if (destType != t)
                Expression.EmitCast(gen, destType, t);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            exp.Process(lm, errors, gen);
        }

        public TypecastExpression(CodePragma loc) : base(loc) { }

        public TypecastExpression(CodePragma loc, Type t, Expression exp) : base(loc)
        {
            this.destType = t;
            this.exp = exp;
        }
    }

    internal class ReturnStatement : Statement
    {
        public Expression expression;

        public override void Emit(LOLMethod lm, ILGenerator gen)
        {
            expression.Emit(lm, lm.info.ReturnType, gen);
            gen.Emit(OpCodes.Ret);
        }

        public override void Process(LOLMethod lm, CompilerErrorCollection errors, ILGenerator gen)
        {
            expression.Process(lm, errors, gen);
        }

        public ReturnStatement(CodePragma loc) : base(loc) { }
    }
}
