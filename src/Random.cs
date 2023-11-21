using System;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS;

/// <summary>
/// This class implements the well equidistributed long-period linear pseudorandom number generator.
/// Code taken from Chris Lomont: http://lomont.org/papers/2008/Lomont_PRNG_2008.pdf
/// </summary>
public class Random
{
	public const int STATE_BYTE_COUNT = 68; // 16 state ints + 1 index int

	uint[] State = new uint[16];
	uint Index = 0;
	uint Seed;

	/// <summary>
	/// Initializes the RNG with an arbitrary seed.
	/// </summary>
	public Random()
	{
		Init((uint) Environment.TickCount);
	}

	/// <summary>
	/// Initializes the RNG with a given seed.
	/// </summary>
	public void Init(uint seed)
	{
		Seed = seed;
		uint s = seed;
		for (int i = 0; i < 16; i++)
		{
			s = (((s * 214013 + 2531011) >> 16) & 0x7fffffff) | 0;
			State[i] = ~ ~s; //i ;
		}
		Index = 0;
	}

	/// <summary>
	/// Returns the seed that was used to initialize the RNG.
	/// </summary>
	public uint GetSeed()
	{
		return Seed;
	}

	/// <summary>
	/// Returns the entire state of the RNG as a string.
	/// </summary>
	public string PrintState()
	{
		var s = "";
		for (var i = 0; i < 16; i += 1)
		{
			s += State[i];
		}
		s += Index;
		return s;
	}

	/// <summary>
	/// Saves the entire state of the RNG to a Span.
	/// </summary>
	/// <param name="bytes">Must be a span of at least STATE_BYTE_COUNT bytes.</param>
	/// <exception cref="ArgumentException">Thrown if the byte span is too short.</exception>
	public unsafe void SaveState(Span<byte> bytes)
	{
#if DEBUG
		if (bytes.Length < STATE_BYTE_COUNT)
		{
			throw new ArgumentException("Byte span too short!");
		}
#endif

		fixed (byte* ptr = bytes)
		{
			var offset = 0;
			for (var i = 0; i < 16; i += 1)
			{
				Unsafe.Write(ptr + offset, State[i]);
				offset += 4;
			}

			Unsafe.Write(ptr + offset, Index);
		}
	}

	/// <summary>
	/// Loads the entire state of the RNG from a Span.
	/// </summary>
	/// <param name="bytes">Must be a span of at least STATE_BYTE_COUNT bytes.</param>
	/// <exception cref="ArgumentException">Thrown if the byte span is too short.</exception>
	public unsafe void LoadState(Span<byte> bytes)
	{
#if DEBUG
		if (bytes.Length < STATE_BYTE_COUNT)
		{
			throw new ArgumentException("Byte span too short!");
		}
#endif

		fixed (byte* ptr = bytes)
		{
			var offset = 0;

			for (var i = 0; i < 16; i += 1)
			{
				State[i] = Unsafe.Read<uint>(ptr + offset);
				offset += 4;
			}

			Index = Unsafe.Read<uint>(ptr + offset);
		}
	}

	private uint NextInternal()
	{
		uint a, b, c, d;
		a = State[Index];
		c = State[(Index+13)&15];
		b = a^c^(a<<16)^(c<<15);
		c = State[(Index+9)&15];
		c ^= (c>>11);
		a = State[Index] = b^c;
		d = (uint) (a ^((a<<5)&0xDA442D24UL));
		Index = (Index + 15)&15;
		a = State[Index];
		State[Index] = a^b^d^(a<<2)^(b<<18)^(c<<28);
		return State[Index];
	}

	/// <summary>
	/// Returns a non-negative signed integer.
	/// </summary>
	public int Next()
	{
		return (int) (NextInternal() >>> 1); // unsigned bitshift right to get rid of signed bit
	}

	/// <summary>
	/// Returns a non-negative signed integer less than max.
	/// </summary>
	public int Next(int max)
	{
		return (int) (((double) Next()) * max / int.MaxValue);
	}

	/// <summary>
	/// Returns a signed integer greater than or equal to min and less than max.
	/// </summary>
	public int Next(int min, int max)
	{
		var diff = max - min;
		var next = Next(diff);
		return min + next;
	}

	/// <summary>
	/// Returns a non-negative signed 64 bit integer.
	/// </summary>
	public long NextInt64()
	{
		long next = NextInternal();
		next <<= 32;
		next |= NextInternal();
		next >>>= 1;
		return next;
	}

	/// <summary>
	/// Returns a non-negative signed 64 bit integer less than max.
	/// </summary>
	public long NextInt64(long max)
	{
		var next = NextInt64();
		return (long) (((double) next) * max / long.MaxValue);
	}

	/// <summary>
	/// Returns a non-negative signed 64 bit integer greater than or equal to min and less than max.
	/// </summary>
	public long NextInt64(long min, long max)
	{
		var diff = max - min;
		var next = NextInt64(diff);
		return min + next;
	}

	/// <summary>
	/// Returns a single-precision floating point value between 0 and 1.
	/// </summary>
	public float NextSingle()
	{
		var n = NextInternal();
		return ((float) n) / uint.MaxValue;
	}

	/// <summary>
	/// Returns a double-precision floating point value between 0 and 1.
	/// </summary>
	public double NextDouble()
	{
		var n = NextInternal();
		return ((double) n) / uint.MaxValue;
	}
}
