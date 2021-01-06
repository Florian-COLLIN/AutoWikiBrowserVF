﻿/*
 * Diff Algorithm in C#
 * Based on Tye McQueen's Algorithm::Diff Perl module version 1.19_01
 * Converted to C# by Joshua Tauberer <tauberer@for.net>
 * 
 * The Perl module's copyright notice:
 * Parts Copyright (c) 2000-2004 Ned Konz.  All rights reserved.
 * Parts by Tye McQueen.
 *
 * The Perl module's readme has a ridiculously long list of
 * thanks for all of the previous authors, who are:
 * Mario Wolczko (author of SmallTalk code the module is based on)
 * Ned Konz
 * Mark-Jason Dominus
 * Mike Schilli
 * Amir Karger
 * Christian Murphy
 *
 * The Perl module was released under the Perl Artistic License,
 * and I leave my additions in the public domain, so I leave
 * it up to you to figure out what you need to do if you want
 * to distribute this file in some form.
 *
 * AWB reuse note: from "you can redistribute it and/or modify it under the same terms
 * as Perl itself" on CPAN it has been deduced that we could use it as Perl is currently
 * multi-licensed under Artistic and GPL.
 */
using System;
using System.Collections;
using System.Text;

using IntList = System.Collections.Generic.List<int>;
//using TrioList = System.Collections.Generic.List<Algorithm.Diff.Trio>;
using TrioList = System.Collections.ArrayList;

namespace WikiFunctions
{
    public interface IDiff : IEnumerable
    {
        IList Left { get; }
        IList Right { get; }
    }

    public abstract class Hunk
    {
        internal Hunk()
        {
        }

        public abstract int ChangedLists { get; }

        public abstract bool Same { get; }
        public abstract bool Conflict { get; }

        public abstract bool IsSame(int index);

        public abstract Range Original();
        public abstract Range Changes(int index);

        public int MaxLines()
        {
            int m = Original().Count;
            for (int i = 0; i < ChangedLists; i++)
                if (Changes(i).Count > m)
                    m = Changes(i).Count;
            return m;
        }
    }

    public class Diff : IDiff
    {
        internal IList left, right;
        private readonly IEqualityComparer comparer;

        public IList Left
        {
            get { return left; }
        }

        public IList Right
        {
            get { return right; }
        }

        private class Trio
        {
            public readonly Trio a;
            public readonly int b, c;

            public Trio(Trio a, int b, int c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            }
        }

        public class Hunk : WikiFunctions.Hunk
        {
            private IList left, right;
            private readonly int s1start, s1end, s2start, s2end;
            private readonly bool same;

            internal Hunk(IList left, IList right, int s1start, int s1end, int s2start, int s2end, bool same)
            {
                this.left = left;
                this.right = right;
                this.s1start = s1start;
                this.s1end = s1end;
                this.s2start = s2start;
                this.s2end = s2end;
                this.same = same;
            }

            internal void SetLists(IList left, IList right)
            {
                this.left = left;
                this.right = right;
            }

            public override int ChangedLists
            {
                get { return 1; }
            }

            public override bool Same
            {
                get { return same; }
            }

            public override bool Conflict
            {
                get { return false; }
            }

            public override bool IsSame(int index)
            {
                if (index != 0) throw new ArgumentException();
                return Same;
            }

            private Range get(int seq)
            {
                int start = (seq == 1 ? s1start : s2start);
                int end = (seq == 1 ? s1end : s2end);
                IList list = (seq == 1 ? left : right);
                if (end < start)
                {
                    return new Range(list, start, 0);
                }
                return new Range(list, start, end - start + 1);
            }

            public Range Left
            {
                get { return get(1); }
            }

            public Range Right
            {
                get { return get(2); }
            }

            public override Range Original()
            {
                return Left;
            }

            public override Range Changes(int index)
            {
                if (index != 0) throw new ArgumentException();
                return Right;
            }

            public override int GetHashCode()
            {
                return unchecked(s1start + s1end + s2start + s2end);
            }

            public override bool Equals(object o)
            {
                Hunk h = o as Hunk;
                return
                    h != null &&
                    s1start == h.s1start &&
                    s1start == h.s1end &&
                    s1start == h.s2start &&
                    s1start == h.s2end &&
                    same == h.same;
            }

            public override string ToString()
            {
                if (left == null || right == null)
                    return base.ToString();
                return DiffString();
            }

            public string DiffString()
            {
                if (left == null || right == null)
                {
                    throw new InvalidOperationException(
                        "This hunk is based on a patch which does not have the compared data.");
                }

                StringBuilder ret = new StringBuilder();

                if (Same)
                {
                    foreach (object item in Left)
                    {
                        ret.Append(" ");
                        ret.Append(item);
                        ret.Append("\n");
                    }
                }
                else
                {
                    foreach (object item in Left)
                    {
                        ret.Append("<");
                        ret.Append(item);
                        ret.Append("\n");
                    }
                    foreach (object item in Right)
                    {
                        ret.Append(">");
                        ret.Append(item);
                        ret.Append("\n");
                    }
                }

                return ret.ToString();
            }

            internal Hunk Crop(int shiftstart, int shiftend)
            {
                return new Hunk(left, right, Left.Start + shiftstart, Left.End - shiftend, Right.Start + shiftstart,
                                Right.End - shiftend, same);
            }

            internal Hunk Reverse()
            {
                return new Hunk(right, left, Right.Start, Right.End, Left.Start, Left.End, same);
            }
        }

        public Diff(IList left, IList right, IEqualityComparer comparer)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            this.left = left;
            this.right = right;
            this.comparer = comparer;
            init();
        }

        public Diff(string leftFile, string rightFile, bool caseSensitive, bool compareWhitespace)
            : this(
                UnifiedDiff.LoadFileLines(leftFile), UnifiedDiff.LoadFileLines(rightFile), caseSensitive,
                compareWhitespace)
        {
        }

        public Diff(string[] left, string[] right, bool caseSensitive, bool compareWhitespace)
            : this(
                StripWhitespace(left, !compareWhitespace),
                StripWhitespace(right, !compareWhitespace),
                caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase
                )
        {
        }

        ////////////////////////////////////////////////////////////


        private static string[] StripWhitespace(string[] lines, bool strip)
        {
            if (lines == null)
            {
                throw new ArgumentNullException();
            }
            if (!strip)
            {
                return lines;
            }
            string[] ret = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                StringBuilder sb = new StringBuilder();
                foreach (char c in lines[i])
                    if (!char.IsWhiteSpace(c))
                        sb.Append(c);
                ret[i] = sb.ToString();
            }
            return ret;
        }

        ////////////////////////////////////////////////////////////

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (cdif == null)
            {
                throw new InvalidOperationException("No comparison has been performed.");
            }
            return new Enumerator(this);
        }

        public override string ToString()
        {
            System.IO.StringWriter w = new System.IO.StringWriter();
            UnifiedDiff.WriteUnifiedDiff(this, w);
            return w.ToString();
        }

        public Patch CreatePatch()
        {
            int ctr = 0;
            foreach (Hunk hunk in this)
            {
                if (!hunk.Same)
                {
                    ctr += hunk.Right.Count;
                }
            }

            object[] rightData = new object[ctr];

            ArrayList hunks = new ArrayList();
            ctr = 0;
            foreach (Hunk hunk in this)
            {
                if (hunk.Same)
                {
                    hunks.Add(new Patch.Hunk(rightData, hunk.Left.Start, hunk.Left.Count, 0, 0, true));
                }
                else
                {
                    hunks.Add(new Patch.Hunk(rightData, hunk.Left.Start, hunk.Left.Count, ctr, hunk.Right.Count, false));
                    foreach (object t in hunk.Right)
                    {
                        rightData[ctr++] = t;
                    }
                }
            }


            return new Patch((Patch.Hunk[]) hunks.ToArray(typeof (Patch.Hunk)));
        }

        /*
        # McIlroy-Hunt diff algorithm
        # Adapted from the Smalltalk code of Mario I. Wolczko, <mario@wolczko.com>
        # by Ned Konz, perl@bike-nomad.com
        # Updates by Tye McQueen, http://perlmonks.org/?node=tye
		
        # Create a hash that maps each element of $aCollection to the set of
        # positions it occupies in $aCollection, restricted to the elements
        # within the range of indexes specified by $start and $end.
        # The fourth parameter is a subroutine reference that will be called to
        # generate a string to use as a key.
        # Additional parameters, if any, will be passed to this subroutine.
        #
        # my $hashRef = _withPositionsOfInInterval( \@array, $start, $end, $keyGen );
        */

        private Hashtable _withPositionsOfInInterval(IList aCollection, int start, int end)
        {
            Hashtable d = new Hashtable(comparer);
            for (int index = start; index <= end; index++)
            {
                object element = aCollection[index];
                if (d.ContainsKey(element))
                {
                    IntList list = (IntList) d[element];
                    list.Add(index);
                }
                else
                {
                    IntList list = new IntList {index};
                    d[element] = list;
                }
            }
            foreach (IntList list in d.Values)
            {
                list.Reverse();
            }
            return d;
        }

        /*
        # Find the place at which aValue would normally be inserted into the
        # array. If that place is already occupied by aValue, do nothing, and
        # return undef. If the place does not exist (i.e., it is off the end of
        # the array), add it to the end, otherwise replace the element at that
        # point with aValue.  It is assumed that the array's values are numeric.
        # This is where the bulk (75%) of the time is spent in this module, so
        # try to make it fast!
        */
        // NOTE: Instead of returning undef, it returns -1.
        private int _replaceNextLargerWith(IntList array, int value, int high)
        {
            if (high <= 0)
            {
                high = array.Count - 1;
            }

            // off the end?
            if (high == -1 || value > array[array.Count - 1])
            {
                array.Add(value);
                return array.Count - 1;
            }

            // binary search for insertion point...
            int low = 0;
            while (low <= high)
            {
                int index = (high + low)/2;

                int found = array[index];

                if (value == found)
                {
                    return -1;
                }
                if (value > found)
                    low = index + 1;
                else
                    high = index - 1;
            }

            // # now insertion point is in $low.
            array[low] = value; // overwrite next larger
            return low;
        }

        /*
        # This method computes the longest common subsequence in $a and $b.
		
        # Result is array or ref, whose contents is such that
        #   $a->[ $i ] == $b->[ $result[ $i ] ]
        # foreach $i in ( 0 .. $#result ) if $result[ $i ] is defined.
		
        # An additional argument may be passed; this is a hash or key generating
        # function that should return a string that uniquely identifies the given
        # element.  It should be the case that if the key is the same, the elements
        # will compare the same. If this parameter is undef or missing, the key
        # will be the element as a string.
		
        # By default, comparisons will use "eq" and elements will be turned into keys
        # using the default stringizing operator '""'.
		
        # Additional parameters, if any, will be passed to the key generation
        # routine.
        */

        private bool compare(object a, object b)
        {
            return comparer == null ? a.Equals(b) : comparer.Equals(a, b);
        }

        private bool IsPrepared(out Hashtable bMatches)
        {
            bMatches = null;
            return false;
        }

        private IntList _longestCommonSubsequence(IList a, IList b)
        {
            int aStart = 0;
            int aFinish = a.Count - 1;
            IntList matchVector = new IntList();
            Hashtable bMatches;

            // initialize matchVector to length of a
            for (int i = 0; i < a.Count; i++)
                matchVector.Add(-1);

            if (!IsPrepared(out bMatches))
            {
                int bStart = 0;
                int bFinish = b.Count - 1;

                // First we prune off any common elements at the beginning
                while (aStart <= aFinish && bStart <= bFinish && compare(a[aStart], b[bStart]))
                    matchVector[aStart++] = bStart++;

                // now the end
                while (aStart <= aFinish && bStart <= bFinish && compare(a[aFinish], b[bFinish]))
                    matchVector[aFinish--] = bFinish--;

                // Now compute the equivalence classes of positions of elements
                bMatches =
                    _withPositionsOfInInterval(b, bStart, bFinish);
            }

            IntList thresh = new IntList();
            TrioList links = new TrioList();

            for (int i = aStart; i <= aFinish; i++)
            {
                IntList aimatches = (IntList) bMatches[a[i]];
                if (aimatches != null)
                {
                    int k = 0;
                    foreach (int j in aimatches)
                    {
                        // # optimization: most of the time this will be true
                        if (k > 0 && thresh[k] > j && thresh[k - 1] < j)
                            thresh[k] = j;
                        else
                            k = _replaceNextLargerWith(thresh, j, k);

                        // oddly, it's faster to always test this (CPU cache?).
                        if (k != -1)
                        {
                            Trio t = new Trio((Trio) (k > 0 ? links[k - 1] : null), i, j);
                            if (k == links.Count)
                                links.Add(t);
                            else
                                links[k] = t;
                        }
                    }
                }
            }

            if (thresh.Count > 0)
            {
                for (Trio link = (Trio) links[thresh.Count - 1]; link != null; link = link.a)
                    matchVector[link.b] = link.c;
            }

            return matchVector;
        }

        /*void prepare(IList list) {
            prepared = _withPositionsOfInInterval(list, 0, list.Count-1);
            preparedlist = list;
        }*/

        private void LCSidx(IList a, IList b, out IntList am, out IntList bm)
        {
            IntList match = _longestCommonSubsequence(a, b);
            am = new IntList();
            for (int i = 0; i < match.Count; i++)
                if (match[i] != -1)
                    am.Add(i);
            bm = new IntList();
            for (int vi = 0; vi < am.Count; vi++)
                bm.Add(match[am[vi]]);
        }

        private IntList compact_diff(IList a, IList b)
        {
            IntList am, bm;
            LCSidx(a, b, out am, out bm);
            IntList cdiff = new IntList();
            int ai = 0, bi = 0;
            cdiff.Add(ai);
            cdiff.Add(bi);
            while (true)
            {
                while (am.Count > 0 && ai == am[0] && bi == bm[0])
                {
                    am.RemoveAt(0);
                    bm.RemoveAt(0);
                    ++ai;
                    ++bi;
                }

                cdiff.Add(ai);
                cdiff.Add(bi);
                if (am.Count == 0) break;
                ai = am[0];
                bi = bm[0];
                cdiff.Add(ai);
                cdiff.Add(bi);
            }

            if (ai < a.Count || bi < b.Count)
            {
                cdiff.Add(a.Count);
                cdiff.Add(b.Count);
            }

            return cdiff;
        }

        private int _End;
        private bool _Same;
        private IntList cdif;

        private void init()
        {
            cdif = compact_diff(left, right);
            _Same = true;
            if (0 == cdif[2] && 0 == cdif[3])
            {
                _Same = false;
                cdif.RemoveAt(0);
                cdif.RemoveAt(0);
            }

            _End = (1 + cdif.Count)/2;
        }

        private class Enumerator : IEnumerator
        {
            private readonly Diff diff;
            private int _Pos, _Off;

            public Enumerator(Diff diff)
            {
                this.diff = diff;
                Reset();
            }

            public object Current
            {
                get
                {
                    _ChkPos();
                    return gethunk();
                }
            }

            public bool MoveNext()
            {
                return next();
            }

            public void Reset()
            {
                reset(0);
            }

            private void _ChkPos()
            {
                if (_Pos == 0)
                {
                    throw new InvalidOperationException("Position is reset.");
                }
            }

            private void reset(int pos)
            {
                if (pos < 0 || diff._End <= pos) pos = -1;
                _Pos = pos;
                _Off = 2*pos - 1;
            }

            private bool next()
            {
                reset(_Pos + 1);
                return _Pos != -1;
            }

            private Hunk gethunk()
            {
                _ChkPos();

                int off1 = 1 + _Off;
                int off2 = 2 + _Off;

                int a1 = diff.cdif[off1 - 2];
                int a2 = diff.cdif[off1] - 1;
                int b1 = diff.cdif[off2 - 2];
                int b2 = diff.cdif[off2] - 1;

                bool s = same();
                return new Hunk(diff.left, diff.right, a1, a2, b1, b2, s);
            }

            private bool same()
            {
                _ChkPos();
                if (diff._Same != ((1 & _Pos) != 0))
                    return false;
                return true;
            }
        }
    }

    public class Range : IList
    {
        private readonly IList list;
        private readonly int start, count;

        private static readonly ArrayList EmptyList = new ArrayList();

        public Range(IList list, int start, int count)
        {
            this.list = list;
            this.start = start;
            this.count = count;
        }

        public int Start
        {
            get { return start; }
        }

        public int Count
        {
            get { return count; }
        }

        public int End
        {
            get { return start + count - 1; }
        }

        private void Check()
        {
            if (count > 0 && list == null)
                throw new InvalidOperationException("This range does not refer to a list with data.");
        }

        public object this[int index]
        {
            get
            {
                Check();
                if (index < 0 || index >= count)
                    throw new ArgumentException("index");
                return list[index + start];
            }
        }

        // IEnumerable Functions

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (count == 0 && list == null) return EmptyList.GetEnumerator();
            Check();
            return new Enumer(this);
        }

        private class Enumer : IEnumerator
        {
            private readonly Range list;
            private int index = -1;

            public Enumer(Range list)
            {
                this.list = list;
            }

            public void Reset()
            {
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < list.Count;
            }

            public object Current
            {
                get { return list[index]; }
            }
        }

        // ICollection Functions

        void ICollection.CopyTo(Array array, int index)
        {
            Check();
            for (int i = 0; i < Count; i++)
                array.SetValue(this[i], i + index);
        }

        object ICollection.SyncRoot
        {
            get { return null; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        // IList Functions

        bool IList.IsFixedSize
        {
            get { return true; }
        }

        bool IList.IsReadOnly
        {
            get { return true; }
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new InvalidOperationException(); }
        }

        int IList.Add(object obj)
        {
            throw new InvalidOperationException();
        }

        void IList.Clear()
        {
            throw new InvalidOperationException();
        }

        void IList.Insert(int index, object obj)
        {
            throw new InvalidOperationException();
        }

        void IList.Remove(object obj)
        {
            throw new InvalidOperationException();
        }

        void IList.RemoveAt(int index)
        {
            throw new InvalidOperationException();
        }

        public bool Contains(object obj)
        {
            return IndexOf(obj) != -1;
        }

        public int IndexOf(object obj)
        {
            for (int i = 0; i < Count; i++)
                if (obj.Equals(this[i]))
                    return i;
            return -1;
        }
    }
}
