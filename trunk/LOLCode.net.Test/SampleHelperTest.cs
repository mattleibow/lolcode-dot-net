using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework; 

namespace LOLCode.net.Tests
{
    [TestFixture]    
    public class SampleHelperTest
    {

        [Test]
        public void GetCodeFromSampleNoBlocks()
        {
            string value = SampleHelper.GetCodeFromSample("fulltest.lol");
            Assert.IsTrue(value.Contains("BTW")); 
        }

        [Test]
        public void GetCodeFromSampleCodeBlock()
        {
            string value = SampleHelper.GetCodeFromSample("visible.lol");
            Assert.IsTrue(value.Contains("BTW")); 
        }

        [Test]
        public void GetBaselineFromSampleBlock()
        {
            string value = SampleHelper.GetBaselineFromSample("visible.lol");
            Assert.IsTrue(value.Contains("HELLO")); 
        }

    }
}
