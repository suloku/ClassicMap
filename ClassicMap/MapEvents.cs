using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicMap
{
	public class Person
	{
		public byte sprite;
		public byte y;
		public byte x;
		public byte movement;
		public byte movement2;
		public byte text;
		public byte originalText;
		public byte trainer;
		public byte pokemonSet;
		public byte item;
	}

	public class Warp
	{
		public byte y;
		public byte x;
		public byte destPoint;
		public byte map;
	}

	public class Sign
	{
		public byte y;
		public byte x;
		public byte text;
	}

	public class WarpTo
	{
		public int address;
		public byte y;
		public byte x;
	}
}
