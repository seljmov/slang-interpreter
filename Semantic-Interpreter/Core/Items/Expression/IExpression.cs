﻿namespace Semantic_Interpreter.Core
{
    public interface IExpression
    {
        IValue Eval();
    }
}