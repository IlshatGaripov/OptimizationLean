using System;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Optimization.Example
{
    /// <summary>
    /// === TO DO ==
    /// We are implementing the lastest version of this indicator found at
    /// ...
    /// </summary>
    public class SuperTrend : BarIndicator
    {
        /// <summary>
        /// === TO DO ==
        /// ... missing declarations for all the veriables.. 
        /// </summary>
        private readonly int _period;
        private readonly decimal _multiplier;
        private decimal superTrend;
        private decimal currentClose;
        private decimal currentBasicUpperBand;
        private decimal currentBasicLowerBand;
        private decimal currentTrailingUpperBand;
        private decimal currentTrailingLowerBand;
        private decimal currentTrend;
        private decimal previousTrend;
        private decimal previousTrailingUpperBand;
        private decimal previousTrailingLowerBand;
        private decimal previousClose;

        /// <summary>
        /// Average true range 
        /// it's values miltiplied by coefficient we use to calculate super trend's basic upper and lower bands
        /// </summary>
        private readonly AverageTrueRange averageTrueRange;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get
            {
                return superTrend != 0m;
            }
        }

        /// <summary>
        /// Creates a new SuperTrend indicator using the specified name, period, multiplier and moving average type
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The smoothing period used in average true range</param>
        /// <param name="multiplier">The coefficient used in calculations of basic upper and lower bands</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public SuperTrend(string name, int period, decimal multiplier, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : base(name)
        {
            averageTrueRange = new AverageTrueRange(period, movingAverageType);
            _period = period;
            _multiplier = multiplier;
        }

        /// <summary>
        /// Creates a new SuperTrend indicator using the specified period, multiplier and moving average type
        /// </summary>
        /// <param name="period">The smoothing period used in average true range</param>
        /// <param name="multiplier">The coefficient used in calculations of basic upper and lower bands</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public SuperTrend(int period, decimal multiplier, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : this($"SuperTrend({period},{multiplier})", period, multiplier, movingAverageType)
        {

        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            averageTrueRange.Update(input);
            var atr = averageTrueRange.Current.Value;

            currentClose = input.Close;

            currentBasicLowerBand = (input.Low + input.High) / 2 - _multiplier * atr;
            currentBasicUpperBand = (input.Low + input.High) / 2 + _multiplier * atr;

            // .. add comments ..
            if (previousClose > previousTrailingLowerBand)
                currentTrailingLowerBand = Math.Max(currentBasicLowerBand, previousTrailingLowerBand);
            else
                // first iteration or direction has changed at previous bar
                currentTrailingLowerBand = currentBasicLowerBand;

            // .. add comments .. 
            if (previousClose < previousTrailingUpperBand)
                currentTrailingUpperBand = Math.Min(currentBasicUpperBand, previousTrailingUpperBand);
            else
                // first iteration or direction has changed at previous bar
                currentTrailingUpperBand = currentBasicUpperBand;

            // when algo is first started value will be 0m ? 
            currentTrend = currentClose > currentTrailingUpperBand ? 1 :
                currentClose < currentTrailingLowerBand ? -1 : previousTrend;

            // define the super trend
            // 0m value will be obtainable (I guess?) only when algorithm first started?
            superTrend = currentTrend == 1 ? currentTrailingLowerBand :
                currentTrend == -1 ? currentTrailingUpperBand : 0m;

            // save the values we'll use with next indicator update to calculate supertrend
            previousClose = currentClose;
            previousTrailingLowerBand = currentTrailingLowerBand;
            previousTrailingUpperBand = currentTrailingUpperBand;
            previousTrend = currentTrend;

            return superTrend;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            averageTrueRange.Reset();
            base.Reset();
        }
    }
}