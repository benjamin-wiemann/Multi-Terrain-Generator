
namespace MultiTerrain.Helper
{
    public class MathHelper
    {

        // Greatest common factor
        public static int Gcf(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        // Least common multiple
        public static int Lcm(int a, int b)
        {
            return (a / Gcf(a, b)) * b;
        }

        public static int CalculateFactorialFromTo(int n, int lowerThreshold)
        {
            if (n <= lowerThreshold || n <= 1) 
                return 1;
            return n * CalculateFactorialFromTo(n - 1, lowerThreshold);
        }

        public static int GetBinCoeff(int n, long k)
        {            
            int r = 1;
            int d;
            if (k > n) 
                return 0;
            for (d = 1; d <= k; d++)
            {
                r *= n--;
                r /= d;
            }
            return r;
        }
    }
}