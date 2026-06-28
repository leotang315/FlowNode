using FlowNode.node.Attribute;
using System;

namespace FlowNode.node
{
    [Node("Random")]
    public class RandomOperator
    {
        private static readonly Random Rng = new Random();
        private static readonly object RngLock = new object();

        /// <summary>[min, max) 范围内的随机整数，与 <see cref="Random.Next(int, int)"/> 一致。</summary>
        [Function("randomInt", true)]
        public static void RandomInt(int min, int max, out int result)
        {
            if (min >= max)
            {
                result = min;
                return;
            }

            lock (RngLock)
            {
                result = Rng.Next(min, max);
            }
        }

        /// <summary>[min, max) 范围内的随机浮点数。</summary>
        [Function("randomFloat", true)]
        public static void RandomFloat(float min, float max, out float result)
        {
            if (min >= max)
            {
                result = min;
                return;
            }

            lock (RngLock)
            {
                result = min + (float)(Rng.NextDouble() * (max - min));
            }
        }
    }
}
