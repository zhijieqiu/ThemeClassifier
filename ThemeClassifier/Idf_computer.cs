using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


namespace ThemeClassifier
{
    public class Idf_computer
    {
        public static int __DOCUMENT_CNT__;
        public static double beta = 0.01;
        public static string trainFile;
        public Idf_computer(string trainF)
        {
            __DOCUMENT_CNT__ = 0;
            trainFile = trainF;
            words = new Dictionary<string, Word>();
        }
        public class Word
        {
            public int occur_cnt;
            public double idf;
        }
        public void traverse()
        {
            using (StreamWriter sw = new StreamWriter("D:\\work\\ngraminfo\\idf.txt"))
            {
                foreach (KeyValuePair<string, Word> kw in words)
                {
                    System.Console.WriteLine(kw.Key + ":" + kw.Value.idf);
                    sw.WriteLine(kw.Key + ":" + kw.Value.idf);
                }
            }
        }
        public Dictionary<string, Word> words { set; get; }
        public void computeIdf()
        {
            computeIdf(trainFile);
        }
        public void computeIdf(string dir)
        {
            string trainFile = dir;
            using (StreamReader sr = new StreamReader(trainFile))
            {
                string line;
                Regex regex = new Regex(@"^\w+$");
                while ((line = sr.ReadLine()) != null)
                {
                    //System.Console.WriteLine(line);
                    HashSet<string> strs = new HashSet<string>();
                    string[] segs = line.Split("#".ToArray());
                    if (segs.Length < 1) continue;
                    __DOCUMENT_CNT__++;
                    string[] innerWords = segs[0].Split(" ".ToArray());
                    foreach (string s_word in innerWords)
                    {
                        Match match = regex.Match(s_word);
                        if (match.Success == false) continue;
                        if (strs.Contains(s_word) == false)
                        {
                            strs.Add(s_word);
                            if (words.ContainsKey(s_word))
                                words[s_word].occur_cnt++;
                            else
                            {
                                Word w_tmp = new Word();
                                w_tmp.occur_cnt = 1;
                                words.Add(s_word, w_tmp);
                                //words[s_word].occur_cnt = 1;
                            }
                        }
                    }
                }
                // foreach()
                foreach (KeyValuePair<string, Word> kw in words.ToList())
                {
                    words[kw.Key].idf = Math.Log((__DOCUMENT_CNT__ / (double)words[kw.Key].occur_cnt) + beta);
                }
            }
        }
    }
}
