using System;
using System.Threading.Tasks;

namespace RawImageShimmer
{
    class Program
    {
        //We need some Channels !
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var sf = Task.Run(() => StreamDataListener.RunImageDataTcp(@"C:/tmp/a.raw"));
            var folder = @"C:\Users\Xela\Downloads\moses191006\A\20191003\";
            var fn = @"JT13-0157_03-10-2019_05.17.14.raw";
            var t = Task.Run(() => ReadFileToShim.CopyRawImageFileToShim(folder + fn));
            // @"C:\XCounter\RawImages\1024_16bit_Output.raw");
            Task.WaitAll(sf,t);
        }
    }
}
