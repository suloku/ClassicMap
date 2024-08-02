using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using GBHL;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace ClassicMap
{
	public partial class Form1 : Form
	{
		private string romLocation = "";
		private GBFile gb = null;
		private TownMapLoader townMapLoader;
		private TilesetLoader tilesetLoader;
		private MapLoader mapLoader;
		private WildPokemonLoader wildPokemonLoader;
		private SpriteLoader spriteLoader;
		private Bitmap townMapMarker;
		private Bitmap townMap;
		private Bitmap[] cachedTilesets;
		private int selectedTile = 0;
		private Point lastClick;
		private ImageAttributes eventTransparency;
		private ImageAttributes eventTTransparency;
		private Rectangle eventRectangle = new Rectangle(0, 0, 16, 16);
		private int selectedPerson = -1, selectedSign = -1, selectedWarp = -1, selectedWarpTo = -1;
		private bool triggerPokemonEvents; //Ugh... Didn't want to have to resort to this. Probably don't, but oh well.

		public static Color[] Palette = new Color[] { Color.White, Color.FromArgb(208, 208, 208), Color.FromArgb(168, 168, 168), Color.Black };


		//Open File Dialogs
		private OpenFileDialog openROM;

		public Form1()
		{
			InitializeComponent();
			//Initialize the Open File Dialogs
			openROM = new OpenFileDialog();
			openROM.Title = "Select a Pokemon Red/Blue ROM";
			openROM.Filter = "All Supported Types|*.bin;*.gb";

			ColorMatrix cm = new ColorMatrix();
			cm.Matrix33 = (float).7;
			eventTransparency = new ImageAttributes();
			eventTransparency.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
			ColorMatrix cm2 = new ColorMatrix();
			cm2.Matrix33 = (float).6;
			eventTTransparency = new ImageAttributes();
			eventTTransparency.SetColorMatrix(cm2, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
		}

		private void openROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (openROM.ShowDialog() != DialogResult.OK)
				return;
			FileInfo f = new FileInfo(openROM.FileName);
			bool enable = true;
			if (f.IsReadOnly)
			{
				MessageBox.Show("Warning! ROM is read-only. You will not be able to save.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				enable = false;
			}
			try
			{
				BinaryReader br = new BinaryReader(File.OpenRead(openROM.FileName));
				byte[] buffer = br.ReadBytes((int)br.BaseStream.Length);
				br.Close();
				gb = new GBFile(buffer);
				if (enable)
					saveROMToolStripMenuItem.Enabled = true;
			}
			catch (IOException ex)
			{
				MessageBox.Show("Error reading ROM.\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			DoInitialLoading();
		}

		public void DoInitialLoading()
		{
			romLocation = openROM.FileName;
			EnableFileWatcher();
			BleControls(true);
			lastClick = new Point(-1, -1);
			triggerPokemonEvents = true;

			townMapLoader = new TownMapLoader(gb);
			tilesetLoader = new TilesetLoader(gb);
			mapLoader = new MapLoader(gb);
			wildPokemonLoader = new WildPokemonLoader(gb);
			spriteLoader = new SpriteLoader(gb);

			cachedTilesets = new Bitmap[256];
			wildPokemonLoader.LoadPokemonNames();
			foreach (string name in wildPokemonLoader.PokemonNames)
			{
				if (name == null)
					continue;
				cboGrassPokemon.Items.Add(name);
				cboWaterPokemon.Items.Add(name);
			}

			townMap = townMapLoader.LoadMap();
			townMapMarker = townMapLoader.LoadMarker();
			nTownMap_ValueChanged(null, null);
			LoadMap((int)nMap.Value);
		}

		private void EnableFileWatcher()
		{
			string[] parts = romLocation.Split('\\');
			string f = "";
			for (int i = 0; i < parts.Length - 1; i++)
				f += parts[i] + "\\";
			romWatcher.Path = f;
			romWatcher.EnableRaisingEvents = true;
		}

		private void BleControls(bool b)
		{
			groupBox1.Enabled = b;
			groupBox2.Enabled = b;
			tabControl1.Enabled = b;
		}

		private void pTownMap_Paint(object sender, PaintEventArgs e)
		{

		}

		private void nTownMap_ValueChanged(object sender, EventArgs e)
		{
			if (nTownMap.Value == 47)
				nTownMap.Value = 0;
			if (nTownMap.Value == -1)
				nTownMap.Value = 46;
			Bitmap b = new Bitmap(townMap);
			if (sender == nMap)
			{
				pTownMap.Image = b;
				return;
			}
			Graphics g = Graphics.FromImage(b);
			int x = 0, y = 0;
			byte map = gb.ReadByte(0x70F11 + (int)nTownMap.Value);
			nMap.Value = map;
			int namePointer = 0;
			if (map <= 0x25)
			{
				gb.BufferLocation = 0x71313 + map * 3;
				byte location = gb.ReadByte();
				x = (location & 0xF) * 8 + 12;
				y = (location >> 4) * 8 + 4;
				namePointer = gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
			}
			else
			{
				gb.BufferLocation = 0x71382;
				while (true)
				{
					if (gb.ReadByte() > map)
					{
						byte location = gb.ReadByte();
						x = (location & 0xF) * 8 + 12;
						y = (location >> 4) * 8 + 4;
						namePointer = gb.ReadByte() + ((gb.ReadByte() - 0x40) * 0x100);
						break;
					}
					else
						gb.BufferLocation += 3;
				}
			}
			g.DrawImage(townMapMarker, x, y);
			pTownMap.Image = b;

			//Read the name
			gb.BufferLocation = 0x70000 + namePointer;
			byte by;
			string text = "";
			while ((by = gb.ReadByte()) != 0x50)
			{
				text = text + TextTable.GetString(by);
			}
			lblName.Text = "Map Name: " + text;
		}

		private void nMap_ValueChanged(object sender, EventArgs e)
		{
			nTownMap_ValueChanged(nMap, null);
			if (!LoadMap((int)nMap.Value))
			{
				groupBox2.Enabled = false;
				tabControl1.Enabled = false;
				pMap.Image = pTileset.Image = pTile.Image = null;
				lblMapHeader.Text = "";
				this.BeginInvoke(new MethodInvoker(ShowMapError));
			}
		}

		private void ShowMapError()
		{
			MessageBox.Show("Error loading map or tileset. Probably a map that isn't actually a map.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public bool LoadMap(int map)
		{
			try
			{
				if (nMap.Value != map)
					nMap.Value = map;
				lastClick = new Point(-1, -1);
				mapLoader.LoadMapHeader(map);
				mapLoader.LoadLevelData();
				wildPokemonLoader.LoadWildPokemon(map);

				lblMapHeader.Text = "Header Location: 0x" + mapLoader.mapHeader.Location.ToString("X") +
					"\nTileset: " + mapLoader.mapHeader.Tileset +
					"\nWidth: " + mapLoader.mapHeader.Width +
					"\nHeight: " + mapLoader.mapHeader.Height +
					"\nLevel Data: 0x" + mapLoader.mapHeader.LevelData.ToString("X") +
					"\nEvent Data: 0x" + mapLoader.mapHeader.EventData.ToString("X");
				lblWildPokemon.Text = "Wild Pokemon: " + (wildPokemonLoader.rarityGrass > 0 ? "Grass" + (wildPokemonLoader.rarityWater > 0 ? "/Water" : "") : (wildPokemonLoader.rarityWater > 0 ? "Water" : "None"));

				tbGrass.Value = wildPokemonLoader.rarityGrass;
				lblGrassEncounter.Text = "Encounter Frequency: " + tbGrass.Value + "/255";
				if (wildPokemonLoader.rarityGrass > 0)
				{
					grpGrass.Visible = true;
					pnlGrass.Visible = false;
					lstGrass.Items.Clear();
					foreach (Pokemon p in wildPokemonLoader.grassPokemon)
					{
						lstGrass.Items.Add(wildPokemonLoader.PokemonNames[wildPokemonLoader.PokemonIndicies[p.id - 1]].Substring(4) + " - " + p.level.ToString());
					}
					if (lstGrass.SelectedIndex == 0)
						lstGrass_SelectedIndexChanged(null, null);
					else
						lstGrass.SelectedIndex = 0;
				}
				else
				{
					pnlGrass.Visible = true;
					grpGrass.Visible = false;
				}

				tbWater.Value = wildPokemonLoader.rarityWater;
				lblWaterEncounter.Text = "Encounter Frequency: " + tbWater.Value + "/255";
				if (wildPokemonLoader.rarityWater > 0)
				{
					grpWater.Visible = true;
					pnlWater.Visible = false;
					lstWater.Items.Clear();
					foreach (Pokemon p in wildPokemonLoader.waterPokemon)
					{
						lstWater.Items.Add(wildPokemonLoader.PokemonNames[wildPokemonLoader.PokemonIndicies[p.id - 1]].Substring(4) + " - " + p.level.ToString());
					}
					if (lstWater.SelectedIndex == 0)
						lstWater_SelectedIndexChanged(null, null);
					else
						lstWater.SelectedIndex = 0;
				}
				else
				{
					pnlWater.Visible = true;
					grpWater.Visible = false;
				}

				if (cachedTilesets[mapLoader.mapHeader.Tileset] == null)
				{
					cachedTilesets[mapLoader.mapHeader.Tileset] = tilesetLoader.LoadTileset(mapLoader.mapHeader.Tileset);
				}
				pTileset.Image = cachedTilesets[mapLoader.mapHeader.Tileset];
				UpdateSelectedTile();
				DrawMap();

				ResetSelects();
				pnlPerson.Visible = false;
				pnlWarp.Visible = false;
				pnlSign.Visible = false;
				pnlWarpTo.Visible = false;
				nSelectedPerson.Maximum = mapLoader.people.Count - 1;
				nSelectedWarp.Maximum = mapLoader.warps.Count - 1;
				nSelectedSign.Maximum = mapLoader.signs.Count - 1;
				nSelectedWarpTo.Maximum = mapLoader.warpTo.Count - 1;

				pEventMap.Image = pMap.Image;
				if (tabControl1.SelectedIndex == 0)
					pEventMap.Invalidate();

				BleControls(true);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			panel4.VerticalScroll.SmallChange = 32;
			panel4.VerticalScroll.LargeChange = 128;
		}

		private void UpdateSelectedTile()
		{
			Bitmap b = new Bitmap(32, 32);
			for (int y = 0; y < 32; y++)
				for (int x = 0; x < 32; x++)
					b.SetPixel(x, y, cachedTilesets[mapLoader.mapHeader.Tileset].GetPixel(x, selectedTile * 32 + y));
			pTile.Image = b;
			pTileset.Invalidate();
		}

		private void pTileset_MouseDown(object sender, MouseEventArgs e)
		{
			selectedTile = e.Y / 32;
			UpdateSelectedTile();
			pTileset.Invalidate();
		}

		private void pTileset_Paint(object sender, PaintEventArgs e)
		{
			if (pTileset.Image != null)
				e.Graphics.DrawRectangle(Pens.Red, 0, selectedTile * 32, 31, 31);
		}

		private void DrawMap()
		{
			Bitmap b = new Bitmap(mapLoader.mapHeader.Width * 32, mapLoader.mapHeader.Height * 32);
			FastPixel fp = new FastPixel(b);
			fp.rgbValues = new byte[b.Width * b.Height * 4];
			fp.Lock();
			FastPixel source = new FastPixel(cachedTilesets[mapLoader.mapHeader.Tileset]);
			source.rgbValues = new byte[source.Width * source.Height * 4];
			source.Lock();
			for (int ytile = 0; ytile < mapLoader.mapHeader.Height; ytile++)
			{
				for (int xtile = 0; xtile < mapLoader.mapHeader.Width; xtile++)
				{
					for (int y = 0; y < 32; y++)
					{
						for (int x = 0; x < 32; x++)
						{
							fp.SetPixel(xtile * 32 + x, ytile * 32 + y, source.GetPixel(x, mapLoader.LevelData[xtile, ytile] * 32 + y));
						}
					}
				}
			}
			source.Unlock(false);
			fp.Unlock(true);
			pMap.Image = b;
		}

		private void DrawEvents(Graphics g)
		{
			foreach (WarpTo w in mapLoader.warpTo)
			{
				eventRectangle.X = w.x * 16;
				eventRectangle.Y = w.y * 16;
				g.DrawImage(ClassicMap.Properties.Resources.trigger, eventRectangle, 0, 0, 16, 16, GraphicsUnit.Pixel, eventTTransparency);
			}

			foreach (Warp w in mapLoader.warps)
			{
				eventRectangle.X = w.x * 16;
				eventRectangle.Y = w.y * 16;
				g.DrawImage(ClassicMap.Properties.Resources.warp, eventRectangle, 0, 0, 16, 16, GraphicsUnit.Pixel, eventTransparency);
			}

			foreach (Sign s in mapLoader.signs)
			{
				eventRectangle.X = s.x * 16;
				eventRectangle.Y = s.y * 16;
				g.DrawImage(ClassicMap.Properties.Resources.sign, eventRectangle, 0, 0, 16, 16, GraphicsUnit.Pixel, eventTransparency);
			}

			foreach (Person p in mapLoader.people)
			{
				eventRectangle.X = p.x * 16 - 64;
				eventRectangle.Y = p.y * 16 - 64;
				if (!spriteImagesToolStripMenuItem.Checked)
					g.DrawImage(ClassicMap.Properties.Resources.person, eventRectangle, 0, 0, 16, 16, GraphicsUnit.Pixel, eventTransparency);
				else
					g.DrawImage(spriteLoader.LoadSprite(p.sprite), p.x * 16 - 64, p.y * 16 - 64);
			}

			if (selectedPerson != -1 && selectedPerson < mapLoader.people.Count)
				g.DrawRectangle(Pens.Red, mapLoader.people[selectedPerson].x * 16 - 64, mapLoader.people[selectedPerson].y * 16 - 64, 16, 16);
			else if (selectedWarp != -1 && selectedWarp < mapLoader.warps.Count)
				g.DrawRectangle(Pens.Red, mapLoader.warps[selectedWarp].x * 16, mapLoader.warps[selectedWarp].y * 16, 16, 16);
			else if (selectedSign != -1 && selectedSign < mapLoader.signs.Count)
				g.DrawRectangle(Pens.Red, mapLoader.signs[selectedSign].x * 16, mapLoader.signs[selectedSign].y * 16, 16, 16);
			else if (selectedWarpTo != -1 && selectedWarpTo < mapLoader.warpTo.Count)
				g.DrawRectangle(Pens.Red, mapLoader.warpTo[selectedWarpTo].x * 16, mapLoader.warpTo[selectedWarpTo].y * 16, 16, 16);
		}

		private void Form1_Resize(object sender, EventArgs e)
		{
			tabControl1.Width = this.Width - 212;
			tabControl1.Height = this.Height - 79;
			panel4.Height = tabControl1.Height - 95;
			pTile.Top = panel4.Top + panel4.Height + 15;
			groupBox2.Height = this.Height - 346;
		}

		private void pMap_MouseDown(object sender, MouseEventArgs e)
		{
			panel2.Focus();
			if (e.Button == MouseButtons.Left)
			{
				if (e.X < 0 || e.Y < 0 || e.X / 32 >= mapLoader.mapHeader.Width || e.Y / 32 >= mapLoader.mapHeader.Height)
					return;
				mapLoader.LevelData[e.X / 32, e.Y / 32] = (byte)selectedTile;
				Graphics g = Graphics.FromImage(pMap.Image);
				g.DrawImage(pTile.Image, e.X / 32 * 32, e.Y / 32 * 32);
				pMap.Invalidate(new Rectangle(e.X / 32 * 32, e.Y / 32 * 32, 32, 32));
			}
			else if (e.Button == MouseButtons.Right)
			{
				selectedTile = mapLoader.LevelData[e.X / 32, e.Y / 32];
				panel4.VerticalScroll.Value = selectedTile * 32 + (selectedTile > 0 ? 2 : 0);
				panel4.VerticalScroll.Value = selectedTile * 32 + (selectedTile > 0 ? 2 : 0);
				UpdateSelectedTile();
				pTileset.Invalidate();
			}
		}

		private void pMap_MouseMove(object sender, MouseEventArgs e)
		{
			SetPositionLabel(e.X, e.Y);
			if (e.Button == MouseButtons.Left)
			{
				int x = e.X / 32 * 32;
				int y = e.Y / 32 * 32;
				if (x == lastClick.X && y == lastClick.Y)
					return;
				lastClick = new Point(x, y);
				if (x < 0 || x / 32 >= mapLoader.mapHeader.Width || y < 0 || y / 32 >= mapLoader.mapHeader.Height)
					return;
				mapLoader.LevelData[x / 32, y / 32] = (byte)selectedTile;
				Graphics g = Graphics.FromImage(pMap.Image);
				g.DrawImage(pTile.Image, x, y);
				pMap.Invalidate(new Rectangle(e.X / 32 * 32, e.Y / 32 * 32, 32, 32));
			}
		}

		private void SetPositionLabel(int x, int y)
		{
			lblPosition.Text = "X: " + (x / 32).ToString() + " Y: " + (y / 32).ToString();
		}

		private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
		{
			lastClick = new Point(-1, -1);
			if (tabControl1.SelectedIndex == 1)
				pEventMap.Image = pMap.Image;
		}

		private void pEventMap_MouseMove(object sender, MouseEventArgs e)
		{
			SetPositionLabel(e.X * 2, e.Y * 2);
			if (e.Button != MouseButtons.Left)
				return;
			if (e.X / 16 == lastClick.X && e.Y / 16 == lastClick.Y)
				return;
			lastClick = new Point(e.X / 16, e.Y / 16);
			//Okay. So here we do Event x = mapLoader.eventGroup[selectedX];
			//However, when I wrote this the events were structures, not classes.
			//Because of this, variable could not be directly modified. This was changed later on.
			if (selectedPerson != -1)
			{
				Person p = mapLoader.people[selectedPerson];
				p.x = (byte)(4 + e.X / 16);
				p.y = (byte)(4 + e.Y / 16);
				mapLoader.people[selectedPerson] = p;
				pEventMap.Invalidate();
			}
			else if (selectedWarp != -1)
			{
				Warp w = mapLoader.warps[selectedWarp];
				w.x = (byte)(e.X / 16);
				w.y = (byte)(e.Y / 16);
				mapLoader.warps[selectedWarp] = w;
				pEventMap.Invalidate();
			}
			else if (selectedSign != -1)
			{
				Sign s = mapLoader.signs[selectedSign];
				s.x = (byte)(e.X / 16);
				s.y = (byte)(e.Y / 16);
				mapLoader.signs[selectedSign] = s;
				pEventMap.Invalidate();
			}
			else if (selectedWarpTo != -1)
			{
				WarpTo w = mapLoader.warpTo[selectedWarpTo];
				w.x = (byte)(e.X / 16);
				w.y = (byte)(e.Y / 16);
				mapLoader.warpTo[selectedWarpTo] = w;
				pEventMap.Invalidate();
			}
		}

		private void pEventMap_Paint(object sender, PaintEventArgs e)
		{
			DrawEvents(e.Graphics);
		}

		private void ResetSelects()
		{
			selectedPerson = -1;
			selectedSign = -1;
			selectedWarp = -1;
			selectedWarpTo = -1;
		}

		private void pEventMap_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)
				return;

			panel1.Focus();
			ResetSelects();

			if ((selectedPerson = GetPersonIndex(e.X, e.Y)) != -1)
			{
				SelectPerson();
			}
			else if ((selectedWarp = GetWarpIndex(e.X, e.Y)) != -1)
			{
				SelectWarp();
			}
			else if ((selectedSign = GetSignIndex(e.X, e.Y)) != -1)
			{
				SelectSign();
			}
			else if ((selectedWarpTo = GetWarpToIndex(e.X, e.Y)) != -1)
			{
				SelectWarpTo();
			}
			else
			{
				pnlPerson.Visible = false;
			}

			pEventMap.Invalidate();
		}

		private void SelectPerson()
		{
			pnlPerson.Visible = true;
			pnlWarp.Visible = false;
			pnlSign.Visible = false;
			pnlWarpTo.Visible = false;
			nSelectedPerson.Value = selectedPerson;
			nPersonPicture.Value = mapLoader.people[selectedPerson].sprite;
			nPersonMovement1.Value = mapLoader.people[selectedPerson].movement;
			nPersonMovement2.Value = mapLoader.people[selectedPerson].movement2;
			nPersonPicture.Value = mapLoader.people[selectedPerson].sprite;
			nPersonText.Value = (mapLoader.people[selectedPerson].text & 0x3F);
			if ((mapLoader.people[selectedPerson].originalText & 0x40) != 0)
			{
				lblPersonItem.Enabled = false;
				nPersonItem.Enabled = false;
				lblPersonTrainer.Enabled = true;
				nPersonTrainer.Enabled = true;
				lblPersonPokemonSet.Enabled = true;
				nPersonPokemonSet.Enabled = true;
				nPersonTrainer.Value = mapLoader.people[selectedPerson].trainer;
				nPersonPokemonSet.Value = mapLoader.people[selectedPerson].pokemonSet;
			}
			else if ((mapLoader.people[selectedPerson].originalText & 0x80) != 0)
			{
				lblPersonItem.Enabled = true;
				nPersonItem.Enabled = true;
				lblPersonTrainer.Enabled = false;
				nPersonTrainer.Enabled = false;
				lblPersonPokemonSet.Enabled = false;
				nPersonPokemonSet.Enabled = false;
				nPersonItem.Value = mapLoader.people[selectedPerson].item;
			}
			else
			{
				lblPersonItem.Enabled = false;
				nPersonItem.Enabled = false;
				lblPersonTrainer.Enabled = false;
				nPersonTrainer.Enabled = false;
				lblPersonPokemonSet.Enabled = false;
				nPersonPokemonSet.Enabled = false;
			}
		}

		public void SelectWarp()
		{
			pnlWarp.Visible = true;
			pnlPerson.Visible = false;
			pnlSign.Visible = false;
			pnlWarpTo.Visible = false;
			nSelectedWarp.Value = selectedWarp;
			nWarpDest.Value = mapLoader.warps[selectedWarp].destPoint;
			nWarpMap.Value = mapLoader.warps[selectedWarp].map;
		}

		public void SelectSign()
		{
			pnlSign.Visible = false;
			pnlPerson.Visible = false;
			pnlSign.Visible = true;
			pnlWarpTo.Visible = false;
			nSelectedSign.Value = selectedSign;
			nSignText.Value = mapLoader.signs[selectedSign].text;
		}

		public void SelectWarpTo()
		{
			pnlWarp.Visible = false;
			pnlPerson.Visible = false;
			pnlSign.Visible = false;
			pnlWarpTo.Visible = true;
			nSelectedWarpTo.Value = selectedWarpTo;
			nWarpToEvent.Value = mapLoader.warpTo[selectedWarpTo].address;
		}

		private int GetPersonIndex(int x, int y)
		{
			for (int i = mapLoader.people.Count - 1; i > -1; i--)
			{
				if (mapLoader.people[i].x - 4 == x / 16 && mapLoader.people[i].y - 4 == y / 16)
					return i;
			}

			return -1;
		}

		private int GetWarpIndex(int x, int y)
		{
			for (int i = mapLoader.warps.Count - 1; i > -1; i--)
			{
				if (mapLoader.warps[i].x == x / 16 && mapLoader.warps[i].y == y / 16)
					return i;
			}

			return -1;
		}

		private int GetSignIndex(int x, int y)
		{
			for (int i = mapLoader.signs.Count - 1; i > -1; i--)
			{
				if (mapLoader.signs[i].x == x / 16 && mapLoader.signs[i].y == y / 16)
					return i;
			}

			return -1;
		}

		private int GetWarpToIndex(int x, int y)
		{
			for (int i = mapLoader.warpTo.Count - 1; i > -1; i--)
			{
				if (mapLoader.warpTo[i].x == x / 16 && mapLoader.warpTo[i].y == y / 16)
					return i;
			}

			return -1;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			int warp = (int)nWarpDest.Value;
			if (!LoadMap((int)nWarpMap.Value))
			{
				ShowMapError();
				return;
			}

			if (warp < mapLoader.warpTo.Count)
			{
				selectedWarpTo = warp;
				SelectWarpTo();
			}
			else
				MessageBox.Show("Destination point out of range.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			//Okay, this had me stumped for a few minutes.
			//On the Pokemon Red notes, a basic formula was provided with very little detail.
			//It turns out the x/y had to be their even numbers (block they were on).
			//That wasn't specified so it threw me off.
			//However, this works, as the original address was calculated successfully with all tests.
			int address = 0xC6EF;
			address += mapLoader.mapHeader.Width;
			address += (mapLoader.mapHeader.Width + 6) * ((mapLoader.warpTo[selectedWarpTo].y / 2) * 2 / 2);
			address += mapLoader.warpTo[selectedWarpTo].x / 2 * 2 / 2;
			nWarpToEvent.Value = address;
		}

		private void pEventMap_MouseUp(object sender, MouseEventArgs e)
		{
			lastClick = new Point(-1, -1);
		}

		private void pMap_MouseUp(object sender, MouseEventArgs e)
		{
			lastClick = new Point(-1, -1);
		}

		private void nWarpToEvent_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.warpTo[selectedWarpTo].address = (int)nWarpToEvent.Value;
		}

		private void nWarpDest_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.warps[selectedWarp].destPoint = (byte)nWarpDest.Value;
		}

		private void nWarpMap_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.warps[selectedWarp].map = (byte)nWarpMap.Value;
		}

		private void nPersonPicture_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.people[selectedPerson].sprite = (byte)nPersonPicture.Value;
			pEventMap.Invalidate();
		}

		private void nPersonMovement1_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.people[selectedPerson].movement = (byte)nPersonMovement1.Value;
		}

		private void nPersonMovement2_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.people[selectedPerson].movement2 = (byte)nPersonMovement2.Value;
		}

		private void nPersonText_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.people[selectedPerson].text = (byte)nPersonText.Value;
		}

		private void nPersonTrainer_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.people[selectedPerson].trainer = (byte)nPersonTrainer.Value;
		}

		private void nPersonPokemonSet_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.people[selectedPerson].pokemonSet = (byte)nPersonPokemonSet.Value;
		}

		private void nPersonItem_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.people[selectedPerson].item = (byte)nPersonItem.Value;
		}

		private void nSelectedPerson_ValueChanged(object sender, EventArgs e)
		{
			if (nSelectedPerson.Maximum == -1)
				return;
			selectedPerson = (int)nSelectedPerson.Value;
			SelectPerson();
			pEventMap.Invalidate();
		}

		private void nSelectedSign_ValueChanged(object sender, EventArgs e)
		{
			if (nSelectedSign.Maximum == -1)
				return;
			selectedSign = (int)nSelectedSign.Value;
			SelectSign();
			pEventMap.Invalidate();
		}

		private void nSignText_ValueChanged(object sender, EventArgs e)
		{
			mapLoader.signs[selectedSign].text = (byte)nSignText.Value;
		}

		private void nSelectedWarpTo_ValueChanged(object sender, EventArgs e)
		{
			if (nSelectedWarpTo.Maximum == -1)
				return;
			selectedWarpTo = (int)nSelectedWarpTo.Value;
			SelectWarpTo();
			pEventMap.Invalidate();
		}

		private void nSelectedWarp_ValueChanged(object sender, EventArgs e)
		{
			if (nSelectedWarp.Maximum == -1)
				return;
			selectedWarp = (int)nSelectedWarp.Value;
			SelectWarp();
			pEventMap.Invalidate();
		}

		private void saveROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (romLocation == "")
				return;
			MapSaver mapSaver = new MapSaver(gb, mapLoader, wildPokemonLoader);
			mapSaver.Save((int)nMap.Value);
			BinaryWriter bw = new BinaryWriter(File.Open(romLocation, FileMode.Open));
			bw.Write(gb.Buffer);
			bw.Close();
		}

		private void romWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			if (e.FullPath != romLocation)
				return;
			FileInfo f = new FileInfo(romLocation);
			if (!f.IsReadOnly)
				saveROMToolStripMenuItem.Enabled = true;
			else
			{
				MessageBox.Show("Warning! ROM has been changed to read-only. You cannot save until it is writable.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				saveROMToolStripMenuItem.Enabled = false;
			}
		}

		private void lstGrass_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!triggerPokemonEvents)
				return;
			if (lstGrass.SelectedIndex == -1 && grpGrass.Visible)
				lstGrass.SelectedIndex = 0;
			nGrassLevel.Value = wildPokemonLoader.grassPokemon[lstGrass.SelectedIndex].level;
			cboGrassPokemon.SelectedIndex = wildPokemonLoader.PokemonIndicies[wildPokemonLoader.grassPokemon[lstGrass.SelectedIndex].id - 1];
		}

		private void lstWater_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!triggerPokemonEvents)
				return;
			if (lstWater.SelectedIndex == -1 && grpWater.Visible)
				lstWater.SelectedIndex = 0;
			nWaterLevel.Value = wildPokemonLoader.waterPokemon[lstWater.SelectedIndex].level;
			cboWaterPokemon.SelectedIndex = wildPokemonLoader.PokemonIndicies[wildPokemonLoader.waterPokemon[lstWater.SelectedIndex].id - 1];
		}

		private void cboGrassPokemon_SelectedIndexChanged(object sender, EventArgs e)
		{
			string s = cboGrassPokemon.SelectedItem.ToString().Substring(4) + " - " + (int)nGrassLevel.Value;
			if ((string)lstGrass.Text == s)
				return;
			triggerPokemonEvents = false;
			int id = int.Parse(cboGrassPokemon.SelectedItem.ToString().Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
			wildPokemonLoader.grassPokemon[lstGrass.SelectedIndex].id = (byte)(id + 1);
			lstGrass.Items[lstGrass.SelectedIndex] = s;
			triggerPokemonEvents = true;
		}

		private void nGrassLevel_ValueChanged(object sender, EventArgs e)
		{
			if (cboGrassPokemon.SelectedItem == null)
				return;
			string s = cboGrassPokemon.SelectedItem.ToString().Substring(4) + " - " + (int)nGrassLevel.Value;
			if ((string)lstGrass.Text == s)
				return;
			triggerPokemonEvents = false;
			wildPokemonLoader.grassPokemon[lstGrass.SelectedIndex].level = (byte)nGrassLevel.Value;
			lstGrass.Items[lstGrass.SelectedIndex] = s;
			triggerPokemonEvents = true;
		}

		private void tbGrass_Scroll(object sender, EventArgs e)
		{
			lblGrassEncounter.Text = "Encounter Frequency: " + tbGrass.Value + "/255";
			wildPokemonLoader.rarityGrass = (byte)tbGrass.Value;
		}

		private void tbWater_Scroll(object sender, EventArgs e)
		{
			lblWaterEncounter.Text = "Encounter Frequency: " + tbWater.Value + "/255";
			wildPokemonLoader.rarityWater = (byte)tbWater.Value;
		}

		private void cboWaterPokemon_SelectedIndexChanged(object sender, EventArgs e)
		{
			string s = cboWaterPokemon.SelectedItem.ToString().Substring(4) + " - " + (int)nWaterLevel.Value;
			if ((string)lstWater.Text == s)
				return;
			triggerPokemonEvents = false;
			int id = int.Parse(cboWaterPokemon.SelectedItem.ToString().Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
			wildPokemonLoader.waterPokemon[lstWater.SelectedIndex].id = (byte)(id + 1);
			lstWater.Items[lstWater.SelectedIndex] = s;
			triggerPokemonEvents = true;
		}

		private void nWaterLevel_ValueChanged(object sender, EventArgs e)
		{
			if (cboWaterPokemon.SelectedItem == null)
				return;
			string s = cboWaterPokemon.SelectedItem.ToString().Substring(4) + " - " + (int)nWaterLevel.Value;
			if ((string)lstWater.Text == s)
				return;
			triggerPokemonEvents = false;
			wildPokemonLoader.waterPokemon[lstWater.SelectedIndex].level = (byte)nWaterLevel.Value;
			lstWater.Items[lstWater.SelectedIndex] = s;
			triggerPokemonEvents = true;
		}

		private void spriteImagesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (tabControl1.SelectedIndex == 1 && tabControl1.Enabled)
				pEventMap.Invalidate();
		}
	}
}
