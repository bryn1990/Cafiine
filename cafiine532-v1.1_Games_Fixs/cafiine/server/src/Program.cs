using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace cafiine_server
{
    class Program
    {
        public const byte BYTE_NORMAL = 0xff;
        public const byte BYTE_SPECIAL = 0xfe;
        public const byte BYTE_OPEN = 0x00;
        public const byte BYTE_READ = 0x01;
        public const byte BYTE_CLOSE = 0x02;
        public const byte BYTE_OK = 0x03;
        public const byte BYTE_SETPOS = 0x04;
        public const byte BYTE_STATFILE = 0x05;
        public const byte BYTE_EOF = 0x06;
        public const byte BYTE_GETPOS = 0x07;

        [Flags]
        public enum FSStatFlag : uint
        {
            None = 0,
            unk_14_present = 0x01000000,
            mtime_present = 0x04000000,
            ctime_present = 0x08000000,
            entid_present = 0x10000000,
            directory = 0x80000000,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FSStat
        {
            public FSStatFlag flags;
            public uint permission;
            public uint owner;
            public uint group;
            public uint file_size;
            public uint unk_14_nonzero;
            public uint unk_18_zero;
            public uint unk_1c_zero;
            public uint ent_id;
            public uint ctime_u;
            public uint ctime_l;
            public uint mtime_u;
            public uint mtime_l;
            public uint unk_34_zero;
            public uint unk_38_zero;
            public uint unk_3c_zero;
            public uint unk_40_zero;
            public uint unk_44_zero;
            public uint unk_48_zero;
            public uint unk_4c_zero;
            public uint unk_50_zero;
            public uint unk_54_zero;
            public uint unk_58_zero;
            public uint unk_5c_zero;
            public uint unk_60_zero;
        }

        public static string root = "cafiine_root";
        
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.Error.WriteLine("Usage: cafiine_server [rootdir]");
                return;
            }
            if (args.Length == 1)
            {
                root = args[0];
            }
            if (!Directory.Exists(root))
            {
                Console.Error.WriteLine("Root directory `{0}' does not exist!", root);
                return;
            }
            string name = "[listener]";
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 7332);
                listener.Start();
                Console.WriteLine(name + " Listening on 7332");

                int index = 0;
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread thread = new Thread(Handle);
                    thread.Name = "[" + index.ToString() + "]";
                    thread.Start(client);
                    index++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(name + " " + e.Message);
            }
            Console.WriteLine(name + " Exit");
        }

        static void Handle(object client_obj)
        {
            string name = Thread.CurrentThread.Name;
            FileStream[] files = new FileStream[256];

            try
            {
                TcpClient client = (TcpClient)client_obj;
                using (NetworkStream stream = client.GetStream())
                {
                    EndianBinaryReader reader = new EndianBinaryReader(stream);
                    EndianBinaryWriter writer = new EndianBinaryWriter(stream);

                    uint[] ids = reader.ReadUInt32s(4);
                    Console.WriteLine(name + " Accepted connection from client " + client.Client.RemoteEndPoint.ToString());
                    Console.WriteLine(name + " TitleID: " + ids[0].ToString("X8") + "-" + ids[1].ToString("X8"));
                    //Console.WriteLine(name + " OSID: " + ids[2].ToString("X8") + "-" + ids[3].ToString("X8"));

                    if (!Directory.Exists(root + "\\" + ids[0].ToString("X8") + "-" + ids[1].ToString("X8")))
                    {
                        writer.Write(BYTE_NORMAL);
                        throw new Exception("Not interested.");
                    }
                    string LocalRoot = root + "\\" + ids[0].ToString("X8") + "-" + ids[1].ToString("X8");
                    writer.Write(BYTE_SPECIAL);

                    while (true)
                    {
                        switch (reader.ReadByte())
                        {
                            case BYTE_OPEN:
                                {
                                    int len_path = reader.ReadInt32();
                                    int len_mode = reader.ReadInt32();
                                    string path = reader.ReadString(Encoding.ASCII, len_path - 1);
                                    if (reader.ReadByte() != 0) throw new InvalidDataException();
                                    string mode = reader.ReadString(Encoding.ASCII, len_mode - 1);
                                    if (reader.ReadByte() != 0) throw new InvalidDataException();
                                    Console.WriteLine(name + " " + path);
                                    if (File.Exists(LocalRoot + path))
                                    {
                                        int handle = -1;
                                        for (int i = 0; i < files.Length; i++)
                                        {
                                            if (files[i] == null)
                                            {
                                                handle = i;
                                                break;
                                            }
                                        }
                                        if (handle == -1)
                                        {
                                            Console.WriteLine(name + " Out of file handles!");
                                            writer.Write(BYTE_SPECIAL);
                                            writer.Write(-19);
                                            writer.Write(0);
                                            break;
                                        }
                                        Console.WriteLine(name + " fopen(\"" + path + "\", \"" + mode + "\") = " + handle.ToString());

                                        files[handle] = new FileStream(LocalRoot + path, FileMode.Open, FileAccess.Read, FileShare.Read);

                                        writer.Write(BYTE_SPECIAL);
                                        writer.Write(0);
                                        writer.Write(0x0fff00ff | (handle << 8));

                                    }
                                    else
                                    {
                                        writer.Write(BYTE_NORMAL);
                                    }
                                    break;
                                }
                            case BYTE_READ:
                                {
                                    int size = reader.ReadInt32();
                                    int count = reader.ReadInt32();
                                    int fd = reader.ReadInt32();
                                    if ((fd & 0x0fff00ff) == 0x0fff00ff)
                                    {
                                        int handle = (fd >> 8) & 0xff;
                                        if (files[handle] == null)
                                        {
                                            writer.Write(BYTE_SPECIAL);
                                            writer.Write(-19);
                                            writer.Write(0);
                                            break;
                                        }
                                        FileStream f = files[handle];

                                        byte[] buffer = new byte[size * count];
                                        int sz = f.Read(buffer, 0, buffer.Length);
                                        writer.Write(BYTE_SPECIAL);
                                        writer.Write(sz / size);
                                        writer.Write(sz);
                                        writer.Write(buffer, 0, sz);
                                        if (reader.ReadByte() != BYTE_OK)
                                            throw new InvalidDataException();
                                    }
                                    else
                                    {
                                        writer.Write(BYTE_NORMAL);
                                    }
                                    break;
                                }
                            case BYTE_CLOSE:
                                {
                                    int fd = reader.ReadInt32();
                                    if ((fd & 0x0fff00ff) == 0x0fff00ff)
                                    {
                                        int handle = (fd >> 8) & 0xff;
                                        if (files[handle] == null)
                                        {
                                            writer.Write(BYTE_SPECIAL);
                                            writer.Write(-38);
                                            break;
                                        }
                                        Console.WriteLine(name + " close(" + handle.ToString() + ")");
                                        FileStream f = files[handle];

                                        writer.Write(BYTE_SPECIAL);
                                        writer.Write(0);
                                        f.Close();
                                        files[handle] = null;
                                    }
                                    else
                                    {
                                        writer.Write(BYTE_NORMAL);
                                    }
                                    break;
                                }
                            case BYTE_SETPOS:
                                {
                                    int fd = reader.ReadInt32();
                                    int pos = reader.ReadInt32();
                                    if ((fd & 0x0fff00ff) == 0x0fff00ff)
                                    {
                                        int handle = (fd >> 8) & 0xff;
                                        if (files[handle] == null)
                                        {
                                            writer.Write(BYTE_SPECIAL);
                                            writer.Write(-38);
                                            break;
                                        }
                                        FileStream f = files[handle];

                                        f.Position = pos;
                                        writer.Write(BYTE_SPECIAL);
                                        writer.Write(0);
                                    }
                                    else
                                    {
                                        writer.Write(BYTE_NORMAL);
                                    }
                                    break;
                                }
                            case BYTE_STATFILE:
                                {
                                    int fd = reader.ReadInt32();
                                    if ((fd & 0x0fff00ff) == 0x0fff00ff)
                                    {
                                        int handle = (fd >> 8) & 0xff;
                                        if (files[handle] == null)
                                        {
                                            writer.Write(BYTE_SPECIAL);
                                            writer.Write(-38);
                                            writer.Write(0);
                                            break;
                                        }
                                        FileStream f = files[handle];

                                        FSStat stat = new FSStat();

                                        stat.flags = FSStatFlag.None;
                                        stat.permission = 0x400;
                                        stat.owner = ids[1];
                                        stat.group = 0x101e;
                                        stat.file_size = (uint)f.Length;

                                        writer.Write(BYTE_SPECIAL);
                                        writer.Write(0);
                                        writer.Write(Marshal.SizeOf(stat));
                                        writer.Write(stat);
                                    }
                                    else
                                    {
                                        writer.Write(BYTE_NORMAL);
                                    }
                                    break;
                                }
                            case BYTE_EOF:
                                {
                                    int fd = reader.ReadInt32();
                                    if ((fd & 0x0fff00ff) == 0x0fff00ff)
                                    {
                                        int handle = (fd >> 8) & 0xff;
                                        if (files[handle] == null)
                                        {
                                            writer.Write(BYTE_SPECIAL);
                                            writer.Write(-38);
                                            break;
                                        }
                                        FileStream f = files[handle];
                                        
                                        writer.Write(BYTE_SPECIAL);
                                        writer.Write(f.Position == f.Length ? -5 : 0);
                                    }
                                    else
                                    {
                                        writer.Write(BYTE_NORMAL);
                                    }
                                    break;
                                }
                            case BYTE_GETPOS:
                                {
                                    int fd = reader.ReadInt32();
                                    if ((fd & 0x0fff00ff) == 0x0fff00ff)
                                    {
                                        int handle = (fd >> 8) & 0xff;
                                        if (files[handle] == null)
                                        {
                                            writer.Write(BYTE_SPECIAL);
                                            writer.Write(-38);
                                            writer.Write(0);
                                            break;
                                        }
                                        FileStream f = files[handle];

                                        writer.Write(BYTE_SPECIAL);
                                        writer.Write(0);
                                        writer.Write((int)f.Position);
                                    }
                                    else
                                    {
                                        writer.Write(BYTE_NORMAL);
                                    }
                                    break;
                                }
                            default:
                                throw new InvalidDataException();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(name + " " + e.Message);
            }
            finally
            {
                foreach (var item in files)
                {
                    if (item != null)
                        item.Close();
                }
            }
            Console.WriteLine(name + " Exit");
        }
    }
}
