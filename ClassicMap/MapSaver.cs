using System;
using System.Collections.Generic;
using System.Text;
using GBHL;

namespace ClassicMap
{
	public class MapSaver
	{
		private GBFile gb;
		private MapLoader mapLoader;
		private WildPokemonLoader wildPokemonLoader;
		public MapSaver(GBFile g, MapLoader ml, WildPokemonLoader wpl)
		{
			gb = g;
			mapLoader = ml;
			wildPokemonLoader = wpl;
		}

		public void Save(int map)
		{
			//Save the map header
			gb.BufferLocation = 0xC23D + map;
			gb.WriteByte((byte)(mapLoader.mapHeader.Location / 0x4000));

			gb.BufferLocation = 0x01AE + map * 2;
			gb.WriteByte((byte)mapLoader.mapHeader.Location);
			gb.WriteByte((byte)((((mapLoader.mapHeader.Location % 0x4000) >> 8) & 0xFF) + 0x40));

			gb.BufferLocation = mapLoader.mapHeader.Location;
			gb.WriteByte(mapLoader.mapHeader.Tileset);
			gb.WriteByte(mapLoader.mapHeader.Height);
			gb.WriteByte(mapLoader.mapHeader.Width);

			gb.WriteByte((byte)mapLoader.mapHeader.LevelData);
			gb.WriteByte((byte)((((mapLoader.mapHeader.LevelData % 0x4000) >> 8) & 0xFF) + 0x40));

			gb.BufferLocation += 4;
			//Skip connections for now
			byte connection = gb.ReadByte();
			if ((connection & (1 << 3)) != 0)
				gb.BufferLocation += 11;
			if ((connection & (1 << 2)) != 0)
				gb.BufferLocation += 11;
			if ((connection & (1 << 1)) != 0)
				gb.BufferLocation += 11;
			if ((connection & (1 << 0)) != 0)
				gb.BufferLocation += 11;

			gb.WriteByte((byte)mapLoader.mapHeader.EventData);
			gb.WriteByte((byte)((((mapLoader.mapHeader.EventData % 0x4000) >> 8) & 0xFF) + 0x40));

			//Save the level data
			gb.BufferLocation = mapLoader.mapHeader.LevelData;
			for (int y = 0; y < mapLoader.mapHeader.Height; y++)
			{
				for (int x = 0; x < mapLoader.mapHeader.Width; x++)
				{
					gb.WriteByte(mapLoader.LevelData[x, y]);
				}
			}

			//Save the event data. Oh boy...
			gb.BufferLocation = mapLoader.mapHeader.EventData;
			gb.WriteByte(mapLoader.borderTile);

			gb.WriteByte((byte)mapLoader.warps.Count);
			foreach (Warp w in mapLoader.warps)
			{
				gb.WriteByte(w.y);
				gb.WriteByte(w.x);
				gb.WriteByte(w.destPoint);
				gb.WriteByte(w.map);
			}
			gb.WriteByte((byte)mapLoader.signs.Count);
			foreach (Sign s in mapLoader.signs)
			{
				gb.WriteByte(s.y);
				gb.WriteByte(s.x);
				gb.WriteByte(s.text);
			}
			gb.WriteByte((byte)mapLoader.people.Count);
			foreach (Person p in mapLoader.people)
			{
				gb.WriteByte(p.sprite);
				//These are already formatted.
				gb.WriteByte(p.y);
				gb.WriteByte(p.x);
				gb.WriteByte(p.movement);
				gb.WriteByte(p.movement2);
				gb.WriteByte((byte)((p.text & 0x3F) | (p.originalText & 0xC0)));
				if ((p.originalText & 0x40) != 0)
				{
					gb.WriteByte(p.trainer);
					gb.WriteByte(p.pokemonSet);
				}
				else if ((p.originalText & 0x80) != 0)
				{
					gb.WriteByte(p.item);
				}
			}
			foreach (WarpTo w in mapLoader.warpTo)
			{
				gb.WriteByte((byte)w.address);
				gb.WriteByte((byte)(w.address >> 8));
				gb.WriteByte(w.y);
				gb.WriteByte(w.x);
			}

			//End of header stuff! Now to save wild Pokemon... Yaaaay...
			//Also, for some very, very weird reason, the foreach loop doesn't work here.
			//There are only 10 pokemon per array, yet some data wasn't saving properly.
			gb.BufferLocation = wildPokemonLoader.wildPokemonLocation;
			gb.WriteByte(wildPokemonLoader.rarityGrass);
			if (wildPokemonLoader.rarityGrass > 0)
			{
				for(int i = 0; i < 10; i++)
				{
					Pokemon p = wildPokemonLoader.grassPokemon[i];
					gb.WriteByte(p.level);
					gb.WriteByte(p.id);
				}
			}
			gb.WriteByte(wildPokemonLoader.rarityWater);
			if (wildPokemonLoader.rarityWater > 0)
			{
				for (int i = 0; i < 10; i++)
				{
					Pokemon p = wildPokemonLoader.waterPokemon[i];
					gb.WriteByte(p.level);
					gb.WriteByte(p.id);
				}
			}

			//Huh... That was a lot easier than I had expected.
		}
	}
}
