using System;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Sink_20
{
   public partial class VirtualKeyboard : DockContent
   {
      Bitmap[] mMaps;
      int mCurMap;
      int mOverRow = -1;
      int mOverColumn;
      Sink20 mSink;

      int[][][] mKeys =
      {
         // unmodified
         new int[][]
         {
            new int[] { 31, '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '+', '-', 28, 147 },
            new int[] { 'q' - 96, 'w' - 96, 'e' - 96, 'r' - 96, 't' - 96, 'y' - 96, 'u' - 96, 'i' - 96, 'o' - 96, 'p' - 96, 0, '*', 30 },
            new int[] { 'a' - 96, 's' - 96, 'd' - 96, 'f' - 96, 'g' - 96, 'h' - 96, 'j' - 96, 'k' - 96, 'l' - 96, ':', ';', '='  },
            new int[] { 'z' - 96, 'x' - 96, 'c' - 96, 'v' - 96, 'b' - 96, 'n' - 96, 'm' - 96, ',', '.', '/' },
            new int[] { 209 },
            new int[] { 221, 145, 157 }
         },
         // ctrl (commodore) pressed
         new int[][]
         {
            new int[] { 31, 208, 133, 156, 223, 220, 158, 159, 222, 146, 210, 160, 97, 98, 147 },
            new int[] { 107, 115, 113, 114, 99, 119, 120, 98, 121, 111, 122, 95, 94 },
            new int[] { 112, 110, 108, 123, 101, 116, 117, 97, 108, ':', ';', '=' },
            new int[] { 109, 125, 124, 126, 127, 106, 103, ',', '.', '/' },
            new int[] { 209 },
            new int[] { 221, 145, 157 }
         },
         // shift pressed
         new int[][]
         {
            new int[] { 31, '!', '\"', '#', '$', '%', '&', '\'', '(', ')', '0', 91, 93, 105, 211 },
            new int[] { 'q' -32, 'w' -32, 'e' -32, 'r' -32, 't' -32, 'y' -32, 'u' -32, 'i' -32, 'o' -32, 'p' -32, 121, 95, 30 },
            new int[] { 'a' -32, 's' -32, 'd' -32, 'f' -32, 'g' -32, 'h' -32, 'j' -32, 'k' -32, 'l' -32, 27, 29, '=' },
            new int[] { 'z' -32, 'x' -32, 'c' -32, 'v' -32, 'b' -32, 'n' -32, 'm' -32, '<', '>', '?' },
            new int[] { 209 },
            new int[] { 221, 145, 157 }
         }
      };

      int[] mRowOffsets = new int[] { 0, 12, 8, 12, 120, 111 };

      public VirtualKeyboard( Sink20 sink )
      {
         InitializeComponent();

         mSink = sink;

         mMaps = new Bitmap[mKeys.Length];

         int mx = 0;
         for ( int i = 0; i < mKeys.Length; ++i )
         {
            for ( int y = 0; y < mKeys[i].Length; ++y )
            {
               mx = Math.Max( mx, mRowOffsets[y] + 9 * mKeys[i][y].Length + 1 );
            }
         }

         for ( int i = 0; i < mKeys.Length; ++i )
         {
            Bitmap b = mMaps[i] = new Bitmap( mx, mKeys[i].Length * 9 + 1 );
            Graphics g = Graphics.FromImage( b );

            for ( int y = 0; y < mKeys[i].Length; ++y )
            {
               for ( int x = 0; x < mKeys[i][y].Length; ++x )
               {
                  int code = mKeys[i][y][x];
                  if ( code >= 0 )
                  {
                     g.DrawImage( VicFont.sOutline, 9 * x + mRowOffsets[y], 9 * y );
                     g.DrawImage( VicFont.sChars[code], 9 * x + 1 + mRowOffsets[y], 9 * y + 1 );
                  }
               }
            }
         }

         pictureBox.Paint += GotPaint;
         pictureBox.MouseDown += GotMouseDown;
         pictureBox.MouseMove += GotMouseMove;

         Timer timer = new Timer();
         timer.Interval = 1;
         timer.Tick += Tick;
         timer.Start();
      }

      void Tick( object sender, EventArgs e )
      {
         int newMap = GetCurMap();
         if ( newMap != mCurMap )
         {
            mCurMap = newMap;
            pictureBox.Refresh();
         }
      }

      int GetScale()
      {
         int xm = pictureBox.Width / mMaps[0].Width;
         int ym = pictureBox.Height / mMaps[0].Height;
         int m = Math.Max( 1, Math.Min( xm, ym ) );

         return m;
      }

      int GetX( int m )
      {
         int x = ( pictureBox.Width - m * mMaps[0].Width ) / 2;
         return x;
      }

      int GetY( int m )
      {
         int y = ( pictureBox.Height - m * mMaps[0].Height ) / 2;
         return y;
      }

      int GetCurMap()
      {
         int curMap = ( ModifierKeys & Keys.Shift ) > 0 ? 2 : 0;
         curMap = ( ModifierKeys & Keys.Control ) > 0 ? 1 : curMap;
         return curMap;
      }

      void GotPaint( object sender, PaintEventArgs e )
      {
         e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
         e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

         int m = GetScale();
         int x = GetX( m );
         int y = GetY( m );

         e.Graphics.DrawImage( mMaps[mCurMap], x, y, m * mMaps[0].Width, m * mMaps[0].Height );

         if ( mOverRow != -1 )
         {
            int code = mKeys[mCurMap][mOverRow][mOverColumn];
            VicFont.WriteLine( e.Graphics, code.ToString(), 0, e.ClipRectangle.Bottom - 8 * m, m );
         }
      }

      bool TryGetKeyUnderMouse( out int row, out int column, MouseEventArgs e )
      {
         int m = GetScale();
         int x = GetX( m );
         int y = GetY( m );

         row = 0;
         column = 0;
         if ( e.Y >= y )
         {
            row = ( e.Y - y ) / ( 9 * m );
            if ( row < mKeys[mCurMap].Length && e.X >= x + m * mRowOffsets[row] )
            {
               column = ( e.X - x - m * mRowOffsets[row] ) / ( 9 * m );
               if ( column < mKeys[mCurMap][row].Length )
               {
                  return true;
               }
            }
         }
         return false;
      }

      void GotMouseDown( object sender, MouseEventArgs e )
      {
         int row;
         int column;
         if ( TryGetKeyUnderMouse( out row, out column, e ) )
         {
            int code = mKeys[mCurMap][row][column];
            if ( code >= 0 )
            {
               mSink.editor.GotKeyPress( code );
            }
         }
      }

      void GotMouseMove( object sender, MouseEventArgs e )
      {
         int row;
         int column;
         if ( TryGetKeyUnderMouse( out row, out column, e ) )
         {
            if ( mOverRow != row || mOverColumn != column )
            {
               mOverRow = row;
               mOverColumn = column;
               Refresh();
            }
         }
         else if ( mOverRow != -1 )
         {
            mOverRow = -1;
            Refresh();
         }
      }
   }
}
