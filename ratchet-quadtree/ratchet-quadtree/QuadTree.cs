using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ratchet.Collections
{
    public class Quadtree<T> : IEnumerable<Quadtree<T>.Node>
    {

        public class Node : IEnumerable<Node>
        {
            long _X;
            long _Y;
            ulong _Size;

            public long X { get { return _X; } }
            public long Y { get { return _Y; } }
            public ulong Size { get { return _Size; } }

            internal Node(long X, long Y, ulong Size)
            {
                _X = X;
                _Y = Y;
                _Size = Size;
            }

            internal Node _0_0 = null;
            internal Node _1_0 = null;
            internal Node _0_1 = null;
            internal Node _1_1 = null;

            public void DeleteChildren()
            {
                _0_0 = null;
                _1_0 = null;
                _0_1 = null;
                _1_1 = null;
            }

            T _Element;

            public T Element { get { return _Element; } set { _Element = value; } }

            internal Node FindNode(long x, long y, ulong width, ulong height)
            {
                Node output = null;
                if ((ulong)(x - _X) + width > _Size) { return null; }
                if ((ulong)(y - _Y) + height > _Size) { return null; }

                if (_0_0 == null)
                {
                    if (_Size > 1)
                    {
                        _0_0 = new Node(_X, _Y, _Size / 2UL);
                        _1_0 = new Node(_X + (long)(_Size / 2UL), _Y, _Size / 2UL);
                        _0_1 = new Node(_X, _Y + (long)(_Size / 2UL), _Size / 2UL);
                        _1_1 = new Node(_X + (long)(_Size / 2UL), _Y + (long)(_Size / 2UL), _Size / 2UL);
                    }
                    else
                    {
                        if (x == _X && y == _Y) { return this; }
                        else { return null; }
                    }
                }

                if (x >= _1_0._X)
                {
                    if (y >= _0_1._Y)
                    {
                        output = _1_1.FindNode(x, y, width, height);
                        if (output != null) { return output; }
                        return this;
                    }
                    else
                    {
                        output = _1_0.FindNode(x, y, width, height);
                        if (output != null) { return output; }
                        return this;
                    }
                }
                else
                {
                    if (y >= _0_1._Y)
                    {
                        output = _0_1.FindNode(x, y, width, height);
                        if (output != null) { return output; }
                        return this;
                    }
                    else
                    {
                        output = _0_0.FindNode(x, y, width, height);
                        if (output != null) { return output; }
                        return this;
                    }
                }
            }

            IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
            {
                return new QuadTreeNodeEnum(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new QuadTreeNodeEnum(this);
            }

            internal IEnumerator<Node> CreateEnumerator()
            {
                return new QuadTreeNodeEnum(this);
            }

            internal IEnumerator<Node> CreateEnumerator(QuadTreeNodeEnum_WithFilter.Filter FilterOut)
            {
                return new QuadTreeNodeEnum_WithFilter(this, FilterOut);
            }

            internal IEnumerator<Node> CreateBoxEnumerator(long X, long Y, ulong Width, ulong Height)
            {
                return new QuadTreeNodeEnum_Box(this, X, Y, Width, Height);
            }

            internal class QuadTreeNodeEnum : IEnumerator<Node>
            {

                public QuadTreeNodeEnum(Node Root)
                {
                    _RootNode = Root;
                    for (int n = 0; n < _NodeChain.Length; n++) { _NodeChain[n] = new QuadTreeNodeEnumState(); }
                    Reset();
                }

                class QuadTreeNodeEnumState
                {
                    internal Node _Node;
                    internal int _SubdivisionIndex = 0;
                }

                bool _First = false;
                Node _RootNode;
                QuadTreeNodeEnumState[] _NodeChain = new QuadTreeNodeEnumState[64];
                int _NodeChainIndex = 0;

                public Node Current
                {
                    get
                    {
                        return _NodeChain[_NodeChainIndex]._Node;
                    }
                }

                object IEnumerator.Current { get { return Current; } }

                public void Dispose() { }

                public bool MoveNext()
                {
                    // MoveNext is call first so we need a special case for the initialization
                    if (_First)
                    {
                        _First = false;
                        if (_RootNode != null) { return true; }
                        else { return false; }
                    }

                    restart:
                    QuadTreeNodeEnumState _Parent = _NodeChain[_NodeChainIndex];
                    if (_Parent._Node._0_0 != null)
                    {
                        _Parent._SubdivisionIndex++;
                        _NodeChainIndex++;
                        _NodeChain[_NodeChainIndex]._SubdivisionIndex = 0;

                        switch (_Parent._SubdivisionIndex)
                        {
                            case 1:
                                _NodeChain[_NodeChainIndex]._Node = _Parent._Node._0_0;
                                return true;
                            case 2:
                                _NodeChain[_NodeChainIndex]._Node = _Parent._Node._0_1;
                                return true;
                            case 3:
                                _NodeChain[_NodeChainIndex]._Node = _Parent._Node._1_0;
                                return true;
                            case 4:
                                _NodeChain[_NodeChainIndex]._Node = _Parent._Node._1_1;
                                return true;
                            default:
                                if (_NodeChainIndex == 1) { return false; }
                                _NodeChainIndex -= 2;
                                goto restart;
                        }
                    }
                    else
                    {
                        if (_NodeChainIndex == 0) { return false; }
                        _NodeChainIndex -= 1;
                        goto restart;
                    }
                }

                public void Reset()
                {
                    _First = true;
                    _NodeChainIndex = 0;
                    _NodeChain[0]._Node = _RootNode;
                    _NodeChain[0]._SubdivisionIndex = 0;

                }
            }
        }

        internal class QuadTreeNodeEnum_WithFilter : IEnumerator<Node>
        {
            public delegate bool Filter(Node Node);
            Filter _FilterIn;
            Filter _FilterOut;

            public QuadTreeNodeEnum_WithFilter(Node Root, Filter FilterOut)
            {
                _FilterOut = FilterOut;
                if (_FilterOut(Root)) { _RootNode = null; }
                else { _RootNode = Root; }
                for (int n = 0; n < _NodeChain.Length; n++) { _NodeChain[n] = new QuadTreeNodeEnumState(); }
                Reset();
            }

            class QuadTreeNodeEnumState
            {
                internal Node _Node;
                internal int _SubdivisionIndex = 0;
                internal bool _FilteredIn;
            }

            bool _First = false;
            Node _RootNode;
            QuadTreeNodeEnumState[] _NodeChain = new QuadTreeNodeEnumState[64];
            int _NodeChainIndex = 0;

            public Node Current
            {
                get
                {
                    return _NodeChain[_NodeChainIndex]._Node;
                }
            }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() { }

            public bool MoveNext()
            {
                // MoveNext is call first so we need a special case for the initialization
                if (_First)
                {
                    _First = false;
                    if (_RootNode != null) { return true; }
                    else { return false; }
                }

                restart:
                QuadTreeNodeEnumState _Parent = _NodeChain[_NodeChainIndex];
                if (_Parent._Node._0_0 != null && !_FilterOut(_Parent._Node))
                {
                    _Parent._SubdivisionIndex++;
                    _NodeChainIndex++;
                    _NodeChain[_NodeChainIndex]._SubdivisionIndex = 0;

                    switch (_Parent._SubdivisionIndex)
                    {
                        case 1:
                            _NodeChain[_NodeChainIndex]._Node = _Parent._Node._0_0;
                            return true;
                        case 2:
                            _NodeChain[_NodeChainIndex]._Node = _Parent._Node._0_1;
                            return true;
                        case 3:
                            _NodeChain[_NodeChainIndex]._Node = _Parent._Node._1_0;
                            return true;
                        case 4:
                            _NodeChain[_NodeChainIndex]._Node = _Parent._Node._1_1;
                            return true;
                        default:
                            if (_NodeChainIndex == 1) { return false; }
                            _NodeChainIndex -= 2;
                            goto restart;
                    }
                }
                else
                {
                    if (_NodeChainIndex == 0) { return false; }
                    _NodeChainIndex -= 1;
                    goto restart;
                }
            }

            public void Reset()
            {
                _First = true;
                _NodeChainIndex = 0;
                _NodeChain[0]._Node = _RootNode;
                _NodeChain[0]._SubdivisionIndex = 0;

            }
        }



        internal class QuadTreeNodeEnum_Box : IEnumerator<Node>
        {
            long _X;
            long _Y;
            long _Width;
            long _Height;

            long _EndX;
            long _EndY;

            public QuadTreeNodeEnum_Box(Node Root, long X, long Y, ulong Width, ulong Height)
            {
                _X = X;
                _Y = Y;
                if (Width > (ulong)long.MaxValue) { throw new Exception("Box query is too big"); }
                if (Height > (ulong)long.MaxValue) { throw new Exception("Box query is too big"); }

                _Width = (long)Width;
                _Height = (long)Height;
                _EndX = _X + (long)_Width;
                _EndY = _Y + (long)_Height;
                _RootNode = Root;
                for (int n = 0; n < _NodeChain.Length; n++) { _NodeChain[n] = new QuadTreeNodeEnumState(); }
                Reset();
            }

            class QuadTreeNodeEnumState
            {
                internal Node _Node;
                internal int _SubdivisionIndex = 0;
                internal bool _Included = false;
            }

            bool _First = false;
            Node _RootNode;
            QuadTreeNodeEnumState[] _NodeChain = new QuadTreeNodeEnumState[64];
            int _NodeChainIndex = 0;

            public Node Current
            {
                get
                {
                    return _NodeChain[_NodeChainIndex]._Node;
                }
            }

            object IEnumerator.Current { get { return Current; } }

            bool Intersect(Node Node)
            {
                long node_maxx = Node.X + (long)Node.Size;
                long node_maxy = Node.Y + (long)Node.Size;
                return Node.X < _EndX && node_maxx > _X && Node.Y < _EndY && node_maxy > _Y;
            }

            bool Included(Node Node)
            {
                long node_maxx = Node.X + (long)Node.Size;
                long node_maxy = Node.Y + (long)Node.Size;
                return Node.X > _X &&  Node.Y > _Y && node_maxx < _Width && node_maxy < _Height;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                // MoveNext is call first so we need a special case for the initialization
                if (_First)
                {
                    _First = false;
                    if (_RootNode != null) { return true; }
                    else { return false; }
                }

                restart:
                QuadTreeNodeEnumState _Parent = _NodeChain[_NodeChainIndex];
                if (_Parent._Node._0_0 != null && (_Parent._Included || Intersect(_Parent._Node)))
                {
                    _Parent._SubdivisionIndex++;
                    _NodeChainIndex++;
                    _NodeChain[_NodeChainIndex]._SubdivisionIndex = 0;

                    switch (_Parent._SubdivisionIndex)
                    {
                        case 1:
                            _NodeChain[_NodeChainIndex]._Node = _Parent._Node._0_0;
                            if (_Parent._Included) { _NodeChain[_NodeChainIndex]._Included = true; }
                            else { _NodeChain[_NodeChainIndex]._Included = Included(_NodeChain[_NodeChainIndex]._Node); }
                            return true;
                        case 2:
                            _NodeChain[_NodeChainIndex]._Node = _Parent._Node._0_1;
                            if (_Parent._Included) { _NodeChain[_NodeChainIndex]._Included = true; }
                            else { _NodeChain[_NodeChainIndex]._Included = Included(_NodeChain[_NodeChainIndex]._Node); }
                            return true;
                        case 3:
                            _NodeChain[_NodeChainIndex]._Node = _Parent._Node._1_0;
                            if (_Parent._Included) { _NodeChain[_NodeChainIndex]._Included = true; }
                            else { _NodeChain[_NodeChainIndex]._Included = Included(_NodeChain[_NodeChainIndex]._Node); }
                            return true;
                        case 4:
                            _NodeChain[_NodeChainIndex]._Node = _Parent._Node._1_1;
                            if (_Parent._Included) { _NodeChain[_NodeChainIndex]._Included = true; }
                            else { _NodeChain[_NodeChainIndex]._Included = Included(_NodeChain[_NodeChainIndex]._Node); }
                            return true;
                        default:
                            if (_NodeChainIndex == 1)
                            { return false; }
                            _NodeChainIndex -= 2;
                            goto restart;
                    }
                }
                else
                {
                    if (_NodeChainIndex == 0)
                    { return false; }
                    _NodeChainIndex -= 1;
                    goto restart;
                }
            }

            public void Reset()
            {
                _First = true;
                _NodeChainIndex = 0;
                _NodeChain[0]._Node = _RootNode;
                _NodeChain[0]._SubdivisionIndex = 0;

            }
        }


        Node _RootNode = null;

        public Quadtree(ulong Size)
        {
            if (Size > ulong.MaxValue / 2UL + 1UL) { throw new Exception("The size must be lower or equal to 0x" + (ulong.MaxValue / 2UL + 1UL).ToString("X")); }
            long minValue = -(long)Size / 2L;
            _RootNode = new Node(minValue, minValue, Size);
        }

        public Quadtree() : this(ulong.MaxValue / 2UL + 1UL)
        {
        }

        IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
        {
            return _RootNode.CreateEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _RootNode.CreateEnumerator();
        }

        public Node GetNode(long x, long y, ulong width, ulong height)
        {
            return _RootNode.FindNode(x, y, width, height);
        }

        class QueryWrapper<T> : IEnumerable<T>
        {
            IEnumerator<T> _Enumerator;
            public QueryWrapper(IEnumerator<T> Enumerator)
            {
                _Enumerator = Enumerator;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _Enumerator;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _Enumerator;
            }
        }

        public IEnumerable<Node> Query(long x, long y, ulong width, ulong height)
        {
            return new QueryWrapper<Node>(GetEnumerator(x, y, width, height));
        }

        public IEnumerator<Node> GetEnumerator(long x, long y, ulong width, ulong height)
        {
            var rootNode = _RootNode.FindNode(x, y, width, height);
            if (rootNode.X == x && rootNode.Y == y && rootNode.Size == width && width == height) { return rootNode.CreateEnumerator(); }
            return rootNode.CreateBoxEnumerator(x, y, width, height);
        }
    }
}
