using System;

namespace Future.Common.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            double mean = 200;
            double stdDev = 50;
            double[] values = new double[300];

            MathNet.Numerics.Distributions.Normal.Samples(values, mean, stdDev);
            foreach (var value in values)
            {
                Console.WriteLine(value);
            }
        }

        /// <summary>
        /// 计算数组期望值
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        private static double GetE(double[] arr)
        {
            double teste;//测试方差
            double sumresult = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                sumresult += arr[i];
            }
            teste = sumresult / arr.Length;
            return teste;
        }

        /// <summary>
        /// 计算数组的方差
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="teste">期望值</param>
        /// <returns></returns>
        private static double GetD(double[] arr, double teste)
        {
            double sumd = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                sumd += Math.Pow(arr[i] - teste, 2);
            }
            return sumd / arr.Length;
        }

        /// <summary>
        /// 返回正态分布的值
        /// </summary>
        /// <param name="u1">第一个均匀分布值</param>
        /// <param name="u2">第二个均匀分布值</param>
        /// <param name="e">正态期望</param>
        /// <param name="d">正态方差</param>
        /// <returns>分布值或者null</returns>
        private static double? GetZTFB(double u1, double u2, double e, double d)
        {
            double? result = null;
            try
            {
                result = e + Math.Sqrt(d) * Math.Sqrt((-2) * (Math.Log(u1) / Math.Log(Math.E))) * Math.Sin(2 * Math.PI * u2);
            }
            catch (Exception ex)
            {
                result = null;
            }
            return result;
        }
    }
}
