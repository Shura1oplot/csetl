using System;
using System.IO;
using System.Text;

namespace etl
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            if (args[0] == "help")
            {
                PrintHelp();
                return;
            }

            if (args[0] == "cat")
            {
                Cat(args[1..]);
                return;
            }

            if (args[0] == "cat")
            {
                if (args.Length < 2)
                {
                    PrintHelp();
                    return;
                }
                Cat(args[1..]);
                return;
            }

            if (args[0] == "decode")
            {
                if (args.Length != 2)
                {
                    PrintHelp();
                    return;
                }
                Decode(args[1]);
                return;
            }

            if (args[0] == "encodings")
            {
                if (args.Length != 1)
                {
                    PrintHelp();
                    return;
                }
                PrintEncodings();
                return;
            }

            if (args[0] == "selectrows")
            {
                if (args.Length != 2)
                {
                    PrintHelp();
                    return;
                }
                SelectRows(long.Parse(args[1]));
                return;
            }

            if (args[0] == "csv2tab")
            {
                if (args.Length != 2)
                {
                    PrintHelp();
                    return;
                }
                CSVToTab(args[1][0]);
                return;
            }

            if (args[0] == "concatrows")
            {
                if (args.Length != 2)
                {
                    PrintHelp();
                    return;
                }
                ConcatRows(args[1][0]);
                return;
            }

            if (args[0] == "addprefix")
            {
                if (args.Length != 2)
                {
                    PrintHelp();
                    return;
                }
                AddPrefix(args[1]);
                return;
            }

            if (args[0] == "countlines")
            {
                if (args.Length != 1)
                {
                    PrintHelp();
                    return;
                }
                CountLines();
                return;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  etl help");
            Console.WriteLine("  etl cat file1 [file2] ...");
            Console.WriteLine("  ... | etl decode from_encoding");
            Console.WriteLine("  etl encodings");
            Console.WriteLine("  ... | etl selectrows from_row_number");
            Console.WriteLine("  ... | etl csv2tab delimiter");
            Console.WriteLine("  ... | etl concatrows delimiter");
            Console.WriteLine("  ... | etl addprefix prefix");
            Console.WriteLine("  ... | etl countlines");
        }

        private static void Cat(string[] files)
        {
            using (Stream fpOut = Console.OpenStandardOutput())
            {
                foreach (string fileName in files)
                {
                    using (BinaryReader fpIn = new(File.Open(fileName, FileMode.Open)))
                    {
                        byte[] buf = new byte[4096];
                        int count;

                        while ((count = fpIn.Read(buf, 0, buf.Length)) > 0)
                        {
                            fpOut.Write(buf, 0, count);
                            fpOut.Flush();
                        }
                    }
                }
            }
        }

        private static void Decode(string encoding)
        {
            Console.InputEncoding = Encoding.GetEncoding(encoding);
            Console.OutputEncoding = Encoding.GetEncoding("Unicode");

            string line;

            while ((line = Console.In.ReadLine()) != null)
            {
                Console.Out.WriteLine(line);
                Console.Out.Flush();
            }
        }

        private static void PrintEncodings()
        {
            Console.Write("Name               ");
            Console.Write("CodePage  ");
            Console.Write("BodyName           ");
            Console.Write("HeaderName         ");
            Console.Write("WebName            ");
            Console.WriteLine("Encoding.EncodingName");

            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                Encoding e = ei.GetEncoding();

                Console.Write("{0,-18} ", ei.Name);
                Console.Write("{0,-9} ", e.CodePage);
                Console.Write("{0,-18} ", e.BodyName);
                Console.Write("{0,-18} ", e.HeaderName);
                Console.Write("{0,-18} ", e.WebName);
                Console.WriteLine("{0} ", e.EncodingName);
            }
        }

        private static void SelectRows(long rowStart)
        {
            Console.InputEncoding = Encoding.GetEncoding("Unicode");
            Console.OutputEncoding = Encoding.GetEncoding("Unicode");

            string line;
            long rowNumber = 1;

            while ((line = Console.In.ReadLine()) != null)
            {
                if (rowNumber >= rowStart)
                {
                    Console.Out.WriteLine(line);
                    Console.Out.Flush();
                }

                rowNumber++;
            }
        }

        private static void CSVToTab(char sep)
        {
            Console.InputEncoding = Encoding.GetEncoding("Unicode");
            Console.OutputEncoding = Encoding.GetEncoding("Unicode");

            bool IsInQuotes = false;
            bool IsEscaped = false;
            int? cc;
            int? pc = null;

            while ((cc = Console.In.Read()) != -1)
            {
                int? co = cc;

                if (IsEscaped)
                {
                    IsEscaped = false;
                }
                else if (cc == sep)
                {
                    if (!IsInQuotes)
                        co = '\t';
                }
                else if (cc == '\t')
                {
                    co = ' ';
                }
                else if (cc == '"')
                {
                    if (IsInQuotes)
                    {
                        int nc = Console.In.Peek();

                        if (nc == -1 || nc == sep || nc == '\r' || pc == '\n')
                        {
                            IsInQuotes = false;
                            co = null;
                        }
                        else if (nc == '"')
                        {
                            IsEscaped = true;
                            co = null;
                        }
                    }
                    else if (pc == null || pc == sep || pc == '\n')
                    {
                        IsInQuotes = true;
                        co = null;
                    }
                }
                else if (cc == '\r')
                {
                    co = null;
                }
                else if (cc == '\n')
                {
                    if (IsInQuotes)
                        co = ' ';
                }

                pc = cc;

                if (co == '\n')
                {
                    Console.Out.Write('\r');
                    Console.Out.Write('\n');
                }
                else if (co != null)
                {
                    Console.Out.Write(Convert.ToChar(co));
                }

                Console.Out.Flush();
            }
        }

        private static void ConcatRows(char sep)
        {
            Console.InputEncoding = Encoding.GetEncoding("Unicode");
            Console.OutputEncoding = Encoding.GetEncoding("Unicode");

            string line;
            line = Console.In.ReadLine();
            long sepCount = CharCount(line, sep);
            Console.Out.WriteLine(line);
            Console.Out.Flush();

            long n = 0;

            while ((line = Console.In.ReadLine()) != null)
            {
                n += CharCount(line, sep);

                if (n < sepCount)
                {
                    Console.Out.Write(line.TrimEnd());
                }
                else
                {
                    Console.Out.WriteLine(line);
                    Console.Out.Flush();
                    n = 0;
                }
            }
        }

        private static long CharCount(string s, char c)
        {
            long count = 0;
            long len = s.Length;

            for (int i = 0; i < len; i++)
            {
                if (s[i] == c)
                    count++;
            }

            return count;
        }

        private static void AddPrefix(string prefix)
        {
            Console.InputEncoding = Encoding.GetEncoding("Unicode");
            Console.OutputEncoding = Encoding.GetEncoding("Unicode");

            string line;

            while ((line = Console.In.ReadLine()) != null)
            {
                Console.Out.Write(prefix);
                Console.Out.WriteLine(line);
                Console.Out.Flush();
            }
        }

        private static void CountLines()
        {
            using (Stream stdin = Console.OpenStandardInput())
            {
                int countN = 0;
                int countR = 0;
                
                int c;

                while ((c = stdin.ReadByte()) != -1)
                {
                    if (c == '\n')
                    {
                        countN++;
                    }
                    if (c == '\r')
                    {
                        countR++;
                    }
                }

                Console.Write(countN);
                Console.Write(" ");
                Console.Write(countR);
            }
        }
    }
}
