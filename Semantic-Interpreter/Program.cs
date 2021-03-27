﻿using System;
using System.IO;
using Semantic_Interpreter.Core;
using Semantic_Interpreter.Parser;

namespace Semantic_Interpreter
{
    public static class Program
    {
        private const string Demo = @"C:\Github\Semantic-Interpreter\Semantic-Interpreter\Demo\";
        private const string Filename = "program2.txt";

        public static void Main(string[] args)
        {

            using var reader = new StreamReader(Demo + Filename);
            var program = reader.ReadToEnd();

            var lexer = new Lexer(program);
            var tokens = lexer.Tokenize();

            var operators = new Parser.Parser(tokens).Parse();
            foreach (var @operator in operators)
            {
                Console.WriteLine(@operator);
            }
            
            Console.WriteLine("\n-----------------\n");
            
            foreach (var @operator in operators)
            {
                @operator.Execute();
            }
        }
    }
}