using System;
using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;
using System.Collections.Generic;

namespace Sink_20
{
   public partial class MemoryConfiguration : DockContent
   {
      Sink20 mSink;


      string[] expansionAddresses = new string[] { "0400", "2000", "4000", "6000", "A000" };
      string[] expansionSizes = new string[] { "3K", "8K", "8K", "8K", "8K" };

      Bitmap b = new Bitmap( 11 * 8, 18 * 8 );

      public MemoryConfiguration( Sink20 sink )
      {
         InitializeComponent();

         mSink = sink;

         UpdateImage();
      }

      void DrawBox( Graphics g, int x, int y, int w, int h )
      {
         // corners
         g.DrawImage( VicFont.sChars[112], 8 * x, 8 * y );
         g.DrawImage( VicFont.sChars[110], 8 * ( x + w - 1 ), 8 * y );
         g.DrawImage( VicFont.sChars[109], 8 * x, 8 * ( y + h - 1 ) );
         g.DrawImage( VicFont.sChars[125], 8 * ( x + w - 1 ), 8 * ( y + h - 1 ) );

         for ( int i = 1; i < w - 1; ++i )
         {
            // horizontal lines
            g.DrawImage( VicFont.sChars[64], 8 * ( x + i ), 8 * y );
            g.DrawImage( VicFont.sChars[64], 8 * ( x + i ), 8 * ( y + h - 1 ) );
         }
         for ( int i = 1; i < h - 1; ++i )
         {
            // vertical lines
            g.DrawImage( VicFont.sChars[93], 8 * x, 8 * ( y + i ) );
            g.DrawImage( VicFont.sChars[93], 8 * ( x + w - 1 ), 8 * ( y + i ) );
         }
      }

      void UpdateImage()
      {
         using ( Graphics g = Graphics.FromImage( b ) )
         {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            DrawBox( g, 0, 1, 6, 17 );
            // soft reset button
            DrawBox( g, 1, 0, 2, 2 );
            g.DrawImage( VicFont.sChars[113], 8, 8 );
            g.DrawImage( VicFont.sChars[113], 16, 8 );

            bool[] hasExpansion = mSink.emulator.mMemory.hasExpansion;
            for ( int i = 0; i < hasExpansion.Length; ++i )
            {
               int x = hasExpansion[i] ? 5 : 6;
               int tx = hasExpansion[i] ? 6 : 8;
               int w = hasExpansion[i] ? 4 : 5;
               int y = 2 + 3 * i;
               DrawBox( g, x, y, w, 3 );
               VicFont.WriteLine( g, expansionAddresses[i], 8, 8 * ( y + 1 ), 1 );
               VicFont.WriteLine( g, expansionSizes[i], 8 * tx, 8 * ( y + 1 ), 1 );
               if ( hasExpansion[i] )
               {
                  // T junctions
                  g.DrawImage( VicFont.sChars[107], 8 * x, 8 * y );
                  g.DrawImage( VicFont.sChars[107], 8 * x, 8 * ( y + 2 ) );
                  for ( int k = y; k < y + 3; ++k )
                  {
                     VicFont.WriteLine( g, "  ", 8 * ( x + w ), 8 * k, 1 );
                  }
               }
               else
               {
                  g.DrawImage( VicFont.sChars[' '], 8 * ( tx - 1 ), 8 * ( y + 1 ) );
               }
            }
         }
         pictureBox.Refresh();
      }

      int GetScale()
      {
         int xm = pictureBox.Width / b.Width;
         int ym = pictureBox.Height / b.Height;
         int m = Math.Max( 1, Math.Min( xm, ym ) );

         return m;
      }

      int GetX( int m )
      {
         int x = ( pictureBox.Width - m * b.Width ) / 2;
         return x;
      }

      int GetY( int m )
      {
         int y = ( pictureBox.Height - m * b.Height ) / 2;
         return y;
      }

      private void pictureBox_MouseDown( object sender, System.Windows.Forms.MouseEventArgs e )
      {
         bool[] hasExpansion = mSink.emulator.mMemory.hasExpansion;
         int m = GetScale();
         int x = ( e.X - GetX( m ) ) / ( 8 * m );
         int y = ( e.Y - GetY( m ) ) / ( 8 * m );

         if ( y > 1 )
         {
            int i = ( y - 2 ) / 3;
            if ( i < hasExpansion.Length )
            {
               hasExpansion[i] = !hasExpansion[i];
               UpdateImage();
            }
         }
         else if ( ( x == 1 || x == 2 ) && ( y == 0 || y == 1 ) )
         {
            mSink.emulator.mCPU.SoftReset();
         }
      }

      private void pictureBox_Paint( object sender, System.Windows.Forms.PaintEventArgs e )
      {
         e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
         e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

         int m = GetScale();

         e.Graphics.DrawImage( b, GetX( m ), GetY( m ), m * b.Width, m * b.Height );
      }
   }
}
