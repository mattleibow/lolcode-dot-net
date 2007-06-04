using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions; 

namespace LOLCode.net.Tests
{
    internal static class SampleHelper
    {
        private static Dictionary<string, string> resourceCache = new Dictionary<string, string>(); 
        private static string SampleNamespace = "LOLCode.net.Tests.Samples"; 

        internal static string GetTestSampleFull(string sampleName)
        {
            string resourceName = String.Format("{0}.{1}", SampleNamespace, sampleName);

            if (!resourceCache.ContainsKey(resourceName))
            {
                StringBuilder content = new StringBuilder();
                Assembly thisAssembly = Assembly.GetExecutingAssembly();

                using (Stream resourceStream = thisAssembly.GetManifestResourceStream(resourceName))
                {

                    if (resourceStream == null)
                    {
                        throw new ArgumentException(String.Format("Unable to locate embedded resource \"{0}\"", resourceName));
                    }

                    using (TextReader reader = new StreamReader(resourceStream))
                    {
                        content.Append(reader.ReadToEnd());
                    }
                }
                resourceCache.Add(resourceName, content.ToString());
            }

            return resourceCache[resourceName];
        }

        internal static void WriteSampleCodeToFile(string sampleName, string file)
        {

            string sampleCode = GetCodeFromSample(sampleName);

            if (!File.Exists(file))
            {
                File.Delete(file); 
            }

            File.WriteAllText(file, sampleCode); 
        }

        internal static string GetCodeFromSample(string sampleName)
        {
            string sampleContent = GetTestSampleFull(sampleName);

            if (!ContainsBeginBlocks(sampleContent))
            {
                return sampleContent;
            }
            else
            {
                Dictionary<string, string> blocks = GetSampleBlocks(sampleContent);
                return blocks["code"]; 
            }
        }

        internal static string GetBaselineFromSample(string sampleName)
        {
            string sampleContent = GetTestSampleFull(sampleName);

            if (!ContainsBeginBlocks(sampleContent))
            {
                return string.Empty;
            }
            else
            {
                Dictionary<string, string> blocks = GetSampleBlocks(sampleContent);
                return blocks["baseline"];
            }
        }

        private static bool ContainsBeginBlocks(string content)
        {
            // TODO: Improve this one might have LOL VALUE R "-->begin" or something
            return content.StartsWith("-->begin ") || content.Contains("\n-->begin ");
        }

        private static Dictionary<string, string> GetSampleBlocks(string content)
        {
            Dictionary<string, string> blocks = new Dictionary<string, string>(); 
            
            // Split into lines
            List<string> lines = new List<string>(content.Split('\n'));

            // Trim any \r
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = lines[i].TrimEnd('\r');
            }

            StringBuilder currentBlock = null;
            string currentBlockKey = string.Empty;

            foreach (string line in lines)
            {
                if (line.StartsWith("-->begin "))
                {
                    if (!String.IsNullOrEmpty(currentBlockKey))
                    {
                        // Place current block content into the dictionary
                        blocks.Add(currentBlockKey, currentBlock.ToString());
                    }
                    currentBlock = new StringBuilder();

                    string[] beginParts = line.Split(new char[] { ' ' }, 2);

                    currentBlockKey = (beginParts.Length == 2 && !String.IsNullOrEmpty(beginParts[1]))
                        ? beginParts[1] : Guid.NewGuid().ToString();
                }
                else
                {
                    currentBlock.AppendLine(line); 
                }
            }

            if (!blocks.ContainsKey(currentBlockKey) && !String.IsNullOrEmpty(currentBlockKey) && currentBlock != null)
            {
                blocks.Add(currentBlockKey, currentBlock.ToString()); 
            }

            return blocks; 
        }

    }
}
