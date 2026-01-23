using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MrezeProjekat.Models
{
    internal class Instructions
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

        public string this[int index]
        {
            get => this._keys[index];
            set => this._keys[index] = value;
        }

        public IPEndPoint PrevNode => this._prevNode;
        public IPEndPoint NextNode => this._nextNode;

    }
}
