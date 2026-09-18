using System.Text;

namespace Garethp.ModsOfMistriaInstallerLib.Audio;

/// <summary>
/// This is a Byte-level editor for FMOD Studio ".strings.bank" files that has the table that maps
/// object paths (<c>event:/…</c>, <c>bus:/…</c>, <c>bank:/…</c>) to their 16-byte GUIDs.
///
/// FoM's audio bridge Tango builds its name to event map by enumerating loaded
/// banks and calling FMOD's <c>EventDescription::getPath</c> — the reverse GUID->path
/// lookup, which resolves only through the loaded strings bank. So to make a brand-new
/// modded event callable by name (<c>tango_play("MyEvent")</c>) it needs it's
/// <c>event:/MyEvent -> GUID</c> pair merged into the game's own <c>Master.strings.bank</c>.
///
/// FMOD's reverse lookup BINARY-SEARCHES the GUID array, which is sorted by the FMOD_GUID field order (Data1,Data2,Data3,Data4).
/// A new GUID has to  be inserted at its sorted slot or getPath returns EVENT_NOTFOUND for it. See <see cref="Insert"/>.
///
/// Format: RIFF -> (nested LIST chunks) -> STDT "RadixTree_24Bit":
///         u32 type=1; nodes[]; guids[]; string-blob; leaf-index[]; parent-index[].
/// Each array is prefixed by a variable-length count (<see cref="ReadX16"/>).
/// </summary>
public static class StringsBank
{
    private sealed class Node
    {
        public uint Ki;   // KeyInfo:   (firstChar &lt;&lt; 24) | stringOffset(0xFFFFFF = none)
        public uint Ci;   // ChildInfo: leaf if (Ci&gt;&gt;24)==0 -> low24 = guid index;
                          //            else internal -> (Ci&gt;&gt;24)=childCount, low24=first child index
        public Node(uint ki, uint ci) { Ki = ki; Ci = ci; }
    }

    private sealed class Table
    {
        public List<Node> Nodes = new();
        public List<byte[]> Guids = new();   // each 16 bytes
        public List<byte> Blob = new();
        public List<int> Leaf = new();       // guid index → node index (reverse walk start)
        public List<int> Parent = new();     // node index → parent node index (0xFFFFFF = root/none)
    }

    // variable-length count codec
    // u16 lo; if the 0x8000 bit is set, read another u16 hi → (lo & 0x7FFF) | (hi << 15).
    private sealed class Reader(byte[] b)
    {
        public int O;
        public ushort U16() { var v = (ushort)(b[O] | (b[O + 1] << 8)); O += 2; return v; }
        public uint U32() { uint v = (uint)(b[O] | (b[O + 1] << 8) | (b[O + 2] << 16) | (b[O + 3] << 24)); O += 4; return v; }
        public int U24() { int v = b[O] | (b[O + 1] << 8) | (b[O + 2] << 16); O += 3; return v; }
        public int X16() { int lo = U16(); return (lo & 0x8000) != 0 ? ((lo & 0x7FFF) | (U16() << 15)) : lo; }
        public byte[] Take(int n) { var v = new byte[n]; Array.Copy(b, O, v, 0, n); O += n; return v; }
    }

    private static void WriteX16(List<byte> outp, int v)
    {
        if (v < 0x8000) { outp.Add((byte)(v & 0xFF)); outp.Add((byte)(v >> 8)); }
        else { int lo = (v & 0x7FFF) | 0x8000, hi = v >> 15; outp.Add((byte)(lo & 0xFF)); outp.Add((byte)(lo >> 8)); outp.Add((byte)(hi & 0xFF)); outp.Add((byte)(hi >> 8)); }
    }

    private static Table ParseStdt(byte[] stdt)
    {
        var r = new Reader(stdt);
        var t = new Table();
        r.U32(); // type == 1
        int nc = r.X16() >> 1; r.U16(); // element size (8) ignored
        for (int i = 0; i < nc; i++) t.Nodes.Add(new Node(r.U32(), r.U32()));
        int gc = r.X16() >> 1; r.U16(); // element size (16)
        for (int i = 0; i < gc; i++) t.Guids.Add(r.Take(16));
        int bl = r.X16(); t.Blob.AddRange(r.Take(bl));
        int lc = r.X16(); for (int i = 0; i < lc; i++) t.Leaf.Add(r.U24());
        int pc = r.X16(); for (int i = 0; i < pc; i++) t.Parent.Add(r.U24());
        return t;
    }

    private static byte[] SerStdt(Table t)
    {
        var o = new List<byte>();
        void U32(uint v) { o.Add((byte)(v & 0xFF)); o.Add((byte)(v >> 8)); o.Add((byte)(v >> 16)); o.Add((byte)(v >> 24)); }
        void U16(int v) { o.Add((byte)(v & 0xFF)); o.Add((byte)(v >> 8)); }
        void U24(int v) { o.Add((byte)(v & 0xFF)); o.Add((byte)((v >> 8) & 0xFF)); o.Add((byte)((v >> 16) & 0xFF)); }

        U32(1);
        WriteX16(o, (t.Nodes.Count << 1) | 1); U16(8);
        foreach (var n in t.Nodes) { U32(n.Ki); U32(n.Ci); }
        WriteX16(o, (t.Guids.Count << 1) | 1); U16(16);
        foreach (var g in t.Guids) o.AddRange(g);
        WriteX16(o, t.Blob.Count); o.AddRange(t.Blob);
        WriteX16(o, t.Leaf.Count); foreach (var v in t.Leaf) U24(v);
        WriteX16(o, t.Parent.Count); foreach (var v in t.Parent) U24(v & 0xFFFFFF);
        return o.ToArray();
    }

    // ── RIFF splice: replace the STDT chunk body in place, fixing enclosing sizes ──
    private abstract class Chunk;
    private sealed class ListChunk : Chunk { public byte[] Type = []; public List<Chunk> Children = new(); }
    private sealed class LeafChunk : Chunk { public byte[] Tag = []; public byte[] Body = []; }

    private static uint ReadU32(byte[] d, int o) => (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));

    private static List<Chunk> ParseChunks(byte[] d, int o, int end)
    {
        var ch = new List<Chunk>();
        while (o < end)
        {
            var tag = d[o..(o + 4)];
            int s = (int)ReadU32(d, o + 4);
            int ds = o + 8;
            if (tag.SequenceEqual("LIST"u8.ToArray()))
                ch.Add(new ListChunk { Type = d[ds..(ds + 4)], Children = ParseChunks(d, ds + 4, ds + s) });
            else
                ch.Add(new LeafChunk { Tag = tag, Body = d[ds..(ds + s)] });
            o = ds + s + (s & 1);
        }
        return ch;
    }

    private static void SerChunks(List<Chunk> ch, List<byte> o)
    {
        void U32(int v) { o.Add((byte)(v & 0xFF)); o.Add((byte)((v >> 8) & 0xFF)); o.Add((byte)((v >> 16) & 0xFF)); o.Add((byte)((v >> 24) & 0xFF)); }
        foreach (var c in ch)
        {
            if (c is ListChunk lc)
            {
                var body = new List<byte>(); body.AddRange(lc.Type); SerChunks(lc.Children, body);
                o.AddRange("LIST"u8.ToArray()); U32(body.Count); o.AddRange(body);
            }
            else
            {
                var l = (LeafChunk)c;
                o.AddRange(l.Tag); U32(l.Body.Length); o.AddRange(l.Body);
            }
            if ((o.Count & 1) != 0) o.Add(0);
        }
    }

    private static bool ReplaceStdt(List<Chunk> ch, byte[] newBody)
    {
        foreach (var c in ch)
        {
            if (c is ListChunk lc) { if (ReplaceStdt(lc.Children, newBody)) return true; }
            else if (((LeafChunk)c).Tag.SequenceEqual("STDT"u8.ToArray())) { ((LeafChunk)c).Body = newBody; return true; }
        }
        return false;
    }

    private static byte[] Splice(byte[] d, byte[] newStdt)
    {
        var form = d[8..12];
        var top = ParseChunks(d, 12, 8 + (int)ReadU32(d, 4));
        if (!ReplaceStdt(top, newStdt)) throw new InvalidOperationException("STDT chunk not found in strings bank");
        var body = new List<byte>(); body.AddRange(form); SerChunks(top, body);
        var outp = new List<byte>(); outp.AddRange("RIFF"u8.ToArray());
        outp.Add((byte)(body.Count & 0xFF)); outp.Add((byte)((body.Count >> 8) & 0xFF));
        outp.Add((byte)((body.Count >> 16) & 0xFF)); outp.Add((byte)((body.Count >> 24) & 0xFF));
        outp.AddRange(body);
        return outp.ToArray();
    }

    private static byte Low(byte c) => (c >= 0x41 && c <= 0x5A) ? (byte)(c | 0x20) : c;

    private static string BlobString(Table t, int off)
    {
        if (off == 0xFFFFFF) return "";
        int e = off; while (e < t.Blob.Count && t.Blob[e] != 0) e++;
        return Encoding.UTF8.GetString(t.Blob.GetRange(off, e - off).ToArray());
    }

    private static int AddBlob(Table t, byte[] bytesNullTerminated)
    {
        int off = t.Blob.Count; t.Blob.AddRange(bytesNullTerminated); return off;
    }

    // sort key mirroring FMOD_GUID comparison: (u32 Data1, u16 Data2, u16 Data3, 8 raw bytes)
    private static int GuidCompare(byte[] a, byte[] b)
    {
        for (int i = 0; i < 4; i++) { int c = a[3 - i].CompareTo(b[3 - i]); if (c != 0) return c; } // Data1 LE
        for (int i = 0; i < 2; i++) { int c = a[5 - i].CompareTo(b[5 - i]); if (c != 0) return c; } // Data2 LE
        for (int i = 0; i < 2; i++) { int c = a[7 - i].CompareTo(b[7 - i]); if (c != 0) return c; } // Data3 LE
        for (int i = 8; i < 16; i++) { int c = a[i].CompareTo(b[i]); if (c != 0) return c; }        // Data4 raw
        return 0;
    }

    /// <summary>Parses a "{d1-d2-d3-d4a-d4b}" GUID string into the 16-byte LE layout used inside a strings bank (Data1/2/3 LE, Data4 raw).</summary>
    public static byte[] ParseGuid(string guid)
    {
        var hex = guid.Trim().Trim('{', '}').Replace("-", "");
        if (hex.Length != 32) throw new FormatException($"Not a GUID: {guid}");
        var raw = Convert.FromHexString(hex);
        return
        [
            raw[3], raw[2], raw[1], raw[0],   // Data1 LE
            raw[5], raw[4],                   // Data2 LE
            raw[7], raw[6],                   // Data3 LE
            raw[8], raw[9], raw[10], raw[11], raw[12], raw[13], raw[14], raw[15] // Data4
        ];
    }

    /// <summary>Reads every (path, 16-byte GUID) pair out of a strings bank.</summary>
    public static List<(string Path, byte[] Guid)> ReadPaths(byte[] bankBytes)
    {
        int i = IndexOf(bankBytes, "STDT"u8.ToArray());
        var t = ParseStdt(bankBytes[(i + 8)..(i + 8 + (int)ReadU32(bankBytes, i + 4))]);
        var outp = new List<(string, byte[])>();
        for (int gi = 0; gi < t.Guids.Count; gi++)
        {
            int ni = t.Leaf[gi];
            var segs = new List<string>();
            int guard = 0;
            while (ni != 0xFFFFFF && guard++ < 10_000_000)
            {
                int so = (int)(t.Nodes[ni].Ki & 0xFFFFFF);
                if (so != 0xFFFFFF) segs.Add(BlobString(t, so));
                ni = t.Parent[ni];
            }
            segs.Reverse();
            outp.Add((string.Concat(segs), t.Guids[gi]));
        }
        return outp;
    }

    /// <summary>
    /// Returns a new strings-bank byte array with <paramref name="path"/> => <paramref name="guid16"/> merged in. <paramref name="guid16"/> is the 16-byte little endian layout (see <see cref="ParseGuid"/>).
    /// Keeps existing entries and inserts the new GUID at its sorted slot.
    /// </summary>
    public static byte[] Insert(byte[] bankBytes, string path, byte[] guid16)
    {
        int stdtAt = IndexOf(bankBytes, "STDT"u8.ToArray());
        var t = ParseStdt(bankBytes[(stdtAt + 8)..(stdtAt + 8 + (int)ReadU32(bankBytes, stdtAt + 4))]);

        var pb = Encoding.UTF8.GetBytes(path);
        int pos = 0, ni = 0;
        int ourGi;      // guid index of the newly-added leaf (before sort)
        int ourLeafIdx; // node index of the newly-added leaf

        while (true)
        {
            var node = t.Nodes[ni];
            int cc = (int)(node.Ci >> 24), cs = (int)(node.Ci & 0xFFFFFF);
            if (cc == 0) throw new InvalidOperationException($"'{path}' collides with an existing value node");

            int found = -1;
            byte tgt = Low(pb[pos]);
            for (int j = cs; j < cs + cc; j++)
                if ((byte)(t.Nodes[j].Ki >> 24) == tgt) { found = j; break; }

            if (found < 0)
            {
                // no child begins with this char =>add a new leaf child to `ni`.
                (ourGi, ourLeafIdx) = AddChildLeaf(t, ni, pb, pos, guid16);
                break;
            }

            var seg = Encoding.UTF8.GetBytes(BlobString(t, (int)(t.Nodes[found].Ki & 0xFFFFFF)));
            int k = 0;
            while (k < seg.Length && pos + k < pb.Length && Low(seg[k]) == Low(pb[pos + k])) k++;
            if (k == seg.Length) { pos += k; ni = found; continue; } // whole segment matched, descend

            // MID-SPLIT => divergence inside child `found`'s segment at offset k.
            (ourGi, ourLeafIdx) = MidSplit(t, found, seg, k, pb, pos + k, guid16);
            break;
        }

        NormaliseGuidOrder(t, ourGi, ourLeafIdx, guid16);
        return Splice(bankBytes, SerStdt(t));
    }

    /// <summary>
    /// Repoints an EXISTING path at a different GUID, so every by-name play of it lands
    /// on the new event without needing to repoint all GML call sites for the event. THis makes it possible to repoint a
    /// name (e.g. <c>event:/Music/Playlists/MinesUpper</c>) at a mod's own added event.
    /// The path's old GUID is removed from the table (so the original event falls out of name resolution so it exists but it cannot be called); <paramref name="newGuid16"/> is inserted at its
    /// sorted slot. Throws if the path is missing, is not a value node, or its old GUID is
    /// shared by more than one path.
    /// </summary>
    public static byte[] Repoint(byte[] bankBytes, string path, byte[] newGuid16)
    {
        int stdtAt = IndexOf(bankBytes, "STDT"u8.ToArray());
        var t = ParseStdt(bankBytes[(stdtAt + 8)..(stdtAt + 8 + (int)ReadU32(bankBytes, stdtAt + 4))]);

        int leafIdx = FindLeaf(t, path);
        if (leafIdx < 0) throw new InvalidOperationException($"'{path}' not found in strings bank");
        int giOld = (int)(t.Nodes[leafIdx].Ci & 0xFFFFFF);

        // The old GUID must be referenced only by this path, or removing it would strip another entry too.
        int refs = 0;
        foreach (var n in t.Nodes) if ((n.Ci >> 24) == 0 && (int)(n.Ci & 0xFFFFFF) == giOld) refs++;
        if (refs != 1) throw new InvalidOperationException($"Audio: '{path}' GUID is shared by {refs} entries and as such cannot repoint safely");

        var leaf = t.Nodes[leafIdx];

        // Remove the old GUID (and its parallel leaf[] slot) and shift references above it.
        t.Guids.RemoveAt(giOld);
        t.Leaf.RemoveAt(giOld);
        foreach (var n in t.Nodes)
        {
            if (ReferenceEquals(n, leaf) || (n.Ci >> 24) != 0) continue; // leaf reassigned in step 2
            int gr = (int)(n.Ci & 0xFFFFFF);
            if (gr > giOld) n.Ci = (uint)(gr - 1);
        }

        // Insert the new GUID at its sorted slot and the path's leaf points at it.
        int gpos = 0;
        while (gpos < t.Guids.Count && GuidCompare(t.Guids[gpos], newGuid16) < 0) gpos++;
        t.Guids.Insert(gpos, (byte[])newGuid16.Clone());
        t.Leaf.Insert(gpos, leafIdx);
        foreach (var n in t.Nodes)
        {
            if (ReferenceEquals(n, leaf) || (n.Ci >> 24) != 0) continue;
            int gr = (int)(n.Ci & 0xFFFFFF);
            if (gr >= gpos) n.Ci = (uint)(gr + 1);
        }
        leaf.Ci = (uint)gpos;

        return Splice(bankBytes, SerStdt(t));
    }

    // Walks the tree to the value node for an exact path; -1 if absent.
    private static int FindLeaf(Table t, string path)
    {
        var pb = Encoding.UTF8.GetBytes(path);
        int pos = 0, ni = 0;
        while (true)
        {
            var node = t.Nodes[ni];
            int cc = (int)(node.Ci >> 24), cs = (int)(node.Ci & 0xFFFFFF);
            if (pos == pb.Length)
            {
                if (cc == 0) return ni;                                   // this node holds the value
                for (int j = cs; j < cs + cc; j++)                        // or a char-0 terminal child (prefix value)
                    if ((t.Nodes[j].Ki >> 24) == 0) return j;
                return -1;
            }
            if (cc == 0) return -1;                                       // value node but path continues
            int found = -1;
            byte tgt = Low(pb[pos]);
            for (int j = cs; j < cs + cc; j++)
                if ((byte)(t.Nodes[j].Ki >> 24) == tgt) { found = j; break; }
            if (found < 0) return -1;
            var seg = Encoding.UTF8.GetBytes(BlobString(t, (int)(t.Nodes[found].Ki & 0xFFFFFF)));
            for (int k = 0; k < seg.Length; k++)
                if (pos + k >= pb.Length || Low(seg[k]) != Low(pb[pos + k])) return -1;
            pos += seg.Length; ni = found;
        }
    }

    // Split child `si` (segment `seg`) at position `k`: `si` becomes the shared prefix pointing at a new 2-node block {suffix-of-old, our-new-leaf}.
    private static (int ourGi, int ourLeafIdx) MidSplit(Table t, int si, byte[] seg, int k, byte[] pb, int remStart, byte[] guid16)
    {
        var rem = pb[remStart..];
        if (rem.Length == 0) throw new InvalidOperationException("prefix-terminal insert not supported");

        var old = t.Nodes[si];
        uint sOff = old.Ki & 0xFFFFFF, sChar = old.Ki >> 24;
        int prefixOff = AddBlob(t, Concat(seg[..k], 0));
        int leafOff = AddBlob(t, Concat(rem, 0));
        uint suffixOff = sOff + (uint)k; // suffix is the tail of the original segment, already in blob

        int ourGi = t.Guids.Count; t.Guids.Add((byte[])guid16.Clone());
        var suffixNode = new Node(((uint)Low(seg[k]) << 24) | (suffixOff & 0xFFFFFF), old.Ci);
        var ourLeaf = new Node((uint)((Low(rem[0]) << 24) | (leafOff & 0xFFFFFF)), (uint)(ourGi & 0xFFFFFF));

        int baseIdx = t.Nodes.Count;
        int suffixIdx, ourLeafIdx;
        if (Low(seg[k]) <= Low(rem[0])) { t.Nodes.Add(suffixNode); t.Nodes.Add(ourLeaf); suffixIdx = baseIdx; ourLeafIdx = baseIdx + 1; }
        else { t.Nodes.Add(ourLeaf); t.Nodes.Add(suffixNode); ourLeafIdx = baseIdx; suffixIdx = baseIdx + 1; }

        t.Nodes[si] = new Node((sChar << 24) | ((uint)prefixOff & 0xFFFFFF), (2u << 24) | (uint)baseIdx);

        int oldCc = (int)(old.Ci >> 24), oldCs = (int)(old.Ci & 0xFFFFFF);
        while (t.Parent.Count < t.Nodes.Count) t.Parent.Add(0xFFFFFF);
        if (oldCc > 0) for (int j = oldCs; j < oldCs + oldCc; j++) t.Parent[j] = suffixIdx; // reparent moved subtree
        else t.Leaf[(int)(old.Ci & 0xFFFFFF)] = suffixIdx;                                  // old value now lives on suffixNode
        t.Parent[suffixIdx] = si; t.Parent[ourLeafIdx] = si;
        t.Leaf.Add(ourLeafIdx);
        return (ourGi, ourLeafIdx);
    }

    // Add a new leaf child (whole remaining key) to node `ni`, rebuilding its child block (children must be consecutive an sorted by first char).
    private static (int ourGi, int ourLeafIdx) AddChildLeaf(Table t, int ni, byte[] pb, int pos, byte[] guid16)
    {
        var rem = pb[pos..];
        var parentNode = t.Nodes[ni];
        int cc = (int)(parentNode.Ci >> 24), cs = (int)(parentNode.Ci & 0xFFFFFF);

        int leafOff = AddBlob(t, Concat(rem, 0));
        int ourGi = t.Guids.Count; t.Guids.Add((byte[])guid16.Clone());
        var ourLeaf = new Node((uint)((Low(rem[0]) << 24) | (leafOff & 0xFFFFFF)), (uint)(ourGi & 0xFFFFFF));

        // gather existing children + the new leaf, sorted by lowercased first char
        var kids = new List<(byte Ch, Node N, int OldIdx)>();
        for (int j = cs; j < cs + cc; j++) kids.Add(((byte)(t.Nodes[j].Ki >> 24), t.Nodes[j], j));
        kids.Add((Low(rem[0]), ourLeaf, -1));
        kids.Sort((a, b) => a.Ch.CompareTo(b.Ch));

        int baseIdx = t.Nodes.Count;
        var oldToNew = new Dictionary<int, int>();
        int ourLeafIdx = -1;
        for (int p = 0; p < kids.Count; p++)
        {
            t.Nodes.Add(kids[p].N);
            if (kids[p].OldIdx >= 0) oldToNew[kids[p].OldIdx] = baseIdx + p; else ourLeafIdx = baseIdx + p;
        }
        while (t.Parent.Count < t.Nodes.Count) t.Parent.Add(0xFFFFFF);

        parentNode.Ci = ((uint)(cc + 1) << 24) | (uint)baseIdx;
        // fix references to the moved children (indices changed)
        foreach (var (oldIdx, newIdx) in oldToNew)
        {
            t.Parent[newIdx] = ni;
            var moved = t.Nodes[newIdx];
            int mcc = (int)(moved.Ci >> 24), mcs = (int)(moved.Ci & 0xFFFFFF);
            if (mcc > 0) for (int c = mcs; c < mcs + mcc; c++) t.Parent[c] = newIdx; // its children now point here
            else t.Leaf[(int)(moved.Ci & 0xFFFFFF)] = newIdx;                        // its value's reverse index
        }
        t.Parent[ourLeafIdx] = ni;
        t.Leaf.Add(ourLeafIdx);
        return (ourGi, ourLeafIdx);
    }

    // Moves an appended GUID to its sorted slot and shif every leafnode guid ref (and the Leaf[] array) so FMOD's binary-search lookup finds it.
    private static void NormaliseGuidOrder(Table t, int ourGi, int ourLeafIdx, byte[] guid16)
    {
        // newly added guid sits at index ourGi should awlays stop at the latest, since Compare(ours, ours) == 0 is not < 0, so gpos <= ourGi.
        int gpos = 0;
        while (gpos < t.Guids.Count && GuidCompare(t.Guids[gpos], guid16) < 0) gpos++;
        int oldIdx = ourGi;
        if (gpos == oldIdx) return;

        var g = t.Guids[oldIdx]; t.Guids.RemoveAt(oldIdx); t.Guids.Insert(gpos, g);
        var lf = t.Leaf[oldIdx]; t.Leaf.RemoveAt(oldIdx); t.Leaf.Insert(gpos, lf);
        foreach (var n in t.Nodes)
        {
            if ((n.Ci >> 24) != 0) continue; // internal node low24 is a child index, not a guid index gotta skip it
            int gi = (int)(n.Ci & 0xFFFFFF);
            if (gi == oldIdx) n.Ci = (uint)gpos;
            else if (gi >= gpos && gi < oldIdx) n.Ci = (uint)(gi + 1);
        }
    }

    private static byte[] Concat(byte[] a, byte terminator)
    {
        var r = new byte[a.Length + 1]; Array.Copy(a, r, a.Length); r[a.Length] = terminator; return r;
    }

    private static int IndexOf(byte[] hay, byte[] needle)
    {
        for (int i = 0; i + needle.Length <= hay.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < needle.Length; j++) if (hay[i + j] != needle[j]) { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }
}
