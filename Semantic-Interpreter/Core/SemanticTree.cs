﻿namespace Semantic_Interpreter.Core
{
    public class SemanticTree
    {
        private int _count = 0;
        public SemanticOperator Root { get; private set; }

        public void InsertOperator(SemanticOperator prevOperator, SemanticOperator newOperator, bool AsChild)
        {
            if (prevOperator == null && newOperator is Module)
            {
                InsertRoot(newOperator);
            }
            else if (newOperator is Beginning beginning)
            {
                ((Module) FindOperator(prevOperator)).SetBeginning(beginning);
            }
            else if (AsChild)
            {
                InsertOperatorAsChild(newOperator, prevOperator);
            }
            else
            {
                InsertOperatorAsNext(newOperator, prevOperator);
            }

            _count++;
        }

        private SemanticOperator FindOperator(SemanticOperator @operator)
        {
            var index = 0;
            SemanticOperator curr = Root;
            SemanticOperator parent = Root;
            while (index != _count)
            {
                if (curr == @operator)
                {
                    return curr;
                }
                else
                {
                    if (curr.Child != null)
                    {
                        parent = curr;
                        curr = curr.Child;
                    }
                    else
                    {
                        if (curr.Next != null)
                        {
                            curr = curr.Next;
                        }
                        else
                        {
                            curr = parent.Next;

                            while (curr == null)
                            {
                                parent = parent.Parent;
                                curr = parent.Next;
                            }
                        }
                    }
                }

                index++;
            }
            
            return null;
        }

        private void InsertRoot(SemanticOperator @operator) => Root = @operator;

        private void InsertOperatorAsPrevious(SemanticOperator newOperator, SemanticOperator previous)
        {
            if (previous.Previous != null)
            {
                previous.Previous.Next = newOperator;
                newOperator.Previous = previous.Previous;
                previous.Previous = newOperator;
                newOperator.Next = previous;
            }
            else
            {
                newOperator.Next = previous.Parent.Child;
                previous.Parent.Child = newOperator;
                previous.Previous = newOperator;
                newOperator.Parent = previous.Parent;
                previous.Parent = null;
            }

            newOperator.Parent = previous.Parent;
        }

        private void InsertOperatorAsNext(SemanticOperator newOperator, SemanticOperator next)
        {
            if (next.Next != null)
            {
                next.Next.Previous = newOperator;
                newOperator.Next = next.Next;
            }

            next.Next = newOperator;
            newOperator.Previous = next;
            newOperator.Parent = next.Parent;
        }

        private void InsertOperatorAsChild(SemanticOperator newOperator, SemanticOperator child)
        {
            if (child.Child != null)
            {
                newOperator.Next = child.Child;
                child.Child.Previous = newOperator;
                child.Child.Parent = null;
            }

            child.Child = newOperator;
            newOperator.Parent = child;
            newOperator.Parent = child;
        }
    }
}