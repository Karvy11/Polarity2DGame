// Pooled undo stack of MoveRecords.

using System.Collections.Generic;

namespace Polarity.Core
{
    public sealed class MoveHistory
    {
        private readonly List<MoveRecord> _stack = new List<MoveRecord>();
        private readonly Stack<MoveRecord> _pool = new Stack<MoveRecord>();

        public int Count => _stack.Count;
        public bool CanUndo => _stack.Count > 0;

        public MoveRecord Rent()
        {
            if (_pool.Count > 0)
            {
                MoveRecord pooled = _pool.Pop();
                pooled.Reset();
                return pooled;
            }

            return new MoveRecord();
        }

        public void Push(MoveRecord record) => _stack.Add(record);

        public MoveRecord Pop()
        {
            int last = _stack.Count - 1;
            MoveRecord record = _stack[last];
            _stack.RemoveAt(last);
            return record;
        }

        public void Recycle(MoveRecord record)
        {
            record.Reset();
            _pool.Push(record);
        }

        public void Clear()
        {
            for (int i = 0; i < _stack.Count; i++) Recycle(_stack[i]);
            _stack.Clear();
        }
    }
}
