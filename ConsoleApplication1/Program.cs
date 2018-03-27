using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
	class Program
	{

		static void Main(string[] args)
		{
            var s = new ProxySniff();
            s.Start();

		}
		
	}
}
