using System;
using System.Collections.Generic;
using System.Text;
using System.IO; 
using CommandLine;

namespace notdot.LOLCode.lolc
{
    /// <summary>
    /// Command line arguments for the compiler
    /// </summary>
    /// <history>
    ///     philprice   06/02/2007      Created
    /// </history>
    internal class LolCompilerArguments
    {

        // Note: LongName set as 'out' is a reserved keyword
        [Argument(ArgumentType.AtMostOnce, 
            HelpText = "Specify output file name", 
            LongName="out")]
        public string output;

        // Note: Superfluous for Lolcode 1.0 
        [Argument(ArgumentType.AtMostOnce, 
            HelpText = "Target assembly type: exe or library. The default is exe.", 
            DefaultValue="exe")]
        public string target;

        [Argument(ArgumentType.AtMostOnce, 
            HelpText = "Limit which platforms this code can run on: x86, Itanium, x64, or anycpu. The default is anycpu.", 
            DefaultValue = "anycpu")]
        public string platform;

        [Argument(ArgumentType.Multiple,
            HelpText = "Reference metadata from the specified assembly files (Short form: /r)", 
            LongName="reference",
            ShortName="r")]
        public string[] references;

        [Argument(ArgumentType.AtMostOnce,
            HelpText = "Emit debugging information",
            DefaultValue = true)]
        public bool debug;

        [Argument(ArgumentType.AtMostOnce,
            HelpText = "Enable optimizations (Short form: /o)",
            ShortName="o")]
        public bool optimize; 

        [DefaultArgument(ArgumentType.MultipleUnique, 
            HelpText = "Source files to compile")]
        public string[] sources;

        #region "Helper methods"

        internal static bool IsTargetValid(string target)
        {
            return (target.ToLowerInvariant() == "exe" || target.ToLowerInvariant() == "library");
        }

        internal static bool IsPlatformValid(string platform)
        {
            return (platform.ToLowerInvariant() == "anycpu" || platform.ToLowerInvariant() == "x86" ||
                    platform.ToLowerInvariant() == "Itanium" || platform.ToLowerInvariant() == "x64"); 
        }

        internal static bool IsValidDebugType(string debugtype)
        {
            return (debugtype.ToLowerInvariant() == "full" || debugtype.ToLowerInvariant() == "pdbonly");
        }

        internal static bool PostValidateArguments(LolCompilerArguments arguments)
        {
            // Are there any files?
            if (arguments.sources.Length == 0)
            {
                Console.Error.WriteLine("lolc error: No source files specified");
                return false; 
            }

            // Do they exist?
            foreach (string file in arguments.sources)
            {
                if (!File.Exists(file))
                {
                    Console.Error.WriteLine("lolc error: Source file '{0}' does not exist.", file);
                    return false; 
                }
            }

            // Is the target platform valid? 
            if (!String.IsNullOrEmpty(arguments.platform) && !IsPlatformValid(arguments.platform))
            {
                Console.Error.WriteLine("lolc error: Platform '{0}' is not valid", arguments.platform);
                return false; 
            }

            // Is the target assembly type valid?
            if (!String.IsNullOrEmpty(arguments.target) && !IsTargetValid(arguments.target))
            {
                Console.Error.WriteLine("lolc error: Target '{0}' is not valid", arguments.platform);
                return false; 
            }

            return true; 
        }

        #endregion
    }

}
