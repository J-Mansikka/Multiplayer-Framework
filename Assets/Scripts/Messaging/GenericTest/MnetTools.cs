using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class MnetTools
{

    // In packets these three integers values are used the most, so this is a helpful converter that can be used
    // with the constant values in the settings class to convert bytes into integer values.
    // The default value used is the segment size, since it sees most action
    public static int BytesToInteger(Span<byte> bytes)//, int byteSize)
    {
        int byteSize = bytes.Length;
        switch (byteSize)
        {
            case 1:
                return bytes[0];
            case 2:
                return BinaryPrimitives.ReadInt16LittleEndian(bytes);
            case 4:
                return BinaryPrimitives.ReadInt32LittleEndian(bytes);
            default:
                throw new NotSupportedException
                    ("Tried to read an unaccounted integer type that uses "+byteSize+" bytes.");
        }
    }


    public static int BytesToInt32(Span<byte> bytes)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    public static void IntegerToBytes(Span<byte> bytes, int value)
    {
        //int byteSize = bytes.Length;
        switch (bytes.Length)//byteSize)
        {
            case 1:
                bytes[0] = (byte)value;
                break;
            case 2:
                BinaryPrimitives.WriteInt16LittleEndian(bytes, (short)value);
                break;
            case 4:
                BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
                break;
            default:
                throw new NotSupportedException
                    ("ERROR: Received non valid span of bytes. Accepted lengths are 1, 2, 4 (byte, short, int32).");
        }
    }

    public static void Int32ToBytes(Span<byte> bytes, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
    }

    // Lis‰‰ float/double t‰nne
    public static void FloatToBytes(Span<byte> bytes, float value)
    {
        BitConverter.TryWriteBytes(bytes, value);
    }

    public static float BytesToFloat(Span<byte> bytes)
    {
        return BitConverter.ToSingle(bytes);
    }

    public static void WriteServerHeader(Span<byte> headerBytes)
    {
        // TODO
        // Voiks olla yhteinen? Eli server ja clienti ero vaan pituus? Kannataa erottaa toisistaan
        // Muttta Messager packet write vois olla yhteinen ja sitte vaan kutsuis ehk‰ t‰n boolilla = isServerType?
        // Tai ehk‰ pist‰‰ t‰n kautta kaikki eri tyyppiset? VOi olla aika monta eri mutta olis hyv‰ olla yhdes paikas..
        // Koska turhaa clientin pohja versios olis vitun server packeettien luonti
    }
    /*

    // This overloaded method works automatically by integer size, but should not be used by values that can change between builds (e.g. max item size)

    // Single byte conversions are pointless, but this enables support to most viable numeric types and is cleaner than typing [index] everytime
    // !!! ! Jos aina palautetaa m‰‰r‰ ja overload ottaa eri tyypit huomioon nii n‰‰ pit‰is aina toimii oli user muutokset mit‰ tahansa
    public static int BytesToInteger(Span<byte> bytes, out byte value)
    {
        value = bytes[0];
        return 1;
    }

    public static int BytesToInteger(Span<byte> bytes, out short value)
    {
        value = BinaryPrimitives.ReadInt16LittleEndian(bytes);
        return 2;
    }

    public static int BytesToInteger(Span<byte> bytes, out int value)
    {
        value = BinaryPrimitives.ReadInt32LittleEndian(bytes);
        return 4;
    }

    // Yeah, this one is a bit pointless but cleaner and user doesn't have to think too much about it
    // !!! Palautetaa kuinka monta tavua kirjotettiin
    public static int IntegerToBytes(Span<byte> bytes, byte value)
    {
        bytes[0] = value;
        return 1;
    }

    public static int IntegerToBytes(Span<byte> bytes, short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(bytes, value);
        return 2;
    }

    public static int IntegerToBytes(Span<byte> bytes, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return 4;
    }
    */
}


