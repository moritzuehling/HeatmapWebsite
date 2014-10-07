﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DemoInfo
{
    static class Helper
    {
        public static string ReadCString(this BinaryReader reader, int length)
        {
            return ReadCString(reader, length, Encoding.Default);
        }

        public static string Reverse(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static string ReadCString(this BinaryReader reader, int length, Encoding encoding)
        {
            return encoding.GetString(reader.ReadBytes(length)).TrimEnd('\0');
        }

        public static int ReadVarInt32(this BinaryReader reader)
        {
            int b = 0, count = 0, result = 0;

            do
            {
                if (count > 5)
                    throw new InvalidDataException("VarInt32 out of range");

                b = reader.ReadByte();

                result |= (b & 0x7F) << (7 * count);

                count++;
            }
            while ((b & 0x80) != 0);

            return result;
        }

        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            return ReadNullTerminatedString(reader, Encoding.Default);
        }

        public static string ReadNullTerminatedString(this BinaryReader reader, Encoding encoding)
        {
            return ReadNullTerminatedString(reader, encoding, 512);
        }


        public static string ReadNullTerminatedString(this BinaryReader reader, Encoding encoding, int initialBufferSize)
        {
            List<byte> result = new List<byte>(initialBufferSize);

            while (true)
            {
                byte b = reader.ReadByte();
                
                if(b == 0)
                    break;

                result.Add(b);
            }

            return Encoding.Default.GetString(result.ToArray());
        }

        public static IEnumerable<Enum> GetFlagsOfEnum(Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        public static byte[] ReadVolvoPacket(this BinaryReader reader)
        {
            return reader.ReadBytes(reader.ReadInt32());
        }

        public static T ReadProtobufMessage<T>(this BinaryReader reader)
        {
            return ReadProtobufMessage<T>(reader, PrefixStyle.Base128);
        }

        public static T ReadProtobufMessage<T>(this BinaryReader reader, PrefixStyle style)
        {
            return ProtoBuf.Serializer.DeserializeWithLengthPrefix<T>(reader.BaseStream, style);
        }

        public static IExtensible ReadProtobufMessage(this BinaryReader reader, Type T, PrefixStyle style)
        {
            var type = typeof(ProtoBuf.Serializer);
            var deserialize = type.GetMethod("DeserializeWithLengthPrefix", new Type[] { typeof(Stream), typeof(PrefixStyle)});

            deserialize = deserialize.MakeGenericMethod(T);

            return (IExtensible)deserialize.Invoke(null, new object[] { reader.BaseStream, style });
        }
    }
}
