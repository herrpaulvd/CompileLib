using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using CompileLib.Common;

namespace CompileLib.ParserTools
{
    internal enum LRActionType
    {
        Error = 0,
        Carry, // nextState
        Fold, // count + nt + handler
        Accept // -
    }

    internal struct LRAction
    {
        public LRActionType Type { get; }
        private readonly int arg1;
        private readonly int arg2;
        private readonly IProductionHandler? arg3;
        public bool IsError => Type == LRActionType.Error;

        private LRAction(LRActionType type, int arg1, int arg2, IProductionHandler? arg3)
        {
            Type = type;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
        }
        public static LRAction CreateCarry(int state)
            => new(LRActionType.Carry, state, 0, null);
        public static LRAction CreateFold(int count, int nt, IProductionHandler productionHandler)
            => new(LRActionType.Fold, count, nt, productionHandler);
        public static readonly LRAction AcceptAction
            = new(LRActionType.Accept, 0, 0, null);
        public static readonly LRAction ErrorAction
            = new(LRActionType.Error, 0, 0, null);

        public int NextState => arg1;
        public int Count => arg1;
        public int NT => arg2;
        public IProductionHandler ProductionHandler
        {
            get
            {
                Debug.Assert(arg3 is not null);
                return arg3;
            }
        }
    }

    /// <summary>
    /// LR(1)-automata
    /// </summary>
    internal class LRMachine
    {
        private readonly LRAction[][] action;
        private readonly int[][] @goto;
        private readonly List<(int, IErrorHandler, int)>[] errorHandlers;
        private readonly Token finalToken;
        private readonly Func<int?, string> tokenTypeToStr;
        private readonly Func<int?, string> nonTokenTypeToStr;

        public LRMachine(
            LRAction[][] action, 
            int[][] @goto, 
            List<(int, IErrorHandler, int)>[] errorHandlers, 
            Token finalToken,
            Func<int?, string> tokenTypeToStr,
            Func<int?, string> nonTokenTypeToStr
            )
        {
            this.action = action;
            this.@goto = @goto;
            this.errorHandlers = errorHandlers;
            this.finalToken = finalToken;
            this.tokenTypeToStr = tokenTypeToStr;
            this.nonTokenTypeToStr = nonTokenTypeToStr;
        }

        public AnyParsed Analyze(IEnumerable<Token> tokens)
        {
            Stack<int> states = new();
            states.Push(0);
            Stack<AnyParsed> elements = new();

            void PushRange(IEnumerable<AnyParsed> basis, IEnumerable<int> savedStates)
            {
                foreach(var e in basis) elements.Push(e);
                foreach(var i in savedStates) states.Push(i);
            }

            void Perform(Token t, bool errorAnyway = false)
            {
                LRAction a;
                if(errorAnyway || !t.Type.HasValue || (a = action[states.Peek()][t.Type.Value]).IsError)
                {
                    foreach(var (count, handler, errorNT) in errorHandlers[states.Peek()])
                    {
                        var basis = new AnyParsed[count];
                        var savedStates = new int[count];
                        for (int i = 0; i < count; i++)
                        {
                            basis[count - 1 - i] = elements.Pop();
                            savedStates[count - 1 - i] = states.Pop();
                        }
                        var decision = handler.Handle(basis, new Parsing.Parsed<string>(
                            tokenTypeToStr(t.Type),
                            t.Self,
                            t.Line,
                            t.Column));
                        switch (decision.Decision)
                        {
                            case ErrorHandlingDecisionType.Skip:
                                PushRange(basis, savedStates);
                                return;
                            case ErrorHandlingDecisionType.Stop:
                                throw new LRStopException(t);
                            case ErrorHandlingDecisionType.Before:
                                PushRange(basis, savedStates);
                                Perform(decision.TokenArgument);
                                Perform(t);
                                return;
                            case ErrorHandlingDecisionType.Instead:
                                PushRange(basis, savedStates);
                                Perform(decision.TokenArgument);
                                return;
                            case ErrorHandlingDecisionType.FoldAndRaise:
                                states.Push(@goto[states.Peek()][errorNT]);
                                elements.Push(new AnyParsed(
                                    Parsing.SpecialTags.TAG_UNKNOWN, 
                                    decision.Argument,
                                    count == 0 ? t.Line : basis[0].Line,
                                    count == 0 ? t.Column : basis[0].Column));
                                Perform(t, true);
                                return;
                            case ErrorHandlingDecisionType.NextHandler:
                                PushRange(basis, savedStates);
                                break; // exit switch but continue loop
                            default:
                                Debug.Fail("ErrorHandlingDecision.???");
                                return;
                        }
                    }
                    return;
                }

                switch(a.Type)
                {
                    case LRActionType.Carry:
                        states.Push(a.NextState);
                        elements.Push(new AnyParsed(tokenTypeToStr(t.Type), t.Self, t.Line, t.Column));
                        return;
                    case LRActionType.Fold:
                        AnyParsed[] basis = new AnyParsed[a.Count];
                        for(int i = 0; i < a.Count; i++)
                        {
                            states.Pop();
                            basis[a.Count - 1 - i] = elements.Pop();
                        }
                        states.Push(@goto[states.Peek()][a.NT]);
                        
                        var tag = nonTokenTypeToStr(a.NT);
                        var handleResult = a.ProductionHandler.Handle(basis, ref tag);
                        elements.Push(new AnyParsed(
                            tag,
                            handleResult,
                            a.Count == 0 ? t.Line : basis[0].Line,
                            a.Count == 0 ? t.Column : basis[0].Column));
                        Perform(t);
                        return;
                    case LRActionType.Accept:
                        return;
                    default:
                        Debug.Fail("LRActionType.???");
                        return;
                }
            }

            foreach(var t in tokens)
                Perform(t);
            Perform(finalToken);
            if (elements.Count == 0) throw new Parsing.ParsingException("Empty LR stack");
            return elements.Peek();
        }
    }
}
