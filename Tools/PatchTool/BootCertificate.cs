using System.Buffers.Binary;
using System.Text;

namespace Meteor.Tools.PatchTool;

// Reads the certificate ffxivboot checks patch signatures against. It's stored in the exe as
// resource BINARY 111.
public static class BootCertificate
{
    const int ResourceId = 111;

    public static byte[] Read(byte[] exe)
    {
        (int offset, int size) = Locate(exe);
        return exe.AsSpan(offset, size).ToArray();
    }

    // File offset and size of the resource data.
    static (int Offset, int Size) Locate(byte[] exe)
    {
        var pe = new PeImage(exe);
        int rsrcRva = pe.DataDirectoryRva(2);
        if (rsrcRva == 0)
            throw new InvalidDataException("no resource directory");
        int root = pe.Offset(rsrcRva);

        int typeDir = FindEntry(exe, root, root, "BINARY") ?? throw new InvalidDataException("no BINARY resources");
        int nameDir = FindEntry(exe, root, typeDir, ResourceId) ?? throw new InvalidDataException($"no BINARY resource {ResourceId}");

        // Take the first language.
        int entry = nameDir + 16;
        int target = BinaryPrimitives.ReadInt32LittleEndian(exe.AsSpan(entry + 4));
        if ((target & int.MinValue) != 0)
            throw new InvalidDataException("unexpected nested directory under the resource name");
        int dataEntry = root + target;
        int dataRva = BinaryPrimitives.ReadInt32LittleEndian(exe.AsSpan(dataEntry));
        int size = BinaryPrimitives.ReadInt32LittleEndian(exe.AsSpan(dataEntry + 4));
        return (pe.Offset(dataRva), size);
    }

    // Finds a subdirectory by name (named entries) or id; returns its file offset.
    static int? FindEntry(byte[] exe, int root, int dir, object key)
    {
        int named = BinaryPrimitives.ReadUInt16LittleEndian(exe.AsSpan(dir + 12));
        int ids = BinaryPrimitives.ReadUInt16LittleEndian(exe.AsSpan(dir + 14));
        for (int i = 0; i < named + ids; i++)
        {
            int entry = dir + 16 + i * 8;
            uint name = BinaryPrimitives.ReadUInt32LittleEndian(exe.AsSpan(entry));
            uint target = BinaryPrimitives.ReadUInt32LittleEndian(exe.AsSpan(entry + 4));
            bool match;
            if ((name & 0x80000000) != 0)
            {
                int s = root + (int)(name & 0x7FFFFFFF);
                int len = BinaryPrimitives.ReadUInt16LittleEndian(exe.AsSpan(s));
                match = key is string text && Encoding.Unicode.GetString(exe, s + 2, len * 2) == text;
            }
            else
            {
                match = key is int id && name == id;
            }
            if (match && (target & 0x80000000) != 0)
                return root + (int)(target & 0x7FFFFFFF);
        }
        return null;
    }

    sealed class PeImage
    {
        readonly byte[] bytes;
        readonly int optionalHeader;
        readonly List<(int Va, int VirtualSize, int RawOffset, int RawSize)> sections = new();

        public PeImage(byte[] bytes)
        {
            this.bytes = bytes;
            if (bytes.Length < 0x40 || bytes[0] != 'M' || bytes[1] != 'Z')
                throw new InvalidDataException("not a PE file");
            int pe = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0x3C));
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pe)) != 0x00004550)
                throw new InvalidDataException("not a PE file");
            int count = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(pe + 6));
            int optionalSize = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(pe + 20));
            optionalHeader = pe + 24;
            if (BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(optionalHeader)) != 0x10B)
                throw new InvalidDataException("expected a 32-bit PE");
            for (int i = 0; i < count; i++)
            {
                int s = optionalHeader + optionalSize + i * 40;
                sections.Add((BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(s + 12)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(s + 8)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(s + 20)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(s + 16))));
            }
        }

        public int DataDirectoryRva(int index)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(optionalHeader + 96 + index * 8));
        }

        public int Offset(int rva)
        {
            foreach (var s in sections)
            {
                if (rva >= s.Va && rva < s.Va + Math.Max(s.VirtualSize, s.RawSize))
                    return s.RawOffset + rva - s.Va;
            }
            throw new InvalidDataException($"RVA 0x{rva:x} is outside every section");
        }
    }
}
