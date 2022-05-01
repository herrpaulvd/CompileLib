using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using TransList = System.Collections.Generic.List<(System.Predicate<char>, int)>;

namespace CompileLib.LexerTools
{
    // TODO: optimizations
    internal class SmartFSMBuilder
    {
        private readonly List<TransList> transition = new();
        private readonly List<bool> isFinal = new();

        private SmartFSMBuilder() { }

        public static SmartFSMBuilder CreateBySinglePredicate(Predicate<char> p)
        {
            SmartFSMBuilder result = new();
            result.transition.Add(new TransList { (p, 1) });
            result.transition.Add(new TransList());
            result.isFinal.Add(false);
            result.isFinal.Add(true);
            return result;
        }

        private static Func<TransList, TransList> MakeConvertionFunction(int offset)
        {
            return transList => transList.Select(tuple => (tuple.Item1, tuple.Item2 + offset)).ToList();
        }

        private static readonly Func<TransList, TransList> convFunc1 = MakeConvertionFunction(1);

        private static TransList Copy(TransList transList)
        {
            return transList.ToList();
        }

        public static SmartFSMBuilder CreateUnion(SmartFSMBuilder a, SmartFSMBuilder b)
        {
            var aConvFunc = convFunc1;
            var bConvFunc = MakeConvertionFunction(1 + a.isFinal.Count);

            SmartFSMBuilder result = new();
            result.transition.Add(aConvFunc(a.transition[0]));
            result.transition[0].AddRange(bConvFunc(b.transition[0]));
            result.transition.AddRange(a.transition.Select(aConvFunc));
            result.transition.AddRange(b.transition.Select(bConvFunc));

            result.isFinal.Add(a.isFinal[0] || b.isFinal[0]);
            result.isFinal.AddRange(a.isFinal);
            result.isFinal.AddRange(b.isFinal);
            return result;
        }

        public static SmartFSMBuilder CreateConcatenation(SmartFSMBuilder a, SmartFSMBuilder b)
        {
            var bConvFunc = MakeConvertionFunction(a.isFinal.Count);
            
            SmartFSMBuilder result = new();
            result.transition.AddRange(a.transition.Select(Copy));
            result.transition.AddRange(b.transition.Select(bConvFunc));
            for (int i = 0; i < a.transition.Count; i++)
                if (a.isFinal[i])
                    result.transition[i].AddRange(bConvFunc(b.transition[0]));

            if (b.isFinal[0])
                result.isFinal.AddRange(a.isFinal);
            else
                result.isFinal.AddRange(Enumerable.Repeat(false, a.isFinal.Count));
            result.isFinal.AddRange(b.isFinal);
            return result;
        }

        public static SmartFSMBuilder CreateOptional(SmartFSMBuilder a)
        {
            if (a.isFinal[0])
                return a;

            var convFunc = convFunc1;
            var result = new SmartFSMBuilder();
            result.transition.Add(convFunc(a.transition[0]));
            result.transition.AddRange(a.transition.Select(convFunc));

            result.isFinal.Add(true);
            result.isFinal.AddRange(a.isFinal);
            return result;
        }

        public static SmartFSMBuilder CreatePlusClosure(SmartFSMBuilder a)
        {
            SmartFSMBuilder result = new();
            result.transition.AddRange(a.transition.Select(Copy));
            for (int i = 1; i < a.transition.Count; i++)
                if (a.isFinal[i])
                    result.transition[i].AddRange(result.transition[0]);

            result.isFinal.AddRange(a.isFinal);
            return result;
        }

        public static SmartFSMBuilder CreateStarClosure(SmartFSMBuilder a)
        {
            if(a.isFinal[0])
                return CreatePlusClosure(a);

            var convFunc = convFunc1;
            SmartFSMBuilder result = new();
            result.transition.Add(convFunc(a.transition[0]));
            result.transition.AddRange(a.transition.Select(convFunc));
            for(int i = 0; i < a.transition.Count; i++)
                if(a.isFinal[i])
                    result.transition[i + 1].AddRange(result.transition[0]);

            result.isFinal.Add(true);
            result.isFinal.AddRange(a.isFinal);
            return result;
        }

        public static SmartFSMBuilder CreateSimpleDup(SmartFSMBuilder a, int cnt)
        {
            Debug.Assert(cnt >= 0);

            if (cnt == 1)
                return a;

            SmartFSMBuilder result = new();
            if (cnt == 0)
            {
                result.transition.Add(new TransList());
                result.isFinal.Add(true);
                return result;
            }

            result.transition.AddRange(a.transition.Select(Copy));
            int asize = a.isFinal.Count;
            for (int i = 1; i < cnt; i++)
            {
                var convFunc = MakeConvertionFunction(i * asize);
                result.transition.AddRange(a.transition.Select(convFunc));

                var nextTrans = convFunc(a.transition[0]);
                for (int s = 0; s < asize; s++)
                    if (a.isFinal[s])
                        result.transition[s + (i - 1) * asize].AddRange(nextTrans);
            }

            if(a.isFinal[0])
            {
                for (int i = 0; i < cnt; i++)
                    result.isFinal.AddRange(a.isFinal);
            }
            else
            {
                result.isFinal.AddRange(Enumerable.Repeat(false, asize * (cnt - 1)));
                result.isFinal.AddRange(a.isFinal);
            }

            return result;
        }

        public static SmartFSMBuilder CreateRay(SmartFSMBuilder a, int left)
        {
            Debug.Assert(left >= 0);
            return left switch
            {
                0 => CreateStarClosure(a),
                1 => CreatePlusClosure(a),
                _ => CreateConcatenation(CreateSimpleDup(a, left - 1), CreatePlusClosure(a)),
            };
        }

        public static SmartFSMBuilder CreateSegment(SmartFSMBuilder a, int left, int right)
        {
            Debug.Assert(left >= 0 && right >= 0 && left <= right);

            if (left == right)
                return CreateSimpleDup(a, left);

            if (left == 0)
                return CreateSimpleDup(CreateOptional(a), right);

            return CreateConcatenation(CreateSimpleDup(a, left), CreateSimpleDup(CreateOptional(a), right - left));
        }

        public SmartFSM Create()
        {
            return new SmartFSM(transition.Select(e => e.ToArray()).ToArray(), isFinal.ToArray());
        }
    }
}
