using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace lockRedis
{
    class Program
    {
        //dotnet add  package StackExchange.Redis
        static void Main(string[] args)
        {            
            TimeSpan expireTime = TimeSpan.FromMinutes(1);             
            string lockKey = "lockObject";
            Parallel.For(0, 8, x =>
            {
                Thread.Sleep(10);
                string taskName = $"{x}.Task";
                bool isLocked = redisLock(lockKey, taskName, expireTime);

                if (isLocked)
                {
                    Thread.Sleep(3000);                    
                    Console.WriteLine($"{taskName} şimdi kitlendi : {DateTimeOffset.Now.ToUnixTimeMilliseconds()}.");
                    //Connection.GetDatabase().KeyDelete(lockKey);                                       
                }
                else
                {
                    Console.WriteLine($"{taskName} kitli.");
                }
            });

            Console.WriteLine("Tüm işlemler bitti.");
            Console.Read();
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
