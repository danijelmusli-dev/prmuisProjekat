using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrezeProjekat.Models
{
    public class Message
    {
        private string _content;
        private int _lenght;
        private int _checkSum;

        public Message(string content)
        {
            _content = content;
            this._lenght = content.Length;

            // Sum of all ASCII values of the string content
            this._checkSum = content.Sum(c => (int)c);
        }
        public Message(string content, int length, int checksum)
        {
            _content = content;
            this._lenght   = length;
            this._checkSum = checksum;
        }

        public string Content
        {
            get => _content;
            set => _content = value;
        }
        public int MessageLenght => _lenght;
        public int CheckSum => _checkSum;

        public byte[] ToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                #region Field serialization
                bw.Write(this._content);
                bw.Write(this._lenght);
                bw.Write(this._checkSum);

                return ms.ToArray();
                #endregion
            }
        }

        public static Message FromBytes(byte[] data) 
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                #region Field deserialisation
                string content = br.ReadString();
                int length     = br.ReadInt32();
                int checksum   = br.ReadInt32();
                return new Message(content, length, checksum);
                #endregion
            }
        }

        public override string ToString()
        {
            return $"\nMessage(Content: {this._content}, Length: {this._lenght}, CheckSum: {this._checkSum})";
        }

    }
}
