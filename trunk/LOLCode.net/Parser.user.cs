using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Diagnostics.SymbolStore;
using System.IO;

namespace notdot.LOLCode
{
    internal partial class Parser
    {
        public static Parser GetParser(ModuleBuilder mb, Program prog, string filename, Stream s) {
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

            return p;
        }

        private string filename;
        private ISymbolDocumentWriter doc;
        private Program program;

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

        void Error(string s)
        {
            if (errDist >= minErrDist) errors.SemErr(filename, la.line, la.col, s);
            errDist = 0;
        }

        private List<string> locals = new List<string>();
        private Stack<int> localScopes = new Stack<int>();

        private void BeginScope()
        {
            localScopes.Push(locals.Count);
        }

        private void EndScope()
        {
            int val = localScopes.Pop();
            locals.RemoveRange(val, locals.Count - val);
        }

        private void CreateLocal(string name)
        {
            int idx = locals.IndexOf(name);
            if (idx > -1 && (localScopes.Count == 0 || idx >= localScopes.Peek()))
            {
                Error(string.Format("Redefinition of \"{0}\".", name));
            }
            else
            {
                locals.Add(name);
            }
        }

        private void ReferenceLocal(string name)
        {
            if (!locals.Contains(name))
                Error(string.Format("Reference to undefined symbol \"{0}\"", name));
        }
    }
}
