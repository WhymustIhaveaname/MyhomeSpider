using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace MyhomeSpider
{
    class Program
    {
        static StreamWriter sw;
        static List<string> worklist = new List<string>();
        static int nextwork = 0;
        static int threadn, finish = 0;
        static object worklock = new object();
        static object threadlock = new object();
        static object writelock = new object();

        static void go()
        {
            int workn;
            while (true)
            {
                lock (worklock)
                {
                    workn = nextwork++;
                }
                if (workn < worklist.Count)
                {
                    bool disp = (workn % 50 == 0);
                    using (var wc = new WebClient())
                    {
                        string work = worklist[workn];
                        byte[] data = Encoding.Default.GetBytes("action=doInput&snum=" + work);
                        wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        string ret = Encoding.Default.GetString(wc.UploadData("http://myhome.tsinghua.edu.cn/chouqian/User/index.asp", "POST", data));
                        if (ret.Contains("请确认下列信息是否正确："))
                        {
                            lock (writelock)
                            {
                                if (disp) Console.Write(work);
                                sw.Write(work);
                                var matches = Regex.Matches(ret, "\"middle\">.+?<");
                                foreach (Match match in matches)
                                {
                                    var str = match.Value.Substring(9).TrimEnd('<').Replace("&nbsp;", "");
                                    if (disp) Console.Write(" " + str);
                                    sw.Write(",\"" + str + "\"");
                                }
                                if (disp) Console.WriteLine();
                                sw.WriteLine();
                                sw.Flush();
                            }
                        }
                        else
                        {
                            if (disp) Console.WriteLine(work + " not exist.");
                        }
                    }
                }
                else break;
            }
            lock (threadlock)
            {
                finish++;
                Monitor.Pulse(threadlock);
            }
        }

        static void Main(string[] args)
        {
            var sr = new StreamReader("settings.txt");
            threadn = int.Parse(sr.ReadLine().Split(' ')[0]);
            var years = new List<string>();
            var middles = new List<Tuple<string, int>>();
            var ts = sr.ReadLine().Split(' ')[0];
            foreach (var year in ts.Split(','))
            {
                years.Add(year);
            }
            while (!sr.EndOfStream)
            {
                ts = sr.ReadLine().Split(' ')[0];
                if (ts.Length < 3) continue;
                middles.Add(new Tuple<string, int>(ts.Split('/')[0], int.Parse(ts.Split('/')[1])));
            }
            foreach (var year in years)
            {
                foreach (var middle in middles)
                {
                    for(int i = 1; i <= middle.Item2; i++)
                    {
                        worklist.Add(year + middle.Item1 + string.Format("{0:D4}", i));
                    }
                }
            }
            sw = new StreamWriter("out.csv", false, Encoding.GetEncoding("gb2312"));
            for (int i = 0; i < threadn; i++)
            {
                new Thread(new ThreadStart(go)).Start();
                Console.WriteLine("线程" + (i + 1) + "启动");
            }
            lock (threadlock)
            {
                while (finish != threadn)
                {
                    Monitor.Wait(threadlock);
                }
            }
            Console.WriteLine("都下载完了！按任意键退出。");
            Console.ReadLine();
        }
        class Tuple<T1,T2>
        {
            public T1 Item1;
            public T2 Item2;
            public Tuple(T1 item1, T2 item2)
            {
                Item1 = item1;
                Item2 = item2;
            }
        }
    }
}
