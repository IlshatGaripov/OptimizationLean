using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Optimization;
using Newtonsoft.Json.Serialization;

namespace Optimization.Batcher
{
    public class Dynasty
    {

        private readonly IFileSystem _file;
        private readonly ILogWrapper _logWrapper;
        const string _configFilename = "optimization_dynasty.json";
        private FixedSizeQueue<string> _queue;
        private DynastyConfiguration _config;
        private OptimizerConfiguration _current;
        private static Dynasty _instance;

        public Dynasty(IFileSystem file, ILogWrapper logWrapper)
        {
            _file = file;
            _logWrapper = logWrapper;
            _queue = new FixedSizeQueue<string>(2);
            _current = null;
            _instance = this;
        }

        public Dynasty() : this(new FileSystem(), new LogWrapper())
        {
        }

        public void Optimize()
        {
            _config = JsonConvert.DeserializeObject<DynastyConfiguration>(_file.File.ReadAllText("dynasty.json"));

            var segmenter = new DynastySegmenter(_config);

            var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

            for (var i = _config.StartDate; i <= _config.EndDate; i = segmenter.GetNext(i))
            {
                if (_current == null)
                {
                    _current = JsonConvert.DeserializeObject<OptimizerConfiguration>(_file.File.ReadAllText(_configFilename));
                }

                _current.StartDate = i;
                var endDate = segmenter.PeekNext(i);

                _current.EndDate = AdjustEndDate(endDate);

                string json = JsonConvert.SerializeObject(_current, settings);

                _file.File.WriteAllText(_configFilename, json);

                _logWrapper.Result($"For period: {_current.StartDate} {_current.EndDate}");

                // TODO: fix this in accordance with changes I made to OptimizerInitializer class
                //var initializer = new OptimizerInitializer(_file, _managerFactory.Create());
                //initializer.Initialize(new[] { _configFilename });
            }
        }

        public void Watch(string message)
        {
            _queue.Enqueue(message);
            //Console.WriteLine(output);

            if (_queue.First() == AlgorithmOptimumFinder.Termination)
            {
                _logWrapper.Result(_queue.Dequeue());
                _logWrapper.Result(_queue.Dequeue());
                string optimal = _queue.Dequeue();
                _logWrapper.Result(optimal);

                if (_config.WalkForward)
                {
                    var split = optimal.Split(',');

                    foreach (var t in split)
                    {
                        var pair = t.Split(':');
                        var gene = _current.Genes.SingleOrDefault(g => g.Key == pair[0].Trim());

                        /*
                        if (int.TryParse(pair[1].Trim(), out var parsedInt))
                        {
                            gene.ActualInt = parsedInt;
                        }
                        else
                        {
                            if (decimal.TryParse(pair[1].Trim(), out var parsedDecimal))
                            {
                                gene.ActualDecimal = parsedDecimal;
                            }
                            else
                            {
                                throw new Exception($"Unable to parse optimal gene from range {_current.StartDate} {_current.EndDate}");
                            }
                        }
                        */
                    }
                }
            }

        }

        public static void LogOutput(string message)
        {
            _instance.Watch(message);
        }

        //Adjust midnight dates to previous day to prevent inclusive end dates in LEAN.
        private DateTime AdjustEndDate(DateTime endDate)
        {
            if (endDate.TimeOfDay == new TimeSpan(0))
            {
                return endDate.Subtract(new TimeSpan(1));                   
            }

            return endDate;
        }

    }
}
