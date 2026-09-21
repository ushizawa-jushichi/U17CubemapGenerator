using System;
using System.Text;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     スレッドローカルに再利用可能な StringBuilder を保持し、GC アロケーションを抑制するユーティリティクラス。
    /// </summary>
    public static class StringBuilderPool
    {
        [ThreadStatic]
        private static StringBuilder? t_Builder;

        /// <summary>
        ///     クリア済みの共有 StringBuilder インスタンスを取得します。
        /// </summary>
        public static StringBuilder Get()
        {
            var sb = t_Builder;
            if (sb == null)
            {
                t_Builder = new StringBuilder(512);
                return t_Builder;
            }

            sb.Clear();
            return sb;
        }
    }
}
