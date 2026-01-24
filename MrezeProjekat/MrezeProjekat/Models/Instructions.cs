using MrezeProjekat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Instructions
{
    private CryptoKey[] _keys;
    private IPEndPoint _prevNode;
    private IPEndPoint _nextNode;

    public Instructions(int keyNum, IPEndPoint prev, IPEndPoint next)
    {
        this._keys = new CryptoKey[keyNum];
        this._prevNode = prev;
        this._nextNode = next;
    }

    public Instructions(CryptoKey[] keys, IPEndPoint prev, IPEndPoint next)
    {
        this._keys = keys;
        this._prevNode = prev;
        this._nextNode = next;
    }

    // indeksator sada radi sa CryptoKey
    public CryptoKey this[int index]
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
                if (k == null)
                {
                    bw.Write(0); // dužina ključa = 0
                    bw.Write(0); // dužina IV = 0
                }
                else
                {
                    // Key
                    bw.Write(k.Key.Length);
                    bw.Write(k.Key);

                    // IV
                    bw.Write(k.IV.Length);
                    bw.Write(k.IV);
                }
            }

            // PrevNode
            if (_prevNode != null)
            {
                byte[] prevAddr = _prevNode.Address.GetAddressBytes();
                bw.Write(prevAddr.Length);
                bw.Write(prevAddr);
                bw.Write(_prevNode.Port);
            }
            else
            {
                bw.Write(0); // no address
                bw.Write(0); // port = 0
            }

            // NextNode
            if (_nextNode != null)
            {
                byte[] nextAddr = _nextNode.Address.GetAddressBytes();
                bw.Write(nextAddr.Length);
                bw.Write(nextAddr);
                bw.Write(_nextNode.Port);

            }
            else
            {
                bw.Write(0); // no address
                bw.Write(0); // port = 0
            }

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
            CryptoKey[] keys = new CryptoKey[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                int keyLen = br.ReadInt32();
                byte[] keyBytes = br.ReadBytes(keyLen);

                int ivLen = br.ReadInt32();
                byte[] ivBytes = br.ReadBytes(ivLen);

                if (keyLen > 0 && ivLen > 0)
                    keys[i] = new CryptoKey(keyBytes, ivBytes);
                else
                    keys[i] = null;
            }

            // PrevNode
            // FromBytes: handle zero-length addresses by using null endpoints
            int prevLen = br.ReadInt32();
            byte[] prevAddr = br.ReadBytes(prevLen);
            int prevPort = br.ReadInt32();
            IPEndPoint prevNode = (prevLen > 0) ? new IPEndPoint(new IPAddress(prevAddr), prevPort) : null;

            // NextNode
            int nextLen = br.ReadInt32();
            byte[] nextAddr = br.ReadBytes(nextLen);
            int nextPort = br.ReadInt32();
            IPEndPoint nextNode = (nextLen > 0) ? new IPEndPoint(new IPAddress(nextAddr), nextPort) : null;

            return new Instructions(keys, prevNode, nextNode);
        }
    }
}
