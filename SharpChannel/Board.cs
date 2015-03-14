using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Net;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace SharpChannel
{
    public delegate void OnNewThread(Thread thread);
    public delegate void OnUpdateFinished(Board board);

    public class Board
    {
        public string Name { get; internal set; }
        public List<Thread> Threads { get; internal set; }
        public int AutoUpdateInterval
        {
            get
            {
                return _sleep;
            }
            set
            {
                _sleep = value;
            }
        }

        private ManualResetEvent _running = new ManualResetEvent(false);
        private int _sleep = 5000;

        public event OnUpdateFinished UpdateFinished;
        public event OnNewThread NewThread;
        public event OnThreadDeleted ThreadDeleted;

        public Board(string board)
        {
            Name = board;
            Threads = new List<Thread>();

            Task.Factory.StartNew(AutoUpdateLoop);
        }

        public void Update()
        {
            string json = Utilities.Download(string.Format("http://a.4cdn.org/{0}/threads.json", Name));

            if (json == "-")
                return;

            JArray pages = JArray.Parse(json);

            List<int> current_threads = new List<int>();

            foreach(JObject page in pages)
            {
                JArray threads = page.Value<JArray>("threads");
                foreach (JObject rawthread in threads)
                {
                    int id = rawthread.Value<int>("no");

                    if(!Threads.Any(t => t.ID == id))
                    {
                        Thread thread = new Thread(id, this);

                        Threads.Add(thread);

                        if (NewThread != null)
                            NewThread(thread);
                    }
                    else
                    {
                        Threads.First(t => t.ID == id).Update();
                    }

                    current_threads.Add(id);
                }
            }

            Func<Thread, bool> dead = (t => !current_threads.Contains(t.ID));

            Threads.Where(dead).ToList().ForEach(RemoveThread);

            if (UpdateFinished != null)
                UpdateFinished(this);
        }

        public void StartAutoUpdate()
        {
            _running.Set();
        }

        public void StopAutoUpdate()
        {
            _running.Reset();
        }

        private void AutoUpdateLoop()
        {
            while(true)
            {
                _running.WaitOne();

                while(_running.WaitOne(0))
                {
                    Update();
                    System.Threading.Thread.Sleep(_sleep);
                }
            }
        }

        internal void RemoveThread(Thread thread)
        {
            if (ThreadDeleted != null)
                ThreadDeleted(thread);

            thread.Removal();
        }
    }
}
