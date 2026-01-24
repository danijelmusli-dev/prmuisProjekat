using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MrezeProjekat.Models
{
    public class Instructions
    {
        private string[] _keys;
        private IPEndPoint _prevNode;
        private IPEndPoint _nextNode;

        public Instructions(int keyNum, IPEndPoint prev, IPEndPoint next) 
        {
            this._keys = new string[keyNum];
            this._prevNode = prev;  
            this._nextNode = next;
        }
        public Instructions(string[] keys, IPEndPoint prev, IPEndPoint next)
        {
            this._keys = keys;
            this._prevNode = prev;
            this._nextNode = next;
        }

        public string this[int index]
        {
            get => this._keys[index];
            set => this._keys[index] = value;
        }

        public IPEndPoint PrevNode => this._prevNode;
        public IPEndPoint NextNode => this._nextNode;

        public byte[] ToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // Keys
                bw.Write(_keys.Length);
                foreach (var k in _keys)
                {
                    bw.Write(k ?? string.Empty);
                }

                // PrevNode
                byte[] prevAddr = _prevNode.Address.GetAddressBytes();
                bw.Write(prevAddr.Length);
                bw.Write(prevAddr);
                bw.Write(_prevNode.Port);

                // NextNode
                byte[] nextAddr = _nextNode.Address.GetAddressBytes();
                bw.Write(nextAddr.Length);
                bw.Write(nextAddr);
                bw.Write(_nextNode.Port);

                return ms.ToArray();
            }
        }

        public static Instructions FromBytes(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                // Keys
                int keyCount = br.ReadInt32();
                string[] keys = new string[keyCount];
                for (int i = 0; i < keyCount; i++)
                {
                    keys[i] = br.ReadString();
                }

                // PrevNode
                int prevLen = br.ReadInt32();
                byte[] prevAddr = br.ReadBytes(prevLen);
                int prevPort = br.ReadInt32();
                IPEndPoint prevNode = new IPEndPoint(new IPAddress(prevAddr), prevPort);

                // NextNode
                int nextLen = br.ReadInt32();
                byte[] nextAddr = br.ReadBytes(nextLen);
                int nextPort = br.ReadInt32();
                IPEndPoint nextNode = new IPEndPoint(new IPAddress(nextAddr), nextPort);

                return new Instructions(keys, prevNode, nextNode);
            }
        }


    }
}
