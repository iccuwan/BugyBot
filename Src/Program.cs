using System;
using System.Threading.Tasks;

namespace BugyBot
{
	class Program
	{
		public static Task Main(string[] args)
			=> Startup.RunAsync(args);
	}
}
