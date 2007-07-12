using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.CodeDom.Compiler;

namespace notdot.LOLCode.Parser.v1_2
{
    internal partial class Parser
    {
        public static Parser GetParser(ModuleBuilder mb, LOLProgram prog, string filename, Stream s, CompilerResults cr) {
            Parser p = new Parser(new Scanner(s));
            p.filename = Path.GetFileName(filename);
            if (prog.compileropts.IncludeDebugInformation)
            {
                p.doc = mb.DefineDocument(p.filename, Guid.Empty, Guid.Empty, Guid.Empty);
            }
            else
            {
                //Not a debug build
                p.doc = null;
            }

            p.program = prog;
            p.errors = new Errors(cr.Errors);
            p.main = prog.methods["Main"];

            return p;
        }

        private string filename;
        private ISymbolDocumentWriter doc;
        private LOLProgram program;
        private LOLMethod main;
        private LOLMethod currentMethod = null;

        private bool IsArrayIndex()
        {
            return scanner.Peek().kind == _in;
        }

        private CodePragma GetPragma(Token tok)
        {
            return new CodePragma(doc, filename, tok.line, tok.col);
        }

        private void SetEndPragma(CodeObject co)
        {
            if (co == null)
                //We encountered an error - we can ignore this, since it will result in a compiler error
                return;

            co.location.endLine = t.line;
            co.location.endColumn = t.col + t.val.Length;
        }

        private void BeginScope()
        {
        }

        private void EndScope()
        {
        }

        void Error(string s)
        {
            if (errDist >= minErrDist) errors.SemErr(filename, t.line, t.col, s);
            errDist = 0;
        }

        void Warning(string s)
        {
            if(errDist >= minErrDist) errors.Warning(filename, t.line, t.col, s);
            errDist = 0;
        }

        private Scope GetScope()
        {
            if (currentMethod == null)
            {
                return main.locals;
            }
            else
            {
                return currentMethod.locals;
            }
        }

        private VariableRef DeclareVariable(string name)
        {
            VariableRef ret;
            if (currentMethod == null)
            {
                /*ret = new GlobalRef(name);
                program.globals.AddSymbol(ret);*/
                ret = new LocalRef(name);
                main.locals.AddSymbol(ret);
            }
            else
            {
                ret = new LocalRef(name);
                currentMethod.locals.AddSymbol(ret);
            }

            return ret;
        }

        private FunctionRef GetFunction(string name)
        {
            SymbolRef ret = GetScope()[name];
            if (ret == null)
                Error(string.Format("Unknown function: \"{0}\"", name));
            if(!(ret is FunctionRef))
                Error(string.Format("{0} is a variable, but is used like a function", name));
            
            return ret as FunctionRef;
        }

        private bool IsFunction(string name)
        {
            return GetScope()[name] is FunctionRef;
        }

        private VariableRef GetVariable(string name)
        {
            SymbolRef ret = GetScope()[name];
            if (ret == null)
                Error(string.Format("Unknown variable: \"{0}\"", name));
            if(!(ret is VariableRef))
                Error(string.Format("{0} is a function, but is used like a variable", name));
            
            return ret as VariableRef;
        }

        private LocalRef CreateLoopVariable(string name)
        {
            LocalRef ret = new LocalRef(name);
            GetScope().AddSymbol(ret);

            return ret;
        }

        private void RemoveLoopVariable(LocalRef var)
        {
            GetScope().RemoveSymbol(var);
        }

        private void AddCase(SwitchStatement ss, object label, Statement block)
        {
            ss.cases.Add(new SwitchStatement.Case(label, block));
        }
    }

}
