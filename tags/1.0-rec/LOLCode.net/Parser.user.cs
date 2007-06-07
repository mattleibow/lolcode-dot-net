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
            if (errDist >= minErrDist) errors.SemErr(filename, t.line, t.col, s);
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

        private string UnescapeString(string str)
        {
            int lastIdx = 1;
            int idx;
            StringBuilder ret = new StringBuilder();

            try
            {
                while ((idx = str.IndexOf('\\', lastIdx)) != -1)
                {
                    //Append the string between the last escape and this one
                    ret.Append(str, lastIdx, idx - lastIdx);

                    //Decipher the escape
                    switch (str[idx + 1])
                    {
                        case '\'':
                            ret.Append('\'');
                            lastIdx = idx + 2;
                            break;
                        case '"':
                            ret.Append('"');
                            lastIdx = idx + 2;
                            break;
                        case '\\':
                            ret.Append('\\');
                            lastIdx = idx + 2;
                            break;
                        case '0':
                            ret.Append('\0');
                            lastIdx = idx + 2;
                            break;
                        case 'a':
                            ret.Append('\a');
                            lastIdx = idx + 2;
                            break;
                        case 'b':
                            ret.Append('\b');
                            lastIdx = idx + 2;
                            break;
                        case 'f':
                            ret.Append('\f');
                            lastIdx = idx + 2;
                            break;
                        case 'n':
                            ret.Append('\n');
                            lastIdx = idx + 2;
                            break;
                        case 'r':
                            ret.Append('\r');
                            lastIdx = idx + 2;
                            break;
                        case 't':
                            ret.Append('\t');
                            lastIdx = idx + 2;
                            break;
                        case 'v':
                            ret.Append('\v');
                            lastIdx = idx + 2;
                            break;
                        case 'x':
                            int end = idx + 3;
                            while ("0123456789abcdefABCDEF".IndexOf(str[end]) != -1)
                                end++;
                            if (end - idx > 6)
                                end = idx + 6;
                            ret.Append(char.ConvertFromUtf32(int.Parse(str.Substring(idx + 2, end - idx - 2), System.Globalization.NumberStyles.AllowHexSpecifier)));
                            lastIdx = end;
                            break;
                        case 'u':
                            ret.Append(char.ConvertFromUtf32(int.Parse(str.Substring(idx + 2, 4), System.Globalization.NumberStyles.AllowHexSpecifier)));
                            lastIdx = idx + 6;
                            break;
                        case 'U':
                            ret.Append(char.ConvertFromUtf32(int.Parse(str.Substring(idx + 2, 8), System.Globalization.NumberStyles.AllowHexSpecifier)));
                            lastIdx = idx + 10;
                            break;
                    }
                }

                //Append the end of the string
                ret.Append(str, lastIdx, str.Length - lastIdx - 1);
            }
            catch (Exception ex)
            {
                SemErr(string.Format("Invalid escape sequence in string constant: {0}", ex.Message));
            }

            return ret.ToString();
        }
    }
}
