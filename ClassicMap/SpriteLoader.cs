using System;
using System.Collections.Generic;
using System.Text;
using GBHL;
using System.Drawing;

namespace ClassicMap
{
	public class SpriteLoader
	{
		private GBFile gb;

		public SpriteLoader(GBFile g)
		{
			gb = g;
		}

		public int CalculateTDA(int sprite)
		{
			return 0x17B27 + (sprite - 1) * 4;
		}

		public Bitmap LoadSprite(int sprite)
		{
			gb.BufferLocation = CalculateTDA(sprite);
			int loc = gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
			int count = gb.ReadByte();
			byte bank = gb.ReadByte();
			if (bank * 0x4000 + loc + 256 >= 0x100000)
			{
				return new Bitmap(16, 16);
			}

			gb.BufferLocation = bank * 0x4000 + loc;

			if (gb.BufferLocation < 0)
				return new Bitmap(16, 16);

			byte[, ,] tileData = new byte[32, 8, 8];
			gb.ReadTiles(16, 2, gb.BufferLocation, ref tileData);

			Bitmap b = new Bitmap(16, 16);
			FastPixel fp = new FastPixel(b);
			fp.rgbValues = new byte[1024];
			fp.Lock();
			for (int y = 0; y < 2; y++)
			{
				for (int x = 0; x < 2; x++)
				{
					for (int xx = 0; xx < 8; xx++)
					{
						for (int yy = 0; yy < 8; yy++)
						{
							fp.SetPixel(x * 8 + xx, y * 8 + yy, Form1.Palette[tileData[x + y * 2, xx, yy]]);
						}
					}
				}
			}
			fp.Unlock(true);

			b.MakeTransparent(Form1.Palette[0]);
			return b;
		}
	}
}
