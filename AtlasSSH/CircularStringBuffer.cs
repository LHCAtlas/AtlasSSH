using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    /// <summary>
    /// A ciruclar string buffer
    /// </summary>
    public class CircularStringBuffer
    {
        private char[] _buffer;
        private readonly int _buffer_size;
        private int _next_insertion_point;
        private int _buffer_used_index;

        /// <summary>
        /// Create a circular string buffer with the given size.
        /// </summary>
        /// <param name="size"></param>
        public CircularStringBuffer(int size)
        {
            _buffer_size = size;
            _buffer = new char[_buffer_size];
            _next_insertion_point = 0;
            _buffer_used_index = 0;
        }

        /// <summary>
        /// Add a string to the buffer
        /// </summary>
        /// <param name="s"></param>
        public void Add (string s)
        {
            var arr = (s ?? throw new ArgumentNullException("String can't be null when added"))
                .ToCharArray();
            foreach (var c in arr)
            {
                _buffer[_next_insertion_point % _buffer_size] = c;
                _next_insertion_point++;
            }
            var tmax = _next_insertion_point > _buffer_size
                ? _buffer_size
                : _next_insertion_point;
            _buffer_used_index = _buffer_used_index > tmax
                ? _buffer_used_index
                : tmax;
            _next_insertion_point = _next_insertion_point % _buffer_size;
        }

        /// <summary>
        /// Return the circular array as a proper, unordered, string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var first_char = _buffer_used_index == _buffer_size
                ? _next_insertion_point
                : 0;
            var unwound = Enumerable.Range(first_char, _buffer_used_index)
                .Select(index => _buffer[index % _buffer_size])
                .ToArray();
            return new string(unwound);
        }
    }
}
