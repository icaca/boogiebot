using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BoogieBot.Common
{
    /// <summary>DataBase Client (DBC) File. This is a base class. Inherited classes provide nicer interfaces :)</summary>
    public class DBCFile
    {
        // The parsed table.
        private byte[][][] data = null;
        private byte[] stringTable = null;
        protected WDBC_header wdbc_header = new WDBC_header();

        public DBCFile(String filename)
        {

        }

        /*public DBCFile(MPQFile mpqfile)
        {
            parseFile(mpqfile);
        }*/

        public byte[][] getRecord(uint record)
        {
            return data[record];
        }

        public byte[] getField(uint record, uint field)
        {
            return data[record][field];
        }

        public float getFieldAsFloat(uint record, uint field)
        {
            return BitConverter.ToSingle(data[record][field], 0);
        }

        public Int32 getFieldAsInt32(uint record, uint field)
        {
            return BitConverter.ToInt32(data[record][field], 0);
        }

        public UInt32 getFieldAsUint32(uint record, uint field)
        {
            return BitConverter.ToUInt32(data[record][field], 0);
        }

        public String getStringForField(uint record, uint field)
        {
            // Get the string offset from the table
            int stringOffset = BitConverter.ToInt32(data[record][field], 0);

            // Check the offset 'makes sense'. Remember, this field might not even be a string pointer for all we know.
            if (stringOffset < 0 || stringOffset > wdbc_header.stringSize)
                throw new Exception("Field does not contain a string.");

            StringBuilder str = new StringBuilder();

            // Extract string out of the byte array, stop at null terminator.
            for (int i = stringOffset; i < wdbc_header.stringSize; i++)
                if(stringTable[i] == '\0')
                    break;
                else
                    str.Append((char)stringTable[i]);

            return str.ToString();
        }

        /*private void parseFile(MPQFile mpqfile)
        {
            BinaryReader bin = mpqfile.GetBinaryReader();

            // Read in Chunk Header Name
            BlizChunkHeader tempHeader = new BlizChunkHeader(bin.ReadChars(4), 0);

            // No BlizFileHeader Flip() for DBC files

            // Validate DBC Header
            if (!tempHeader.Is("WDBC"))
                throw new Exception("Not a DBC file.");

            wdbc_header = new WDBC_header();

            // Read in DBC header (the rest of it)
            wdbc_header.nRecords = bin.ReadUInt32();
            wdbc_header.nFields = bin.ReadUInt32();
            wdbc_header.recordSize = bin.ReadUInt32();
            wdbc_header.stringSize = bin.ReadUInt32();


            if (wdbc_header.nFields * 4 != wdbc_header.recordSize)
                throw new Exception("Non-standard DBC file.");


            // Read in each record
            data = new byte[wdbc_header.nRecords][][];

            for (int i = 0; i < wdbc_header.nRecords; i++)
            {
                data[i] = new byte[wdbc_header.nFields][];

                for (int j = 0; j < wdbc_header.nFields; j++)
                {
                    data[i][j] = bin.ReadBytes(4);
                }
            }


            // Read in string table
            if (wdbc_header.stringSize > 0)
            {
                stringTable = bin.ReadBytes((int)wdbc_header.stringSize);
            }


            // Just a little sanity check to make sure we're at the end of the file :)
            try
            {
                bin.ReadByte();
                Console.WriteLine("Warning, successfully read 1 byte beyond what should have been the end of the file! Investigate {0}.", mpqfile.Filename);
            }
            catch(System.IO.EndOfStreamException) // good!
            {
                //if(Program.Debug) Console.WriteLine("DBCFile: EOF confirmed.");
            }
        }*/

        // DBC File Header Structure
        protected struct WDBC_header
        {
            public UInt32 nRecords;    // nRecords - number of records in the file
            public UInt32 nFields;     // nFields - number of 4-byte fields per record
            public UInt32 recordSize;  // recordSize = nFields * 4 (not always true!)
            public UInt32 stringSize;  // string block size
        }
    }
}