using System;
using System.Collections.Generic;
using System.Text;
using GBHL;
using System.Drawing;

namespace ClassicMap
{
	//Well, here we load the town map and stuff.
	//There are 2 very weak compressions which we take care of and then create the map.
	public class TownMapLoader
	{
		private GBFile gb;
		public byte[] data;
		public byte[,,] tileData;
		private int tileLocation = 0x125A8;
		private int markerLocation = 0x70F40;

		public TownMapLoader(GBFile g)
		{
			gb = g;
			data = new byte[360];
		}

		public Bitmap LoadMap()
		{
			Bitmap b = new Bitmap(160, 144);
			FastPixel fp = new FastPixel(b);
			fp.rgbValues = new byte[160 * 144 * 4];
			fp.Lock();
			LoadData();
			LoadTileData();
			for (int i = 0; i < 360; i++)
			{
				for (int y = 0; y < 8; y++)
				{
					for (int x = 0; x < 8; x++)
					{
						fp.SetPixel((i % 20) * 8 + x, (i / 20) * 8 + y, Form1.Palette[tileData[data[i], x, y]]);
					}
				}
			}
			fp.Unlock(true);
			return b;
		}

		public Bitmap LoadMarker()
		{
			byte[] decompressed = new byte[0x40];
			gb.BufferLocation = markerLocation;
			int index = 0;
			for (int i = 0; i < 0x10; i++)
			{
				byte by = gb.ReadByte();
				decompressed[index++] = by;
				decompressed[index++] = by;
				by = gb.ReadByte();
				decompressed[index++] = by;
				decompressed[index++] = by;
			}

			byte[, ,] markerData = new byte[4, 8, 8];
			gb.ReadTiles(2, 2, decompressed, ref markerData);

			Bitmap b = new Bitmap(16, 16);
			FastPixel fp = new FastPixel(b);
			fp.rgbValues = new byte[1024];
			fp.Lock();
			LoadData();
			LoadTileData();
			for (int i = 0; i < 4; i++)
			{
				for (int y = 0; y < 8; y++)
				{
					for (int x = 0; x < 8; x++)
					{
						fp.SetPixel((i % 2) * 8 + x, (i / 2) * 8 + y, Form1.Palette[markerData[i, x, y]]);
					}
				}
			}
			fp.Unlock(true);
			b.MakeTransparent(Form1.Palette[0]);
			return b;
		}

		public void LoadData()
		{
			gb.BufferLocation = 0x71100;
			int index = 0;
			while (true)
			{
				byte b = gb.ReadByte();
				if (b == 0)
					break;
				int count = (b & 0xF);
				for (int i = 0; i < count; i++)
				{
					data[index++] = (byte)((b >> 4)); //Initially did + 60, however we're not using tiles like that
				}
			}
		}

		public void LoadTileData()
		{
			tileData = new byte[32, 8, 8];
			gb.ReadTiles(16, 2, tileLocation, ref tileData); 
		}
	}
}
