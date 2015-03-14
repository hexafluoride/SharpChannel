using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace SharpChannel
{
    public delegate void OnNewPost(Post post);
    public delegate void OnPostDeleted(Post post);
    public delegate void OnThreadDeleted(Thread thread);

    public class Thread
    {
        public event OnNewPost NewPost;
        public event OnPostDeleted PostDeleted;
        public event OnThreadDeleted ThreadDeleted;

        public int ID { get; internal set; }
        public List<Post> Posts { get; internal set; }
        public Board Parent { get; internal set; }
        public bool Alive { get; internal set; }

        public Thread(int id, Board board)
        {
            ID = id;
            Parent = board;
            Posts = new List<Post>();
            Alive = true;

            Update();
        }

        public void Update()
        {
            string raw = Utilities.Download(string.Format("http://a.4cdn.org/{0}/thread/{1}.json", Parent.Name, ID));

            if (raw == "-")
            {
                Removal();
                return;
            }

            JObject root = JObject.Parse(raw);

            JArray posts = root.Value<JArray>("posts");

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            List<int> current_posts = new List<int>();

            foreach (JObject rawpost in posts)
            {
                Post post = serializer.Deserialize<Post>(new JTokenReader(rawpost));
                post.Parent = this;

                current_posts.Add(post.ID);

                if (!Posts.Any(p => p.ID == post.ID))
                {
                    Posts.Add(post);

                    if (NewPost != null)
                        NewPost(post);
                }
            }

            Func<Post, bool> dead = (p => !current_posts.Contains(p.ID));

            Posts.Where(dead).ToList().ForEach(RemovePost);
        }

        internal void Removal()
        {
            if (ThreadDeleted != null)
                ThreadDeleted(this);

            Alive = false;
        }

        private void RemovePost(Post post)
        {
            if (PostDeleted != null)
                PostDeleted(post);

            post.Removal();
        }
    }
}
