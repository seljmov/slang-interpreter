﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Semantic_Interpreter.Core;
using Semantic_Interpreter.Library;

namespace Semantic_Interpreter.Parser
{
    public class Parser
    {
        private static readonly Token Eof = new(TokenType.Eof, "");

        private readonly List<Token> _tokens;
        private readonly int _length;

        private int _pos;
        private readonly SemanticTree _semanticTree = new();
        private readonly Stack<SemanticOperator> _operatorsStack = new();
        private delegate bool ParseBlockPredicate();

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _length = tokens.Count;
            _pos = 0;
        }

        public SemanticTree Parse()
        {
            SemanticOperator lastOperator = null;
            
            while (!Match(TokenType.Eof))
            {
                if (Match(TokenType.End)) return _semanticTree;

                var prevOperator = lastOperator;
                var newOperator = ParseOperator();
                var asChild = false;
                // Операторы Module и Beginning имеют индивидуальные правила, по которым они вставляются
                // в дерево, а остальные операторы вставляются по общим правилам (кейс default)
                switch (newOperator)
                {
                    case Module:
                        _operatorsStack.Push(newOperator);
                        break;
                    case Start:
                        asChild = _operatorsStack.Peek().Child == null;
                        _operatorsStack.Push(newOperator);
                        break;
                    default:
                        asChild = _operatorsStack.Peek().Child == null;
                        break;
                }
                
                _semanticTree.InsertOperator(prevOperator, newOperator, asChild);
                lastOperator = newOperator;
            }

            return _semanticTree;
        }

        private SemanticOperator ParseOperator()
        {
            var token = Get();
            _pos++;
            var @operator = token.Type switch
            {
                TokenType.Module => ParseModuleOperator(),
                TokenType.Start => ParseStartOperator(),
                TokenType.VisibilityType => ParseVisibilityOperator(),
                TokenType.Return => ParseReturnOperator(),
                TokenType.While => ParseWhileOperator(),
                TokenType.If => ParseIfOperator(),
                TokenType.Call => ParseCallOperator(),
                TokenType.Variable => ParseVariableOperator(),
                TokenType.Let => ParseLetOperator(),
                TokenType.Input => ParseInputOperator(),
                TokenType.Output => ParseOutputOperator(),
                _ => null
            };
            @operator.Parent = _operatorsStack.Count > 0 ? _operatorsStack.Peek() : null;
            return @operator;
        }
        
        private SemanticOperator ParseModuleOperator()
        {
            var name = Consume(TokenType.Word).Text;
            return new Module(name);
        }

        private SemanticOperator ParseStartOperator() => new Start();

        private SemanticOperator ParseVisibilityOperator()
        {
            SemanticOperator @operator = null;
            if (Match(TokenType.Function)) @operator = ParseFunctionOperator();
            if (Match(TokenType.Procedure)) @operator = ParseProcedureOperator();

            return @operator;
        }

        private SemanticOperator ParseReturnOperator()
        {
            var expression = ParseExpression();
            Consume(TokenType.Semicolon);

            return new Return(expression);
        }

        private List<Parameter> GetFunctionParameters()
        {
            List<Parameter> parameters = null;

            Consume(TokenType.LParen);
            while (!Match(TokenType.RParen))
            {
                parameters ??= new List<Parameter>();
                var parameter = new Parameter();
                var parameterTypeToken = Consume(TokenType.ParameterType);
                var variableTypeToken = Consume(TokenType.Word);
                var variableNameToken = Consume(TokenType.Word);
                parameter.ParameterType = parameterTypeToken.Text == "in" ? ParameterType.In : ParameterType.Var;
                parameter.VariableType = variableTypeToken.Text switch
                {
                    "integer" => VariableType.Integer,
                    "real" => VariableType.Real,
                    "boolean" => VariableType.Boolean,
                    "char" => VariableType.Char,
                    _ => VariableType.String
                };
                parameter.Name = variableNameToken.Text;
                parameters.Add(parameter);
                Match(TokenType.Comma);
            }
            
            return parameters;
        }
        
        private SemanticOperator ParseFunctionOperator()
        {
            var function = new Function();
            var visibilityToken = Get(-2);
            var visibilityType = visibilityToken.Text == "public" ? VisibilityType.Public : VisibilityType.Private;
            var name = Get().Text;
            _pos++;
            
            var parameters = GetFunctionParameters();
            
            Consume(TokenType.Colon);
            var returnType = Consume(TokenType.Word).Text switch
            {
                "integer" => VariableType.Integer,
                "real" => VariableType.Real,
                "boolean" => VariableType.Boolean,
                "char" => VariableType.Char,
                _ => VariableType.String
            };

            function.Parent = _operatorsStack.Peek();
            function.Parameters = parameters;
            
            ParseBlock(function, () => !Match(TokenType.End));
            
            Consume(TokenType.Word);   // Skip function name
            Consume(TokenType.Semicolon);   // Skip ;
            
            function.VisibilityType = visibilityType;
            function.Name = name;
            function.Parameters = parameters;
            // function.Operators = block;
            function.ReturnType = returnType;
            // var id = name;
            ((Module) _semanticTree.Root).FunctionStorage.Add(name, new DefineFunction(function));
            return function;
        }

        private SemanticOperator ParseProcedureOperator()
        {
            var procedure = new Procedure();
            var visibilityToken = Get(-2);
            var visibilityType = visibilityToken.Text == "public" ? VisibilityType.Public : VisibilityType.Private;
            var name = Get().Text;
            _pos++;
            
            var parameters = GetFunctionParameters();
            
            procedure.Parent = _operatorsStack.Peek();
            procedure.Parameters = parameters;
            
            ParseBlock(procedure, () => !Match(TokenType.End));
            
            Consume(TokenType.Word);   // Skip procedure name
            Consume(TokenType.Semicolon);   // Skip ;

            procedure.VisibilityType = visibilityType;
            procedure.Name = name;
            procedure.Parameters = parameters;
            // procedure.Operators = block;
            // var id = name;
            ((Module) _semanticTree.Root).FunctionStorage.Add(name, new DefineFunction(procedure));
            return procedure;
        }

        private void ParseBlock(MultilineOperator multiline, ParseBlockPredicate predicate)
        {
            multiline.Operators.Parent = multiline;
            _operatorsStack.Push(multiline);
            while (predicate())
            {
                multiline.Operators.Add(ParseOperator());
            }
            _operatorsStack.Pop();
        }
        
        private SemanticOperator ParseWhileOperator()
        {
            var whileOperator = new While();
            var expression = ParseExpression();
            Consume(TokenType.Word); // Skip repeat
            
            ParseBlock(whileOperator, () => !Match(TokenType.End));

            Consume(TokenType.While);   // Skip while word
            Consume(TokenType.Semicolon);   // Skip ;

            whileOperator.Expression = expression;
            return whileOperator;
        }

        // TODO: вытащить парсинг else-if и else в отдельные функции
        private SemanticOperator ParseIfOperator()
        {
            var ifOperator = new If();
            var expression = ParseExpression();
            Consume(TokenType.Word);    // Skip then
            
            // var ifBlock = new BlockSemanticOperator();
            List<ElseIf> elseIfs = null;
            Else elseOperator = null;
            
            var currentToken = Get();
            while (!Match(TokenType.End))
            {
                if (currentToken.Type != TokenType.Else)
                {
                    ParseBlock(ifOperator, () => !Next(TokenType.Else) && !Next(TokenType.End));
                }
                else
                {
                    Consume(TokenType.Else);
                    if (Match(TokenType.If))
                    {
                        var elseIfOperator = new ElseIf();
                        elseIfs ??= new List<ElseIf>();
                        var elseIfExpr = ParseExpression();
                        Consume(TokenType.Word);    // Skip then
                        
                        // var elseIfBlock = new BlockSemanticOperator();
                        
                        ParseBlock(elseIfOperator, () => !Next(TokenType.Else) && !Next(TokenType.End));
                        
                        // elseIfBlock.Parent = elseIfOperator;
                        elseIfOperator.Expression = elseIfExpr;
                        // elseIfOperator.Operators = elseIfBlock;
                        elseIfOperator.Parent = _operatorsStack.Peek();
                        elseIfs.Add(elseIfOperator);
                    }
                    else
                    {
                        elseOperator = new Else();
                        
                        // var elseBlock = new BlockSemanticOperator();
                        
                        ParseBlock(elseOperator, () => !Next(TokenType.End));

                        // elseBlock.Parent = elseOperator;
                        // elseOperator.Operators = elseBlock;
                        elseOperator.Parent = _operatorsStack.Peek();
                    }
                }
                
                currentToken = Get();
            }
            
            Consume(TokenType.If);      // Skip if word
            Consume(TokenType.Semicolon);   // Skip ;

            // ifBlock.Parent = ifOperator;
            ifOperator.Expression = expression;
            // ifOperator.Operators = ifBlock;
            ifOperator.ElseIfs = elseIfs;
            ifOperator.Else = elseOperator;
            return ifOperator;
        }
        
        private SemanticOperator ParseCallOperator()
        {
            var functionName = Consume(TokenType.Word).Text;
            ParseFunctionArguments(functionName);
            Consume(TokenType.Semicolon);
            
            return new Call(functionName);
        }

        private void ParseFunctionArguments(string functionName)
        {
            var module = (Module) _semanticTree.Root;
            var function = module.FunctionStorage.At(functionName).BaseFunction;
            
            var arguments = new List<string>();
            Consume(TokenType.LParen);
            while (!Match(TokenType.RParen))
            {
                var name = Consume(TokenType.Word).Text;
                arguments.Add(name);
                Match(TokenType.Comma);
            }
            // Consume(TokenType.Semicolon);
            
            if (function.Parameters != null)
            {
                if (function.Parameters.Count != arguments.Count)
                {
                    // Проверяем не указанные параметры
                    var message = $"Для {functionName} не были указаны аргументы - ";
                    for (var i = arguments.Count; i < function.Parameters.Count; i++)
                    {
                        message += $"{function.Parameters[i].Name}";
                        if (i != function.Parameters.Count - 1)
                            message += ", ";
                    }
                    
                    throw new Exception($"{message}.");
                }

                for (var i = 0; i < arguments.Count; i++)
                {
                    var id = GetVariableScopeId(arguments[i]);
                    function.Parameters[i].VariableId = id;
                    function.Parameters[i].Parent = function;
                }
            }
        }
        
        // TODO: вытащить парсинг массива и переменной в отдельные функции
        private SemanticOperator ParseVariableOperator()
        {
            Consume(TokenType.Minus);  // Skip -
            var type = Consume(TokenType.Word).Text switch
            {
                "integer" => VariableType.Integer,
                "real" => VariableType.Real,
                "boolean" => VariableType.Boolean,
                "char" => VariableType.Char,
                "string" => VariableType.String,
                _ => VariableType.Array
            };

            if (type == VariableType.Array)
            {
                List<ArrayValue> list = new();

                while (type == VariableType.Array)
                {
                    Consume(TokenType.LBracket);
                    var expression = ParseExpression();
                    // TODO: добавить проверку на отрицательность
                    var size = expression.Eval() is IntegerValue value 
                        ? value.AsInteger() 
                        : throw new Exception("Только целое число может быть размером массива");
                    Consume(TokenType.RBracket);

                    var array = new ArrayValue(size);
                    list.Add(array);
                    
                    // TODO: вынести определение типа в самостоятельную функцию
                    type = Consume(TokenType.Word).Text switch
                    {
                        "integer" => VariableType.Integer,
                        "real" => VariableType.Real,
                        "boolean" => VariableType.Boolean,
                        "char" => VariableType.Char,
                        "string" => VariableType.String,
                        _ => VariableType.Array
                    };
                }

                for (int i = 0; i < list.Count-1; i++)
                {
                    for (int j = 0; j < list[i].Size; j++)
                    {
                        var copyArr = new ArrayValue(list[i + 1].AsArray());
                        list[i].Set(j, copyArr);
                    }
                }

                var name = Consume(TokenType.Word).Text;
                Consume(TokenType.Semicolon);
                
                var parentId = ((MultilineOperator) _operatorsStack.Peek()).OperatorId;
                var variableId = $"{parentId}^{name}";

                var arrayExpression = new ArrayExpression(name, type, list.First());
                var variable = new Variable(type, name, variableId, arrayExpression);
                // VariableStorage.Add(variableId, variable);
                
                return variable;
            }
            else
            {
                var name = Get().Text;
                IExpression expression = null;
                if (Match(TokenType.Word) && Get().Type == TokenType.Assign)
                {
                    Consume(TokenType.Assign);
                    expression = ParseExpression();
                }
                Consume(TokenType.Semicolon);
                
                var parentId = ((MultilineOperator) _operatorsStack.Peek()).OperatorId;
                var variableId = $"{parentId}^{name}";
            
                var variable = new Variable(type, name, variableId, expression);
                // VariableStorage.Add(variableId, variable);
                
                return variable;
            }
        }
        
        private SemanticOperator ParseLetOperator()
        {
            var name = Consume(TokenType.Word).Text;
            var scopeId = GetVariableScopeId(name);
            List<IExpression> indexes = null;
            while (Next(TokenType.LBracket))
            {
                indexes ??= new List<IExpression>();
                Consume(TokenType.LBracket);
                var index = ParseExpression();
                indexes.Add(index);
                Consume(TokenType.RBracket);
            }
            Consume(TokenType.Assign);
            var expression = ParseExpression();
            Consume(TokenType.Semicolon);
            
            return new Let(scopeId, expression, indexes);
        }

        private SemanticOperator ParseInputOperator()
        {
            var name = Consume(TokenType.Word).Text;
            var scopeId = GetVariableScopeId(name);
            Consume(TokenType.Semicolon);

            return new Input(scopeId);
        }

        private SemanticOperator ParseOutputOperator()
        {
            var expression = ParseExpression();
            Consume(TokenType.Semicolon);

            return new Output(expression);
        }
        
        private string GetVariableScopeId(string name)
        {
            var stack1 = new Stack<SemanticOperator>(_operatorsStack.Reverse());
            while (stack1.Count > 0)
            {
                if (stack1.Pop() is BaseFunction function && function.ParameterIsExist(name))
                {
                    return name;
                }
            }
            
            var stack2 = new Stack<SemanticOperator>(_operatorsStack.Reverse());
            while (stack2.Count > 0)
            {
                var t = (MultilineOperator) stack2.Pop();
                var parentId = t.OperatorId;
                var variableId = $"{parentId}^{name}";

                if (t.Operators != null)
                {
                    var any = t.Operators.Operators.Any(x => x is Variable v && v.Id == variableId);
                    if (any)
                    {
                        return variableId;
                    }
                }
                
                var variable = _semanticTree.FindVariableWithId(variableId);
                if (variable != null)
                {
                    return variableId;
                }
                
                /*
                if (VariableStorage.IsExist(variableId))
                {
                    return variableId;
                }
                */
            }

            throw new Exception($"Переменной/параметра {name} не существует!");
        }

        private IExpression ParseExpression()
        {
            return LogicalOr();
        }

        private IExpression LogicalOr()
        {
            var result = LogicalAnd();

            while (true)
            {
                if (Match(TokenType.OrOr))
                {
                    result = new ConditionalExpression(TokenType.OrOr, result, LogicalAnd());
                    continue;
                }
                break;
            }

            return result;
        }

        private IExpression LogicalAnd()
        {
            var result = Equality();

            while (true)
            {
                if (Match(TokenType.AndAnd))
                {
                    result = new ConditionalExpression(TokenType.AndAnd, result, Equality());
                    continue;
                }
                
                break;
            }

            return result;
        }

        private IExpression Equality()
        {
            var result = Conditional();

            if (Match(TokenType.Equal))
            {
                return new ConditionalExpression(TokenType.Equal, result, Conditional());
            }

            if (Match(TokenType.NotEqual))
            {
                return new ConditionalExpression(TokenType.NotEqual, result, Conditional());
            }

            return result;
        }

        private IExpression Conditional()
        {
            var result = Additive();

            while (true)
            {
                if (Match(TokenType.Less))
                {
                    result = new ConditionalExpression(TokenType.Less, result, Additive());
                    continue;
                }

                if (Match(TokenType.LessOrEqual))
                {
                    result = new ConditionalExpression(TokenType.LessOrEqual, result, Additive());
                    continue;
                }

                if (Match(TokenType.Greater))
                {
                    result = new ConditionalExpression(TokenType.Greater, result, Additive());
                    continue;
                }

                if (Match(TokenType.GreaterOrEqual))
                {
                    result = new ConditionalExpression(TokenType.GreaterOrEqual, result, Additive());
                    continue;
                }
                
                break;
            }

            return result;
        }

        private IExpression Additive()
        {
            var result = Multiplicative();
            
            while (true)
            {
                if (Match(TokenType.Plus))
                {
                    result = new BinaryExpression(Operations.Plus, result, Multiplicative());
                    continue;
                }

                if (Match(TokenType.Minus))
                {
                    result = new BinaryExpression(Operations.Minus, result, Multiplicative());
                    continue;
                }
                
                break;
            }

            return result;
        }

        private IExpression Multiplicative()
        {
            var result = Unary();

            while (true)
            {
                if (Match(TokenType.Multiply))
                {
                    result = new BinaryExpression(Operations.Multiply, result, Unary());
                    continue;
                }

                if (Match(TokenType.Divide))
                {
                    result = new BinaryExpression(Operations.Divide, result, Unary());
                    continue;
                }
                
                break;
            }

            return result;
        }

        private IExpression Unary()
        {
            return Match(TokenType.Minus) 
                ? new UnaryExpression(Operations.Minus, Primary()) 
                : Primary();
        }
        
        private IExpression Primary()
        {
            var current = Get();
            _pos++;
            switch (current.Type)
            {
                case TokenType.Boolean: return new ValueExpression(current.Text == "true");
                case TokenType.Char: return new ValueExpression(Convert.ToChar(current.Text));
                case TokenType.Text: return new ValueExpression(current.Text);
                case TokenType.Word:
                    if (Next(TokenType.LParen))
                    {
                        var functionName = current.Text;
                        ParseFunctionArguments(functionName);
                        
                        var functionDefine = ((Module) _semanticTree.Root).FunctionStorage.At(functionName);

                        return new CalculatedExpression(functionDefine);
                    }
                    else if (Next(TokenType.LBracket))
                    {
                        var arrayName = current.Text;
                        var scopeId = GetVariableScopeId(arrayName);
                        
                        List<IExpression> indexes = null;
                        while (Next(TokenType.LBracket))
                        {
                            indexes ??= new List<IExpression>();
                            Consume(TokenType.LBracket);
                            var index = ParseExpression();
                            indexes.Add(index);
                            Consume(TokenType.RBracket);
                        }
                        
                        // var arrayExpression = (ArrayExpression) VariableStorage.At(scopeId).Expression;
                        var arrayExpression = (ArrayExpression) _semanticTree.FindVariableWithId(scopeId).Expression;

                        return new ArrayAccessExpression(indexes, arrayExpression);

                    }
                    var name = GetVariableScopeId(current.Text);
                    var stack1 = new Stack<SemanticOperator>(_operatorsStack.Reverse());
                    while (stack1.Count > 0)
                    {
                        if (stack1.Pop() is BaseFunction function && function.ParameterIsExist(name))
                        {
                            var parameter = function.GetParameterWithName(name);
                            return new CalculatedExpression(parameter);
                        }
                    }

                    // var variable2 = VariableStorage.At(name);
                    var variable2 = _semanticTree.FindVariableWithId(name);
                    if (variable2 == null)
                    {
                        var stack2 = new Stack<SemanticOperator>(_operatorsStack.Reverse());
                        while (stack2.Count > 0)
                        {
                            var parent = (MultilineOperator) stack2.Pop();
                            if (parent.Operators != null)
                            {
                                var any = parent.Operators.Operators.Any(x => x is Variable v && v.Id == name);
                                if (any)
                                {
                                    variable2 = (Variable) parent.Operators.Operators.Single(x => x is Variable v && v.Id == name);
                                    return new CalculatedExpression(variable2);
                                }
                            }
                        }
                    }
                    return new CalculatedExpression(variable2);
                case TokenType.Number:
                    // Если точки нет, то число целое, иначе - вещественное
                    if (!current.Text.Contains('.'))
                        return new ValueExpression(Convert.ToInt32(current.Text));
                
                    IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };
                    return new ValueExpression(Convert.ToDouble(current.Text, formatter));
                case TokenType.LParen:
                    var result = ParseExpression();
                    Match(TokenType.RParen);
                    return result;
            }
            
            throw new Exception("Неизвестный оператор.");
        }
        
        private Token Consume(TokenType type)
        {
            var current = Get();
            if (type != current.Type) 
                throw new Exception($"Токен '{current}' не найден ({type}).");
            
            _pos++;
            return current;
        }

        private bool Match(TokenType type)
        {
            var current = Get();
            if (type != current.Type) 
                return false;
            
            _pos++;
            return true;
        }
        
        private bool Next(TokenType type)
        {
            return Get().Type == type;
        }

        private Token Get(int i = 0)
        {
            var position = _pos + i;
            return position >= _length 
                ? Eof 
                : _tokens[position];
        }
    }
}