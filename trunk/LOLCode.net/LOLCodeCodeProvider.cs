using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom.Compiler;

namespace LOLCode.net
{
    class LOLCodeCodeProvider : CodeDomProvider, ICodeCompiler
    {

        public override ICodeCompiler CreateCompiler()
        {
            return this;
        }

        public override ICodeGenerator CreateGenerator()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override ICodeParser CreateParser()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #region ICodeCompiler Members

        public CompilerResults CompileAssemblyFromDom(CompilerParameters options, System.CodeDom.CodeCompileUnit compilationUnit)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, System.CodeDom.CodeCompileUnit[] compilationUnits)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
