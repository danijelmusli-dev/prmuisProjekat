using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrezeProjekat
{
    public class CryptoKey
    {
        private byte[] _key;
        private byte[] _iv;

        public CryptoKey() { }
        public CryptoKey(byte[] key, byte[] iv)
        {
            this._key = key;
            this._iv = iv;
        }

        public byte[] Key => this._key;
        public byte[] IV  => this._iv;

    }
}
