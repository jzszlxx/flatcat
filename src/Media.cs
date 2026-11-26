// FlatCat - 电台与本地音乐
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;

namespace FlatCat
{
    [DataContract]
    public class RadioStation
    {
        [DataMember(Name = "name")] public string Name = "";
        [DataMember(Name = "url")] public string Url = "";

        public RadioStation() { }
        public RadioStation(string name, string url) { Name = name; Url = url; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? Url : Name;
        }
    }

    /// <summary>内置电台。均为 http 直连 mp3 流：WPF MediaPlayer 对本机 https 流会内部抛错，只保留验证过的 http 源。</summary>
    internal static class Radios
    {
        public static List<RadioStation> Presets()
        {
            List<RadioStation> list = new List<RadioStation>();
            list.Add(new RadioStation("AsiaFM 亚洲热歌台", "http://hot.asiafm.net:8000/asiafm"));
            list.Add(new RadioStation("1766 线上电台 · 百家知识", "http://livestream.1766.today:1768/live1.mp3"));
            list.Add(new RadioStation("Anison 动漫音乐台", "http://pool.anison.fm:9000/AniSonFM(320)"));
            list.Add(new RadioStation("台中古典音乐台", "http://onair.family977.com.tw:8000/live.mp3"));
            list.Add(new RadioStation("古典音乐台 FM 97.7", "http://59.120.88.155:8000/live.mp3"));
            return list;
        }

        public static bool IsHttp(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 音频播放器：WPF MediaPlayer 需要自己的 Dispatcher 泵，
    /// 因此在独立 STA 线程上跑 Dispatcher.Run()，所有调用经 BeginInvoke 投递。
    /// </summary>
    internal sealed class AudioPlayer : IDisposable
    {
        private Thread _thread;
        private Dispatcher _disp;
        private MediaPlayer _mp;
        private volatile bool _ready;

        private volatile string _status = "未播放";
        private volatile bool _playing;
        private volatile string _title = "";

        private double _volume = 0.7;
        private List<string> _playlist;
        private int _index = -1;
        private bool _disposed;

        public AudioPlayer()
        {
            _thread = new Thread(Pump);
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.IsBackground = true;
            _thread.Start();
            int guard = 0;
            while (!_ready && guard++ < 300) Thread.Sleep(10);
        }

        private void Pump()
        {
            try
            {
                _disp = Dispatcher.CurrentDispatcher;
                _mp = new MediaPlayer();
                _mp.Volume = _volume;
                _mp.MediaOpened += delegate
                {
                    _status = "播放中";
                    _playing = true;
                };
                _mp.MediaFailed += delegate(object s, ExceptionEventArgs e)
                {
                    _playing = false;
                    string msg = (e == null || e.ErrorException == null) ? "未知错误" : e.ErrorException.Message;
                    _status = "打不开：" + Short(msg);
                };
                _mp.MediaEnded += delegate { NextInternal(); };
                _ready = true;
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                _status = "音频线程失败：" + Short(ex.Message);
            }
        }

        private void Post(Action a)
        {
            Dispatcher d = _disp;
            if (d == null) return;
            try { d.BeginInvoke(DispatcherPriority.Normal, a); }
            catch { }
        }

        private static string Short(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ");
            return s.Length > 42 ? s.Substring(0, 42) + "…" : s;
        }

        public string Status { get { return _status; } }
        public bool IsPlaying { get { return _playing; } }
        public string Title { get { return _title; } }

        public void SetVolume(double v)
        {
            if (v < 0) v = 0;
            if (v > 1) v = 1;
            _volume = v;
            Post(delegate { try { _mp.Volume = v; } catch { } });
        }

        public double Volume { get { return _volume; } }

        /// <summary>播放网络电台；返回 false 表示地址非法。</summary>
        public bool PlayUrl(string url, string title)
        {
            if (string.IsNullOrEmpty(url)) return false;
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) { _status = "地址不合法"; return false; }

            _playlist = null;
            _index = -1;
            _title = title;
            _status = "连接中…";
            _playing = false;
            Post(delegate
            {
                try
                {
                    _mp.Stop();
                    _mp.Close();
                    _mp.Open(uri);
                    _mp.Volume = _volume;
                    _mp.Play();
                }
                catch (Exception ex)
                {
                    _playing = false;
                    _status = "打不开：" + Short(ex.Message);
                }
            });
            return true;
        }

        /// <summary>播放本地音乐列表。</summary>
        public void PlayFiles(List<string> files, int startIndex)
        {
            if (files == null || files.Count == 0) { _status = "没有可播放的文件"; return; }
            _playlist = new List<string>(files);
            if (startIndex < 0 || startIndex >= _playlist.Count) startIndex = 0;
            _index = startIndex;
            PlayCurrentFile("本地音乐");
        }

        private void PlayCurrentFile(string kind)
        {
            if (_playlist == null || _index < 0 || _index >= _playlist.Count) return;
            string path = _playlist[_index];
            _title = Path.GetFileNameWithoutExtension(path);
            _status = kind + " · " + (_index + 1) + "/" + _playlist.Count;
            _playing = false;
            string uriText = new Uri(path).AbsoluteUri;
            Post(delegate
            {
                try
                {
                    _mp.Stop();
                    _mp.Close();
                    _mp.Open(new Uri(uriText));
                    _mp.Volume = _volume;
                    _mp.Play();
                }
                catch (Exception ex)
                {
                    _playing = false;
                    _status = "打不开：" + Short(ex.Message);
                }
            });
        }

        private void NextInternal()
        {
            if (_playlist == null || _playlist.Count == 0) return;
            _index++;
            if (_index >= _playlist.Count) _index = 0;
            PlayCurrentFile("本地音乐");
        }

        public void Next()
        {
            if (_playlist == null || _playlist.Count == 0) return;
            Post(delegate
            {
                _index++;
                if (_index >= _playlist.Count) _index = 0;
                PlayCurrentFile("本地音乐");
            });
        }

        public void Previous()
        {
            if (_playlist == null || _playlist.Count == 0) return;
            Post(delegate
            {
                _index--;
                if (_index < 0) _index = _playlist.Count - 1;
                PlayCurrentFile("本地音乐");
            });
        }

        public void Pause()
        {
            Post(delegate { try { _mp.Pause(); _playing = false; _status = "已暂停"; } catch { } });
        }

        public void Resume()
        {
            Post(delegate { try { _mp.Play(); _playing = true; _status = "播放中"; } catch { } });
        }

        public void Stop()
        {
            _playlist = null;
            _index = -1;
            Post(delegate
            {
                try { _mp.Stop(); _mp.Close(); } catch { }
                _playing = false;
                _status = "已停止";
                _title = "";
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                Dispatcher d = _disp;
                if (d != null) d.BeginInvoke(new Action(delegate
                {
                    try { _mp.Close(); } catch { }
                    d.BeginInvokeShutdown(DispatcherPriority.Background);
                }));
            }
            catch { }
        }

        // ---------------- 本地音乐扫描 ----------------

        private static readonly string[] AudioExt = new string[] { ".mp3", ".wav", ".wma", ".m4a", ".aac", ".flac" };

        public static bool IsAudioFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            for (int i = 0; i < AudioExt.Length; i++)
                if (AudioExt[i] == ext) return true;
            return false;
        }

        /// <summary>目录递归收集音频文件（最多 500 首），按名称排序。</summary>
        public static List<string> ScanFolder(string folder)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return list;
            try
            {
                foreach (string f in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
                {
                    if (list.Count >= 500) break;
                    if (IsAudioFile(f)) list.Add(f);
                }
                list.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch { }
            return list;
        }
    }
}