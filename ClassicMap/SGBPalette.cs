using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
//using JR.Utils.GUI.Forms; //Flexiblemessagebox

namespace ClassicMap
{
    public partial class SGBPalette : Form
    {
        public static SGBPALETTEDATA sgbpalettes;

        public static int SGBPaletteOffset = 0x1f4000;
        public static int SGBPaletteSize = 8;
        public static int SGBPaletteNum = 256;
        public static int SGBPalettes_totalSize = SGBPaletteSize * SGBPaletteNum;

        public static int mapPalettesAddr = 0x72750; //Stored as 1 byte per map, in map index order (256 entries)
        public static int normalPalettesAddr = 0x73250;
        public static int shinyPalettesAddr = 0x725d0;
        public static int trainerPalettesAddr = 0x726d0;
        public static int TrainerPicAndMoneyPointers = 0x101914;

        public static string mapSharedPalettes;
        public static string monSharedPalettes;
        public static string shinySharedPalettes;
        public static string trainerSharedPalettes;

        private static byte[] frontbppbuffer = new byte[784];
        private static byte[] backbppbuffer = new byte[784];

        private static int MonFrontSpriteAddress = 0;
        private static int MonBackSpriteAddress = 0;

        private static byte[] tempbuffer;

        private string twobppfilter = "2 byte per pixel image|*.2bpp|All Files (*.*)|*.*";
        private static byte[] external2bpp;

        private bool SwapColumns2bpp = true;

        private bool trainerMode = false;


        Color[] CurrentPaletteColors = new Color[4];

        Bitmap Frontbmp = new Bitmap(56, 56);
        Bitmap Frontresized;
        Bitmap Backbmp = new Bitmap(56, 56);
        Bitmap Backresized;

        bool autoupdate = false;
        public SGBPalette()
        {
            //InitializeComponent();
            /*
            mapComboBox.Items.AddRange(BrownEditor.editor.evomoves.brownMaps);

            SwapColumns2bpp = true;

            tempbuffer = new byte[0x200000];
            BrownEditor.MainForm.filebuffer.CopyTo(tempbuffer, 0);
            sgbpalettes = new SGBPALETTEDATA(tempbuffer.Skip(SGBPaletteOffset).Take(SGBPalettes_totalSize).ToArray());

            Note_label.Text = "Note: Color 0 is forced to 7fbf in most\n" +
                              "circumstances regardless of the palette.\n" +
                              "The same happens with Color 3 and black.";

            load_palette((int)paletteIndex.Value); //Also loads images

            pkmTrnComboBox.SelectedIndex = 5; //Default to charizard

            updateSharedPalettes();
            updateOrgPalette();
            mapComboBox.SelectedIndex = 0;
           */

        }
        /*
        void loadPicfromRom(string romPath, int offset)
        {

            string pkdecompPath = Path.Combine(
                System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
                , "pkdecomp.exe");
            if (!File.Exists(pkdecompPath))
                File.WriteAllBytes(pkdecompPath, Properties.Resources.pkdecomp);

            ProcessStartInfo procStartInfo = new ProcessStartInfo();

            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            procStartInfo.Arguments = "--seek " + offset.ToString() + " " + romPath;
            procStartInfo.FileName = pkdecompPath;

            pictureBox1.Visible = false;
            loadingLabel.Text = "Loading...";
            //MessageBox.Show(romPath);
            // wrap IDisposable into using (in order to release hProcess) 
            using (Process process = new Process())
            {
                process.StartInfo = procStartInfo;
                process.Start();

                // Add this: wait until process does its work
                process.WaitForExit();

                // and only then read the result
                //string result = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(result);
            }
            external2bpp = File.ReadAllBytes(Path.Combine(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "temp.2bpp"));
            pictureBox1.Visible = true;
            loadingLabel.Text = "";
        }
        void reload_images(bool columnOrder)
        {
            if (!trainerMode)
            {
                process_5656_2bpp(frontbppbuffer, Frontbmp, columnOrder, CurrentPaletteColors);
                process_5656_2bpp(backbppbuffer, Backbmp, columnOrder, CurrentPaletteColors);

            }
            else
            {
                process_5656_2bpp(external2bpp, Frontbmp, columnOrder, CurrentPaletteColors);
            }


            pictureBox1.Image = ResizeBitmap(Frontbmp, 168, 168);
            pictureBox2.Image = ResizeBitmap(Backbmp, 168, 168);
        }

        private Bitmap ResizeBitmap(Bitmap sourceBMP, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.DrawImage(sourceBMP, 0, 0, width, height);
            }
            return result;
        }

        void process_5656_2bpp(byte[] twobpp, Bitmap bmp, bool colMajor, Color[] palette)
        {

            byte[] rawtile = new byte[16];


            int column = 0;
            int row = 0;
            int curtile = 0;

            //Each tile is 8x8 pixels, a 56x56 image has 7x7 tiles

            for (row = 0; row < 7; row++)
            {
                //Buffer.BlockCopy(twobpp, row*16+16*column, rawtile, 0, 16);
                for (column = 0; column < 7; column++)
                {
                    Buffer.BlockCopy(twobpp, curtile * 16, rawtile, 0, 16);
                    if (colMajor)
                        drawtile(rawtile, bmp, column, row, CurrentPaletteColors);
                    else
                        drawtile(rawtile, bmp, row, column, CurrentPaletteColors);
                    curtile++;
                }
                column = 0;
            }
            //drawtile(twobpp, bmp, row, column, color0, color1, color2, color3);
        }
        void drawtile(byte[] rawtile, Bitmap bmp, int row, int column, Color[] palette)
        {
            int tilerow = 0;
            int tilecolumn = 0;
            int pixel = 0;
            byte[] pixelcolors = gettilepixelcolors(rawtile);
            foreach (byte i in pixelcolors)
            {
                //Debug.WriteLine (i.ToString("X"));
            }
            for (tilerow = 0; tilerow < 8; tilerow++)
            {
                for (tilecolumn = 0; tilecolumn < 8; tilecolumn++)
                {
                    //Get the color for each pixel
                    //get2bppcolor(twobpp[1], twobpp[0], tilecolumn;

                    bmp.SetPixel((column * 8) + tilecolumn, (row * 8) + tilerow, CurrentPaletteColors[pixelcolors[pixel]]);
                    //Debug.WriteLine(pixel.ToString());
                    //Debug.WriteLine("X:" + ((column*8) + tilecolumn).ToString() + " Y:" + ((row*8) + tilerow).ToString()) ;
                    pixel++;
                }
                tilecolumn = 0;
            }
        }

        //byte[] tile: 16 byte 2bpp tile array
        byte[] gettilepixelcolors(byte[] tile)
        {
            byte[] pixels = new byte[8 * 8];
            int j = 0;
            int i = 0;
            for (j = 0; j < 8; j++)
            {
                for (i = 0; i < 8; i++)
                {
                    byte hiBit = (byte)((tile[j * 2 + 1] >> (7 - i)) & 1);
                    byte loBit = (byte)((tile[j * 2] >> (7 - i)) & 1);
                    pixels[j * 8 + i] = (byte)((hiBit << 1) | loBit);
                }
            }
            return pixels;
        }

        void UpdateDrawingColors()
        {
            if (forcecolorCB.Checked)
            {
                //Force color 0 and 3 to white and black (the game does this for in-battle sprites)
                CurrentPaletteColors[0] = Color.FromArgb(255, 255, 239, 255);
                CurrentPaletteColors[3] = Color.FromArgb(255, 0, 0, 0);

            }
            else
            {
                CurrentPaletteColors[0] = palettePanel0.BackColor;
                CurrentPaletteColors[3] = palettePanel3.BackColor;
            }

            CurrentPaletteColors[1] = palettePanel1.BackColor;
            CurrentPaletteColors[2] = palettePanel2.BackColor;
        }

        void load_palette(int index)
        {
            sgbpalettes.SetcurrentPalette(index);

            palettePanel0.BackColor = sgbpalettes.toRGB(0);
            palettePanel1.BackColor = sgbpalettes.toRGB(1);
            palettePanel2.BackColor = sgbpalettes.toRGB(2);
            palettePanel3.BackColor = sgbpalettes.toRGB(3);

            UpdateDrawingColors();

            LoadPaletteColour(0);

            reload_images(SwapColumns2bpp);
        }

        void LoadPaletteColour(int index)
        {
            autoupdate = false;

            panelActiveColor.BackColor = sgbpalettes.toRGB(index);


            RGBupdownR.Value = panelActiveColor.BackColor.R;
            RGBupdownG.Value = panelActiveColor.BackColor.G;
            RGBupdownB.Value = panelActiveColor.BackColor.B;

            RGBHupdownR.Value = panelActiveColor.BackColor.R;
            RGBHupdownG.Value = panelActiveColor.BackColor.G;
            RGBHupdownB.Value = panelActiveColor.BackColor.B;

            trackBarR.Value = panelActiveColor.BackColor.R;
            trackBarG.Value = panelActiveColor.BackColor.G;
            trackBarB.Value = panelActiveColor.BackColor.B;


            RGB15updownR.Value = sgbpalettes.RGB15(index, 'r');
            RGB15updownG.Value = sgbpalettes.RGB15(index, 'g');
            RGB15updownB.Value = sgbpalettes.RGB15(index, 'b');

            textboxRGBHex.Text = panelActiveColor.BackColor.R.ToString("X2") + panelActiveColor.BackColor.G.ToString("X2") + panelActiveColor.BackColor.B.ToString("X2");
            textboxBGR15.Text = sgbpalettes.RGB15(index, 'z').ToString("X");

            autoupdate = true;

        }

        void ReloadLoadPannelColour(int panel)
        {
            autoupdate = false;

            if (panel == 0)
            {
                RGBupdownR.Value = palettePanel0.BackColor.R;
                RGBupdownG.Value = palettePanel0.BackColor.G;
                RGBupdownB.Value = palettePanel0.BackColor.B;

                RGBHupdownR.Value = palettePanel0.BackColor.R;
                RGBHupdownG.Value = palettePanel0.BackColor.G;
                RGBHupdownB.Value = palettePanel0.BackColor.B;

                trackBarR.Value = palettePanel0.BackColor.R;
                trackBarG.Value = palettePanel0.BackColor.G;
                trackBarB.Value = palettePanel0.BackColor.B;


                RGB15updownR.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel0.BackColor), 'r');
                RGB15updownG.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel0.BackColor), 'g');
                RGB15updownB.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel0.BackColor), 'b');

                panelActiveColor.BackColor = palettePanel0.BackColor;
            }
            if (panel == 1)
            {
                RGBupdownR.Value = palettePanel1.BackColor.R;
                RGBupdownG.Value = palettePanel1.BackColor.G;
                RGBupdownB.Value = palettePanel1.BackColor.B;

                RGBHupdownR.Value = palettePanel1.BackColor.R;
                RGBHupdownG.Value = palettePanel1.BackColor.G;
                RGBHupdownB.Value = palettePanel1.BackColor.B;

                trackBarR.Value = palettePanel1.BackColor.R;
                trackBarG.Value = palettePanel1.BackColor.G;
                trackBarB.Value = palettePanel1.BackColor.B;

                RGB15updownR.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel1.BackColor), 'r');
                RGB15updownG.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel1.BackColor), 'g');
                RGB15updownB.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel1.BackColor), 'b');

                panelActiveColor.BackColor = palettePanel1.BackColor;
            }
            if (panel == 2)
            {
                RGBupdownR.Value = palettePanel2.BackColor.R;
                RGBupdownG.Value = palettePanel2.BackColor.G;
                RGBupdownB.Value = palettePanel2.BackColor.B;

                RGBHupdownR.Value = palettePanel2.BackColor.R;
                RGBHupdownG.Value = palettePanel2.BackColor.G;
                RGBHupdownB.Value = palettePanel2.BackColor.B;

                trackBarR.Value = palettePanel2.BackColor.R;
                trackBarG.Value = palettePanel2.BackColor.G;
                trackBarB.Value = palettePanel2.BackColor.B;

                RGB15updownR.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel2.BackColor), 'r');
                RGB15updownG.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel2.BackColor), 'g');
                RGB15updownB.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel2.BackColor), 'b');

                panelActiveColor.BackColor = palettePanel2.BackColor;
            }
            if (panel == 3)
            {
                RGBupdownR.Value = palettePanel3.BackColor.R;
                RGBupdownG.Value = palettePanel3.BackColor.G;
                RGBupdownB.Value = palettePanel3.BackColor.B;

                RGBHupdownR.Value = palettePanel3.BackColor.R;
                RGBHupdownG.Value = palettePanel3.BackColor.G;
                RGBHupdownB.Value = palettePanel3.BackColor.B;

                trackBarR.Value = palettePanel3.BackColor.R;
                trackBarG.Value = palettePanel3.BackColor.G;
                trackBarB.Value = palettePanel3.BackColor.B;

                RGB15updownR.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel3.BackColor), 'r');
                RGB15updownG.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel3.BackColor), 'g');
                RGB15updownB.Value = RGB15GetComponent(ConvertColortoSFC(palettePanel3.BackColor), 'b');

                panelActiveColor.BackColor = palettePanel3.BackColor;
            }

            textboxRGBHex.Text = panelActiveColor.BackColor.R.ToString("X2") + panelActiveColor.BackColor.G.ToString("X2") + panelActiveColor.BackColor.B.ToString("X2");
            //textboxBGR15.Text = sgbpalettes.RGB15(index, 'z').ToString("X");

            autoupdate = true;

        }

        void UpdateRGBDecColor()
        {
            if (autoupdate)
            {
                panelActiveColor.BackColor = Color.FromArgb(255, (int)RGBupdownR.Value, (int)RGBupdownG.Value, (int)RGBupdownB.Value);

                RGBHupdownR.Value = panelActiveColor.BackColor.R;
                RGBHupdownG.Value = panelActiveColor.BackColor.G;
                RGBHupdownB.Value = panelActiveColor.BackColor.B;

                trackBarR.Value = panelActiveColor.BackColor.R;
                trackBarG.Value = panelActiveColor.BackColor.G;
                trackBarB.Value = panelActiveColor.BackColor.B;

                RGB15updownR.Value = ((ConvertColortoSFC(panelActiveColor.BackColor) & 0x7C00) >> 10);
                RGB15updownG.Value = ((ConvertColortoSFC(panelActiveColor.BackColor) & 0x03e0) >> 5);
                RGB15updownB.Value = (ConvertColortoSFC(panelActiveColor.BackColor) & 0x1f);

                textboxRGBHex.Text = panelActiveColor.BackColor.R.ToString("X2") + panelActiveColor.BackColor.G.ToString("X2") + panelActiveColor.BackColor.B.ToString("X2");
                int rgb15 = (int)RGB15tou16Color((int)RGB15updownR.Value, (int)RGB15updownG.Value, (int)RGB15updownB.Value);
                textboxBGR15.Text = rgb15.ToString("X");

            }
        }

        void UpdateRGBHexColor()
        {
            if (autoupdate)
            {
                panelActiveColor.BackColor = Color.FromArgb(255, (int)RGBHupdownR.Value, (int)RGBHupdownG.Value, (int)RGBHupdownB.Value);

                RGBupdownR.Value = panelActiveColor.BackColor.R;
                RGBupdownG.Value = panelActiveColor.BackColor.G;
                RGBupdownB.Value = panelActiveColor.BackColor.B;

                trackBarR.Value = panelActiveColor.BackColor.R;
                trackBarG.Value = panelActiveColor.BackColor.G;
                trackBarB.Value = panelActiveColor.BackColor.B;

                RGB15updownR.Value = ((ConvertColortoSFC(panelActiveColor.BackColor) & 0x7C00) >> 10);
                RGB15updownG.Value = ((ConvertColortoSFC(panelActiveColor.BackColor) & 0x03e0) >> 5);
                RGB15updownB.Value = (ConvertColortoSFC(panelActiveColor.BackColor) & 0x1f);

                textboxRGBHex.Text = panelActiveColor.BackColor.R.ToString("X2") + panelActiveColor.BackColor.G.ToString("X2") + panelActiveColor.BackColor.B.ToString("X2");
                int rgb15 = (int)RGB15tou16Color((int)RGB15updownR.Value, (int)RGB15updownG.Value, (int)RGB15updownB.Value);
                textboxBGR15.Text = rgb15.ToString("X");

            }
        }
        UInt16 RGB15tou16Color(int r, int g, int b)
        {
            int rgb15 = (r << 10) + (g << 5) + b;
            return (UInt16)rgb15;
        }
        Color RGB15toColor(int r, int g, int b)
        {
            int rgb15 = (r << 10) + (g << 5) + b;
            Color tempcolor = ConvertSFCtoColor((UInt16)rgb15);
            return tempcolor;
        }
        void UpdateRGB15Color()
        {
            if (autoupdate)
            {
                //Build rgb15 u16 color

                int rgb15 = (int)RGB15tou16Color((int)RGB15updownR.Value, (int)RGB15updownG.Value, (int)RGB15updownB.Value);
                panelActiveColor.BackColor = ConvertSFCtoColor((UInt16)rgb15);

                RGBupdownR.Value = panelActiveColor.BackColor.R;
                RGBupdownG.Value = panelActiveColor.BackColor.G;
                RGBupdownB.Value = panelActiveColor.BackColor.B;

                RGBHupdownR.Value = panelActiveColor.BackColor.R;
                RGBHupdownG.Value = panelActiveColor.BackColor.G;
                RGBHupdownB.Value = panelActiveColor.BackColor.B;

                trackBarR.Value = panelActiveColor.BackColor.R;
                trackBarG.Value = panelActiveColor.BackColor.G;
                trackBarB.Value = panelActiveColor.BackColor.B;

                textboxRGBHex.Text = panelActiveColor.BackColor.R.ToString("X2") + panelActiveColor.BackColor.G.ToString("X2") + panelActiveColor.BackColor.B.ToString("X2");
                textboxBGR15.Text = rgb15.ToString("X");

            }
        }

        void UpdateTrackBarColor()
        {
            if (autoupdate)
            {
                panelActiveColor.BackColor = Color.FromArgb(255, (int)trackBarR.Value, (int)trackBarG.Value, (int)trackBarB.Value);

                RGBupdownR.Value = panelActiveColor.BackColor.R;
                RGBupdownG.Value = panelActiveColor.BackColor.G;
                RGBupdownB.Value = panelActiveColor.BackColor.B;

                RGBHupdownR.Value = panelActiveColor.BackColor.R;
                RGBHupdownG.Value = panelActiveColor.BackColor.G;
                RGBHupdownB.Value = panelActiveColor.BackColor.B;

                RGB15updownR.Value = ((ConvertColortoSFC(panelActiveColor.BackColor) & 0x7C00) >> 10);
                RGB15updownG.Value = ((ConvertColortoSFC(panelActiveColor.BackColor) & 0x03e0) >> 5);
                RGB15updownB.Value = (ConvertColortoSFC(panelActiveColor.BackColor) & 0x1f);

                textboxRGBHex.Text = panelActiveColor.BackColor.R.ToString("X2") + panelActiveColor.BackColor.G.ToString("X2") + panelActiveColor.BackColor.B.ToString("X2");
                int rgb15 = RGB15tou16Color((int)RGB15updownR.Value, (int)RGB15updownG.Value, (int)RGB15updownB.Value);
                textboxBGR15.Text = rgb15.ToString("X");

            }
        }
        */
        public UInt16 RGB15GetComponent(UInt16 color15, char rgbcol)
        {
            if (rgbcol == 'r')
            {
                return (UInt16)((color15 & 0x7C00) >> 10);
            }
            if (rgbcol == 'g')
            {
                return (UInt16)((color15 & 0x03e0) >> 5);
            }
            if (rgbcol == 'b')
            {
                return (UInt16)(color15 & 0x1f);
            }

            return color15;

        }

        public static Color ConvertSFCtoColor(int bgr15)
        {
            var (r, g, b) = ConvertSFCtoRGB(bgr15);
            return Color.FromArgb(r, g, b);
        }

        // Convert 15 bit BGR value to 24 bit RGB value
        // Algorithm from: https://wiki.superfamicom.org/palettes
        public static (int r, int g, int b) ConvertSFCtoRGB(int bgr15)
        {
            if (bgr15 > 0x7FFF)
            {
                return (255, 255, 255);
            }
            int r = bgr15 % 32 << 3;
            int g = (bgr15 >> 5) % 32 << 3;
            int b = bgr15 >> 10 % 32 << 3;
            // adjust for higher precision
            r += r / 32;
            g += g / 32;
            b += b / 32;
            return (r, g, b);
        }
        public static UInt16 ConvertColortoSFC(Color c)
        {
            return ConvertRGBtoSFC(c.R, c.G, c.B);
        }

        public static UInt16 ConvertRGBtoSFC(int r, int g, int b)
        {
            int bgr15 = (b >> 3 << 10) + (g >> 3 << 5) + (r >> 3);
            return (UInt16)bgr15;
        }

        public class SGBPALETTEDATA
        {
            internal int Size = SGBPalette.SGBPalettes_totalSize;
            internal int _currentPalette = 0;

            public byte[] Data;
            public SGBPALETTEDATA(byte[] data = null)
            {
                Data = data ?? new byte[Size];
            }
            public int CurrentPalette
            {
                get
                {
                    return _currentPalette;
                }
            }
            public void SetcurrentPalette(int palette)
            {
                if (palette > 255)
                    _currentPalette = 255;
                else
                    _currentPalette = palette;
            }
            public Color toRGB(int colorIndex)
            {

                if (colorIndex > 3) colorIndex = 3;
                UInt16 color15 = RGB15(colorIndex, 'z');

                Color colorRGB = ConvertSFCtoColor(color15);

                return colorRGB;
            }

            public UInt16 RGB15(int colorIndex, char rgbcol)
            {
                if (colorIndex > 3) colorIndex = 3;
                UInt16 color15 = BitConverter.ToUInt16(Data, (SGBPaletteSize * _currentPalette) + (colorIndex * 2));

                if (rgbcol == 'r')
                {
                    return (UInt16)((color15 & 0x7C00) >> 10);
                }
                if (rgbcol == 'g')
                {
                    return (UInt16)((color15 & 0x03e0) >> 5);
                }
                if (rgbcol == 'b')
                {
                    return (UInt16)(color15 & 0x1f);
                }

                return color15;

            }

            public void storeSGBPalette(Color color0, Color color1, Color color2, Color color3)
            {
                UInt16 u16color = ConvertColortoSFC(color0);
                Buffer.BlockCopy(BitConverter.GetBytes(u16color), 0, Data, (SGBPaletteSize * _currentPalette), 2);
                u16color = ConvertColortoSFC(color1);
                Buffer.BlockCopy(BitConverter.GetBytes(u16color), 0, Data, (SGBPaletteSize * _currentPalette) + 2, 2);
                u16color = ConvertColortoSFC(color2);
                Buffer.BlockCopy(BitConverter.GetBytes(u16color), 0, Data, (SGBPaletteSize * _currentPalette) + 4, 2);
                u16color = ConvertColortoSFC(color3);
                Buffer.BlockCopy(BitConverter.GetBytes(u16color), 0, Data, (SGBPaletteSize * _currentPalette) + 6, 2);
            }
        }
        /*
        private void updateOrgPalette()
        {
            if (paletteIndex.Value < 37)
            {
                OrgPalLabel.Text = OrgPalettes[(int)paletteIndex.Value];
            }
            else
            {
                OrgPalLabel.Text = "";
            }
        }
        private void paletteIndex_ValueChanged(object sender, EventArgs e)
        {
            load_palette((int)paletteIndex.Value);
            palSlotHexLabel.Text = "0x" + ((int)paletteIndex.Value).ToString("X2");
            saveSlotNUD.Value = paletteIndex.Value;

            updateSharedPalettes();
            updateOrgPalette();

        }
        private void palettePanel0_MouseClick(object sender, MouseEventArgs e)
        {
            ReloadLoadPannelColour(0);
        }

        private void palettePanel1_MouseClick(object sender, MouseEventArgs e)
        {
            ReloadLoadPannelColour(1);
        }

        private void palettePanel2_MouseClick(object sender, MouseEventArgs e)
        {
            ReloadLoadPannelColour(2);
        }

        private void palettePanel3_MouseClick(object sender, MouseEventArgs e)
        {
            ReloadLoadPannelColour(3);
        }

        private void RGB15updown_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown activeNUD = (NumericUpDown)sender;
            if (!activeNUD.Focused)
                return;
            UpdateRGB15Color();
        }

        private void RGBHupdown_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown activeNUD = (NumericUpDown)sender;
            if (!activeNUD.Focused)
                return;
            UpdateRGBHexColor();
        }


        private void RGBupdown_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown activeNUD = (NumericUpDown)sender;
            if (!activeNUD.Focused)
                return;
            UpdateRGBDecColor();
        }

        private void trackBarChanged(object sender, EventArgs e)
        {
            if (!((TrackBar)sender).Focused)
                return;
            UpdateTrackBarColor();
        }

        private void setPal0_Click(object sender, EventArgs e)
        {
            palettePanel0.BackColor = panelActiveColor.BackColor;
            UpdateDrawingColors();
            reload_images(SwapColumns2bpp);
        }

        private void setPal1_Click(object sender, EventArgs e)
        {
            palettePanel1.BackColor = panelActiveColor.BackColor;
            UpdateDrawingColors();
            reload_images(SwapColumns2bpp);
        }

        private void setPal2_Click(object sender, EventArgs e)
        {
            palettePanel2.BackColor = panelActiveColor.BackColor;
            UpdateDrawingColors();
            reload_images(SwapColumns2bpp);
        }

        private void setPal3_Click(object sender, EventArgs e)
        {
            palettePanel3.BackColor = panelActiveColor.BackColor;
            UpdateDrawingColors();
            reload_images(SwapColumns2bpp);
        }

        private void savePaletteBut_Click(object sender, EventArgs e)
        {
            if (saveSlotNUD.Value < 37)
            {
                DialogResult dialogResult = MessageBox.Show("Warning!\nThis palette slot is one of the first 37 palettes used by the game for things other than sprites. Are you sure you want to overwrite the palette slot #" + saveSlotNUD.Value.ToString() + " ( " + OrgPalettes[(int)saveSlotNUD.Value] + " ) ?", "Warning!", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    sgbpalettes.SetcurrentPalette((int)saveSlotNUD.Value);
                    sgbpalettes.storeSGBPalette(palettePanel0.BackColor, palettePanel1.BackColor, palettePanel2.BackColor, palettePanel3.BackColor);
                    paletteIndex.Value = saveSlotNUD.Value;

                    MessageBox.Show("Palette Saved at index" + saveSlotNUD.Value.ToString() + "\nPalette is switched to that index.");
                }
                else if (dialogResult == DialogResult.No)
                {
                    MessageBox.Show("Palette was NOT saved.");
                }
            }
            else
            {
                sgbpalettes.SetcurrentPalette((int)saveSlotNUD.Value);
                sgbpalettes.storeSGBPalette(palettePanel0.BackColor, palettePanel1.BackColor, palettePanel2.BackColor, palettePanel3.BackColor);
                paletteIndex.Value = saveSlotNUD.Value;

                MessageBox.Show("Palette Saved at index" + saveSlotNUD.Value.ToString() + "\nPalette is switched to that index.");
            }

        }

        private void ReloadPaletteBut_Click(object sender, EventArgs e)
        {
            load_palette((int)paletteIndex.Value);
        }

        void loadTrainerPic(int index)
        {
            int offset = TrainerPicAndMoneyPointers;
            int trainerPicBank = 0x77; //All trainer sprites are in the same bank

            int playerM = 0x1ff470;
            int playerF = 0x1ff5a0;
            if (index == 0x30)//Special case for trainer backsprite
            {
                loadPicfromRom(BrownEditor.MainForm.loadedFilePath, playerM);
            }
            else if (index == 0x31)//Special case for female trainer backsprite
            {
                loadPicfromRom(BrownEditor.MainForm.loadedFilePath, playerF);
            }
            else
            {
                //Calculate offset
                offset += (5 * (index - 1));
                // MessageBox.Show("Pointer Offset: 0x" + offset.ToString("X") + " Pointer: 0x" + BitConverter.ToUInt16(tempbuffer, offset).ToString("X") +" Full Pointer 0x"+ BrownEditor.MainForm.ThreeByteToTwoByte(trainerPicBank, BitConverter.ToUInt16(tempbuffer, offset)).ToString("X"));
                loadPicfromRom(BrownEditor.MainForm.loadedFilePath, BrownEditor.MainForm.ThreeByteToTwoByte(trainerPicBank, BitConverter.ToUInt16(tempbuffer, offset)));
            }

            reload_images(SwapColumns2bpp);
        }
        void loadMonPic(int dexnum)
        {
            int BasesstatsOffset = 0xFC336;
            int basestat_size = 28;
            int FrontpicAddr = BasesstatsOffset + (dexnum * basestat_size) + 11;
            int BackpicAddr = BasesstatsOffset + (dexnum * basestat_size) + 13;
            int BankAddr = BasesstatsOffset + (dexnum * basestat_size) + 27;

            UInt16 pointer = BitConverter.ToUInt16(tempbuffer, FrontpicAddr);
            MonFrontSpriteAddress = BrownEditor.MainForm.ThreeByteToTwoByte(tempbuffer[BankAddr], pointer);
            Buffer.BlockCopy(tempbuffer, MonFrontSpriteAddress, frontbppbuffer, 0, 784);


            pointer = BitConverter.ToUInt16(tempbuffer, BackpicAddr);
            MonBackSpriteAddress = BrownEditor.MainForm.ThreeByteToTwoByte(tempbuffer[BankAddr], pointer);
            Buffer.BlockCopy(tempbuffer, MonBackSpriteAddress, backbppbuffer, 0, 784);

            reload_images(SwapColumns2bpp);

        }

        private int getTrainerPalette(int index)
        {
            if (index == 0x30) //Special case for player backsprite
            {
                return tempbuffer[trainerPalettesAddr + index - 1 + 0x10];
            }
            else if (index == 0x31) //Special case for female player backsprite
            {
                return tempbuffer[trainerPalettesAddr + index - 2 + 0x10];
            }
            else
            {
                return tempbuffer[trainerPalettesAddr + index];
            }

        }
        private void setTrainerPalette(int index)
        {
            if (index == 0x30) //Special case for player backsprite
            {
                tempbuffer[trainerPalettesAddr + index - 1 + 0x10] = (byte)monpaletteUD.Value;
            }
            else if (index == 0x31) //Special case for female player backsprite
            {
                tempbuffer[trainerPalettesAddr + index - 2 + 0x10] = (byte)monpaletteUD.Value;
            }
            else
            {
                tempbuffer[trainerPalettesAddr + index] = (byte)monpaletteUD.Value;
            }

        }
        private int getMonPalette(int index, bool shiny)
        {
            if (shiny)
                return tempbuffer[shinyPalettesAddr + index];
            return tempbuffer[normalPalettesAddr + index];

        }

        private void setMonPalette(int index, bool shiny)
        {
            if (shiny)
            {
                tempbuffer[shinyPalettesAddr + index] = (byte)monpaletteUD.Value;
            }
            else
            {
                tempbuffer[normalPalettesAddr + index] = (byte)monpaletteUD.Value;
            }

        }

        private void setMapPalette(int index)
        {
            tempbuffer[mapPalettesAddr + index] = (byte)paletteIndex.Value;
            curMapPal_label.Text = "Current Palette: " + ((int)tempbuffer[mapPalettesAddr + index]).ToString("D3") + " (0x" + ((int)tempbuffer[mapPalettesAddr + index]).ToString("X2") + ")";
        }

        private void pokemonComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (trainerMode)
            {
                loadTrainerPic(pkmTrnComboBox.SelectedIndex + 1);
                monpaletteUD.Value = getTrainerPalette(pkmTrnComboBox.SelectedIndex + 1);
            }
            else
            {
                loadMonPic(pkmTrnComboBox.SelectedIndex);
                monpaletteUD.Value = getMonPalette(pkmTrnComboBox.SelectedIndex + 1, shinyRadioBut.Checked);
            }
        }

        private void monpaletteUD_ValueChanged(object sender, EventArgs e)
        {
            paletteIndex.Value = monpaletteUD.Value;
            monPalIndexHex.Text = "0x" + ((int)monpaletteUD.Value).ToString("X2");
        }

        private void normalRadioBut_CheckedChanged(object sender, EventArgs e)
        {
            if (!trainerMode)
            {
                loadMonPic(pkmTrnComboBox.SelectedIndex);
                monpaletteUD.Value = getMonPalette(pkmTrnComboBox.SelectedIndex + 1, shinyRadioBut.Checked);
            }

        }

        private void shinyRadioBut_CheckedChanged(object sender, EventArgs e)
        {
            if (!trainerMode)
            {
                loadMonPic(pkmTrnComboBox.SelectedIndex);
                monpaletteUD.Value = getMonPalette(pkmTrnComboBox.SelectedIndex + 1, shinyRadioBut.Checked);
            }

        }

        private void saveMonPalBut_Click(object sender, EventArgs e)
        {
            if (trainerMode)
            {
                setTrainerPalette(pkmTrnComboBox.SelectedIndex + 1);
            }
            else
            {
                setMonPalette(pkmTrnComboBox.SelectedIndex + 1, shinyRadioBut.Checked);
            }
            updateSharedPalettes();
            MessageBox.Show("Palette ID saved.");
        }

        private void exitBut_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveExitBut_Click(object sender, EventArgs e)
        {
            //Mon palettes and picture...should be more more self contained instead of a whole rom copy...
            Buffer.BlockCopy(tempbuffer, 0, BrownEditor.MainForm.filebuffer, 0, 0x200000);
            Buffer.BlockCopy(sgbpalettes.Data, 0, BrownEditor.MainForm.filebuffer, SGBPaletteOffset, SGBPalettes_totalSize);
            MessageBox.Show("Saved.");
            this.Close();
        }


        void load2bpp(bool back, bool inject)
        {
            MessageBox.Show("2BPP image must be exactly 784 bytes.\nGenerate it from a 4 colour 56x56 PNG image file with the following command using rgbgfx:\n\n\trgbgfx -Z -o battlesprite.2bpp battlepsrite.png.\n\n(Trainer sprites don't use the -Z option in-game, but to see one here you'll need to)");
            string filepath = null;
            int filesize = FileIO.load_file(ref external2bpp, ref filepath, twobppfilter);
            if (filesize != 784)
            {
                MessageBox.Show("Wrong image size: " + filesize + " bytes");
            }
            else
            {
                if (back)
                    Buffer.BlockCopy(external2bpp, 0, backbppbuffer, 0, 784);
                else
                    Buffer.BlockCopy(external2bpp, 0, frontbppbuffer, 0, 784);

                if (inject)
                {
                    if (back)
                    {
                        Buffer.BlockCopy(external2bpp, 0, tempbuffer, MonBackSpriteAddress, 784);
                        MessageBox.Show("Back battle Sprite injected to ROM. Save to keep changes.");
                    }
                    else
                    {
                        Buffer.BlockCopy(external2bpp, 0, tempbuffer, MonFrontSpriteAddress, 784);
                        MessageBox.Show("Front battle Sprite injected to ROM. Save to keep changes.");
                    }
                }

                reload_images(SwapColumns2bpp);
            }
        }

        private void loadFront2bppBut_Click(object sender, EventArgs e)
        {
            load2bpp(false, false);
        }

        private void loadBack2bppBut_Click(object sender, EventArgs e)
        {
            load2bpp(true, false);
        }

        private void pkmModeRB_CheckedChanged(object sender, EventArgs e)
        {
            if (pkmModeRB.Checked) //Ensures update is only called once when toogling
                updateMode();
        }

        private void trainerModeRB_CheckedChanged(object sender, EventArgs e)
        {
            if (trainerModeRB.Checked) //Ensures update is only called once when toogling
                updateMode();
        }

        void updateMode()
        {
            trainerMode = trainerModeRB.Checked;

            pkmTrnComboBox.Items.Clear();

            if (trainerMode)
            {
                //Disable backpic
                pictureBox2.Visible = false;
                //Disable normal/shiny buttons
                normalRadioBut.Enabled = false;
                shinyRadioBut.Enabled = false;
                //Disable load buttons
                loadFront2bppBut.Enabled = false;
                loadBack2bppBut.Enabled = false;
                //Disable inject buttons
                loadinjectFrontBut.Enabled = false;
                LoadInjectBackBut.Enabled = false;
                //Disable export buttons
                exportBackBut.Enabled = false;
                exportBack2bppBut.Enabled = false;
                exportAllBut.Enabled = false;

                //Update label
                PkmTrnLabel.Text = "Trainer";

                pkmTrnComboBox.Items.AddRange(brownTrainers);
            }
            else //Pokémon mode
            {
                //Enable backpic
                pictureBox2.Visible = true;
                //Enable normal/shiny buttons
                normalRadioBut.Enabled = true;
                shinyRadioBut.Enabled = true;
                //Enable load buttons
                loadFront2bppBut.Enabled = true;
                loadBack2bppBut.Enabled = true;
                //Enable inject buttons
                loadinjectFrontBut.Enabled = true;
                LoadInjectBackBut.Enabled = true;
                //Enable export buttons
                exportBackBut.Enabled = true;
                exportBack2bppBut.Enabled = true;
                exportAllBut.Enabled = true;

                //Update label
                PkmTrnLabel.Text = "Pokémon";

                pkmTrnComboBox.Items.AddRange(brownSpecies);
            }

            pkmTrnComboBox.SelectedIndex = 0;
            normalRadioBut.Checked = true;
        }
        private string[] OrgPalettes =
        {
            "PAL_ROUTE",
            "PAL_GRAVEL",
            "PAL_SEASHORE, Startup Logos",
            "PAL_JAERU",
            "PAL_HAYWARD",
            "PAL_MERSON",
            "PAL_CASTRO",
            "PAL_MORAGA",
            "PAL_OWSAURI",
            "PAL_EAGULOU",
            "PAL_LEAGUE",
            "PAL_BOTAN",
            "PAL_TOWNMAP, Town Map",
            "PAL_LOGO1, Title Screen",
            "PAL_LOGO2, Title Screen",
            "PAL_0F, Unused(?)",
            "PAL_MEWMON, Default, Party menu,"+"\n            Title Screen, Trainer Card",
            "PAL_BLUEMON, Startup Logos",
            "PAL_REDMON, Trainer Card, Startup Logos",
            "PAL_CYANMON",
            "PAL_PURPLEMON, Title Screen",
            "PAL_BROWNMON, Used in Pokedex data"+"\n            screen",
            "PAL_GREENMON",
            "PAL_PINKMON",
            "PAL_YELLOWMON, Trainer Card",
            "PAL_GREYMON",
            "PAL_SLOTS1, GC Slots",
            "PAL_SLOTS2, GC Slots",
            "PAL_SLOTS3, GC Slots",
            "PAL_SLOTS4, GC Slots",
            "PAL_BLACK, Used at battle start",
            "PAL_GREENBAR, HP Bar",
            "PAL_YELLOWBAR, HP Bar",
            "PAL_REDBAR, HP Bar",
            "PAL_BADGE, Trainer Card",
            "PAL_CAVE",
            "PAL_GAMEFREAK, Startup Logos",
        };
        private string[] brownTrainers =
        {
            "Youngster",
            "Bug Catcher",
            "Lass",
            "Sailor",
            "Jr Trainer M",
            "Jr Trainer F",
            "Pokemaniac",
            "Super Nerd",
            "Hiker",
            "Biker",
            "Burglar",
            "Engineer",
            "Black Patrol",
            "Fisher",
            "Swimmer",
            "Cue Ball",
            "Gamber",
            "Beauty",
            "Psychic",
            "Rocker",
            "Juggler",
            "Tamer",
            "Bird Keeper",
            "Blackbelt",
            "Rival1",
            "Red Patrol",
            "Bugsy",
            "Scientist",
            "Giovanni",
            "Rocket",
            "Cooltrainer M",
            "Cooltrainer F",
            "Jared",
            "Karpman",
            "Lily",
            "Sparky",
            "Lois",
            "Koji",
            "Joe",
            "Sheral",
            "Gentleman",
            "Rival2",
            "Rival3",
            "Redd",
            "Channeler",
            "Agatha",
            "Blanch",
            "BrownBack",
            "BeigeBack",
        };
        private string[] brownSpecies =
        {
            "001 - Bulbasaur",
            "002 - Ivysaur",
            "003 - Venusaur",
            "004 - Charmander",
            "005 - Charmeleon",
            "006 - Charizard",
            "007 - Squirtle",
            "008 - Wartortle",
            "009 - Blastoise",
            "010 - Caterpie",
            "011 - Metapod",
            "012 - Butterfree",
            "013 - Weedle",
            "014 - Kakuna",
            "015 - Beedrill",
            "016 - Pidgey",
            "017 - Pidgeotto",
            "018 - Pidgeot",
            "019 - Rattata",
            "020 - Raticate",
            "021 - Spearow",
            "022 - Fearow",
            "023 - Ekans",
            "024 - Arbok",
            "025 - Pikachu",
            "026 - Raichu",
            "027 - Sandshrew",
            "028 - Sandslash",
            "029 - NidoranF",
            "030 - Nidorina",
            "031 - Nidoqueen",
            "032 - NidoranM",
            "033 - Nidorino",
            "034 - Nidoking",
            "035 - Clefairy",
            "036 - Clefable",
            "037 - Vulpix",
            "038 - Ninetales",
            "039 - Jigglypuff",
            "040 - Wigglytuff",
            "041 - Zubat",
            "042 - Golbat",
            "043 - Oddish",
            "044 - Gloom",
            "045 - Vileplume",
            "046 - Paras",
            "047 - Parasect",
            "048 - Venonat",
            "049 - Venomoth",
            "050 - Diglett",
            "051 - Dugtrio",
            "052 - Meowth",
            "053 - Persian",
            "054 - Psyduck",
            "055 - Golduck",
            "056 - Mankey",
            "057 - Primeape",
            "058 - Growlithe",
            "059 - Arcanine",
            "060 - Poliwag",
            "061 - Poliwhirl",
            "062 - Poliwrath",
            "063 - Abra",
            "064 - Kadabra",
            "065 - Alakazam",
            "066 - Machop",
            "067 - Machoke",
            "068 - Machamp",
            "069 - Bellsprout",
            "070 - Weepinbell",
            "071 - Victreebel",
            "072 - Tentacool",
            "073 - Tentacruel",
            "074 - Geodude",
            "075 - Graveler",
            "076 - Golem",
            "077 - Ponyta",
            "078 - Rapidash",
            "079 - Slowpoke",
            "080 - Slowbro",
            "081 - Magnemite",
            "082 - Magneton",
            "083 - Farfetch'd",
            "084 - Doduo",
            "085 - Dodrio",
            "086 - Seel",
            "087 - Dewgong",
            "088 - Grimer",
            "089 - Muk",
            "090 - Shellder",
            "091 - Cloyster",
            "092 - Gastly",
            "093 - Haunter",
            "094 - Gengar",
            "095 - Onix",
            "096 - Drowzee",
            "097 - Hypno",
            "098 - Krabby",
            "099 - Kingler",
            "100 - Voltorb",
            "101 - Electrode",
            "102 - Exeggcute",
            "103 - Exeggutor",
            "104 - Cubone",
            "105 - Marowak",
            "106 - Hitmonlee",
            "107 - Hitmonchan",
            "108 - Lickitung",
            "109 - Koffing",
            "110 - Weezing",
            "111 - Rhyhorn",
            "112 - Rhydon",
            "113 - Chansey",
            "114 - Tangela",
            "115 - Kangaskhan",
            "116 - Horsea",
            "117 - Seadra",
            "118 - Goldeen",
            "119 - Seaking",
            "120 - Staryu",
            "121 - Starmie",
            "122 - Mr. Mime",
            "123 - Scyther",
            "124 - Jynx",
            "125 - Electabuzz",
            "126 - Magmar",
            "127 - Pinsir",
            "128 - Tauros",
            "129 - Magikarp",
            "130 - Gyarados",
            "131 - Lapras",
            "132 - Ditto",
            "133 - Eevee",
            "134 - Vaporeon",
            "135 - Jolteon",
            "136 - Flareon",
            "137 - Porygon",
            "138 - Omanyte",
            "139 - Omastar",
            "140 - Kabuto",
            "141 - Kabutops",
            "142 - Aerodactyl",
            "143 - Snorlax",
            "144 - Articuno",
            "145 - Zapdos",
            "146 - Moltres",
            "147 - Dratini",
            "148 - Dragonair",
            "149 - Dragonite",
            "150 - Mewtwo",
            "151 - Mew",
            "152 - Chikorita",
            "153 - Bayleef",
            "154 - Meganium",
            "155 - Cyndaquil",
            "156 - Quilava",
            "157 - Typhlosion",
            "158 - Totodile",
            "159 - Croconaw",
            "160 - Feraligatr",
            "161 - Houndour",
            "162 - Houndoom",
            "163 - Heracross",
            "164 - Yanma",
            "165 - Yanmega",
            "166 - Spinarak",
            "167 - Ariados",
            "168 - Chinchou",
            "169 - Lanturn",
            "170 - Swinub",
            "171 - Piloswine",
            "172 - Mamoswine",
            "173 - Natu",
            "174 - Xatu",
            "175 - Mareep",
            "176 - Flaaffy",
            "177 - Ampharos",
            "178 - Marill",
            "179 - Azumarill",
            "180 - Murkrow",
            "181 - Honchkrow",
            "182 - Larvitar",
            "183 - Pupitar",
            "184 - Tyranitar",
            "185 - Phanpy",
            "186 - Donphan",
            "187 - Wooper",
            "188 - Quagsire",
            "189 - Togepi",
            "190 - Togetic",
            "191 - Togekiss",
            "192 - Gligar",
            "193 - Gliscor",
            "194 - Sneasel",
            "195 - Weavile",
            "196 - Tyrogue",
            "197 - Hitmontop",
            "198 - Misdreavus",
            "199 - Mismagius",
            "200 - Espeon",
            "201 - Umbreon",
            "202 - Leafeon",
            "203 - Glaceon",
            "204 - Magnezone",
            "205 - Electivire",
            "206 - Magmortar",
            "207 - Porygon2",
            "208 - Porygon-Z",
            "209 - Tangrowth",
            "210 - Scizor",
            "211 - Steelix",
            "212 - Slowking",
            "213 - Kingdra",
            "214 - Rhyperior",
            "215 - Blissey",
            "216 - Crobat",
            "217 - Politoed",
            "218 - Raikou",
            "219 - Entei",
            "220 - Suicune",
            "221 - Lugia",
            "222 - Ho-Oh",
            "223 - Cranidos",
            "224 - Rampardos",
            "225 - Sylveon",
            "226 - Annihilape",
            "227 - G.Weezing",
            "228 - Lickilicky",
            "229 - Noibat",
            "230 - Noivern",
            "231 - ?????",
            "232 - ?????",
            "233 - ?????",
            "234 - ?????",
            "235 - ?????",
            "236 - ?????",
            "237 - ?????",
            "238 - ?????",
            "239 - GlitchPhancero",
            "240 - ?????",
            "241 - ?????",
            "242 - ?????",
            "243 - ?????",
            "244 - ?????",
            "245 - ?????",
            "246 - ?????",
            "247 - ?????",
            "248 - ?????",
            "249 - ?????",
            "250 - ?????",
            "251 - ?????",
            "252 - Phancero",
            "253 - ?????",
            "254 - ?????",
            "255 - ?????",
        };

        private void exportFrontBut_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = @"PNG|*.png" })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image.Save(saveFileDialog.FileName);
                }
            }
        }

        private void exportBackBut_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = @"PNG|*.png" })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox2.Image.Save(saveFileDialog.FileName);
                }
            }
        }

        private void loadinjectFrontBut_Click(object sender, EventArgs e)
        {
            load2bpp(false, true);
        }

        private void LoadInjectBackBut_Click(object sender, EventArgs e)
        {
            load2bpp(true, true);
        }

        private void exportFront2bppBut_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = @"2bpp|*.2bpp" })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, frontbppbuffer);
                }
            }
        }

        private void exportBack2bppBut_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = @"2bpp|*.2bpp" })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, backbppbuffer);
                }
            }
        }

        private void exportAllBut_Click(object sender, EventArgs e)
        {
            int tempindex = pkmTrnComboBox.SelectedIndex;
            string exportdir;
            string exportname;
            exportingLabel.Text = "Exporting...";
            //Get a directory to store the export
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            DialogResult res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {

                exportdir = dlg.SelectedPath;
                int i = 0;
                for (i = 0; i < 255; i++)
                {
                    loadMonPic(i);
                    monpaletteUD.Value = getMonPalette(i + 1, shinyRadioBut.Checked);
                    //Build name
                    exportname = (i + 1).ToString("D3") + "_F.2bpp";
                    File.WriteAllBytes(Path.Combine(exportdir, exportname), frontbppbuffer);
                    exportname = (i + 1).ToString("D3") + "_B.2bpp";
                    File.WriteAllBytes(Path.Combine(exportdir, exportname), backbppbuffer);
                    exportname = (i + 1).ToString("D3") + "_F.png";
                    pictureBox1.Image.Save(Path.Combine(exportdir, exportname));
                    exportname = (i + 1).ToString("D3") + "_B.png";
                    pictureBox2.Image.Save(Path.Combine(exportdir, exportname));
                }
                exportingLabel.Text = "";
                MessageBox.Show("Export complete.");
                pkmTrnComboBox.SelectedIndex = tempindex;

            }
        }

        private void loadColorToEditor(Color color)
        {
            autoupdate = false;

            panelActiveColor.BackColor = color;


            RGBupdownR.Value = panelActiveColor.BackColor.R;
            RGBupdownG.Value = panelActiveColor.BackColor.G;
            RGBupdownB.Value = panelActiveColor.BackColor.B;

            RGBHupdownR.Value = panelActiveColor.BackColor.R;
            RGBHupdownG.Value = panelActiveColor.BackColor.G;
            RGBHupdownB.Value = panelActiveColor.BackColor.B;

            trackBarR.Value = panelActiveColor.BackColor.R;
            trackBarG.Value = panelActiveColor.BackColor.G;
            trackBarB.Value = panelActiveColor.BackColor.B;


            RGB15updownR.Value = ((ConvertColortoSFC(panelActiveColor.BackColor) & 0x7C00) >> 10);
            RGB15updownG.Value = ((ConvertColortoSFC(panelActiveColor.BackColor) & 0x03e0) >> 5);
            RGB15updownB.Value = (ConvertColortoSFC(panelActiveColor.BackColor) & 0x1f);

            textboxRGBHex.Text = panelActiveColor.BackColor.R.ToString("X2") + panelActiveColor.BackColor.G.ToString("X2") + panelActiveColor.BackColor.B.ToString("X2");
            textboxBGR15.Text = ConvertColortoSFC(panelActiveColor.BackColor).ToString("X");

            autoupdate = true;
        }

        private void storedColor0_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor0.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor0.BackColor);
            }
        }

        private void storedColor1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor1.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor1.BackColor);
            }
        }

        private void storedColor2_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor2.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor2.BackColor);
            }
        }

        private void storedColor3_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor3.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor3.BackColor);
            }
        }

        private void storedColor4_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor4.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor4.BackColor);
            }
        }

        private void storedColor5_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor5.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor5.BackColor);
            }
        }

        private void storedColor6_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor6.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor6.BackColor);
            }
        }

        private void storedColor7_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor7.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor7.BackColor);
            }
        }

        private void storedColor8_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor8.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor8.BackColor);
            }
        }

        private void storedColor9_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor9.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor9.BackColor);
            }
        }

        private void storedColor10_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor10.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor10.BackColor);
            }
        }

        private void storedColor11_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                storedColor11.BackColor = panelActiveColor.BackColor;
            }
            else
            {
                loadColorToEditor(storedColor11.BackColor);
            }
        }

        private void forcecolorCB_CheckedChanged(object sender, EventArgs e)
        {

            UpdateDrawingColors();
            reload_images(SwapColumns2bpp);
        }

        int FindMonsThatUsePalette (int paletteIndex, int offset)
        {
            string tempstring = string.Empty;

            int duplicates = 0;
            int i = 0;
            for (i = 1; i < 255; i++) //Index 0 is unused
            {
                // Mon Palettes are stored in pokedex order
                if ( (tempbuffer[offset + i]&0xff) == paletteIndex)
                {
                        tempstring = tempstring + brownSpecies[i - 1] + "\n";
                        duplicates++;

                }
            }
            monSharedPalettes = tempstring;
            return duplicates;
            
        }
        int FindShinyThatUsePalette(int paletteIndex, int offset)
        {
            string tempstring = string.Empty;
            int duplicates = 0;
            int i = 0;
            for (i = 1; i < 255; i++) //Index 0 is unused
            {
                // Mon Palettes are stored in pokedex order
                if ((tempbuffer[offset + i] & 0xff) == paletteIndex)
                {
                    tempstring = tempstring + brownSpecies[i - 1] + "\n";
                    duplicates++;
                }
            }
            shinySharedPalettes = tempstring;
            return duplicates;

        }

        int FindMapsThatUsePalette(int paletteIndex, int offset)
        {
            string tempstring = string.Empty;
            int duplicates = 0;
            int i = 0;
            for (i = 0; i < 255; i++)
            {
                int curPal = (tempbuffer[offset + i] & 0xff);
                // Mon Palettes are stored in pokedex order
                if ((curPal & 0xff) == paletteIndex)
                {
                    int curMap = i;
                    //map index (tempbuffer[offset + j] & 0xff)
                    if (curMap < 248)
                    {
                        tempstring = tempstring + BrownEditor.editor.evomoves.brownMaps[(curMap & 0xff)] + "\n";
                        duplicates++;
                    }
                }
            }
            mapSharedPalettes = tempstring;
            return duplicates;

        }

        int FindTrainersThatUsePalette(int paletteIndex, int offset)
        {
            string tempstring = string.Empty;
            int duplicates = 0;
            int i = 0;
            for (i = 0; i < 64; i++)
            {
                // Mon Palettes are stored in pokedex order
                if ((tempbuffer[offset + i] & 0xff) == paletteIndex)
                {
                    if (i == 63) //exception for brown backsprite, there are only 47 trainer entries
                    {
                        tempstring = tempstring + brownTrainers[47] + "/" + brownTrainers[48] + "\n";
                        duplicates++;
                    }
                    else if (i== 0)
                    {
                        tempstring = tempstring + "Error?" + "\n";
                    }
                    else if (i < 48)
                    {
                        tempstring = tempstring + brownTrainers[i-1] + "\n";
                        duplicates++;
                    }

                }
            }
            trainerSharedPalettes = tempstring;
            return duplicates;

        }

        void updateSharedPalettes()
        {
            //Update Shared map palettes
            int buttoncolor = FindMapsThatUsePalette((int)paletteIndex.Value, mapPalettesAddr);
            if (buttoncolor == 0)
            {
                PalUseMapBut.BackColor = Color.LightGray;
                PalUseMapBut.Enabled = false;
            }
            else PalUseMapBut.Enabled = true;
            if (buttoncolor > 1) PalUseMapBut.BackColor = Color.PaleVioletRed;
            if (buttoncolor == 1) PalUseMapBut.BackColor = Color.LightGreen;
            PalUseMapBut.Text = "Maps: " + buttoncolor.ToString();

            //Update Shared mon palettes
            buttoncolor = FindMonsThatUsePalette((int)paletteIndex.Value, normalPalettesAddr);
            if (buttoncolor == 0)
            {
                PalUseMonBut.BackColor = Color.LightGray;
                PalUseMonBut.Enabled = false;
            }
            else PalUseMonBut.Enabled = true;
            if (buttoncolor > 1) PalUseMonBut.BackColor = Color.PaleVioletRed;

            if (buttoncolor == 1) PalUseMonBut.BackColor = Color.LightGreen;
            PalUseMonBut.Text = "Mons: " + buttoncolor.ToString();

            //Update Shared shiny mon palettes
            buttoncolor = FindShinyThatUsePalette((int)paletteIndex.Value, shinyPalettesAddr);
            if (buttoncolor == 0)
            {
                PalUseShinyBut.BackColor = Color.LightGray;
                PalUseShinyBut.Enabled = false;
            }
            else PalUseShinyBut.Enabled = true;
            if (buttoncolor > 1) PalUseShinyBut.BackColor = Color.PaleVioletRed;
            if (buttoncolor == 1) PalUseShinyBut.BackColor = Color.LightGreen;
            PalUseShinyBut.Text = "Shinies: " + buttoncolor.ToString();

            //Update Shared Traner palettes
            buttoncolor = FindTrainersThatUsePalette ((int)paletteIndex.Value, trainerPalettesAddr);
            if (buttoncolor == 0) {
                PalUseTrainerBut.BackColor = Color.LightGray;
                PalUseTrainerBut.Enabled = false;
            } else PalUseTrainerBut.Enabled = true;
            if (buttoncolor > 1) PalUseTrainerBut.BackColor = Color.PaleVioletRed;
           
            if (buttoncolor == 1) PalUseTrainerBut.BackColor = Color.LightGreen;
            PalUseTrainerBut.Text = "Trainer: " + buttoncolor.ToString();
        }

        private void PalUseTrainerBut_Click(object sender, EventArgs e)
        {
            MessageBox.Show(trainerSharedPalettes);
        }

        private void PalUseMapBut_Click(object sender, EventArgs e)
        {
            //FlexibleMessageBox.Show(mapSharedPalettes);
            MessageBox.Show(mapSharedPalettes);
        }

        private void PalUseMonBut_Click(object sender, EventArgs e)
        {
            MessageBox.Show(monSharedPalettes);
        }

        private void PalUseShinyBut_Click(object sender, EventArgs e)
        {
            MessageBox.Show(shinySharedPalettes);
        }

        private void mapComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            curMapPal_label.Text = "Current Palette: " + ((int)tempbuffer[mapPalettesAddr + (int)mapComboBox.SelectedIndex]).ToString("D3") + " (0x" + ((int)tempbuffer[mapPalettesAddr + (int)mapComboBox.SelectedIndex]).ToString("X2") + ")";
        }

        private void saveMapPal_but_Click(object sender, EventArgs e)
        {
            setMapPalette((int)mapComboBox.SelectedIndex);
        }
        */
    }
}
