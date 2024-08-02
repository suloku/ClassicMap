using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using GBHL;

namespace ClassicMap
{
	public class TilesetLoader
	{
		private GBFile gb;
		private byte bank;
		private int formationAddress;
		private int graphicsAddress;
		private byte[, ,] formation;
		private byte[, ,] graphics;

		public TilesetLoader(GBFile g)
		{
			gb = g;
		}

		public void LoadTilesetHeader(int tileset)
		{
			gb.BufferLocation = 0xC7BE + tileset * 12;
			bank = gb.ReadByte();
			formationAddress = bank * 0x4000 + gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
			graphicsAddress = bank * 0x4000 + gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
		}

		public void LoadFormation()
		{
			formation = new byte[256, 4, 4];
			gb.BufferLocation = formationAddress;
			for (int i = 0; i < 256; i++)
			{
				for (int y = 0; y < 4; y++)
				{
					for (int x = 0; x < 4; x++)
					{
						formation[i, x, y] = gb.ReadByte();
					}
				}
			}
		}

		public void LoadGraphics()
		{
			graphics = new byte[256, 8, 8];
			gb.ReadTiles(16, 6, graphicsAddress, ref graphics);
		}

		public Bitmap LoadTileset(int tileset)
		{
			LoadTilesetHeader(tileset);
			LoadFormation();
			LoadGraphics();

			Bitmap b = new Bitmap(32, 8192);
			FastPixel fp = new FastPixel(b);
			fp.rgbValues = new byte[32 * 8192 * 4];
			fp.Lock();
			for (int i = 0; i < 256; i++)
			{
				for (int ytile = 0; ytile < 4; ytile++)
				{
					for (int xtile = 0; xtile < 4; xtile++)
					{
						for (int y = 0; y < 8; y++)
						{
							for (int x = 0; x < 8; x++)
							{
								fp.SetPixel(xtile * 8 + x, i * 32 + ytile * 8 + y, Form1.Palette[graphics[formation[i, xtile, ytile], x, y]]);
							}
						}
					}
				}
			}

			fp.Unlock(true);
			return b;
		}
	}
}
