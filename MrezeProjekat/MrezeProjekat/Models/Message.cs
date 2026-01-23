using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrezeProjekat.Models
{
    internal class Message
    {
        private string _content;
        private int _lenght;
        private int _checkSum;

        public Message() { }
        public Message(string content)
        {
            _content = content;
            this._lenght = content.Length;

            // Sum of all ASCII values of the string content
            this._checkSum = content.Sum(c => (int)c);
        }

        // Only getters
        public string Content => _content;
        public int Lenght => _lenght;
        public int CheckSum => _checkSum;

    }
}
