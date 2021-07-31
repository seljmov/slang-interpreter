﻿using System;
using Semantic_Interpreter.Core.Items;

namespace Semantic_Interpreter.Core
{
    public class DefineFunction : IFunction, ICalculated
    {
        public DefineFunction(BaseFunction baseFunction)
        {
            BaseFunction = baseFunction;
        }
        
        public BaseFunction BaseFunction { get; }

        public IValue Execute(params IValue[] args)
        {
            BaseFunction.Parameters?.ForEach(x => x.Execute());

            try
            {
                BaseFunction.Execute();
                return new IntegerValue(0);
            }
            catch (Exception)
            {
                return ((IHaveReturn) BaseFunction).Return.Result;
            }
        }

        public IValue Calculate() => Execute();
    }
}