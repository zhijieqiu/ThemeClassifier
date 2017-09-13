using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JiebaNet.Segmenter.PosSeg;

namespace ThemeClassifier.Analysis
{
    public class ChineseHotTopicDetection
    {
        private readonly PosSegmenter PosSeg;
        // ReSharper disable once NotAccessedField.Local
        //private readonly ILogger _logger;
        public ChineseHotTopicDetection(PosSegmenter posSeg )
        {
            PosSeg = posSeg;
            
        }

        public HashSet<string> GetKeyword(string sentence)
        {
            var wordlist = new HashSet<string>();
            if (FilterTags(sentence))
                wordlist = UseJieBa(sentence);
            return wordlist;
        }

        /// <summary>
        ///     break sentence with speech
        /// </summary>
        /// <param name="sentence">the sentence to break words</param>
        /// <returns>List of words in sentence with required speech</returns>
        /// 名词、动词、形容词、状态词、区别词、数词、量词、代词
        private HashSet<string> UseJieBa(string sentence)
        {
            var breakList = new HashSet<string>();
            sentence = Regex.Replace(sentence, "[\\s\\p{P}\n\r=<>$>+￥^]", "");

            var tokens = PosSeg.Cut(sentence);
            //split only if it's chinese words

            foreach (var token in tokens)
            {
                if (Regex.IsMatch(token.Word, @"[\u4e00-\u9fa5]"))
                {
                    if (FilterSpeech(token.Flag[0]) && FilterShortWord(token.Word) && FilterCommonWords(token.Word))
                        breakList.Add(token.Word);
                }
            }
            return breakList;
        }

        /// <summary>
        ///     Judge the speech of  the word
        ///     n 名词 t 时间词 s 处所词 f 方位词 v 动词 a 形容词 b 区别词 z 状态词 r 代词 m 数词 q 量词
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        private bool FilterSpeech(char flag)
        {
            char[] charList = { 'n', 'a', 'v' };
            foreach (var ch in charList)
            {
                if (flag == ch) return true;
            }
            return false;
        }

        private bool FilterShortWord(string word)
        {
            if (word.Length <= 1)
                return false;
            return true;
        }

        private bool FilterTags(string message)
        {
            if (Regex.IsMatch(message, @"[\\s\\p{P}\n\r<>]"))
                return false;
            return true;
        }

        private bool FilterCommonWords(string word)
        {
            string[] commonWord =
            {
                "谢谢", "你好", "请问", "没有", "需要", "时间", "时候", "还有", "老师", "告诉", "知道", "告知", "不会", "邮箱",
                "获取", "加入", "看看", "能否", "得到", "QQ号"
            };
            foreach (var cWord in commonWord)
            {
                if (word.Equals(cWord)) return false;
            }
            return true;
        }
    }
}
