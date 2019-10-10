using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RawImageShimmer
{
    public static class StreamDataListener
    {
        const string FilePath = "1024_16bit_Output.raw";
        const string HostInterfaceIP = "127.0.0.1";
        const int DetectorDataPort = 45002;
        static byte[] HeaderBuf = new byte[1024];

        public static async Task RunImageDataTcp(string fn, CancellationToken ct = default(CancellationToken))
        {
            using (TcpClient imageDataTcp = WaitForDetToConnectOnDataPort(ct))
            {
                Console.WriteLine("Det Data Connected!");
                ct.Register(() => imageDataTcp.Close());
                NetworkStream imageDataStream = imageDataTcp.GetStream();

                //long headerBytes = ReadImageDataHeader(imageDataStream);
                //Console.WriteLine($"{headerBytes } byte header Done. Now reading pixel data ");

                long imageDataSize = await ProcessImageDataStream(fn, imageDataStream, ct);
                Console.WriteLine($"{imageDataSize } byte data Done");
            }
            return;
        }
        static TcpClient WaitForDetToConnectOnDataPort(CancellationToken ct = default(CancellationToken))
        {
            TcpListener server = null;
            try
            {
                server = new TcpListener(IPAddress.Parse(HostInterfaceIP), DetectorDataPort);
                server.Server.ReceiveTimeout = 50000;
                //server.ExclusiveAddressUse = false;
                //server.Server.ExclusiveAddressUse = false;
                server.Server.ReceiveBufferSize = 2048 * 2048 * 64;
                // Listen for Detector TCP connection attempt.
                try
                {
                    server.Start();
                    ct.Register(() => server.Stop());
                }
                catch (SocketException)
                {
                    Console.WriteLine($"Unable to bind ethernet cable from detector. Is Detector powered On ?");
                    throw;
                }
                Console.WriteLine($"I'm Waiting for Data ... ");
                return server.AcceptTcpClient();//Blocking here
            }
            finally
            {
                try { server.Stop(); } catch { }
                server = null;
            }
        }
        public static long ReadImageDataHeader(Stream detDataStream)
        {
            int nBytesRead;
            nBytesRead = detDataStream.Read(HeaderBuf, 0, 1024);//Blocking call
            return nBytesRead;
        }

        public static long ProcessImageDataStreamCopy(Stream detDataStream, CancellationToken ct = default(CancellationToken))
        {
            try
            {
                using (FileStream file = new FileStream(FilePath, FileMode.Create, System.IO.FileAccess.Write))
                {
                    //ct.Register(() => detDataStream.Close());
                    detDataStream.CopyTo(file);
                    return file.Length;
                }
            }
            catch { Console.WriteLine($"Error saving image data to the file >{FilePath}< "); };
            return 0;
        }
        public static async Task<long> ProcessImageDataStream(string afilePath, Stream detDataStream, CancellationToken ct = default(CancellationToken))
        {
            long result = 0;
            byte[] dataBuf = new byte[1024 * 2];
            int nBytesRead;
            long nLines = 0;
            long nLinesLast = 0;
            try
            {
                using (FileStream file = new FileStream(afilePath, FileMode.Create, System.IO.FileAccess.Write))
                {
                    do
                    {
                        ct.ThrowIfCancellationRequested();
                        nBytesRead = await detDataStream.ReadAsync(dataBuf, 0, dataBuf.Length);//Blocking call
                        await file.WriteAsync(dataBuf, 0, nBytesRead);
                        result += nBytesRead;
                        nLines = result / 2048;
                        if (nLines >= nLinesLast + 1)
                        {
                            Console.CursorLeft = 0;
                            Console.Write($"Saved >{nLines}< Lines     ");
                            nLinesLast = nLines;
                        }
                    }
                    while (nBytesRead > 0);
                }
                Console.CursorLeft = 0;
                Console.WriteLine($"Finished {nLines}          ");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error saving image data to the file >{afilePath}< ");
                Console.WriteLine($" {e} ");
            };
            return result;
        }
        public static long ProcessImageDataStreamDummy(Stream detDataStream, CancellationToken ct = default(CancellationToken))
        {
            long result = 0;
            byte[] dataBuf = new byte[1024 * 2];
            int nBytesRead;
            do
            {
                ct.ThrowIfCancellationRequested();
                nBytesRead = detDataStream.Read(dataBuf, 0, dataBuf.Length);//Blocking call
                result += nBytesRead;
            }
            while (nBytesRead > 0);
            return result;
        }
    }
}

