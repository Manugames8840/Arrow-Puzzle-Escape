// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("RixmBUIpsy5mEu1J+T4JeWHqkdyPaA3FxX+6i/R/sTkT/I0UYBFZuzoZIztL/fRh0Z6W6cpmyHlgbbl9jwwCDT2PDAcPjwwMDdQFdZ38bqkDyqlwBEde0CtQK73wCxWrEA7k7HAQ/cK1eBkPf0QjNJcLrN/EpC+Yt5g8moCppO6duDe2zrna6QG/kTLpZBOSbNvvkZFSVIzLJs3i7k4JPB6qRzBsYFqXH0bn8jSH749WdJClPY8MLz0ACwQni0WL+gAMDAwIDQ7yab51iwzjquqH621OpcUg+WOig9EyfCFF0by7jv4rf/sWxBGH/gmT1YFURLMV6i3CNvHaFRAdxCFrInJgO1FMN2xHHNezhoA6abEg3CmfBlHc4aHJAsXcdA8ODA0M");
        private static int[] order = new int[] { 2,11,4,11,13,9,12,7,9,13,13,11,12,13,14 };
        private static int key = 13;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
