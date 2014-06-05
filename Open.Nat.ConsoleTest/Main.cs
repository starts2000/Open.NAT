//
// Authors:
//   Ben Motmans <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Nat.ConsoleTest
{
	class NatTest
	{
        public static void Main(string[] args)
		{
            NatDiscoverer.TraceSource.Switch.Level = SourceLevels.Verbose;
            NatDiscoverer.TraceSource.Listeners.Add(new ConsoleTraceListener());
            Test().Wait();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private async static Task Test()
        {
            var nat = new NatDiscoverer();
            var cts = new CancellationTokenSource();
            cts.CancelAfter(3000);
            var d = DateTime.UtcNow;
            var devices = await nat.DiscoverDevicesAsync(PortMapper.Upnp, cts);
            Console.WriteLine("Time: {0}", (DateTime.UtcNow - d).TotalSeconds);
            Console.WriteLine("{0} devices!", devices.Count());
            foreach (var device in devices)
            {
                var ip = await device.GetExternalIPAsync();

                Console.WriteLine("Your IP: {0}", ip);
                await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, 1600, 1700, 10000, "Open.Nat Testing"));
                Console.WriteLine("Added mapping: {0}:1700 -> 127.0.0.1:1600\n", ip);
                Console.WriteLine("+------+-------------------------------+--------------------------------+------------------------------------+-------------------------+");
                Console.WriteLine("| PROT | PUBLIC (Reacheable)           | PRIVATE (Your computer)        | Descriptopn                        |                         |");
                Console.WriteLine("+------+----------------------+--------+-----------------------+--------+------------------------------------+-------------------------+");
                Console.WriteLine("|      | IP Address           | Port   | IP Address            | Port   |                                    | Expires                 |");
                Console.WriteLine("+------+----------------------+--------+-----------------------+--------+------------------------------------+-------------------------+");
                foreach (var mapping in await device.GetAllMappingsAsync())
                {
                    Console.WriteLine("|  {5} | {0,-20} | {1,6} | {2,-21} | {3,6} | {4,-35}|{6,25}|",
                        ip, mapping.PublicPort, mapping.PrivateIP, mapping.PrivatePort, mapping.Description, mapping.Protocol == Protocol.Tcp ? "TCP" : "UDP", mapping.Expiration);
                }
                Console.WriteLine("+------+----------------------+--------+-----------------------+--------+------------------------------------+-------------------------+");

                Console.WriteLine("[Removing TCP mapping] {0}:1700 -> 127.0.0.1:1600", ip);
                await device.DeletePortMapAsync(new Mapping(Protocol.Tcp, 1600, 1700));
                Console.WriteLine("[Done]");

                var mappings = await device.GetAllMappingsAsync();
                var deleted = !mappings.Any(x => x.Description == "Open.Nat Testing");
                Console.WriteLine(deleted
                    ? "[SUCCESS]: Test mapping effectively removed ;)"
                    : "[FAILURE]: Test mapping wan not removed!");
                
            }
        }
    }
}