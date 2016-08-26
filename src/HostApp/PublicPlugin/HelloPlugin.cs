using System;
using HostApi;

namespace PublicPlugin
{
    internal class HelloPlugin : Plugin
    {
        public override void Run() =>
            Console.WriteLine("Hello from Public Plugin!");
    }
}
