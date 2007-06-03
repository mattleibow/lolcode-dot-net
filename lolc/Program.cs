using System;
using System.Collections.Generic;
using System.Text;
using notdot.LOLCode;
using System.CodeDom.Compiler;
using System.IO;

namespace notdot.LOLCode.lolc
{
    class Program
    {
        static int Main(string[] args)
        {
            LolCompilerArguments arguments = new LolCompilerArguments();

            if (!CommandLine.Parser.ParseArgumentsWithUsage(args, arguments))
            {
                return 2; 
            }

            // Ensure some goodness in the arguments
            if (!LolCompilerArguments.PostValidateArguments(arguments))
            {
                // Errors are output by PostValidateArguments to STDERR
                return 3;
            }

            // Warn the user if there is more than one source file, as they will be ignored (for now) 
            // TODO: Should be removed eventually
            if (arguments.sources.Length > 1)
            {
                Console.Error.WriteLine("lolc warning: More than one source file specifed. Only '{0}' will be compiled.", arguments.sources[0]);
            }

            // Good to go
            string outfileFile = String.IsNullOrEmpty(arguments.output) ? Path.ChangeExtension(arguments.sources[0], ".exe") : arguments.output;
            LOLCodeCodeProvider compiler = new LOLCodeCodeProvider();
            CompilerParameters cparam = new CompilerParameters();
            cparam.GenerateExecutable = true;
            cparam.GenerateInMemory = false;
            cparam.OutputAssembly = outfileFile;
            cparam.IncludeDebugInformation = arguments.debug;
            cparam.ReferencedAssemblies.AddRange(arguments.references); 
            CompilerResults results = compiler.CompileAssemblyFromFile(cparam, args[0]);

            for (int i = 0; i < results.Errors.Count; i++)
                Console.Error.WriteLine(results.Errors[i].ToString());

            if (results.Errors.HasErrors)
            {
                Console.Out.WriteLine("Failed to compile.");
                return 1;
            }
            else
            {
                Console.Out.WriteLine("Successfully compiled.");
                return 0;
            }
        }
    }
}
