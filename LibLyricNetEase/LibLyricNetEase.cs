﻿using LibPlug.Interface;
using LibPlug;
using LibNet;
using System.Text;
using Newtonsoft.Json.Linq;

namespace LibLyricNetEase
{
    [Plugins("网易云Lrc歌词下载插件", "Zony", "从网易云下载lrc格式的歌词。", 1400, PluginTypesEnum.LrcSource)]
    public class LibLyricNetEase : IPlug_Lrc
    {
        private NetUtils m_netUtils;

        public PluginsAttribute PlugInfo { get; set; }

        public bool DownLoad(string artist, string songName, out byte[] lrcData)
        {
            m_netUtils = new NetUtils();
            lrcData = null;
            const string _requestUrl = @"http://music.163.com/api/search/get/web?csrf_token=";

            string _artistName = m_netUtils.URL_Encoding(artist, Encoding.UTF8);
            string _songName = m_netUtils.URL_Encoding(songName, Encoding.UTF8);
            string _searchKey = string.Format("{0}+{1}", _artistName, _songName);

            string _requestData = "&s=" + _searchKey + "&type=1&offset=0&total=true&limit=5";
            string _result = m_netUtils.HttpPost(_requestUrl, Encoding.UTF8, _requestData, @"http://music.163.com");
            string _sid = getSID(_result);
            if (string.IsNullOrEmpty(_sid)) return false;

            string _lrcUrl = "http://music.163.com/api/song/lyric?os=osx&id=" + _sid + "&lv=-1&kv=-1&tv=-1";
            _result = m_netUtils.HttpGet(_lrcUrl, Encoding.UTF8, @"http://music.163.com");

            if (_result.Contains("nolyric")) return false;
            if (_result.Contains("uncollected")) return false;

            string _lyric = JObject.Parse(_result)["lrc"].ToString();
            if (!_lyric.Contains("lyric")) return false;
            string _lrc = JObject.Parse(_lyric)["lyric"].ToString();
            string _trc = getTranslateLyric(_result);
            string _lrcString = splitLyricBuildResultValue(_lrc, _trc);

            lrcData = Encoding.UTF8.GetBytes(_lrcString);
            return true;
        }

        /// <summary>
        /// 获得歌曲的SID
        /// </summary>
        /// <returns></returns>
        private string getSID(string json)
        {
            JObject _jsonSid = JObject.Parse(json);
            if (!json.Contains("result")) return null;
            if (_jsonSid["result"]["songCount"].Value<int>() == 0)
            {
                return null;
            }

            JArray _jarraySID = (JArray)_jsonSid["result"]["songs"];
            return _jarraySID[0]["id"].ToString();
        }

        /// <summary>
        /// 获得翻译歌词
        /// </summary>
        /// <param name="tlrc">已翻译的歌词</param>
        /// <returns></returns>
        private string getTranslateLyric(string json)
        {
            if (!json.Contains("tlyric")) return null;
            JObject _jsonObj = JObject.Parse(json);
            if (_jsonObj["tlyric"]["lyric"] == null) return null;
            else return _jsonObj["tlyric"]["lyric"].ToObject<string>();
        }

        /// <summary>
        /// 如果有翻译歌词的情况下构建双语歌词
        /// </summary>
        /// <param name="lyric">原始歌词</param>
        /// <param name="tlyric">翻译歌词</param>
        /// <returns></returns>
        private string splitLyricBuildResultValue(string lyric,string tlyric)
        {
            if (!string.IsNullOrEmpty(tlyric))
            {
                return lyric + tlyric;
            }
            else return lyric;
        }
    }
}
