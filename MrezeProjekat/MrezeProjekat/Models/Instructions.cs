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
    private CryptoKey[] _keys;    // Array of cryptographic keys for each node in the path
    private IPEndPoint _prevNode; // Previous node's IP endpoint (null -> client)
    private IPEndPoint _nextNode; // Next     node's IP endpoint (null -> server)

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

    // indexing for CryptoKeys
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
            #region Field serialisation
            // Keys
            bw.Write(_keys.Length);
            foreach (var k in _keys)
            {
                if (k == null)
                {
                    bw.Write(0); // length key = 0
                    bw.Write(0); // length IV  = 0
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
            else // handling null endpoints by writing zero-length addresses and port 0
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
            #endregion
        }
    }

    public static Instructions FromBytes(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader br = new BinaryReader(ms))
        {
            #region Field deserialisation
            // Keys
            int keyCount = br.ReadInt32();
            CryptoKey[] keys = new CryptoKey[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                int keyLen = br.ReadInt32();    // serialising 
                byte[] keyBytes = br.ReadBytes(keyLen);

                int ivLen = br.ReadInt32();
                byte[] ivBytes = br.ReadBytes(ivLen);

                if (keyLen > 0 && ivLen > 0)
                    keys[i] = new CryptoKey(keyBytes, ivBytes);
                else
                    keys[i] = null;
            }

            // PrevNode
            int prevLen = br.ReadInt32();
            byte[] prevAddr = br.ReadBytes(prevLen);
            int prevPort = br.ReadInt32();
            // Handling zero-length addresses by using null endpoints
            IPEndPoint prevNode = (prevLen > 0) ? new IPEndPoint(new IPAddress(prevAddr), prevPort) : null;

            // NextNode
            int nextLen = br.ReadInt32();
            byte[] nextAddr = br.ReadBytes(nextLen);
            int nextPort = br.ReadInt32();
            IPEndPoint nextNode = (nextLen > 0) ? new IPEndPoint(new IPAddress(nextAddr), nextPort) : null;

            return new Instructions(keys, prevNode, nextNode);
            #endregion
        }
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();

        #region PrevNode
        builder.Append("Prev Node: ");
        builder.Append(this.PrevNode?.Address?.ToString() ?? "None");
        builder.Append("\tPort: ");
        builder.AppendLine(this.PrevNode?.Port.ToString() ?? "None");
        #endregion
        #region NextNode
        builder.Append("Next Node: ");
        builder.Append(this.NextNode?.Address?.ToString() ?? "None");
        builder.Append("\tPort: ");
        builder.AppendLine(this.NextNode?.Port.ToString() ?? "None");
        #endregion

        #region Keys
        foreach(CryptoKey key in this._keys)
        {  builder.AppendLine(key.ToString()); }
        #endregion

        return builder.ToString();
    }

}
