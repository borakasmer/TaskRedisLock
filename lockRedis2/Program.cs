using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace lockRedis2
{
    class Program
    {
        static void Main(string[] args)
        {
            RunAllTaskAsync().Wait();
            Console.ReadLine();
        }

        public static async Task RunAllTaskAsync()
        {
            Task jobTask1 = CounterByMultiTask("lockTask1", "Task1", TimeSpan.FromMinutes(1), 100);
            Task jobTask2 = CounterByMultiTask("lockTask2", "Task2", TimeSpan.FromMinutes(1), 200);
            Task jobTask22 = CounterByMultiTask("lockTask2", "Task Senkron", TimeSpan.FromMinutes(1), 300);
            Task jobTask3 = CounterByMultiTask("lockTask3", "Task3", TimeSpan.FromMinutes(1), 400);

            List<Task> TaskList = new List<Task>() { jobTask1, jobTask2, jobTask22, jobTask3 };
            Task allTasks = Task.WhenAll(TaskList);

            try
            {
                await allTasks;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine("Task IsFaulted: " + allTasks.IsFaulted);
                foreach (var inEx in allTasks.Exception.InnerExceptions)
                {
                    Console.WriteLine("Task Inner Exception: " + inEx.Message);
                }
            }
        }
        private static async Task CounterByMultiTask(string lockKey, string taskName, TimeSpan expireTime, int delay)
        {
            bool isLocked = false;
            while (!isLocked)
            {
                isLocked = redisLock(lockKey, taskName, expireTime);
                if (isLocked)
                {
                    for (var i = 0; i < 10; i++)
                    {
                        await Task.Delay(delay);
                        Console.WriteLine(taskName + "-" + i);
                    }
                }
            }
        }

        private static Lazy<ConnectionMultiplexer> redisConnection = new Lazy<ConnectionMultiplexer>(() =>
       {
           ConfigurationOptions conf = new ConfigurationOptions
           {
               AbortOnConnectFail = false,
               ConnectTimeout = 100,
           };

           conf.EndPoints.Add("localhost", 6379);

           return ConnectionMultiplexer.Connect(conf.ToString());
       });
        public static ConnectionMultiplexer Connection => redisConnection.Value;

        static bool redisLock(string key, string value, TimeSpan expiration)
        {
            bool isLock = false;

            try
            {
                isLock = Connection.GetDatabase().StringSet(key, value, expiration, When.NotExists);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Lock: {ex.Message}");
                isLock = true;
            }

            return isLock;

        }
    }
}
