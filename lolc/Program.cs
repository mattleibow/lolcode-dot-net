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
        static void Main(string[] args)
        {
            LOLCodeCodeProvider compiler = new LOLCodeCodeProvider();
            CompilerParameters cparam = new CompilerParameters();
            cparam.GenerateExecutable = true;
            cparam.GenerateInMemory = false;
            cparam.OutputAssembly = Path.ChangeExtension(args[0], ".exe");
            CompilerResults results = compiler.CompileAssemblyFromFile(cparam, args[0]);

            for (int i = 0; i < results.Errors.Count; i++)
                Console.Error.WriteLine(results.Errors[i].ToString());

            if (results.Errors.HasErrors)
            {
                Console.Out.WriteLine("Failed to compile.");
            }
            else
            {
                Console.Out.WriteLine("Successfully compiled.");
            }
        }
    }
}
