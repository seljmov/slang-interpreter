﻿using Semantic_Interpreter.Library;

namespace Semantic_Interpreter.Parser.Expressions
{
    public class ValueExpression : IExpression
    {
        public ValueExpression(int value) => Value = new IntegerValue(value);

        public ValueExpression(double value) => Value = new RealValue(value);
        
        public ValueExpression(string value) => Value = new StringValue(value);

        private IValue Value { get; set; }

        public IValue Eval() => Value;

        public override string ToString() => Value.AsString();
    }
}