﻿namespace Semantic_Interpreter.Core
{
    public interface IValue
    {
        int AsInteger();

        double AsReal();

        bool AsBoolean();

        string AsString();
    }
}