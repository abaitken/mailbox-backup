using System;
using System.Diagnostics;

namespace MailboxBackup
{
    class ConsoleProgressDisplay
    {
        private int _left;
        private int _top;
        private int _items;
        private int _currentItem;
        private Stopwatch _outputDelay;

        internal void Begin(int count)
        {
            _left = Console.CursorLeft;
            _top = Console.CursorTop;
            if (_left != 0)
            {
                Console.WriteLine();
                _left = Console.CursorLeft;
                _top = Console.CursorTop;
            }

            _items = count;
            _currentItem = 0;
            _outputDelay = new Stopwatch();
            _outputDelay.Start();
            UpdateImpl();
        }

        private void UpdateImpl()
        {
            Console.CursorLeft = _left;
            Console.CursorTop = _top;

            var itemDisplay = $@"{_currentItem} / {_items} ";
            var leftHandMarker = "[ ";
            var rightHandMarker = " ]";
            var availbleSpace = Console.BufferWidth - itemDisplay.Length - leftHandMarker.Length - rightHandMarker.Length - 1;
            int fillLength, remainingLength;
            var ratio = _currentItem == 0 ? 0 : (double)_currentItem / _items;
            fillLength = (int)(ratio * availbleSpace);
            remainingLength = availbleSpace - fillLength;
            var complete = new string('=', fillLength);
            var remaining = new string('-', remainingLength);
            Console.Write($@"{itemDisplay}{leftHandMarker}{complete}{remaining}{rightHandMarker}");
        }

        internal void Update()
        {
            _currentItem++;
            if (_outputDelay.ElapsedMilliseconds > 250)
            {
                UpdateImpl();
                _outputDelay.Restart();
            }
        }

        internal void End()
        {
            UpdateImpl();
            Console.WriteLine();
        }
    }
}