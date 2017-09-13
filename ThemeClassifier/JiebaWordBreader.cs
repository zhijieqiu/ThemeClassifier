using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JiebaNet.Segmenter;
using JiebaNet.Segmenter.PosSeg;

namespace ThemeClassifier
{
    public class JiebaWordBreaker
    {
        public readonly PosSegmenter posSegmenter;
        public readonly JiebaSegmenter segmenter;
        private readonly HashSet<string> stopWordsSet;
        private readonly Dictionary<string, double> segmentIdfDic;
        private double maxIdfWeight;
        

        public JiebaWordBreaker()
        {
            
            // 0. Get resource path
            string localStorageName = "JiebaResourceFiles";
            string resourcePath = "";
            //if (!RoleEnvironment.IsAvailable || RoleEnvironment.IsEmulated)
            //{
            //    // Use fixed folder path to store resource files to prevent downloading files everytime start emulator.
            //    resourcePath = Path.Combine(CloudConfigurationManager.GetSetting("EmulatedResourceRoot"), localStorageName);
            //}
            //else
            //{
            //    resourcePath = RoleEnvironment.GetLocalResource(localStorageName).RootPath;
            //}

            //if (string.IsNullOrEmpty(resourcePath) || !Directory.Exists(resourcePath))
            //{
            //    Logger.Error("Error: can't find WordBreakerFiles resource path in: {resourcePath}", resourcePath);
            //    return;
            //}

            // 1. Initialize segmenter
            ConfigurationManager.AppSettings["JiebaConfigFileDir"] = resourcePath;
            segmenter = new JiebaSegmenter();
            posSegmenter = new PosSegmenter();

            // 2. Import user defined dictionary
            var userDict = new HashSet<string>();
           
            
            
            foreach (var word in userDict)
            {
                segmenter.AddWord(word);
            }

            // 2. Initialize stop words list
            stopWordsSet = new HashSet<string>();
            string path = Path.Combine(resourcePath, "stopwords.txt");
            var reader = new StreamReader(path);
            string line = "";
            while ((line = reader.ReadLine()) != null)
            {
                stopWordsSet.Add(line);
            }
            reader.Close();

            // 3. Initialize idf weight
            segmentIdfDic = new Dictionary<string, double>();
            path = Path.Combine(resourcePath, "idf.txt");
            reader = new StreamReader(path);
            maxIdfWeight = 0.0;
            char[] spaceSpliter = { ' ' };
            while ((line = reader.ReadLine()) != null)
            {
                string[] pair = line.Split(spaceSpliter, StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length == 2)
                {
                    var token = pair[0];
                    var weight = Convert.ToDouble(pair[1]);
                    if (!segmentIdfDic.ContainsKey(token))
                    {
                        segmentIdfDic.Add(token, weight);
                        if (weight > maxIdfWeight)
                        {
                            maxIdfWeight = weight;
                        }
                    }
                }
            }
        }

        public List<JiebaSegment> Break(string content)
        {
            var segments = segmenter.Cut(content);
            var results = segments.Select(m => new JiebaSegment() { segment = m, isStopWord = false, idfWeight = maxIdfWeight }).ToList();
            foreach (var r in results)
            {
                if (stopWordsSet.Contains(r.segment))
                {
                    r.isStopWord = true;
                }
                if (segmentIdfDic.ContainsKey(r.segment))
                {
                    r.idfWeight = segmentIdfDic[r.segment];
                }
            }
            return results;
        }
    }
    public class JiebaSegment
    {
        public string segment;
        public bool isStopWord;
        public double idfWeight;
    }
}
