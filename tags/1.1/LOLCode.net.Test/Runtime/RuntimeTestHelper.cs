using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom.Compiler;
using System.IO;
using System.Diagnostics; 
using NUnit.Framework;
using notdot.LOLCode;

namespace LOLCode.net.Tests.Runtime
{
    internal class RuntimeTestHelper
    {

        internal static void TestExecuteSourcesNoInput(string source, string baseline, string testname, ExecuteMethod method)
        {
            TestExecuteSourcesNoInput(new string[] { source }, baseline, testname, method); 
        }

        internal static void TestExecuteSourcesNoInput(string[] sources, string baseline, string testname, ExecuteMethod method)
        {

            LOLCodeCodeProvider provider = new LOLCodeCodeProvider();
            string assemblyName = String.Format("{0}.exe", testname);
            CompilerParameters cparam = GetDefaultCompilerParams(assemblyName, method);

            // Compile test
            CompilerResults results = provider.CompileAssemblyFromSourceBatch(cparam, sources);

            // Collect errors (if any) 
            StringBuilder errors = new StringBuilder(); 
            if (results.Errors.HasErrors)
            {
                for (int i = 0; i < results.Errors.Count; i++)
                {
                    errors.AppendLine(results.Errors[i].ToString());
                }
            }

            // Ensure there are no errors before trying to execute
            Assert.AreEqual(0, results.Errors.Count, errors.ToString());

            if (method == ExecuteMethod.InMemory)
            {
                throw new NotImplementedException("In memory execution is not implimented yet");
            }
            else
            {
                // Run the executeable (collecting it's output) compare to baseline 
                Assert.AreEqual(baseline, RunExecuteable(assemblyName));
                File.Delete(assemblyName);
            }
        }

        private static string RunExecuteable(string path)
        {

            Process p = new Process();
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = path;
            p.Start();
            p.WaitForExit();
            return p.StandardOutput.ReadToEnd();
        }


        private static CompilerParameters GetDefaultCompilerParams(string outfile, ExecuteMethod method)
        {
            CompilerParameters cparam = new CompilerParameters();
            cparam.GenerateExecutable = true;
            cparam.GenerateInMemory = (method == ExecuteMethod.InMemory) ? true : false;
            cparam.OutputAssembly = outfile;
            cparam.MainClass = "Program";
            cparam.IncludeDebugInformation = true;
            return cparam;
        }
    }

    internal enum ExecuteMethod
    {
        ExternalProcess,
        InMemory
    }
}
