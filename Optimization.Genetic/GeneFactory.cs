﻿using System;
using Optimization.Base;

namespace Optimization.Genetic
{
    /// <summary>
    /// Static class providing set of helpful methods to generate chromosome genes.
    /// </summary>
    public static class GeneFactory
    {
        /// <summary>
        /// Static constructor
        /// </summary>
        static GeneFactory()
        {
            // Set general randomization provider
            RandomizationProvider.Current = new BasicRandomization();
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
        public static decimal RandomBetween(decimal minValue, decimal maxValue)
        {
            // get random double
            var value = RandomizationProvider.Current.GetDouble() * ((double)maxValue - (double)minValue) + (double)minValue;

            // cast back and return
            return (decimal)value;
        }

        /// <summary>
        /// Determine the precision of an input decimal number.
        /// More information can be found here:
        /// https://docs.microsoft.com/en-us/dotnet/api/system.decimal.getbits
        /// </summary>
        public static int DecimalScale(decimal value)
        {
            /*
             * acording to the article above an alternative way to do the requested op. is:
             * byte scale = (byte) ((parts[3] >> 16) & 0x7F); 
             */
            return BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
        }

        /// <summary>
        /// Generate gene randomly withing acceptable boundaries set by GeneConfiguration.
        /// </summary>
        /// <param name="config">Gene configuration.</param>
        public static Gene GenerateRandom(GeneConfiguration config)
        {
            // generate random decimal within an interval
            if (config.MinDecimal.HasValue && config.MaxDecimal.HasValue)
            {
                var randomDecimal = RandomBetween(config.MinDecimal.Value, config.MaxDecimal.Value);
                return new Gene(randomDecimal);
            }

            // if no decimal nor int values specified - there is a mistake.
            if (!config.MinInt.HasValue || !config.MaxInt.HasValue)
                throw new Exception("GeneFactory ~ GenerateRandom => Gene configuration is invalid");
            
            // if has int values interval - generate random int in between.
            var randomInteger = RandomBetween(config.MinInt.Value, config.MaxInt.Value);

            return new Gene(randomInteger);
        }

    }
}
