using System;
using System.Collections.Generic;
using System.Text;

namespace notdot.LOLCode
{
    class LOLProgram
    {
        public static int Main(string[] args)
        {
            Parser p = new Parser(new Scanner(args[0]));
            p.Parse();
            for (int i = 0; i < p.errors.Count; i++)
                Console.WriteLine(p.errors[i]);

            if (p.errors.Count > 0)
                Console.ReadLine();

            return 0;
        }
    }
}
