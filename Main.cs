using System;
using System.IO;
using System.Collections.Generic;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("[ERR] Incorrect usage; Correct usage:");
            Console.Error.WriteLine("barec <input.hy>");
            return 1;
        }

        string contents = File.ReadAllText(args[0]);

        var tokenizer = new Tokenizer(contents);
        List<Token> tokens = tokenizer.Tokenize();

        var parser = new Parser(tokens);
        NodeProg prog = parser.ParseProg();

        if (prog == null)
        {
            Console.Error.WriteLine("[ERR] Invalid program");
            return 1;
        }

        var generator = new Generator(prog);
        File.WriteAllText("out.asm", generator.GenProg());

        System.Diagnostics.Process.Start("nasm", "-felf64 out.asm").WaitForExit();
        System.Diagnostics.Process.Start("ld", "-o out out.o").WaitForExit();

        return 0;
    }
}