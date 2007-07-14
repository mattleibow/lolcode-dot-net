LOLCode.net, Revision 34
------------------------
LoLCode.net is a .net framework compiler for the language LOLCode (http://www.lolcode.com).
This distribution includes the compiler (lolc.exe), compiler library (lolcode.net.dll),
standard library (stdlol.dll) and code samples.

Up-to-date releases, information, and source code for LOLCode.net can be found at
http://code.google.com/p/lolcode-dot-net/

The lolc compiler is simple to use. At its most basic, just invoke it with the filename of
the LOLCode program you want to compile:

  lolc.exe fulltest.lol

lolc will compile your file, and, if successful, output an EXE file with the same name.

For more advanced usage, including debug builds and specifying the name of the output file,
invoke lolc with the /? option:

  lolc.exe /?

If you instruct lolc to build a program with debug information (/debug+), you can debug your
LOLCode program in Visual Studio. Simply choose Debug->Attach in Visual Studio to attach
to a running LOLCode program. You can then open the source file and set breakpoints, step,
evaluate variables, etc, as you would in any Visual Studio language.

If you encounter bugs, issues, or would like to make enhancement suggestions, please add
them to the issue tracker on the LOLCode.net project page.
