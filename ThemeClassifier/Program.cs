using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using JiebaNet.Segmenter;
using JiebaNet.Segmenter.Common;
using JiebaNet.Segmenter.PosSeg;
using NLPTool;
using ThemeClassifier.Analysis;

namespace ThemeClassifier
{
    class Program
    {
        static string baseDir = @"D:\work\theme_commercial\theme\data\";
        static Stemmer stemmer = null;
        static int compare2(KeyValuePair<string,double> a,KeyValuePair<string,double> b)
        {
            return -a.Value.CompareTo(b.Value);
        }
        static int compare1(KeyValuePair<int, double> a, KeyValuePair<int, double> b)
        {
            return a.Key.CompareTo(b.Key);
        }
        static Dictionary<string, double> strToIdf = new Dictionary<string, double>();
        static Dictionary<string, int> wordToId = new Dictionary<string, int>();
        static Dictionary<int, string> idToWord = new Dictionary<int, string>();
        static Dictionary<int, int> classInfo = new Dictionary<int, int>();
        static HashSet<string> stopWords = new HashSet<string>();
        static JiebaSegmenter Segmenter = new JiebaSegmenter();
        static PosSegmenter PosSegmenter = new PosSegmenter();
        static void loadStopWords()
        {
            string fileName = baseDir + "stop_words.txt";
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            while((line = sr.ReadLine())!=null)
            {
                line = line.ToLower().Trim();
                if (stopWords.Contains(line) == false)
                    stopWords.Add(line);
            }
            sr.Close();
        }
        static void loadClassInfo()
        {
            classInfo.Clear();
            string fileName = baseDir + "classInfo.txt";
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split(":".ToArray());
                classInfo[int.Parse(tokens[0])] = int.Parse(tokens[1]);
            }
            sr.Close();
        }
        static void scanToGenerateIDFTable(string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            int docCnt = 0;
            strToIdf.Clear();
            while((line = sr.ReadLine()) != null)
            {
                line = line.ToLower();
                string[] segs = line.Split('\t');
                line = segs[2] + " " + segs[3];
                if (line.Length == 0) continue;
                docCnt++;
                string[] tokens = line.Split(seperator,StringSplitOptions.RemoveEmptyEntries);
                HashSet<string> tmpSet = new HashSet<string>();
                foreach(string token in tokens)
                {
                    string word = stemmer.GetBaseFormWord(token);
                    if (tmpSet.Contains(word)) continue;
                    tmpSet.Add(word);
                    if (strToIdf.ContainsKey(word) == false)
                        strToIdf[word] = 1;
                    else strToIdf[word]++;

                }
            }
            foreach(KeyValuePair<string,double> kv in strToIdf.ToList())
            {
                strToIdf[kv.Key] = Math.Log(docCnt/kv.Value+0.01);
            }
            List<KeyValuePair<string, double>> kvs = strToIdf.ToList();
            kvs.Sort(compare2);
            StreamWriter sw = new StreamWriter(baseDir+"idf.txt");
            StreamWriter wordIdSw = new StreamWriter(baseDir+"wordIDTable.txt");
            int id = 0;
            foreach(KeyValuePair<string, double> kv in kvs)
            {
                sw.WriteLine(kv.Key+"\t"+kv.Value);
                wordIdSw.WriteLine(kv.Key+"\t"+(id++));
            }
            sw.Close();
            wordIdSw.Close();
            sr.Close();
        }
        static void loadidfAndWordId()
        {
            string idfFileName = baseDir + "idf.txt";
            string wordidFileName = baseDir + "wordIDTable.txt";
            StreamReader idfReader  = new StreamReader(idfFileName);
            StreamReader wordidReader = new StreamReader(wordidFileName);
            strToIdf.Clear();
            wordToId.Clear();
            string line = null;
            while ((line = idfReader.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                strToIdf[tokens[0]] = double.Parse(tokens[1]);
            }
            int id = -1;
            while ((line = wordidReader.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                id = int.Parse(tokens[1]);
                wordToId[tokens[0]] = id;
                idToWord[id] = tokens[0];
            }
            idfReader.Close();
            wordidReader.Close();
        }
        static void IdfAndWordID(string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            string line = null;
            //string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };
            int docCnt = 0;
            strToIdf.Clear();
            HashSet<string> _removeTokens = new HashSet<string>(removeTokens);
            while ((line = sr.ReadLine()) != null)
            {
                line = line.ToLower();
                string[] segs = line.Split('\t');
                int appearTimes = 0;
                bool isNumber = int.TryParse(segs[0], out appearTimes);
                if (!isNumber) continue;
                
                line = segs[1];
                
                for (int i = 0; i < appearTimes; i++)
                {
                    docCnt++;
                    var tokens = Segmenter.Cut(line, cutAll: true);
                    HashSet<string> tmpSet = new HashSet<string>();
                    foreach (string token in tokens)
                    {
                        string word = token;
                        if (_removeTokens.Contains(word)) continue;
                        if (tmpSet.Contains(word)) continue;
                        tmpSet.Add(word);
                        if (strToIdf.ContainsKey(word) == false)
                            strToIdf[word] = 1;
                        else strToIdf[word]++;
                    }
                }
            }
            foreach (KeyValuePair<string, double> kv in strToIdf.ToList())
            {
                strToIdf[kv.Key] = Math.Log(docCnt / kv.Value + 0.01);
            }
            List<KeyValuePair<string, double>> kvs = strToIdf.ToList();
            kvs.Sort(compare2);
            StreamWriter sw = new StreamWriter(baseDir + "idf.txt");
            StreamWriter wordIdSw = new StreamWriter(baseDir + "wordIDTable.txt");
            int id = 0;
            foreach (KeyValuePair<string, double> kv in kvs)
            {
                sw.WriteLine(kv.Key + "\t" + kv.Value);
                wordIdSw.WriteLine(kv.Key + "\t" + (id++));
            }
            sw.Close();
            wordIdSw.Close();
            sr.Close();
        }
        static string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/", "-" };
        static string[] removeTokens = new string[]
        {
            " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/",
            "。", "；", "？", "！", "：", "、", "，", "（", "）"
        };
        static void generateIsHelloFile(string fileName, string featureName)
        {

            string trainFileName = baseDir + fileName;
            string trainFeature = baseDir + featureName;
            StreamWriter sw = new StreamWriter(trainFeature);
            StreamReader sr = new StreamReader(trainFileName);
            //StreamReader testSr = new StreamReader(testFilelName);
            HashSet<string> _removeTokens = new HashSet<string>(removeTokens);
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                string[] segs = line.Split('\t');
                int appearTimes = 0;
                bool isNumber= int.TryParse(segs[0], out appearTimes);
                if (!isNumber) continue;
                line = segs[1];
                for (int i = 0; i < appearTimes; i++)
                {
                    AllDatas.Add(line);
                    //Tuple<int, string, List<KeyValuePair<string, string>>> ret = SpellingChecker.check(line);
                    //line = ret.Item2;
                    line = line.ToLower();
                    var tokens = Segmenter.Cut(line, cutAll: true);
                    Dictionary<string, int> tfMap = new Dictionary<string, int>();
                    double total = 0.0;
                    foreach (string token in tokens)
                    {

                        string word = token;
                        if (_removeTokens.Contains(word)) continue;
                        //string word = stemmer.GetBaseFormWord(token);
                        //if (stopWords.Contains(word)) continue;
                        if (strToIdf.ContainsKey(word) && strToIdf[word] > 12.5) continue;
                        total++;
                        if (tfMap.ContainsKey(word) == false) tfMap.Add(word, 1);
                        else tfMap[word]++;
                    }
                    if(segs.Length>=3&&segs[2]!=null&&segs[2].Trim()=="问候语")
                        sw.Write(0);
                    else
                    {
                        sw.Write(1);
                    }
                    List<KeyValuePair<string, int>> allKVs = tfMap.ToList();
                    List<KeyValuePair<int, double>> kvs = new List<KeyValuePair<int, double>>();
                    foreach (KeyValuePair<string, int> kv in allKVs)
                    {
                        if (wordToId.ContainsKey(kv.Key) == false) continue;
                        int id = wordToId[kv.Key];
                        if (id > wordToId.Count + 1) continue;
                        kvs.Add(new KeyValuePair<int, double>(id, kv.Value / total * strToIdf[kv.Key]));
                        //sw.Write("\t"+id+":"+kv.Value/total*strToIdf[kv.Key]);
                    }
                    kvs.Sort(compare1);
                    foreach (KeyValuePair<int, double> kv in kvs)
                    {
                        sw.Write("\t" + kv.Key + ":" + kv.Value);
                    }
                    sw.Write("\t" + (wordToId.Count() + 50) + ":" + 0);
                    sw.Write("\n");
                }
            }

            sr.Close();
            sw.Close();
        }

        static void MySampling()
        {
            string fileName = baseDir + "allFeatures.txt";
            string allTestOriginFileName = baseDir + "testOriginFile.txt";
            StreamReader sr = new StreamReader(fileName);
            StreamWriter testOriginStreamWriter = new StreamWriter(allTestOriginFileName);
            string line = null;
           
            List<int> allInts = new List<int>();
            
            List<int> allNegInts = new List<int>();
            int index = 0;
            List<string> allDataLines = new List<string>();
            while ((line = sr.ReadLine()) != null)
            {
                if (line.IsEmpty()) continue;
                allDataLines.Add(line);
                if(line.StartsWith("1"))
                    allNegInts.Add(index);
                else 
                    allInts.Add(index);
                index++;
            }
            
            var reservedPosInts = randonSelectKFrom(allInts, (int)(allInts.Count()*0.5));
            var revervedNegInts = randonSelectKFrom(allNegInts, (int) (allInts.Count()*0.5));
            string trainFile = baseDir + "trainFeatures.txt";
            StreamWriter trainStreamWriter = new StreamWriter(trainFile);
            StreamWriter testStreamWriter = new StreamWriter(baseDir+"testFeatures.txt");
            for (int i=0;i< allDataLines.Count;i++)
            {
                if(reservedPosInts.Contains(i)||revervedNegInts.Contains(i))
                    trainStreamWriter.WriteLine(allDataLines[i]);
                else
                {
                    testStreamWriter.WriteLine(allDataLines[i]);
                    testOriginStreamWriter.WriteLine(AllDatas[i]);
                }
            }
            
            trainStreamWriter.Close();
            testStreamWriter.Close();
            testOriginStreamWriter.Close();
            sr.Close();
        }

        private static IList<string> AllDatas=null;
        static void MyInit()
        {
           
            AllDatas = new List<string>();
            baseDir = @"D:\work\cssBot\data\";
            IdfAndWordID(baseDir+"allData.txt");
            loadidfAndWordId();
            generateIsHelloFile("allData.txt", "allFeatures.txt");
            MySampling();
        }

        static void MyInit2()
        {
            string baseDir = @"D:\work\cssBot\data\GreetingClassifierData\";
            string[] removeTokens = new string[]
           {
                " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/",
                "。", "；", "？", "！", "：", "、", "，", "（", "）"
           };
            var allDatas = new List<string>();
            Func<string,string,bool> extractFeaturesToSVMFormat = (fileName,featureName) =>
            {
                string trainFileName = baseDir + fileName;
                string trainFeature = baseDir + featureName;
                StreamWriter sw = new StreamWriter(trainFeature);
                StreamReader sr = new StreamReader(trainFileName);
                //StreamReader testSr = new StreamReader(testFilelName);
                HashSet<string> _removeTokens = new HashSet<string>(removeTokens);
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] segs = line.Split('\t');
                    int appearTimes = 1;
                    line = segs[0];
                    for (int i = 0; i < appearTimes; i++)
                    {
                        allDatas.Add(line);
                        //Tuple<int, string, List<KeyValuePair<string, string>>> ret = SpellingChecker.check(line);
                        //line = ret.Item2;
                        line = line.ToLower();
                        var tokens = Segmenter.Cut(line, cutAll: true);
                        Dictionary<string, int> tfMap = new Dictionary<string, int>();
                        double total = 0.0;
                        foreach (string token in tokens)
                        {

                            string word = token;
                            if (_removeTokens.Contains(word)) continue;
                            //string word = stemmer.GetBaseFormWord(token);
                            //if (stopWords.Contains(word)) continue;
                            if (strToIdf.ContainsKey(word) && strToIdf[word] > 12.5) continue;
                            total++;
                            if (tfMap.ContainsKey(word) == false) tfMap.Add(word, 1);
                            else tfMap[word]++;
                        }
                        sw.Write(segs[1]);
                        List<KeyValuePair<string, int>> allKVs = tfMap.ToList();
                        List<KeyValuePair<int, double>> kvs = new List<KeyValuePair<int, double>>();
                        foreach (KeyValuePair<string, int> kv in allKVs)
                        {
                            if (wordToId.ContainsKey(kv.Key) == false) continue;
                            int id = wordToId[kv.Key];
                            if (id > wordToId.Count + 1) continue;
                            kvs.Add(new KeyValuePair<int, double>(id, kv.Value / total * strToIdf[kv.Key]));
                            //sw.Write("\t"+id+":"+kv.Value/total*strToIdf[kv.Key]);
                        }
                        kvs.Sort(compare1);
                        foreach (KeyValuePair<int, double> kv in kvs)
                        {
                            sw.Write("\t" + kv.Key + ":" + kv.Value);
                        }
                        sw.Write("\t" + (wordToId.Count() + 50) + ":" + 0);
                        sw.Write("\n");
                    }
                }

                sr.Close();
                sw.Close();
                return true;
            };
           
            //generate wordId and Idf 
            {
                string testFile = "";
                StreamReader sr = new StreamReader(baseDir+ "train_zh-cn.txt");
                string line = null;
                //string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };
                int docCnt = 0;
                strToIdf.Clear();//Dictionary<string, double> strToIdf = new Dictionary<string, double>();
                HashSet<string> _removeTokens = new HashSet<string>(removeTokens);
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.ToLower();
                    string[] segs = line.Split('\t');
                    if (segs.Length != 2) continue;
                    line = segs[0];

                    docCnt++;
                    var tokens = Segmenter.Cut(line, cutAll: true);
                    HashSet<string> tmpSet = new HashSet<string>();
                    foreach (string token in tokens)
                    {
                        string word = token;
                        if (_removeTokens.Contains(word)) continue;
                        if (tmpSet.Contains(word)) continue;
                        tmpSet.Add(word);
                        if (strToIdf.ContainsKey(word) == false)
                            strToIdf[word] = 1;
                        else strToIdf[word]++;
                    }
                }
                if (testFile != null)
                {
                    sr = new StreamReader(baseDir + "test_zh-cn.txt");
                    line = null;
                    //string[] seperator = new string[] { " ", ".", ";", "?", "!", ":", "\"", ",", "(", ")", "|", "[", "]", "{", "}", "+", "=", "~", "`", "/" };
                    
                    strToIdf.Clear();//Dictionary<string, double> strToIdf = new Dictionary<string, double>();
                    
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.ToLower();
                        string[] segs = line.Split('\t');
                        if (segs.Length != 2) continue;
                        line = segs[0];

                        docCnt++;
                        var tokens = Segmenter.Cut(line, cutAll: true);
                        HashSet<string> tmpSet = new HashSet<string>();
                        foreach (string token in tokens)
                        {
                            string word = token;
                            if (_removeTokens.Contains(word)) continue;
                            if (tmpSet.Contains(word)) continue;
                            tmpSet.Add(word);
                            if (strToIdf.ContainsKey(word) == false)
                                strToIdf[word] = 1;
                            else strToIdf[word]++;
                        }
                    }
                }
                foreach (KeyValuePair<string, double> kv in strToIdf.ToList())
                {
                    strToIdf[kv.Key] = Math.Log(docCnt / kv.Value + 0.01);
                }
                List<KeyValuePair<string, double>> kvs = strToIdf.ToList();
                kvs.Sort(compare2);
                StreamWriter sw = new StreamWriter(baseDir + "idf.txt");
                StreamWriter wordIdSw = new StreamWriter(baseDir + "wordIDTable.txt");
                int id = 0;
                foreach (KeyValuePair<string, double> kv in kvs)
                {
                    sw.WriteLine(kv.Key + "\t" + kv.Value);
                    wordIdSw.WriteLine(kv.Key + "\t" + (id++));
                }
                sw.Close();
                wordIdSw.Close();
                sr.Close();
            }
            {
                string idfFileName = baseDir + "idf.txt";
                string wordidFileName = baseDir + "wordIDTable.txt";
                StreamReader idfReader = new StreamReader(idfFileName);
                StreamReader wordidReader = new StreamReader(wordidFileName);
                strToIdf.Clear();
                wordToId.Clear();
                //static Dictionary<string, int> wordToId = new Dictionary<string, int>();
                //static Dictionary<int, string> idToWord = new Dictionary<int, string>();
                idToWord.Clear();
                string line = null;
                while ((line = idfReader.ReadLine()) != null)
                {
                    string[] tokens = line.Split('\t');
                    strToIdf[tokens[0]] = double.Parse(tokens[1]);
                }
                int id = -1;
                while ((line = wordidReader.ReadLine()) != null)
                {
                    string[] tokens = line.Split('\t');
                    id = int.Parse(tokens[1]);
                    wordToId[tokens[0]] = id;
                    idToWord[id] = tokens[0];
                }
                idfReader.Close();
                wordidReader.Close();
            }
            {
                extractFeaturesToSVMFormat("train_zh-cn.txt", "trainFeatures.txt");
                extractFeaturesToSVMFormat("test_zh-cn.txt", "testFeatures.txt");
            }
        }

        static void GetAllGreeting()
        {
            baseDir = @"D:\work\cssBot\data\";
            StreamReader sr =new StreamReader(baseDir + "allData.txt");
            StreamWriter sw = new StreamWriter(baseDir+"allGreetingFiles.txt");
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                var segs = line.Split("\t".ToArray(),StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length >= 3 && segs[2] != null && segs[2].Trim() == "问候语")
                {
                    sw.WriteLine(line);
                }
            }
            sr.Close();
            sw.Close();
        }

        class GreetingWithType
        {
            public string Greeting { get; set; }
            public int Type { get; set; }
        }

        static void CutFileToWords(string source, string dest)
        {
            string baseDir = @"D:\work\cssBot\data\";
            StreamReader sr = new StreamReader(baseDir+"tmp\\"+source);
            StreamWriter sw = new StreamWriter(baseDir + "tmp\\" + dest);
            HashSet<string> stopwords = new HashSet<string>();
            string line = null;
            StreamReader stopWordsSr = new StreamReader(baseDir+"stopwords.txt");
            while ((line = stopWordsSr.ReadLine()) != null)
            {
                stopwords.Add(line.Trim());
            }
            stopWordsSr.Close();
            
            HashSet<string> _removeTokens = new HashSet<string>(removeTokens);
            string[] commonWord =
            {
                "谢谢", "你好", "请问", "没有", "需要", "时间", "时候", "还有", "老师", "告诉", "知道", "告知", "不会", "邮箱",
                "获取", "加入", "看看", "能否", "得到", "QQ号"
            };
            while ((line = sr.ReadLine()) != null)
            {
                var segs = line.Split("\t".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                var tokens = Segmenter.Cut(segs[0], cutAll: true);
                tokens = tokens.Where(x => !_removeTokens.Contains(x)&&!stopwords.Contains(x)&&!commonWord.Contains(x)).ToList();
                sw.WriteLine(string.Join(" ", tokens));
            }
            sw.Close();
            sr.Close();
        }

        static void ElegantCutWords(string source, string dest)
        {
            string baseDir = @"D:\work\cssBot\data\";
            StreamReader sr = new StreamReader(baseDir + source);
            StreamWriter sw = new StreamWriter(baseDir + dest);
            HashSet<string> stopwords = new HashSet<string>();
            string line = null;
            ChineseHotTopicDetection chtd = new ChineseHotTopicDetection(PosSegmenter);
            
            while ((line = sr.ReadLine()) != null)
            {
                var tokens = chtd.GetKeyword(line);
                //if (tokens.Any())
                {
                    sw.WriteLine(string.Join(" ", tokens));
                }
            }
            sw.Close();
            sr.Close();
        }
        static void RuleBasedCovered()
        {
            baseDir = @"D:\work\cssBot\data\";
            StreamReader sr = new StreamReader(baseDir + "tmp.txt");
            StreamReader greetWordsSr = new StreamReader(baseDir+"greetingWords.txt");
            HashSet<string> allGreetingWords = new HashSet<string>();
            string line = null;
            while ((line = greetWordsSr.ReadLine()) != null)
            {
                allGreetingWords.Add(line.Trim().ToLower());
            }
            HashSet<string> _removeTokens = new HashSet<string>(removeTokens);
            greetWordsSr.Close();
            int rightCnt = 0;
            while ((line = sr.ReadLine()) != null)
            {
                var segs = line.Split("\t".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                var tokens = Segmenter.Cut(segs[0], cutAll: true);
                tokens = tokens.Where(x => !_removeTokens.Contains(x)).ToList();
                bool isGreeting = true;
                foreach (var token in tokens)
                {

                    if (!allGreetingWords.Contains(token.ToLower()))
                    {
                        isGreeting = false;
                        break;
                    }
                }
                if (isGreeting && segs[1].Trim() == "0")
                    rightCnt++;
            }
            Console.WriteLine(rightCnt);
            sr.Close();
        }

        static void AnalysisGreetingTypes()
        {
            baseDir = @"D:\work\cssBot\data\";
            StreamReader sr = new StreamReader(baseDir + "greetingTypes.txt");
            HashSet<string> greetingWords = new HashSet<string>();
            HashSet<string> _removeTokens = new HashSet<string>(removeTokens);
            StreamWriter sw = new StreamWriter(baseDir + "sortedGreetingTypes.txt");
            StreamWriter greetingWordsSw = new StreamWriter(baseDir + "greetingWords.txt");
            string line = null;
            IList<GreetingWithType> greetingWithTypes = new List<GreetingWithType>();
            while ((line = sr.ReadLine()) != null)
            {
                var segs = line.Split("\t".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                var tokens = Segmenter.Cut(segs[1], cutAll: true);
                tokens = tokens.Where(x => !_removeTokens.Contains(x)).ToList();
                foreach (var token in tokens)
                {
                    if (!greetingWords.Contains(token))
                        greetingWords.Add(token);
                }
                greetingWithTypes.Add(new GreetingWithType
                {
                    Greeting = segs[1],
                    Type = int.Parse(segs[3])
                });
            }
            greetingWithTypes = greetingWithTypes.OrderBy(x => x.Type).ToList();
            foreach (var gwt in greetingWithTypes)
            {
                sw.WriteLine(gwt.Greeting+"\t"+gwt.Type);
            }
            foreach (var greetingWord in greetingWords)
            {
                greetingWordsSw.WriteLine(greetingWord);
            }
            sr.Close();
            sw.Close();
            greetingWordsSw.Close();
        }
        static void generateTrainAndTestFile(string fileName,string featureName)
        {
            
            string trainFileName = baseDir + fileName;
            string trainFeature = baseDir + featureName;
            StreamWriter sw = new StreamWriter(trainFeature);
            StreamReader sr = new StreamReader(trainFileName);
            //StreamReader testSr = new StreamReader(testFilelName);
            string line = null;
            while((line = sr.ReadLine()) != null)
            {
                string[] segs = line.Split('\t');
                line = segs[2] + " " + segs[3];
                //Tuple<int, string, List<KeyValuePair<string, string>>> ret = SpellingChecker.check(line);
                //line = ret.Item2;
                line = line.ToLower();
                string[] tokens = line.Split(seperator,StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, int> tfMap = new Dictionary<string, int>();
                double total = 0.0;
                foreach(string token in tokens)
                {
                    string word = stemmer.GetBaseFormWord(token);
                    if (stopWords.Contains(word)) continue;
                    if (strToIdf.ContainsKey(word) && strToIdf[word] > 12.5) continue;
                    total++;
                    if (tfMap.ContainsKey(word) == false) tfMap.Add(word, 1);
                    else tfMap[word]++;
                }
                sw.Write(segs[0]);
                List<KeyValuePair<string, int>> allKVs = tfMap.ToList();
                List<KeyValuePair<int, double>> kvs = new List<KeyValuePair<int, double>>();
                foreach(KeyValuePair<string,int> kv in allKVs)
                {
                    if (wordToId.ContainsKey(kv.Key) == false) continue;
                    int id = wordToId[kv.Key];
                    if (id > wordToId.Count+1) continue;
                    kvs.Add(new KeyValuePair<int,double>(id, kv.Value / total * strToIdf[kv.Key]));
                    //sw.Write("\t"+id+":"+kv.Value/total*strToIdf[kv.Key]);
                }
                kvs.Sort(compare1);
                foreach(KeyValuePair<int,double> kv in kvs)
                {
                    sw.Write("\t" + kv.Key + ":" + kv.Value);
                }
                sw.Write("\t"+(wordToId.Count()+50)+":"+0);
                sw.Write("\n");
            }
      
            sr.Close();
            sw.Close();
        }
        static void init(bool needScan=false)
        {
            stemmer = new Stemmer();
            stemmer.LoadStemmingTerms(baseDir + "StemmingDict.extended.txt");
            if (needScan)
                scanToGenerateIDFTable(baseDir + "new_theme_data.txt");
            loadidfAndWordId();
            loadStopWords();
            loadClassInfo();
            Console.WriteLine("resource is load successfully");
        }
        static List<int> randonSelectKFrom(List<int> negAll,int k)
        {
            List<int> ret = new List<int>();
            Random random = new Random();
            for(int i = k; i < negAll.Count(); i++)
            {
                int t = random.Next() % (i+1);
                if (t < k)
                {
                    int replaceIndex = random.Next() % k;
                    negAll[replaceIndex] = negAll[i];
                }
            }
            for(int i = 0; i < k; i++)
            {
                ret.Add(negAll[i]);
            }
            return ret;
        }
        static void generateFinalTrainData(List<string> allData, Dictionary<int, List<int>> labelToLineNumber,int classLabel)
        {
            StreamWriter trainData = new StreamWriter(baseDir+"trainFolder\\train"+classLabel+".data");
            foreach(int lineNum in labelToLineNumber[classLabel])
            {
                string[] tokens = allData[lineNum].Split('\t');
                tokens[0] = "1";
                string line = string.Join("\t", tokens);
                trainData.WriteLine(line);
            }
            List<int> negData = new List<int>();
            foreach(KeyValuePair<int,List<int>> kv in labelToLineNumber)
            {
                if (kv.Key != classLabel)
                {
                    negData.AddRange(kv.Value);
                }
            }
            List<int> negDataLineNums = randonSelectKFrom(negData,(int)(labelToLineNumber[classLabel].Count()*5.5));
            foreach(int lineNum in negDataLineNums)
            {
                string[] tokens = allData[lineNum].Split('\t');
                tokens[0] = "0";
                string line = string.Join("\t",tokens);
                trainData.WriteLine(line);
            }
            trainData.Close();
            Console.WriteLine("generate neg data for {0} finished",classLabel);
            
        }
        static void generateFinalTrainData(List<string> allData, List<string> allData2, Dictionary<int, List<int>> labelToLineNumber, int classLabel)
        {
            StreamWriter trainData = new StreamWriter(baseDir + "trainFolder\\train" + classLabel + ".data");
            StreamWriter trainData2 = new StreamWriter(baseDir + "trainFolder2\\train" + classLabel + ".data");
            foreach (int lineNum in labelToLineNumber[classLabel])
            {
                string[] tokens = allData[lineNum].Split('\t');
                tokens[0] = "1";
                string line = string.Join("\t", tokens);
                trainData.WriteLine(line);
                trainData2.WriteLine(line);
            }
            List<int> negData = new List<int>();
            foreach (KeyValuePair<int, List<int>> kv in labelToLineNumber)
            {
                if (kv.Key != classLabel)
                {
                    negData.AddRange(kv.Value);
                }
            }
            List<int> negDataLineNums = randonSelectKFrom(negData, (int)(labelToLineNumber[classLabel].Count() * 5.0));
            foreach (int lineNum in negDataLineNums)
            {
                string[] tokens = allData[lineNum].Split('\t');
                tokens[0] = "0";
                string line = string.Join("\t", tokens);
                trainData.WriteLine(line);
                tokens = allData2[lineNum].Split('\t');
                tokens[0] = "0";
                line = string.Join("\t", tokens);
                trainData2.WriteLine(line);
            }
            trainData.Close();
            trainData2.Close();
            Console.WriteLine("generate neg data for {0} finished", classLabel);

        }
        static void negSampling()
        {
            Dictionary<int, List<int>> labelToLineNumber = new Dictionary<int, List<int>>();
            List<string> allData = new List<string>();
            string fileName = baseDir + "train_all_feature.txt";
            int id = 0;
            string line = null;
            StreamReader sr = new StreamReader(fileName);
            int index = 0;
            while((line = sr.ReadLine()) != null)
            {
                allData.Add(line);
                string[] tokens = line.Split("\t".ToArray());
                id = int.Parse(tokens[0]);
                if (labelToLineNumber.ContainsKey(id))
                    labelToLineNumber[id].Add(index);
                else
                {
                    labelToLineNumber[id] = new List<int>();
                    labelToLineNumber[id].Add(index);
                }
                index++;
            }
            foreach(int classLebel in labelToLineNumber.Keys)
            {
                generateFinalTrainData(allData, labelToLineNumber, classLebel);
            }
            sr.Close();
        }
        static void negSampling2()
        {
            Dictionary<int, List<int>> labelToLineNumber = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> labelToLineNumber2 = new Dictionary<int, List<int>>();
            List<string> allData = new List<string>();
            List<string> allData2 = new List<string>();
            string fileName = baseDir + "train_all_feature.txt";
            string fileName2 = baseDir + "train_all_feature2.txt";
            int id = 0;
            string line = null;
            StreamReader sr = new StreamReader(fileName);
            StreamReader sr2 = new StreamReader(fileName2);
            int index = 0;
            while ((line = sr.ReadLine()) != null)
            {
                allData.Add(line);
                string[] tokens = line.Split("\t".ToArray());
                id = int.Parse(tokens[0]);
                if (labelToLineNumber.ContainsKey(id))
                    labelToLineNumber[id].Add(index);
                else
                {
                    labelToLineNumber[id] = new List<int>();
                    labelToLineNumber[id].Add(index);
                }
                index++;
            }
            index = 0;
            while ((line = sr2.ReadLine()) != null)
            {
                allData2.Add(line);
                string[] tokens = line.Split("\t".ToArray());
                id = int.Parse(tokens[0]);
                if (labelToLineNumber2.ContainsKey(id))
                    labelToLineNumber2[id].Add(index);
                else
                {
                    labelToLineNumber2[id] = new List<int>();
                    labelToLineNumber2[id].Add(index);
                }
                index++;
            }
            foreach (int classLebel in labelToLineNumber.Keys)
            {
                generateFinalTrainData(allData,allData2, labelToLineNumber, classLebel);
            }
           
            sr.Close();
            sr2.Close();
        }
        static void getClassInfo()
        {
            string FileName =baseDir+ "Theme_Training_Set_includeDes.txt";
            StreamWriter sw = new StreamWriter(baseDir+"classInfo.txt");
            StreamReader sr = new StreamReader(FileName);
            string line = null;
            Dictionary<int, int> labelToCnt = new Dictionary<int, int>();
            while((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                int classLabel = int.Parse(tokens[0]);
                if (labelToCnt.ContainsKey(classLabel) == false) labelToCnt[classLabel] = 1;
                else labelToCnt[classLabel]++;
            }
            foreach(KeyValuePair<int,int> kv in labelToCnt)
            {
                sw.WriteLine(kv.Key+":"+kv.Value);
            }
            sw.Close();
            sr.Close();
        }
        static void score(string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            StreamReader sr2 = new StreamReader(baseDir+"Theme_testSet_newDocid.txt");
            List<int> allLabels = new List<int>();
            string line = null;
            while((line = sr2.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                allLabels.Add(int.Parse(tokens[0]));
            }
            int i = 0;
            double right = 0,total=0;
            double thre = 0.0;
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                double sc = double.Parse(tokens[1]);
                if (allLabels[i]==555||allLabels[i]==105||allLabels[i]==245)
                {
                    i++;
                }else
                {
                    if (sc < thre)
                    {
                        i++;
                        continue;
                    }
                    if (tokens[0] == "" + allLabels[i])
                    {
                        right++;
                    }
                    total++;
                    i++;
                }
            }
            Console.WriteLine(right+"\t"+total+"\t"+right/total);
        }
        static void score2(string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            StreamReader sr2 = new StreamReader(baseDir + "trainClick.txt");
            StreamWriter sw = new StreamWriter(baseDir+"goodAdd.txt");
            StreamWriter sw2 = new StreamWriter(baseDir + "badAdd.txt");
            List<int> allLabels = new List<int>();
            List<string> allData = new List<string>();
            string line = null;
            while ((line = sr2.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                allLabels.Add(int.Parse(tokens[0]));
                allData.Add(line);
            }
            int i = 0;
            double right = 0, total = 0;
            double thre = 0.0;
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                double sc = double.Parse(tokens[1]);
                if (allLabels[i] == 555 || allLabels[i] == 105 || allLabels[i] == 245)
                {
                    i++;
                }
                else
                {
                    if (sc < thre)
                    {
                        i++;
                        continue;
                    }
                    if (tokens[0] == "" + allLabels[i])
                    {
                        sw.WriteLine(allData[i]);
                        right++;
                    }
                    else
                    {
                        sw2.WriteLine(tokens[0]+"\t"+allData[i]);
                    }

                    total++;
                    i++;
                }
            }

            Console.WriteLine(right + "\t" + total + "\t" + right / total);
            sr.Close();
            sr2.Close();
            sw.Close();
            sw2.Close();
        }
        static void toSpellingCheckerResult()
        {
            string trainFileName = baseDir + "Theme_Training_Set_includeDes.txt";
            string outTrainFile = baseDir + "train_sp.txt";
            string testFileName = baseDir + "Theme_testSet_newDocid.txt";
            string outTestFile = baseDir + "test_sp.txt";
            StreamReader sr = new StreamReader(trainFileName);
            StreamReader sr2 = new StreamReader(testFileName);
            StreamWriter sw = new StreamWriter(outTrainFile);
            StreamWriter sw2 = new StreamWriter(outTestFile);
            string line = null;
            int cnt = 0;
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                line = tokens[2] + ". " + tokens[3];
                Tuple<int, string, List<KeyValuePair<string, string>>> ret = SpellingChecker.check(line);
                sw.WriteLine(tokens[0] + "\t" + tokens[1] + "\t" + " \t" + ret.Item2);
                cnt++;
                if (cnt % 1000 == 0)
                {
                    Console.WriteLine(cnt);
                }
            }
            while ((line = sr2.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                line = tokens[2] + ". " + tokens[3];
                Tuple<int, string, List<KeyValuePair<string, string>>> ret = SpellingChecker.check(line);
                sw2.WriteLine(tokens[0] + "\t" + tokens[1] + "\t" + " \t" + ret.Item2);
                cnt++;
                if (cnt % 1000 == 0)
                {
                    Console.WriteLine(cnt);
                }
            }
            sr.Close();
            sr2.Close();
            sw.Close();
            sw2.Close();
        }
        static void transform()
        {
            StreamReader sr = new StreamReader(baseDir+"resultFolder\\finalResult.data5");
            StreamWriter sw = new StreamWriter(baseDir+"resultFolder\\finalResult.data8");
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split(':');
                sw.WriteLine(tokens[0]+"\t"+tokens[1]);
            }
            sr.Close();
            sw.Close();
        }
        static void generateAllFeature()
        {
            int[] indexes = {1,2,3,5,7,8,9,10,11,12,13,14,15,16,17,18,20,21,22,23,24,26,27,
                28,29,30,32 };
            string finalSr = baseDir+"testFinal9.txt";
            StreamWriter sw = new StreamWriter(finalSr);
            StreamReader sr = new StreamReader(baseDir+"test_all_feature.txt");
            string line = null;
            int j = 1;
            while((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                string outLine = j+"\t"+tokens[0];
                j++;
                outLine += "\t";
                for(int i = 1; i < tokens.Length; i++)
                {
                    if (i == 1)
                    {
                        outLine += tokens[i];
                    } else
                        outLine += (" "+tokens[i]);
                }
                sw.WriteLine(outLine);
            }
            sr.Close();
            sw.Close();
        }
        static void generateAllTrainFeature()
        {
            int[] indexes = {1,2,3,5,7,8,9,10,11,12,13,14,15,16,17,18,20,21,22,23,24,26,27,
                28,29,30,32 };
            string finalSr = baseDir + "trainFinal9.txt";
            StreamWriter sw = new StreamWriter(finalSr);
           
            foreach(int index in indexes)
            {
                string line = null;
                StreamReader sr = new StreamReader(baseDir + "trainFolder\\train"+index+".data");
                while ((line = sr.ReadLine()) != null)
                {
                    string[] tokens = line.Split('\t');

                    string outLine = "" + (10000 + index) + "\t";
                    if (tokens[0] == "0")
                        tokens[0] = "-1";
                    
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        if (i ==0 )
                        {
                            outLine += tokens[i];
                        }
                        else
                            outLine += (" " + tokens[i]);
                    }
                    sw.WriteLine(outLine);
                }
                sr.Close();
            }
            sw.Close();
        }
        static void compareFormulationAndNoFormulation()
        {
            Dictionary<int, int> idToLabel = new Dictionary<int, int>();
            StreamReader sr = new StreamReader(baseDir + "fff.txt");
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                idToLabel[int.Parse(tokens[0])] = int.Parse(tokens[1]);
                //Console.WriteLine(line);
            }
            sr.Close();
            
            sr = new StreamReader(baseDir + "cos_mos");
            double total = 0, right = 0;
            Dictionary<int, int> noFormulationIDLable = new Dictionary<int, int>();
            Dictionary<int, double> noFormulationScore = new Dictionary<int, double>();
            Dictionary<int, Dictionary<int, double>> idToLableScoreDict = new Dictionary<int, Dictionary<int, double>>();
            while ((line = sr.ReadLine()) != null)
            {
                total++;
                string[] tokens = line.Split('\t');
                int id = int.Parse(tokens[0]);
                idToLableScoreDict.Add(id,new Dictionary<int, double>());
                string[] nts = tokens[1].Split(' ');
                int maxId = 0;
                double maxScore = -1;
                foreach (string t in nts)
                {
                    string[] tks = t.Split(":".ToArray());
                    int _id = int.Parse(tks[0]);
                    double score = double.Parse(tks[1]);
                    idToLableScoreDict[id].Add(_id,score);
                    if (score > maxScore)
                    {
                        maxId = _id;
                        maxScore = score;
                    }
                }
                if (maxId == idToLabel[id])
                {
                    right++;
                }
                noFormulationIDLable[id] = maxId;
                noFormulationScore[id] = maxScore;
            }
            Console.WriteLine(right + "\t" + total + "\t" + right / total);
            sr.Close();
            sr = new StreamReader(baseDir + "outNormScore_c4be0fae-1b32-47e0-8100-323838c8e8b9_formulation");
            total = 0; right = 0;
            int anotherRight = 0;
            //Dictionary<int, int> noFormulationIDLable = new Dictionary<int, int>();
            StreamWriter sw = new StreamWriter(baseDir+"compare_f_nof.txt");
            StreamWriter sw_n_r = new StreamWriter(baseDir + "noRightFWrong.txt");
            StreamWriter sw_n_w = new StreamWriter(baseDir + "noWrongFRight.txt");
            int notTheSameTotal = 0, noRightFWrong = 0, noWrongFRight = 0;
            while ((line = sr.ReadLine()) != null)
            {
                total++;
                string[] tokens = line.Split('\t');
                int id = int.Parse(tokens[0]);
                string[] nts = tokens[1].Split(' ');
                int maxId = 0, amaxId = 0;
                double maxScore = -1, anotherMaxScore = -1;
                foreach (string t in nts)
                {
                    string[] tks = t.Split(":".ToArray());
                    int _id = int.Parse(tks[0]);
                    double score = double.Parse(tks[1]);
                    if ((idToLableScoreDict[id].ContainsKey(_id))&& idToLableScoreDict[id][_id] + 0.5*score > anotherMaxScore)
                    {
                        amaxId = _id;
                        anotherMaxScore = idToLableScoreDict[id][_id] + 0.5*score;
                    }
                    if (score > maxScore)
                    {
                        maxId = _id;
                        maxScore = score;
                        
                    }
                }
                if (maxId == idToLabel[id])
                {
                    right++;
                }
                if (amaxId == idToLabel[id]) anotherRight++;
                if (maxId != noFormulationIDLable[id])
                    notTheSameTotal++;
                if (maxId != idToLabel[id] && noFormulationIDLable[id] == idToLabel[id])
                {
                    sw_n_r.WriteLine(id + "\t" + idToLabel[id]  + "\t" + maxId + "\t" + maxScore + "\t" + noFormulationIDLable[id] + "\t" + noFormulationScore[id]);
                    
                    noRightFWrong++;
                }
                if (maxId == idToLabel[id] && noFormulationIDLable[id] != idToLabel[id])
                {
                    sw_n_w.WriteLine(id + "\t" + idToLabel[id]  + "\t" + maxId + "\t" + maxScore + "\t" + noFormulationIDLable[id] + "\t" + noFormulationScore[id]);
                    noWrongFRight++;
                }
                sw.WriteLine(id+"\t"+maxId+"\t"+noFormulationIDLable[id]+"\t"+idToLabel[id]);
                noFormulationIDLable[id] = maxId;
            }
            sw_n_r.Close();
            sw_n_w.Close();
            sw.Close();
            Console.WriteLine(right + "\t" + total+"\t"+anotherRight + "\t" + right / total);
            Console.WriteLine(notTheSameTotal+"\t"+noRightFWrong+"\t"+noWrongFRight);
            sr.Close();
        }
        static void seeResult()
        {
            Dictionary<int, int> idToLabel = new Dictionary<int, int>();
            StreamReader sr = new StreamReader(baseDir+ "fff.txt");
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split('\t');
                idToLabel[int.Parse(tokens[0])] = int.Parse(tokens[1]);
                //Console.WriteLine(line);
            }
            sr.Close();
            //sr = new StreamReader(baseDir+ "outNormScore_e41c09d4-a346-4c34-9bb4-931d79c6b52c");//1:5 add right
            sr = new StreamReader(baseDir + "outNormScore_c4be0fae-1b32-47e0-8100-323838c8e8b9_formulation");
            //sr = new StreamReader(baseDir + "outNormScore_e41c09d4-a346-4c34-9bb4-931d79c6b52c");
            //outNormScore_e593faa1-e455-4547-9680-04b7550a4917
            double total = 0,right=0;
            while ((line = sr.ReadLine()) != null)
            {
                total++;
                string[] tokens = line.Split('\t');
                int id = int.Parse(tokens[0]);
                string[] nts = tokens[1].Split(' ');
                int maxId = 0;
                double maxScore = -1;
                foreach(string t in nts)
                {
                    string[] tks = t.Split(":".ToArray());
                    int _id = int.Parse(tks[0]);
                    double score = double.Parse(tks[1]);
                    if (score > maxScore)
                    {
                        maxId = _id;
                        maxScore = score;
                    }
                }
                if (maxId < 10000) maxId += 10000;
                if (maxId == idToLabel[id])
                {
                    right++;
                }
            }
            Console.WriteLine(right+"\t"+total+"\t"+right/total);
            sr.Close();
        }
        static void toTrainClick()
        {
            string officeTax = baseDir + "commercial\\office_taxonomy.txt";
            Dictionary<string, int> themeToLabel = new Dictionary<string, int>();
            StreamReader taxSr = new StreamReader(officeTax);
            string line = null;
            int i = 1;
            while((line = taxSr.ReadLine()) != null)
            {
                if (themeToLabel.ContainsKey(line.Trim().ToLower()) == false)
                {
                    themeToLabel.Add(line.Trim().ToLower(),i);
                    i++;
                }
            }
            taxSr.Close();
            string fileName = baseDir + "commercial\\ContentEffectiveness-08182016.csv";
            
            StreamReader sr = new StreamReader(fileName);
            Dictionary<string, int> themesToCnt = new Dictionary<string, int>();
            int total = 0;
            StreamWriter trainDataSw = new StreamWriter(baseDir+"commercial\\trainClick.txt");
            HashSet<int> allLabels = new HashSet<int>();
            //int index = 0;
            while((line = sr.ReadLine()) != null)
            {
                total++;
                string[] tokens = line.Split(',');
                if (tokens.Length < 11) continue;
                string query = tokens[tokens.Length - 1];
                string[] qtokens = query.Split(seperator,StringSplitOptions.RemoveEmptyEntries);
                if (qtokens.Length < 3) continue;
                string theme = tokens[10];
                theme = theme.Replace("#TAB#",",").ToLower().Trim();

                if (tokens[tokens.Length - 1] == null || tokens[tokens.Length - 1] == ""||themeToLabel.ContainsKey(theme)==false)
                    continue;
                if (tokens[8] == "True")
                {
                    int label = themeToLabel[theme];
                    if (allLabels.Contains(label) == false)
                    {
                        allLabels.Add(label);
                    }
                    query = query.Replace("#TAB#", ",");
                    trainDataSw.WriteLine(""+label+"\t"+1+"\t" +query+"\t"+"");
                }

                if (total % 1000 == 0)
                {
                    Console.WriteLine(total);
                }
                
            }
            foreach(int label in allLabels)
            {
                Console.WriteLine(label);
            }
            /*StreamWriter sw = new StreamWriter(baseDir+"commercial\\result.txt");
            sw.WriteLine(""+total);
            sw.WriteLine(""+themesToCnt.Count());
            foreach(KeyValuePair<string,int> kv in themesToCnt)
            {
                sw.WriteLine(kv.Key+":"+kv.Value);
            }*/
            trainDataSw.Close();
            //sw.Close();
            sr.Close();
        }
        static void loadCommercial()
        {
            string officeTax = baseDir + "commercial\\office_taxonomy.txt";
            Dictionary<string, int> themeToLabel = new Dictionary<string, int>();
            StreamReader taxSr = new StreamReader(officeTax);
            string line = null;
            int i = 1;
            while((line = taxSr.ReadLine()) != null)
            {
                if (themeToLabel.ContainsKey(line.Trim().ToLower()) == false)
                {
                    themeToLabel.Add(line.Trim().ToLower(),i);
                    i++;
                }
            }
            taxSr.Close();
            string fileName = baseDir + "commercial\\ContentEffectiveness-08182016.csv";
            
            StreamReader sr = new StreamReader(fileName);
            Dictionary<string, int> themesToCnt = new Dictionary<string, int>();
            int total = 0;
            while((line = sr.ReadLine()) != null)
            {
                total++;
                string[] tokens = line.Split(',');
                if (tokens.Length < 11) continue;
                
                if (themesToCnt.ContainsKey(tokens[10].ToLower()) == false)
                {
                    themesToCnt.Add(tokens[10].ToLower(),1);
                }else
                {
                    themesToCnt[tokens[10].ToLower()]++;
                }
            }
            StreamWriter sw = new StreamWriter(baseDir+"commercial\\result.txt");
            sw.WriteLine(""+total);
            sw.WriteLine(""+themesToCnt.Count());
            foreach(KeyValuePair<string,int> kv in themesToCnt)
            {
                sw.WriteLine(kv.Key+":"+kv.Value);
            }
            sw.Close();
            sr.Close();
            Console.WriteLine("I have finished");
        }
        public static readonly char[] Punctuation = "\r\n\t ~!@#$%^&*()=+{}[]|\\:\";'<>?,./".ToCharArray();

        public static void SplitGreetingPositiveByLength()
        {
            string baseDir = @"D:\HP_test\";

            //StreamWriter sw = new StreamWriter(baseDir + "HelloCandidate.txt");
            StreamReader sr = new StreamReader(baseDir + "GreetingPositive.txt");
            StreamWriter swLong = new StreamWriter(baseDir + "GreetingPositiveLong.txt");
            StreamWriter swShort = new StreamWriter(baseDir + "GreetingPositiveShort.txt");
            string line = null;
            while ((line = sr.ReadLine() )!= null)
            {
                var tokens = line.Split(Punctuation);
                if (tokens.Length <= 4)
                {
                    swShort.WriteLine(line);
                }else
                    swLong.WriteLine(line);
            }
            sr.Close();
            swLong.Close();
            swShort.Close();
        }
        public static void SplitTestGreetingPositiveByLength()
        {
            string baseDir = @"D:\HP_test\";

            //StreamWriter sw = new StreamWriter(baseDir + "HelloCandidate.txt");
            StreamReader sr = new StreamReader(baseDir + "train.txt");
            StreamWriter swLong = new StreamWriter(baseDir + "trainLong.txt");
            StreamWriter swShort = new StreamWriter(baseDir + "trainShort.txt");
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.EndsWith("1") == false)
                    continue;
                line = line.Substring(0, line.Length - 2);
                var tokens = line.Split(Punctuation,StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length <= 4)
                {
                    swShort.WriteLine(line);
                }
                else
                    swLong.WriteLine(line);
            }
            sr.Close();
            swLong.Close();
            swShort.Close();
        }

        private static List<string> GenerateEmulatedNames()
        {
            var emulatedNames = new List<string>();
            Random random = new Random();
            for (int i = 0; i < 10; i++)
            {
                var len = random.Next(3, 9);
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < len; j++)
                {
                    int c = (int)'a' + random.Next(0, 26);
                    char cc = (char) c;
                    sb.Append(cc);
                }
                emulatedNames.Add(sb.ToString());
            }
            return emulatedNames;
        }

        public static void GenerateTrainNegativeWithGreeting()
        {
            var greetingLines = new List<string>()
            {
                "hello","hey","hi","good morning","good afternoon","how are you"
            };
            string baseDir = @"D:\Repos\test\Toronto\src\websites\Toronto\CNTKModelTrainingTest\bin\x64\Debug\TestData\v2_t1";
            StreamReader sr = new StreamReader(Path.Combine(baseDir, "negative_greeting.txt"));
            StreamWriter sw = new StreamWriter(Path.Combine(baseDir,"negative_greeting.result"));
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                foreach (var greetingSeg in greetingLines)
                {
                    sw.WriteLine(greetingSeg+", "+line);
                }
            }
            sw.Close();
            sr.Close();
        }

        public static void GenerateTrainAndTest()
        {
            string baseDir = @"D:\Repos\test\Toronto\src\websites\Toronto\CNTKModelTrainingTest\TestData\";
            //StreamWriter sw = new StreamWriter(baseDir + "HelloCandidate.txt");
            StreamReader sr = new StreamReader(baseDir + "GreetingPositive.txt");
            StreamReader sr2 = new StreamReader(baseDir + "GreetingNegative.txt");
            StreamWriter sw = new StreamWriter(baseDir + "mytrainNew.txt");
            string line = null;
            int i = 0;
            var greetingLines = new List<string>()
            {
                "hello","hey","hi","good morning","good afternoon","how are you"
            };
            var userNames = new List<string>() {};
            StreamReader nameReader = new StreamReader("D:\\hp_test\\englishnames.txt");
            while ((line = nameReader.ReadLine())!=null)
            {
                userNames.Add(line);
            }
            nameReader.Close();
            Random rand = new Random();
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Replace("<Agent>",userNames[rand.Next(0,userNames.Count)]);
                sw.WriteLine(line+"\t1");
            }
            foreach (var greetingWord in greetingLines)
            {
                //foreach (var name in GenerateEmulatedNames())
                //{
                //    sw.WriteLine(greetingWord +" "+name+"\t1");
                //}
                var names = Enumerable.Range(0, 20).Select(x=>userNames[rand.Next(0, userNames.Count)]).ToList();
                foreach (var name in names)
                {
                    sw.WriteLine(greetingWord + " " + name + "\t1");
                }
            }
            
            while ((line = sr2.ReadLine()) != null)
            {
                line = line.Replace("<Agent>", userNames[rand.Next(0, userNames.Count)]);
                sw.WriteLine(line + "\t0");
                if (line.Length < 45) continue;
                if (i < 100)
                {
                    foreach (var greetWord in greetingLines)
                    {
                        line = line.Replace("<Agent>", userNames[rand.Next(0, userNames.Count)]);
                        sw.WriteLine(greetWord + ", " + line + "\t0");
                    }
                    i++;
                }
            }
            sr.Close();
            sr2.Close();
            sw.Close();
        }

        static bool CheckIsNegativeToken(string word)
        {
            //if (word.Length >= 15)
            //{
            //    return true;
            //}
            var t = new List<List<int>>(10);
            var enumerable = Enumerable.Range(0, 10).Select(index => t[index] = new List<int>());
            foreach (var tl in t)
            {
                Console.WriteLine(tl.Count);
            }
            word = word.Trim("0123456789-?=`~!()*&^%$#_/><:;|][}{".ToCharArray());
            if (word.Length == 0)
                return true;
            var tt = "0123456789-?=`~!()*&^%$#_/><:;|][}{".ToCharArray().Select(x=>x.ToString()).ToList();
            if (tt.Any(x => word.Contains(x)))
                return true;
            return false;
            //int cnt = 0;
            //int numCnt = 0;
            //foreach (var c in word)
            //{
            //    if (c >= 'a' && c <= 'z')
            //        cnt++;
            //    if (c >= '0' && c <= '9')
            //        numCnt++;
            //}
            //if (numCnt * 1.0 / word.Length >= 0.25)
            //    return true;
            //if (cnt >= word.Length * 0.6)
            //    return false;
            //else
            //    return true;
        }

        static bool NegativeRule(string line)
        {
            var segs = line.Split("\t".ToCharArray()).ToList();
            line = segs[2];
            string[] tokens = line.Split(" ,;.!".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            {
                if (tokens.Length == 1&&tokens.First().Length<=3)
                {
                    var blackList = "aoeiuhm".ToCharArray().Select(x=>x.ToString()).ToList();
                    var word = tokens.First();
                    if (!blackList.Any(word.Contains))
                        return true;
                }
            }
            //if (tokens.Length == 1 && CheckIsNegativeToken(tokens.First()))
            //    return true;
            if (tokens.Length >= 1 && tokens.All(CheckIsNegativeToken))
                return true;
            return false;
        }

        static bool PositiveRule(string line)
        {
            line = line.Split("\t".ToCharArray())[2];
            string[] tokens = line.Trim("12".ToCharArray()).Split(" ,;.!".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var greetingLines = new List<string>()
            {
                "hello","hey","hi"
            };
            var greetingPhases = new List<string>
            {
                "good morning",
                "good afternoon",
                "how are you"
            };
            if (tokens.Length<=5&&greetingPhases.Any(line.Contains))
                return true;
            if (tokens.Length <= 4)
            {
                if (tokens.Length < 1)
                    return false;
                if (greetingLines.Contains(tokens[0]))
                    return true;
            }
            return false;
        }

        static void NewTrainDataAnalysis()
        {
            string bd = @"D:\HP_test\greetingIntention\new_labeled_data\";
            StreamReader sr = new StreamReader(bd + "result.txt");
            string line = null;
            List<Tuple<string, string>> datas = new List<Tuple<string, string>>();
            int cnt = 0;
            while ((line = sr.ReadLine()) != null)
            {
                var tokens = line.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                cnt++;
                datas.Add(Tuple.Create(tokens[0],tokens[1]));
            }
            StreamWriter sw = new StreamWriter(bd+"finalTrainData.txt");
            foreach (var tp in datas.OrderByDescending(x => x.Item2))
            {
                sw.WriteLine(tp.Item1+"\t"+tp.Item2);
            }
            sw.Close();
            sr.Close();
        }
        static void NegativeSelection()
        {
            string originTrainData = @"D:\Repos\test\Toronto\src\websites\Toronto\CNTKModelTrainingTest\bin\x64\Debug\TestData\trainDataV2_add_negativeGreeting.txt";
            var allTrainData = new HashSet<string>();
            StreamReader trainDataReader = new StreamReader(originTrainData);
            string line = null;
            while ((line = trainDataReader.ReadLine())!=null)
            {
                allTrainData.Add(line.ToLower());
            }
            trainDataReader.Close();
            string bd = @"D:\HP_test\greetingIntention\new_labeled_data\";
            StreamReader sr = new StreamReader(bd + "training.v2.negative.txt");
            
            List<Tuple<string, string>> datas = new List<Tuple<string, string>>();
            //HashSet<string> allDat
            var allDatas2 = new List<string>();
            var allIndex = new List<int>();
            HashSet<string> trainDatas = new HashSet<string>();
            while ((line = sr.ReadLine()) != null)
            {
                var tokens = line.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (trainDatas.Contains(tokens[2].ToLower()) == false&&allTrainData.Contains(tokens[2].ToLower())==false)
                {
                    trainDatas.Add(tokens[2].ToLower());
                    allIndex.Add(trainDatas.Count-1);
                }
            }

            allDatas2 = trainDatas.ToList();
            var sampleIndex = randonSelectKFrom(allIndex, 2000);
            StreamWriter sw = new StreamWriter(bd + "negativeSampleValidate.txt");
            foreach (var index in sampleIndex)
            {
                sw.WriteLine(allDatas2[index]+"\t0");
            }

            sw.Close();
            sr.Close();
        }

        static void PositiveSelection()
        {
            string originTrainData = @"D:\Repos\test\Toronto\src\websites\Toronto\CNTKModelTrainingTest\bin\x64\Debug\TestData\trainDataV2_add_negativeGreeting.txt";
            var allTrainData = new HashSet<string>();
            StreamReader trainDataReader = new StreamReader(originTrainData);
            string line = null;
            while ((line = trainDataReader.ReadLine()) != null)
            {
                allTrainData.Add(line.ToLower());
            }
            trainDataReader.Close();
            string bd = @"D:\HP_test\greetingIntention\new_labeled_data\";
            StreamReader sr = new StreamReader(bd + "training.v2.positive.txt");
            List<Tuple<string, string>> datas = new List<Tuple<string, string>>();
            //HashSet<string> allDat
            var allDatas2 = new List<string>();
            var allIndex = new List<int>();
            HashSet<string> trainDatas = new HashSet<string>();
            while ((line = sr.ReadLine()) != null)
            {
                var tokens = line.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (double.Parse(tokens[0]) < 0.5263432264328) break;
                if (trainDatas.Contains(tokens[2].ToLower()) == false && allTrainData.Contains(tokens[2].ToLower()) == false)
                {
                    trainDatas.Add(tokens[2].ToLower());
                    allIndex.Add(trainDatas.Count - 1);
                }
            }

            allDatas2 = trainDatas.ToList();
            var sampleIndex = randonSelectKFrom(allIndex, 2000);
            StreamWriter sw = new StreamWriter(bd + "positiveSampleValidate.txt");
            foreach (var index in sampleIndex)
            {
                sw.WriteLine(allDatas2[index] + "\t1");
            }

            sw.Close();
            sr.Close();
        }

        static void ExtactlyMatch()
        {
            StreamReader sr  = new StreamReader(@"\\STEVEZHENG23\Model\hp.greeting.10\testing.greeting.txt");
            HashSet<string> allTestData = new HashSet<string>();
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                var token = line.Split("\t".ToCharArray()).First();
                allTestData.Add(token);
            }
            sr.Close();
            sr = new StreamReader(@"\\STEVEZHENG23\Model\hp.greeting.10\testing.greeting.txt");
            while ((line = sr.ReadLine()) != null)
            {
                var token = line.Split("\t".ToCharArray()).First();
                allTestData.Add(token);
            }
            sr.Close();
            StreamWriter sw = new StreamWriter(@"D:\Repos\test\Toronto\src\websites\Toronto\CNTKModelTrainingTest\bin\x64\Debug\TestData\exactMatch2.txt");
            sr = new StreamReader(@"\\STEVEZHENG23\Model\hp.greeting.10\training.greeting.txt");
            int cnt = 0;
            while ((line = sr.ReadLine()) != null)
            {
                var token = line.Split("\t".ToCharArray()).First();
                if (allTestData.Contains(token))
                {
                    sw.WriteLine(line);
                    cnt++;
                }
            }
            sr.Close();
            Console.WriteLine(cnt);
            
            sw.Close();
        }

        static Dictionary<string, Dictionary<string, double>> LoadWordSuffix()
        {
            var sr = new StreamReader(@"D:\hp_test\wordSuffix.txt");
            //string line = null;
            //while ((line = sr.ReadLine()) != null)
            //{
            //    var segs = line.Split("\t".ToCharArray());
            //    var tokens = segs[1].Split(" ".ToCharArray());

            //}
            string line = null;
            Dictionary<string, Dictionary<string, double>> suffixMap = new Dictionary<string, Dictionary<string, double>>();
            while ((line = sr.ReadLine()) != null)
            {
                var tokens = line.Split("\t".ToCharArray());
                suffixMap[tokens[0]] = new Dictionary<string, double>();
                for (int i = 1; i < tokens.Length; i += 2)
                {
                    suffixMap[tokens[0]][tokens[i]] = double.Parse(tokens[i + 1]);
                }
            }

            return suffixMap;
        }

        static void ComputeProbability()
        {
            var sr = new StreamReader(@"D:\hp_test\allEntitiesBase.txt");
            var wordSuffix = LoadWordSuffix();
            string line = null;
            Dictionary<string,double> phraseProbaility = new DefaultDictionary<string, double>();
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("instant ink hi"))
                    ;
                var segs = line.Split("\t".ToCharArray());
                var tokens = segs[0].Split(" ".ToCharArray());
                if (tokens.Length == 1)
                {
                    continue;
                }

                double probability = 1.0;
                for (int i = 0; i < tokens.Length - 1; i++)
                {
                    if (wordSuffix.ContainsKey(tokens[i]) && wordSuffix[tokens[i]].ContainsKey(tokens[i + 1]))
                    {
                        probability = Math.Min(probability, wordSuffix[tokens[i]][tokens[i + 1]]);
                    }
                    else
                    {
                        probability = 0;
                    }
                }
                if (Math.Abs(probability - 1) < 0.00001)
                {
                    phraseProbaility[line] = 0;
                }
                else
                {
                    phraseProbaility[line] = probability*double.Parse(segs[1]);
                }

            }
            var phraseSegs = phraseProbaility.OrderByDescending(x => x.Value).ToList();
            var sw = new StreamWriter(@"D:\hp_test\phraseProbability.txt");
            foreach (var phrase in phraseSegs)
            {
                sw.WriteLine(phrase.Key+"\t"+phrase.Value);
            }
            sw.Close();
        }

        static void Main(string[] args)
        {
            ComputeProbability();
            Console.WriteLine(AppDomain.CurrentDomain.RelativeSearchPath);
            ExtactlyMatch();
            return;
            {
                string originTrainData = @"D:\Repos\test\Toronto\src\websites\Toronto\CNTKModelTrainingTest\bin\x64\Debug\TestData\trainDataV2_add_negativeGreeting.txt";
                var allTrainData = new HashSet<string>();
                StreamReader trainDataReader = new StreamReader(originTrainData);
                string line = null;
                var indexList = new List<int>();
                while ((line = trainDataReader.ReadLine()) != null)
                {
                    allTrainData.Add(line.ToLower());
                    indexList.Add(indexList.Count);
                        
                }
                trainDataReader.Close();
                var selectedIndexes = randonSelectKFrom(indexList,1000);
                trainDataReader = new StreamReader(originTrainData);
                int index = 0;
                StreamWriter trainDataSw = new StreamWriter(@"D:\HP_test\greetingIntention\trainDataRemoveValidateSet.txt");
                StreamWriter validateDataSw = new StreamWriter(@"D:\HP_test\greetingIntention\finalValidateSet.txt");
                foreach (var data in allTrainData)
                {
                    if (selectedIndexes.Contains(index))
                    {
                        validateDataSw.WriteLine(data);
                    }
                    else
                    {
                        trainDataSw.WriteLine(data);
                    }
                    index++;
                } 
                trainDataSw.Close();
                validateDataSw.Close();
                trainDataReader.Close();
            }
            PositiveSelection();
            
            return;

            {
                string bd = @"D:\HP_test\greetingIntention\";
                StreamReader sr = new StreamReader(bd + "hpSortScore.txt");
                var sw = new StreamWriter(bd + "5000_new_2.txt");
                var swToLabel = new StreamWriter(bd + "ambiguous.txt");
                var sw2 = new StreamWriter(bd + "positive.txt");
                var sw3 = new StreamWriter(bd + "negative.txt");
                var swSample = new StreamWriter(bd + "sample1000.txt");
                string line = null;
                line = sr.ReadLine();
                sw.WriteLine("PredictScore" + "\t" + "Intent" + "\t" + "NormQuery");
                List<string> allDatas = new List<string>();
                int cnt = -1;
                var allDatas2 = new List<string>();
                var allIndex = new List<int>();
                int tCnt = 0;
                
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.ToLower();
                    if(NegativeRule(line))
                        sw3.WriteLine(line);
                    else if (PositiveRule(line))
                        sw2.WriteLine(line);
                    else
                    {
                        swToLabel.WriteLine(line);
                        tCnt++;
                        if (tCnt >= 2000)
                        {
                            var lineTokens = line.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            if (lineTokens.Length < 3) continue;
                            if (
                                lineTokens[2].Split(" ,;.!".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length <=
                                50)
                            {
                                allDatas2.Add(line);
                                allIndex.Add(allDatas2.Count-1);
                            }
                            //swSample
                        }
                    }
                }
                var sampleIndex = randonSelectKFrom(allIndex, 1000);
                foreach (var index in sampleIndex)
                {
                    swSample.WriteLine(allDatas2[index]);
                }
                sw2.Close();
                sw3.Close();
                swSample.Close();
                sw.Close();
                swToLabel.Close();
                return;
            }

            {
                string bd = @"D:\HP_test\greetingIntention\";
                StreamReader sr = new StreamReader(bd + "hpSortScore.txt");
                var sw = new StreamWriter(bd+"5000_new_2.txt");
                var sw2 = new StreamWriter(bd + "positive.txt");
                var sw3 = new StreamWriter(bd+"negative.txt");
                string line = null;
                line = sr.ReadLine();
                sw.WriteLine("PredictScore" + "\t" + "Intent" + "\t" + "NormQuery");
                List<string> allDatas = new List<string>();
                int cnt = -1;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.ToLower();
                    allDatas.Add(line);
                    var tokens = line.Split("\t".ToCharArray(), StringSplitOptions.None);
                    if (tokens.Length == 0) continue;
                    double score = Convert.ToDouble(tokens[0]);
                    
                    if (score < 0.536 && cnt<0)
                    {
                        var needPositives = new List<string>();
                        for(int i=allDatas.Count-1;i>=0&&cnt<2500;i--)
                        {
                            var tLine = allDatas[i];
                            if (!PositiveRule(tLine))
                            {
                                needPositives.Add(tLine);
                                cnt++;
                            }
                            else
                            {
                                sw2.WriteLine(tLine);
                            }
                        }
                        for(int i=needPositives.Count-1;i>=0;i--)
                            sw.WriteLine(needPositives[i]);
                        cnt = 0;
                        while (cnt <= 2500)
                        {
                            line = sr.ReadLine();
                            if (line == null)
                            {
                                break;
                            }
                            line = line.ToLower();
                            if (!NegativeRule(line))
                            {
                                sw.WriteLine(line);
                                cnt++;
                            }
                            else
                            {
                                sw3.WriteLine(line);
                            }
                        }
                        sw.Close();
                        return;
                        
                    }
                }
                sw.Close();
                return;
            }
            GenerateTrainAndTest();
            return;
            SplitTestGreetingPositiveByLength();
            //SplitGreetingPositiveByLength();
            return;
            {
                string baseDir = @"D:\HP_test\";
                
                //StreamWriter sw = new StreamWriter(baseDir + "HelloCandidate.txt");
                StreamWriter sw2 = new StreamWriter(baseDir + "NotHelloCandidateLong.txt");
                var myReader = new StreamReader(baseDir + "source_text.txt");
                IList<List<string>> allSplitTokens = new List<List<string>>();
                string line = null;
                while ((line = myReader.ReadLine()) != null)
                {
                    var tokens = line.Split("\t".ToCharArray(), StringSplitOptions.None);
                    if (tokens.Length == 0) continue;
                    allSplitTokens.Add(tokens.ToList());
                }
                var sessionGroup = allSplitTokens.GroupBy(x => x.First()).ToList();
                var usedTestSet = new HashSet<string>();
                var blackList = new List<string>()
                {
                    "scan","screen","error","work","usa","ink","correct","problem","issue","print"
                };
                foreach (var group in sessionGroup)
                {

                    //var firstAgentMessage = group.Where(x => x[3] == "Agent Post").First();
                    var userFirstTwoMessage = group.Where(x => x[3] == "User Post").Take(4).ToList();
                    if (!userFirstTwoMessage.Any())
                        continue;
                    var userFirstMessage = userFirstTwoMessage.Last();
                    //sw2.WriteLine(firstAgentMessage[6]);
                    if (userFirstMessage.Count >= 7)
                    {
                        
                        if (userFirstMessage[6].Contains("<Phone Number / Session Number>"))
                            continue;
                        if (userFirstMessage[6].Contains("<Email Address>"))
                            continue;
                        if (usedTestSet.Contains(userFirstMessage[6].ToLower()))
                            continue;
                        bool needContinue = true;
                        var tokens = userFirstMessage[6].Split(Punctuation);
                        if (tokens.Length <4) continue;
                        /*foreach (var blackWord in blackList)
                        {
                            if (userFirstMessage[6].ToLower().Contains(blackWord))
                            {
                                needContinue = false;
                                break;
                            }
                        }*/
                        if(!needContinue) 
                            continue;
                        sw2.WriteLine(userFirstMessage[6]);
                        usedTestSet.Add(userFirstMessage[6].ToLower());
                    }
                    //sw2.WriteLine(userFirstTwoMessage[1][6]);
                }
                sw2.Close();
                //sw.Close();
                myReader.Close();
                return;
            }
            {
                //MyInit2();
                //return;
                ElegantCutWords("clusterTrainData.txt", "trainDataWordsElegant.txt");
                CutFileToWords("clusterTrainData.txt", "4Words.txt");
                return;
                RuleBasedCovered();
                return;
                AnalysisGreetingTypes();
                return;
                GetAllGreeting();
                return;
                MyInit();
                return;
            }
            //toTrainClick();
            //return;
            //Console.ReadLine();
            //loadCommercial();
            //return;
            //compareFormulationAndNoFormulation();
           seeResult();
            Console.ReadLine();
           return;
           generateAllFeature();
           generateAllTrainFeature();
           return;
            bool isResult =!true;
            //toSpellingCheckerResult();
            //return;
            if (isResult)
            {
                //score2(baseDir + "resultFolder\\finalResult.data7");
                score(baseDir + "resultFolder\\finalResult.data7");
                //score(baseDir + "resultFolder\\finalResult2.data");
                //score(baseDir + "resultFolder\\finalResult - Copy.data");
                Console.ReadLine();
                return;
            }
            SpellingChecker.loadResource(baseDir+@"sp\data\");
            getClassInfo();
            init(false);
            //return;
            {
                generateTrainAndTestFile("new_theme_data.txt", "train_all_feature.txt");
                //generateTrainAndTestFile("trainClick.txt", "test_all_feature.txt");
                generateTrainAndTestFile("Theme_testSet_newDocid.txt", "test_all_feature.txt");
                //generateTrainAndTestFile("train_qf.txt","train_all_feature.txt");
                //generateTrainAndTestFile("test_qf.txt", "test_all_feature.txt");
            }
            negSampling();
        }
    }
}
