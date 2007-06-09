using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace notdot.LOLCode
{
    public class LOLCodeCodeProvider : CodeDomProvider, ICodeCompiler
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
            return CompileAssemblyFromFileBatch(options, new string[] { fileName });
        }

        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            Stream[] streams = new Stream[fileNames.Length];
            for (int i = 0; i < streams.Length; i++)
                streams[i] = File.OpenRead(fileNames[i]);

            return CompileAssemblyFromStreamBatch(options, fileNames, streams);
        }

        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            return CompileAssemblyFromSourceBatch(options, new string[] { source });
        }

        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            Stream[] streams = new Stream[sources.Length];
            string[] filenames = new string[sources.Length];
            for (int i = 0; i < streams.Length; i++)
            {
                streams[i] = new MemoryStream(Encoding.UTF8.GetBytes(sources[i]));
                filenames[i] = "unknown";
            }

            return CompileAssemblyFromStreamBatch(options, filenames, streams);
        }

        private CompilerResults CompileAssemblyFromStreamBatch(CompilerParameters options, string[] filenames, Stream[] streams)
        {
            AssemblyName name = new AssemblyName();
            name.Name = Path.GetFileName(options.OutputAssembly);

            AssemblyBuilder ab = Thread.GetDomain().DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndSave);

            if (options.IncludeDebugInformation)
            {
                ConstructorInfo daCtor = typeof(DebuggableAttribute).GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });
                CustomAttributeBuilder daBuilder = new CustomAttributeBuilder(daCtor, new object[] { DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations });
                ab.SetCustomAttribute(daBuilder);
            }

            ModuleBuilder mb = ab.DefineDynamicModule(Path.GetFileName(options.OutputAssembly), options.IncludeDebugInformation);

            CompilerResults ret = new CompilerResults(options.TempFiles);
            Errors err = new Errors(ret.Errors);

            LOLProgram prog = new LOLProgram(options);
            for (int i = 0; i < streams.Length; i++)
            {
                Parser p = Parser.GetParser(mb, prog, filenames[i], streams[i]);
                p.errors = err;
                p.Parse();
            }

            if(ret.Errors.Count > 0)
                return ret;

            MethodInfo entryMethod = prog.Emit(ret.Errors, mb);
            if (ret.Errors.Count > 0)
                return ret;

            ab.SetEntryPoint(entryMethod);

            if (!options.GenerateInMemory)
                ab.Save(options.OutputAssembly);

            ret.CompiledAssembly = ab;

            return ret;
        }

        #endregion
    }
}
