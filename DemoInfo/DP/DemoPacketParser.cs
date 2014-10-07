﻿using DemoInfo.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DemoInfo.DP
{
    class DemoPacketParser
    {
        private DemoParser demo;

        List<IMessageParser> parsers = new List<IMessageParser>();

        public DemoPacketParser(DemoParser parser)
        {
            this.demo = parser;

            InitializeHandlers();
        }

        private void InitializeHandlers()
        {
            foreach(var parser in Assembly.GetExecutingAssembly().GetTypes().Where(a => a.GetInterfaces().Contains(typeof(IMessageParser))))
            {
                IMessageParser p = (IMessageParser)parser.GetConstructor(new Type[0]).Invoke(new object[0]);
                parsers.Add(p);
            }

            parsers.OrderByDescending(a => a.GetPriority());
        }

        public void ParsePacket(byte[] data)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(data));

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int cmd = reader.ReadVarInt32();

                Type toParse = null;

                if (Enum.IsDefined(typeof(SVC_Messages), cmd))
                {
                    SVC_Messages msg = (SVC_Messages)cmd;
                    toParse = Assembly.GetExecutingAssembly().GetType("DemoInfo.Messages.CSVCMsg_" + msg.ToString().Substring(4));
                }
                else if (Enum.IsDefined(typeof(NET_Messages), cmd))
                {
                    NET_Messages msg = (NET_Messages)cmd;
                    toParse = Assembly.GetExecutingAssembly().GetType("DemoInfo.Messages.CNETMsg_" + msg.ToString().Substring(4));
                }

                if (toParse == null)  
                {
                    reader.ReadBytes(reader.ReadVarInt32());
                    continue;
                }


                var result = reader.ReadProtobufMessage(toParse, ProtoBuf.PrefixStyle.Base128);

                foreach (var parser in parsers)
                {
                    if (parser.CanHandleMessage(result))
                    {
                        parser.ApplyMessage(result, demo);

                        if(parser.GetPriority() > 0)
                            break;
                    }
                }
                
            }
        }
    }
}
