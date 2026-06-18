using System;
using Windows.Foundation.Metadata;

namespace Avora
{
    internal class StaticParams
    {
        public static readonly string tokenStatSly = Environment.GetEnvironmentVariable("TOKEN_STAT_SLY");
    }

    public class VKMStatSly : StatSlyLib.StatSLY
    {
        public  static string Token { get; set; } = StaticParams.tokenStatSly;
        public VKMStatSly() : base(Token) 
        {
        }
    }
}
