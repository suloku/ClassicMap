using System;
using System.Collections.Generic;
using System.Text;
using GBHL;

namespace ClassicMap
{
	public struct MapHeader
	{
		public int Location;
		public byte Tileset;
		public byte Width;
		public byte Height;
		public int LevelData;
		public int TextData;
		public int ScriptData;
		public int EventData;
	}

	public class MapLoader
	{
		private GBFile gb;
		public MapHeader mapHeader;
		public byte[,] LevelData;
		public byte borderTile;
		public List<Warp> warps;
		public List<Sign> signs;
		public List<Person> people;
		public List<WarpTo> warpTo;

		public MapLoader(GBFile g)
		{
			gb = g;
		}

		public void LoadMapHeader(int map)
		{
			mapHeader = new MapHeader();

			gb.BufferLocation = 0xC23D + map;
			byte bank = gb.ReadByte();

			gb.BufferLocation = 0x01AE + map * 2;
			mapHeader.Location = bank * 0x4000 + gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);

			gb.BufferLocation = mapHeader.Location;
			mapHeader.Tileset = gb.ReadByte();
			mapHeader.Height = gb.ReadByte();
			mapHeader.Width = gb.ReadByte();
			mapHeader.LevelData = bank * 0x4000 + gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
			mapHeader.TextData = bank * 0x4000 + gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
			mapHeader.ScriptData = bank * 0x4000 + gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
			LoadConnections();
			mapHeader.EventData = bank * 0x4000 + gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
			LoadEvents();
		}

		public void LoadLevelData()
		{
			gb.BufferLocation = mapHeader.LevelData;
			LevelData = new byte[mapHeader.Width, mapHeader.Height];
			for (int y = 0; y < mapHeader.Height; y++)
			{
				for (int x = 0; x < mapHeader.Width; x++)
				{
					LevelData[x, y] = gb.ReadByte();
				}
			}
		}

		public void LoadConnections()
		{
			byte connection = gb.ReadByte();
			if ((connection & (1 << 3)) != 0)
				gb.BufferLocation += 11;
			if ((connection & (1 << 2)) != 0)
				gb.BufferLocation += 11;
			if ((connection & (1 << 1)) != 0)
				gb.BufferLocation += 11;
			if ((connection & (1 << 0)) != 0)
				gb.BufferLocation += 11;
		}

		public void LoadEvents()
		{
			gb.BufferLocation = mapHeader.EventData;
			borderTile = gb.ReadByte();

			warps = new List<Warp>();
			byte warpCount = gb.ReadByte();
			for (int i = 0; i < warpCount; i++)
			{
				Warp w = new Warp();
				w.y = gb.ReadByte();
				w.x = gb.ReadByte();
				w.destPoint = gb.ReadByte();
				w.map = gb.ReadByte();
				warps.Add(w);
			}

			signs = new List<Sign>();
			byte signCount = gb.ReadByte();
			for (int i = 0; i < signCount; i++)
			{
				Sign s = new Sign();
				s.y = gb.ReadByte();
				s.x = gb.ReadByte();
				s.text = gb.ReadByte();
				signs.Add(s);
			}

			people = new List<Person>();
			byte personCount = gb.ReadByte();
			for (int i = 0; i < personCount; i++)
			{
				Person p = new Person();
				p.sprite = gb.ReadByte();
				p.y = gb.ReadByte();
				p.x = gb.ReadByte();
				p.movement = gb.ReadByte();
				p.movement2 = gb.ReadByte();
				p.text = gb.ReadByte();
				p.originalText = p.text;
				if ((p.text & 0x40) != 0)
				{
					p.trainer = gb.ReadByte();
					p.pokemonSet = gb.ReadByte();
				}
				else if ((p.text & 0x80) != 0)
				{
					p.item = gb.ReadByte();
				}
				people.Add(p);
			}

			warpTo = new List<WarpTo>();
			for (int i = 0; i < warpCount; i++)
			{
				WarpTo w = new WarpTo();
				w.address = gb.ReadByte() + gb.ReadByte() * 0x100;
				w.y = gb.ReadByte();
				w.x = gb.ReadByte();
				warpTo.Add(w);
			}
		}
	}
}
