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

    [Serializable]
    public class Board
    {
        #region boards
        public static List<string> Boards = new List<string>()
        {
            "a","b","c","d","e","f","g","gif","h","hr","k","m","o","p","r","s","t","u","v","vg","vr","w","wg","i","ic","r9k","s4s","cm","hm","lgbt","y","3","aco","adv","an","asp","biz","cgl","ck","co","diy","fa","fit","gd","hc","his","int","jp","lit","mlp","mu","n","news","out","po","pol","sci","soc","sp","tg","toy","trv","tv","vp","wsg","wsr","x"
        };
        #endregion

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

        [NonSerialized]
        private ManualResetEvent _running = new ManualResetEvent(false);
        [NonSerialized]
        private ManualResetEvent _updated = new ManualResetEvent(false);
        public bool Updating { get; set; }
        private int _sleep = 5000;

        [field: NonSerialized]
        public event OnUpdateFinished UpdateFinished;
        [field: NonSerialized]
        public event OnNewThread NewThread;
        [field: NonSerialized]
        public event OnThreadDeleted ThreadDeleted;

        public IEndpointProvider EndpointProvider = EndpointManager.DefaultProvider;

        public Board(string board, bool auto_update = true)
        {
            Name = board;
            Threads = new List<Thread>();

            if(auto_update)
                Task.Factory.StartNew(AutoUpdateLoop);
        }

        public void Init()
        {
            _running = new ManualResetEvent(false);

            Task.Factory.StartNew(AutoUpdateLoop);
        }

        public void Update()
        {
            _updated.Reset();
            Updating = true;
            Console.WriteLine("Started update on board /{0}/", Name);
            string board_url = EndpointProvider.GetBoardEndpoint(Name);

            if (board_url == "")
                return;

            string json = Utilities.Download(board_url);

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

            Updating = false;
            _updated.Set();
            Console.WriteLine("Ended update on board /{0}/", Name);
        }

        public void StartAutoUpdate()
        {
            Console.WriteLine("Starting auto-update...");
            _running.Set();
        }

        public void StopAutoUpdate()
        {
            _running.Reset();
        }

        public bool WaitUntilUpdated(int length)
        {
            return _updated.WaitOne(length);
        }

        public void AutoUpdateLoop()
        {
            while(true)
            {
                _running.WaitOne();

                while(_running.WaitOne(0))
                {
                    long epoch = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                    long epoch_excess = epoch % _sleep;
                    int wait = (int)(_sleep - epoch_excess);

                    Console.Write("Next auto-update for board /{0}/ is due in {1} milliseconds({2}): ", Name, wait, DateTime.Now.AddMilliseconds(wait));
                    Console.WriteLine("epoch: {0}, epoch_excess: {1}, wait: {2}", epoch, epoch_excess, wait);

                    System.Threading.Thread.Sleep(wait);
                    Update();
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
