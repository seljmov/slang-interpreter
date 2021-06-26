﻿using System;

namespace Semantic_Interpreter.Core
{
    public class DefineFunction
    {
        public DefineFunction(BaseFunction baseFunction)
        {
            BaseFunction = baseFunction;
        }
        
        public BaseFunction BaseFunction { get; set; }

        public IValue Execute()
        {
            try
            {
                BaseFunction.Execute();
                return new IntegerValue(0);
            }
            catch (Exception e)
            {
                return ((Function) BaseFunction).Return.Result;
            }
        }
    }
}