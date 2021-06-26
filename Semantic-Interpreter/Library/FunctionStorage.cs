﻿using System;
using System.Collections.Generic;
using Semantic_Interpreter.Core;

namespace Semantic_Interpreter.Library
{
    public class FunctionStorage
    {
        private readonly Dictionary<string, DefineFunction> _functions = new();

        public bool IsExist(string name) => _functions.ContainsKey(name);

        public DefineFunction At(string name)
            => IsExist(name) 
                ? _functions[name] 
                : throw new Exception("Функции/Процедуры с таким именем не существует!");
        
        public void Add(string name, DefineFunction function)
        {
            if (IsExist(name))
            {
                throw new Exception("Функция/Процедура с таким именем уже существует!");
            }

            _functions.Add(name, function);
        }
    }
}