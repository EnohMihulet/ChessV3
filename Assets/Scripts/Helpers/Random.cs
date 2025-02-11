using System.Collections;
using System.Collections.Generic;

namespace Chess.Core {
    public static class RKISS
    {
        // RKISS psuedo random number generator   
        // The struct that holds the PRNG state.
        public struct RanCtx
        {
            public ulong a;
            public ulong b;
            public ulong c;
            public ulong d;
        }

        // Rotate-left operation
        private static ulong Rot64(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }

        // Generates the next random value and updates the PRNG state.
        public static ulong RanVal(ref RanCtx x)
        {
            ulong e = x.a - Rot64(x.b, 7);

            x.a = x.b ^ Rot64(x.c, 13);
            x.b = x.c + Rot64(x.d, 37);
            x.c = x.d + e;
            x.d = e + x.a;

            return x.d;
        }

        // Initializes the PRNG state with a seed, then "warms up" the generator.
        public static void RanInit(ref RanCtx x, ulong seed)
        {
            x.a = 0xf1ea5eed;

            x.b = seed;
            x.c = seed;
            x.d = seed;

            // Warm up the generator by calling RanVal 20 times.
            for (int i = 0; i < 20; i++)
            {
                RanVal(ref x);
            }
        }
    }
}