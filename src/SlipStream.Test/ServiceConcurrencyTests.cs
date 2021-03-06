﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Threading;

using NUnit.Framework;

using SlipStream.Model;

namespace SlipStream.Test
{
    [TestFixture]
    public sealed class ServiceConcurrencyTests : ServiceTestCaseBase
    {
        [Test]
        public void TestMultithreadRead()
        {

            var threadProc = new ThreadStart(this.SearchTestProc);

            //启动多个线程并发测试
            const int ThreadCount = 50;
            var threads = new List<Thread>();
            for (int i = 0; i < ThreadCount; i++)
            {
                var t = new Thread(threadProc);
                threads.Add(t);
                t.Start();
            }

            //等待全部线程结束
            foreach (var t in threads)
            {
                t.Join();
            }
        }

        private void SearchTestProc()
        {
            var service = SlipstreamEnvironment.RootService;

            //每个线程中读取5次
            const int ReadTimes = 5;
            for (int i = 0; i < ReadTimes; i++)
            {
                var ids = (long[])service.Execute(
                    TestingDatabaseName, base.SessionToken, "core.menu", "Search", null, null, 0, 0);
                dynamic records = service.Execute(
                    TestingDatabaseName, base.SessionToken, "core.menu", "Read", ids, null);
                Assert.AreEqual(ids.Length, records.Length);
            }
        }


    }
}
