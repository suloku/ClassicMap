using System;
using System.Collections.Generic;
using System.Text;
using GBHL;

namespace ClassicMap
{
	public class Pokemon
	{
		public byte level;
		public byte id;
	}

	public class WildPokemonLoader
	{
		GBFile gb;
		public byte rarityGrass;
		public byte rarityWater;
		public Pokemon[] grassPokemon;
		public Pokemon[] waterPokemon;
		public string[] PokemonNames;
		public byte[] PokemonIndicies;
		public int wildPokemonLocation;

		public WildPokemonLoader(GBFile g)
		{
			gb = g;
		}

		public void LoadWildPokemon(int map)
		{
			gb.BufferLocation = 0xCEEB + map * 2;
			gb.BufferLocation = 0xC000 + gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
			wildPokemonLocation = gb.BufferLocation;
			rarityGrass = gb.ReadByte();
			rarityWater = 0;
			grassPokemon = new Pokemon[10];
			waterPokemon = new Pokemon[10];
			if (rarityGrass != 0)
			{
				for (int i = 0; i < 10; i++)
				{
					Pokemon p = new Pokemon();
					p.level = gb.ReadByte();
					p.id = gb.ReadByte();
					grassPokemon[i] = p;
				}
			}

			rarityWater = gb.ReadByte();
			if (rarityWater == 0)
				return;
			for (int i = 0; i < 10; i++)
			{
				Pokemon p = new Pokemon();
				p.level = gb.ReadByte();
				p.id = gb.ReadByte();
				waterPokemon[i] = p;
			}
		}

		public void LoadPokemonNames()
		{
			gb.BufferLocation = 0x42800;
			byte[] indicies = gb.ReadBytes(256);
			PokemonIndicies = indicies;
			string[] names = new string[256];
			for (int i = 0; i < 254; i++)
			{
				gb.BufferLocation = 0xff000 + (i * 10);
				byte[] nameBytes = gb.ReadBytes(10);
				string s = "";
				for (int k = 0; k < 10; k++)
					if (nameBytes[k] != 0x50)
						s += TextTable.GetString(nameBytes[k]);
				string ss = i.ToString("X");
				if (ss.Length == 1)
					ss = "0" + ss;
				names[indicies[i]] = ss + " - " + s;
			}
			PokemonNames = names;
		}
	}
}
