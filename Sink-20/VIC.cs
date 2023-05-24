using System;
using System.Drawing;
using System.Windows.Forms;

using NAudio.Wave;

namespace Sink_20
{
   class VIC : Chip
   {
      // This is PAL speed! - will be called each scan line...
      public const int CLOCK_FREQUENCY = 17734472/16; // PAL

      private const int CYCLES_PER_LINE = 71;
      private const int PIXELS_PER_CYCLE = 4;
      private const int RASTER_MAX_VALUE = 312;
      private const int CYCLES_PER_FRAME = RASTER_MAX_VALUE*CYCLES_PER_LINE;

      // This is the vicScreen width and height used...
      private const int SC_WIDTH = PIXELS_PER_CYCLE*CYCLES_PER_LINE;
      // neatly, 3*3*71 = 639, so 2*240 = 480 gives a 4x3 vicScreen
      private const int SC_HEIGHT = 272;
      private const int RASTER_FIRST_VISIBLE = 36;
      public const int IMG_HALFWIDTH = 362;
      public const int IMG_HALFHEIGHT = 272;
      public const int IMG_TOTALWIDTH = 2*IMG_HALFWIDTH;
      public const int IMG_TOTALHEIGHT = 2*IMG_HALFHEIGHT;

      // 20000 us per scan
      private int actualScanTime = CLOCK_FREQUENCY/50;

      public int[] skColorPalette = new int[]
      {
         0x000000, // 0 Black
         0xffffff, // 1 White
         0x782922, // 2 Red
         0x87D6DD, // 3 Cyan
         0xAA5FB6, // 4 Purple
         0x55A049, // 5 Green
         0x40318D, // 6 Blue
         0xBFCE72, // 7 Yellow

         0xAA7449, // 8 Brown
         0xEAB489, // 9 Pink
         0xB86962, // 10 Lt.Red
         0xC7FFFF, // 11 Lt.Cyan
         0xEA9FF6, // 12 Lt.Purple
         0x94E089, // 13 Lt.Green
         0x8071CC, // 14 Lt.Blue
         0xFFFFB2 // 15 Lt.Yellow
      };

      private Memory mMemory;
      private PictureBox mPictureBox;
      private byte[] mImage;
      private SoundChannels mAudio;

      public void Init(CPU _cpu, Memory _mem, PictureBox _box)
      {
         mCpu = _cpu;
         mMemory = _mem;
         mPictureBox = _box;
         mPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
         mPictureBox.Image = new Bitmap(SC_WIDTH, SC_HEIGHT);

         mImage = new byte[3 * SC_WIDTH * SC_HEIGHT];

         for (int i = 0; i < 16; ++i)
         {
            chipMem[0 + i] = skVicContents[i];
         }

         mAudio = new SoundChannels();
      }

      public int getActualScanRate()
      {
         // This should be calculated... if it is too slow it will be
         // shown here
         return CLOCK_FREQUENCY/actualScanTime;
      }

      void restoreKey(bool down)
      {
         if (down)
         {
            setNMI(KEYBOARD_NMI);
         }
         else
         {
            clearNMI(KEYBOARD_NMI);
         }
      }
  
      private int mMemoryExpansion = 0;
  
      public void setMemoryExpansion(int kilobytes)
      {
         mMemoryExpansion = kilobytes;
      }

      public override bool isAddrInRange(int address)
      {
         return (address & 0xff00) == 0x9000;
      }

      public int[] chipMem = new int[16];

      public override int performRead(int address, long cycles)
      {
         // map any 90?X to X
         address &= 0x0f;
         return chipMem[address];
      }

      public override void performWrite(int address, int data, long cycles)
      {
         // map any 90XX to 900X
         address &= 0x0f;

         switch (address)
         {
            case 4:
            case 6:
            case 7:
            case 8:
            case 9:
               // read only
               break;
            case 3:
               // don't write over the upper bit (part of the raster value)
               chipMem[address] &= 0x80;
               chipMem[address] |= (data & 0x7f);
               break;
            default:
               chipMem[address] = data;
               break;
         }
      }

      // -------------------------------------------------------------------
      // Screen rendering!
      // -------------------------------------------------------------------

      long lastCycles = 0;

      public void Update(long cycles)
      {
         int frameCycles = (int)(lastCycles % CYCLES_PER_FRAME);
         int rasterValue = frameCycles / CYCLES_PER_LINE;

         if (rasterValue >= RASTER_FIRST_VISIBLE && rasterValue < RASTER_FIRST_VISIBLE + SC_HEIGHT)
         {
            int lineCycle = frameCycles - rasterValue * CYCLES_PER_LINE;
            int linePixel = PIXELS_PER_CYCLE * lineCycle;

            int screenOriginX = chipMem[0] & 0x7f;
            int screenOriginY = chipMem[1];

            // the defaults place the vicScreen at 54, 76
            int renderOriginX = 54 + 4 * (screenOriginX - skVicContents[0]);
            int renderOriginY = 76 + 2 * (screenOriginY - skVicContents[1]);

            int numberOfColumns = chipMem[2] & 0x7f;
            int numberOfRows = (chipMem[3] & 0x7e) >> 1;

            int borderColorIndex = chipMem[0xf] & 0x07;
            int borderColor = skColorPalette[borderColorIndex];

            int backgroundColorIndex = chipMem[0xf] >> 4;
            int backgroundColor = skColorPalette[backgroundColorIndex];

            int auxiliaryColorIndex = chipMem[0xe] >> 4;
            int auxiliaryColor = skColorPalette[auxiliaryColorIndex];

            int screenMemoryLocation = 4 * (chipMem[2] & 0x80) + 64 * (chipMem[5] & 0x70);
            int colorMemoryLocation = 0x9400 + 4 * (chipMem[2] & 0x80);
            int characterMemoryLocationIndex = chipMem[5] & 0x0f;
            bool reverseMode = ((chipMem[0xf] & 0x08) == 0);
            if (reverseMode)
            {
               characterMemoryLocationIndex += 0x01;
               characterMemoryLocationIndex &= 0x0f;
            }
            bool doubleHeight = (chipMem[3] & 1) > 0;
            int characterHeightLog2 = (doubleHeight) ? 4 : 3;
            int characterHeight = 1 << characterHeightLog2;
            int shiftForCharacterBlock = 10 - characterHeightLog2;
            int characterBlockMask = (1 << shiftForCharacterBlock) - 1;

            // map rasterValue and linePixel to vicScreen memory
            int screenMemoryY = rasterValue - renderOriginY;
            int screenMemoryRow = screenMemoryY >> characterHeightLog2;
            int screenMemoryRowOffset = screenMemoryY & (characterHeight - 1);
            int screenMemoryX = linePixel - renderOriginX;

            int pixy = rasterValue - RASTER_FIRST_VISIBLE;
            if (pixy < SC_HEIGHT)
            {
               int numPixels = (int)(PIXELS_PER_CYCLE * (cycles - lastCycles));
               for (int x = screenMemoryX - numPixels; x <= screenMemoryX; ++x)
               {
                  int pixAddress = x + renderOriginX;
                  if (pixAddress >= 0 && pixAddress < SC_WIDTH)
                  {
                     int column = x >> 3;
                     int columnOffset = x & 0x7;

                     int color = borderColor;

                     if (screenMemoryY >= 0 && screenMemoryRow < numberOfRows
                         && x >= 0 && column < numberOfColumns)
                     {
                        int characterOffset = screenMemoryRow * numberOfColumns + column;
                        int characterIndex = mMemory.Peek(screenMemoryLocation + characterOffset);
                        int characterColorPlusHi = mMemory.Peek(colorMemoryLocation + characterOffset) & 0x0f;
                        int characterColor = characterColorPlusHi & 0x07;
                        bool hiColor = (characterColorPlusHi & 0x08) > 0;
                        int characterMemoryOffset = characterHeight * (characterIndex & characterBlockMask) + screenMemoryRowOffset;
                        int characterMemoryBlock = (characterMemoryLocationIndex + (characterIndex >> shiftForCharacterBlock)) & 0x0f;
                        int characterDef = mMemory.Peek(skCharacterMemoryLocations[characterMemoryBlock] + characterMemoryOffset);
                        if (hiColor)
                        {
                           int colorIndex = (characterDef >> (2 * (3 - columnOffset / 2))) & 3;
                           switch (colorIndex)
                           {
                              case 0:
                                 color = backgroundColor;
                                 break;
                              case 1:
                                 color = borderColor;
                                 break;
                              case 2:
                                 color = skColorPalette[characterColor];
                                 break;
                              case 3:
                                 color = auxiliaryColor;
                                 break;
                           }
                        }
                        else
                        {
                           bool bitIsSet = (characterDef & (1 << (7 - columnOffset))) > 0;
                           if (bitIsSet)
                           {
                              color = skColorPalette[characterColor];
                           }
                           else
                           {
                              color = backgroundColor;
                           }
                        }
                     }
                     int a = 3 * (pixy * SC_WIDTH + pixAddress);
                     mImage[a + 2] = (byte)(color >> 16);
                     mImage[a + 1] = (byte)((color & 0xff00) >> 8);
                     mImage[a + 0] = (byte)(color & 0xff);
                  }
               }
            }
         }

         chipMem[4] = (rasterValue >> 1) & 0xff;
         chipMem[3] &= 0x7f;
         chipMem[3] |= (rasterValue << 7) & 0x80;

         if (rasterValue > RASTER_FIRST_VISIBLE + SC_HEIGHT)
         {
            if (drawAgain)
            {
               // repaint
               Bitmap b = mPictureBox.Image as Bitmap;
               System.Drawing.Imaging.BitmapData bd = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
               // Copy the RGB values back to the bitmap
               System.Runtime.InteropServices.Marshal.Copy(mImage, 0, bd.Scan0, 3 * b.Width * b.Height);
               b.UnlockBits(bd);

               float pw = mPictureBox.Parent.Size.Width;
               float ph = mPictureBox.Parent.Size.Height;

               float aspect = 1.5f;

               float sw = aspect * b.Width;
               float sh = b.Height;

               float zoomW = pw / sw;
               float zoomH = ph / sh;
               float zoom = Math.Min( zoomW, zoomH );

               sw *= zoom;
               sh *= zoom;

               mPictureBox.Left = (int) ( ( pw - sw ) / 2 );
               mPictureBox.Top = (int) ( ph - sh ) / 2;
               mPictureBox.Width = (int) sw;
               mPictureBox.Height = (int) sh;

               mPictureBox.Invalidate();
               drawAgain = false;
            }
         }
         else
         {
            drawAgain = true;
         }

         lastCycles = cycles;

         mAudio.Update( chipMem );
      }

      bool drawAgain;
  
      private static readonly int[] skVicContents =
      {
         12, 38, 150, 46, 0, 240, 0, 0, 255, 255, 0, 0, 0, 0, 0, 27
      };
  
      private static readonly int[] skCharacterMemoryLocations =
      {
         0x8000, 0x8400, 0x8800, 0x8c00, 0x9000, 0x9400, 0x9800, 0x9c00,
         0x0000, 0x0400, 0x0800, 0x0c00, 0x1000, 0x1400, 0x1800, 0x1c00
      };
   }
}
