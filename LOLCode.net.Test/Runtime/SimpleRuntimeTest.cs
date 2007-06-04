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
    [TestFixture]
    public class SimpleRuntimeTest
    {

        [Test]
        public void VisibleKeywordRuntime()
        {
            string sources = SampleHelper.GetCodeFromSample("visible.lol");
            string baseline = SampleHelper.GetBaselineFromSample("visible.lol");
            RuntimeTestHelper.TestExecuteSourcesNoInput(sources, baseline, 
                "VisibleKeywordRuntime", ExecuteMethod.ExternalProcess); 
        }

        [Test]
        public void HaiWorldRuntime()
        {
            string sources = SampleHelper.GetCodeFromSample("haiworld.lol");
            string baseline = SampleHelper.GetBaselineFromSample("haiworld.lol");
            RuntimeTestHelper.TestExecuteSourcesNoInput(sources, baseline,
                "HaiWorldRuntime", ExecuteMethod.ExternalProcess);
        }

        [Test]
        public void Simple1Runtime()
        {
            string sources = SampleHelper.GetCodeFromSample("simple1.lol");
            string baseline = SampleHelper.GetBaselineFromSample("simple1.lol");
            RuntimeTestHelper.TestExecuteSourcesNoInput(sources, baseline,
                "Simple1Runtime", ExecuteMethod.ExternalProcess);
        }

    }
}
