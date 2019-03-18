using System;

namespace RSMaster.Data
{
    public static class Worlds
    {
        private static Random random;

        static Worlds()
        {
            random = new Random();
        }

        public static readonly int[] FreeWorlds =
        {
            301,
            308,
            316,
            326,
            335,
            371,
            //372,
            379,
            380,
            //381,
            382,
            383,
            384,
            //385,
            393,
            394,
            397,
            398,
            399,
            //413,
            //414,
            418,
            //419,
            425,
            426,
            //427,
            430,
            431,
            //432,
            433,
            434,
            435,
            436,
            437,
            438,
            439,
            440,
            451,
            452,
            453,
            454,
            455,
            456,
            457,
            458,
            459,
            //468,
            469,
            470,
            471,
            472,
            473,
            474,
            475,
            476,
            477,
            497,
            498,
            499,
            500,
            501,
            502,
            503,
            504
        };

        public static int GetRandom()
        {
            return FreeWorlds[random.Next(0, FreeWorlds.Length)];
        }
    }
}
