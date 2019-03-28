using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;
using System;
using System.Collections.Generic;

namespace Optimization
{
    /// <summary>
    /// Static class providing set of helpful methods to generate chromosome genes.
    /// </summary>
    public static class GeneFactory
    {
        /// <summary>
        /// Gene configurations array.
        /// </summary>
        public static GeneConfiguration[] GeneConfigArray { get; private set; }

        /// <summary>
        /// Randomizer basic.
        /// </summary>
        private static IRandomization _basic;

        /// <summary>
        /// Randomizer fibonacci.
        /// </summary>
        private static IRandomization _fibonacci;

        /// <summary>
        /// Static initialization method. To assign class variables the values.
        /// </summary>
        public static void Initialize(GeneConfiguration[] config)
        {
            GeneConfigArray = config;
            _basic = RandomizationProvider.Current;
            _fibonacci = new FibonacciRandomization();
        }

        /// <summary>
        /// Returns random int within an interval.
        /// </summary>
        public static int RandomBetween(int minValue, int maxValue)
        {
            return RandomizationProvider.Current.GetInt(minValue, maxValue + 1);
        }

        /// <summary>
        /// Returns random decimal within an interval.
        /// </summary>
        public static decimal RandomBetween(decimal minValue, decimal maxValue, int? precision = null)
        {
            // get random double
            var value = RandomizationProvider.Current.GetDouble() * ((double)maxValue - (double)minValue) + (double)minValue;

            if (precision.HasValue) return (decimal) System.Math.Round(value, precision.Value);

            // else.. calculate the scale of border value and take max of two.
            var precisionMinValue = DecimalScale(minValue);
            var precisionMaxValue = DecimalScale(maxValue);
            precision = Math.Max(precisionMinValue, precisionMaxValue);

            return (decimal)System.Math.Round(value, precision.Value);
        }

        /// <summary>
        /// Determine the precision of an input decimal number.
        /// More information can be found here:
        /// https://docs.microsoft.com/en-us/dotnet/api/system.decimal.getbits
        /// </summary>
        public static byte DecimalScale(decimal value)
        {
            /*
             * acording to the article above an alternative way to do the requested op. is:
             * byte scale = (byte) ((parts[3] >> 16) & 0x7F); 
             */
            return BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
        }

        /// <summary>
        /// Generate gene according to GeneConfiguration.
        /// </summary>
        /// <param name="config">Gene configuration.</param>
        /// <param name="isActual">Inicated if take pre-defined value or generate random.</param>
        public static Gene Generate(GeneConfiguration config, bool isActual)
        {
            // set randomization scheme.
            RandomizationProvider.Current = config.Fibonacci ? _fibonacci : _basic;

            // if actual int value is set
            if (isActual && config.ActualInt.HasValue)
            {
                return new Gene(new KeyValuePair<string, object>(config.Key, config.ActualInt));
            }

            // if actual decimal value is set
            if (isActual && config.ActualDecimal.HasValue)
            {
                return new Gene(new KeyValuePair<string, object>(config.Key, config.ActualDecimal));
            }

            // generate random decimal within an interval
            if (config.MinDecimal.HasValue && config.MaxDecimal.HasValue)
            {
                var randomDecimal = RandomBetween(config.MinDecimal.Value, config.MaxDecimal.Value, config.Precision);
                return new Gene(new KeyValuePair<string, object>(config.Key, randomDecimal));
            }

            // if no decimal nor int values specified - there is a mistake.
            if (!config.MinInt.HasValue || !config.MaxInt.HasValue)
                throw new Exception("Not valid gene config specification.");
            
            // if has int values interval - generate random int in between.
            var randomInt = RandomBetween(config.MinInt.Value, config.MaxInt.Value);
            return new Gene(new KeyValuePair<string, object>(config.Key, randomInt));
        }

    }
}
