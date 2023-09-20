using System;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 累加器.
    /// </summary>
    /// <remarks>
    /// <br/>------------------------------------------------------------------------------
    /// <br/> * 内部会持有1个值:<see cref="Value"/>,此值可以在此类型实例上调用++增1,--减1(但不会小于0)
    /// <br/> * 其应用场景是代替单纯的true或false,具体参考example
    /// <br/>------------------------------------------------------------------------------
    /// </remarks>
    /// <example>可直接对实例进行布尔值判断:<code>值>0时:if(a)</code>或<code>值小于1时:if(!a)</code></example>
    public class Accumulator : IDisposable
    {
        private int _num;

        /// <summary>
        /// 内部值
        /// </summary>
        public int Value => _num;

        /// <summary>
        /// 根据布尔值递增或递减
        /// </summary>
        /// <param name="val">true:递增+1<see cref="Value"/>反之递减-1</param>
        /// <returns>返回当前值</returns>
        public int SetValue(bool val) => val ? ++_num : --_num;

        public static Accumulator operator ++(Accumulator a)
        {
            ++a._num;
            return a;
        }

        public static Accumulator operator --(Accumulator a)
        {
            if (0 < a._num) --a._num;
            return a;
        }

        public static bool operator true(Accumulator a) => a._num > 0;

        public static bool operator false(Accumulator a) => a._num < 1;

        public static bool operator !(Accumulator a) => a._num < 1;


        /// <summary>
        /// 内部值会置为0
        /// </summary>
        public void Dispose()
        {
            _num = 0;
        }

        public override string ToString()
        {
            return _num.ToString();
        }
    }
}