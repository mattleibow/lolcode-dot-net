using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Core;
using NUnit.Framework;
using stdlol;

namespace LOLCode.net.Tests.stdlol
{
    /// <summary>
    /// Tests methods in StdLol.Utils
    /// </summary>
    /// <history>
    ///     philprice   06/03/2007      Created (incomplete)
    /// </history>
    [TestFixture] 
    public class UtilsTest
    {

        //TODO: ReadWord, GetDict, GetObject 

        #region "ToString Tests"
        //Note: Stubbed out because automatic casting of ints to strings has been
        //disabled in favor of a more versatile VISIBLE statement, for now.
        /*
        [Test]
        public void ToStringInt()
        {
            int value = 1337;

            Assert.AreEqual(value.ToString(), Utils.ToString(value));
            Assert.AreEqual(value.ToString(), Utils.ToString(value as object));
            Assert.AreNotEqual("7331", Utils.ToString(value));
        }
        */

        [Test]
        public void ToStringString()
        {
            string value = "CHEEZBURGER"; 

            Assert.AreEqual(value, Utils.ToString(value));
            Assert.AreEqual(value.ToString(), Utils.ToString(value as object));
            Assert.AreNotEqual("cheezburger", Utils.ToString(value));
            Assert.AreNotEqual("bucket", Utils.ToString(value));
        }

        [Test]
        public void ToStringNull()
        {
            object value = null;
            Assert.AreEqual(String.Empty, Utils.ToString(value));
        }

        [Test]
        public void ToStringDictionary()
        {
            Dictionary<object, object> dict = new Dictionary<object, object>();
            dict.Add(0, "KITTEH");
            Assert.AreEqual("KITTEH", Utils.ToString(dict));
        }

        [Test]
        public void ToStringDictionaryNoZeroIndex()
        {
            Dictionary<object, object> dict = new Dictionary<object, object>();
            dict.Add(1, "KITTEH");
            Assert.AreEqual(String.Empty, Utils.ToString(dict));
        }

        [Test]
        [ExpectedException(typeof(InvalidCastException), 
            ExpectedMessage="Cannot cast type \"Guid\" to string")]
        public void ToStringInvalidType()
        {
            Guid value = Guid.NewGuid();
            Utils.ToString(value); 
        }

        #endregion

        #region "ToInt Tests"

        [Test]
        public void ToIntInt()
        {
            int value = 1337;
            int value2 = 0;

            Assert.AreEqual(1337, Utils.ToInt(value));
            Assert.AreEqual(1337, Utils.ToInt(value as object));
            Assert.AreEqual(0, Utils.ToInt(value2));
        }

        [Test]
        public void ToIntNull()
        {
            object value = null; 
            Assert.AreEqual(0, Utils.ToInt(value));
        }

        [Test]
        public void ToIntDictionary()
        {
            Dictionary<object, object> dictionary = new Dictionary<object, object>();
            dictionary.Add(0, 1337);

            Assert.AreEqual(1337, Utils.ToInt(dictionary));
            Assert.AreEqual(1337, Utils.ToInt(dictionary as object)); 
        }

        [Test]
        public void ToIntDictionaryNoZeroIndex()
        {
            // NOTE: I'm unsure of this behavior...
            Dictionary<object, object> dictionary = new Dictionary<object, object>();
            dictionary.Add(1, 1337);

            Assert.AreEqual(0, Utils.ToInt(dictionary));
            Assert.AreEqual(0, Utils.ToInt(dictionary as object));
        }

        [Test]
        [ExpectedException(typeof(InvalidCastException), 
            ExpectedMessage="Cannot cast type \"String\" to int")]
        public void ToIntInvalidString()
        {
            string value = "KITTEH";
            Utils.ToInt(value); 
        }

        [Test]
        [ExpectedException(typeof(InvalidCastException),
           ExpectedMessage = "Cannot cast type \"Guid\" to int")]
        public void ToIntInvalidType()
        {
            Guid value = Guid.NewGuid();
            Utils.ToInt(value);
        }

        #endregion

        #region "ToDict Tests"
        /*
        [Test]
        public void ToDictDictionary()
        {
            Dictionary<object, object> dictionary = new Dictionary<object,object>();
            dictionary.Add(0, 1);
            dictionary.Add(1, "KITTEH");

            object dictObj = dictionary as object;

            Dictionary<object, object> toDictValue = Utils.ToDict(ref dictObj);

            Assert.AreEqual(dictionary, toDictValue);
            Assert.AreEqual(dictionary.Count, toDictValue.Count, "Ensure same length"); 
            Assert.AreEqual(dictionary[0], toDictValue[0], "Ensure same content");
            Assert.AreEqual("KITTEH", toDictValue[1], "Ensure same content");
        }

        [Test]
        public void ToDictString()
        {
            Type exptecedType = typeof(Dictionary<object, object>);
            object value = "KITTEH" as object; // String
         
            object toDictValue = Utils.ToDict(ref value);

            Assert.AreEqual(toDictValue.GetType(), exptecedType, "Return value is a dictionary");
            Assert.AreEqual(value.GetType(), exptecedType, "Input value is now a dictionary");
            Assert.AreEqual(toDictValue, value, "Return value and input are the same object");

            // Cast the return value to Dictionary<object, object>

            Dictionary<object, object> toDictDict = toDictValue as Dictionary<object, object>;

            Assert.AreEqual("KITTEH", toDictDict[0], "Generated dictionary has expected string");
            Assert.AreEqual(1, toDictDict.Count, "Generated dictionary has 1 thing in it");
        }

        [Test]
        public void ToDictInt()
        {
            Type exptecedType = typeof(Dictionary<object, object>);
            object value = 1337 as object; // Int32

            object toDictValue = Utils.ToDict(ref value);

            Assert.AreEqual(toDictValue.GetType(), exptecedType, "Return value is a dictionary");
            Assert.AreEqual(value.GetType(), exptecedType, "Input value is now a dictionary");
            Assert.AreEqual(toDictValue, value, "Return value and input are the same object");

            // Cast the return value to Dictionary<object, object>

            Dictionary<object, object> toDictDict = toDictValue as Dictionary<object, object>;

            Assert.AreEqual(1337, toDictDict[0], "Generated dictionary has expected string");
            Assert.AreEqual(1, toDictDict.Count, "Generated dictionary has 1 thing in it");
        }

        [Test]
        [ExpectedException(typeof(InvalidCastException), 
            ExpectedMessage="Unknown type \"Guid\"")]
        public void ToDictInvalidType()
        {
            object value = Guid.NewGuid() as object;
            Utils.ToDict(ref value);
        }
        */
        #endregion

    }
}
